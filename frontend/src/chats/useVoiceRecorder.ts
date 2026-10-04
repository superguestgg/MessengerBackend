import { useCallback, useEffect, useRef, useState } from 'react'
import { limits } from '../limits'

// Opus at this bitrate keeps the longest allowed recording under the file size limit (≈ 0.9 MB for 5 minutes).
const bitsPerSecond = 24_000

// Safari records only mp4.
const mimeTypes = ['audio/webm;codecs=opus', 'audio/ogg;codecs=opus', 'audio/mp4']

// Room for the container's own overhead.
const sizeReserve = 32 * 1024

export interface VoiceRecording {
  blob: Blob
  fileName: string
  durationSeconds: number
}

function supportedMimeType() {
  return mimeTypes.find((type) => MediaRecorder.isTypeSupported(type)) ?? ''
}

function extension(mimeType: string) {
  if (mimeType.startsWith('audio/ogg')) return 'ogg'
  if (mimeType.startsWith('audio/mp4')) return 'm4a'
  return 'webm'
}

export const voiceRecordingSupported =
  typeof navigator !== 'undefined' && navigator.mediaDevices != null && typeof MediaRecorder !== 'undefined'

// finish() and hitting a limit both end in onRecorded; cancel() drops the recording.
export function useVoiceRecorder(onRecorded: (recording: VoiceRecording) => void) {
  const [recording, setRecording] = useState(false)
  const [elapsed, setElapsed] = useState(0)
  const [error, setError] = useState<string | null>(null)

  const recorder = useRef<MediaRecorder | null>(null)
  const timer = useRef<ReturnType<typeof setInterval> | null>(null)
  const recorded = useRef(onRecorded)

  useEffect(() => {
    recorded.current = onRecorded
  }, [onRecorded])

  const release = useCallback(() => {
    if (timer.current != null) clearInterval(timer.current)
    timer.current = null
    recorder.current?.stream.getTracks().forEach((track) => track.stop())
    recorder.current = null
    setRecording(false)
  }, [])

  const start = useCallback(async () => {
    if (recorder.current != null) return
    setError(null)
    let stream: MediaStream
    try {
      stream = await navigator.mediaDevices.getUserMedia({ audio: true })
    } catch {
      setError('Нет доступа к микрофону')
      return
    }

    const mimeType = supportedMimeType()
    const media = new MediaRecorder(stream, { mimeType: mimeType || undefined, audioBitsPerSecond: bitsPerSecond })
    const chunks: Blob[] = []
    let size = 0
    const startedAt = Date.now()
    recorder.current = media

    media.ondataavailable = (event) => {
      chunks.push(event.data)
      size += event.data.size
      // Stop before the upload limit, whatever bitrate the browser actually chose.
      if (size > limits.fileMaxBytes - sizeReserve && media.state === 'recording') media.stop()
    }

    media.onstop = () => {
      const durationSeconds = Math.round((Date.now() - startedAt) / 1000)
      const type = media.mimeType || mimeType || 'audio/webm'
      const blob = new Blob(chunks, { type })
      release()
      if (durationSeconds < 1 || blob.size === 0) {
        setError('Слишком короткое голосовое')
        return
      }
      recorded.current({
        blob,
        fileName: `voice.${extension(type)}`,
        durationSeconds: Math.min(durationSeconds, limits.voiceMaxSeconds),
      })
    }

    setElapsed(0)
    timer.current = setInterval(() => {
      const seconds = (Date.now() - startedAt) / 1000
      setElapsed(seconds)
      if (seconds >= limits.voiceMaxSeconds && media.state === 'recording') media.stop()
    }, 250)

    media.start(1000)
    setRecording(true)
  }, [release])

  const finish = useCallback(() => {
    const current = recorder.current
    if (current != null && current.state !== 'inactive') current.stop()
  }, [])

  const cancel = useCallback(() => {
    const current = recorder.current
    if (current == null) return
    current.onstop = null
    if (current.state !== 'inactive') current.stop()
    release()
  }, [release])

  // Leaving the chat must free the microphone.
  useEffect(() => cancel, [cancel])

  return { recording, elapsed, error, start, finish, cancel }
}

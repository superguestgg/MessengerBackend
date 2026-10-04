import { useState } from 'react'
import { errorMessage, type Attachment } from '../api/client'
import { formatDuration, formatSize } from '../format'
import { saveAttachment, useAttachmentUrl } from './files'

interface Props {
  chatId: string
  seq: number
  attachments: Attachment[]
}

export function MessageAttachments({ chatId, seq, attachments }: Props) {
  if (attachments.length === 0) return null
  const images = attachments.filter((attachment) => attachment.kind === 'Image')
  const others = attachments.filter((attachment) => attachment.kind !== 'Image')
  return (
    <div className="attachments">
      {images.length > 0 && (
        <div className="attachment-images">
          {images.map((image) => (
            <ImageAttachment key={image.fileId} chatId={chatId} seq={seq} attachment={image} />
          ))}
        </div>
      )}
      {others.map((attachment) =>
        attachment.kind === 'Voice' ? (
          <VoiceAttachment key={attachment.fileId} chatId={chatId} seq={seq} attachment={attachment} />
        ) : (
          <FileAttachment key={attachment.fileId} chatId={chatId} seq={seq} attachment={attachment} />
        ),
      )}
    </div>
  )
}

interface AttachmentProps {
  chatId: string
  seq: number
  attachment: Attachment
}

function ImageAttachment({ chatId, seq, attachment }: AttachmentProps) {
  const { url, error } = useAttachmentUrl(chatId, seq, attachment.fileId, true)
  if (error != null) return <span className="attachment-error">{attachment.fileName}: {error}</span>
  if (url == null) return <span className="attachment-image placeholder" aria-label={attachment.fileName} />
  return (
    <a href={url} target="_blank" rel="noreferrer" className="attachment-image">
      <img src={url} alt={attachment.fileName} />
    </a>
  )
}

// Chrome's MediaRecorder writes webm without a duration, so the player can't seek.
// Seeking far past the end makes the browser scan the file and find it.
function play(audio: HTMLAudioElement) {
  const start = () => void audio.play().catch(() => undefined)
  if (Number.isFinite(audio.duration)) {
    start()
    return
  }
  audio.addEventListener(
    'timeupdate',
    () => {
      audio.currentTime = 0
      start()
    },
    { once: true },
  )
  audio.currentTime = Number.MAX_SAFE_INTEGER
}

function VoiceAttachment({ chatId, seq, attachment }: AttachmentProps) {
  // Audio is loaded on the first play, not for every voice message in the history.
  const [requested, setRequested] = useState(false)
  const { url, error } = useAttachmentUrl(chatId, seq, attachment.fileId, requested)
  const duration = attachment.durationSeconds != null ? formatDuration(attachment.durationSeconds) : ''

  return (
    <div className="voice">
      {url != null ? (
        <audio className="voice-player" src={url} controls onLoadedMetadata={(event) => play(event.currentTarget)} />
      ) : (
        <button
          type="button"
          className="button small secondary voice-play"
          disabled={requested && error == null}
          onClick={() => setRequested(true)}
        >
          {requested && error == null ? 'Загрузка…' : `▶ Голосовое ${duration}`}
        </button>
      )}
      {error != null && <span className="attachment-error">{error}</span>}
      <Transcript attachment={attachment} />
    </div>
  )
}

function Transcript({ attachment }: { attachment: Attachment }) {
  switch (attachment.transcript?.status) {
    case 'Pending':
      return <div className="transcript muted">Расшифровывается…</div>
    case 'Done':
      return <div className="transcript">{attachment.transcript.text}</div>
    case 'Failed':
      return <div className="transcript muted">Не удалось расшифровать</div>
    default:
      return null
  }
}

function FileAttachment({ chatId, seq, attachment }: AttachmentProps) {
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function save() {
    setSaving(true)
    setError(null)
    try {
      await saveAttachment(chatId, seq, attachment.fileId, attachment.fileName)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="attachment-file">
      <button type="button" className="link-button" disabled={saving} onClick={save} title="Скачать">
        📎 <span className="ellipsis">{attachment.fileName}</span>
      </button>
      <span className="muted small">{formatSize(attachment.size)}</span>
      {error != null && <span className="attachment-error">{error}</span>}
    </div>
  )
}

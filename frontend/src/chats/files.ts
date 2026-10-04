import { useEffect, useState } from 'react'
import { api, errorMessage, isAbort, unwrap, type Message, type UploadedFile } from '../api/client'

// Step one of sending a file: the message then refers to it by fileId.
export function uploadFile(file: Blob, name: string): Promise<UploadedFile> {
  const form = new FormData()
  form.append('file', file, name)
  return unwrap(
    api.POST('/api/files', {
      // Swagger types the binary field as a string; openapi-fetch sends FormData as is.
      body: form as unknown as { file: string },
    }),
  )
}

export async function downloadAttachment(
  chatId: string,
  seq: number,
  fileId: string,
  signal?: AbortSignal,
): Promise<Blob> {
  const blob = await unwrap(
    api.GET('/api/chats/{chatId}/messages/{seq}/attachments/{fileId}', {
      params: { path: { chatId, seq, fileId } },
      parseAs: 'blob',
      signal,
    }),
  )
  return blob as Blob
}

// <img> and <audio> can't send the Authorization header, so the file is fetched and shown by an object URL.
export function useAttachmentUrl(chatId: string, seq: number, fileId: string, enabled: boolean) {
  const [url, setUrl] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!enabled) return
    const controller = new AbortController()
    let objectUrl: string | null = null
    downloadAttachment(chatId, seq, fileId, controller.signal)
      .then((blob) => {
        objectUrl = URL.createObjectURL(blob)
        setUrl(objectUrl)
      })
      .catch((err: unknown) => {
        if (!isAbort(err)) setError(errorMessage(err))
      })
    return () => {
      controller.abort()
      if (objectUrl != null) URL.revokeObjectURL(objectUrl)
    }
  }, [chatId, seq, fileId, enabled])

  return { url, error }
}

export async function saveAttachment(chatId: string, seq: number, fileId: string, fileName: string) {
  const blob = await downloadAttachment(chatId, seq, fileId)
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  link.click()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

// One line for quotes and the reply bar.
export function messagePreview(message: Message) {
  if (message.text != null) return message.text
  const attachments = message.attachments
  if (attachments.some((attachment) => attachment.kind === 'Voice')) return 'Голосовое сообщение'
  if (attachments.length > 0 && attachments.every((attachment) => attachment.kind === 'Image')) {
    return attachments.length === 1 ? 'Фото' : `Фото: ${attachments.length}`
  }
  if (attachments.length === 1) return `Файл: ${attachments[0].fileName}`
  return `Файлы: ${attachments.length}`
}

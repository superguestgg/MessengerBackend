import {
  useCallback,
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
  type ClipboardEvent,
  type DragEvent,
  type FormEvent,
  type KeyboardEvent,
} from 'react'
import { Link, useParams } from 'react-router'
import { api, errorMessage, isAbort, unwrap, type Chat, type Message, type UploadedFile } from '../api/client'
import { useMe } from '../auth/context'
import { BotBadge, ErrorText } from '../components/common'
import { displayName, formatDateTime, formatDuration, formatSize, formatTime, pluralize } from '../format'
import { limits } from '../limits'
import { messagePreview, uploadFile } from './files'
import { MessageAttachments } from './Attachments'
import { MembersPanel } from './MembersPanel'
import { chatTitle, otherMember, useChats } from './model'
import { useChatMessages } from './useChatMessages'
import { useReadMarker } from './useReadMarker'
import { useVoiceRecorder, voiceRecordingSupported, type VoiceRecording } from './useVoiceRecorder'

export function ChatView() {
  const { chatId } = useParams()
  if (chatId == null) return null
  return <ChatRoom key={chatId} chatId={chatId} />
}

// The last message whose top edge is on screen.
function lastVisibleSeq(list: HTMLElement): number | null {
  const bottom = list.getBoundingClientRect().bottom
  const items = list.querySelectorAll<HTMLElement>('li[data-seq]')
  for (let i = items.length - 1; i >= 0; i--) {
    if (items[i].getBoundingClientRect().top < bottom) return Number(items[i].dataset.seq)
  }
  return null
}

function excerpt(text: string) {
  return text.length > 120 ? `${text.slice(0, 120)}…` : text
}

// A file picked for the next message: uploaded at once, sent by id.
interface PendingFile {
  key: number
  name: string
  size: number
  uploaded: UploadedFile | null
  error: string | null
}

let nextPendingKey = 1

function ChatRoom({ chatId }: { chatId: string }) {
  const me = useMe()
  const { refreshChats } = useChats()
  const [chat, setChat] = useState<Chat | null>(null)
  const [chatError, setChatError] = useState<string | null>(null)
  const [chatVersion, setChatVersion] = useState(0)
  const [showMembers, setShowMembers] = useState(false)
  const { messages, loading, error, reconnecting, hasOlder, loadingOlder, loadOlder, addMessage } =
    useChatMessages(chatId)

  const [text, setText] = useState('')
  const [replyTo, setReplyTo] = useState<Message | null>(null)
  const [sending, setSending] = useState(false)
  const [sendError, setSendError] = useState<string | null>(null)
  const [highlighted, setHighlighted] = useState<number | null>(null)
  const [pending, setPending] = useState<PendingFile[]>([])
  const recorder = useVoiceRecorder(sendVoice)
  const markRead = useReadMarker(chatId, refreshChats)

  const listRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLTextAreaElement>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const atBottom = useRef(true)
  const restoreFrom = useRef<{ height: number; top: number } | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    unwrap(api.GET('/api/chats/{chatId}', { params: { path: { chatId } }, signal: controller.signal }))
      .then(setChat)
      .catch((err: unknown) => {
        if (!isAbort(err)) setChatError(errorMessage(err))
      })
    return () => controller.abort()
  }, [chatId, chatVersion])

  // The chat list is ordered by activity: move this chat up when something new arrives.
  const lastSeq = messages.length > 0 ? messages[messages.length - 1].seq : 0
  useEffect(() => {
    if (lastSeq > 0) refreshChats()
  }, [lastSeq, refreshChats])

  useLayoutEffect(() => {
    const list = listRef.current
    if (list == null) return
    if (restoreFrom.current != null) {
      // Older messages were prepended: keep the visible ones in place.
      list.scrollTop = list.scrollHeight - restoreFrom.current.height + restoreFrom.current.top
      restoreFrom.current = null
    } else if (atBottom.current) {
      list.scrollTop = list.scrollHeight
    }
  }, [messages])

  // Only what the user can actually see counts as read: not in a background tab or window.
  const reportRead = useCallback(() => {
    const list = listRef.current
    if (list == null || document.visibilityState !== 'visible' || !document.hasFocus()) return
    const seq = lastVisibleSeq(list)
    if (seq != null) markRead(seq)
  }, [markRead])

  useEffect(() => {
    reportRead()
  }, [messages, reportRead])

  useEffect(() => {
    window.addEventListener('focus', reportRead)
    document.addEventListener('visibilitychange', reportRead)
    return () => {
      window.removeEventListener('focus', reportRead)
      document.removeEventListener('visibilitychange', reportRead)
    }
  }, [reportRead])

  function onScroll() {
    const list = listRef.current
    if (list == null) return
    atBottom.current = list.scrollHeight - list.scrollTop - list.clientHeight < 40
    reportRead()
  }

  async function showOlder() {
    const list = listRef.current
    if (list != null) restoreFrom.current = { height: list.scrollHeight, top: list.scrollTop }
    await loadOlder()
  }

  const messageBySeq = useCallback((seq: number) => messages.find((message) => message.seq === seq), [messages])

  function jumpTo(seq: number) {
    document.getElementById(`msg-${seq}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' })
    setHighlighted(seq)
  }

  useEffect(() => {
    if (highlighted == null) return
    const timer = setTimeout(() => setHighlighted(null), 1600)
    return () => clearTimeout(timer)
  }, [highlighted])

  function startReply(message: Message) {
    setReplyTo(message)
    inputRef.current?.focus()
  }

  const uploading = pending.some((file) => file.uploaded == null && file.error == null)
  const uploaded = pending.flatMap((file) => (file.uploaded != null ? [file.uploaded] : []))

  function addFiles(files: File[]) {
    if (files.length === 0) return
    setSendError(null)
    if (pending.length + files.length > limits.attachmentsMax) {
      setSendError(`В сообщении не больше ${limits.attachmentsMax} файлов`)
      return
    }
    for (const file of files) {
      const key = nextPendingKey++
      const tooLarge = file.size > limits.fileMaxBytes
      setPending((current) => [
        ...current,
        {
          key,
          name: file.name,
          size: file.size,
          uploaded: null,
          error: tooLarge ? `больше ${formatSize(limits.fileMaxBytes)}` : null,
        },
      ])
      if (tooLarge) continue
      uploadFile(file, file.name)
        .then((result) => setPending((current) => current.map((p) => (p.key === key ? { ...p, uploaded: result } : p))))
        .catch((err: unknown) =>
          setPending((current) => current.map((p) => (p.key === key ? { ...p, error: errorMessage(err) } : p))),
        )
    }
  }

  function removePending(key: number) {
    setPending((current) => current.filter((file) => file.key !== key))
  }

  async function send(event?: FormEvent) {
    event?.preventDefault()
    const body = text.trim()
    if ((body === '' && uploaded.length === 0) || sending || uploading) return
    setSending(true)
    setSendError(null)
    try {
      const replyToSeq = replyTo?.seq ?? null
      const fileIds = uploaded.map((file) => file.fileId)
      const result = await unwrap(
        api.POST('/api/chats/{chatId}/messages', {
          params: { path: { chatId } },
          body: { text: body === '' ? null : body, fileIds, replyToSeq },
        }),
      )
      atBottom.current = true
      // A message with files comes back through long polling at once, with attachment kinds set by the server.
      if (fileIds.length === 0) {
        addMessage({
          messageId: result.messageId,
          chatId: result.chatId,
          seq: result.seq,
          authorId: me.accountId,
          authorName: me.profile?.displayName ?? null,
          authorIsBot: me.isBot,
          text: body,
          attachments: [],
          replyToSeq,
          createdAt: result.createdAt,
        })
      }
      setText('')
      setReplyTo(null)
      setPending([])
    } catch (err) {
      setSendError(errorMessage(err))
    } finally {
      setSending(false)
      inputRef.current?.focus()
    }
  }

  async function sendVoice(recording: VoiceRecording) {
    setSending(true)
    setSendError(null)
    try {
      const file = await uploadFile(recording.blob, recording.fileName)
      await unwrap(
        api.POST('/api/chats/{chatId}/messages', {
          params: { path: { chatId } },
          body: {
            voice: { fileId: file.fileId, durationSeconds: recording.durationSeconds },
            replyToSeq: replyTo?.seq ?? null,
          },
        }),
      )
      atBottom.current = true
      setReplyTo(null)
    } catch (err) {
      setSendError(errorMessage(err))
    } finally {
      setSending(false)
    }
  }

  function onPaste(event: ClipboardEvent<HTMLTextAreaElement>) {
    const files = [...event.clipboardData.files]
    if (files.length === 0) return
    event.preventDefault()
    addFiles(files)
  }

  function onDrop(event: DragEvent) {
    if (!event.dataTransfer.types.includes('Files')) return
    event.preventDefault()
    if (!recorder.recording) addFiles([...event.dataTransfer.files])
  }

  function onKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing) {
      event.preventDefault()
      void send()
    } else if (event.key === 'Escape' && replyTo != null) {
      setReplyTo(null)
    }
  }

  if (chatError != null) {
    return (
      <div className="empty-state">
        <ErrorText error={chatError} />
        <Link to="/chats">К списку чатов</Link>
      </div>
    )
  }

  const title = chat != null ? chatTitle(chat, me.accountId) : ''
  const other = chat?.type === 'Direct' ? otherMember(chat, me.accountId) : undefined

  return (
    <div
      className="chat-room"
      onDragOver={(event) => event.dataTransfer.types.includes('Files') && event.preventDefault()}
      onDrop={onDrop}
    >
      <header className="chat-head">
        <Link to="/chats" className="button small ghost back" aria-label="К списку чатов">
          ←
        </Link>
        <div className="grow">
          <h2 className="chat-title">
            <span className="ellipsis">{title}</span>
            {other?.isBot === true && <BotBadge />}
          </h2>
          {chat != null && (
            <span className="muted small">
              {chat.type === 'Group'
                ? `Группа · ${pluralize(chat.members.length, ['участник', 'участника', 'участников'])}`
                : 'Личный чат'}
              {reconnecting && ' · переподключение…'}
            </span>
          )}
        </div>
        <button
          type="button"
          className="button small secondary"
          aria-pressed={showMembers}
          onClick={() => setShowMembers((value) => !value)}
        >
          Участники
        </button>
      </header>

      <div className="chat-body">
        <div className="messages" ref={listRef} onScroll={onScroll} data-testid="messages">
          {hasOlder && (
            <div className="center pad">
              <button type="button" className="button small secondary" disabled={loadingOlder} onClick={showOlder}>
                {loadingOlder ? 'Загрузка…' : 'Показать более ранние'}
              </button>
            </div>
          )}
          {loading && <p className="muted center pad">Загрузка…</p>}
          {!loading && messages.length === 0 && error == null && (
            <p className="muted center pad">Сообщений пока нет — напишите первым.</p>
          )}
          <ErrorText error={error} />
          <ol className="message-list">
            {messages.map((message) => {
              const mine = message.authorId === me.accountId
              const quoted = message.replyToSeq != null ? messageBySeq(message.replyToSeq) : undefined
              const classes = ['message', mine ? 'mine' : '', highlighted === message.seq ? 'highlight' : '']
              return (
                <li key={message.seq} id={`msg-${message.seq}`} className={classes.join(' ')} data-seq={message.seq}>
                  {!mine && (
                    <div className="message-author">
                      {displayName(message.authorName, message.authorId)}
                      {message.authorIsBot && <BotBadge />}
                    </div>
                  )}
                  {message.replyToSeq != null && (
                    <button
                      type="button"
                      className="quote"
                      disabled={quoted == null}
                      onClick={() => message.replyToSeq != null && jumpTo(message.replyToSeq)}
                    >
                      {quoted != null ? (
                        <>
                          <strong>{displayName(quoted.authorName, quoted.authorId)}</strong>
                          <span className="ellipsis">{excerpt(messagePreview(quoted))}</span>
                        </>
                      ) : (
                        <span>Ответ на сообщение #{message.replyToSeq}</span>
                      )}
                    </button>
                  )}
                  <MessageAttachments chatId={chatId} seq={message.seq} attachments={message.attachments} />
                  {message.text != null && <div className="message-text">{message.text}</div>}
                  <div className="message-meta">
                    <time dateTime={message.createdAt} title={formatDateTime(message.createdAt)}>
                      {formatTime(message.createdAt)}
                    </time>
                    <button type="button" className="link-button" onClick={() => startReply(message)}>
                      Ответить
                    </button>
                  </div>
                </li>
              )
            })}
          </ol>
        </div>
        {showMembers && chat != null && (
          <MembersPanel chat={chat} myId={me.accountId} onChanged={() => setChatVersion((v) => v + 1)} />
        )}
      </div>

      <form className="composer" onSubmit={send}>
        {replyTo != null && (
          <div className="reply-bar">
            <div className="grow">
              <strong>Ответ {displayName(replyTo.authorName, replyTo.authorId)}</strong>
              <div className="ellipsis muted">{excerpt(messagePreview(replyTo))}</div>
            </div>
            <button type="button" className="button small ghost" aria-label="Отменить ответ" onClick={() => setReplyTo(null)}>
              ✕
            </button>
          </div>
        )}
        {pending.length > 0 && (
          <ul className="pending-files">
            {pending.map((file) => (
              <li key={file.key} className={file.error != null ? 'pending-file failed' : 'pending-file'}>
                <span className="ellipsis">{file.name}</span>
                <span className="small">{file.error ?? (file.uploaded == null ? 'загрузка…' : formatSize(file.size))}</span>
                <button
                  type="button"
                  className="button small ghost"
                  aria-label={`Убрать ${file.name}`}
                  onClick={() => removePending(file.key)}
                >
                  ✕
                </button>
              </li>
            ))}
          </ul>
        )}
        <ErrorText error={sendError ?? recorder.error} />
        {recorder.recording ? (
          <div className="composer-row">
            <div className="recording grow" role="status">
              <span className="recording-dot" aria-hidden="true" />
              Запись {formatDuration(recorder.elapsed)} из {formatDuration(limits.voiceMaxSeconds)}
            </div>
            <button type="button" className="button secondary" onClick={recorder.cancel}>
              Отмена
            </button>
            <button type="button" className="button" onClick={recorder.finish}>
              Отправить
            </button>
          </div>
        ) : (
          <div className="composer-row">
            <input
              ref={fileInputRef}
              type="file"
              multiple
              hidden
              onChange={(event) => {
                addFiles([...(event.target.files ?? [])])
                event.target.value = ''
              }}
            />
            <button
              type="button"
              className="button secondary icon"
              aria-label="Прикрепить файл"
              title={`Прикрепить файл (до ${formatSize(limits.fileMaxBytes)})`}
              disabled={error != null && messages.length === 0}
              onClick={() => fileInputRef.current?.click()}
            >
              📎
            </button>
            <textarea
              ref={inputRef}
              name="message"
              aria-label="Сообщение"
              placeholder="Сообщение"
              title="Enter — отправить, Shift+Enter — новая строка"
              rows={Math.min(Math.max(text.split('\n').length, 1), 6)}
              maxLength={limits.messageMax}
              value={text}
              onChange={(event) => setText(event.target.value)}
              onKeyDown={onKeyDown}
              onPaste={onPaste}
              disabled={error != null && messages.length === 0}
            />
            {voiceRecordingSupported && text.trim() === '' && pending.length === 0 ? (
              <button
                type="button"
                className="button icon"
                aria-label="Записать голосовое"
                title="Записать голосовое"
                disabled={sending || (error != null && messages.length === 0)}
                onClick={() => void recorder.start()}
              >
                🎤
              </button>
            ) : (
              <button
                type="submit"
                className="button"
                disabled={sending || uploading || (text.trim() === '' && uploaded.length === 0)}
              >
                Отправить
              </button>
            )}
          </div>
        )}
      </form>
    </div>
  )
}

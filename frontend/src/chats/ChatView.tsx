import { useCallback, useEffect, useLayoutEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { Link, useParams } from 'react-router'
import { api, errorMessage, isAbort, unwrap, type Chat, type Message } from '../api/client'
import { useMe } from '../auth/context'
import { BotBadge, ErrorText } from '../components/common'
import { displayName, formatDateTime, formatTime, pluralize } from '../format'
import { limits } from '../limits'
import { MembersPanel } from './MembersPanel'
import { chatTitle, otherMember, useChats } from './model'
import { useChatMessages } from './useChatMessages'

export function ChatView() {
  const { chatId } = useParams()
  if (chatId == null) return null
  return <ChatRoom key={chatId} chatId={chatId} />
}

function excerpt(text: string) {
  return text.length > 120 ? `${text.slice(0, 120)}…` : text
}

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

  const listRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLTextAreaElement>(null)
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

  function onScroll() {
    const list = listRef.current
    if (list == null) return
    atBottom.current = list.scrollHeight - list.scrollTop - list.clientHeight < 40
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

  async function send(event?: FormEvent) {
    event?.preventDefault()
    const body = text.trim()
    if (body === '' || sending) return
    setSending(true)
    setSendError(null)
    try {
      const replyToSeq = replyTo?.seq ?? null
      const result = await unwrap(
        api.POST('/api/chats/{chatId}/messages', { params: { path: { chatId } }, body: { text: body, replyToSeq } }),
      )
      atBottom.current = true
      addMessage({
        messageId: result.messageId,
        chatId: result.chatId,
        seq: result.seq,
        authorId: me.accountId,
        authorName: me.profile?.displayName ?? null,
        authorIsBot: me.isBot,
        text: body,
        replyToSeq,
        createdAt: result.createdAt,
      })
      setText('')
      setReplyTo(null)
    } catch (err) {
      setSendError(errorMessage(err))
    } finally {
      setSending(false)
      inputRef.current?.focus()
    }
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
    <div className="chat-room">
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
                          <span className="ellipsis">{excerpt(quoted.text)}</span>
                        </>
                      ) : (
                        <span>Ответ на сообщение #{message.replyToSeq}</span>
                      )}
                    </button>
                  )}
                  <div className="message-text">{message.text}</div>
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
              <div className="ellipsis muted">{excerpt(replyTo.text)}</div>
            </div>
            <button type="button" className="button small ghost" aria-label="Отменить ответ" onClick={() => setReplyTo(null)}>
              ✕
            </button>
          </div>
        )}
        <ErrorText error={sendError} />
        <div className="composer-row">
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
            disabled={error != null && messages.length === 0}
          />
          <button type="submit" className="button" disabled={sending || text.trim() === ''}>
            Отправить
          </button>
        </div>
      </form>
    </div>
  )
}

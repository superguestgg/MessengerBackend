import { useCallback, useEffect, useMemo, useState } from 'react'
import { NavLink, Outlet, useParams } from 'react-router'
import { api, errorMessage, isAbort, unwrap, type Chat } from '../api/client'
import { useMe } from '../auth/context'
import { BotBadge, ErrorText } from '../components/common'
import { formatShort } from '../format'
import { chatTitle, ChatsContext, otherMember, type ChatsValue } from './model'
import { NewChatDialog } from './NewChatDialog'

// There is no account-wide update stream yet (/api/updates is postponed), so the list is polled.
const refreshMs = 15000

function ChatListItem({ chat, myId }: { chat: Chat; myId: string }) {
  const title = chatTitle(chat, myId)
  const isBot = chat.type === 'Direct' && otherMember(chat, myId)?.isBot === true
  const time = chat.lastMessageAt ?? chat.createdAt

  return (
    <NavLink to={`/chats/${chat.chatId}`} className={({ isActive }) => (isActive ? 'chat-item active' : 'chat-item')}>
      {({ isActive }) => (
        <>
          <span className="avatar" aria-hidden="true">
            {chat.type === 'Group' ? '#' : title.charAt(0).toUpperCase()}
          </span>
          <span className="chat-item-body">
            <span className="chat-item-title">
              <span className="ellipsis">{title}</span>
              {isBot && <BotBadge />}
            </span>
            <span className="muted small">
              {chat.type === 'Group' ? `Группа · ${chat.members.length}` : 'Личный чат'}
            </span>
          </span>
          <span className="chat-item-side">
            <span className="muted small">{formatShort(time)}</span>
            {/* The open chat is being read right now: its counter would only flicker. */}
            {!isActive && chat.unreadCount > 0 && (
              <span className="unread" aria-label={`Непрочитанных: ${chat.unreadCount}`}>
                {chat.unreadCount > 99 ? '99+' : chat.unreadCount}
              </span>
            )}
          </span>
        </>
      )}
    </NavLink>
  )
}

export function ChatsLayout() {
  const me = useMe()
  const { chatId } = useParams()
  const [chats, setChats] = useState<Chat[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const [version, setVersion] = useState(0)

  const refreshChats = useCallback(() => setVersion((v) => v + 1), [])

  useEffect(() => {
    const controller = new AbortController()
    unwrap(api.GET('/api/chats', { signal: controller.signal }))
      .then((result) => {
        setChats(result)
        setError(null)
      })
      .catch((err: unknown) => {
        if (!isAbort(err)) setError(errorMessage(err))
      })
    return () => controller.abort()
  }, [version])

  useEffect(() => {
    const timer = setInterval(refreshChats, refreshMs)
    window.addEventListener('focus', refreshChats)
    return () => {
      clearInterval(timer)
      window.removeEventListener('focus', refreshChats)
    }
  }, [refreshChats])

  const value = useMemo<ChatsValue>(() => ({ chats, refreshChats }), [chats, refreshChats])

  return (
    <ChatsContext.Provider value={value}>
      <div className={chatId != null ? 'chats-layout has-chat' : 'chats-layout'}>
        <aside className="sidebar">
          <div className="sidebar-head">
            <h1>Чаты</h1>
            <button type="button" className="button small" onClick={() => setCreating(true)}>
              Новый чат
            </button>
          </div>
          <ErrorText error={error} />
          {chats == null ? (
            error == null && <p className="muted pad">Загрузка…</p>
          ) : chats.length === 0 ? (
            <p className="muted pad">Чатов пока нет. Начните личный чат или создайте группу.</p>
          ) : (
            <nav className="chat-list" aria-label="Список чатов">
              {chats.map((chat) => (
                <ChatListItem key={chat.chatId} chat={chat} myId={me.accountId} />
              ))}
            </nav>
          )}
        </aside>
        <section className="chat-pane">
          <Outlet />
        </section>
        {creating && <NewChatDialog onClose={() => setCreating(false)} />}
      </div>
    </ChatsContext.Provider>
  )
}

export function NoChatSelected() {
  return (
    <div className="empty-state">
      <p className="muted">Выберите чат слева или начните новый.</p>
    </div>
  )
}

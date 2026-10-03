import { createContext, useContext } from 'react'
import type { Bot, Chat, ChatMember, Message } from '../api/client'
import { displayName } from '../format'

export interface Contact {
  userId: string
  name: string | null
  isBot: boolean
}

export interface ChatsValue {
  chats: Chat[] | null
  refreshChats: () => void
}

export const ChatsContext = createContext<ChatsValue | null>(null)

export function useChats(): ChatsValue {
  const value = useContext(ChatsContext)
  if (value == null) throw new Error('useChats must be used inside ChatsLayout')
  return value
}

export function otherMember(chat: Chat, myId: string): ChatMember | undefined {
  return chat.members.find((member) => member.userId !== myId) ?? chat.members[0]
}

export function chatTitle(chat: Chat, myId: string) {
  if (chat.type === 'Group') return chat.title ?? 'Группа'
  const other = otherMember(chat, myId)
  return other != null ? displayName(other.displayName, other.userId) : 'Личный чат'
}

// There is no user search in the API: people come from shared chats, plus the user's own bots.
export function collectContacts(chats: Chat[], bots: Bot[], myId: string): Contact[] {
  const contacts = new Map<string, Contact>()
  for (const bot of bots) {
    contacts.set(bot.botId, { userId: bot.botId, name: bot.displayName ?? null, isBot: true })
  }
  for (const chat of chats) {
    for (const member of chat.members) {
      if (member.userId === myId || contacts.has(member.userId)) continue
      contacts.set(member.userId, { userId: member.userId, name: member.displayName ?? null, isBot: member.isBot })
    }
  }
  return [...contacts.values()].sort((a, b) =>
    displayName(a.name, a.userId).localeCompare(displayName(b.name, b.userId), 'ru'),
  )
}

export function mergeMessages(current: Message[], incoming: Message[]): Message[] {
  if (incoming.length === 0) return current
  const bySeq = new Map(current.map((message) => [message.seq, message]))
  for (const message of incoming) bySeq.set(message.seq, message)
  return [...bySeq.values()].sort((a, b) => a.seq - b.seq)
}

// Where to start waiting after the latest page. Seq numbers are handed out before the message
// is written, so a gap may be a message still in flight: resume before it, or it would be skipped.
export function resumeAfter(page: Message[]): number {
  for (let i = 1; i < page.length; i++) {
    if (page[i].seq !== page[i - 1].seq + 1) return page[i - 1].seq
  }
  return page.length > 0 ? page[page.length - 1].seq : 0
}

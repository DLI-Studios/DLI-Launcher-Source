import {
  collection,
  doc,
  getDoc,
  setDoc,
  addDoc,
  query,
  where,
  orderBy,
  onSnapshot,
  limit,
} from 'firebase/firestore'
import { db } from '@/lib/firestore'
import { authService } from './authService'
import { friendService, type FriendProfile } from './friendService'

export interface Conversation {
  convId: string
  peerUid: string
  peer: FriendProfile | null
  lastMessage: string
  lastMessageAt: number
  lastSenderUid: string
  unread: boolean
}

export interface ChatMessage {
  id: string
  senderUid: string
  text: string
  createdAt: number
}

const MESSAGES_LIMIT = 200

function currentUid(): string | null {
  return authService.getUser()?.id ?? null
}

export function conversationIdFor(uidA: string, uidB: string): string {
  return [uidA, uidB].sort().join('_')
}

async function fetchProfile(uid: string): Promise<FriendProfile | null> {
  const snap = await getDoc(doc(db, 'users', uid))
  if (!snap.exists()) return null
  const data = snap.data() as Record<string, any>
  const privacy = data.privacy ?? {}
  return {
    uid,
    username: data.username ?? '',
    displayName: data.displayName ?? data.username ?? uid,
    avatar: data.avatar ?? '',
    status: data.status ?? 'offline',
    lastSeen: data.lastSeen ?? 0,
    hidePresence: privacy.showStatus === false,
  }
}

async function ensureConversation(peerUid: string): Promise<string> {
  const me = currentUid()
  if (!me) throw new Error('not-authed')
  if (peerUid === me) throw new Error('self-message')

  const state = await friendService.getRelationState(peerUid)
  if (state !== 'friends') throw new Error('not-friends')

  const convId = conversationIdFor(me, peerUid)
  const ref = doc(db, 'conversations', convId)
  const snap = await getDoc(ref)
  if (!snap.exists()) {
    const now = Date.now()
    await setDoc(ref, {
      participants: [me, peerUid],
      createdAt: now,
      lastMessage: '',
      lastMessageAt: now,
      lastSenderUid: me,
      readBy: { [me]: now, [peerUid]: 0 },
    })
  }
  return convId
}

/** Sohbet listesi: conversations dokümanlarını dinler, eş kullanıcı profilini çözer. */
export function subscribeConversations(callback: (convs: Conversation[]) => void): () => void {
  const me = currentUid()
  if (!me) return () => {}

  const q = query(collection(db, 'conversations'), where('participants', 'array-contains', me))

  return onSnapshot(
    q,
    async (snap) => {
      const convs: Conversation[] = []
      for (const d of snap.docs) {
        const data = d.data()
        const participants: string[] = data.participants ?? []
        const peerUid = participants.find((p: string) => p !== me) ?? ''
        if (!peerUid) continue
        const peer = await fetchProfile(peerUid)
        const readBy: Record<string, number> = data.readBy ?? {}
        convs.push({
          convId: d.id,
          peerUid,
          peer,
          lastMessage: data.lastMessage ?? '',
          lastMessageAt: data.lastMessageAt ?? 0,
          lastSenderUid: data.lastSenderUid ?? '',
          unread: (data.lastMessageAt ?? 0) > (readBy[me] ?? 0) && (data.lastSenderUid ?? '') !== me,
        })
      }
      convs.sort((a, b) => b.lastMessageAt - a.lastMessageAt)
      callback(convs)
    },
    (err) => {
      console.warn('[DLI] conversation snapshot error', err)
    },
  )
}

/** Açık sohbetin mesajlarını dinler (eski → yeni). */
export function subscribeMessages(convId: string, callback: (msgs: ChatMessage[]) => void): () => void {
  const q = query(
    collection(db, 'conversations', convId, 'messages'),
    orderBy('createdAt', 'asc'),
    limit(MESSAGES_LIMIT),
  )

  return onSnapshot(q, (snap) => {
    const msgs: ChatMessage[] = snap.docs.map((d) => {
      const data = d.data()
      return {
        id: d.id,
        senderUid: data.senderUid ?? '',
        text: data.text ?? '',
        createdAt: data.createdAt ?? 0,
      }
    })
    callback(msgs)
  })
}

/** Sohbeti açar (yoksa oluşturur) ve okundu olarak işaretler. */
export async function openConversation(peerUid: string): Promise<string> {
  const me = currentUid()
  if (!me) throw new Error('not-authed')
  const convId = await ensureConversation(peerUid)
  await markConversationRead(convId)
  return convId
}

export async function sendMessage(peerUid: string, text: string): Promise<void> {
  const me = currentUid()
  const trimmed = text.trim()
  if (!me) throw new Error('not-authed')
  if (!trimmed) return

  const convId = await ensureConversation(peerUid)
  const now = Date.now()

  await addDoc(collection(db, 'conversations', convId, 'messages'), {
    senderUid: me,
    text: trimmed,
    createdAt: now,
  })

  await setDoc(
    doc(db, 'conversations', convId),
    {
      lastMessage: trimmed,
      lastMessageAt: now,
      lastSenderUid: me,
      [`readBy.${me}`]: now,
    },
    { merge: true },
  )
}

export async function markConversationRead(convId: string): Promise<void> {
  const me = currentUid()
  if (!me) return
  await setDoc(
    doc(db, 'conversations', convId),
    { [`readBy.${me}`]: Date.now() },
    { merge: true },
  ).catch(() => {})
}

export const messageService = {
  subscribeConversations,
  subscribeMessages,
  openConversation,
  sendMessage,
  markConversationRead,
  conversationIdFor,
}

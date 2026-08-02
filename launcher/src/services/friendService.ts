import {
  collection,
  doc,
  getDoc,
  getDocs,
  setDoc,
  deleteDoc,
  query,
  where,
  onSnapshot,
  writeBatch,
} from 'firebase/firestore'
import { db } from '@/lib/firestore'
import { authService } from './authService'
import type { QueryConstraint } from 'firebase/firestore'
import type { TKey } from '@/lib/i18n'

export interface FriendProfile {
  uid: string
  username: string
  displayName: string
  avatar: string
  status: string
  lastSeen: number
  hidePresence: boolean
  since?: number
}

export interface FriendRequestItem {
  requestId: string
  fromUid: string
  toUid: string
  status: string
  createdAt: number
  profile?: FriendProfile
}

export type RelationState = 'none' | 'pending' | 'friends' | 'incoming'

const HEARTBEAT_MS = 30_000
const PRESENT_WINDOW_MS = 90_000
const ONLINE = 'online'
const OFFLINE = 'offline'

function currentUid(): string | null {
  return authService.getUser()?.id ?? null
}

export function isPresent(status: string, lastSeen: number): boolean {
  return (
    (status === ONLINE || status === 'away' || status === 'dnd') &&
    Date.now() - lastSeen < PRESENT_WINDOW_MS
  )
}

export interface FriendStatusMeta {
  key: 'online' | 'away' | 'dnd' | 'offline'
  textKey: TKey
  isOnline: boolean
}

export function friendStatusMeta(f: {
  status: string
  lastSeen: number
  hidePresence: boolean
}): FriendStatusMeta {
  const present = !f.hidePresence && isPresent(f.status, f.lastSeen)
  if (!present) return { key: 'offline', textKey: 'friends.statusOffline', isOnline: false }

  switch (f.status) {
    case 'away':
      return { key: 'away', textKey: 'friends.statusAway', isOnline: true }
    case 'dnd':
      return { key: 'dnd', textKey: 'friends.statusDnd', isOnline: true }
    case 'invisible':
      return { key: 'offline', textKey: 'friends.statusInvisible', isOnline: false }
    default:
      return { key: 'online', textKey: 'friends.statusOnline', isOnline: true }
  }
}

function toFriendProfile(uid: string, data: Record<string, any>): FriendProfile {
  const privacy = data.privacy ?? {}
  return {
    uid,
    username: data.username ?? '',
    displayName: data.displayName ?? data.username ?? uid,
    avatar: data.avatar ?? '',
    status: data.status ?? OFFLINE,
    lastSeen: data.lastSeen ?? 0,
    hidePresence: privacy.showStatus === false,
  }
}

async function fetchProfile(uid: string): Promise<FriendProfile | null> {
  const snap = await getDoc(doc(db, 'users', uid))
  if (!snap.exists()) return null
  return toFriendProfile(snap.id, snap.data() as Record<string, any>)
}

function sortFriends(list: FriendProfile[]): FriendProfile[] {
  return [...list].sort(
    (a, b) =>
      Number(friendStatusMeta(b).isOnline) - Number(friendStatusMeta(a).isOnline) ||
      a.displayName.localeCompare(b.displayName, 'tr'),
  )
}

// ---- Presence ----

let presenceTimer: ReturnType<typeof setInterval> | null = null

async function writePresence(status: string): Promise<void> {
  const uid = currentUid()
  if (!uid) return
  await setDoc(doc(db, 'users', uid), { status, lastSeen: Date.now() }, { merge: true })
}

/** Login sonrası: profil dokümanını garanti altına alır ve online durumunu başlatır. */
export async function startPresence(): Promise<void> {
  const user = authService.getUser()
  const uid = user?.id
  if (!uid) return

  await setDoc(
    doc(db, 'users', uid),
    {
      uid,
      username: user.username,
      displayName: user.global_name || user.username,
      avatar: user.avatar || '',
    },
    { merge: true },
  ).catch(() => {})

  await writePresence(ONLINE)
  stopPresenceHeartbeat()
  presenceTimer = setInterval(() => {
    writePresence(ONLINE).catch(() => {})
  }, HEARTBEAT_MS)
}

export function stopPresenceHeartbeat(): void {
  if (presenceTimer) {
    clearInterval(presenceTimer)
    presenceTimer = null
  }
}

/** Logout / uygulama kapanışı: durumu çevrimdışı yapar. */
export async function stopPresence(): Promise<void> {
  stopPresenceHeartbeat()
  await writePresence(OFFLINE).catch(() => {})
}

// ---- Search & relation ----

export async function searchUsers(queryText: string): Promise<FriendProfile[]> {
  const uid = currentUid()
  if (!uid) return []

  const q = queryText.trim().toLocaleLowerCase('tr')
  if (!q) return []

  const snap = await getDocs(collection(db, 'users'))
  const results: FriendProfile[] = []

  for (const d of snap.docs) {
    if (d.id === uid) continue
    const data = d.data() as Record<string, any>
    const username = (data.username ?? '').toLocaleLowerCase('tr')
    const displayName = (data.displayName ?? '').toLocaleLowerCase('tr')
    if (username.includes(q) || displayName.includes(q)) {
      results.push(toFriendProfile(d.id, data))
    }
  }

  return results
    .sort((a, b) => a.displayName.localeCompare(b.displayName, 'tr'))
    .slice(0, 20)
}

export async function getRelationState(targetUid: string): Promise<RelationState> {
  const uid = currentUid()
  if (!uid) return 'none'
  if (targetUid === uid) return 'none'

  const [friendDoc, sentDoc, incomingDoc] = await Promise.all([
    getDoc(doc(db, 'friends', uid, 'friends', targetUid)),
    getDoc(doc(db, 'friend_requests', `${uid}_${targetUid}`)),
    getDoc(doc(db, 'friend_requests', `${targetUid}_${uid}`)),
  ])

  if (friendDoc.exists()) return 'friends'
  if (sentDoc.exists() && sentDoc.data()?.status === 'pending') return 'pending'
  if (incomingDoc.exists() && incomingDoc.data()?.status === 'pending') return 'incoming'
  return 'none'
}

// ---- Request operations ----

export async function sendFriendRequest(targetUid: string): Promise<void> {
  const uid = currentUid()
  if (!uid) throw new Error('not-authed')
  if (targetUid === uid) throw new Error('self-request')

  const state = await getRelationState(targetUid)
  if (state === 'friends') throw new Error('already-friends')
  if (state === 'pending') throw new Error('already-sent')
  if (state === 'incoming') {
    throw new Error('incoming-exists')
  }

  const target = await getDoc(doc(db, 'users', targetUid))
  if (target.exists() && target.data()?.privacy?.friendRequests === 'nobody') {
    throw new Error('requests-disabled')
  }

  const requestId = `${uid}_${targetUid}`
  await setDoc(
    doc(db, 'friend_requests', requestId),
    {
      requestId,
      fromUid: uid,
      toUid: targetUid,
      status: 'pending',
      createdAt: Date.now(),
    },
    { merge: true },
  )
}

export async function acceptFriendRequest(requesterUid: string): Promise<void> {
  const uid = currentUid()
  if (!uid) return

  const now = Date.now()
  const batch = writeBatch(db)
  batch.set(doc(db, 'friends', uid, 'friends', requesterUid), {
    friendUid: requesterUid,
    since: now,
  })
  batch.set(doc(db, 'friends', requesterUid, 'friends', uid), {
    friendUid: uid,
    since: now,
  })
  batch.delete(doc(db, 'friend_requests', `${requesterUid}_${uid}`))
  await batch.commit()
}

export async function declineFriendRequest(requesterUid: string): Promise<void> {
  const uid = currentUid()
  if (!uid) return
  await deleteDoc(doc(db, 'friend_requests', `${requesterUid}_${uid}`))
}

export async function cancelSentRequest(targetUid: string): Promise<void> {
  const uid = currentUid()
  if (!uid) return
  await deleteDoc(doc(db, 'friend_requests', `${uid}_${targetUid}`))
}

export async function removeFriend(friendUid: string): Promise<void> {
  const uid = currentUid()
  if (!uid) return

  const batch = writeBatch(db)
  batch.delete(doc(db, 'friends', uid, 'friends', friendUid))
  batch.delete(doc(db, 'friends', friendUid, 'friends', uid))
  await batch.commit()
}

// ---- Reads ----

export async function getFriends(): Promise<FriendProfile[]> {
  const uid = currentUid()
  if (!uid) return []

  const snap = await getDocs(collection(db, 'friends', uid, 'friends'))
  const list: FriendProfile[] = []

  for (const edge of snap.docs) {
    const data = edge.data()
    const fuid = data.friendUid || edge.id
    const profile = await fetchProfile(fuid)
    if (profile) {
      profile.since = data.since
      list.push(profile)
    }
  }

  return sortFriends(list)
}

export async function getIncomingRequests(): Promise<FriendRequestItem[]> {
  const uid = currentUid()
  if (!uid) return []
  return loadRequests([where('toUid', '==', uid)])
}

export async function getSentRequests(): Promise<FriendRequestItem[]> {
  const uid = currentUid()
  if (!uid) return []
  return loadRequests([where('fromUid', '==', uid)])
}

async function loadRequests(constraints: QueryConstraint[]): Promise<FriendRequestItem[]> {
  const uid = currentUid()
  if (!uid) return []

  const q = query(collection(db, 'friend_requests'), ...constraints)
  const snap = await getDocs(q)
  const items: FriendRequestItem[] = []

  for (const d of snap.docs) {
    const data = d.data()
    if (data.status !== 'pending') continue
    const profile = await fetchProfile(data.fromUid)
    items.push({
      requestId: d.id,
      fromUid: data.fromUid,
      toUid: data.toUid,
      status: data.status,
      createdAt: data.createdAt ?? 0,
      profile: profile ?? undefined,
    })
  }

  return items.sort((a, b) => b.createdAt - a.createdAt)
}

// ---- Real-time subscriptions ----

export function subscribeFriends(callback: (friends: FriendProfile[]) => void): () => void {
  const uid = currentUid()
  if (!uid) return () => {}

  let dispose: Array<() => void> = []
  let cache: FriendProfile[] = []
  let disposed = false

  const push = () => {
    if (!disposed) callback(sortFriends(cache))
  }

  const unsubEdges = onSnapshot(collection(db, 'friends', uid, 'friends'), async (snap) => {
    const ids: string[] = []
    const list: FriendProfile[] = []

    for (const edge of snap.docs) {
      const data = edge.data()
      const fuid = data.friendUid || edge.id
      ids.push(fuid)

      const cached = cache.find((f) => f.uid === fuid)
      if (cached) {
        list.push({ ...cached, since: data.since })
        continue
      }
      const profile = await fetchProfile(fuid)
      if (profile) {
        profile.since = data.since
        list.push(profile)
      }
    }

    cache = list
    dispose.forEach((u) => u())
    dispose = ids.map((id) =>
      onSnapshot(doc(db, 'users', id), (userSnap) => {
        if (!userSnap.exists()) return
        const profile = toFriendProfile(id, userSnap.data() as Record<string, any>)
        const idx = cache.findIndex((f) => f.uid === id)
        if (idx >= 0) {
          cache[idx] = { ...profile, since: cache[idx].since }
          push()
        }
      }),
    )
    push()
  })

  return () => {
    disposed = true
    unsubEdges()
    dispose.forEach((u) => u())
  }
}

export function subscribeIncomingRequests(
  callback: (requests: FriendRequestItem[]) => void,
): () => void {
  const uid = currentUid()
  if (!uid) return () => {}

  return onSnapshot(
    query(collection(db, 'friend_requests'), where('toUid', '==', uid)),
    async (snap) => {
      const items: FriendRequestItem[] = []
      for (const d of snap.docs) {
        const data = d.data()
        if (data.status !== 'pending') continue
        const profile = await fetchProfile(data.fromUid)
        items.push({
          requestId: d.id,
          fromUid: data.fromUid,
          toUid: data.toUid,
          status: data.status,
          createdAt: data.createdAt ?? 0,
          profile: profile ?? undefined,
        })
      }
      callback(items.sort((a, b) => b.createdAt - a.createdAt))
    },
  )
}

export const friendService = {
  startPresence,
  stopPresence,
  searchUsers,
  getRelationState,
  sendFriendRequest,
  acceptFriendRequest,
  declineFriendRequest,
  cancelSentRequest,
  removeFriend,
  getFriends,
  getIncomingRequests,
  getSentRequests,
  subscribeFriends,
  subscribeIncomingRequests,
}

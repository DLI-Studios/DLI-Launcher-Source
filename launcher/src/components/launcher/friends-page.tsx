import { useEffect, useRef, useState } from 'react'
import {
  Check,
  Clock,
  Loader2,
  Search,
  Trash2,
  UserCheck,
  UserPlus,
  Users,
  X,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import {
  friendService,
  friendStatusMeta,
  type FriendProfile,
  type FriendRequestItem,
  type RelationState,
} from '@/services/friendService'
import { useI18n } from '@/lib/i18n'
import type { TKey } from '@/lib/i18n'

type Tab = 'friends' | 'requests' | 'add'

const STATUS_DOT: Record<string, string> = {
  online: 'bg-success',
  away: 'bg-gold',
  dnd: 'bg-destructive',
  offline: 'bg-muted-foreground/40',
}

function StatusDot({ friend }: { friend: FriendProfile }) {
  const meta = friendStatusMeta(friend)
  return <span className={cn('size-2.5 rounded-full', STATUS_DOT[meta.key])} />
}

function Avatar({ friend, size = 'size-10', text = 'text-sm' }: { friend: FriendProfile; size?: string; text?: string }) {
  return friend.avatar ? (
    <img src={friend.avatar} alt="" className={cn(size, 'shrink-0 rounded-full border border-primary/30 object-cover')} />
  ) : (
    <div className={cn(size, 'flex shrink-0 items-center justify-center rounded-full bg-primary/20 font-bold text-primary', text)}>
      {(friend.displayName || '?').charAt(0).toUpperCase()}
    </div>
  )
}

function formatRelativeTime(ms: number, t: (key: TKey, vars?: Record<string, string | number>) => string): string {
  if (!ms) return ''
  const diff = Math.max(0, Date.now() - ms)
  const min = Math.floor(diff / 60_000)
  if (min < 1) return t('friends.now')
  if (min < 60) return t('friends.minAgo', { n: min })
  const hours = Math.floor(min / 60)
  if (hours < 24) return t('friends.hourAgo', { n: hours })
  const days = Math.floor(hours / 24)
  if (days < 7) return t('friends.dayAgo', { n: days })
  return new Date(ms).toLocaleDateString('tr-TR')
}

function EmptyState({ icon: Icon, title, subtitle }: { icon: typeof Users; title: string; subtitle: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 rounded-xl border border-dashed border-border bg-card/30 px-6 py-14 text-center">
      <div className="flex size-14 items-center justify-center rounded-2xl bg-primary/10">
        <Icon className="size-7 text-primary" />
      </div>
      <div>
        <p className="text-sm font-bold text-foreground">{title}</p>
        <p className="mt-1 text-xs text-muted-foreground">{subtitle}</p>
      </div>
    </div>
  )
}

export function FriendsPage() {
  const { t, tErr } = useI18n()
  const [tab, setTab] = useState<Tab>('friends')
  const [friends, setFriends] = useState<FriendProfile[]>([])
  const [incoming, setIncoming] = useState<FriendRequestItem[]>([])
  const [sent, setSent] = useState<FriendRequestItem[]>([])
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<FriendProfile[]>([])
  const [relationMap, setRelationMap] = useState<Record<string, RelationState>>({})
  const [searching, setSearching] = useState(false)
  const [error, setError] = useState('')
  const [busyUid, setBusyUid] = useState('')
  const searchTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    const unsub1 = friendService.subscribeFriends(setFriends)
    const unsub2 = friendService.subscribeIncomingRequests(setIncoming)
    friendService.getSentRequests().then(setSent).catch(() => {})
    return () => {
      unsub1()
      unsub2()
    }
  }, [])

  useEffect(() => {
    if (searchTimer.current) clearTimeout(searchTimer.current)
    if (tab !== 'add' || !query.trim()) {
      setResults([])
      setRelationMap({})
      setSearching(false)
      return
    }

    searchTimer.current = setTimeout(async () => {
      setSearching(true)
      setError('')
      try {
        const res = await friendService.searchUsers(query)
        setResults(res)
        const rels: Record<string, RelationState> = {}
        await Promise.all(
          res.map(async (u) => {
            rels[u.uid] = await friendService.getRelationState(u.uid)
          }),
        )
        setRelationMap(rels)
      } catch (e: any) {
        setError(tErr(e?.message, 'friends.searchFailed'))
      } finally {
        setSearching(false)
      }
    }, 350)

    return () => {
      if (searchTimer.current) clearTimeout(searchTimer.current)
    }
  }, [query, tab])

  const run = async (uid: string, fn: () => Promise<void>, successMessage?: (r: RelationState) => void) => {
    setBusyUid(uid)
    setError('')
    try {
      await fn()
      successMessage?.(relationMap[uid])
    } catch (e: any) {
      setError(tErr(e?.message, 'friends.opFailed'))
    } finally {
      setBusyUid('')
    }
  }

  const handleSend = async (uid: string) => {
    await run(uid, () => friendService.sendFriendRequest(uid))
    setRelationMap((m) => ({ ...m, [uid]: 'pending' }))
    friendService.getSentRequests().then(setSent).catch(() => {})
  }

  const handleAccept = async (requesterUid: string) => {
    await run(requesterUid, () => friendService.acceptFriendRequest(requesterUid))
    setRelationMap((m) => ({ ...m, [requesterUid]: 'friends' }))
  }

  const handleDecline = async (requesterUid: string) => {
    await run(requesterUid, () => friendService.declineFriendRequest(requesterUid))
  }

  const handleCancel = async (targetUid: string) => {
    await run(targetUid, () => friendService.cancelSentRequest(targetUid))
    setRelationMap((m) => ({ ...m, [targetUid]: 'none' }))
    friendService.getSentRequests().then(setSent).catch(() => {})
  }

  const handleRemove = async (friendUid: string) => {
    if (!window.confirm(t('friends.removeConfirm'))) return
    await run(friendUid, () => friendService.removeFriend(friendUid))
  }

  const onlineCount = friends.filter((f) => friendStatusMeta(f).isOnline).length

  const tabs: { id: Tab; labelKey: TKey; icon: typeof Users; badge?: number }[] = [
    { id: 'friends', labelKey: 'friends.tabFriends', icon: Users, badge: friends.length },
    { id: 'requests', labelKey: 'friends.tabRequests', icon: UserPlus, badge: incoming.length },
    { id: 'add', labelKey: 'friends.tabAdd', icon: Search },
  ]

  return (
    <div className="flex h-full flex-col gap-5 overflow-y-auto p-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex size-10 items-center justify-center rounded-xl bg-primary/15">
            <Users className="size-5 text-primary" />
          </div>
          <div>
            <h1 className="text-2xl font-black tracking-widest text-foreground uppercase">{t('friends.title')}</h1>
            <p className="text-sm text-muted-foreground">
              {t('friends.subtitle')}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <span className="flex items-center gap-2 rounded-xl border border-border bg-card/50 px-4 py-2 text-xs font-semibold text-muted-foreground">
            <span className="size-2 rounded-full bg-success" />
            {t('friends.online', { n: onlineCount })}
          </span>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-2">
        {tabs.map(({ id, labelKey, icon: Icon, badge }) => (
          <button
            key={id}
            type="button"
            onClick={() => setTab(id)}
            className={cn(
              'relative flex items-center gap-2 rounded-xl px-4 py-2 text-xs font-bold tracking-wider uppercase transition-all duration-200',
              tab === id
                ? 'bg-primary text-primary-foreground'
                : 'border border-border bg-card/50 text-muted-foreground hover:border-primary/40 hover:text-foreground',
            )}
          >
            <Icon className="size-4" />
            {t(labelKey)}
            {typeof badge === 'number' && badge > 0 && (
              <span
                className={cn(
                  'flex min-w-[18px] items-center justify-center rounded-full px-1.5 py-0.5 text-[10px] font-bold',
                  tab === id ? 'bg-white/25 text-white' : 'bg-primary text-white',
                )}
              >
                {badge}
              </span>
            )}
          </button>
        ))}
      </div>

      {error && (
        <div className="rounded-xl border border-destructive/40 bg-destructive/10 px-4 py-2.5 text-xs font-semibold text-destructive">
          {error}
        </div>
      )}

      {/* Friends tab */}
      {tab === 'friends' && (
        <div className="flex flex-col gap-2">
          {friends.length === 0 ? (
            <EmptyState
              icon={Users}
              title={t('friends.noFriends')}
              subtitle={t('friends.noFriendsSub')}
            />
          ) : (
            friends.map((friend) => {
              const meta = friendStatusMeta(friend)
              return (
                <div
                  key={friend.uid}
                  className="flex items-center gap-3 rounded-xl border border-border bg-card/60 px-4 py-3 transition-colors hover:border-primary/40"
                >
                  <div className="relative">
                    <Avatar friend={friend} />
                    <span className="absolute -bottom-0.5 -right-0.5 rounded-full border-2 border-card bg-background">
                      <StatusDot friend={friend} />
                    </span>
                  </div>
                  <div className="flex min-w-0 flex-1 flex-col">
                    <span className="truncate text-sm font-bold text-foreground">{friend.displayName}</span>
                    <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
                      <StatusDot friend={friend} />
                      {t(meta.textKey)}
                    </span>
                  </div>
                  <button
                    type="button"
                    onClick={() => handleRemove(friend.uid)}
                    title={t('friends.removeTitle')}
                    disabled={busyUid === friend.uid}
                    className="flex size-8 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive"
                  >
                    {busyUid === friend.uid ? (
                      <Loader2 className="size-4 animate-spin" />
                    ) : (
                      <Trash2 className="size-4" />
                    )}
                  </button>
                </div>
              )
            })
          )}
        </div>
      )}

      {/* Requests tab */}
      {tab === 'requests' && (
        <div className="flex flex-col gap-6">
          <div className="flex flex-col gap-2">
            <h2 className="flex items-center gap-2 text-xs font-bold tracking-widest text-muted-foreground uppercase">
              <UserCheck className="size-4 text-primary" />
              {t('friends.incomingTitle')}
            </h2>
            {incoming.length === 0 ? (
              <EmptyState
                icon={UserPlus}
                title={t('friends.noIncoming')}
                subtitle={t('friends.noIncomingSub')}
              />
            ) : (
              incoming.map((req) => (
                <div
                  key={req.requestId}
                  className="flex items-center gap-3 rounded-xl border border-border bg-card/60 px-4 py-3"
                >
                  <Avatar friend={req.profile ?? fallbackProfile(req)} />
                  <div className="flex min-w-0 flex-1 flex-col">
                    <span className="truncate text-sm font-bold text-foreground">
                      {req.profile?.displayName || req.fromUid}
                    </span>
                    <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
                      <Clock className="size-3" />
                      {formatRelativeTime(req.createdAt, t)}
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => handleAccept(req.fromUid)}
                      disabled={busyUid === req.fromUid}
                      className="flex items-center gap-1.5 rounded-lg bg-success px-3 py-1.5 text-xs font-bold text-white transition-all hover:brightness-110 active:scale-95"
                    >
                      {busyUid === req.fromUid ? (
                        <Loader2 className="size-3.5 animate-spin" />
                      ) : (
                        <Check className="size-3.5" />
                      )}
                      {t('friends.accept')}
                    </button>
                    <button
                      type="button"
                      onClick={() => handleDecline(req.fromUid)}
                      disabled={busyUid === req.fromUid}
                      className="flex items-center gap-1.5 rounded-lg border border-border bg-card/70 px-3 py-1.5 text-xs font-bold text-muted-foreground transition-colors hover:border-destructive/40 hover:text-destructive"
                    >
                      <X className="size-3.5" />
                      {t('friends.decline')}
                    </button>
                  </div>
                </div>
              ))
            )}
          </div>

          <div className="flex flex-col gap-2">
            <h2 className="flex items-center gap-2 text-xs font-bold tracking-widest text-muted-foreground uppercase">
              <Clock className="size-4 text-primary" />
              {t('friends.sentTitle')}
            </h2>
            {sent.length === 0 ? (
              <p className="rounded-xl border border-dashed border-border bg-card/30 px-4 py-5 text-center text-xs text-muted-foreground">
                {t('friends.noSent')}
              </p>
            ) : (
              sent.map((req) => (
                <div
                  key={req.requestId}
                  className="flex items-center gap-3 rounded-xl border border-border bg-card/60 px-4 py-3"
                >
                  <Avatar friend={req.profile ?? fallbackProfile(req)} />
                  <div className="flex min-w-0 flex-1 flex-col">
                    <span className="truncate text-sm font-bold text-foreground">
                      {req.profile?.displayName || req.toUid}
                    </span>
                    <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
                      <Clock className="size-3" />
                      {formatRelativeTime(req.createdAt, t)}
                    </span>
                  </div>
                  <button
                    type="button"
                    onClick={() => handleCancel(req.toUid)}
                    disabled={busyUid === req.toUid}
                    className="flex items-center gap-1.5 rounded-lg border border-border bg-card/70 px-3 py-1.5 text-xs font-bold text-muted-foreground transition-colors hover:border-destructive/40 hover:text-destructive"
                  >
                    <X className="size-3.5" />
                    {t('friends.cancelRequest')}
                  </button>
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {/* Add tab */}
      {tab === 'add' && (
        <div className="flex flex-col gap-4">
          <div className="relative">
            <Search className="pointer-events-none absolute left-4 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <input
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder={t('friends.searchPlaceholder')}
              autoFocus
              className="w-full rounded-xl border border-border bg-card/60 py-3 pl-11 pr-4 text-sm text-foreground caret-primary placeholder:text-muted-foreground outline-none transition-colors focus:border-primary/60 focus:ring-2 focus:ring-primary/20"
            />
          </div>

          {searching && (
            <div className="flex items-center justify-center gap-2 py-8 text-xs text-muted-foreground">
              <Loader2 className="size-4 animate-spin" />
              {t('friends.searching')}
            </div>
          )}

          {!searching && query.trim() && results.length === 0 && !error && (
            <EmptyState icon={Search} title={t('friends.noResults')} subtitle={t('friends.noResultsSub')} />
          )}

          {!searching && results.length > 0 && (
            <div className="flex flex-col gap-2">
              {results.map((user) => {
                const relation = relationMap[user.uid] ?? 'none'
                return (
                  <div
                    key={user.uid}
                    className="flex items-center gap-3 rounded-xl border border-border bg-card/60 px-4 py-3"
                  >
                    <Avatar friend={user} />
                    <div className="flex min-w-0 flex-1 flex-col">
                      <span className="truncate text-sm font-bold text-foreground">{user.displayName}</span>
                      <span className="truncate text-xs text-muted-foreground">@{user.username}</span>
                    </div>
                    {relation === 'friends' && (
                      <span className="flex items-center gap-1.5 rounded-lg bg-success/15 px-3 py-1.5 text-xs font-bold text-success">
                        <UserCheck className="size-3.5" />
                        {t('friends.friend')}
                      </span>
                    )}
                    {relation === 'pending' && (
                      <span className="flex items-center gap-1.5 rounded-lg border border-border bg-card/70 px-3 py-1.5 text-xs font-bold text-muted-foreground">
                        <Clock className="size-3.5" />
                        {t('friends.requestSent')}
                      </span>
                    )}
                    {relation === 'incoming' && (
                      <button
                        type="button"
                        onClick={() => handleAccept(user.uid)}
                        disabled={busyUid === user.uid}
                        className="flex items-center gap-1.5 rounded-lg bg-success px-3 py-1.5 text-xs font-bold text-white transition-all hover:brightness-110 active:scale-95"
                      >
                        {busyUid === user.uid ? (
                          <Loader2 className="size-3.5 animate-spin" />
                        ) : (
                          <UserCheck className="size-3.5" />
                        )}
                        {t('friends.accept')}
                      </button>
                    )}
                    {relation === 'none' && (
                      <button
                        type="button"
                        onClick={() => handleSend(user.uid)}
                        disabled={busyUid === user.uid}
                        className="flex items-center gap-1.5 rounded-lg bg-primary px-3 py-1.5 text-xs font-bold text-primary-foreground transition-all hover:brightness-110 active:scale-95"
                      >
                        {busyUid === user.uid ? (
                          <Loader2 className="size-3.5 animate-spin" />
                        ) : (
                          <UserPlus className="size-3.5" />
                        )}
                        {t('friends.addFriend')}
                      </button>
                    )}
                  </div>
                )
              })}
            </div>
          )}

          {!searching && !query.trim() && (
            <p className="text-center text-xs text-muted-foreground">
              {t('friends.addHint')}
            </p>
          )}
        </div>
      )}
    </div>
  )
}

function fallbackProfile(req: FriendRequestItem): FriendProfile {
  return {
    uid: req.fromUid,
    username: req.fromUid,
    displayName: req.fromUid,
    avatar: '',
    status: 'offline',
    lastSeen: 0,
    hidePresence: false,
  }
}

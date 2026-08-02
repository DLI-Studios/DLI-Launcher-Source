import { useEffect, useRef, useState } from 'react'
import {
  Search,
  Plus,
  Phone,
  Video,
  Mic,
  Headphones,
  Settings as SettingsIcon,
  Smile,
  Paperclip,
  Send,
  Check,
  CheckCheck,
  MessagesSquare,
  Hash,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { useI18n } from '@/lib/i18n'
import { authService, type DiscordUser } from '@/services/authService'
import { friendStatusMeta } from '@/services/friendService'
import { messageService, type Conversation, type ChatMessage } from '@/services/messageService'
import type { FriendProfile } from '@/services/friendService'

const GROUP_WINDOW_MS = 2 * 60 * 1000

function Avatar({ profile, size = 'size-10', className }: { profile?: FriendProfile | DiscordUser | null; size?: string; className?: string }) {
  const avatar = profile?.avatar
  const name =
    (profile && 'global_name' in profile && profile.global_name) ||
    (profile && 'username' in profile && profile.username) ||
    (profile && 'displayName' in profile && profile.displayName) ||
    '?'
  return avatar ? (
    <img src={avatar} alt="" className={cn(size, 'shrink-0 rounded-full border border-primary/30 object-cover', className)} />
  ) : (
    <div className={cn(size, 'flex shrink-0 items-center justify-center rounded-full border border-primary/30 bg-primary/20 font-black text-primary', className)}>
      {name.charAt(0).toUpperCase()}
    </div>
  )
}

export function MessagesPage() {
  const { t, lang } = useI18n()
  const me = authService.getUser()
  const meUid = me?.id ?? ''

  const [conversations, setConversations] = useState<Conversation[]>([])
  const [activeConvId, setActiveConvId] = useState<string | null>(null)
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [draft, setDraft] = useState('')
  const [search, setSearch] = useState('')
  const [error, setError] = useState('')
  const messagesEndRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const unsub = messageService.subscribeConversations(setConversations)
    return unsub
  }, [])

  const activeConversation = conversations.find((c) => c.convId === activeConvId) ?? null

  useEffect(() => {
    if (!activeConvId) {
      setMessages([])
      return
    }
    const unsub = messageService.subscribeMessages(activeConvId, setMessages)
    return unsub
  }, [activeConvId])

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const handleSelect = async (conv: Conversation) => {
    setActiveConvId(conv.convId)
    setError('')
    messageService.markConversationRead(conv.convId).catch(() => {})
  }

  const tErrFor = (code: string | null | undefined): string => {
    if (code === 'not-friends') return t('messages.notFriends')
    if (code === 'self-message') return t('messages.selfMessage')
    return t('errors.auth.generic', { code: code || 'unknown' })
  }

  const handleSend = async () => {
    const text = draft.trim()
    if (!text || !activeConversation) return
    setDraft('')
    try {
      await messageService.sendMessage(activeConversation.peerUid, text)
      setError('')
    } catch (e: any) {
      setDraft(text)
      setError(tErrFor(e?.message))
    }
  }

  const filtered = search.trim()
    ? conversations.filter((c) => (c.peer?.displayName || c.peerUid).toLocaleLowerCase('tr').includes(search.toLocaleLowerCase('tr')))
    : conversations

  const formatTime = (ts: number) =>
    new Date(ts).toLocaleTimeString(lang === 'tr' ? 'tr-TR' : 'en-GB', { hour: '2-digit', minute: '2-digit' })

  const isSameDay = (a: number, b: number) => {
    const da = new Date(a)
    const db = new Date(b)
    return da.getFullYear() === db.getFullYear() && da.getMonth() === db.getMonth() && da.getDate() === db.getDate()
  }

  const dateLabel = (ts: number): string => {
    const today = new Date()
    const d = new Date(ts)
    if (isSameDay(ts, today.getTime())) return t('messages.today')
    if (isSameDay(ts, today.getTime() - 86400000)) return t('messages.yesterday')
    return d.toLocaleDateString(lang === 'tr' ? 'tr-TR' : 'en-GB', { day: 'numeric', month: 'long', year: 'numeric' })
  }

  const rowTime = (ts: number): string => {
    const d = new Date(ts)
    const today = new Date()
    if (isSameDay(ts, today.getTime())) return formatTime(ts)
    if (isSameDay(ts, today.getTime() - 86400000)) return t('messages.yesterday')
    return d.toLocaleDateString(lang === 'tr' ? 'tr-TR' : 'en-GB', { day: '2-digit', month: '2-digit' })
  }

  return (
    <div className="flex h-full min-h-0 overflow-hidden">
      {/* ─── Left rail (Discord-style DM list) ─── */}
      <aside className="flex w-72 shrink-0 flex-col border-r border-border bg-card/40">
        <div className="flex items-center justify-between px-4 pt-5 pb-3">
          <div className="flex items-center gap-2">
            <span className="flex size-6 items-center justify-center rounded-lg bg-primary/20">
              <MessagesSquare className="size-3.5 text-primary" />
            </span>
            <h2 className="text-sm font-bold tracking-wider text-foreground uppercase">{t('messages.directMessages')}</h2>
          </div>
          <button
            type="button"
            aria-label={t('messages.dm')}
            className="flex size-7 items-center justify-center rounded-lg border border-border bg-card/50 text-muted-foreground transition-colors hover:border-primary/40 hover:text-primary"
          >
            <Plus className="size-4" />
          </button>
        </div>

        <div className="px-3 pb-2">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-3.5 -translate-y-1/2 text-muted-foreground" />
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t('messages.searchPlaceholder')}
              className="w-full rounded-lg border border-border bg-secondary py-2 pl-9 pr-3 text-xs text-foreground caret-primary placeholder:text-muted-foreground outline-none transition-colors focus:border-primary/50"
            />
          </div>
        </div>

        <div className="flex-1 overflow-y-auto px-2 pb-2">
          {filtered.length === 0 ? (
            <div className="flex flex-col items-center gap-2 px-4 py-10 text-center">
              <MessagesSquare className="size-8 text-muted-foreground/40" />
              <span className="text-xs font-semibold text-muted-foreground">{t('messages.emptyConversations')}</span>
              <span className="text-[10px] text-muted-foreground/70">{t('messages.emptyConversationsSub')}</span>
            </div>
          ) : (
            filtered.map((conv) => {
              const meta = friendStatusMeta(conv.peer ?? { status: 'offline', lastSeen: 0, hidePresence: false })
              const isActive = conv.convId === activeConvId
              const preview = conv.lastSenderUid === meUid && conv.lastMessage ? `${t('messages.you')}: ${conv.lastMessage}` : conv.lastMessage
              return (
                <button
                  key={conv.convId}
                  type="button"
                  onClick={() => handleSelect(conv)}
                  className={cn(
                    'group mb-0.5 flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left transition-colors',
                    isActive ? 'bg-secondary/80' : 'hover:bg-secondary/50',
                  )}
                >
                  <div className="relative">
                    <Avatar profile={conv.peer} size="size-10" />
                    <span
                      className={cn(
                        'absolute -bottom-0.5 -right-0.5 size-3 rounded-full border-2 border-card',
                        meta.isOnline ? 'bg-success' : 'bg-muted-foreground/40',
                      )}
                    />
                  </div>
                  <div className="flex min-w-0 flex-1 flex-col">
                    <span className="truncate text-sm font-bold text-foreground">{conv.peer?.displayName || conv.peerUid}</span>
                    <span className={cn('truncate text-[11px]', conv.unread ? 'font-semibold text-foreground' : 'text-muted-foreground')}>
                      {preview || ' '}
                    </span>
                  </div>
                  <div className="flex shrink-0 flex-col items-end gap-1">
                    <span className="text-[10px] text-muted-foreground tabular-nums">{rowTime(conv.lastMessageAt)}</span>
                    {conv.unread && <span className="size-2 rounded-full bg-primary shadow-[0_0_6px] shadow-primary/60" />}
                  </div>
                </button>
              )
            })
          )}
        </div>

        {/* Discord-style user panel */}
        <div className="flex items-center gap-2 border-t border-border bg-card/40 px-3 py-2.5">
          <Avatar profile={me} size="size-8" />
          <div className="flex min-w-0 flex-1 flex-col">
            <span className="truncate text-xs font-bold text-foreground">{me?.global_name || me?.username}</span>
            <span className="text-[10px] text-success">●</span>
          </div>
          <button type="button" aria-label="mic" className="flex size-7 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground">
            <Mic className="size-3.5" />
          </button>
          <button type="button" aria-label="headphones" className="flex size-7 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground">
            <Headphones className="size-3.5" />
          </button>
          <button type="button" aria-label="settings" className="flex size-7 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground">
            <SettingsIcon className="size-3.5" />
          </button>
        </div>
      </aside>

      {/* ─── Chat area ─── */}
      <section className="flex min-w-0 flex-1 flex-col">
        {!activeConversation ? (
          <div className="flex flex-1 flex-col items-center justify-center gap-3 p-10 text-center">
            <div className="flex size-16 items-center justify-center rounded-2xl border border-border bg-card/50">
              <MessagesSquare className="size-7 text-muted-foreground/50" />
            </div>
            <span className="text-base font-bold text-foreground">{t('messages.noSelection')}</span>
            <span className="text-xs text-muted-foreground">{t('messages.noSelectionSub')}</span>
          </div>
        ) : (
          <>
            {/* Header */}
            <div className="flex shrink-0 items-center gap-3 border-b border-border px-5 py-3">
              <Hash className="hidden size-4 text-muted-foreground/40" />
              <Avatar profile={activeConversation.peer} size="size-9" />
              <div className="flex min-w-0 flex-col">
                <span className="truncate text-sm font-bold text-foreground">{activeConversation.peer?.displayName || activeConversation.peerUid}</span>
                <span className="flex items-center gap-1.5 text-[10px] text-muted-foreground">
                  <span className={cn('size-1.5 rounded-full', friendStatusMeta(activeConversation.peer ?? { status: 'offline', lastSeen: 0, hidePresence: false }).isOnline ? 'bg-success' : 'bg-muted-foreground/40')} />
                  {friendStatusMeta(activeConversation.peer ?? { status: 'offline', lastSeen: 0, hidePresence: false }).isOnline ? t('messages.online') : t('messages.offline')}
                </span>
              </div>
              <div className="ml-auto flex items-center gap-1">
                <button type="button" aria-label="call" className="flex size-8 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground">
                  <Phone className="size-4" />
                </button>
                <button type="button" aria-label="video" className="flex size-8 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground">
                  <Video className="size-4" />
                </button>
                <button type="button" aria-label="search" className="flex size-8 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground">
                  <Search className="size-4" />
                </button>
              </div>
            </div>

            {/* Messages */}
            <div className="flex-1 overflow-y-auto bg-background/40 px-6 py-4">
              {messages.length === 0 ? (
                <div className="flex h-full flex-col items-center justify-center gap-3 text-center">
                  <Avatar profile={activeConversation.peer} size="size-16" />
                  <span className="text-base font-bold text-foreground">{activeConversation.peer?.displayName || activeConversation.peerUid}</span>
                  <span className="max-w-xs text-xs text-muted-foreground">{t('messages.chatStart')}</span>
                </div>
              ) : (
                <div className="flex flex-col">
                  {messages.map((msg, i) => {
                    const prev = i > 0 ? messages[i - 1] : null
                    const next = i < messages.length - 1 ? messages[i + 1] : null
                    const isOwn = msg.senderUid === meUid
                    const showDate = !prev || !isSameDay(prev.createdAt, msg.createdAt)
                    const grouped = !!prev && prev.senderUid === msg.senderUid && msg.createdAt - prev.createdAt < GROUP_WINDOW_MS
                    const isLastInGroup =
                      !next || next.senderUid !== msg.senderUid || next.createdAt - msg.createdAt >= GROUP_WINDOW_MS
                    return (
                      <div key={msg.id}>
                        {showDate && (
                          <div className="my-3 flex items-center justify-center">
                            <span className="rounded-full bg-secondary px-3 py-1 text-[10px] font-bold text-muted-foreground uppercase">{dateLabel(msg.createdAt)}</span>
                          </div>
                        )}
                        <div className={cn('flex items-end gap-2', isOwn ? 'justify-end' : 'justify-start', grouped ? 'mt-1' : 'mt-2.5')}>
                          {!isOwn && isLastInGroup && <Avatar profile={activeConversation.peer} size="size-8" />}
                          {!isOwn && !isLastInGroup && <div className="w-8 shrink-0" />}
                          <div
                            className={cn(
                              'max-w-[70%] px-3.5 py-2 text-sm leading-relaxed break-words shadow-sm',
                              isOwn
                                ? 'rounded-2xl rounded-br-md bg-gradient-to-r from-primary to-accent text-white'
                                : 'rounded-2xl rounded-bl-md border border-border bg-card text-foreground',
                            )}
                          >
                            <p>{msg.text}</p>
                            <span className={cn('mt-1 flex items-center justify-end gap-1 text-[10px]', isOwn ? 'text-white/70' : 'text-muted-foreground')}>
                              {formatTime(msg.createdAt)}
                              {isOwn && (isLastInGroup ? <CheckCheck className="size-3.5 text-accent" /> : <Check className="size-3.5" />)}
                            </span>
                          </div>
                        </div>
                      </div>
                    )
                  })}
                  <div ref={messagesEndRef} />
                </div>
              )}
            </div>

            {/* Input */}
            <div className="shrink-0 border-t border-border px-4 py-3">
              {error && <p className="mb-2 text-[11px] font-semibold text-destructive">{error}</p>}
              <div className="flex items-center gap-2 rounded-xl border border-border bg-secondary px-3 py-2 transition-colors focus-within:border-primary/50">
                <button type="button" aria-label="attach" className="flex size-7 shrink-0 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:text-primary">
                  <Paperclip className="size-4" />
                </button>
                <input
                  value={draft}
                  onChange={(e) => setDraft(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' && !e.shiftKey) {
                      e.preventDefault()
                      handleSend()
                    }
                  }}
                  placeholder={t('messages.typePlaceholder')}
                  className="min-w-0 flex-1 bg-transparent text-sm text-foreground caret-primary outline-none placeholder:text-muted-foreground"
                />
                <button type="button" aria-label="emoji" className="flex size-7 shrink-0 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:text-primary">
                  <Smile className="size-4" />
                </button>
                <button
                  type="button"
                  aria-label={t('messages.send')}
                  onClick={handleSend}
                  disabled={!draft.trim()}
                  className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-primary text-primary-foreground shadow-lg shadow-primary/30 transition-all hover:brightness-110 active:scale-95 disabled:opacity-40"
                >
                  <Send className="size-4" />
                </button>
              </div>
            </div>
          </>
        )}
      </section>
    </div>
  )
}

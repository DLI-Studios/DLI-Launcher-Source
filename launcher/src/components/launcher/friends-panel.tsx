import { useEffect, useState } from 'react'
import { ChevronRight, Users } from 'lucide-react'
import { friendService, friendStatusMeta, type FriendProfile } from '@/services/friendService'
import { useI18n } from '@/lib/i18n'

interface FriendsPanelProps {
  onNavigate: (page: string) => void
}

export function FriendsPanel({ onNavigate }: FriendsPanelProps) {
  const { t } = useI18n()
  const [friends, setFriends] = useState<FriendProfile[]>([])

  useEffect(() => friendService.subscribeFriends(setFriends), [])

  const online = friends.filter((f) => friendStatusMeta(f).isOnline)

  return (
    <section className="glass shrink-0 rounded-xl border border-border p-4" aria-label={t('aria.friends')}>
      <div className="flex items-center justify-between">
        <h2 className="text-sm font-bold tracking-widest text-foreground uppercase">{t('friends.panelTitle')}</h2>
        <span className="flex items-center gap-1.5 rounded-full border border-border bg-card/70 px-2 py-0.5 text-[10px] font-bold text-muted-foreground">
          <span className="size-1.5 rounded-full bg-success" />
          {online.length}/{friends.length}
        </span>
      </div>

      <div className="mt-3 flex flex-col gap-2">
        {friends.length === 0 ? (
          <p className="rounded-lg border border-dashed border-border bg-card/30 px-3 py-4 text-center text-[11px] text-muted-foreground">
            {t('friends.noFriendsPanel')}
          </p>
        ) : (
          <>
            {online.slice(0, 4).map((friend) => {
              const meta = friendStatusMeta(friend)
              return (
                <div key={friend.uid} className="flex items-center gap-2.5">
                  {friend.avatar ? (
                    <img
                      src={friend.avatar}
                      alt=""
                      className="size-8 shrink-0 rounded-full border border-primary/30 object-cover"
                    />
                  ) : (
                    <div className="flex size-8 shrink-0 items-center justify-center rounded-full bg-primary/20 text-xs font-bold text-primary">
                      {(friend.displayName || '?').charAt(0).toUpperCase()}
                    </div>
                  )}
                  <div className="flex min-w-0 flex-1 flex-col">
                    <span className="truncate text-xs font-bold text-foreground">{friend.displayName}</span>
                    <span className="text-[10px] text-muted-foreground">{t(meta.textKey)}</span>
                  </div>
                  <span
                    className={
                      meta.key === 'online'
                        ? 'size-2 rounded-full bg-success'
                        : meta.key === 'away'
                          ? 'size-2 rounded-full bg-gold'
                          : meta.key === 'dnd'
                            ? 'size-2 rounded-full bg-destructive'
                            : 'size-2 rounded-full bg-muted-foreground/40'
                    }
                  />
                </div>
              )
            })}
            {online.length > 4 && (
              <p className="text-center text-[10px] font-semibold text-muted-foreground">
                {t('friends.moreOnline', { n: online.length - 4 })}
              </p>
            )}
            {online.length === 0 && (
              <p className="text-center text-[11px] text-muted-foreground">{t('friends.noOneOnline')}</p>
            )}
          </>
        )}
      </div>

      <button
        type="button"
        onClick={() => onNavigate('friends')}
        className="mt-3 flex w-full items-center justify-center gap-1.5 rounded-lg border border-border bg-card/50 px-3 py-2 text-[11px] font-semibold text-muted-foreground transition-colors hover:border-primary/40 hover:text-foreground"
      >
        <Users className="size-3.5" />
        {t('friends.viewFriends')}
        <ChevronRight className="size-3.5" />
      </button>
    </section>
  )
}

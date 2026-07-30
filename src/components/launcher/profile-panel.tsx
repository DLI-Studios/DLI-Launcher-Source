import { useEffect, useState } from 'react'
import { BadgeCheck, Coins, Crown, Gem, LogOut } from 'lucide-react'
import { playerService, type PlayerInfo } from '@/services/playerService'

interface ProfilePanelProps {
  onLogout?: () => void
}

export function ProfilePanel({ onLogout }: ProfilePanelProps) {
  const [player, setPlayer] = useState<PlayerInfo | null>(null)

  useEffect(() => {
    playerService.getPlayer().then(setPlayer)
  }, [])

  if (!player) {
    return (
      <section className="glass shrink-0 rounded-xl border border-border p-4 animate-pulse" aria-label="Player profile">
        <div className="flex items-center gap-4">
          <div className="size-16 rounded-xl bg-secondary" />
          <div className="flex flex-col gap-2">
            <div className="h-5 w-32 rounded bg-secondary" />
            <div className="h-4 w-20 rounded bg-secondary" />
          </div>
        </div>
      </section>
    )
  }

  return (
    <section className="glass shrink-0 rounded-xl border border-border p-4" aria-label="Player profile">
      <div className="flex items-center gap-4">
        {player.avatarUrl ? (
          <img
            src={player.avatarUrl}
            alt={`${player.username} avatar`}
            className="size-16 rounded-xl border-2 border-primary/50 object-cover shadow-[0_0_16px] shadow-primary/30"
          />
        ) : (
          <div className="flex size-16 items-center justify-center rounded-xl border-2 border-primary/50 bg-primary/20 shadow-[0_0_16px] shadow-primary/30">
            <span className="text-2xl font-black text-primary">{player.username.charAt(0).toUpperCase()}</span>
          </div>
        )}
        <div className="flex flex-col gap-1 min-w-0">
          <span className="flex items-center gap-1.5 text-lg font-bold text-foreground truncate">
            {player.username}
            <BadgeCheck className="size-4.5 shrink-0 text-accent" aria-label="Verified" />
          </span>
          {player.isPremium && (
            <span className="flex items-center gap-1.5 text-xs font-bold tracking-wider text-gold uppercase">
              <Crown className="size-3.5" />
              Premium
            </span>
          )}
        </div>
      </div>

      <button
        type="button"
        onClick={() => {
          playerService.invalidateCache()
          onLogout?.()
        }}
        className="mt-4 flex w-full items-center justify-center gap-2 rounded-xl border border-border bg-card/50 px-4 py-2.5 text-xs font-semibold text-muted-foreground transition-all duration-200 hover:border-destructive/40 hover:bg-destructive/10 hover:text-destructive"
      >
        <LogOut className="size-3.5" />
        Log Out
      </button>

      <div className="mt-4 grid grid-cols-2 gap-3">
        <div className="flex items-center gap-3 rounded-xl border border-border bg-card/70 px-4 py-3 transition-colors hover:border-gold/40">
          <span className="flex size-8 items-center justify-center rounded-full bg-gold/15">
            <Coins className="size-4 text-gold" />
          </span>
          <div className="flex flex-col">
            <span className="text-sm font-bold text-foreground">{player.coins.toLocaleString()}</span>
            <span className="text-[10px] text-muted-foreground">DLI Coins</span>
          </div>
        </div>
        <div className="flex items-center gap-3 rounded-xl border border-primary/30 bg-primary/10 px-4 py-3 transition-colors hover:border-primary/60">
          <span className="flex size-8 items-center justify-center rounded-full bg-primary/20">
            <Gem className="size-4 text-accent" />
          </span>
          <div className="flex flex-col">
            <span className="text-sm font-bold text-foreground">{player.gems.toLocaleString()}</span>
            <span className="text-[10px] text-muted-foreground">DLI Gems</span>
          </div>
        </div>
      </div>
    </section>
  )
}

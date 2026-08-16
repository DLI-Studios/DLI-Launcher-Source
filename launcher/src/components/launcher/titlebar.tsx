import { useEffect, useState } from 'react'
import { Bell, Minus, Settings, Square, X, BadgeCheck } from 'lucide-react'
import { launcherBridge } from '@/services/launcherBridge'
import { authService } from '@/services/authService'
import { playerService, type PlayerInfo } from '@/services/playerService'
import { useI18n } from '@/lib/i18n'

export function Titlebar() {
  const { t } = useI18n()
  const [player, setPlayer] = useState<PlayerInfo | null>(null)

  useEffect(() => {
    playerService.getPlayer().then(setPlayer)
  }, [])

  const handleMinimize = () => {
    launcherBridge.send('MINIMIZE_WINDOW')
  }

  const handleMaximize = () => {
    launcherBridge.send('MAXIMIZE_WINDOW')
  }

  const handleClose = () => {
    launcherBridge.send('CLOSE_WINDOW')
  }

  return (
    <header className="flex h-11 shrink-0 items-center justify-between gap-1 px-3">
      <div className="flex items-center gap-2.5 min-w-0">
        {player?.avatarUrl ? (
          <img
            src={player.avatarUrl}
            alt="avatar"
            className="size-7 rounded-lg border border-primary/40 object-cover shrink-0"
          />
        ) : (
          <div className="flex size-7 items-center justify-center rounded-lg border border-primary/40 bg-primary/20 shrink-0">
            <span className="text-xs font-black text-primary">{player?.username?.charAt(0).toUpperCase() || '?'}</span>
          </div>
        )}
        <span className="text-sm font-bold text-foreground truncate">{player?.username || t('common.unknown')}</span>
        <BadgeCheck className="size-4 shrink-0 text-accent" />
      </div>

      <div className="flex items-center gap-1">
        <button
          type="button"
          aria-label={t('aria.notifications')}
          className="flex size-8 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
        >
          <Bell className="size-4" />
        </button>
        <button
          type="button"
          aria-label={t('aria.settings')}
          className="flex size-8 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
        >
          <Settings className="size-4" />
        </button>
        <span className="mx-1 h-4 w-px bg-border" />
        <button
          type="button"
          onClick={handleMinimize}
          aria-label={t('aria.minimize')}
          className="flex size-8 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
        >
          <Minus className="size-4" />
        </button>
        <button
          type="button"
          onClick={handleMaximize}
          aria-label={t('aria.maximize')}
          className="flex size-8 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
        >
          <Square className="size-3.5" />
        </button>
        <button
          type="button"
          onClick={handleClose}
          aria-label={t('aria.close')}
          className="flex size-8 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-destructive hover:text-foreground"
        >
          <X className="size-4" />
        </button>
      </div>
    </header>
  )
}

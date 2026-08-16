import { useEffect, useState } from 'react'
import {
  Home,
  Play,
  Package,
  MessagesSquare,
  Users,
  User,
  Settings,
  List,
  MessageCircle,
  AtSign,
  Clapperboard,
  Globe,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { launcherBridge } from '@/services/launcherBridge'
import { friendService } from '@/services/friendService'
import { useI18n } from '@/lib/i18n'
import type { TKey } from '@/lib/i18n'

const navItems: { id: string; labelKey: TKey; icon: typeof Home }[] = [
  { id: 'home', labelKey: 'nav.home', icon: Home },
  { id: 'messages', labelKey: 'nav.messages', icon: MessagesSquare },
  { id: 'friends', labelKey: 'nav.friends', icon: Users },
  { id: 'play', labelKey: 'nav.play', icon: Play },
  { id: 'versions', labelKey: 'nav.versions', icon: List },
  { id: 'modpacks', labelKey: 'nav.modpacks', icon: Package },
  { id: 'profile', labelKey: 'nav.profile', icon: User },
  { id: 'settings', labelKey: 'nav.settings', icon: Settings },
]

const socials = [
  { label: 'Discord', icon: MessageCircle },
  { label: 'Twitter', icon: AtSign },
  { label: 'YouTube', icon: Clapperboard },
  { label: 'Website', icon: Globe },
]

interface SidebarProps {
  activePage: string
  onNavigate: (page: string) => void
  onLogout?: () => void
}

export function Sidebar({ activePage, onNavigate }: SidebarProps) {
  const { t } = useI18n()
  const [appVersion, setAppVersion] = useState('1.0.0')
  const [pendingRequests, setPendingRequests] = useState(0)

  useEffect(() => {
    launcherBridge.send('GET_VERSION').then((res) => {
      const data = res.data as { version?: string } | null
      if (res.success && data?.version) {
        setAppVersion(data.version)
      }
    }).catch(() => {})
  }, [])

  useEffect(() => {
    const unsub = friendService.subscribeIncomingRequests((reqs) => setPendingRequests(reqs.length))
    return unsub
  }, [])

  return (
    <aside className="flex h-full w-56 shrink-0 flex-col border-r border-sidebar-border bg-sidebar">
      <div className="flex flex-col items-start px-6 pt-7 pb-8">
        <span className="text-3xl font-black italic tracking-tight text-sidebar-foreground">DLI</span>
        <span className="text-[10px] font-semibold tracking-[0.4em] text-muted-foreground uppercase">{t('sidebar.launcher')}</span>
      </div>

      <nav className="flex flex-col gap-1 px-3" aria-label={t('aria.mainNav')}>
        {navItems.map((item) => {
          const Icon = item.icon
          const isActive = activePage === item.id
          return (
            <button
              key={item.id}
              type="button"
              onClick={() => onNavigate(item.id)}
              aria-current={isActive ? 'page' : undefined}
              className={cn(
                'group relative flex items-center gap-3 rounded-lg px-4 py-3 text-sm font-semibold tracking-wide uppercase transition-all duration-200',
                isActive
                  ? 'bg-sidebar-accent text-sidebar-foreground'
                  : 'text-muted-foreground hover:bg-sidebar-accent/60 hover:text-sidebar-foreground',
              )}
            >
              {isActive && (
                <span className="absolute right-0 top-1/2 h-6 w-0.5 -translate-y-1/2 rounded-full bg-primary shadow-[0_0_8px_2px] shadow-primary/60" />
              )}
              <Icon className={cn('size-5 transition-colors', isActive ? 'text-primary' : 'text-muted-foreground group-hover:text-primary')} />
              <span className="flex-1 text-left">{t(item.labelKey)}</span>
              {item.id === 'friends' && pendingRequests > 0 && (
                <span className="flex min-w-[20px] items-center justify-center rounded-full bg-primary px-1.5 py-0.5 text-[10px] font-bold text-primary-foreground">
                  {pendingRequests}
                </span>
              )}
            </button>
          )
        })}
      </nav>

      <div className="flex-1" />

      <div className="mx-4 mb-4 rounded-xl border border-border bg-card/60 px-4 py-3">
        <div className="flex items-center gap-2">
          <span className="relative flex size-2">
            <span className="absolute inline-flex size-full animate-ping rounded-full bg-success opacity-60" />
            <span className="relative inline-flex size-2 rounded-full bg-success" />
          </span>
          <div className="flex flex-col">
            <span className="text-[10px] tracking-widest text-muted-foreground uppercase">{t('sidebar.allSystems')}</span>
            <span className="text-xs font-bold text-foreground">{t('sidebar.operational')}</span>
          </div>
        </div>
      </div>

      <div className="flex items-center gap-2 px-4 pb-3">
        {socials.map(({ label, icon: Icon }) => (
          <button key={label} type="button" aria-label={label} className="flex size-9 items-center justify-center rounded-lg border border-border bg-card/60 text-muted-foreground transition-all duration-200 hover:border-primary/50 hover:text-primary">
            <Icon className="size-4" />
          </button>
        ))}
      </div>

      <p className="px-5 pb-4 text-xs text-muted-foreground">v{appVersion}</p>
    </aside>
  )
}

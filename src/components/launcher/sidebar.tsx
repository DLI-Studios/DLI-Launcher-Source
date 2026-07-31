import { useEffect, useState } from 'react'
import {
  Home,
  Play,
  Package,
  Sparkles,
  ShoppingCart,
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

const navItems = [
  { id: 'home', label: 'Home', icon: Home },
  { id: 'play', label: 'Play', icon: Play },
  { id: 'versions', label: 'Versions', icon: List },
  { id: 'modpacks', label: 'Modpacks', icon: Package },
  { id: 'cosmetics', label: 'Cosmetics', icon: Sparkles },
  { id: 'store', label: 'Store', icon: ShoppingCart },
  { id: 'profile', label: 'Profile', icon: User },
  { id: 'settings', label: 'Settings', icon: Settings },
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
  const [appVersion, setAppVersion] = useState('1.0.0')

  useEffect(() => {
    launcherBridge.send('GET_VERSION').then((res) => {
      const data = res.data as { version?: string } | null
      if (res.success && data?.version) {
        setAppVersion(data.version)
      }
    }).catch(() => {})
  }, [])

  return (
    <aside className="flex h-full w-56 shrink-0 flex-col border-r border-sidebar-border bg-sidebar">
      <div className="flex flex-col items-start px-6 pt-7 pb-8">
        <span className="text-3xl font-black italic tracking-tight text-sidebar-foreground">DLI</span>
        <span className="text-[10px] font-semibold tracking-[0.4em] text-muted-foreground uppercase">Launcher</span>
      </div>

      <nav className="flex flex-col gap-1 px-3" aria-label="Main navigation">
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
              {item.label}
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
            <span className="text-[10px] tracking-widest text-muted-foreground uppercase">All Systems</span>
            <span className="text-xs font-bold text-foreground">OPERATIONAL</span>
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

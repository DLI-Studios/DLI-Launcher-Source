import { useEffect, useState } from 'react'
import { BadgeCheck, Crown, Coins, Gem, Clock, Shield, LogOut, ExternalLink, Copy, Check } from 'lucide-react'
import { authService, type DiscordUser } from '@/services/authService'
import { playerService, type PlayerInfo } from '@/services/playerService'
import { usePlayTime } from '@/hooks/usePlayTime'

interface ProfilePageProps { onLogout: () => void }

export function ProfilePage({ onLogout }: ProfilePageProps) {
  const [user, setUser] = useState<DiscordUser | null>(authService.getUser())
  const [player, setPlayer] = useState<PlayerInfo | null>(null)
  const [copied, setCopied] = useState(false)
  const playTime = usePlayTime()

  useEffect(() => { playerService.getPlayer().then(setPlayer) }, [])

  const avatarUrl = user?.avatar ? `https://cdn.discordapp.com/avatars/${user.id}/${user.avatar}.png?size=256` : null
  const copyId = () => { if (user?.id) { navigator.clipboard.writeText(user.id); setCopied(true); setTimeout(() => setCopied(false), 2000) } }

  return (
    <div className="flex flex-col h-full p-6 gap-5 overflow-y-auto">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-black tracking-widest text-foreground uppercase">Profile</h1>
        <button type="button" onClick={onLogout} className="flex items-center gap-2 rounded-xl border border-destructive/40 bg-destructive/10 px-4 py-2 text-xs font-semibold text-destructive transition-all hover:bg-destructive/20"><LogOut className="size-3.5" />Sign Out</button>
      </div>
      <div className="rounded-xl border border-border bg-card/50 p-6">
        <div className="flex items-center gap-5">
          {avatarUrl ? <img src={avatarUrl} alt="" className="size-20 rounded-2xl border-2 border-primary/50 object-cover shadow-[0_0_20px] shadow-primary/30" /> : <div className="flex size-20 items-center justify-center rounded-2xl border-2 border-primary/50 bg-primary/20 shadow-[0_0_20px] shadow-primary/30"><span className="text-3xl font-black text-primary">{user?.username?.charAt(0).toUpperCase() || '?'}</span></div>}
          <div className="flex flex-col gap-1">
            <div className="flex items-center gap-2"><h2 className="text-2xl font-bold text-foreground">{user?.global_name || user?.username || 'Unknown'}</h2><BadgeCheck className="size-5 text-accent" /></div>
            <p className="text-sm text-muted-foreground">@{user?.username || 'unknown'}</p>
            {player?.isPremium && <span className="flex items-center gap-1.5 text-xs font-bold tracking-wider text-gold uppercase mt-1"><Crown className="size-3.5" />Premium Member</span>}
          </div>
        </div>
      </div>
      <div className="grid grid-cols-4 gap-3">
        <div className="rounded-xl border border-border bg-card/50 p-4 text-center"><Coins className="size-6 text-gold mx-auto mb-2" /><span className="text-xl font-bold text-foreground block">{player?.coins.toLocaleString() || '0'}</span><span className="text-[10px] text-muted-foreground">DLI Coins</span></div>
        <div className="rounded-xl border border-primary/30 bg-primary/10 p-4 text-center"><Gem className="size-6 text-accent mx-auto mb-2" /><span className="text-xl font-bold text-foreground block">{player?.gems.toLocaleString() || '0'}</span><span className="text-[10px] text-muted-foreground">DLI Gems</span></div>
        <div className="rounded-xl border border-border bg-card/50 p-4 text-center"><Clock className="size-6 text-muted-foreground mx-auto mb-2" /><span className="text-xl font-bold text-foreground block">{playTime}</span><span className="text-[10px] text-muted-foreground">Play Time</span></div>
        <div className="rounded-xl border border-border bg-card/50 p-4 text-center"><Shield className="size-6 text-muted-foreground mx-auto mb-2" /><span className="text-xl font-bold text-foreground block">12</span><span className="text-[10px] text-muted-foreground">Achievements</span></div>
      </div>
      <div className="rounded-xl border border-border bg-card/50 p-4 space-y-3">
        <h3 className="text-sm font-bold text-foreground">Account Information</h3>
        <div className="space-y-2">
          <div className="flex items-center justify-between py-2 border-b border-border"><span className="text-xs text-muted-foreground">Discord Username</span><span className="text-xs font-semibold text-foreground">@{user?.username || 'unknown'}</span></div>
          <div className="flex items-center justify-between py-2 border-b border-border"><span className="text-xs text-muted-foreground">Display Name</span><span className="text-xs font-semibold text-foreground">{user?.global_name || user?.username || 'Unknown'}</span></div>
          <div className="flex items-center justify-between py-2 border-b border-border"><span className="text-xs text-muted-foreground">User ID</span><button type="button" onClick={copyId} className="flex items-center gap-1.5 text-xs font-semibold text-foreground hover:text-primary transition-colors">{user?.id || 'Unknown'}{copied ? <Check className="size-3 text-success" /> : <Copy className="size-3 text-muted-foreground" />}</button></div>
          <div className="flex items-center justify-between py-2"><span className="text-xs text-muted-foreground">Minecraft Username</span><span className="text-xs font-semibold text-foreground">{player?.username || 'Not linked'}</span></div>
        </div>
      </div>
      <div className="rounded-xl border border-border bg-card/50 p-4 space-y-3">
        <h3 className="text-sm font-bold text-foreground">Quick Links</h3>
        <div className="grid grid-cols-2 gap-2">
          <a href="https://discord.gg/delicraft" target="_blank" rel="noopener noreferrer" className="flex items-center gap-2 rounded-xl border border-border bg-card/30 px-4 py-3 text-xs font-semibold text-muted-foreground transition-all hover:border-primary/40 hover:text-foreground"><ExternalLink className="size-3.5 text-primary" />Discord Server</a>
          <a href="https://delicraft.net" target="_blank" rel="noopener noreferrer" className="flex items-center gap-2 rounded-xl border border-border bg-card/30 px-4 py-3 text-xs font-semibold text-muted-foreground transition-all hover:border-primary/40 hover:text-foreground"><ExternalLink className="size-3.5 text-primary" />Website</a>
        </div>
      </div>
    </div>
  )
}

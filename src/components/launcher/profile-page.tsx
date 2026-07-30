import { useEffect, useRef, useState, useCallback } from 'react'
import { BadgeCheck, Crown, Coins, Gem, Clock, Shield, LogOut, ExternalLink, Copy, Check, Upload, Loader2, AlertCircle, X, TriangleAlert } from 'lucide-react'
import { authService, type DiscordUser } from '@/services/authService'
import { playerService, type PlayerInfo } from '@/services/playerService'
import { usePlayTime } from '@/hooks/usePlayTime'

interface AvatarError {
  code: string
  message: string
}

const AVATAR_UPLOAD_TIMEOUT = 15000

interface ProfilePageProps {
  onLogout: () => void
}

export function ProfilePage({ onLogout }: ProfilePageProps) {
  const [user, setUser] = useState<DiscordUser | null>(authService.getUser())
  const [player, setPlayer] = useState<PlayerInfo | null>(null)
  const [copied, setCopied] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [uploadError, setUploadError] = useState<AvatarError | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const uploadTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const playTime = usePlayTime()

  useEffect(() => {
    playerService.getPlayer().then(setPlayer)
  }, [])

  const isDiscord = user?.authProvider === 'discord' || (!user?.authProvider && !!user?.avatar)
  const isEmail = user?.authProvider === 'email'

  const avatarUrl = isDiscord && user?.avatar
    ? `https://cdn.discordapp.com/avatars/${user.id}/${user.avatar}.png?size=256`
    : player?.avatarUrl || null

  const copyId = () => {
    if (user?.id) {
      navigator.clipboard.writeText(user.id)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    }
  }

  const handleAvatarSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    if (!file.type.startsWith('image/png')) {
      setUploadError({ code: 'ERR_AVATAR_FORMAT', message: 'Only PNG files are allowed' })
      return
    }

    if (file.size > 2 * 1024 * 1024) {
      setUploadError({ code: 'ERR_AVATAR_SIZE', message: 'File size must be less than 2MB' })
      return
    }

    setUploadError(null)
    setUploading(true)

    const uploadPromise = playerService.uploadAvatar(file)
    const timeoutPromise = new Promise<never>((_, reject) => {
      uploadTimeoutRef.current = setTimeout(() => {
        reject(new Error('TIMEOUT'))
      }, AVATAR_UPLOAD_TIMEOUT)
    })

    try {
      await Promise.race([uploadPromise, timeoutPromise])
      const updated = await playerService.getPlayer()
      setPlayer(updated)
    } catch (err: any) {
      if (err?.message === 'TIMEOUT') {
        setUploadError({ code: 'ERR_AVATAR_TIMEOUT', message: 'Upload timed out after 15 seconds' })
      } else if (err?.message === 'No authenticated user') {
        setUploadError({ code: 'ERR_AVATAR_AUTH', message: 'Session not found, please sign in again' })
      } else {
        setUploadError({ code: 'ERR_AVATAR_STORAGE', message: 'Could not upload avatar to storage' })
      }
    } finally {
      setUploading(false)
      if (uploadTimeoutRef.current) {
        clearTimeout(uploadTimeoutRef.current)
        uploadTimeoutRef.current = null
      }
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  const dismissError = useCallback(() => {
    setUploadError(null)
  }, [])

  return (
    <div className="flex flex-col h-full p-6 gap-5 overflow-y-auto">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-black tracking-widest text-foreground uppercase">Profile</h1>
        <button
          type="button"
          onClick={onLogout}
          className="flex items-center gap-2 rounded-xl border border-destructive/40 bg-destructive/10 px-4 py-2 text-xs font-semibold text-destructive transition-all hover:bg-destructive/20"
        >
          <LogOut className="size-3.5" />
          Sign Out
        </button>
      </div>

      {/* Profile Card */}
      <div className="rounded-xl border border-border bg-card/50 p-6">
        <div className="flex items-center gap-5">
          <div className="relative group shrink-0">
            {avatarUrl ? (
              <img
                src={avatarUrl}
                alt={`${user?.username} avatar`}
                className="size-20 rounded-2xl border-2 border-primary/50 object-cover shadow-[0_0_20px] shadow-primary/30"
              />
            ) : (
              <div className="flex size-20 items-center justify-center rounded-2xl border-2 border-primary/50 bg-primary/20 shadow-[0_0_20px] shadow-primary/30">
                <span className="text-3xl font-black text-primary">
                  {user?.username?.charAt(0).toUpperCase() || '?'}
                </span>
              </div>
            )}
            {isEmail && (
              <>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/png"
                  className="hidden"
                  onChange={handleAvatarSelect}
                />
                <button
                  type="button"
                  onClick={() => fileInputRef.current?.click()}
                  disabled={uploading}
                  className="absolute -bottom-1 -right-1 flex size-8 items-center justify-center rounded-full border-2 border-border bg-card text-foreground shadow-lg transition-all hover:bg-primary hover:text-primary-foreground disabled:opacity-60"
                >
                  {uploading ? <Loader2 className="size-4 animate-spin" /> : <Upload className="size-3.5" />}
                </button>
              </>
            )}
          </div>
          <div className="flex flex-col gap-1">
            <div className="flex items-center gap-2">
              <h2 className="text-2xl font-bold text-foreground">{user?.global_name || user?.username || 'Unknown'}</h2>
              <BadgeCheck className="size-5 text-accent" />
            </div>
            <p className="text-sm text-muted-foreground">@{user?.username || 'unknown'}</p>
            <div className="flex items-center gap-2">
              {player?.isPremium && (
                <span className="flex items-center gap-1.5 text-xs font-bold tracking-wider text-gold uppercase">
                  <Crown className="size-3.5" />
                  Premium Member
                </span>
              )}
              {isEmail && (
                <span className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground/60 border border-border rounded-md px-1.5 py-0.5">
                  Email
                </span>
              )}
              {isDiscord && (
                <span className="text-[10px] font-semibold uppercase tracking-wider text-[#5865F2]/80 border border-[#5865F2]/20 rounded-md px-1.5 py-0.5">
                  Discord
                </span>
              )}
            </div>

          </div>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-4 gap-3">
        <div className="rounded-xl border border-border bg-card/50 p-4 text-center">
          <Coins className="size-6 text-gold mx-auto mb-2" />
          <span className="text-xl font-bold text-foreground block">{player?.coins.toLocaleString() || '0'}</span>
          <span className="text-[10px] text-muted-foreground">DLI Coins</span>
        </div>
        <div className="rounded-xl border border-primary/30 bg-primary/10 p-4 text-center">
          <Gem className="size-6 text-accent mx-auto mb-2" />
          <span className="text-xl font-bold text-foreground block">{player?.gems.toLocaleString() || '0'}</span>
          <span className="text-[10px] text-muted-foreground">DLI Gems</span>
        </div>
        <div className="rounded-xl border border-border bg-card/50 p-4 text-center">
          <Clock className="size-6 text-muted-foreground mx-auto mb-2" />
          <span className="text-xl font-bold text-foreground block">{playTime}</span>
          <span className="text-[10px] text-muted-foreground">Play Time</span>
        </div>
        <div className="rounded-xl border border-border bg-card/50 p-4 text-center">
          <Shield className="size-6 text-muted-foreground mx-auto mb-2" />
          <span className="text-xl font-bold text-foreground block">12</span>
          <span className="text-[10px] text-muted-foreground">Achievements</span>
        </div>
      </div>

      {/* Account Info */}
      <div className="rounded-xl border border-border bg-card/50 p-4 space-y-3">
        <h3 className="text-sm font-bold text-foreground">Account Information</h3>
        <div className="space-y-2">
          <div className="flex items-center justify-between py-2 border-b border-border">
            <span className="text-xs text-muted-foreground">Username</span>
            <span className="text-xs font-semibold text-foreground">@{user?.username || 'unknown'}</span>
          </div>
          <div className="flex items-center justify-between py-2 border-b border-border">
            <span className="text-xs text-muted-foreground">Display Name</span>
            <span className="text-xs font-semibold text-foreground">{user?.global_name || user?.username || 'Unknown'}</span>
          </div>
          <div className="flex items-center justify-between py-2 border-b border-border">
            <span className="text-xs text-muted-foreground">User ID</span>
            <button
              type="button"
              onClick={copyId}
              className="flex items-center gap-1.5 text-xs font-semibold text-foreground hover:text-primary transition-colors"
            >
              {user?.id || 'Unknown'}
              {copied ? <Check className="size-3 text-success" /> : <Copy className="size-3 text-muted-foreground" />}
            </button>
          </div>
          <div className="flex items-center justify-between py-2">
            <span className="text-xs text-muted-foreground">Minecraft Username</span>
            <span className="text-xs font-semibold text-foreground">{player?.username || 'Not linked'}</span>
          </div>
        </div>
      </div>

      {/* Quick Links */}
      <div className="rounded-xl border border-border bg-card/50 p-4 space-y-3">
        <h3 className="text-sm font-bold text-foreground">Quick Links</h3>
        <div className="grid grid-cols-2 gap-2">
          <a
            href="https://discord.gg/delicraft"
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center gap-2 rounded-xl border border-border bg-card/30 px-4 py-3 text-xs font-semibold text-muted-foreground transition-all hover:border-primary/40 hover:text-foreground"
          >
            <ExternalLink className="size-3.5 text-primary" />
            Discord Server
          </a>
          <a
            href="https://delicraft.net"
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center gap-2 rounded-xl border border-border bg-card/30 px-4 py-3 text-xs font-semibold text-muted-foreground transition-all hover:border-primary/40 hover:text-foreground"
          >
            <ExternalLink className="size-3.5 text-primary" />
            Website
          </a>
        </div>
      </div>

      {/* Error Popup */}
      {uploadError && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
          <div className="mx-4 w-full max-w-sm rounded-2xl border border-destructive/30 bg-card p-6 shadow-2xl shadow-destructive/10">
            <div className="flex items-start justify-between">
              <div className="flex items-center gap-3">
                <div className="flex size-10 items-center justify-center rounded-full bg-destructive/15">
                  <TriangleAlert className="size-5 text-destructive" />
                </div>
                <div>
                  <h3 className="text-sm font-bold text-foreground">Upload Failed</h3>
                  <p className="text-xs text-muted-foreground mt-0.5">{uploadError.message}</p>
                </div>
              </div>
              <button
                type="button"
                onClick={dismissError}
                className="flex size-7 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive"
              >
                <X className="size-4" />
              </button>
            </div>
            <div className="mt-4 rounded-lg border border-border bg-card/50 px-3 py-2">
              <span className="text-[10px] font-mono text-muted-foreground">{uploadError.code}</span>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

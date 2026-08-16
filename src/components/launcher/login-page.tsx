import { useState, useEffect } from 'react'
import { Loader2, Check } from 'lucide-react'
import { authService } from '@/services/authService'

export function LoginPage() {
  const [loading, setLoading] = useState(false)
  const [remember, setRemember] = useState(true)
  const [mounted, setMounted] = useState(false)

  useEffect(() => { const t = setTimeout(() => setMounted(true), 50); return () => clearTimeout(t) }, [])

  const handleDiscordLogin = () => { setLoading(true); authService.login() }

  return (
    <div className="relative flex h-screen w-screen items-center justify-center overflow-hidden bg-background">
      <div className="absolute inset-0">
        <div className="absolute left-1/2 top-1/2 h-[600px] w-[600px] -translate-x-1/2 -translate-y-1/2 rounded-full bg-primary/8 blur-[120px]" />
        <div className="absolute bottom-0 left-0 h-[400px] w-[400px] -translate-x-1/2 translate-y-1/2 rounded-full bg-accent/5 blur-[100px]" />
        <div className="absolute right-0 top-0 h-[300px] w-[300px] translate-x-1/2 -translate-y-1/2 rounded-full bg-primary/5 blur-[80px]" />
      </div>
      <div className="absolute inset-0 opacity-[0.03]" style={{ backgroundImage: 'linear-gradient(oklch(0.58 0.25 295 / 0.3) 1px, transparent 1px), linear-gradient(90deg, oklch(0.58 0.25 295 / 0.3) 1px, transparent 1px)', backgroundSize: '60px 60px' }} />
      <div className="relative z-10 flex w-full max-w-md flex-col items-center transition-all duration-700 ease-out" style={{ opacity: mounted ? 1 : 0, transform: mounted ? 'translateY(0)' : 'translateY(30px)' }}>
        <div className="mb-2 flex flex-col items-center">
          <div className="mb-6 flex items-center gap-1">
            <span className="text-6xl font-black italic tracking-tight text-foreground drop-shadow-[0_0_30px_rgba(168,85,247,0.6)]">DLI</span>
          </div>
          <p className="text-sm font-semibold tracking-[0.5em] text-foreground/70 uppercase">Gaming Platform</p>
        </div>
        <div className="glass mt-8 w-full rounded-2xl border border-border p-8 shadow-2xl shadow-primary/5">
          <div className="mb-8 text-center">
            <h2 className="text-xl font-bold text-foreground">Welcome Back</h2>
            <p className="mt-2 text-sm text-muted-foreground">Sign in with your Discord account to continue</p>
          </div>
          <button type="button" onClick={handleDiscordLogin} disabled={loading}
            className="group relative flex w-full items-center justify-center gap-3 overflow-hidden rounded-xl bg-[#5865F2] px-6 py-4 text-base font-bold text-white shadow-lg shadow-[#5865F2]/25 transition-all duration-300 hover:bg-[#4752C4] hover:shadow-xl hover:shadow-[#5865F2]/35 hover:scale-[1.02] active:scale-[0.98] disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100">
            <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/10 to-transparent opacity-0 transition-opacity duration-500 group-hover:opacity-100" />
            {loading ? <Loader2 className="size-5 animate-spin" /> : <svg className="size-5" viewBox="0 0 24 24" fill="currentColor"><path d="M20.317 4.37a19.791 19.791 0 0 0-4.885-1.515.074.074 0 0 0-.079.037c-.21.375-.444.864-.608 1.25a18.27 18.27 0 0 0-5.487 0 12.64 12.64 0 0 0-.617-1.25.077.077 0 0 0-.079-.037A19.736 19.736 0 0 0 3.677 4.37a.07.07 0 0 0-.032.027C.533 9.046-.32 13.58.099 18.057a.082.082 0 0 0 .031.057 19.9 19.9 0 0 0 5.993 3.03.078.078 0 0 0 .084-.028c.462-.63.874-1.295 1.226-1.994a.076.076 0 0 0-.041-.106 13.107 13.107 0 0 1-1.872-.892.077.077 0 0 1-.008-.128 10.2 10.2 0 0 0 .372-.292.074.074 0 0 1 .077-.01c3.928 1.793 8.18 1.793 12.062 0a.074.074 0 0 1 .078.01c.12.098.246.198.373.292a.077.077 0 0 1-.006.127 12.299 12.299 0 0 1-1.873.892.077.077 0 0 0-.041.107c.36.698.772 1.362 1.225 1.993a.076.076 0 0 0 .084.028 19.839 19.839 0 0 0 6.002-3.03.077.077 0 0 0 .032-.054c.5-5.177-.838-9.674-3.549-13.66a.061.061 0 0 0-.031-.03zM8.02 15.33c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.956-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.956 2.418-2.157 2.418zm7.975 0c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.955-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.946 2.418-2.157 2.418z" /></svg>}
            <span className="relative z-10">{loading ? 'Redirecting...' : 'Sign in with Discord'}</span>
          </button>
          <button type="button" onClick={() => setRemember(!remember)} className="mt-4 flex w-full items-center gap-3 rounded-xl border border-border bg-card/30 px-4 py-3 text-sm text-muted-foreground transition-all duration-200 hover:border-primary/30 hover:text-foreground">
            <div className={`flex size-5 shrink-0 items-center justify-center rounded-md border transition-all duration-200 ${remember ? 'border-primary bg-primary text-white' : 'border-border bg-transparent text-transparent hover:border-primary/50'}`}>
              {remember && <Check className="size-3.5" strokeWidth={3} />}
            </div>
            Remember Me
          </button>
        </div>
        <p className="mt-6 text-xs text-muted-foreground/60">By signing in, you agree to our <span className="text-primary/80">Terms of Service</span>.</p>
      </div>
    </div>
  )
}

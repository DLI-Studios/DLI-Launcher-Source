import { useState, useEffect } from 'react'
import { Loader2, Check, Mail, Lock, User, ArrowRight, RefreshCw, CheckCircle2 } from 'lucide-react'
import { authService } from '@/services/authService'

type AuthTab = 'email' | 'discord'
type EmailMode = 'login' | 'register' | 'verify'

export function LoginPage() {
  const [loading, setLoading] = useState(false)
  const [remember, setRemember] = useState(true)
  const [mounted, setMounted] = useState(false)
  const [authTab, setAuthTab] = useState<AuthTab>('email')
  const [emailMode, setEmailMode] = useState<EmailMode>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [username, setUsername] = useState('')
  const [errorMsg, setErrorMsg] = useState('')
  const [resendCooldown, setResendCooldown] = useState(0)

  useEffect(() => {
    const t = setTimeout(() => setMounted(true), 50)
    return () => clearTimeout(t)
  }, [])

  useEffect(() => {
    if (resendCooldown > 0) {
      const t = setInterval(() => setResendCooldown((c) => c - 1), 1000)
      return () => clearInterval(t)
    }
  }, [resendCooldown])

  const handleDiscordLogin = () => {
    setLoading(true)
    authService.login()
  }

  const handleEmailSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setErrorMsg('')
    setLoading(true)

    try {
      if (emailMode === 'login') {
        const res = await authService.loginWithEmail(email, password)
        if (!res.success) {
          if (res.needsVerification) {
            setEmailMode('verify')
          } else {
            setErrorMsg(res.error || 'Giriş yapılamadı.')
          }
        }
      } else if (emailMode === 'register') {
        const res = await authService.registerWithEmail(email, password, username)
        if (res.success) {
          setEmailMode('verify')
          setResendCooldown(60)
        } else {
          setErrorMsg(res.error || 'Kayıt olunamadı.')
        }
      }
    } catch (err: any) {
      setErrorMsg(err.message || 'Bir hata oluştu.')
    } finally {
      setLoading(false)
    }
  }

  const handleResendVerification = async () => {
    if (resendCooldown > 0) return
    setLoading(true)
    setErrorMsg('')
    try {
      const res = await authService.resendVerificationEmail(email, password)
      if (res.success) {
        setResendCooldown(60)
      } else {
        setErrorMsg(res.error || 'E-posta gönderilemedi.')
      }
    } catch {
      setErrorMsg('E-posta gönderilemedi. Lütfen tekrar deneyin.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="relative flex h-screen w-screen items-center justify-center overflow-hidden bg-background">
      {/* Animated background */}
      <div className="absolute inset-0">
        <div className="absolute left-1/2 top-1/2 h-[600px] w-[600px] -translate-x-1/2 -translate-y-1/2 rounded-full bg-primary/10 blur-[120px]" />
        <div className="absolute bottom-0 left-0 h-[400px] w-[400px] -translate-x-1/2 translate-y-1/2 rounded-full bg-purple-600/10 blur-[100px]" />
        <div className="absolute right-0 top-0 h-[300px] w-[300px] translate-x-1/2 -translate-y-1/2 rounded-full bg-primary/5 blur-[80px]" />
      </div>

      {/* Grid pattern overlay */}
      <div
        className="absolute inset-0 opacity-[0.03]"
        style={{
          backgroundImage:
            'linear-gradient(oklch(0.58 0.25 295 / 0.3) 1px, transparent 1px), linear-gradient(90deg, oklch(0.58 0.25 295 / 0.3) 1px, transparent 1px)',
          backgroundSize: '60px 60px',
        }}
      />

      {/* Login card */}
      <div
        className="relative z-10 flex w-full max-w-md flex-col items-center px-4 transition-all duration-700 ease-out"
        style={{
          opacity: mounted ? 1 : 0,
          transform: mounted ? 'translateY(0)' : 'translateY(30px)',
        }}
      >
        {/* Logo */}
        <div className="mb-2 flex flex-col items-center text-center">
          <div className="mb-2 flex items-center gap-1">
            <span className="text-6xl font-black italic tracking-tight text-foreground drop-shadow-[0_0_30px_rgba(168,85,247,0.6)]">
              DLI
            </span>
          </div>
          <p className="text-xs font-bold tracking-[0.4em] text-foreground/70 uppercase">Gaming Platform</p>
        </div>

        {/* Card */}
        <div className="glass mt-6 w-full rounded-2xl border border-border p-6 shadow-2xl shadow-primary/5 backdrop-blur-xl">

          {/* Doğrulama Bekleniyor Ekranı */}
          {emailMode === 'verify' ? (
            <div className="flex flex-col items-center text-center py-2">
              <div className="mb-5 flex h-16 w-16 items-center justify-center rounded-2xl bg-gradient-to-br from-primary/30 to-purple-600/30 border border-primary/30">
                <Mail className="h-8 w-8 text-primary" />
              </div>
              <h2 className="text-xl font-black italic text-white mb-2">E-Posta Doğrulaması</h2>
              <p className="text-sm text-muted-foreground mb-1">
                <span className="font-semibold text-primary">{email}</span> adresine doğrulama bağlantısı gönderildi.
              </p>
              <p className="text-xs text-muted-foreground mb-6">
                E-postanızdaki bağlantıya tıkladıktan sonra aşağıdan giriş yapabilirsiniz.
              </p>

              {errorMsg && (
                <div className="mb-4 w-full rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-xs font-medium text-red-400">
                  {errorMsg}
                </div>
              )}

              <div className="flex w-full flex-col gap-3">
                <button
                  type="button"
                  onClick={() => { setEmailMode('login'); setErrorMsg('') }}
                  className="flex w-full items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-primary to-purple-600 py-3 text-sm font-bold text-white shadow-lg shadow-primary/25 hover:brightness-110 active:scale-[0.98] transition-all"
                >
                  <CheckCircle2 className="h-4 w-4" />
                  Doğruladım, Giriş Yap
                </button>

                <button
                  type="button"
                  onClick={handleResendVerification}
                  disabled={resendCooldown > 0 || loading}
                  className="flex w-full items-center justify-center gap-2 rounded-xl border border-border bg-card/30 py-3 text-xs font-semibold text-muted-foreground hover:border-primary/30 hover:text-white transition-all disabled:opacity-50"
                >
                  {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : <RefreshCw className="h-4 w-4" />}
                  {resendCooldown > 0 ? `Tekrar gönder (${resendCooldown}s)` : 'Tekrar Gönder'}
                </button>
              </div>
            </div>
          ) : (
            <>
              {/* Tab Switcher */}
              <div className="mb-5 flex rounded-xl border border-white/10 bg-black/40 p-1">
                <button
                  type="button"
                  onClick={() => { setAuthTab('email'); setErrorMsg('') }}
                  className={`flex-1 rounded-lg py-2 text-xs font-bold uppercase tracking-wider transition-all duration-200 ${
                    authTab === 'email'
                      ? 'bg-primary text-white shadow-lg shadow-primary/30'
                      : 'text-muted-foreground hover:text-white'
                  }`}
                >
                  E-Posta
                </button>
                <button
                  type="button"
                  onClick={() => { setAuthTab('discord'); setErrorMsg('') }}
                  className={`flex-1 rounded-lg py-2 text-xs font-bold uppercase tracking-wider transition-all duration-200 ${
                    authTab === 'discord'
                      ? 'bg-[#5865F2] text-white shadow-lg shadow-[#5865F2]/30'
                      : 'text-muted-foreground hover:text-white'
                  }`}
                >
                  Discord
                </button>
              </div>

              {errorMsg && (
                <div className="mb-4 rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-xs font-medium text-red-400">
                  {errorMsg}
                </div>
              )}

              {/* E-POSTA */}
              {authTab === 'email' && (
                <form onSubmit={handleEmailSubmit} className="space-y-3">
                  <div className="text-center mb-4">
                    <h2 className="text-lg font-bold text-foreground">
                      {emailMode === 'login' ? 'Giriş Yap' : 'Hesap Oluştur'}
                    </h2>
                    <p className="mt-1 text-xs text-muted-foreground">
                      {emailMode === 'login'
                        ? 'DLI hesabınızla oturum açın'
                        : 'E-postanıza doğrulama bağlantısı göndereceğiz'}
                    </p>
                  </div>

                  {emailMode === 'register' && (
                    <div className="relative">
                      <User className="absolute left-3.5 top-3.5 h-4 w-4 text-muted-foreground" />
                      <input
                        type="text"
                        placeholder="Kullanıcı Adı"
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
                        required
                        className="w-full rounded-xl border border-border bg-black/30 py-3 pl-10 pr-4 text-sm text-foreground placeholder:text-muted-foreground/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                      />
                    </div>
                  )}

                  <div className="relative">
                    <Mail className="absolute left-3.5 top-3.5 h-4 w-4 text-muted-foreground" />
                    <input
                      type="email"
                      placeholder="E-Posta Adresi"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      required
                      className="w-full rounded-xl border border-border bg-black/30 py-3 pl-10 pr-4 text-sm text-foreground placeholder:text-muted-foreground/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                    />
                  </div>

                  <div className="relative">
                    <Lock className="absolute left-3.5 top-3.5 h-4 w-4 text-muted-foreground" />
                    <input
                      type="password"
                      placeholder="Şifre"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      required
                      className="w-full rounded-xl border border-border bg-black/30 py-3 pl-10 pr-4 text-sm text-foreground placeholder:text-muted-foreground/60 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
                    />
                  </div>

                  <button
                    type="submit"
                    disabled={loading}
                    className="group flex w-full items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-primary to-purple-600 py-3.5 text-sm font-bold text-white shadow-lg shadow-primary/25 transition-all duration-300 hover:brightness-110 active:scale-[0.98] disabled:opacity-50"
                  >
                    {loading ? (
                      <Loader2 className="h-5 w-5 animate-spin" />
                    ) : (
                      <>
                        <span>{emailMode === 'login' ? 'Giriş Yap' : 'Kayıt Ol - Doğrulama Gönder'}</span>
                        <ArrowRight className="h-4 w-4" />
                      </>
                    )}
                  </button>

                  <div className="pt-1 text-center text-xs text-muted-foreground">
                    {emailMode === 'login' ? (
                      <span>
                        Hesabınız yok mu?{' '}
                        <button type="button" onClick={() => { setEmailMode('register'); setErrorMsg('') }} className="font-bold text-primary hover:underline">
                          Kayıt Olun
                        </button>
                      </span>
                    ) : (
                      <span>
                        Zaten hesabınız var mı?{' '}
                        <button type="button" onClick={() => { setEmailMode('login'); setErrorMsg('') }} className="font-bold text-primary hover:underline">
                          Giriş Yapın
                        </button>
                      </span>
                    )}
                  </div>
                </form>
              )}

              {/* DISCORD */}
              {authTab === 'discord' && (
                <div>
                  <div className="mb-5 text-center">
                    <h2 className="text-lg font-bold text-foreground">Discord İle Bağlan</h2>
                    <p className="mt-1 text-xs text-muted-foreground">Discord hesabınızla güvenli giriş yapın</p>
                  </div>

                  <button
                    type="button"
                    onClick={handleDiscordLogin}
                    disabled={loading}
                    className="group relative flex w-full items-center justify-center gap-3 overflow-hidden rounded-xl bg-[#5865F2] px-6 py-4 text-base font-bold text-white shadow-lg shadow-[#5865F2]/25 transition-all duration-300 hover:bg-[#4752C4] hover:scale-[1.02] active:scale-[0.98] disabled:opacity-50"
                  >
                    <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/10 to-transparent opacity-0 transition-opacity duration-500 group-hover:opacity-100" />
                    {loading ? (
                      <Loader2 className="size-5 animate-spin" />
                    ) : (
                      <svg className="size-5" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M20.317 4.37a19.791 19.791 0 0 0-4.885-1.515.074.074 0 0 0-.079.037c-.21.375-.444.864-.608 1.25a18.27 18.27 0 0 0-5.487 0 12.64 12.64 0 0 0-.617-1.25.077.077 0 0 0-.079-.037A19.736 19.736 0 0 0 3.677 4.37a.07.07 0 0 0-.032.027C.533 9.046-.32 13.58.099 18.057a.082.082 0 0 0 .031.057 19.9 19.9 0 0 0 5.993 3.03.078.078 0 0 0 .084-.028c.462-.63.874-1.295 1.226-1.994a.076.076 0 0 0-.041-.106 13.107 13.107 0 0 1-1.872-.892.077.077 0 0 1-.008-.128 10.2 10.2 0 0 0 .372-.292.074.074 0 0 1 .077-.01c3.928 1.793 8.18 1.793 12.062 0a.074.074 0 0 1 .078.01c.12.098.246.198.373.292a.077.077 0 0 1-.006.127 12.299 12.299 0 0 1-1.873.892.077.077 0 0 0-.041.107c.36.698.772 1.362 1.225 1.993a.076.076 0 0 0 .084.028 19.839 19.839 0 0 0 6.002-3.03.077.077 0 0 0 .032-.054c.5-5.177-.838-9.674-3.549-13.66a.061.061 0 0 0-.031-.03zM8.02 15.33c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.956-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.956 2.418-2.157 2.418zm7.975 0c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.955-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.946 2.418-2.157 2.418z" />
                      </svg>
                    )}
                    <span className="relative z-10">{loading ? 'Yönlendiriliyor...' : 'Discord ile Giriş Yap'}</span>
                  </button>
                </div>
              )}

              {/* Beni Hatırla */}
              <button
                type="button"
                onClick={() => setRemember(!remember)}
                className="mt-4 flex w-full items-center gap-3 rounded-xl border border-border bg-card/30 px-4 py-3 text-xs text-muted-foreground transition-all duration-200 hover:border-primary/30 hover:text-foreground"
              >
                <div
                  className={`flex size-4 shrink-0 items-center justify-center rounded-md border transition-all duration-200 ${
                    remember ? 'border-primary bg-primary text-white' : 'border-border bg-transparent text-transparent'
                  }`}
                >
                  {remember && <Check className="size-3" strokeWidth={3} />}
                </div>
                Oturumumu Açık Tut (Beni Hatırla)
              </button>
            </>
          )}
        </div>

        {/* Footer */}
        <p className="mt-5 text-xs text-muted-foreground/60">
          Giriş yaparak <span className="text-primary/80">Kullanım Şartları</span>'nı kabul etmiş olursunuz.
        </p>
      </div>
    </div>
  )
}

import { launcherBridge } from './launcherBridge'
import {
  createUserWithEmailAndPassword,
  signInWithEmailAndPassword,
  sendEmailVerification,
  signOut,
  onAuthStateChanged,
  updateProfile,
  User as FirebaseUser,
  reload,
} from 'firebase/auth'
import { auth } from '@/lib/firebase'

export interface DiscordUser {
  id: string
  username: string
  discriminator: string
  avatar: string
  global_name: string | null
  avatar_decoration: string | null
  email?: string
  emailVerified?: boolean
  authProvider?: 'discord' | 'email'
}

class AuthService {
  private user: DiscordUser | null = null
  private listeners: ((user: DiscordUser | null) => void)[] = []
  private firebaseUser: FirebaseUser | null = null

  constructor() {
    // Discord bridge listener
    launcherBridge.onMessage((data: any) => {
      if (data && data.user && data.token) {
        this.setToken(data.token)
        this.setUser(data.user)
      }
    })

    // Firebase auth state listener
    onAuthStateChanged(auth, (fbUser) => {
      this.firebaseUser = fbUser

      if (fbUser && fbUser.emailVerified) {
        const mappedUser: DiscordUser = {
          id: fbUser.uid,
          username: fbUser.displayName || fbUser.email?.split('@')[0] || 'DLI Player',
          discriminator: '0000',
          avatar: fbUser.photoURL || '',
          global_name: fbUser.displayName,
          avatar_decoration: null,
          email: fbUser.email || '',
          emailVerified: true,
          authProvider: 'email',
        }
        this.user = mappedUser
        localStorage.setItem('dli_discord_user', JSON.stringify(mappedUser))
        this.listeners.forEach((fn) => fn(mappedUser))
      }
    })
  }

  getUser(): DiscordUser | null {
    if (this.user) return this.user
    const stored = localStorage.getItem('dli_discord_user')
    if (stored) {
      try {
        this.user = JSON.parse(stored)
        return this.user
      } catch {
        localStorage.removeItem('dli_discord_user')
      }
    }
    return null
  }

  isAuthenticated(): boolean {
    return this.getUser() !== null
  }

  onAuthChange(listener: (user: DiscordUser | null) => void): () => void {
    this.listeners.push(listener)
    return () => {
      this.listeners = this.listeners.filter((fn) => fn !== listener)
    }
  }

  setUser(user: DiscordUser | null) {
    this.user = user
    if (user) {
      localStorage.setItem('dli_discord_user', JSON.stringify(user))
      // Backend'e kullanıcı adını bildir (Minecraft'ta kullanılması için)
      const displayName = user.global_name || user.username
      launcherBridge.send('SET_USERNAME', { username: displayName })
    } else {
      localStorage.removeItem('dli_discord_user')
    }
    this.listeners.forEach((fn) => fn(user))
  }

  login() {
    launcherBridge.send('DISCORD_LOGIN')
  }

  // Firebase: E-posta ile giriş
  async loginWithEmail(email: string, pass: string): Promise<{ success: boolean; error?: string; needsVerification?: boolean }> {
    if (!email || !pass) {
      return { success: false, error: 'Lütfen tüm alanları doldurun.' }
    }

    try {
      const cred = await signInWithEmailAndPassword(auth, email, pass)
      
      if (!cred.user.emailVerified) {
        return { 
          success: false, 
          needsVerification: true,
          error: 'E-posta adresiniz henüz doğrulanmamış. Lütfen gelen kutunuzu kontrol edin.' 
        }
      }

      const mappedUser: DiscordUser = {
        id: cred.user.uid,
        username: cred.user.displayName || email.split('@')[0],
        discriminator: '0000',
        avatar: cred.user.photoURL || '',
        global_name: cred.user.displayName,
        avatar_decoration: null,
        email: cred.user.email || '',
        emailVerified: true,
        authProvider: 'email',
      }

      this.setUser(mappedUser)
      this.setToken(await cred.user.getIdToken())
      return { success: true }

    } catch (err: any) {
      return { success: false, error: this.translateFirebaseError(err.code) }
    }
  }

  // Firebase: Yeni kayıt + doğrulama maili gönder
  async registerWithEmail(email: string, pass: string, username: string): Promise<{ success: boolean; error?: string }> {
    if (!email || !pass || !username) {
      return { success: false, error: 'Lütfen tüm alanları doldurun.' }
    }

    try {
      const cred = await createUserWithEmailAndPassword(auth, email, pass)

      // Kullanıcı adını Firebase'e kaydet
      await updateProfile(cred.user, { displayName: username })

      // Doğrulama e-postası gönder
      await sendEmailVerification(cred.user, {
        url: 'https://dlistudios.web.app',
        handleCodeInApp: false,
      })

      // Oturumu kapat - doğrulama bekleniyor
      await signOut(auth)

      return { success: true }

    } catch (err: any) {
      return { success: false, error: this.translateFirebaseError(err.code) }
    }
  }

  // Doğrulama e-postasını yeniden gönder
  async resendVerificationEmail(email: string, pass: string): Promise<{ success: boolean; error?: string }> {
    try {
      const cred = await signInWithEmailAndPassword(auth, email, pass)
      await sendEmailVerification(cred.user)
      await signOut(auth)
      return { success: true }
    } catch (err: any) {
      return { success: false, error: this.translateFirebaseError(err.code) }
    }
  }

  // Firebase hata kodlarını Türkçe'ye çevir
  private translateFirebaseError(code: string): string {
    const errors: Record<string, string> = {
      'auth/email-already-in-use': 'Bu e-posta adresi zaten kayıtlı.',
      'auth/invalid-email': 'Geçersiz e-posta adresi.',
      'auth/weak-password': 'Şifre en az 6 karakter olmalıdır.',
      'auth/user-not-found': 'Bu e-posta ile kayıtlı hesap bulunamadı.',
      'auth/wrong-password': 'Şifre hatalı. Lütfen tekrar deneyin.',
      'auth/invalid-credential': 'E-posta veya şifre hatalı.',
      'auth/too-many-requests': 'Çok fazla deneme yaptınız. Lütfen biraz bekleyin.',
      'auth/network-request-failed': 'İnternet bağlantınızı kontrol edin.',
      'auth/user-disabled': 'Bu hesap devre dışı bırakılmış.',
      'auth/operation-not-allowed': 'E-posta girişi Firebase konsolundan aktifleştirilmemiş.',
      'auth/configuration-not-found': 'Firebase yapılandırması eksik. Konsol ayarlarını kontrol edin.',
    }
    return errors[code] || `Hata: ${code || 'bilinmeyen'} — Lütfen tekrar deneyin.`
  }

  async logout() {
    try {
      if (auth.currentUser) {
        await signOut(auth)
      }
    } catch {}

    this.user = null
    this.firebaseUser = null
    localStorage.removeItem('dli_discord_user')
    localStorage.removeItem('dli_discord_token')
    launcherBridge.send('CLEAR_SESSION')
    this.listeners.forEach((fn) => fn(null))
  }

  getToken(): string | null {
    return localStorage.getItem('dli_discord_token')
  }

  setToken(token: string) {
    localStorage.setItem('dli_discord_token', token)
  }
}

export const authService = new AuthService()

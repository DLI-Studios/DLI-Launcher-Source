import { launcherBridge } from './launcherBridge'

export interface DiscordUser {
  id: string
  username: string
  discriminator: string
  avatar: string
  global_name: string | null
  avatar_decoration: string | null
}

class AuthService {
  private user: DiscordUser | null = null
  private listeners: ((user: DiscordUser | null) => void)[] = []

  constructor() {
    launcherBridge.onMessage((data: any) => {
      if (data && data.user && data.token) {
        this.setToken(data.token)
        this.setUser(data.user)
      }
    })
  }

  getUser(): DiscordUser | null {
    if (this.user) return this.user
    const stored = localStorage.getItem('dli_discord_user')
    if (stored) {
      try { this.user = JSON.parse(stored); return this.user } catch { localStorage.removeItem('dli_discord_user') }
    }
    return null
  }

  isAuthenticated(): boolean { return this.getUser() !== null }

  onAuthChange(listener: (user: DiscordUser | null) => void): () => void {
    this.listeners.push(listener)
    return () => { this.listeners = this.listeners.filter((fn) => fn !== listener) }
  }

  setUser(user: DiscordUser | null) {
    this.user = user
    if (user) localStorage.setItem('dli_discord_user', JSON.stringify(user))
    else localStorage.removeItem('dli_discord_user')
    this.listeners.forEach((fn) => fn(user))
  }

  login() { launcherBridge.send('DISCORD_LOGIN') }

  logout() {
    this.setUser(null)
    localStorage.removeItem('dli_discord_token')
    launcherBridge.send('CLEAR_SESSION')
  }

  getToken(): string | null { return localStorage.getItem('dli_discord_token') }
  setToken(token: string) { localStorage.setItem('dli_discord_token', token) }
}

export const authService = new AuthService()

import { launcherBridge } from './launcherBridge'
import { authService, type DiscordUser } from './authService'

export interface PlayerInfo {
  username: string
  uuid: string
  coins: number
  gems: number
  isPremium: boolean
  avatarUrl?: string
}

const CUSTOM_AVATAR_KEY = 'dli_custom_avatar'
const MAX_AVATAR_SIZE = 128

class PlayerService {
  private cachedPlayer: PlayerInfo | null = null

  async getPlayer(): Promise<PlayerInfo> {
    if (this.cachedPlayer) return this.cachedPlayer

    const discordUser = authService.getUser()
    const username = discordUser?.global_name || discordUser?.username || 'DeliPlayer'

    const customAvatar = this.getCustomAvatarUrl()
    const avatarUrl = customAvatar || (discordUser?.avatar
      ? `https://cdn.discordapp.com/avatars/${discordUser.id}/${discordUser.avatar}.png?size=128`
      : undefined)

    return {
      username,
      uuid: discordUser?.id || '550e8400-e29b-41d4-a716-446655440000',
      coins: 2450,
      gems: 850,
      isPremium: true,
      avatarUrl,
    }
  }

  async getCurrencies(): Promise<{ coins: number; gems: number }> {
    const player = await this.getPlayer()
    return { coins: player.coins, gems: player.gems }
  }

  getCustomAvatarUrl(): string | null {
    return localStorage.getItem(CUSTOM_AVATAR_KEY)
  }

  setCustomAvatarUrl(url: string) {
    localStorage.setItem(CUSTOM_AVATAR_KEY, url)
    this.invalidateCache()
  }

  clearCustomAvatar() {
    localStorage.removeItem(CUSTOM_AVATAR_KEY)
    this.invalidateCache()
  }

  async uploadAvatar(file: File): Promise<string> {
    const user = authService.getUser()
    if (!user) throw new Error('No authenticated user')

    const dataUrl = await this.fileToDataUrl(file)
    const resized = await this.resizeAvatar(dataUrl)

    this.setCustomAvatarUrl(resized)
    return resized
  }

  private fileToDataUrl(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader()
      reader.onload = () => resolve(reader.result as string)
      reader.onerror = () => reject(new Error('FileReader error'))
      reader.readAsDataURL(file)
    })
  }

  private resizeAvatar(dataUrl: string): Promise<string> {
    return new Promise((resolve, reject) => {
      const img = new Image()
      img.onload = () => {
        const canvas = document.createElement('canvas')
        canvas.width = MAX_AVATAR_SIZE
        canvas.height = MAX_AVATAR_SIZE

        const ctx = canvas.getContext('2d')
        if (!ctx) {
          reject(new Error('Canvas context error'))
          return
        }

        const size = Math.min(img.width, img.height)
        const sx = (img.width - size) / 2
        const sy = (img.height - size) / 2

        ctx.imageSmoothingEnabled = true
        ctx.imageSmoothingQuality = 'high'
        ctx.drawImage(img, sx, sy, size, size, 0, 0, MAX_AVATAR_SIZE, MAX_AVATAR_SIZE)
        resolve(canvas.toDataURL('image/png', 0.9))
      }
      img.onerror = () => reject(new Error('Image load error'))
      img.src = dataUrl
    })
  }

  invalidateCache() {
    this.cachedPlayer = null
  }
}

export const playerService = new PlayerService()

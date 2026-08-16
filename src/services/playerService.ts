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

class PlayerService {
  private cachedPlayer: PlayerInfo | null = null

  async getPlayer(): Promise<PlayerInfo> {
    if (this.cachedPlayer) return this.cachedPlayer
    const discordUser = authService.getUser()
    const username = discordUser?.global_name || discordUser?.username || 'DeliPlayer'
    const avatarUrl = discordUser?.avatar
      ? `https://cdn.discordapp.com/avatars/${discordUser.id}/${discordUser.avatar}.png?size=128`
      : undefined
    return { username, uuid: discordUser?.id || '550e8400-e29b-41d4-a716-446655440000', coins: 2450, gems: 850, isPremium: true, avatarUrl }
  }

  async getCurrencies(): Promise<{ coins: number; gems: number }> {
    const player = await this.getPlayer()
    return { coins: player.coins, gems: player.gems }
  }

  invalidateCache() { this.cachedPlayer = null }
}

export const playerService = new PlayerService()

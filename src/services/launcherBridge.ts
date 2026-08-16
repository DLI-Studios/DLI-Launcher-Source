export interface BridgeResponse {
  success: boolean
  data?: unknown
  error?: string
  id?: string
}

type MessageHandler = (response: BridgeResponse) => void
type PushHandler = (data: unknown) => void

class LauncherBridge {
  private handlers: Map<string, MessageHandler> = new Map()
  private pushListeners: PushHandler[] = []
  private messageId = 0
  private isWebView2: boolean

  constructor() {
    this.isWebView2 = typeof window !== 'undefined' && 'chrome' in window && 'webview' in (window as any).chrome
    this.setupListener()
  }

  private setupListener() {
    if (this.isWebView2) {
      (window as any).chrome.webview.addEventListener('message', (event: MessageEvent) => {
        const msg = event.data
        if (msg.id) {
          const handler = this.handlers.get(msg.id)
          if (handler) {
            handler(msg as BridgeResponse)
            this.handlers.delete(msg.id)
          }
        }
        if (msg.type && !msg.id) {
          this.pushListeners.forEach((fn) => fn(msg))
        }
      })
    }
  }

  private generateId(): string {
    return `msg_${++this.messageId}_${Date.now()}`
  }

  send(type: string, payload?: unknown): Promise<BridgeResponse> {
    const id = this.generateId()
    return new Promise((resolve) => {
      if (this.isWebView2) {
        this.handlers.set(id, resolve)
        ;(window as any).chrome.webview.postMessage({ type, payload, id })
        setTimeout(() => {
          if (this.handlers.has(id)) {
            this.handlers.delete(id)
            resolve(this.getMockResponse(type))
          }
        }, 5000)
      } else {
        setTimeout(() => resolve(this.getMockResponse(type)), 300 + Math.random() * 500)
      }
    })
  }

  onMessage(handler: PushHandler): () => void {
    this.pushListeners.push(handler)
    return () => { this.pushListeners = this.pushListeners.filter((fn) => fn !== handler) }
  }

  private getMockResponse(type: string): BridgeResponse {
    switch (type) {
      case 'LAUNCH_GAME':
        return { success: true, data: { status: 'launching', downloadSizeMb: 350 } }
      case 'GET_PLAYER_INFO':
        return { success: true, data: { username: 'DeliPlayer', uuid: '550e8400-e29b-41d4-a716-446655440000', level: 42, xp: 12450, xpMax: 20000, coins: 2450, gems: 850, isPremium: true } }
      case 'GET_MINECRAFT_VERSIONS':
        return { success: true, data: { versions: ['1.21.1','1.21','1.20.6','1.20.5','1.20.4','1.20.3','1.20.2','1.20.1','1.20'], source: 'fallback-mock', count: 9 } }
      default:
        return { success: true, data: null }
    }
  }
}

export const launcherBridge = new LauncherBridge()

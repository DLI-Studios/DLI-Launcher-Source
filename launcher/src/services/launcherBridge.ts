/**
 * LauncherBridge - WebView2 <-> C# Communication Layer
 */

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

        // Id varsa - cevap (send() icin)
        if (msg.id) {
          const handler = this.handlers.get(msg.id)
          if (handler) {
            handler(msg as BridgeResponse)
            this.handlers.delete(msg.id)
          }
        }

        // Push mesajlari (C# tarafindan tek taraflı gonderilen)
        // type alani varsa ve id yoksa push mesajidir
        if (msg.type && !msg.id) {
          console.log('[DLI Bridge] Push mesaj:', msg.type, JSON.stringify(msg).substring(0, 200))
          this.pushListeners.forEach((fn) => fn(msg))
        }
      })
    }
  }

  private generateId(): string {
    return `msg_${++this.messageId}_${Date.now()}`
  }

  /** C# tarafina mesaj gonder */
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
        setTimeout(() => {
          resolve(this.getMockResponse(type))
        }, 300 + Math.random() * 500)
      }
    })
  }

  /** C# tarafindan gelen push mesajlarini dinle */
  onMessage(handler: PushHandler): () => void {
    this.pushListeners.push(handler)
    return () => {
      this.pushListeners = this.pushListeners.filter((fn) => fn !== handler)
    }
  }

  private getMockResponse(type: string): BridgeResponse {
    switch (type) {
      case 'INSTALL_LOADER':
        return {
          success: true,
          data: { status: 'started', loader: 'forge' },
        }
      case 'LAUNCH_GAME':
        return {
          success: true,
          data: { status: 'launching', downloadSizeMb: 350 },
        }
      case 'GET_VERSION':
        return {
          success: true,
          data: { version: '1.0.1', buildNumber: 2 },
        }
      case 'GET_PLAYER_INFO':
        return {
          success: true,
          data: {
            username: 'DeliPlayer',
            uuid: '550e8400-e29b-41d4-a716-446655440000',
            level: 42,
            xp: 12450,
            xpMax: 20000,
            coins: 2450,
            gems: 850,
            isPremium: true,
          },
        }
      case 'GET_MINECRAFT_VERSIONS':
        return {
          success: true,
          data: {
            versions: [
              '1.21.1','1.21','1.20.6','1.20.5','1.20.4','1.20.3','1.20.2','1.20.1','1.20',
              '1.19.4','1.19.3','1.19.2','1.19.1','1.19',
              '1.18.2','1.18.1','1.18',
              '1.17.1','1.17',
              '1.16.5','1.16.4','1.16.3','1.16.2','1.16.1','1.16',
              '1.15.2','1.15.1','1.15',
              '1.14.4','1.14.3','1.14.2','1.14.1','1.14',
              '1.13.2','1.13.1','1.13',
              '1.12.2','1.12.1','1.12',
              '1.11.2','1.11.1','1.11',
              '1.10.2','1.10.1','1.10',
              '1.9.4','1.9.2','1.9.1','1.9',
              '1.8.9','1.8.8','1.8.7','1.8.6','1.8.5','1.8.4','1.8.3','1.8.2','1.8.1','1.8',
              '1.7.10','1.7.9','1.7.8','1.7.7','1.7.6','1.7.5','1.7.4','1.7.2',
              '1.6.4','1.6.2','1.6.1',
              '1.5.2','1.5.1',
              '1.4.7','1.4.6','1.4.5','1.4.4','1.4.2',
              '1.3.2','1.3.1',
              '1.2.5','1.2.4','1.2.3','1.2.2','1.2.1',
              '1.1','1.0',
              '1.20.1-forge-47.2.0',
              '1.20.1-neoforge-47.1.106',
              '1.20.1-OptiFine-HD_U_H6',
              'fabric-loader-0.15.11-1.20.1',
            ],
            categories: {
              vanilla: [
                '1.21.1','1.21','1.20.6','1.20.5','1.20.4','1.20.3','1.20.2','1.20.1','1.20',
                '1.19.4','1.19.3','1.19.2','1.19.1','1.19',
                '1.18.2','1.18.1','1.18',
                '1.17.1','1.17',
                '1.16.5','1.16.4','1.16.3','1.16.2','1.16.1','1.16',
                '1.15.2','1.15.1','1.15',
                '1.14.4','1.14.3','1.14.2','1.14.1','1.14',
                '1.13.2','1.13.1','1.13',
                '1.12.2','1.12.1','1.12',
                '1.11.2','1.11.1','1.11',
                '1.10.2','1.10.1','1.10',
                '1.9.4','1.9.2','1.9.1','1.9',
                '1.8.9','1.8.8','1.8.7','1.8.6','1.8.5','1.8.4','1.8.3','1.8.2','1.8.1','1.8',
                '1.7.10','1.7.9','1.7.8','1.7.7','1.7.6','1.7.5','1.7.4','1.7.2',
                '1.6.4','1.6.2','1.6.1',
                '1.5.2','1.5.1',
                '1.4.7','1.4.6','1.4.5','1.4.4','1.4.2',
                '1.3.2','1.3.1',
                '1.2.5','1.2.4','1.2.3','1.2.2','1.2.1',
                '1.1','1.0',
              ],
              forge: ['1.20.1-forge-47.2.0'],
              neoforge: ['1.20.1-neoforge-47.1.106'],
              optifine: ['1.20.1-OptiFine-HD_U_H6'],
              fabric: ['fabric-loader-0.15.11-1.20.1'],
            },
            installed: ['1.20.1-forge-47.2.0', '1.20.1-neoforge-47.1.106'],
            source: 'fallback-mock',
            count: 69,
          },
        }
      default:
        return { success: true, data: null }
    }
  }
}

export const launcherBridge = new LauncherBridge()

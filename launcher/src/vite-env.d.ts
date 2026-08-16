/// <reference types="vite/client" />

interface Window {
  chrome?: {
    webview?: {
      postMessage(message: unknown): void
      addEventListener(type: string, listener: (event: MessageEvent) => void): void
      removeEventListener(type: string, listener: (event: MessageEvent) => void): void
    }
  }
}

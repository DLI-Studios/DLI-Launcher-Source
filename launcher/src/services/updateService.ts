/**
 * UpdateService - Guncelleme sistemi
 *
 * Oyun dosyalarinin guncellenmesini yonetir.
 * Progress, speed, status, pause, cancel eventleri destekler.
 */

import { launcherBridge } from './launcherBridge'

export type DownloadStatus = 'idle' | 'downloading' | 'paused' | 'completed' | 'error' | 'cancelled'

export interface DownloadState {
  status: DownloadStatus
  progress: number
  speed: number // MB/s
  downloaded: number // MB
  total: number // MB
  fileName: string
  error?: string
}

export interface DownloadEvents {
  onProgress?: (state: DownloadState) => void
  onComplete?: () => void
  onError?: (error: string) => void
  onCancel?: () => void
}

const TOTAL_MB = 1229 // 1.2 GB

class UpdateService {
  private state: DownloadState = {
    status: 'idle',
    progress: 0,
    speed: 0,
    downloaded: 0,
    total: TOTAL_MB,
    fileName: 'DLI Client Update',
  }

  private events: DownloadEvents = {}
  private intervalId: ReturnType<typeof setInterval> | null = null

  getState(): DownloadState {
    return { ...this.state }
  }

  setEvents(events: DownloadEvents) {
    this.events = events
  }

  async startDownload(): Promise<void> {
    if (this.state.status === 'downloading') return

    const response = await launcherBridge.send('START_DOWNLOAD')

    if (!response.success) {
      this.state.status = 'error'
      this.state.error = response.error || 'Download failed to start'
      this.events.onError?.(this.state.error)
      return
    }

    this.state.status = 'downloading'
    this.state.progress = 0
    this.state.downloaded = 0
    this.state.speed = 2.4 // Mock speed

    this.simulateProgress()
  }

  async pauseDownload(): Promise<void> {
    if (this.state.status !== 'downloading') return

    await launcherBridge.send('PAUSE_DOWNLOAD')

    this.stopSimulation()
    this.state.status = 'paused'
    this.state.speed = 0
    this.events.onProgress?.(this.getState())
  }

  async resumeDownload(): Promise<void> {
    if (this.state.status !== 'paused') return

    await launcherBridge.send('RESUME_DOWNLOAD')

    this.state.status = 'downloading'
    this.state.speed = 2.4
    this.simulateProgress()
  }

  async cancelDownload(): Promise<void> {
    await launcherBridge.send('CANCEL_DOWNLOAD')

    this.stopSimulation()
    this.state.status = 'cancelled'
    this.state.progress = 0
    this.state.downloaded = 0
    this.state.speed = 0
    this.events.onCancel?.()
    this.events.onProgress?.(this.getState())
  }

  private simulateProgress() {
    this.stopSimulation()

    this.intervalId = setInterval(() => {
      if (this.state.status !== 'downloading') {
        this.stopSimulation()
        return
      }

      // Simulate realistic download progress
      const increment = 0.05 + Math.random() * 0.1
      this.state.progress = Math.min(100, this.state.progress + increment)
      this.state.downloaded = Math.round((this.state.progress / 100) * TOTAL_MB)

      // Vary speed slightly
      this.state.speed = 2.0 + Math.random() * 0.8

      this.events.onProgress?.(this.getState())

      if (this.state.progress >= 100) {
        this.stopSimulation()
        this.state.status = 'completed'
        this.state.speed = 0
        this.events.onComplete?.()
        this.events.onProgress?.(this.getState())
      }
    }, 400)
  }

  private stopSimulation() {
    if (this.intervalId) {
      clearInterval(this.intervalId)
      this.intervalId = null
    }
  }

  destroy() {
    this.stopSimulation()
  }
}

export const updateService = new UpdateService()

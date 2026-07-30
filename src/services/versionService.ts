/**
 * VersionService - Tum Minecraft release surumlerini ceker
 */
import { launcherBridge } from './launcherBridge'

class VersionService {
  private versions: string[] = []
  private loaded = false

  async loadVersions(): Promise<string[]> {
    if (this.loaded && this.versions.length > 0) return this.versions

    try {
      const response = await launcherBridge.send('GET_MINECRAFT_VERSIONS')
      console.log('[VersionService] Bridge response:', JSON.stringify(response))

      if (response.success && response.data) {
        const d = response.data as Record<string, unknown>
        console.log('[VersionService] data keys:', Object.keys(d))
        console.log('[VersionService] data.versions type:', typeof d.versions)

        if (Array.isArray(d.versions)) {
          this.versions = d.versions as string[]
          this.loaded = true
          console.log('[VersionService] Loaded', this.versions.length, 'versions from', d.source)
        }
      }

      if (this.versions.length === 0) {
        console.warn('[VersionService] No versions from bridge, using hard-coded fallback')
        this.versions = this.getHardcoded()
        this.loaded = true
      }
    } catch (err) {
      console.error('[VersionService] Bridge error:', err)
      this.versions = this.getHardcoded()
      this.loaded = true
    }

    return this.versions
  }

  private getHardcoded(): string[] {
    return [
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
    ]
  }
}

export const versionService = new VersionService()

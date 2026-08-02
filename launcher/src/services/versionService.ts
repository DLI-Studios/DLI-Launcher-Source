/**
 * VersionService - Minecraft surumlerini kategorileriyle birlikte ceker
 * Kategoriler: vanilla, forge, neoforge, optifine, fabric, liteloader, quilt, other
 */
import { launcherBridge } from './launcherBridge'

export interface VersionEntry {
  id: string
  category: string
  releaseTime: string
  installed: boolean
}

const CATEGORY_ORDER = ['vanilla', 'forge', 'neoforge', 'optifine', 'fabric', 'liteloader', 'quilt', 'other']

class VersionService {
  private versions: string[] = []
  private entries: VersionEntry[] = []
  private categories: Record<string, string[]> = {}
  private loaded = false

  async loadVersions(): Promise<string[]> {
    if (this.loaded && this.versions.length > 0) return this.versions

    try {
      const response = await launcherBridge.send('GET_MINECRAFT_VERSIONS')
      console.log('[VersionService] Bridge response:', JSON.stringify(response))

      if (response.success && response.data) {
        const d = response.data as Record<string, unknown>
        if (Array.isArray(d.versions)) {
          this.versions = d.versions as string[]
          this.categories = (d.categories as Record<string, string[]>) || {}
          const items = d.items as Array<Partial<VersionEntry>> | undefined
          if (Array.isArray(items) && items.length > 0) {
            this.entries = items.map((it) => ({
              id: String(it.id || ''),
              category: it.category || this.findCategory(String(it.id || '')),
              releaseTime: it.releaseTime || '',
              installed: !!it.installed,
            }))
          } else {
            this.entries = []
            const installedSet = new Set((d.installed as string[]) || [])
            for (const id of this.versions) {
              this.entries.push({
                id,
                category: this.findCategory(id),
                releaseTime: '',
                installed: installedSet.has(id),
              })
            }
          }
          this.loaded = true
          console.log('[VersionService] Loaded', this.versions.length, 'versions from', d.source, 'categories:', Object.keys(this.categories))
        }
      }

      if (this.versions.length === 0) {
        console.warn('[VersionService] No versions from bridge, using hard-coded fallback')
        this.entries = this.getHardcoded().map((id) => ({ id, category: 'vanilla', releaseTime: '', installed: false }))
        this.versions = this.entries.map((e) => e.id)
        this.loaded = true
      }
    } catch (err) {
      console.error('[VersionService] Bridge error:', err)
      this.entries = this.getHardcoded().map((id) => ({ id, category: 'vanilla', releaseTime: '', installed: false }))
      this.versions = this.entries.map((e) => e.id)
      this.loaded = true
    }

    return this.versions
  }

  getEntries(): VersionEntry[] {
    return this.entries
  }

  getCategories(): Record<string, string[]> {
    return this.categories
  }

  getCategoryOrder(): string[] {
    return CATEGORY_ORDER
  }

  getInstalledVersions(): string[] {
    return this.entries.filter((e) => e.installed).map((e) => e.id)
  }

  reset(): void {
    this.versions = []
    this.entries = []
    this.categories = {}
    this.loaded = false
  }

  private findCategory(id: string): string {
    for (const [cat, list] of Object.entries(this.categories)) {
      if (list.includes(id)) return cat
    }
    const lower = id.toLowerCase()
    if (lower.includes('neoforge')) return 'neoforge'
    if (lower.includes('forge')) return 'forge'
    if (lower.includes('optifine')) return 'optifine'
    if (lower.includes('fabric')) return 'fabric'
    if (lower.includes('liteloader')) return 'liteloader'
    if (lower.includes('quilt')) return 'quilt'
    return 'other'
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

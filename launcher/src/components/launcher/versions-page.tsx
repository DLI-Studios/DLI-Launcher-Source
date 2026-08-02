import { useEffect, useMemo, useState } from 'react'
import { Search, Download, Check, Boxes, Layers, Loader2, XCircle } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useI18n, type TKey } from '@/lib/i18n'
import { versionService, type VersionEntry } from '@/services/versionService'
import { launcherBridge } from '@/services/launcherBridge'

const CATEGORY_ORDER = ['vanilla', 'forge', 'neoforge', 'optifine', 'fabric', 'liteloader', 'quilt', 'other']
const LOADERS = ['forge', 'neoforge', 'fabric'] as const

const LOADER_LABEL_KEY: Record<string, TKey> = {
  forge: 'versions.installForge',
  neoforge: 'versions.installNeoForge',
  fabric: 'versions.installFabric',
}

const CATEGORY_LOADER: Record<string, string> = {
  forge: 'forge',
  neoforge: 'neoforge',
  fabric: 'fabric',
}

/** Loader-surumlerinden vanilla tabanini cikarir (1.20.1-forge-47.2.0 -> 1.20.1, fabric-loader-0.15.11-1.20.1 -> 1.20.1) */
function getBaseVersion(id: string): string {
  const lower = id.toLowerCase()
  if (lower.startsWith('fabric-loader') || lower.startsWith('quilt-loader') || lower.startsWith('liteloader')) {
    const parts = id.split('-')
    return parts[parts.length - 1] || id
  }
  return id.split('-')[0] || id
}

interface InstallState {
  status: string
  versionName?: string
  error?: string
}

function getVersionStatus(versionId: string, selectedVersion: string, installed: boolean): 'installed' | 'not-installed' {
  if (versionId === selectedVersion) return 'installed'
  return installed ? 'installed' : 'not-installed'
}

interface VersionsPageProps {
  onSelect: (version: string) => void
  selectedVersion: string
}

export function VersionsPage({ onSelect, selectedVersion }: VersionsPageProps) {
  const { t } = useI18n()
  const [search, setSearch] = useState('')
  const [activeCategory, setActiveCategory] = useState<string>('vanilla')
  const [installedOnly, setInstalledOnly] = useState(false)
  const [entries, setEntries] = useState<VersionEntry[]>([])
  const [installState, setInstallState] = useState<Record<string, InstallState>>({})

  const reloadVersions = () => {
    versionService.reset()
    versionService.loadVersions().then(() => {
      setEntries(versionService.getEntries())
    })
  }

  useEffect(() => {
    versionService.loadVersions().then(() => {
      setEntries(versionService.getEntries())
    })

    const unsubscribe = launcherBridge.onMessage((msg) => {
      const m = msg as { type?: string; data?: Record<string, unknown> }
      if (m.type === 'LOADER_INSTALL_STATUS') {
        const d = m.data || {}
        const mc = String(d.minecraftVersion || '')
        const loader = String(d.loader || '')
        if (mc && loader) {
          const key = `${mc}|${loader}`
          setInstallState((s) => ({
            ...s,
            [key]: {
              status: String(d.status || ''),
              versionName: d.versionName ? String(d.versionName) : undefined,
              error: d.error ? String(d.error) : undefined,
            },
          }))
        }
      }
      if (m.type === 'VERSIONS_REFRESHED') {
        reloadVersions()
      }
    })

    return () => unsubscribe()
  }, [])

  const installLoader = async (mcVersion: string, loader: string) => {
    const key = `${mcVersion}|${loader}`
    setInstallState((s) => ({ ...s, [key]: { status: 'starting' } }))
    try {
      await launcherBridge.send('INSTALL_LOADER', { loader, minecraftVersion: mcVersion })
    } catch (err) {
      setInstallState((s) => ({ ...s, [key]: { status: 'error', error: String(err) } }))
    }
  }

  const { filtered, grouped, categoryCounts, totalInstalled } = useMemo(() => {
    const e = installedOnly ? entries.filter((v) => v.installed) : entries
    const filteredEntries = search
      ? e.filter((v) => v.id.includes(search) || v.releaseTime.includes(search))
      : e

    const counts: Record<string, number> = {}
    for (const v of e) counts[v.category] = (counts[v.category] || 0) + 1

    const groupedEntries = filteredEntries.reduce<Record<string, VersionEntry[]>>((acc, v) => {
      ;(acc[v.category] = acc[v.category] || []).push(v)
      return acc
    }, {})

    return {
      filtered: filteredEntries,
      grouped: groupedEntries,
      categoryCounts: counts,
      totalInstalled: entries.filter((v) => v.installed).length,
    }
  }, [entries, search, installedOnly])

  const categoryLabel = (cat: string) => {
    const key = `versions.category.${cat}` as TKey
    const label = t(key)
    return label === key ? cat : label
  }

  return (
    <div className="flex flex-col h-full p-6 gap-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-black tracking-widest text-foreground uppercase">{t('versions.title')}</h1>
          <p className="text-sm text-muted-foreground">{t('versions.subtitle')}</p>
        </div>
        <span className="text-sm text-muted-foreground">{t('versions.count', { n: entries.length })}</span>
      </div>

      {/* Search + filter */}
      <div className="flex items-center gap-2">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
          <input
            type="text"
            placeholder={t('versions.search')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full rounded-xl border border-border bg-secondary pl-10 pr-4 py-3 text-sm text-foreground placeholder:text-muted-foreground focus:border-primary focus:outline-none"
          />
        </div>
        <button
          type="button"
          onClick={() => setInstalledOnly((v) => !v)}
          className={cn(
            'flex items-center gap-1.5 rounded-xl border px-3 py-3 text-xs font-semibold transition-colors',
            installedOnly
              ? 'border-success bg-success/15 text-success'
              : 'border-border bg-secondary text-muted-foreground hover:border-primary/40 hover:text-foreground',
          )}
        >
          <Download className="size-3.5" />
          {t('versions.installedOnly')}
          {totalInstalled > 0 && <span className="tabular-nums opacity-70">({totalInstalled})</span>}
        </button>
      </div>

      {/* Category tabs */}
      <div className="flex flex-wrap items-center gap-2">
        {CATEGORY_ORDER.filter((c) => categoryCounts[c]).map((cat) => (
          <button
            key={cat}
            type="button"
            onClick={() => setActiveCategory(cat)}
            className={cn(
              'flex items-center gap-1.5 rounded-lg border px-3 py-1.5 text-xs font-bold uppercase tracking-wide transition-colors',
              activeCategory === cat
                ? 'border-primary bg-primary/15 text-primary'
                : 'border-border bg-card/30 text-muted-foreground hover:border-primary/30 hover:text-foreground',
            )}
          >
            <Boxes className="size-3.5" />
            {categoryLabel(cat)}
            <span className="tabular-nums opacity-70">({categoryCounts[cat]})</span>
          </button>
        ))}
      </div>

      {/* Selected */}
      {selectedVersion && (
        <div className="flex items-center gap-2 text-sm">
          <span className="text-muted-foreground">{t('versions.selected')}</span>
          <span className="font-bold text-primary">{selectedVersion}</span>
        </div>
      )}

      {/* Legend */}
      <div className="flex items-center gap-4 text-xs text-muted-foreground">
        <span className="flex items-center gap-1.5"><span className="flex size-5 items-center justify-center rounded-md bg-success/20"><Check className="size-3 text-success" /></span> {t('versions.installed')}</span>
        <span className="flex items-center gap-1.5"><span className="flex size-5 items-center justify-center rounded-md bg-primary/20"><Download className="size-3 text-primary" /></span> {t('versions.notInstalled')}</span>
        <span className="flex items-center gap-1.5"><span className="flex size-5 items-center justify-center rounded-md bg-secondary/60"><Layers className="size-3 text-muted-foreground" /></span> {t('versions.loaderHint')}</span>
      </div>

      {/* Version list */}
      <div className="flex-1 overflow-y-auto rounded-xl border border-border bg-card/30">
        {([activeCategory])
          .filter((cat) => grouped[cat])
          .map((cat) => (
            <div key={cat}>
              <div className="sticky top-0 z-10 flex items-center gap-2 border-b border-border bg-card/90 px-5 py-2.5 backdrop-blur">
                <span className="text-[10px] font-black tracking-widest text-primary uppercase">{categoryLabel(cat)}</span>
                <span className="rounded-md bg-secondary px-1.5 py-0.5 text-[10px] font-bold text-muted-foreground tabular-nums">{grouped[cat].length}</span>
              </div>
              <div className="grid grid-cols-1 divide-y divide-border">
                {grouped[cat].map((v) => {
                  const status = getVersionStatus(v.id, selectedVersion, v.installed)
                  const loaderButtons = cat === 'vanilla' ? [...LOADERS] : CATEGORY_LOADER[cat] && !v.installed ? [CATEGORY_LOADER[cat]] : []
                  const targetMc = cat === 'vanilla' ? v.id : getBaseVersion(v.id)
                  return (
                    <div key={v.id} className={cn('flex items-center transition-all duration-150 hover:bg-secondary/40', v.id === selectedVersion && 'bg-primary/10')}>
                      <button
                        type="button"
                        onClick={() => onSelect(v.id)}
                        className={cn(
                          'flex flex-1 items-center justify-between px-5 py-3 text-left',
                          v.id === selectedVersion && 'border-l-2 border-l-primary',
                        )}
                      >
                        <div className="flex items-center gap-3">
                          <span className={cn(
                            'text-base font-bold',
                            v.id === selectedVersion ? 'text-primary' : 'text-foreground',
                          )}>
                            {v.id}
                          </span>
                          {status === 'installed' ? (
                            <span className="flex items-center gap-1 rounded-md bg-success/20 px-2 py-0.5 text-[10px] font-bold text-success">
                              <Check className="size-3" strokeWidth={3} />
                              {t('versions.installed')}
                            </span>
                          ) : (
                            <span className="flex items-center gap-1 rounded-md bg-primary/10 px-2 py-0.5 text-[10px] font-semibold text-primary/70">
                              <Download className="size-3" />
                              {t('versions.notInstalled')}
                            </span>
                          )}
                        </div>
                        {v.releaseTime ? (
                          <span className="text-xs text-muted-foreground">{v.releaseTime}</span>
                        ) : v.installed ? (
                          <span className="text-xs text-muted-foreground">{t('versions.installed')}</span>
                        ) : null}
                      </button>
                      {loaderButtons.length > 0 && (
                        <div className="flex shrink-0 items-center gap-1.5 px-4">
                          {loaderButtons.map((loader) => {
                            const key = `${targetMc}|${loader}`
                            const st = installState[key]
                            const busy = st && ['starting', 'fetching', 'installing'].includes(st.status)
                            const done = st?.status === 'done'
                            const error = st?.status === 'error'
                            const labelKey = LOADER_LABEL_KEY[loader]
                            return (
                              <button
                                key={loader}
                                type="button"
                                disabled={!!busy}
                                onClick={() => installLoader(targetMc, loader)}
                                className={cn(
                                  'flex items-center gap-1 rounded-md border px-2 py-1 text-[10px] font-bold transition-colors',
                                  done
                                    ? 'border-success bg-success/15 text-success'
                                    : error
                                      ? 'border-destructive bg-destructive/15 text-destructive'
                                      : busy
                                        ? 'border-border bg-secondary text-muted-foreground cursor-wait'
                                        : 'border-border bg-secondary text-muted-foreground hover:border-primary/40 hover:text-primary',
                                )}
                              >
                                {busy ? (
                                  <Loader2 className="size-3 animate-spin" />
                                ) : done ? (
                                  <Check className="size-3" strokeWidth={3} />
                                ) : error ? (
                                  <XCircle className="size-3" />
                                ) : (
                                  <Download className="size-3" />
                                )}
                                {t(labelKey)}
                              </button>
                            )
                          })}
                        </div>
                      )}
                    </div>
                  )
                })}
              </div>
            </div>
          ))}
        {filtered.length === 0 && (
          <div className="px-5 py-10 text-center text-sm text-muted-foreground">
            {t('versions.noMatch', { q: search })}
          </div>
        )}
      </div>
    </div>
  )
}

import { useEffect, useState } from 'react'
import { CheckCircle2, Settings2, X, Monitor, Eye, Cloud, Sparkles, RotateCcw } from 'lucide-react'
import { cn } from '@/lib/utils'
import { launcherBridge } from '@/services/launcherBridge'

const tags = ['Survival', 'Economy', 'Quests', 'PvP']
const ALL_VERSIONS = [
  { id: '26.2', date: '2026-06-16' }, { id: '26.1.2', date: '2026-04-09' }, { id: '26.1.1', date: '2026-04-01' }, { id: '26.1', date: '2026-03-24' },
  { id: '1.21.11', date: '2025-12-09' }, { id: '1.21.10', date: '2025-10-07' }, { id: '1.21.9', date: '2025-09-30' }, { id: '1.21.8', date: '2025-07-17' },
  { id: '1.21.7', date: '2025-06-30' }, { id: '1.21.6', date: '2025-06-17' }, { id: '1.21.5', date: '2025-03-25' }, { id: '1.21.4', date: '2024-12-03' },
  { id: '1.21.3', date: '2024-10-23' }, { id: '1.21.2', date: '2024-10-22' }, { id: '1.21.1', date: '2024-08-08' }, { id: '1.21', date: '2024-06-13' },
  { id: '1.20.6', date: '2024-04-29' }, { id: '1.20.5', date: '2024-04-23' }, { id: '1.20.4', date: '2023-12-07' }, { id: '1.20.3', date: '2023-12-05' },
  { id: '1.20.2', date: '2023-09-21' }, { id: '1.20.1', date: '2023-06-12' }, { id: '1.20', date: '2023-06-07' },
  { id: '1.19.4', date: '2023-03-14' }, { id: '1.19.3', date: '2022-12-07' }, { id: '1.19.2', date: '2022-08-05' }, { id: '1.19.1', date: '2022-07-27' }, { id: '1.19', date: '2022-06-07' },
  { id: '1.18.2', date: '2022-02-28' }, { id: '1.18.1', date: '2021-12-10' }, { id: '1.18', date: '2021-11-30' },
  { id: '1.17.1', date: '2021-07-06' }, { id: '1.17', date: '2021-06-08' },
  { id: '1.16.5', date: '2021-01-15' }, { id: '1.16.4', date: '2020-11-02' }, { id: '1.16.3', date: '2020-09-10' }, { id: '1.16.2', date: '2020-08-11' }, { id: '1.16.1', date: '2020-06-24' }, { id: '1.16', date: '2020-06-23' },
  { id: '1.15.2', date: '2020-01-21' }, { id: '1.15.1', date: '2019-12-17' }, { id: '1.15', date: '2019-12-10' },
  { id: '1.14.4', date: '2019-07-19' }, { id: '1.14.3', date: '2019-06-24' }, { id: '1.14.2', date: '2019-05-27' }, { id: '1.14.1', date: '2019-05-13' }, { id: '1.14', date: '2019-04-23' },
  { id: '1.13.2', date: '2018-10-22' }, { id: '1.13.1', date: '2018-08-22' }, { id: '1.13', date: '2018-07-18' },
  { id: '1.12.2', date: '2017-09-18' }, { id: '1.12.1', date: '2017-08-03' }, { id: '1.12', date: '2017-06-07' },
  { id: '1.11.2', date: '2016-12-21' }, { id: '1.11.1', date: '2016-12-20' }, { id: '1.11', date: '2016-11-14' },
  { id: '1.10.2', date: '2016-06-23' }, { id: '1.10.1', date: '2016-06-22' }, { id: '1.10', date: '2016-06-08' },
  { id: '1.9.4', date: '2016-05-10' }, { id: '1.9.2', date: '2016-03-30' }, { id: '1.9.1', date: '2016-03-09' }, { id: '1.9', date: '2016-02-29' },
  { id: '1.8.9', date: '2015-12-09' }, { id: '1.8.8', date: '2015-07-28' }, { id: '1.8.7', date: '2015-06-05' }, { id: '1.8.6', date: '2015-05-25' },
  { id: '1.8.5', date: '2015-05-22' }, { id: '1.8.4', date: '2015-04-17' }, { id: '1.8.3', date: '2015-02-20' }, { id: '1.8.2', date: '2015-02-19' },
  { id: '1.8.1', date: '2014-11-24' }, { id: '1.8', date: '2014-09-02' },
  { id: '1.7.10', date: '2014-06-26' }, { id: '1.7.9', date: '2014-04-14' }, { id: '1.7.8', date: '2014-04-11' }, { id: '1.7.7', date: '2014-04-09' },
  { id: '1.7.6', date: '2014-04-03' }, { id: '1.7.5', date: '2014-02-26' }, { id: '1.7.4', date: '2013-12-10' },
]

interface GameSettings { renderDistance: number; graphics: 'fast' | 'fancy' | 'fabulous'; fpsLimit: number; vsync: boolean; viewBobbing: boolean; clouds: boolean; particles: 'all' | 'decreased' | 'minimal'; antialiasing: boolean; mipmapLevels: number; entityShadows: boolean }
const DEFAULT_SETTINGS: GameSettings = { renderDistance: 12, graphics: 'fancy', fpsLimit: 0, vsync: true, viewBobbing: true, clouds: true, particles: 'all', antialiasing: true, mipmapLevels: 4, entityShadows: true }
function loadSettings(): GameSettings { try { const s = localStorage.getItem('dli_game_settings'); if (s) return { ...DEFAULT_SETTINGS, ...JSON.parse(s) } } catch {} return DEFAULT_SETTINGS }
function saveSettings(s: GameSettings) { localStorage.setItem('dli_game_settings', JSON.stringify(s)) }

type LaunchStatus = 'idle' | 'launching' | 'launched' | 'error'
interface PlaySectionProps { onLaunch: (version: string, sizeMb: number) => void; selectedVersion?: string }

export function PlaySection({ onLaunch, selectedVersion }: PlaySectionProps) {
  const [version, setVersion] = useState(selectedVersion || ALL_VERSIONS[0].id)
  const [launchStatus, setLaunchStatus] = useState<LaunchStatus>('idle')
  const [showSettings, setShowSettings] = useState(false)
  const [settings, setSettings] = useState<GameSettings>(loadSettings)

  useEffect(() => { if (selectedVersion) setVersion(selectedVersion) }, [selectedVersion])
  useEffect(() => { saveSettings(settings) }, [settings])

  const handlePlay = async () => {
    if (launchStatus !== 'idle') return
    setLaunchStatus('launching')
    try {
      const response = await launcherBridge.send('LAUNCH_GAME', { version, settings })
      if (response.success) { setLaunchStatus('launched'); const data = response.data as { downloadSizeMb?: number } | undefined; onLaunch(version, data?.downloadSizeMb ?? 350) }
      else { setLaunchStatus('error'); setTimeout(() => setLaunchStatus('idle'), 2000) }
    } catch { setLaunchStatus('error'); setTimeout(() => setLaunchStatus('idle'), 2000) }
  }

  useEffect(() => {
    const unsub = launcherBridge.onMessage((msg) => {
      const data = msg as Record<string, unknown>
      if (data.type === 'GAME_STARTED') setLaunchStatus('launched')
      if (data.type === 'GAME_STOPPED' || data.type === 'DOWNLOAD_CANCELLED') setLaunchStatus('idle')
    })
    return unsub
  }, [])

  const btnLabel = { idle: 'PLAY', launching: 'Launching...', launched: 'Running', error: 'Error' }[launchStatus]
  const getButtonBg = () => { switch (launchStatus) { case 'idle': return 'linear-gradient(135deg, #7c3aed 0%, #a855f7 50%, #7c3aed 100%)'; case 'launching': return 'linear-gradient(135deg, #a855f7 0%, #c084fc 100%)'; case 'launched': return '#27272a'; case 'error': return '#dc2626'; default: return '#7c3aed' } }
  const resetSettings = () => setSettings(DEFAULT_SETTINGS)
  const updateSetting = <K extends keyof GameSettings>(key: K, value: GameSettings[K]) => setSettings(prev => ({ ...prev, [key]: value }))

  return (
    <section className="relative flex flex-1 flex-col overflow-hidden">
      <img src="/images/hero-bg.png" alt="" className="absolute inset-0 size-full object-cover" />
      <div className="absolute inset-0 bg-gradient-to-t from-background via-background/60 to-background/40" />
      <div className="relative z-10 flex flex-1 flex-col gap-4 px-6 py-5">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-bold tracking-widest text-foreground uppercase">Play DLI</h2>
          <span className="text-sm font-semibold text-primary">{version}</span>
        </div>
        <div className="group flex items-center gap-5 overflow-hidden rounded-xl border border-border bg-card/70 p-4 transition-all duration-300 hover:border-primary/40">
          <img src="/images/game-icon.png" alt="DLI Survival" className="size-24 shrink-0 rounded-xl border border-primary/30 object-cover shadow-[0_0_20px] shadow-primary/20 transition-transform duration-300 group-hover:scale-105" />
          <div className="flex min-w-0 flex-col gap-1.5">
            <div className="flex flex-wrap items-center gap-3"><h3 className="text-xl font-bold text-foreground">DLI SURVIVAL</h3><span className="rounded-md bg-primary/20 px-2.5 py-0.5 text-[10px] font-bold tracking-wider text-accent uppercase">Recommended</span></div>
            <p className="text-xs text-muted-foreground">v1.0.0 <span className="mx-1.5">&bull;</span> Minecraft {version}</p>
            <p className="text-sm leading-relaxed text-muted-foreground">Custom plugins, quests, economy system and more!</p>
            <div className="mt-1 flex flex-wrap gap-2">{tags.map((tag) => (<span key={tag} className="rounded-md border border-border bg-secondary px-2.5 py-1 text-[10px] font-semibold tracking-wider text-foreground/80 uppercase transition-colors hover:border-primary/50 hover:text-primary">{tag}</span>))}</div>
          </div>
        </div>
        <div className="grid grid-cols-[1fr_auto_1fr] items-stretch gap-4">
          <div className="flex flex-col justify-center gap-0.5 rounded-xl border border-border bg-card/70 px-5">
            <span className="flex items-center gap-2 text-sm font-bold text-foreground"><CheckCircle2 className="size-4 text-success" /> READY TO PLAY</span>
            <span className="text-xs text-muted-foreground">All files are up to date</span>
          </div>
          <button type="button" onClick={handlePlay} disabled={launchStatus !== 'idle'} style={{ background: getButtonBg() }}
            className={cn('flex w-72 items-center justify-center gap-3 rounded-2xl px-10 py-4 text-xl font-black text-white tracking-[0.15em] uppercase transition-all duration-200 active:scale-[0.98] shadow-lg',
              launchStatus === 'idle' && 'animate-play-pulse hover:brightness-110 hover:shadow-[0_0_30px_rgba(168,85,247,0.5)]',
              launchStatus === 'launching' && 'animate-pulse cursor-wait', launchStatus === 'launched' && 'text-zinc-400 animate-none cursor-not-allowed', launchStatus === 'error' && 'animate-none')}>
            {launchStatus === 'launched' ? <svg className="size-5 fill-current" viewBox="0 0 24 24"><path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" /></svg> : <svg className="size-5 fill-current" viewBox="0 0 24 24"><path d="M8 5v14l11-7z" /></svg>}
            {btnLabel}
          </button>
          <button type="button" onClick={() => setShowSettings(true)} className="group flex flex-col justify-center gap-0.5 rounded-xl border border-border bg-card/70 px-5 text-left transition-colors hover:border-primary/40">
            <span className="flex items-center gap-2 text-sm font-bold text-foreground"><Settings2 className="size-4 text-primary" /> GAME SETTINGS</span>
            <span className="text-xs text-muted-foreground">Performance &amp; Graphics</span>
          </button>
        </div>
      </div>

      {showSettings && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-md animate-in fade-in duration-200" onClick={() => setShowSettings(false)}>
          <div className="glass w-[92vw] max-w-5xl max-h-[82vh] rounded-2xl border border-border shadow-2xl flex flex-col overflow-hidden animate-in zoom-in-95 fade-in duration-200" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between px-6 py-4 border-b border-border">
              <div className="flex items-center gap-3"><div className="flex size-8 items-center justify-center rounded-lg bg-primary/15"><Settings2 className="size-4 text-primary" /></div><div><h3 className="text-sm font-bold text-foreground">Game Settings</h3><p className="text-[10px] text-muted-foreground">Minecraft graphics and performance settings</p></div></div>
              <div className="flex items-center gap-2">
                <button type="button" onClick={resetSettings} className="flex items-center gap-1.5 rounded-lg border border-border bg-card/50 px-3 py-1.5 text-[11px] font-semibold text-muted-foreground transition-colors hover:border-primary/40 hover:text-foreground"><RotateCcw className="size-3" />Reset to Default</button>
                <button type="button" onClick={() => setShowSettings(false)} className="flex size-7 items-center justify-center rounded-lg border border-border bg-card/50 text-muted-foreground transition-colors hover:border-destructive/40 hover:text-destructive"><X className="size-3.5" /></button>
              </div>
            </div>
            <div className="grid grid-cols-3 gap-0 flex-1 overflow-hidden">
              <div className="border-r border-border p-4 space-y-3">
                <h4 className="text-[10px] font-bold tracking-widest text-muted-foreground uppercase mb-3">Graphics</h4>
                <div className="space-y-2"><div className="flex items-center justify-between"><span className="flex items-center gap-1.5 text-xs font-semibold text-foreground"><Monitor className="size-3.5 text-primary" /> Render Distance</span><span className="text-xs font-bold text-primary tabular-nums">{settings.renderDistance} chunks</span></div><input type="range" min={2} max={32} step={2} value={settings.renderDistance} onChange={(e) => updateSetting('renderDistance', Number(e.target.value))} className="w-full accent-primary h-1.5" /><div className="flex justify-between text-[9px] text-muted-foreground"><span>2 (Near)</span><span>32 (Far)</span></div></div>
                <div className="space-y-2"><span className="flex items-center gap-1.5 text-xs font-semibold text-foreground"><Eye className="size-3.5 text-primary" /> Graphics Quality</span><div className="grid grid-cols-3 gap-1.5">{([ { id: 'fast', label: 'Fast', desc: 'Low detail' }, { id: 'fancy', label: 'Fancy', desc: 'High detail' }, { id: 'fabulous', label: 'Fabulous', desc: 'Best quality' } ] as const).map((g) => (<button key={g.id} type="button" onClick={() => updateSetting('graphics', g.id)} className={cn('rounded-lg border px-2 py-2 text-center transition-all', settings.graphics === g.id ? 'border-primary bg-primary/15 text-primary' : 'border-border bg-card/30 text-muted-foreground hover:border-primary/30 hover:text-foreground')}><span className="text-[10px] font-bold block">{g.label}</span><span className="text-[8px] opacity-60 block">{g.desc}</span></button>))}</div></div>
                <div className="space-y-2"><span className="flex items-center gap-1.5 text-xs font-semibold text-foreground"><Sparkles className="size-3.5 text-primary" /> Particles</span><div className="grid grid-cols-3 gap-1.5">{([ { id: 'all', label: 'All', desc: 'Full effects' }, { id: 'decreased', label: 'Reduced', desc: 'Medium' }, { id: 'minimal', label: 'Minimal', desc: 'Low' } ] as const).map((p) => (<button key={p.id} type="button" onClick={() => updateSetting('particles', p.id)} className={cn('rounded-lg border px-2 py-2 text-center transition-all', settings.particles === p.id ? 'border-primary bg-primary/15 text-primary' : 'border-border bg-card/30 text-muted-foreground hover:border-primary/30 hover:text-foreground')}><span className="text-[10px] font-bold block">{p.label}</span><span className="text-[8px] opacity-60 block">{p.desc}</span></button>))}</div></div>
              </div>
              <div className="border-r border-border p-4 space-y-3">
                <h4 className="text-[10px] font-bold tracking-widest text-muted-foreground uppercase mb-3">Performance</h4>
                <div className="space-y-2"><div className="flex items-center justify-between"><span className="flex items-center gap-1.5 text-xs font-semibold text-foreground"><Monitor className="size-3.5 text-primary" /> FPS Limit</span><span className="text-xs font-bold text-primary tabular-nums">{settings.fpsLimit === 0 ? 'Unlimited' : settings.fpsLimit}</span></div><div className="grid grid-cols-4 gap-1.5">{([ { value: 0, label: 'Unlimited' }, { value: 30, label: '30' }, { value: 60, label: '60' }, { value: 120, label: '120' } ]).map((fps) => (<button key={fps.value} type="button" onClick={() => updateSetting('fpsLimit', fps.value)} className={cn('rounded-lg border px-2 py-2 text-center transition-all', settings.fpsLimit === fps.value ? 'border-primary bg-primary/15 text-primary' : 'border-border bg-card/30 text-muted-foreground hover:border-primary/30 hover:text-foreground')}><span className="text-[10px] font-bold">{fps.label}</span></button>))}</div></div>
                <div className="space-y-2"><span className="text-xs font-semibold text-foreground">Quick Presets</span><div className="grid grid-cols-2 gap-1.5">
                  <button type="button" onClick={() => setSettings({ ...DEFAULT_SETTINGS, renderDistance: 6, graphics: 'fast', fpsLimit: 30, particles: 'minimal', clouds: false, antialiasing: false, entityShadows: false })} className="rounded-lg border border-border bg-card/30 px-3 py-2.5 text-left transition-all hover:border-success/40 hover:bg-success/5"><span className="text-[10px] font-bold text-foreground block">Low-End PC</span><span className="text-[8px] text-muted-foreground">Max performance</span></button>
                  <button type="button" onClick={() => setSettings({ ...DEFAULT_SETTINGS, renderDistance: 12, graphics: 'fancy', fpsLimit: 60, particles: 'decreased' })} className="rounded-lg border border-border bg-card/30 px-3 py-2.5 text-left transition-all hover:border-primary/40 hover:bg-primary/5"><span className="text-[10px] font-bold text-foreground block">Balanced</span><span className="text-[8px] text-muted-foreground">Medium settings</span></button>
                  <button type="button" onClick={() => setSettings({ ...DEFAULT_SETTINGS, renderDistance: 24, graphics: 'fabulous', fpsLimit: 0, particles: 'all' })} className="rounded-lg border border-border bg-card/30 px-3 py-2.5 text-left transition-all hover:border-gold/40 hover:bg-gold/5"><span className="text-[10px] font-bold text-foreground block">High</span><span className="text-[8px] text-muted-foreground">Best graphics</span></button>
                  <button type="button" onClick={() => setSettings({ ...DEFAULT_SETTINGS, renderDistance: 32, graphics: 'fabulous', fpsLimit: 0, particles: 'all', antialiasing: true, mipmapLevels: 8 })} className="rounded-lg border border-border bg-card/30 px-3 py-2.5 text-left transition-all hover:border-accent/40 hover:bg-accent/5"><span className="text-[10px] font-bold text-foreground block">Ultra</span><span className="text-[8px] text-muted-foreground">Everything on</span></button>
                </div></div>
              </div>
              <div className="p-4 space-y-3">
                <h4 className="text-[10px] font-bold tracking-widest text-muted-foreground uppercase mb-3">Effects</h4>
                <div className="space-y-1">
                  <ToggleRow icon={<Cloud className="size-3.5 text-primary" />} label="Clouds" desc="Sky clouds" checked={settings.clouds} onChange={(v) => updateSetting('clouds', v)} />
                  <ToggleRow icon={<Monitor className="size-3.5 text-primary" />} label="View Bobbing" desc="Camera shake while walking" checked={settings.viewBobbing} onChange={(v) => updateSetting('viewBobbing', v)} />
                  <ToggleRow icon={<Monitor className="size-3.5 text-primary" />} label="VSync" desc="Prevents screen tearing" checked={settings.vsync} onChange={(v) => updateSetting('vsync', v)} />
                  <ToggleRow icon={<Eye className="size-3.5 text-primary" />} label="Antialiasing" desc="Edge smoothing" checked={settings.antialiasing} onChange={(v) => updateSetting('antialiasing', v)} />
                  <ToggleRow icon={<Eye className="size-3.5 text-primary" />} label="Shadows" desc="Entity shadows" checked={settings.entityShadows} onChange={(v) => updateSetting('entityShadows', v)} />
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}

function ToggleRow({ icon, label, desc, checked, onChange }: { icon: React.ReactNode; label: string; desc: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <div className="flex items-center justify-between rounded-lg px-2 py-1.5 transition-colors hover:bg-card/30">
      <div className="flex items-center gap-2">{icon}<div className="flex flex-col"><span className="text-[11px] font-semibold text-foreground">{label}</span><span className="text-[8px] text-muted-foreground">{desc}</span></div></div>
      <button type="button" onClick={() => onChange(!checked)} className={cn('relative h-5 w-9 rounded-full transition-colors duration-200', checked ? 'bg-primary' : 'bg-border')}>
        <span className={cn('absolute top-0.5 left-0.5 size-4 rounded-full bg-white shadow-sm transition-transform duration-200', checked && 'translate-x-4')} />
      </button>
    </div>
  )
}

import { useState, useEffect } from 'react'
import { Monitor, Eye, Cloud, Sparkles, RotateCcw, Gamepad2, Info } from 'lucide-react'
import { cn } from '@/lib/utils'

interface GameSettings {
  renderDistance: number
  graphics: 'fast' | 'fancy' | 'fabulous'
  fpsLimit: number
  vsync: boolean
  viewBobbing: boolean
  clouds: boolean
  particles: 'all' | 'decreased' | 'minimal'
  antialiasing: boolean
  mipmapLevels: number
  entityShadows: boolean
}

const DEFAULT_SETTINGS: GameSettings = {
  renderDistance: 12,
  graphics: 'fancy',
  fpsLimit: 0,
  vsync: true,
  viewBobbing: true,
  clouds: true,
  particles: 'all',
  antialiasing: true,
  mipmapLevels: 4,
  entityShadows: true,
}

function loadSettings(): GameSettings {
  try {
    const stored = localStorage.getItem('dli_game_settings')
    if (stored) return { ...DEFAULT_SETTINGS, ...JSON.parse(stored) }
  } catch {}
  return DEFAULT_SETTINGS
}

function saveSettings(settings: GameSettings) {
  localStorage.setItem('dli_game_settings', JSON.stringify(settings))
}

export function SettingsPage() {
  const [settings, setSettings] = useState<GameSettings>(loadSettings)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    saveSettings(settings)
    setSaved(true)
    const t = setTimeout(() => setSaved(false), 1500)
    return () => clearTimeout(t)
  }, [settings])

  const resetSettings = () => setSettings(DEFAULT_SETTINGS)
  const updateSetting = <K extends keyof GameSettings>(key: K, value: GameSettings[K]) => {
    setSettings(prev => ({ ...prev, [key]: value }))
  }

  return (
    <div className="flex flex-col h-full p-6 gap-5 overflow-y-auto">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex size-10 items-center justify-center rounded-xl bg-primary/15">
            <Gamepad2 className="size-5 text-primary" />
          </div>
          <div>
            <h1 className="text-2xl font-black tracking-widest text-foreground uppercase">Game Settings</h1>
            <p className="text-sm text-muted-foreground">Minecraft graphics and performance settings</p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {saved && (
            <span className="text-xs text-success font-semibold animate-in fade-in">Saved!</span>
          )}
          <button
            type="button"
            onClick={resetSettings}
            className="flex items-center gap-2 rounded-xl border border-border bg-card/50 px-4 py-2 text-xs font-semibold text-muted-foreground transition-colors hover:border-primary/40 hover:text-foreground"
          >
            <RotateCcw className="size-3.5" />
            Reset to Default
          </button>
        </div>
      </div>

      {/* Row 1: Render Distance + FPS Limit */}
      <div className="grid grid-cols-2 gap-4">
        <div className="rounded-xl border border-border bg-card/50 p-4 space-y-3">
          <div className="flex items-center justify-between">
            <span className="flex items-center gap-2 text-sm font-semibold text-foreground">
              <Monitor className="size-4 text-primary" /> Render Distance
            </span>
            <span className="text-sm font-bold text-primary tabular-nums">{settings.renderDistance} chunks</span>
          </div>
          <input
            type="range"
            min={2}
            max={32}
            step={2}
            value={settings.renderDistance}
            onChange={(e) => updateSetting('renderDistance', Number(e.target.value))}
            className="w-full accent-primary h-2"
          />
          <div className="flex justify-between text-[10px] text-muted-foreground">
            <span>2 (Near)</span>
            <span>32 (Far)</span>
          </div>
        </div>

        <div className="rounded-xl border border-border bg-card/50 p-4 space-y-3">
          <div className="flex items-center justify-between">
            <span className="flex items-center gap-2 text-sm font-semibold text-foreground">
              <Monitor className="size-4 text-primary" /> FPS Limit
            </span>
            <span className="text-sm font-bold text-primary tabular-nums">{settings.fpsLimit === 0 ? 'Unlimited' : settings.fpsLimit}</span>
          </div>
          <div className="grid grid-cols-4 gap-2">
            {([
              { value: 0, label: 'Unlimited' },
              { value: 30, label: '30' },
              { value: 60, label: '60' },
              { value: 120, label: '120' },
            ]).map((fps) => (
              <button
                key={fps.value}
                type="button"
                onClick={() => updateSetting('fpsLimit', fps.value)}
                className={cn(
                  'rounded-xl border px-3 py-3 text-center transition-all',
                  settings.fpsLimit === fps.value
                    ? 'border-primary bg-primary/15 text-primary shadow-[0_0_12px] shadow-primary/20'
                    : 'border-border bg-card/30 text-muted-foreground hover:border-primary/30 hover:text-foreground',
                )}
              >
                <span className="text-xs font-bold">{fps.label}</span>
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Row 2: Graphics Quality + Particles */}
      <div className="grid grid-cols-2 gap-4">
        <div className="rounded-xl border border-border bg-card/50 p-4 space-y-3">
          <span className="flex items-center gap-2 text-sm font-semibold text-foreground">
            <Eye className="size-4 text-primary" /> Graphics Quality
          </span>
          <div className="grid grid-cols-3 gap-2">
            {([
              { id: 'fast', label: 'Fast', desc: 'Low detail' },
              { id: 'fancy', label: 'Fancy', desc: 'High detail' },
              { id: 'fabulous', label: 'Fabulous', desc: 'Best quality' },
            ] as const).map((g) => (
              <button
                key={g.id}
                type="button"
                onClick={() => updateSetting('graphics', g.id)}
                className={cn(
                  'rounded-xl border px-3 py-3 text-center transition-all',
                  settings.graphics === g.id
                    ? 'border-primary bg-primary/15 text-primary shadow-[0_0_12px] shadow-primary/20'
                    : 'border-border bg-card/30 text-muted-foreground hover:border-primary/30 hover:text-foreground',
                )}
              >
                <span className="text-xs font-bold block">{g.label}</span>
                <span className="text-[10px] opacity-60 block mt-0.5">{g.desc}</span>
              </button>
            ))}
          </div>
        </div>

        <div className="rounded-xl border border-border bg-card/50 p-4 space-y-3">
          <span className="flex items-center gap-2 text-sm font-semibold text-foreground">
            <Sparkles className="size-4 text-primary" /> Particles
          </span>
          <div className="grid grid-cols-3 gap-2">
            {([
              { id: 'all', label: 'All', desc: 'Full effects' },
              { id: 'decreased', label: 'Reduced', desc: 'Medium' },
              { id: 'minimal', label: 'Minimal', desc: 'Low' },
            ] as const).map((p) => (
              <button
                key={p.id}
                type="button"
                onClick={() => updateSetting('particles', p.id)}
                className={cn(
                  'rounded-xl border px-3 py-3 text-center transition-all',
                  settings.particles === p.id
                    ? 'border-primary bg-primary/15 text-primary shadow-[0_0_12px] shadow-primary/20'
                    : 'border-border bg-card/30 text-muted-foreground hover:border-primary/30 hover:text-foreground',
                )}
              >
                <span className="text-xs font-bold block">{p.label}</span>
                <span className="text-[10px] opacity-60 block mt-0.5">{p.desc}</span>
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Row 3: Quick Presets */}
      <div className="rounded-xl border border-border bg-card/50 p-4 space-y-3">
        <span className="text-sm font-semibold text-foreground">Quick Presets</span>
        <div className="grid grid-cols-4 gap-2">
          <button
            type="button"
            onClick={() => setSettings({ ...DEFAULT_SETTINGS, renderDistance: 6, graphics: 'fast', fpsLimit: 30, particles: 'minimal', clouds: false, antialiasing: false, entityShadows: false })}
            className="rounded-xl border border-border bg-card/30 px-4 py-3 text-left transition-all hover:border-success/40 hover:bg-success/5"
          >
            <span className="text-xs font-bold text-foreground block">Low-End PC</span>
            <span className="text-[10px] text-muted-foreground">Max performance</span>
          </button>
          <button
            type="button"
            onClick={() => setSettings({ ...DEFAULT_SETTINGS, renderDistance: 12, graphics: 'fancy', fpsLimit: 60, particles: 'decreased' })}
            className="rounded-xl border border-border bg-card/30 px-4 py-3 text-left transition-all hover:border-primary/40 hover:bg-primary/5"
          >
            <span className="text-xs font-bold text-foreground block">Balanced</span>
            <span className="text-[10px] text-muted-foreground">Medium settings</span>
          </button>
          <button
            type="button"
            onClick={() => setSettings({ ...DEFAULT_SETTINGS, renderDistance: 24, graphics: 'fabulous', fpsLimit: 0, particles: 'all' })}
            className="rounded-xl border border-border bg-card/30 px-4 py-3 text-left transition-all hover:border-gold/40 hover:bg-gold/5"
          >
            <span className="text-xs font-bold text-foreground block">High</span>
            <span className="text-[10px] text-muted-foreground">Best graphics</span>
          </button>
          <button
            type="button"
            onClick={() => setSettings({ ...DEFAULT_SETTINGS, renderDistance: 32, graphics: 'fabulous', fpsLimit: 0, particles: 'all', antialiasing: true, mipmapLevels: 8 })}
            className="rounded-xl border border-border bg-card/30 px-4 py-3 text-left transition-all hover:border-accent/40 hover:bg-accent/5"
          >
            <span className="text-xs font-bold text-foreground block">Ultra</span>
            <span className="text-[10px] text-muted-foreground">Everything on</span>
          </button>
        </div>
      </div>

      {/* Row 4: Toggles + Tips */}
      <div className="grid grid-cols-[1fr_1fr] gap-4">
        <div className="rounded-xl border border-border bg-card/50 p-4 space-y-1">
          <span className="flex items-center gap-2 text-sm font-semibold text-foreground mb-2">
            <Cloud className="size-4 text-primary" /> Effects
          </span>
          <ToggleRow icon={<Cloud className="size-4 text-primary" />} label="Clouds" desc="Sky clouds" checked={settings.clouds} onChange={(v) => updateSetting('clouds', v)} />
          <ToggleRow icon={<Monitor className="size-4 text-primary" />} label="View Bobbing" desc="Camera shake while walking" checked={settings.viewBobbing} onChange={(v) => updateSetting('viewBobbing', v)} />
          <ToggleRow icon={<Monitor className="size-4 text-primary" />} label="VSync" desc="Prevents screen tearing" checked={settings.vsync} onChange={(v) => updateSetting('vsync', v)} />
          <ToggleRow icon={<Eye className="size-4 text-primary" />} label="Antialiasing" desc="Edge smoothing" checked={settings.antialiasing} onChange={(v) => updateSetting('antialiasing', v)} />
          <ToggleRow icon={<Eye className="size-4 text-primary" />} label="Shadows" desc="Entity shadows" checked={settings.entityShadows} onChange={(v) => updateSetting('entityShadows', v)} />
        </div>

        <div className="rounded-xl border border-primary/15 bg-primary/5 p-4">
          <span className="flex items-center gap-2 text-xs font-bold text-primary mb-3">
            <Info className="size-3.5" /> Performance Tips
          </span>
          <ul className="space-y-2 text-[11px] text-muted-foreground leading-relaxed">
            <li className="flex items-start gap-2">
              <span className="text-primary mt-0.5">&#9654;</span>
              <span>Lower Render Distance to 8 chunks = <strong className="text-foreground">30%+ FPS boost</strong></span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-primary mt-0.5">&#9654;</span>
              <span>"Fast" graphics auto-disables shadows and clouds</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-primary mt-0.5">&#9654;</span>
              <span>Set Particles to "Minimal" to reduce lag in fights</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-primary mt-0.5">&#9654;</span>
              <span>Disable VSync to reduce input latency</span>
            </li>
          </ul>
        </div>
      </div>
    </div>
  )
}

function ToggleRow({ icon, label, desc, checked, onChange }: { icon: React.ReactNode; label: string; desc: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <div className="flex items-center justify-between rounded-xl px-3 py-3 transition-colors hover:bg-card/30">
      <div className="flex items-center gap-3">
        {icon}
        <div className="flex flex-col">
          <span className="text-sm font-semibold text-foreground">{label}</span>
          <span className="text-[10px] text-muted-foreground">{desc}</span>
        </div>
      </div>
      <button
        type="button"
        onClick={() => onChange(!checked)}
        className={cn(
          'relative h-6 w-11 rounded-full transition-colors duration-200',
          checked ? 'bg-primary' : 'bg-border',
        )}
      >
        <span
          className={cn(
            'absolute top-0.5 left-0.5 size-5 rounded-full bg-white shadow-sm transition-transform duration-200',
            checked && 'translate-x-5',
          )}
        />
      </button>
    </div>
  )
}

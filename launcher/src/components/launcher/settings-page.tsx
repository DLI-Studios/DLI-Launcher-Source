import { useState, useEffect } from 'react'
import { Gauge, Sparkles, Crosshair, Cog, Languages, RotateCcw, Gamepad2, Info, ChevronDown } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useI18n } from '@/lib/i18n'

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
  fov: number
  sensitivity: number
  fullscreen: boolean
  showFps: boolean
  autoLaunch: boolean
  richPresence: boolean
  keepLauncherOpen: boolean
  autoCheckUpdates: boolean
  theme: 'original' | 'cyan'
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
  fov: 70,
  sensitivity: 0.5,
  fullscreen: true,
  showFps: false,
  autoLaunch: false,
  richPresence: true,
  keepLauncherOpen: true,
  autoCheckUpdates: true,
  theme: 'original',
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
  const { t, lang, setLang } = useI18n()
  const [settings, setSettings] = useState<GameSettings>(loadSettings)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    saveSettings(settings)
    setSaved(true)
    const timer = setTimeout(() => setSaved(false), 1500)
    return () => clearTimeout(timer)
  }, [settings])

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', settings.theme)
  }, [settings.theme])

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
            <h1 className="text-2xl font-black tracking-widest text-foreground uppercase">{t('settings.title')}</h1>
            <p className="text-sm text-muted-foreground">{t('settings.subtitle')}</p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {saved && (
            <span className="text-xs text-success font-semibold animate-in fade-in">{t('settings.saved')}</span>
          )}
          <button
            type="button"
            onClick={resetSettings}
            className="flex items-center gap-2 rounded-xl border border-border bg-card/50 px-4 py-2 text-xs font-semibold text-muted-foreground transition-colors hover:border-primary/40 hover:text-foreground"
          >
            <RotateCcw className="size-3.5" />
            {t('settings.reset')}
          </button>
        </div>
      </div>

      {/* ─── Performance ─── */}
      <Section icon={<Gauge className="size-4 text-primary" />} title={t('settings.category.performance')}>
        {/* Quick Presets */}
        <div className="space-y-2">
          <span className="text-xs font-semibold text-muted-foreground">{t('settings.quickPresets')}</span>
          <div className="grid grid-cols-4 gap-2">
            <PresetButton
              label={t('settings.lowEndPc')}
              desc={t('settings.maxPerformance')}
              onClick={() => setSettings({ ...DEFAULT_SETTINGS, renderDistance: 6, graphics: 'fast', fpsLimit: 30, particles: 'minimal', clouds: false, antialiasing: false, entityShadows: false })}
              hoverClass="hover:border-success/40 hover:bg-success/5"
            />
            <PresetButton
              label={t('settings.balanced')}
              desc={t('settings.mediumSettings')}
              onClick={() => setSettings({ ...DEFAULT_SETTINGS, renderDistance: 12, graphics: 'fancy', fpsLimit: 60, particles: 'decreased' })}
              hoverClass="hover:border-primary/40 hover:bg-primary/5"
            />
            <PresetButton
              label={t('settings.high')}
              desc={t('settings.bestGraphics')}
              onClick={() => setSettings({ ...DEFAULT_SETTINGS, renderDistance: 24, graphics: 'fabulous', fpsLimit: 0, particles: 'all' })}
              hoverClass="hover:border-gold/40 hover:bg-gold/5"
            />
            <PresetButton
              label={t('settings.ultra')}
              desc={t('settings.everythingOn')}
              onClick={() => setSettings({ ...DEFAULT_SETTINGS, renderDistance: 32, graphics: 'fabulous', fpsLimit: 0, particles: 'all', antialiasing: true, mipmapLevels: 8 })}
              hoverClass="hover:border-accent/40 hover:bg-accent/5"
            />
          </div>
        </div>

        {/* Render Distance + FPS Limit */}
        <div className="grid grid-cols-2 gap-3">
          <SliderCard
            label={t('settings.renderDistance')}
            display={t('settings.chunkCount', { n: settings.renderDistance })}
            min={2}
            max={32}
            step={2}
            value={settings.renderDistance}
            minLabel={t('settings.near')}
            maxLabel={t('settings.far')}
            onChange={(v) => updateSetting('renderDistance', v)}
          />
          <OptionCard
            label={t('settings.fpsLimit')}
            display={settings.fpsLimit === 0 ? t('settings.unlimited') : String(settings.fpsLimit)}
            options={[
              { id: 0 as const, label: t('settings.unlimited') },
              { id: 30 as const, label: '30' },
              { id: 60 as const, label: '60' },
              { id: 120 as const, label: '120' },
            ]}
            value={settings.fpsLimit}
            onSelect={(v) => updateSetting('fpsLimit', v)}
          />
        </div>

        {/* Graphics + Particles */}
        <div className="grid grid-cols-2 gap-3">
          <OptionCard
            label={t('settings.graphics')}
            options={[
              { id: 'fast' as const, label: t('settings.fast'), desc: t('settings.lowDetail') },
              { id: 'fancy' as const, label: t('settings.fancy'), desc: t('settings.highDetail') },
              { id: 'fabulous' as const, label: t('settings.fabulous'), desc: t('settings.bestQuality') },
            ]}
            value={settings.graphics}
            onSelect={(v) => updateSetting('graphics', v)}
          />
          <OptionCard
            label={t('settings.particles')}
            options={[
              { id: 'all' as const, label: t('settings.all'), desc: t('settings.fullEffects') },
              { id: 'decreased' as const, label: t('settings.reduced'), desc: t('settings.medium') },
              { id: 'minimal' as const, label: t('settings.minimal'), desc: t('settings.low') },
            ]}
            value={settings.particles}
            onSelect={(v) => updateSetting('particles', v)}
          />
        </div>

        {/* Tips */}
        <div className="rounded-xl border border-primary/15 bg-primary/5 p-4">
          <span className="flex items-center gap-2 text-xs font-bold text-primary mb-3">
            <Info className="size-3.5" /> {t('settings.performanceTips')}
          </span>
          <ul className="space-y-2 text-[11px] text-muted-foreground leading-relaxed">
            <li className="flex items-start gap-2">
              <span className="text-primary mt-0.5">&#9654;</span>
              <span>{t('settings.tip1a')} <strong className="text-foreground">{t('settings.tip1b')}</strong></span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-primary mt-0.5">&#9654;</span>
              <span>{t('settings.tip2')}</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-primary mt-0.5">&#9654;</span>
              <span>{t('settings.tip3')}</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-primary mt-0.5">&#9654;</span>
              <span>{t('settings.tip4')}</span>
            </li>
          </ul>
        </div>
      </Section>

      {/* ─── Visual Effects ─── */}
      <Section icon={<Sparkles className="size-4 text-primary" />} title={t('settings.category.visuals')}>
        <div className="grid grid-cols-2 gap-2">
          <ToggleRow label={t('settings.antialiasing')} desc={t('settings.edgeSmoothing')} checked={settings.antialiasing} onChange={(v) => updateSetting('antialiasing', v)} />
          <ToggleRow label={t('settings.shadows')} desc={t('settings.entityShadows')} checked={settings.entityShadows} onChange={(v) => updateSetting('entityShadows', v)} />
          <ToggleRow label={t('settings.clouds')} desc={t('settings.skyClouds')} checked={settings.clouds} onChange={(v) => updateSetting('clouds', v)} />
          <ToggleRow label={t('settings.viewBobbing')} desc={t('settings.cameraShake')} checked={settings.viewBobbing} onChange={(v) => updateSetting('viewBobbing', v)} />
          <ToggleRow label={t('settings.vsync')} desc={t('settings.screenTearing')} checked={settings.vsync} onChange={(v) => updateSetting('vsync', v)} />
        </div>
      </Section>

      {/* ─── Gameplay ─── */}
      <Section icon={<Crosshair className="size-4 text-primary" />} title={t('settings.category.gameplay')}>
        <div className="grid grid-cols-2 gap-3">
          <SliderCard
            label={t('settings.fov')}
            display={`${settings.fov}°`}
            min={50}
            max={120}
            step={1}
            value={settings.fov}
            minLabel="50°"
            maxLabel="120°"
            onChange={(v) => updateSetting('fov', v)}
          />
          <SliderCard
            label={t('settings.sensitivity')}
            display={settings.sensitivity.toFixed(2)}
            min={0.1}
            max={2}
            step={0.05}
            value={settings.sensitivity}
            minLabel="0.1"
            maxLabel="2.0"
            onChange={(v) => updateSetting('sensitivity', v)}
          />
        </div>
        <div className="grid grid-cols-2 gap-2">
          <ToggleRow label={t('settings.fullscreen')} desc={t('settings.fullscreenDesc')} checked={settings.fullscreen} onChange={(v) => updateSetting('fullscreen', v)} />
          <ToggleRow label={t('settings.showFps')} desc={t('settings.showFpsDesc')} checked={settings.showFps} onChange={(v) => updateSetting('showFps', v)} />
        </div>
      </Section>

      {/* ─── Launcher ─── */}
      <Section icon={<Cog className="size-4 text-primary" />} title={t('settings.category.launcher')}>
        <div className="grid grid-cols-2 gap-2">
          <ToggleRow label={t('settings.autoLaunch')} desc={t('settings.autoLaunchDesc')} checked={settings.autoLaunch} onChange={(v) => updateSetting('autoLaunch', v)} />
          <ToggleRow label={t('settings.richPresence')} desc={t('settings.richPresenceDesc')} checked={settings.richPresence} onChange={(v) => updateSetting('richPresence', v)} />
          <ToggleRow label={t('settings.keepLauncherOpen')} desc={t('settings.keepLauncherOpenDesc')} checked={settings.keepLauncherOpen} onChange={(v) => updateSetting('keepLauncherOpen', v)} />
          <ToggleRow label={t('settings.autoCheckUpdates')} desc={t('settings.autoCheckUpdatesDesc')} checked={settings.autoCheckUpdates} onChange={(v) => updateSetting('autoCheckUpdates', v)} />
        </div>
      </Section>

      {/* ─── Theme ─── */}
      <Section icon={<Sparkles className="size-4 text-primary" />} title={t('settings.theme')}>
        <div className="flex items-center gap-3">
          <span className="text-sm text-muted-foreground w-24 shrink-0">{t('settings.themeLabel')}</span>
          <ThemeSelect value={settings.theme} onChange={(v) => updateSetting('theme', v)} />
        </div>
      </Section>

      {/* ─── Language ─── */}
      <Section icon={<Languages className="size-4 text-primary" />} title={t('settings.language')}>
        <p className="text-[11px] text-muted-foreground -mt-2">{t('settings.languageDesc')}</p>
        <div className="flex w-fit gap-2 rounded-xl border border-border bg-card/30 p-1">
          {(['tr', 'en'] as const).map((l) => (
            <button
              key={l}
              type="button"
              onClick={() => setLang(l)}
              className={`flex items-center gap-2 rounded-lg px-4 py-2 text-xs font-bold uppercase transition-all ${
                lang === l ? 'bg-primary text-white shadow-lg shadow-primary/30' : 'text-muted-foreground hover:text-foreground'
              }`}
            >
              {l === 'tr' ? t('settings.languageTr') : t('settings.languageEn')}
            </button>
          ))}
        </div>
      </Section>
    </div>
  )
}

function Section({ icon, title, children }: { icon: React.ReactNode; title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-2xl border border-border bg-card/50 p-5 space-y-4">
      <div className="flex items-center gap-3">
        <div className="flex size-9 items-center justify-center rounded-xl bg-primary/15">
          {icon}
        </div>
        <h2 className="text-sm font-bold tracking-wide text-foreground uppercase">{title}</h2>
      </div>
      {children}
    </div>
  )
}

function SliderCard({
  label,
  display,
  min,
  max,
  step,
  value,
  minLabel,
  maxLabel,
  onChange,
}: {
  label: string
  display: string
  min: number
  max: number
  step: number
  value: number
  minLabel: string
  maxLabel: string
  onChange: (v: number) => void
}) {
  return (
    <div className="rounded-xl border border-border bg-card/30 p-4 space-y-3">
      <div className="flex items-center justify-between">
        <span className="text-sm font-semibold text-foreground">{label}</span>
        <span className="text-sm font-bold text-primary tabular-nums">{display}</span>
      </div>
      <input
        type="range"
        min={min}
        max={max}
        step={step}
        value={value}
        onChange={(e) => onChange(Number(e.target.value))}
        className="w-full accent-primary h-2"
      />
      <div className="flex justify-between text-[10px] text-muted-foreground">
        <span>{minLabel}</span>
        <span>{maxLabel}</span>
      </div>
    </div>
  )
}

function OptionCard<T extends string | number>({
  label,
  display,
  options,
  value,
  onSelect,
}: {
  label: string
  display?: string
  options: { id: T; label: string; desc?: string }[]
  value: T
  onSelect: (v: T) => void
}) {
  return (
    <div className="rounded-xl border border-border bg-card/30 p-4 space-y-3">
      <div className="flex items-center justify-between">
        <span className="text-sm font-semibold text-foreground">{label}</span>
        {display !== undefined && (
          <span className="text-sm font-bold text-primary tabular-nums">{display}</span>
        )}
      </div>
      <div className="grid gap-2" style={{ gridTemplateColumns: `repeat(${Math.min(options.length, 4)}, minmax(0, 1fr))` }}>
        {options.map((o) => (
          <button
            key={String(o.id)}
            type="button"
            onClick={() => onSelect(o.id)}
            className={cn(
              'rounded-xl border px-3 py-3 text-center transition-all',
              value === o.id
                ? 'border-primary bg-primary/15 text-primary shadow-[0_0_12px] shadow-primary/20'
                : 'border-border bg-card/30 text-muted-foreground hover:border-primary/30 hover:text-foreground',
            )}
          >
            <span className="text-xs font-bold block">{o.label}</span>
            {o.desc && <span className="text-[10px] opacity-60 block mt-0.5">{o.desc}</span>}
          </button>
        ))}
      </div>
    </div>
  )
}

function ToggleRow({ label, desc, checked, onChange }: { label: string; desc: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <div className="flex items-center justify-between gap-3 rounded-xl border border-border bg-card/30 px-4 py-3 transition-colors hover:border-primary/25">
      <div className="flex flex-col">
        <span className="text-sm font-semibold text-foreground">{label}</span>
        <span className="text-[10px] text-muted-foreground">{desc}</span>
      </div>
      <button
        type="button"
        onClick={() => onChange(!checked)}
        className={cn(
          'relative h-6 w-11 shrink-0 rounded-full transition-colors duration-200',
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

function PresetButton({ label, desc, onClick, hoverClass }: { label: string; desc: string; onClick: () => void; hoverClass: string }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`rounded-xl border border-border bg-card/30 px-4 py-3 text-left transition-all ${hoverClass}`}
    >
      <span className="text-xs font-bold text-foreground block">{label}</span>
      <span className="text-[10px] text-muted-foreground">{desc}</span>
    </button>
  )
}

function ThemeSelect({ value, onChange }: { value: 'original' | 'cyan'; onChange: (v: 'original' | 'cyan') => void }) {
  const [open, setOpen] = useState(false)
  const { t } = useI18n()
  const options = [
    { value: 'original' as const, label: 'Orijinal' },
    { value: 'cyan' as const, label: 'Cyan' },
  ] as const
  return (
    <div className="relative flex-1">
      <button
        type="button"
        onClick={() => setOpen(!open)}
        className="flex w-full items-center justify-between rounded-xl border border-border bg-card/30 px-4 py-2.5 text-sm font-medium text-foreground transition-colors hover:border-primary/50 focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
      >
        <span>{t(value === 'original' ? 'settings.themeOriginal' : 'settings.themeCyan')}</span>
        <ChevronDown className={`size-4 transition-transform ${open ? 'rotate-180' : ''}`} />
      </button>
      {open && (
        <div className="absolute z-50 mt-1 w-full rounded-xl border border-border bg-card p-1 shadow-lg animate-in fade-in-0 zoom-in-95 duration-150">
          {options.map((opt) => (
            <button
              key={opt.value}
              type="button"
              onClick={() => {
                onChange(opt.value)
                setOpen(false)
              }}
              className={`flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
                value === opt.value
                  ? 'bg-primary/15 text-primary'
                  : 'text-foreground hover:bg-primary/5'
              }`}
            >
              <span className="size-2 rounded-full" style={{ background: opt.value === 'original' ? 'linear-gradient(135deg, #7c3aed, #a855f7)' : 'linear-gradient(135deg, #06b6d4, #22d3ee)' }} />
              <span>{t(opt.value === 'original' ? 'settings.themeOriginal' : 'settings.themeCyan')}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

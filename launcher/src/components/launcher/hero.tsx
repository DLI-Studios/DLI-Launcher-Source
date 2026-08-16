import { useI18n } from '@/lib/i18n'

export function Hero() {
  const { t } = useI18n()
  return (
    <section
      className="relative flex flex-1 flex-col items-center justify-center overflow-hidden"
      aria-label={t('aria.gamingPlatform')}
    >
      {/* Background */}
      <img
        src="/images/hero-bg.png"
        alt=""
        className="absolute inset-0 size-full object-cover"
      />
      <div className="absolute inset-0 bg-gradient-to-t from-background via-background/30 to-background/20" />

      {/* Content */}
      <div className="relative z-10 flex flex-col items-center gap-1 px-6 text-center">
        <h1 className="text-8xl font-black italic tracking-tight text-foreground drop-shadow-[0_0_50px_rgba(168,85,247,0.5)]">
          DLI
        </h1>
        <p className="text-sm font-semibold tracking-[0.6em] text-foreground/70 uppercase">
          {t('hero.gamingPlatform')}
        </p>
        <p className="mt-6 text-xl font-bold tracking-wider text-foreground/80">
          {t('hero.tagline')}
        </p>
        <p className="text-xs tracking-[0.2em] text-foreground/40 uppercase">
          {t('hero.premiumExperience')}
        </p>
      </div>
    </section>
  )
}

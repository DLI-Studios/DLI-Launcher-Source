import { Newspaper } from 'lucide-react'
import { useI18n } from '@/lib/i18n'

export function NewsPanel() {
  const { t } = useI18n()
  return (
    <section className="glass flex min-h-0 flex-1 flex-col rounded-xl border border-border p-5" aria-label={t('aria.latestNews')}>
      <div className="flex items-center justify-between">
        <h2 className="text-sm font-bold tracking-widest text-foreground uppercase">
          {t('news.latest')}
        </h2>
        <button
          type="button"
          className="text-xs font-semibold tracking-wider text-accent uppercase transition-colors hover:text-primary"
        >
          {t('news.viewAll')}
        </button>
      </div>

      <div className="mt-4 flex flex-1 flex-col items-center justify-center gap-3 text-center">
        <div className="flex size-12 items-center justify-center rounded-xl bg-primary/10">
          <Newspaper className="size-6 text-primary/50" />
        </div>
        <p className="text-xs text-muted-foreground">{t('news.noNews')}</p>
        <p className="text-[10px] text-muted-foreground/50">{t('news.checkBack')}</p>
      </div>
    </section>
  )
}

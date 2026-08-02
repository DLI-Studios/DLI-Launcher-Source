import { Headphones, MessageCircle, UserPlus, Vote } from 'lucide-react'
import { useI18n } from '@/lib/i18n'
import type { TKey } from '@/lib/i18n'

const actions: { labelKey: TKey; icon: typeof UserPlus }[] = [
  { labelKey: 'quickActions.invite', icon: UserPlus },
  { labelKey: 'quickActions.vote', icon: Vote },
  { labelKey: 'quickActions.support', icon: Headphones },
  { labelKey: 'quickActions.discord', icon: MessageCircle },
]

export function QuickActions() {
  const { t } = useI18n()
  return (
    <section className="glass shrink-0 rounded-xl border border-border p-4" aria-label={t('aria.quickActions')}>
      <h2 className="text-sm font-bold tracking-widest text-foreground uppercase">
        {t('quickActions.title')}
      </h2>
      <div className="mt-4 grid grid-cols-4 gap-3">
        {actions.map(({ labelKey, icon: Icon }) => (
          <button
            key={labelKey}
            type="button"
            className="group flex flex-col items-center gap-2 rounded-xl border border-border bg-card/70 py-3 transition-all duration-200 hover:-translate-y-0.5 hover:border-primary/50"
          >
            <Icon className="size-5 text-muted-foreground transition-colors group-hover:text-primary" />
            <span className="text-[11px] font-semibold text-foreground/80">{t(labelKey)}</span>
          </button>
        ))}
      </div>
    </section>
  )
}

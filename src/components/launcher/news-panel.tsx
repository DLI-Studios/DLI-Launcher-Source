import { Newspaper } from 'lucide-react'

export function NewsPanel() {
  return (
    <section className="glass flex min-h-0 flex-1 flex-col rounded-xl border border-border p-5" aria-label="Latest news">
      <div className="flex items-center justify-between">
        <h2 className="text-sm font-bold tracking-widest text-foreground uppercase">
          Latest News
        </h2>
        <button
          type="button"
          className="text-xs font-semibold tracking-wider text-accent uppercase transition-colors hover:text-primary"
        >
          View All
        </button>
      </div>

      <div className="mt-4 flex flex-1 flex-col items-center justify-center gap-3 text-center">
        <div className="flex size-12 items-center justify-center rounded-xl bg-primary/10">
          <Newspaper className="size-6 text-primary/50" />
        </div>
        <p className="text-xs text-muted-foreground">No news yet</p>
        <p className="text-[10px] text-muted-foreground/50">Check back later for updates</p>
      </div>
    </section>
  )
}

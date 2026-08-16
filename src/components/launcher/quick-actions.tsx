import { Headphones, MessageCircle, UserPlus, Vote } from 'lucide-react'

const actions = [
  { label: 'Invite', icon: UserPlus },
  { label: 'Vote', icon: Vote },
  { label: 'Support', icon: Headphones },
  { label: 'Discord', icon: MessageCircle },
]

export function QuickActions() {
  return (
    <section className="glass shrink-0 rounded-xl border border-border p-4" aria-label="Quick actions">
      <h2 className="text-sm font-bold tracking-widest text-foreground uppercase">Quick Actions</h2>
      <div className="mt-4 grid grid-cols-4 gap-3">
        {actions.map(({ label, icon: Icon }) => (
          <button key={label} type="button" className="group flex flex-col items-center gap-2 rounded-xl border border-border bg-card/70 py-3 transition-all duration-200 hover:-translate-y-0.5 hover:border-primary/50">
            <Icon className="size-5 text-muted-foreground transition-colors group-hover:text-primary" />
            <span className="text-[11px] font-semibold text-foreground/80">{label}</span>
          </button>
        ))}
      </div>
    </section>
  )
}

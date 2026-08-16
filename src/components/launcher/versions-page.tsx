import { useState } from 'react'
import { Search, Check, Download, ArrowUp } from 'lucide-react'
import { cn } from '@/lib/utils'

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

interface VersionsPageProps { onSelect: (version: string) => void; selectedVersion: string }

export function VersionsPage({ onSelect, selectedVersion }: VersionsPageProps) {
  const [search, setSearch] = useState('')
  const filtered = search ? ALL_VERSIONS.filter(v => v.id.includes(search) || v.date.includes(search)) : ALL_VERSIONS

  return (
    <div className="flex flex-col h-full p-6 gap-4">
      <div className="flex items-center justify-between">
        <div><h1 className="text-2xl font-black tracking-widest text-foreground uppercase">Versions</h1><p className="text-sm text-muted-foreground">Select a Minecraft version</p></div>
        <span className="text-sm text-muted-foreground">{ALL_VERSIONS.length} versions</span>
      </div>
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
        <input type="text" placeholder="Search version or date... (e.g. 1.20 or 2024)" value={search} onChange={(e) => setSearch(e.target.value)} className="w-full rounded-xl border border-border bg-secondary pl-10 pr-4 py-3 text-sm text-foreground placeholder:text-muted-foreground focus:border-primary focus:outline-none" />
      </div>
      {selectedVersion && <div className="flex items-center gap-2 text-sm"><span className="text-muted-foreground">Selected:</span><span className="font-bold text-primary">{selectedVersion}</span></div>}
      <div className="flex-1 overflow-y-auto rounded-xl border border-border bg-card/30">
        <div className="grid grid-cols-1 divide-y divide-border">
          {filtered.map((v) => (
            <button key={v.id} type="button" onClick={() => onSelect(v.id)} className={cn('flex items-center justify-between px-5 py-3.5 text-left transition-all duration-150 hover:bg-secondary/60', v.id === selectedVersion && 'bg-primary/10 border-l-2 border-l-primary')}>
              <div className="flex items-center gap-3">
                <span className={cn('text-base font-bold', v.id === selectedVersion ? 'text-primary' : 'text-foreground')}>{v.id}</span>
                {v.id === selectedVersion && <Check className="size-4 text-primary" />}
              </div>
              <span className="text-xs text-muted-foreground">{v.date}</span>
            </button>
          ))}
          {filtered.length === 0 && <div className="px-5 py-10 text-center text-sm text-muted-foreground">No versions match "{search}"</div>}
        </div>
      </div>
    </div>
  )
}

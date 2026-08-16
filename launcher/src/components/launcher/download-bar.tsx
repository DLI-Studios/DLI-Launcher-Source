import { useEffect, useRef, useState } from 'react'
import { Download } from 'lucide-react'
import { launcherBridge } from '@/services/launcherBridge'
import { useI18n } from '@/lib/i18n'

interface DownloadBarProps {
  version: string
  totalMb: number
  onComplete: () => void
}

export function DownloadBar({ version, totalMb, onComplete }: DownloadBarProps) {
  const { t } = useI18n()
  const [show, setShow] = useState(false)
  const [progress, setProgress] = useState(0)
  const [status, setStatus] = useState<'downloading' | 'completed' | 'error'>('downloading')
  const [currentFile, setCurrentFile] = useState('')
  const hideTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    const unsubscribe = launcherBridge.onMessage((msg) => {
      const data = msg as Record<string, unknown>
      const type = data.type as string

      if (type === 'DOWNLOAD_PROGRESS') {
        const d = data.data as Record<string, unknown>
        setProgress(d.percent as number)
        setCurrentFile((d.file as string) || '')
      }

      if (type === 'DOWNLOAD_COMPLETE') {
        setStatus('completed')
        setProgress(100)
        hideTimerRef.current = setTimeout(() => {
          setShow(false)
          hideTimerRef.current = setTimeout(() => onComplete(), 300)
        }, 3000)
      }

      if (type === 'DOWNLOAD_ERROR') {
        setStatus('error')
        hideTimerRef.current = setTimeout(() => {
          setShow(false)
          hideTimerRef.current = setTimeout(() => onComplete(), 300)
        }, 3000)
      }
    })

    requestAnimationFrame(() => requestAnimationFrame(() => setShow(true)))

    return () => {
      unsubscribe()
      if (hideTimerRef.current) clearTimeout(hideTimerRef.current)
    }
  }, [])

  const downloaded = Math.round((progress / 100) * totalMb)
  const remaining = Math.round(((100 - progress) / 100) * totalMb)
  const done = status === 'completed'
  const isError = status === 'error'

  return (
    <div className="transition-all duration-300 ease-in-out" style={{ opacity: show ? 1 : 0, transform: show ? 'translateY(0)' : 'translateY(10px)' }}>
      {done ? (
        <div className="flex items-center gap-4 rounded-xl border border-white/10 bg-white/5 px-5 py-3 backdrop-blur-md">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-green-500/15">
            <Download className="size-5 text-green-400" />
          </span>
          <div className="flex min-w-0 flex-1 flex-col">
            <span className="truncate text-sm font-semibold text-white">{t('download.ready', { version })}</span>
            <span className="text-xs text-white/50">{t('download.starting')}</span>
          </div>
        </div>
      ) : isError ? (
        <div className="flex items-center gap-4 rounded-xl border border-white/10 bg-white/5 px-5 py-3 backdrop-blur-md">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-red-500/15">
            <Download className="size-5 text-red-400" />
          </span>
          <span className="truncate text-sm font-semibold text-white">{t('download.error')}</span>
        </div>
      ) : (
        <div className="flex items-center gap-4 rounded-xl border border-white/10 bg-white/5 px-5 py-3 backdrop-blur-md">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-primary/15">
            <Download className="size-5 text-primary" />
          </span>
          <div className="flex min-w-0 flex-1 flex-col gap-1.5">
            <div className="flex items-center justify-between gap-4 text-sm">
              <span className="truncate font-semibold text-white">{t('download.downloading', { version })}</span>
              <span className="flex shrink-0 items-center gap-4 text-xs text-white/60">
                <span>{t('download.pctRemaining', { p: Math.floor(progress), r: 100 - Math.floor(progress) })}</span>
              </span>
            </div>
            {currentFile && (
              <span className="text-[10px] text-white/40 truncate">{currentFile}</span>
            )}
            <div role="progressbar" aria-valuenow={Math.floor(progress)} aria-valuemin={0} aria-valuemax={100} className="h-1.5 overflow-hidden rounded-full bg-white/10">
              <div className="h-full rounded-full bg-primary shadow-[0_0_8px] shadow-primary/70 transition-[width] duration-300" style={{ width: `${progress}%` }} />
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

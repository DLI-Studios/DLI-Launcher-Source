import { useEffect, useRef, useState } from 'react'
import { Download } from 'lucide-react'
import { launcherBridge } from '@/services/launcherBridge'

interface DownloadBarProps { version: string; totalMb: number; onComplete: () => void }

export function DownloadBar({ version, totalMb, onComplete }: DownloadBarProps) {
  const [show, setShow] = useState(false)
  const [progress, setProgress] = useState(0)
  const [status, setStatus] = useState<'downloading' | 'completed' | 'error'>('downloading')
  const [currentFile, setCurrentFile] = useState('')
  const hideTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    const unsubscribe = launcherBridge.onMessage((msg) => {
      const data = msg as Record<string, unknown>
      const type = data.type as string
      if (type === 'DOWNLOAD_PROGRESS') { const d = data.data as Record<string, unknown>; setProgress(d.percent as number); setCurrentFile((d.file as string) || '') }
      if (type === 'DOWNLOAD_COMPLETE') { setStatus('completed'); setProgress(100); hideTimerRef.current = setTimeout(() => { setShow(false); hideTimerRef.current = setTimeout(() => onComplete(), 300) }, 3000) }
      if (type === 'DOWNLOAD_ERROR') { setStatus('error'); hideTimerRef.current = setTimeout(() => { setShow(false); hideTimerRef.current = setTimeout(() => onComplete(), 300) }, 3000) }
    })
    requestAnimationFrame(() => requestAnimationFrame(() => setShow(true)))
    return () => { unsubscribe(); if (hideTimerRef.current) clearTimeout(hideTimerRef.current) }
  }, [])

  const done = status === 'completed'
  const isError = status === 'error'

  return (
    <div className="transition-all duration-300 ease-in-out" style={{ opacity: show ? 1 : 0, transform: show ? 'translateY(0)' : 'translateY(10px)' }}>
      {done ? (
        <div className="flex items-center gap-4 rounded-xl border border-white/10 bg-white/5 px-5 py-3 backdrop-blur-md">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-green-500/15"><Download className="size-5 text-green-400" /></span>
          <div className="flex min-w-0 flex-1 flex-col"><span className="truncate text-sm font-semibold text-white">Minecraft {version} ready!</span><span className="text-xs text-white/50">Starting game...</span></div>
        </div>
      ) : isError ? (
        <div className="flex items-center gap-4 rounded-xl border border-white/10 bg-white/5 px-5 py-3 backdrop-blur-md">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-red-500/15"><Download className="size-5 text-red-400" /></span>
          <span className="truncate text-sm font-semibold text-white">Download error</span>
        </div>
      ) : (
        <div className="flex items-center gap-4 rounded-xl border border-white/10 bg-white/5 px-5 py-3 backdrop-blur-md">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-purple-500/15"><Download className="size-5 text-purple-400" /></span>
          <div className="flex min-w-0 flex-1 flex-col gap-1.5">
            <div className="flex items-center justify-between gap-4 text-sm">
              <span className="truncate font-semibold text-white">Downloading Minecraft {version}...</span>
              <span className="flex shrink-0 items-center gap-4 text-xs text-white/60"><span>{Math.floor(progress)}% downloaded &bull; {100 - Math.floor(progress)}% remaining</span></span>
            </div>
            {currentFile && <span className="text-[10px] text-white/40 truncate">{currentFile}</span>}
            <div role="progressbar" aria-valuenow={Math.floor(progress)} aria-valuemin={0} aria-valuemax={100} className="h-1.5 overflow-hidden rounded-full bg-white/10">
              <div className="h-full rounded-full bg-purple-500 shadow-[0_0_8px] shadow-purple-500/70 transition-[width] duration-300" style={{ width: `${progress}%` }} />
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

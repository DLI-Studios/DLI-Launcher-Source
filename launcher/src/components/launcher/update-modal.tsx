import { useState, useEffect } from 'react'
import { launcherBridge } from '@/services/launcherBridge'
import { Sparkles, Download, RefreshCw, AlertCircle, CheckCircle2, X } from 'lucide-react'
import { useI18n } from '@/lib/i18n'

export interface UpdateInfo {
  available: boolean
  currentVersion: string
  latestVersion: string
  downloadUrl: string
  changelog: string[]
  mandatory?: boolean
}

export function UpdateModal() {
  const { t } = useI18n()
  const [updateInfo, setUpdateInfo] = useState<UpdateInfo | null>(null)
  const [isOpen, setIsOpen] = useState(false)
  const [isUpdating, setIsUpdating] = useState(false)
  const [progress, setProgress] = useState(0)
  const [statusText, setStatusText] = useState('')
  const [errorText, setErrorText] = useState('')

  useEffect(() => {
    // Açılışta güncelleme kontrolü yap
    const checkUpdate = async () => {
      try {
        const res = await launcherBridge.send('CHECK_FOR_UPDATES')
        if (res.success && res.data) {
          const info = res.data as UpdateInfo
          if (info.available) {
            setUpdateInfo(info)
            setIsOpen(true)
          }
        }
      } catch (err) {
        console.error('[Update Check Error]', err)
      }
    }

    checkUpdate()

    // C# tarafındaki push mesajlarını dinle
    const unsubscribe = launcherBridge.onMessage((msg: any) => {
      if (msg.type === 'UPDATE_PROGRESS') {
        setIsUpdating(true)
        if (typeof msg.data?.percent === 'number') {
          setProgress(msg.data.percent)
        }
        if (msg.data?.status) {
          setStatusText(msg.data.status)
        }
      } else if (msg.type === 'UPDATE_ERROR') {
        setIsUpdating(false)
        setErrorText(msg.data?.error || t('update.error'))
      }
    })

    return () => {
      unsubscribe()
    }
  }, [])

  const handleStartUpdate = async () => {
    setIsUpdating(true)
    setErrorText('')
    setStatusText(t('update.starting'))

    const res = await launcherBridge.send('START_UPDATE', {
      downloadUrl: updateInfo?.downloadUrl,
    })

    if (!res.success) {
      setIsUpdating(false)
      setErrorText(res.error || t('update.startFail'))
    }
  }

  if (!isOpen || !updateInfo) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4 backdrop-blur-md animate-in fade-in duration-300">
      <div className="relative w-full max-w-lg overflow-hidden rounded-2xl border border-primary/30 bg-background/90 p-6 shadow-2xl backdrop-blur-xl">
        {/* Arka plan parıltısı */}
        <div className="pointer-events-none absolute -right-20 -top-20 h-64 w-64 rounded-full bg-primary/20 blur-3xl" />
        <div className="pointer-events-none absolute -bottom-20 -left-20 h-64 w-64 rounded-full bg-accent/20 blur-3xl" />

        {/* Kapatma Butonu (Zorunlu değilse) */}
        {!updateInfo.mandatory && !isUpdating && (
          <button
            onClick={() => setIsOpen(false)}
            className="absolute right-4 top-4 rounded-full p-2 text-muted-foreground hover:bg-white/10 hover:text-white transition-colors"
          >
            <X className="h-5 w-5" />
          </button>
        )}

        {/* Header */}
        <div className="mb-6 flex items-center gap-4">
          <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-gradient-to-br from-primary to-accent shadow-lg shadow-primary/30">
            <Sparkles className="h-7 w-7 text-white" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="rounded-full bg-primary/20 px-2.5 py-0.5 text-xs font-bold text-primary border border-primary/30">
                v{updateInfo.latestVersion}
              </span>
              <span className="text-xs text-muted-foreground">{t('update.currentVersion', { cur: updateInfo.currentVersion })}</span>
            </div>
            <h2 className="text-2xl font-black italic tracking-wide text-white">{t('update.newUpdate')}</h2>
          </div>
        </div>

        {/* Güncelleme Notları */}
        {updateInfo.changelog && updateInfo.changelog.length > 0 && (
          <div className="mb-6 rounded-xl border border-white/10 bg-white/5 p-4">
            <h4 className="mb-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              {t('update.changelog')}
            </h4>
            <ul className="space-y-1.5 text-sm text-gray-200 max-h-40 overflow-y-auto pr-1 custom-scrollbar">
              {updateInfo.changelog.map((item, idx) => (
                <li key={idx} className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-primary shrink-0 mt-0.5" />
                  <span>{item}</span>
                </li>
              ))}
            </ul>
          </div>
        )}

        {/* Hata Mesajı */}
        {errorText && (
          <div className="mb-4 flex items-center gap-2 rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-400">
            <AlertCircle className="h-5 w-5 shrink-0" />
            <span>{errorText}</span>
          </div>
        )}

        {/* İndirme İlerleme Çubuğu */}
        {isUpdating ? (
          <div className="space-y-3 py-2">
            <div className="flex justify-between text-xs font-semibold">
              <span className="text-primary flex items-center gap-2">
                <RefreshCw className="h-3.5 w-3.5 animate-spin" />
                {statusText || t('update.updating')}
              </span>
              <span className="text-white font-mono">{progress}%</span>
            </div>
            <div className="h-3 w-full overflow-hidden rounded-full bg-white/10 p-0.5 border border-white/10">
              <div
                className="h-full rounded-full bg-gradient-to-r from-primary via-accent to-primary transition-all duration-300 shadow-lg shadow-primary/50"
                style={{ width: `${progress}%` }}
              />
            </div>
            <p className="text-center text-xs text-muted-foreground">
              {t('update.restartNote')}
            </p>
          </div>
        ) : (
          /* Butonlar */
          <div className="flex items-center justify-end gap-3 pt-2">
            {!updateInfo.mandatory && (
              <button
                onClick={() => setIsOpen(false)}
                className="rounded-xl px-5 py-2.5 text-sm font-semibold text-muted-foreground hover:bg-white/10 hover:text-white transition-colors"
              >
                {t('update.remindLater')}
              </button>
            )}
            <button
              onClick={handleStartUpdate}
              className="flex items-center gap-2 rounded-xl bg-gradient-to-r from-primary to-accent px-6 py-2.5 text-sm font-bold text-white shadow-lg shadow-primary/30 hover:brightness-110 active:scale-95 transition-all"
            >
              <Download className="h-4 w-4" />
              {t('update.updateNow')}
            </button>
          </div>
        )}
      </div>
    </div>
  )
}

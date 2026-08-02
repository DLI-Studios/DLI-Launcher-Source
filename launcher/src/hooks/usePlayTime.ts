import { useState, useEffect } from 'react'

const ACCUM_KEY = 'dli_launcher_play_time'
const START_KEY = 'dli_launcher_start'

function readInt(key: string, fallback: number): number {
  const raw = localStorage.getItem(key)
  const n = raw ? parseInt(raw, 10) : NaN
  return Number.isFinite(n) ? n : fallback
}

// Total banked from previous sessions (never reset on restart)
function accumulated(): number {
  return readInt(ACCUM_KEY, 0)
}

function sessionStart(): number {
  const s = readInt(START_KEY, 0)
  if (s > 0) return s
  const now = Date.now()
  localStorage.setItem(START_KEY, now.toString())
  return now
}

// Add the current session's elapsed time to the total and reset the session
// marker, so closed time is never counted and nothing is lost on restart.
function bankSession() {
  const start = readInt(START_KEY, 0)
  if (start <= 0) return
  const elapsed = Math.max(0, Date.now() - start)
  if (elapsed > 0) {
    localStorage.setItem(ACCUM_KEY, String(accumulated() + elapsed))
  }
  localStorage.setItem(START_KEY, Date.now().toString())
}

// Bank on close / tab-hidden regardless of which page is mounted
window.addEventListener('beforeunload', bankSession)
window.addEventListener('pagehide', bankSession)
document.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'hidden') bankSession()
})

function totalMs(): number {
  return accumulated() + Math.max(0, Date.now() - sessionStart())
}

function formatPlayTime(ms: number): string {
  const totalSeconds = Math.floor(ms / 1000)
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60

  if (hours > 0) {
    return `${hours}h ${minutes}m`
  }
  if (minutes > 0) {
    return `${minutes}m ${seconds}s`
  }
  return `${seconds}s`
}

export function usePlayTime() {
  const [playTime, setPlayTime] = useState(() => formatPlayTime(totalMs()))

  useEffect(() => {
    const interval = setInterval(() => {
      setPlayTime(formatPlayTime(totalMs()))
    }, 1000)

    return () => clearInterval(interval)
  }, [])

  return playTime
}

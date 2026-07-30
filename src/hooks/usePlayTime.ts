import { useState, useEffect } from 'react'

function getStartTime(): number {
  const stored = localStorage.getItem('dli_launcher_start')
  if (stored) return parseInt(stored, 10)
  const now = Date.now()
  localStorage.setItem('dli_launcher_start', now.toString())
  return now
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
  const [playTime, setPlayTime] = useState(() => {
    const start = getStartTime()
    return formatPlayTime(Date.now() - start)
  })

  useEffect(() => {
    const interval = setInterval(() => {
      const start = parseInt(localStorage.getItem('dli_launcher_start') || '0', 10)
      setPlayTime(formatPlayTime(Date.now() - start))
    }, 1000)

    return () => clearInterval(interval)
  }, [])

  return playTime
}

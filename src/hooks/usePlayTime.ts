import { useState, useEffect } from 'react'

const TOTAL_KEY = 'dli_total_play_time'
const SESSION_START_KEY = 'dli_session_start'

function getSessionStart(): number {
  const stored = sessionStorage.getItem(SESSION_START_KEY)
  if (stored) return parseInt(stored, 10)
  const now = Date.now()
  sessionStorage.setItem(SESSION_START_KEY, now.toString())
  return now
}

function getPreviouslyAccumulated(): number {
  const stored = localStorage.getItem(TOTAL_KEY)
  return stored ? parseInt(stored, 10) : 0
}

function saveAccumulatedTime(ms: number) {
  localStorage.setItem(TOTAL_KEY, ms.toString())
}

function formatPlayTime(ms: number): string {
  const totalSeconds = Math.floor(ms / 1000)
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)

  if (hours > 0) {
    return `${hours}h ${minutes}m`
  }
  return `${minutes}m`
}

export function usePlayTime() {
  const [playTime, setPlayTime] = useState(() => {
    const sessionStart = getSessionStart()
    const accumulated = getPreviouslyAccumulated()
    return formatPlayTime(accumulated + (Date.now() - sessionStart))
  })

  useEffect(() => {
    const interval = setInterval(() => {
      const sessionStart = parseInt(sessionStorage.getItem(SESSION_START_KEY) || '0', 10)
      const accumulated = getPreviouslyAccumulated()
      const currentSession = Date.now() - sessionStart
      setPlayTime(formatPlayTime(accumulated + currentSession))
    }, 1000)

    // Save accumulated time every 10 seconds
    const saveInterval = setInterval(() => {
      const sessionStart = parseInt(sessionStorage.getItem(SESSION_START_KEY) || '0', 10)
      const accumulated = getPreviouslyAccumulated()
      const currentSession = Date.now() - sessionStart
      saveAccumulatedTime(accumulated + currentSession)
    }, 10000)

    // Save on page unload
    const handleUnload = () => {
      const sessionStart = parseInt(sessionStorage.getItem(SESSION_START_KEY) || '0', 10)
      const accumulated = getPreviouslyAccumulated()
      const currentSession = Date.now() - sessionStart
      saveAccumulatedTime(accumulated + currentSession)
    }
    window.addEventListener('beforeunload', handleUnload)

    return () => {
      clearInterval(interval)
      clearInterval(saveInterval)
      window.removeEventListener('beforeunload', handleUnload)
      handleUnload()
    }
  }, [])

  return playTime
}

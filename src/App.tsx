import { useState, useEffect, useCallback } from 'react'
import { DownloadBar } from '@/components/launcher/download-bar'
import { Hero } from '@/components/launcher/hero'
import { LoginPage } from '@/components/launcher/login-page'
import { NewsPanel } from '@/components/launcher/news-panel'
import { PlaySection } from '@/components/launcher/play-section'
import { ProfilePanel } from '@/components/launcher/profile-panel'
import { QuickActions } from '@/components/launcher/quick-actions'
import { SettingsPage } from '@/components/launcher/settings-page'
import { ModpacksPage } from '@/components/launcher/modpacks-page'
import { ProfilePage } from '@/components/launcher/profile-page'
import { Sidebar } from '@/components/launcher/sidebar'
import { VersionsPage } from '@/components/launcher/versions-page'
import { UpdateModal } from '@/components/launcher/update-modal'
import { authService, type DiscordUser } from '@/services/authService'

export default function App() {
  const [user, setUser] = useState<DiscordUser | null>(authService.getUser())
  const [showLogin, setShowLogin] = useState(!authService.getUser())
  const [transitioning, setTransitioning] = useState(false)
  const [loggingOut, setLoggingOut] = useState(false)
  const [activePage, setActivePage] = useState('home')
  const [selectedVersion, setSelectedVersion] = useState('26.2')
  const [downloading, setDownloading] = useState(false)
  const [downloadVersion, setDownloadVersion] = useState('')
  const [downloadSizeMb, setDownloadSizeMb] = useState(350)

  useEffect(() => {
    const unsubscribe = authService.onAuthChange((newUser) => {
      if (newUser && !user) {
        setTransitioning(true)
        setTimeout(() => {
          setUser(newUser)
          setShowLogin(false)
          setTimeout(() => setTransitioning(false), 50)
        }, 500)
      } else if (!newUser) {
        setUser(null)
        setShowLogin(true)
      } else {
        setUser(newUser)
      }
    })
    return unsubscribe
  }, [user])

  const handleLogout = useCallback(() => {
    setLoggingOut(true)
    setTimeout(() => {
      authService.logout()
      setLoggingOut(false)
    }, 600)
  }, [])

  const handleLaunch = (version: string, sizeMb: number) => {
    setDownloadVersion(version)
    setDownloadSizeMb(sizeMb)
    setDownloading(true)
  }

  const handleVersionSelect = (version: string) => {
    setSelectedVersion(version)
    setActivePage('play')
  }

  const renderContent = () => {
    switch (activePage) {
      case 'versions':
        return <VersionsPage onSelect={handleVersionSelect} selectedVersion={selectedVersion} />
      case 'settings':
        return <SettingsPage />
      case 'modpacks':
        return <ModpacksPage />
      case 'profile':
        return <ProfilePage onLogout={handleLogout} />
      case 'play':
        return <PlaySection onLaunch={handleLaunch} selectedVersion={selectedVersion} />
      default:
        return <Hero />
    }
  }

  return (
    <div className="relative h-screen w-screen overflow-hidden bg-background">
      {/* Otomatik Guncelleme Modali */}
      <UpdateModal />

      {/* Login sayfasi */}
      <div
        className="absolute inset-0 z-20 transition-all duration-700 ease-in-out"
        style={{
          opacity: transitioning ? 0 : showLogin && !loggingOut ? 1 : 0,
          transform: transitioning ? 'scale(1.05)' : showLogin ? 'scale(1)' : 'scale(0.95)',
          pointerEvents: transitioning || !showLogin ? 'none' : 'auto',
        }}
      >
        {showLogin && <LoginPage />}
      </div>

      {/* Ana launcher sayfasi */}
      <main
        className="absolute inset-0 z-10 flex transition-all duration-600 ease-out"
        style={{
          opacity: user && !transitioning && !loggingOut ? 1 : 0,
          transform: user && !transitioning && !loggingOut ? 'translateY(0)' : 'translateY(10px)',
          pointerEvents: loggingOut ? 'none' : 'auto',
        }}
      >
        <Sidebar activePage={activePage} onNavigate={setActivePage} onLogout={handleLogout} />
        <div className="flex min-w-0 flex-1 flex-col">
          <div className="flex min-h-0 flex-1">
            <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
              {renderContent()}
            </div>
            <div className="flex w-80 shrink-0 flex-col border-l border-border bg-background/60">
              <div className="flex min-h-0 flex-1 flex-col gap-3 px-4 pb-4">
                <ProfilePanel onLogout={handleLogout} />
                <NewsPanel />
                <QuickActions />
              </div>
            </div>
          </div>
          {downloading && (
            <div className="shrink-0 border-t border-border px-4 py-3">
              <DownloadBar version={downloadVersion} totalMb={downloadSizeMb} onComplete={() => setDownloading(false)} />
            </div>
          )}
        </div>
      </main>
    </div>
  )
}

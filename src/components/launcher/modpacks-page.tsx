import { FolderOpen, RefreshCw, Package, ExternalLink } from 'lucide-react'
import { launcherBridge } from '@/services/launcherBridge'

export function ModpacksPage() {
  const openModsFolder = () => {
    launcherBridge.send('OPEN_MODS_FOLDER')
  }

  return (
    <div className="flex flex-col h-full p-6 gap-5 overflow-y-auto">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex size-10 items-center justify-center rounded-xl bg-primary/15">
            <Package className="size-5 text-primary" />
          </div>
          <div>
            <h1 className="text-2xl font-black tracking-widest text-foreground uppercase">Modpacks</h1>
            <p className="text-sm text-muted-foreground">Manage your Minecraft mods folder</p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={openModsFolder}
            className="flex items-center gap-2 rounded-xl border border-primary/40 bg-primary/10 px-4 py-2 text-xs font-semibold text-primary transition-all hover:bg-primary/20 hover:border-primary/60"
          >
            <FolderOpen className="size-3.5" />
            Open Mods Folder
          </button>
          <button
            type="button"
            onClick={openModsFolder}
            className="flex items-center gap-2 rounded-xl border border-border bg-card/50 px-4 py-2 text-xs font-semibold text-muted-foreground transition-colors hover:border-primary/40 hover:text-foreground"
          >
            <RefreshCw className="size-3.5" />
            Refresh
          </button>
        </div>
      </div>

      {/* Mods Folder Card */}
      <div className="rounded-xl border border-border bg-card/50 p-6">
        <div className="flex flex-col items-center gap-4 text-center">
          <div className="flex size-16 items-center justify-center rounded-2xl bg-primary/10">
            <FolderOpen className="size-8 text-primary" />
          </div>
          <div>
            <h3 className="text-lg font-bold text-foreground">Mods Folder</h3>
            <p className="text-sm text-muted-foreground mt-1">
              Click the button below to open your Minecraft mods folder.
              <br />
              Drop your .jar mod files here to install them.
            </p>
          </div>
          <button
            type="button"
            onClick={openModsFolder}
            className="flex items-center gap-2 rounded-xl bg-primary px-6 py-3 text-sm font-bold text-white transition-all hover:brightness-110 active:scale-[0.98]"
          >
            <ExternalLink className="size-4" />
            Open %appdata%/.minecraft/mods
          </button>
        </div>
      </div>

      {/* Info */}
      <div className="rounded-xl border border-primary/15 bg-primary/5 p-4">
        <span className="text-xs font-bold text-primary mb-2 block">How to Install Mods</span>
        <ul className="space-y-1.5 text-[11px] text-muted-foreground leading-relaxed">
          <li className="flex items-start gap-2">
            <span className="text-primary mt-0.5">1.</span>
            <span>Download mod .jar files from CurseForge or Modrinth</span>
          </li>
          <li className="flex items-start gap-2">
            <span className="text-primary mt-0.5">2.</span>
            <span>Click "Open Mods Folder" above</span>
          </li>
          <li className="flex items-start gap-2">
            <span className="text-primary mt-0.5">3.</span>
            <span>Drag and drop the .jar files into the folder</span>
          </li>
          <li className="flex items-start gap-2">
            <span className="text-primary mt-0.5">4.</span>
            <span>Launch Minecraft - mods will load automatically</span>
          </li>
        </ul>
      </div>
    </div>
  )
}

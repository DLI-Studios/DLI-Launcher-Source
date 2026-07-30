<div align="center">
  <br/>
  <h1>
    <span style="font-size:72px;font-weight:900;font-style:italic;background:linear-gradient(135deg,#c084fc,#a855f7);-webkit-background-clip:text;-webkit-text-fill-color:transparent;">DLI</span>
    <span style="font-size:28px;font-weight:300;color:#888;"> Launcher</span>
  </h1>
  <p><strong>A modern Minecraft launcher — React 18 · TypeScript · Vite 6 · Tailwind CSS 3 · C# WebView2</strong></p>
  <br/>
</div>

---

## ✦ Features

| | |
|---|---|
| **Dual Authentication** | Discord OAuth (PKCE) & Firebase Email/Password |
| **Profile System** | DLI Coins, Gems, Play Time, Achievements |
| **Custom Avatars** | Discord CDN for Discord users, base64 upload for Email users |
| **Minecraft Launcher** | Version selection, auto-download, RAM config via CmlLib |
| **Self-Updating** | Automatic update check on startup with progress bar |
| **Single Instance** | Mutex-based — only one window, like Valorant |
| **Dark Citadel UI** | OKLCH palette · glassmorphism · Tailwind dark mode |

## ✦ Tech Stack

```
Frontend  →  React 18 · TypeScript · Vite 6 · Tailwind CSS 3 · Lucide Icons
Desktop   →  C# WPF · WebView2 · .NET 9
Auth      →  Discord OAuth (PKCE) · Firebase Auth
Minecraft →  CmlLib.Core (version management & launch)
Storage   →  Firebase Storage (not used yet) · localStorage (avatars)
```

## ✦ Project Structure

```
src/
├── components/launcher/
│   ├── download-bar.tsx      # Download progress bar
│   ├── hero.tsx              # Home page hero
│   ├── login-page.tsx        # Discord + Email auth forms
│   ├── modpacks-page.tsx     # Modpack browser
│   ├── news-panel.tsx        # News feed panel
│   ├── play-section.tsx      # Game launch section
│   ├── profile-page.tsx      # Full profile w/ avatar upload
│   ├── profile-panel.tsx     # Sidebar profile card
│   ├── quick-actions.tsx     # Social & quick action buttons
│   ├── settings-page.tsx     # App settings
│   ├── sidebar.tsx           # Left navigation sidebar
│   ├── titlebar.tsx          # Top bar w/ avatar + window controls
│   ├── update-modal.tsx      # Update notification popup
│   └── versions-page.tsx     # Minecraft version list
├── services/
│   ├── authService.ts        # Discord OAuth + Firebase auth
│   ├── launcherBridge.ts     # IPC bridge to C# host
│   ├── playerService.ts      # Player data & avatar management
│   ├── updateService.ts      # Update download simulation
│   └── versionService.ts     # Version utilities
├── hooks/
│   └── usePlayTime.ts        # Play timer hook
└── lib/
    ├── firebase.ts           # Firebase config
    └── utils.ts              # Shared utilities (cn)
```

## ✦ Development

```bash
# Install dependencies
npm install

# Start Vite dev server
npm run dev

# Build for production
npm run build
```

The built output (`dist/`) is served by the C# WebView2 host from a `DLI-Launcher` folder next to the EXE. The C# project is at [`DLI-Launcher-App`](https://github.com/DLI-Studios/DLI-Launcher-Source/tree/master/DLI-Launcher-App).

## ✦ Architecture

```
┌─────────────────────────────────────────────────────┐
│                  C# WPF Application                  │
│  ┌───────────────────────────────────────────────┐  │
│  │           WebView2 (Chromium)                  │  │
│  │  ┌─────────────────────────────────────────┐  │  │
│  │  │     React App (Vite-built SPA)          │  │  │
│  │  │  · Auth UI / Profile / Settings         │  │  │
│  │  │  · Minecraft version browser            │  │  │
│  │  │  · Avatar upload & preview              │  │  │
│  │  └──────────────────┬──────────────────────┘  │  │
│  │                     │ chrome.webview.postMessage│  │
│  │  ┌──────────────────▼──────────────────────┐  │  │
│  │  │     launcherBridge.ts (IPC)             │  │  │
│  │  │  · LAUNCH_GAME · DISCORD_LOGIN          │  │  │
│  │  │  · CHECK_FOR_UPDATES · MINIMIZE/CLOSE   │  │  │
│  │  └─────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │     C# Backend                                 │  │
│  │  · OAuth PKCE flow                             │  │
│  │  · CmlLib Minecraft launcher                   │  │
│  │  · Self-update (ZIP download + batch)          │  │
│  │  · Mutex single-instance                       │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

## ✦ License

Proprietary — DLI Studios

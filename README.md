# DLI Launcher

A modern Minecraft launcher built with React 18, TypeScript, Vite 6, and Tailwind CSS 3, running inside a C# WPF WebView2 host.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 18 + TypeScript + Vite 6 + Tailwind CSS 3 |
| Desktop Host | C# WPF + WebView2 |
| Auth | Discord OAuth (PKCE) + Firebase Email/Password |
| Minecraft | CmlLib.Core for version management & launching |

## Features

- **Dual Authentication** — Sign in with Discord or Email/Password
- **Profile System** — DLI Coins, Gems, Play Time, Achievements
- **Avatar Upload** — Discord users see their Discord avatar; Email users can upload custom PNG avatars (stored locally as base64)
- **Minecraft Launcher** — Version selection, automatic download, RAM configuration
- **Self-Updating** — Built-in update checker with progress tracking
- **Single Instance** — Only one launcher window can run at a time

## Project Structure

```
src/
├── components/
│   └── launcher/
│       ├── download-bar.tsx    # Download progress bar
│       ├── hero.tsx            # Home page hero
│       ├── login-page.tsx      # Auth pages (Discord + Email)
│       ├── modpacks-page.tsx   # Modpack browser
│       ├── news-panel.tsx      # News feed
│       ├── play-section.tsx    # Game launch section
│       ├── profile-page.tsx    # Full profile page
│       ├── profile-panel.tsx   # Sidebar profile card
│       ├── quick-actions.tsx   # Quick action buttons
│       ├── settings-page.tsx   # Settings page
│       ├── sidebar.tsx         # Left navigation
│       ├── titlebar.tsx        # Top bar with user info
│       ├── update-modal.tsx    # Update notification
│       └── versions-page.tsx   # Minecraft version list
├── services/
│   ├── authService.ts          # Discord + Firebase auth
│   ├── launcherBridge.ts       # IPC to C# host
│   ├── playerService.ts        # Player data & avatar
│   ├── updateService.ts        # Update management
│   └── versionService.ts       # Version utilities
├── hooks/
│   └── usePlayTime.ts          # Play time hook
└── lib/
    ├── firebase.ts             # Firebase config
    └── utils.ts                # Shared utilities
```

## Development

```bash
# Install dependencies
npm install

# Start dev server
npm run dev

# Build for production
npm run build
```

The built frontend is served by the C# WebView2 host (`DLI-Launcher-App`). Development uses Vite's dev server on a standard port.

<div align="center">

<img src="assets/logo.png" width="120">

# DLI Launcher

### Premium Minecraft Launcher built for the DLI Gaming Platform.

Modern • Fast • Secure • Beautiful

![Platform](https://img.shields.io/badge/Platform-Windows-7c3aed?style=for-the-badge)
![Framework](https://img.shields.io/badge/.NET-9-512BD4?style=for-the-badge)
![React](https://img.shields.io/badge/React-18-61DAFB?style=for-the-badge)
![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?style=for-the-badge)
![License](https://img.shields.io/badge/License-Proprietary-111827?style=for-the-badge)

---

</div>

# ✨ About

DLI Launcher is a premium desktop launcher designed for the **DLI Gaming Platform**.

Unlike traditional Minecraft launchers, DLI focuses on creating a complete ecosystem including authentication, launcher services, client integration, cosmetics, profiles and future online services.

---

# 🖥 Preview

> Screenshots coming soon.

| Home | Profile |
|------|---------|
| Image | Image |

---

# 🚀 Features

## Authentication

- Discord OAuth (PKCE)
- Firebase Email & Password
- Secure Login Flow

## Launcher

- Automatic Minecraft installation
- Version Manager
- RAM Configuration
- Auto Update
- Download Progress
- Single Instance Protection

## Profile

- Custom Avatar
- DLI Coins
- Gems
- Play Time
- Achievements

## Future

- Friends
- Cosmetics
- Badges
- Cloud Profiles
- Cape System
- DLI Client Integration

---

# 🏗 Architecture

```
React + TypeScript
        │
        ▼
WebView2
        │
        ▼
launcherBridge (IPC)
        │
        ▼
C# Backend
        │
        ▼
Minecraft (CmlLib)
```

---

# ⚙ Tech Stack

| Layer | Technology |
|--------|------------|
| Frontend | React 18 |
| Language | TypeScript |
| Desktop | C# WPF |
| Runtime | .NET 9 |
| Browser | WebView2 |
| Launcher | CmlLib.Core |
| Authentication | Discord OAuth + Firebase |
| Styling | Tailwind CSS |
| Icons | Lucide |

---

# 📂 Project Structure

```
src/
 ├── components/
 ├── hooks/
 ├── services/
 ├── lib/
 └── assets/

DLI-Launcher-App/
```

---

# 🛠 Development

```bash
npm install
npm run dev
```

Build

```bash
npm run build
```

---

# 🎯 Roadmap

- [x] Authentication
- [x] Launcher UI
- [x] Minecraft Launch
- [x] Auto Update
- [x] Custom Avatar

- [ ] DLI Client
- [ ] Friends
- [ ] Cosmetics
- [ ] Badge System
- [ ] Cloud Save
- [ ] Marketplace

---

# 🔒 License

This project is proprietary.

Copyright © DLI Studios.

All Rights Reserved.

---

<div align="center">

Built with ❤️ by DLI Studios

</div>

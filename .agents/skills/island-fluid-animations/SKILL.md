---
name: island-fluid-animations
description: Design system, spring physics, and fluid morphing transitions for Apple-style Dynamic Island states on Windows.
---

# Island Fluid Animations & State Machine

## 1. Island States
- **Idle / Minimal Pill**: Compact black pill (`width: 130px`, `height: 32px`, `border-radius: 20px`).
- **Compact Leading/Trailing**: Compact with left/right icons (e.g. mini visualizer on left, album thumbnail on right).
- **Expanded Card**: Expanded view on hover or click (`width: 380px`, `height: 160px-220px`).
- **Notification Banner**: Dynamic expansion for quick alerts (charging, volume change, caps lock, timer).

## 2. Spring Physics & CSS Transitions
- Use cubic-bezier curves that mimic iOS spring physics:
  - Spring Open: `cubic-bezier(0.175, 0.885, 0.32, 1.275)` or custom spring curves `cubic-bezier(0.34, 1.56, 0.64, 1)`.
  - Spring Close: `cubic-bezier(0.4, 0, 0.2, 1)`.
- Fluid width/height/border-radius morphing in sync.
- Blur/Glow: Dark glassmorphism (`background: rgba(10, 10, 10, 0.85)`, `backdrop-filter: blur(24px)`, subtle 1px border `rgba(255, 255, 255, 0.12)`).

---
version: 1.0-mobile-app
name: WasteWise AR Design System
platform: Unity 6 URP / Android Mobile (Portrait 1080x1920)
description: Design System UI/UX berbasis Mobile Application (Clean, Eco-Modern, Educational App) untuk aplikasi WasteWise AR. Fokus mengubah visual dari gaya "Game AR" menjadi "Aplikasi Edukasi Modern".

colors:
  primary: "#1f8a5a"          # Emerald Green (Warna utama aplikasi / Eco theme)
  primary-active: "#156340"   # Dark Emerald (Pressed state)
  primary-light: "#e8f5e9"    # Mint Soft (Latar highlight/badge)
  
  # Kategori Sampah
  cat-organik: "#2e7d32"      # Green
  cat-anorganik: "#f57f17"    # Amber/Yellow
  cat-b3: "#c62828"           # Red

  # Neutral / Surface (App Canvas)
  bg-app: "#0f172a"           # Slate Dark Blue (Latar belakang utama aplikasi)
  surface-card: "#1e293b"     # Slate Card View (Solid & Rapi)
  surface-overlay: "#0f172a"  # Overlay Solid (Alpha 0.95 - Penutup menu belakang)
  
  # Text
  text-main: "#f8fafc"        # Off-White / High Contrast Header
  text-sub: "#94a3b8"         # Cool Gray / Subtitle & Body
  text-muted: "#64748b"       # Dark Gray / Caption
  text-on-primary: "#ffffff"  # White text on buttons

typography:
  header-lg:
    fontSize: 28
    fontWeight: Bold
    color: "{colors.text-main}"
  header-md:
    fontSize: 22
    fontWeight: Bold
    color: "{colors.text-main}"
  body-md:
    fontSize: 15
    fontWeight: Regular
    color: "{colors.text-sub}"
  caption:
    fontSize: 12
    fontWeight: Medium
    color: "{colors.text-muted}"
  button:
    fontSize: 16
    fontWeight: SemiBold
    color: "{colors.text-on-primary}"

components:
  # UI Application Components (2D Canvas Overlay)
  app-button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.text-on-primary}"
    cornerRadius: 12px
    height: 52px
    shadow: "0px 4px 12px rgba(0,0,0,0.15)"

  app-card-item:
    backgroundColor: "{colors.surface-card}"
    cornerRadius: 16px
    padding: 16px
    border: "1px solid #334155"

  app-popover-panel:
    backgroundColor: "{colors.surface-overlay}"
    padding: 24px
    cornerRadius: 20px

  # AR HUD & Feedback Overlay
  ar-top-bar:
    backgroundColor: "{colors.surface-card}"
    height: 64px
    padding: 12px 20px
    cornerRadius: 12px

  ar-feedback-toast:
    backgroundColorCorrect: "#1e293b"
    borderColorCorrect: "{colors.cat-organik}"
    backgroundColorWrong: "#1e293b"
    borderColorWrong: "{colors.cat-b3}"
    cornerRadius: 16px
    padding: 16px
    animation: "SlideInFromBottom"

  # World-Space AR Labels
  ar-world-label:
    type: "3D TextMeshPro + Billboard"
    fontSize: 3.5
    fontWeight: Bold
    alignment: Center
---

## Overview

Design System ini mentransformasi antarmuka **WasteWise AR** dari bentuk "Demo Game AR" menjadi **Aplikasi Edukasi Mobile Modern** (Eco-App). 

### Prinsip Utama UI/UX:
1. **Latar Belakang Solid & Berhierarki (Card View):** Menggunakan nuansa *Dark Slate* (`#0f172a`) dengan kartu berbiku tumpul (`#1e293b` + `16px radius`). Pop-up panel bersifat solid penuh untuk mencegah pembayangan teks menu belakang.
2. **Karakter Aplikasi Edukasi (Eco-Emerald Theme):** Warna aksi didominasi oleh *Emerald Green* (`#1f8a5a`), dipadukan dengan aksen warna kategorikal yang tegas untuk Organik (Hijau), Anorganik (Kuning), dan B3 (Merah).
3. **UX Feedback Berbasis Bottom Sheet / Toast Notification:** Notifikasi "BENAR / SALAH" di `ARScene` tidak lagi menutup seluruh layar seperti game over, melainkan berupa *Toast Notification* bersih yang muncul dari bawah layar dengan batas warna indikator yang jelas.
4. **Tipografi Bersih & Kontras Tinggi:** Menggunakan hirarki TextMeshPro yang konsisten antara *Header Bold*, *Body Regular*, dan *Caption*.
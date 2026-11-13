# WasteWise AR

Aplikasi edukasi pemilahan sampah rumah tangga berbasis **Augmented Reality**. Pengguna menempatkan tong sampah virtual di atas lantai nyata melalui kamera, lalu memilah objek sampah 3D ke kategori **Organik**, **Anorganik**, atau **B3** dengan umpan balik skor secara langsung.

Dibangun dengan Flutter untuk antarmuka dan Unity untuk pengalaman AR, dihubungkan oleh `flutter_unity_widget` melalui bridge dua arah.

## Fitur Utama

- **Deteksi Lantai ARCore** — bidang lantai dideteksi otomatis sehingga tong sampah virtual dapat ditempatkan pada permukaan nyata.
- **3 Tong Sampah Kategori** — Organik, Anorganik, dan B3, masing-masing dengan warna, label, dan model 3D sendiri. Tong sampah otomatis menghadap kamera (*auto-facing camera*) saat muncul.
- **15 Objek 3D Interaktif** — sampah rumah tangga ditampilkan sebagai model 3D yang dapat diseret. Urutan kemunculan diacak unik per sesi (*unique random shuffle*), sehingga setiap latihan terasa berbeda.
- **Sistem Skor Real-Time** — HUD Flutter menampilkan skor dan progres sesi saat jawaban dievaluasi (+10 untuk pemilahan benar, -5 untuk salah).
- **Panel Hasil Akhir** — skor akhir dan rekor terbaik ditampilkan pada akhir sesi, lengkap dengan tombol kembali ke menu.
- **Katalog & Panduan Kategori** — katalog material dan halaman informasi kategori dengan tampilan **Material Design 3**, termasuk animasi splash dan penyimpanan rekor terbaik di perangkat.

## Arsitektur & Tech Stack

| Lapisan | Teknologi |
| --- | --- |
| Antarmuka & navigasi | Flutter 3 + Dart, Material Design 3 |
| Scene AR & fisika | Unity 6, AR Foundation, ARCore XR Plugin |
| Jembatan Flutter–Unity | `flutter_unity_widget` (embedding platform view + pesan JSON dua arah) |
| Target platform | Android 8.0+ (minSdk 26), ARM64 (`arm64-v8a`) |

Alur singkat:

1. Flutter membuka layar AR dan menanamkan scene Unity melalui `UnityWidget`.
2. Unity menjalankan sesi ARCore, menempatkan tong sampah, dan mengacak urutan sampah.
3. Setiap jawaban dikirim Unity ke Flutter sebagai pesan `gameUpdate`/`feedback`.
4. Flutter memperbarui HUD skor dan menampilkan panel hasil saat sesi selesai.

## Instalasi APK (Pengguna)

1. Buka tab **Releases** pada repository GitHub ini.
2. Unduh berkas `app-release.apk` dari rilis terbaru.
3. Aktifkan opsi **Instal dari sumber tidak dikenal** pada perangkat Android.
4. Buka APK dan pasang aplikasi.
5. Saat pertama kali menjalankan latihan AR, izinkan akses **kamera** dan **gerak** perangkat.

**Persyaratan:** Android 8.0 (API 26) atau lebih baru, perangkat dengan dukungan ARCore, dan ruang penyimpanan yang cukup.

## Build Lokal (Pengembang)

### Prasyarat

- Flutter SDK 3.x dan Dart
- Unity 6 dengan modul **Android Build Support**, AR Foundation, dan ARCore XR Plugin
- Android SDK, JDK 17 (Android Studio JBR)
- Perangkat/emulator Android dengan dukungan ARCore

### Langkah Build

1. Clone repository, lalu buka root proyek di Unity Editor.
2. Pastikan scene AR dan pengaturan XR sudah termuat, kemudian jalankan menu:
   `WasteWise > Export to Flutter Android Project`
   untuk menyegarkan modul `wastewise_flutter/android/unityLibrary/`.
3. Siapkan dependensi Flutter:
   ```bash
   cd wastewise_flutter
   flutter pub get
   ```
4. Buat keystore rilis lokal Anda dan daftarkan pada `wastewise_flutter/android/key.properties`:
   ```properties
   storePassword=<password-anda>
   keyPassword=<password-anda>
   keyAlias=upload
   storeFile=upload-keystore.jks
   ```
   Letakkan `upload-keystore.jks` di `wastewise_flutter/android/app/`. Berkas keystore dan `key.properties` **tidak boleh** dikomit ke Git.
5. Build APK rilis:
   ```bash
   flutter build apk --release --target-platform android-arm64
   ```
6. APK dihasilkan di:
   ```
   wastewise_flutter/build/app/outputs/flutter-apk/app-release.apk
   ```

Konfigurasi rilis menonaktifkan `minifyEnabled` dan `shrinkResources` agar refleksi bridge Unity–Flutter tidak terhapus oleh R8.

## Struktur Proyek

```
WasteWiseAR/
├── Assets/                     # Scene, skrip C#, prefab, dan model 3D Unity
├── ProjectSettings/            # Konfigurasi build Unity (ARCore, graphics API)
├── wastewise_flutter/          # Aplikasi Flutter
│   ├── lib/                    # UI, state, dan layar aplikasi
│   ├── android/                # Proyek Android + unityLibrary hasil ekspor
│   └── packages/               # Paket flutter_unity_widget lokal
├── AUDIT.md                    # Catatan audit dan perbaikan
└── README.md
```

## Pengembang

**Yosua Arimon Lende**
Teknik Informatika — Universitas Dhyana Pura

GitHub: [@arimon17](https://github.com/arimon17)

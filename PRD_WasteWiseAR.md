# PRD: WasteWise AR - Media Edukasi Pemilahan Sampah Rumah Tangga

## 1. Tujuan dan Masalah
Mayoritas mahasiswa telah memiliki pengetahuan umum yang baik mengenai jenis sampah, namun sebagian besar masih jarang memilah sampah secara konsisten di lingkungan sehari-hari karena keterbatasan sarana dan minimnya sarana latihan atau pembiasaan langsung (Azka dkk., 2021). Di tingkat nasional, timbulan sampah mencapai 31,9 juta ton dengan 35,67% di antaranya belum terkelola (Tamam dkk., 2024). Aplikasi "WasteWise AR" bertujuan menyediakan media simulasi praktis berbasis Android AR untuk melatih dan membiasakan mahasiswa serta anak kos dalam memilah sampah rumah tangga secara mandiri melalui latihan interaktif. *(Persentase nasional lain: perlu verifikasi)*.

## 2. Pembeda Aplikasi
WasteWise AR memadukan simulasi interaktif yang menampilkan sampah virtual dan tempat sampah virtual secara langsung di atas lantai nyata menggunakan teknologi plane detection.

## 3. Fitur Inti (Tepat 5 Fitur)
1. **Daftar Sampah Rumah Tangga:** Katalog UI 2D memuat 20-30 jenis sampah rumah tangga yang dikelompokkan ke dalam 3 kategori (Organik, Anorganik, B3) [PERLU SUMBER RESMI untuk penetapan detail daftar 20-30 jenis sampah, mengacu standar SIPSN/KLHK]. Daftar ini hanya katalog informasi; sesi latihan AR (Fitur 3) memakai 6-8 jenis yang dipilih acak dari daftar ini.
2. **Tempat Sampah 3D AR (Plane Detection):** Mengidentifikasi permukaan lantai mendatar (horizontal plane) untuk menempatkan 3 objek tempat sampah 3D (Organik - Hijau, Anorganik - Kuning, B3 - Merah) [PERLU SUMBER RESMI untuk warna].
3. **Interaksi Pemilahan & Feedback:** Menampilkan 6-8 jenis sampah virtual dalam bentuk 3D sederhana bawaan Unity (Cube/Cylinder) yang diberi warna dan teks nama objek. Interaksi tahap awal: tap pada objek sampah lalu tap pada tempat sampah tujuan (drag-and-drop hanya jika sempat). Sistem memberikan respons visual berupa teks umpan balik (Benar/Salah) beserta penjelasan singkat edukatif, tanpa respons audio. Contoh jenis sampah yang realistis untuk anak kos: bungkus mi instan, botol plastik, kulit pisang, baterai bekas, kardus, dan kaleng. Daftar final dan kategorinya menunggu sumber resmi.
4. **Info Kategori & Tips Pengelolaan:** Tampilan UI teks statis memuat deskripsi tiap kategori sampah (Organik, Anorganik, B3) dan tips praktis pengelolaan/daur ulang rumah tangga.
5. **Skor Sederhana Lokal:** Menghitung jumlah pemilahan benar dan salah selama sesi latihan serta menyimpan rekor skor terbaik secara lokal di perangkat (`PlayerPrefs`).

### Kalau Sempat (bukan fitur inti)
- **Drag-and-drop:** memindahkan sampah virtual ke tempat sampah dengan menyeret objek.
- **Mode belajar:** pengguna memilih jenis sampah dari katalog, lalu AR menyorot tempat sampah dengan warna yang benar.

## 4. Alur Layar
Aplikasi memakai dua scene: `MenuScene` (UI 2D saja, kamera AR tidak aktif) dan `ARScene` (AR Session dan XR Origin, kamera belakang).
- (a) Menu berisi **Mulai Latihan**, **Katalog Sampah**, **Info Kategori**, dan skor terbaik.
- (b) Katalog (Fitur 1) dan Info (Fitur 4) berupa layar teks statis.
- (c) **Mulai Latihan** membuka `ARScene`; kamera belakang menyala dengan instruksi "Arahkan kamera ke lantai".
- (d) Setelah lantai terdeteksi, pengguna tap untuk menaruh 3 tempat sampah virtual.
- (e) Sampah virtual muncul acak dari 6-8 jenis.
- (f) Pengguna tap sampah lalu tap tempat sampah, lalu muncul umpan balik benar/salah dan penjelasan singkat.
- (g) Di akhir sesi tampil skor dan rekor, dengan tombol kembali ke menu.

Tidak ada pemindaian atau pengenalan sampah nyata lewat kamera.

## 5. Aturan UI dan Teks
- Kamera yang dipakai adalah kamera belakang.
- Semua teks panjang (umpan balik, penjelasan, skor, instruksi, nama sampah yang dipilih) memakai Canvas Screen Space - Overlay dengan TextMeshPro.
- Hanya label kategori pendek (ORGANIK, ANORGANIK, B3) pada tempat sampah yang memakai TextMeshPro world-space dengan skrip billboard (selalu menghadap kamera).
- Umpan balik pada objek berupa tempat sampah berkedip hijau (benar) atau merah (salah) dengan mengganti warna material.

## 6. Daftar Yang TIDAK Dikerjakan (Out of Scope)
- Klasifikasi sampah otomatis via kamera/AI/Machine Learning.
- Pemindaian atau pengenalan sampah nyata lewat kamera, termasuk objek apa pun di atas bidang datar.
- Pemindaian QR Code / Barcode.
- Sistem Login, Registrasi, atau Otentikasi Akun.
- Backend server, API external, atau Database Online.
- Statistik analitik kompleks atau histori riwayat panjang.
- Fitur multiplayer, fitur sosial, atau papan peringkat global.

## 7. Stack dan Versi Perangkat Lunak
- **Unity Engine:** Unity 6.3 LTS (6000.3.27f1), modul Android Build Support terpasang.
- **AR Framework:** AR Foundation 6.3.5 dan Google ARCore XR Plugin 6.3.5 (dibaca dari Package Manager pada proyek, 2026-10-29).
- **Catatan versi:** AR Foundation 6 berbeda dari versi 4 dan 5. `XR Origin` menggantikan `AR Session Origin`. Jangan memakai contoh kode atau tutorial versi lama.
- **Bahasa Pemrograman:** C#.
- **Konfigurasi Android:** Minimum API Level 24 (syarat minimum ARCore); jika Unity atau plugin menampilkan peringatan nilai minimum yang lebih tinggi, ikuti nilai itu. Target API Level: otomatis (highest installed). Scripting Backend IL2CPP, arsitektur ARM64.
- **Perangkat uji:** Tecno Pova 7 4G (model LJ6, Android 16). ARCore terkonfirmasi berfungsi lewat demo AR web pada 2026-10-29.
- **Sumber Pengecekan:** Window > Package Manager > In Project pada proyek ini, dan https://developers.google.com/ar/devices

## 8. Aset
- Menggunakan objek 3D bentuk dasar bawaan Unity (*primitive shapes*: Cube, Cylinder, Sphere) yang diberi pewarnaan Material dan label Teks Canvas/TextMeshPro.
- Penggunaan model 3D eksternal berlisensi bebas (CC0 / Gratis) hanya akan ditambahkan jika sempat.

## 9. Struktur Folder Proyek Unity
```text
Assets/
├── Materials/
├── Prefabs/
│   ├── Bins/
│   └── TrashItems/
├── Scenes/
│   └── SampleScene.unity   (bawaan template; boleh diganti nama menjadi MainScene nanti)
├── Scripts/
│   ├── Controllers/
│   ├── Managers/
│   └── UI/
└── Textures/
```
Folder bawaan template (`XR`, `Settings`, `TutorialInfo`) tidak diubah.

## 10. Setup Scene (dikerjakan manual oleh pengguna di Unity Editor)
- Scene wajib memuat `AR Session` dan `XR Origin (Mobile AR)`. XR Origin membawa kamera AR sendiri, jadi `Main Camera` lama dihapus.
- Untuk Fitur 2, tambahkan `AR Plane Manager` dan `AR Raycast Manager` pada XR Origin. Agen akan menjelaskan komponen yang diperlukan setiap tugas.

## 11. Aturan untuk Agen AI Coding
- **Restriksi Akses:** Hanya boleh membuat, membaca, atau mengubah berkas C# di dalam folder `Assets/Scripts/`.
- **Dilarang Menyentuh:** Dilarang keras mengubah berkas `.meta`, `.unity`, `.prefab`, isi folder `ProjectSettings/`, atau konfigurasi Packages/Manifest secara langsung. Semua perubahan scene dan prefab dilakukan manual oleh pengguna di Editor.
- **Cek Versi:** Sebelum menulis kode AR, baca `Packages/manifest.json` untuk versi AR Foundation, lalu tulis kode sesuai versi itu (saat ini 6.3.5). Jangan memakai API versi lama.
- **Strategi Tugas:** Selesaikan tepat 1 fitur per tugas secara modular.
- **Instruksi Post-Task:** Setelah menyelesaikan penulisan script, berikan instruksi eksplisit mengenai GameObjects, Components, Tag, Layer, dan Serialized Fields yang harus dipasang di Unity Inspector dan Scene.

## 12. Definisi Selesai (Definition of Done)
Setiap fitur dinyatakan selesai jika script C# terbebas dari compilation error (Console Unity bersih dari error merah) dan telah diuji secara fisik melalui APK yang terinstal dan berjalan lancar pada HP Tecno Pova 7 4G (Android 16) tanpa crash.

## 13. Checkpoint Proyek & Rencana B
- **Checkpoint Minggu 1:** Proyek Unity AR Foundation kosong berhasil di-build menjadi APK, terinstal, dan dapat dibuka tanpa crash di HP Tecno Pova 7 4G.
- **Checkpoint Minggu 3:** APK buatan sendiri mampu mendeteksi lantai nyata dan memunculkan 1 objek Cube 3D di permukaan lantai via sentuhan layar.
- **Checkpoint Minggu 5:** Tiga tempat sampah 3D terpasang presisi di lantai, serta 1 sampah virtual dapat ditaruh ke tempat sampah dengan penilaian Benar/Salah yang akurat.
- **Rencana B (Fallback Mechanism):** Jika plane detection AR Foundation tidak stabil pada perangkat target, sistem AR dialihkan menggunakan AR Foundation Image Tracking dengan 1 gambar marker fisik untuk memunculkan tempat sampah 3D dan teks penjelasan.

## 14. Pemetaan Ketentuan Dosen
- **Project 1 (aplikasi yang menjawab masalah nyata):** Fungsi aplikasi berupa katalog, info kategori, dan skor. Masalahnya kurangnya latihan dan pembiasaan memilah; klaim ditulis sebagai "media latihan pemilahan", bukan "menyelesaikan masalah sampah".
- **Project 2 (aplikasi AR Android bertema edukasi):** Latihan memilah sampah virtual di lantai nyata.

## 15. Rencana Pengujian
- 10 soal pemilahan sampah sehari-hari.
- 10-15 responden mahasiswa mengerjakan soal sebelum memakai aplikasi, berlatih sekitar 10 menit, lalu mengerjakan soal lagi sesudahnya.
- Bandingkan rata-rata skor sebelum dan sesudah.
- Kuesioner kegunaan System Usability Scale (SUS).
- Hasilnya untuk bab pengujian laporan.

## 16. Status Proyek
- [x] Setup: Unity 6.3 LTS (6000.3.27f1) dengan modul Android terpasang
- [x] Setup: Proyek WasteWiseAR dibuat (AR Foundation 6.3.5, ARCore XR Plugin 6.3.5)
- [x] Setup: AR Session dan XR Origin (Mobile AR) ada di scene
- [ ] Scene: MenuScene dan ARScene
- [x] Checkpoint Minggu 1: Build APK Kosong di HP Target
- [x] Checkpoint Minggu 3: Plane Detection & Spawning Cube 3D (cube muncul di lantai, sudah diverifikasi pengguna di HP)
- [ ] Checkpoint Minggu 5: Tempat Sampah 3D, Interaksi Pemilahan & Skor
- [ ] Fitur 1: UI Daftar 20-30 Jenis Sampah
- [ ] Fitur 2: Placement 3 Tempat Sampah AR (Hijau, Kuning, Merah)
- [ ] Fitur 3: Interaksi Tap Sampah/Tempat Sampah & Feedback Teks
- [ ] Fitur 4: UI Info Kategori & Tips Pengelolaan
- [ ] Fitur 5: Sistem Skor Lokal (PlayerPrefs)
- [ ] Pengujian sebelum-sesudah dan SUS

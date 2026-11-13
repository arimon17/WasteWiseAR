---
name: wastewise-audit
description: Audit proyek WasteWise AR (Flutter + Unity) terhadap PRD_WasteWiseAR.md, tulis daftar tugas bercentang di AUDIT.md, lalu perbaiki satu per satu setelah disetujui pengguna. Gunakan saat pengguna meminta audit, cek konsistensi, cari bug, atau perbaikan terakhir sebelum rilis.
---

# Audit WasteWise AR

Skill ini punya dua fase. Fase 1 hanya membaca dan melaporkan. Fase 2 hanya berjalan setelah pengguna menyetujui daftar tugas.

## Aturan keras (berlaku di kedua fase)
- Jangan mengedit berkas `.meta`, `.unity`, `.prefab`, `ProjectSettings/`, atau `Packages/manifest.json`. Temuan yang butuh perubahan di Unity Editor atau di HP masuk bagian "Tugas manual".
- Jangan menambah fitur di luar PRD. Fitur inti tepat 5. Klasifikasi sampah lewat kamera/AI, QR, login, dan backend adalah out-of-scope; kalau ada di kode, itu temuan.
- Jangan menebak. Setiap temuan wajib punya bukti: `path:baris`, kutipan kode singkat, atau keluaran perintah. Jika tidak bisa dibuktikan, tulis di bagian "Perlu verifikasi", bukan sebagai bug.
- Isi edukasi (kategori sampah, warna tempat sampah, teks penjelasan) tidak bisa kamu verifikasi. Tandai "perlu verifikasi sumber resmi" dan jangan mengubahnya sendiri.
- Jangan memasang paket atau menjalankan perintah di luar yang dibutuhkan audit.

## Fase 1: Audit (READ-ONLY)
Satu-satunya berkas yang boleh dibuat atau diubah: `AUDIT.md` di root proyek.

1. Baca `PRD_WasteWiseAR.md` (dan `Design.md` jika ada), lalu petakan struktur proyek.
2. Jalankan pemeriksaan otomatis yang tersedia, misalnya `flutter analyze` dan `flutter test` bila ada tes. Catat hasilnya apa adanya. Jika perintah gagal atau tidak tersedia, catat itu.
3. Tinjau kode dengan checklist ini:
   - **Kepatuhan PRD:** kelima fitur ada dan sesuai definisi selesai; alur layar sesuai; tidak ada fitur out-of-scope.
   - **Konsistensi:** nama dan urutan kategori, warna tempat sampah, teks UI, key PlayerPrefs, nama scene dan nama pesan di kedua sisi jembatan Flutter-Unity, penamaan berkas dan kelas, bahasa dan nada teks, kesesuaian dengan `Design.md`.
   - **Bug umum Unity C#:** null reference, `GetComponent` atau `Find` di `Update`, listener yang tidak dilepas, tap yang tidak menangani UI, objek dibuat berulang tanpa dihancurkan, siklus hidup AR saat pindah scene, izin kamera.
   - **Bug umum Flutter/Dart:** `setState` setelah dispose, async tanpa penanganan error, navigasi dan state, izin Android.
   - **Kebersihan:** `print`/`Debug.Log` berlebihan, kode mati, TODO, nilai hardcode, berkas tak terpakai.
4. Tulis `AUDIT.md` dengan format:

```
# AUDIT
## Ringkasan
(jumlah temuan per tingkat: Tinggi / Sedang / Rendah)
## Hasil pemeriksaan otomatis
## Tugas kode (bisa dikerjakan agen)
- [ ] T-01 [Tinggi] Judul | path:baris | Bukti | Perbaikan yang diusulkan | Cara verifikasi
## Tugas manual (Unity Editor / HP)
- [ ] M-01 Judul | Langkah | Hasil yang diharapkan
## Perlu verifikasi (tidak bisa dipastikan agen)
- [ ] V-01 ...
```

5. BERHENTI. Tampilkan jumlah tugas per tingkat, lalu minta pengguna menyetujui atau mencoret tugas. Jangan masuk Fase 2 sebelum ada persetujuan eksplisit.

## Fase 2: Perbaikan (hanya setelah disetujui)
1. Jalankan `git status`. Jika working tree tidak bersih, berhenti dan tanya pengguna.
2. Kerjakan hanya tugas kode (T-xx) yang disetujui, urut Tinggi ke Rendah, satu per satu.
3. Untuk setiap tugas:
   - Ubah seminimal mungkin.
   - Jalankan cara verifikasi yang tercantum.
   - Jika lolos: ubah `- [ ]` menjadi `- [x]` di `AUDIT.md` dengan catatan singkat (apa yang diubah dan hasil verifikasi), lalu `git commit -m "audit T-xx: ringkasan"`.
   - Jika verifikasi gagal, atau perbaikan meluas melebihi berkas yang disebut: kembalikan perubahan tugas itu, tandai `- [!]` beserta alasannya, lalu lanjut ke tugas berikutnya.
4. Satu tugas satu commit. Jangan menggabungkan tugas.
5. Setelah selesai, buat ringkasan: berapa yang selesai, berapa yang `[!]`, dan tugas manual yang tersisa. Jangan menyatakan "semua beres" jika masih ada `[!]` atau tugas manual.

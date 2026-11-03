import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

enum WasteCategory {
  organik(label: 'Organik', color: AppColors.organik, icon: Icons.eco_outlined),
  anorganik(
    label: 'Anorganik',
    color: AppColors.anorganik,
    icon: Icons.recycling_outlined,
  ),
  b3(label: 'B3', color: AppColors.b3, icon: Icons.warning_amber_outlined);

  const WasteCategory({
    required this.label,
    required this.color,
    required this.icon,
  });

  final String label;
  final Color color;
  final IconData icon;
}

class WasteItem {
  const WasteItem(this.name, this.category, {required this.tip});

  final String name;
  final WasteCategory category;
  final String tip;
}

class CategoryGuide {
  const CategoryGuide({
    required this.category,
    required this.description,
    required this.tips,
  });

  final WasteCategory category;
  final String description;
  final List<String> tips;
}

const List<CategoryGuide> categoryGuides = [
  CategoryGuide(
    category: WasteCategory.organik,
    description:
        'Sampah organik berasal dari makhluk hidup dan mudah terurai secara '
        'alami. Sisa makanan, dedaunan, dan potongan buah termasuk di dalamnya.',
    tips: [
      'Pisahkan dari plastik, kaca, dan logam sebelum dibuang.',
      'Tiriskan sisa makanan agar tidak menimbulkan bau dan lindi.',
      'Olah menjadi kompos atau serahkan ke pengelola sampah organik.',
    ],
  ),
  CategoryGuide(
    category: WasteCategory.anorganik,
    description:
        'Sampah anorganik berasal dari bahan non-hayati seperti plastik, '
        'kertas, logam, dan kaca. Sebagian besar butuh waktu lama untuk terurai '
        'dan dapat didaur ulang.',
    tips: [
      'Bilas dan keringkan kemasan bekas sebelum dikumpulkan.',
      'Pilah berdasarkan jenis: plastik, kertas, logam, dan kaca.',
      'Bawa ke bank sampah atau pengepul untuk didaur ulang.',
    ],
  ),
  CategoryGuide(
    category: WasteCategory.b3,
    description:
        'Sampah B3 (bahan berbahaya dan beracun) mengandung zat yang berisiko '
        'bagi kesehatan dan lingkungan. Contohnya baterai, lampu neon, obat '
        'kedaluwarsa, dan bahan kimia rumah tangga.',
    tips: [
      'Jangan mencampur B3 dengan sampah rumah tangga biasa.',
      'Simpan dalam wadah tertutup dan jauhkan dari anak-anak.',
      'Serahkan ke titik drop-off B3 atau fasilitas pengolahan limbah.',
    ],
  ),
];

const List<WasteItem> wasteItems = [
  WasteItem(
    'Nasi sisa',
    WasteCategory.organik,
    tip: 'Tiriskan airnya, lalu olah menjadi kompos.',
  ),
  WasteItem(
    'Kulit pisang',
    WasteCategory.organik,
    tip: 'Cocok untuk kompos atau pupuk tanaman.',
  ),
  WasteItem(
    'Sisa sayur',
    WasteCategory.organik,
    tip: 'Potong kecil agar lebih cepat terurai.',
  ),
  WasteItem(
    'Ampas kopi',
    WasteCategory.organik,
    tip: 'Bisa langsung dicampur ke dalam kompos.',
  ),
  WasteItem(
    'Cangkang telur',
    WasteCategory.organik,
    tip: 'Remukkan, lalu jadikan campuran kompos.',
  ),
  WasteItem(
    'Daun kering',
    WasteCategory.organik,
    tip: 'Gunakan sebagai bahan kompos atau mulsa.',
  ),
  WasteItem(
    'Sisa buah',
    WasteCategory.organik,
    tip: 'Pisahkan dari kemasan plastiknya lebih dulu.',
  ),
  WasteItem(
    'Tulang ikan',
    WasteCategory.organik,
    tip: 'Buang terpisah, jangan dicampur dengan plastik.',
  ),
  WasteItem(
    'Botol plastik',
    WasteCategory.anorganik,
    tip: 'Bilas, lepaskan tutup, lalu remukkan.',
  ),
  WasteItem(
    'Bungkus mi instan',
    WasteCategory.anorganik,
    tip: 'Bersihkan sisa makanan sebelum dibuang.',
  ),
  WasteItem(
    'Kardus bekas',
    WasteCategory.anorganik,
    tip: 'Lipat agar hemat ruang untuk didaur ulang.',
  ),
  WasteItem(
    'Kaleng minuman',
    WasteCategory.anorganik,
    tip: 'Bilas dan pipihkan sebelum dikumpulkan.',
  ),
  WasteItem(
    'Kantong plastik',
    WasteCategory.anorganik,
    tip: 'Kumpulkan untuk didaur ulang, jangan dibakar.',
  ),
  WasteItem(
    'Gelas plastik',
    WasteCategory.anorganik,
    tip: 'Lepaskan sedotan dan bersihkan gelasnya.',
  ),
  WasteItem(
    'Koran bekas',
    WasteCategory.anorganik,
    tip: 'Simpan dalam keadaan kering dan ikat rapi.',
  ),
  WasteItem(
    'Botol kaca',
    WasteCategory.anorganik,
    tip: 'Hati-hati pecah, bungkus sebelum dibuang.',
  ),
  WasteItem(
    'Baterai bekas',
    WasteCategory.b3,
    tip: 'Simpan di wadah tertutup, jangan dibuang ke tempat sampah biasa.',
  ),
  WasteItem(
    'Lampu neon',
    WasteCategory.b3,
    tip: 'Bungkus rapat agar tidak pecah, lalu serahkan ke drop-off B3.',
  ),
  WasteItem(
    'Obat kedaluwarsa',
    WasteCategory.b3,
    tip: 'Jangan dibuang ke saluran air, ikuti drop-off B3.',
  ),
  WasteItem(
    'Kaleng aerosol',
    WasteCategory.b3,
    tip: 'Pastikan kosong, jangan ditusuk atau dibakar.',
  ),
  WasteItem(
    'Cat sisa',
    WasteCategory.b3,
    tip: 'Tutup rapat dan serahkan ke fasilitas pengolahan B3.',
  ),
  WasteItem(
    'Oli bekas',
    WasteCategory.b3,
    tip: 'Tampung di wadah tertutup, jangan ke saluran air.',
  ),
  WasteItem(
    'Termometer',
    WasteCategory.b3,
    tip: 'Mengandung merkuri, tangani sebagai limbah B3.',
  ),
  WasteItem(
    'Pemutih pakaian',
    WasteCategory.b3,
    tip: 'Simpan sisa dalam wadah asli, jangan dicampur bahan lain.',
  ),
];

List<WasteItem> wasteItemsFor(WasteCategory category) {
  return wasteItems.where((item) => item.category == category).toList();
}

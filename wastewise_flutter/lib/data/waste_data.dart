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
    'Apel',
    WasteCategory.organik,
    tip: 'Apel sisa termasuk sampah organik yang mudah terurai dan bisa diolah menjadi kompos.',
  ),
  WasteItem(
    'Kulit Pisang',
    WasteCategory.organik,
    tip: 'Kulit pisang adalah sampah organik yang kaya nutrisi untuk kompos dan pupuk tanaman.',
  ),
  WasteItem(
    'Sisa Roti',
    WasteCategory.organik,
    tip: 'Roti yang sudah basi termasuk organik; pisahkan dari kemasan plastiknya sebelum dikompos.',
  ),
  WasteItem(
    'Wortel',
    WasteCategory.organik,
    tip: 'Sisa wortel termasuk sampah organik; potong kecil agar lebih cepat terurai menjadi kompos.',
  ),
  WasteItem(
    'Tulang Ikan',
    WasteCategory.organik,
    tip: 'Tulang ikan termasuk sampah organik; buang terpisah dan jangan dicampur dengan plastik.',
  ),
  WasteItem(
    'Botol Plastik',
    WasteCategory.anorganik,
    tip: 'Botol plastik PET bernilai daur ulang; bilas, lepaskan tutup, lalu remukkan sebelum dikumpulkan.',
  ),
  WasteItem(
    'Botol Saus',
    WasteCategory.anorganik,
    tip: 'Botol saus bekas termasuk anorganik; bilas sisa sausnya lalu serahkan ke bank sampah.',
  ),
  WasteItem(
    'Kaleng Minuman',
    WasteCategory.anorganik,
    tip: 'Kaleng aluminium mudah didaur ulang dan bernilai jual; bilas lalu pipihkan.',
  ),
  WasteItem(
    'Bungkus Permen',
    WasteCategory.anorganik,
    tip: 'Bungkus permen berbahan plastik sulit terurai; kumpulkan dengan sampah anorganik.',
  ),
  WasteItem(
    'Kotak Karton',
    WasteCategory.anorganik,
    tip: 'Kotak karton minuman berlapis plastik dan aluminium; bilas, lipat, lalu keringkan sebelum didaur ulang.',
  ),
  WasteItem(
    'Baterai Bekas',
    WasteCategory.b3,
    tip: 'Baterai mengandung logam berat berbahaya; buang ke tempat sampah B3, jangan ke sampah biasa.',
  ),
  WasteItem(
    'Botol Oli Bekas',
    WasteCategory.b3,
    tip: 'Oli bekas mengandung bahan berbahaya; tampung di wadah tertutup dan serahkan ke fasilitas B3.',
  ),
  WasteItem(
    'Lampu Bekas',
    WasteCategory.b3,
    tip: 'Lampu bekas dapat mengandung merkuri; bungkus rapat dan serahkan ke titik drop-off B3.',
  ),
  WasteItem(
    'Kaleng Cat',
    WasteCategory.b3,
    tip: 'Cat sisa mengandung bahan kimia berbahaya; tutup rapat dan serahkan ke pengolahan limbah B3.',
  ),
  WasteItem(
    'Wadah Pestisida',
    WasteCategory.b3,
    tip: 'Pestisida adalah limbah B3; jangan dibuang ke saluran air, serahkan ke fasilitas pengolahan khusus.',
  ),
];

List<WasteItem> wasteItemsFor(WasteCategory category) {
  return wasteItems.where((item) => item.category == category).toList();
}

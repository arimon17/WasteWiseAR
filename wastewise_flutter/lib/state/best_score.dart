import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';

const MethodChannel _storageChannel = MethodChannel('wastewise/storage');
const String _bestScoreKey = 'best_score';

final ValueNotifier<int> bestScore = ValueNotifier<int>(0);

Future<void> loadBestScore() async {
  try {
    final stored = await _storageChannel.invokeMethod<int>('getInt', {
      'key': _bestScoreKey,
    });
    if (stored != null && stored > bestScore.value) {
      bestScore.value = stored;
    }
  } on PlatformException {
    // Penyimpanan lokal tidak tersedia; rekor memakai nilai di memori.
  } on MissingPluginException {
    // Terjadi pada lingkungan tanpa implementasi platform, mis. pengujian.
  }
}

Future<void> saveBestScore(int score) async {
  if (score <= bestScore.value) {
    return;
  }

  bestScore.value = score;

  try {
    await _storageChannel.invokeMethod<void>('setInt', {
      'key': _bestScoreKey,
      'value': score,
    });
  } on PlatformException {
    // Gagal menyimpan tidak boleh menghentikan sesi AR.
  } on MissingPluginException {
    // Lingkungan tanpa implementasi platform.
  }
}

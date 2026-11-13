import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:wastewise_flutter/screens/ar_view_screen.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('UnityWidget tidak dipasang saat izin kamera ditolak', (
    WidgetTester tester,
  ) async {
    const channel = MethodChannel('wastewise/storage');
    final messenger =
        TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger;

    messenger.setMockMethodCallHandler(channel, (call) async {
      if (call.method == 'ensureCameraPermission') {
        return false;
      }
      return null;
    });

    await tester.pumpWidget(const MaterialApp(home: ARViewScreen()));
    await tester.pump();

    expect(
      find.text('Izin kamera diperlukan untuk menampilkan AR.'),
      findsOneWidget,
    );
    expect(find.text('Izinkan Kamera'), findsOneWidget);

    messenger.setMockMethodCallHandler(channel, null);
  });
}

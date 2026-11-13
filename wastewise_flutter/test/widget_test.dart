import 'package:flutter_test/flutter_test.dart';

import 'package:wastewise_flutter/main.dart';

void main() {
  testWidgets('splash tampil lalu masuk ke menu utama', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(const MyApp());

    expect(find.text('WasteWise AR'), findsOneWidget);
    expect(find.text('Edukasi Pemilahan Sampah'), findsOneWidget);

    await tester.pump(const Duration(milliseconds: 2200));
    await tester.pumpAndSettle();

    expect(find.text('Mulai Latihan AR'), findsOneWidget);
  });
}

import 'package:flutter_test/flutter_test.dart';

import 'package:wastewise_flutter/main.dart';

void main() {
  testWidgets('menu utama tampil', (WidgetTester tester) async {
    await tester.pumpWidget(const MyApp());

    expect(find.text('WasteWise AR'), findsOneWidget);
    expect(find.text('Mulai Latihan AR'), findsOneWidget);
  });
}

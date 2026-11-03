import 'package:flutter/material.dart';

import 'routes/app_routes.dart';
import 'screens/ar_view_screen.dart';
import 'screens/category_info_screen.dart';
import 'screens/main_menu_screen.dart';
import 'screens/trash_catalog_screen.dart';
import 'theme/app_theme.dart';

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'WasteWise AR',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.dark,
      initialRoute: AppRoutes.mainMenu,
      routes: {
        AppRoutes.mainMenu: (_) => const MainMenuScreen(),
        AppRoutes.categoryInfo: (_) => const CategoryInfoScreen(),
        AppRoutes.trashCatalog: (_) => const TrashCatalogScreen(),
        AppRoutes.arView: (_) => const ARViewScreen(),
      },
    );
  }
}

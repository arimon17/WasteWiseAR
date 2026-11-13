import 'package:flutter/material.dart';

import 'routes/app_routes.dart';
import 'screens/ar_view_screen.dart';
import 'screens/category_info_screen.dart';
import 'screens/main_menu_screen.dart';
import 'screens/splash_screen.dart';
import 'screens/trash_catalog_screen.dart';
import 'state/best_score.dart';
import 'theme/app_theme.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  loadBestScore();
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
      initialRoute: AppRoutes.splash,
      onGenerateInitialRoutes: _generateInitialRoutes,
      onGenerateRoute: _onGenerateRoute,
    );
  }
}

List<Route<dynamic>> _generateInitialRoutes(String initialRouteName) {
  return <Route<dynamic>>[
    _onGenerateRoute(RouteSettings(name: initialRouteName)),
  ];
}

Route<dynamic> _onGenerateRoute(RouteSettings settings) {
  final Widget page = switch (settings.name) {
    AppRoutes.splash => const SplashScreen(),
    AppRoutes.mainMenu => const MainMenuScreen(),
    AppRoutes.categoryInfo => const CategoryInfoScreen(),
    AppRoutes.trashCatalog => const TrashCatalogScreen(),
    AppRoutes.arView => const ARViewScreen(),
    _ => const MainMenuScreen(),
  };

  return PageRouteBuilder<dynamic>(
    settings: settings,
    transitionDuration: const Duration(milliseconds: 420),
    reverseTransitionDuration: const Duration(milliseconds: 320),
    pageBuilder: (_, _, _) => page,
    transitionsBuilder: (_, animation, _, child) {
      return FadeTransition(
        opacity: CurvedAnimation(
          parent: animation,
          curve: Curves.easeOutCubic,
          reverseCurve: Curves.easeInCubic,
        ),
        child: child,
      );
    },
  );
}

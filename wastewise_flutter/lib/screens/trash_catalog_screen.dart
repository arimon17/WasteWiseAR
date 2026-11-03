import 'package:flutter/material.dart';

import '../data/waste_data.dart';
import '../theme/app_theme.dart';

class TrashCatalogScreen extends StatelessWidget {
  const TrashCatalogScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: WasteCategory.values.length,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Katalog Sampah'),
          bottom: TabBar(
            indicatorColor: AppColors.primary,
            indicatorWeight: 2.5,
            labelColor: AppColors.textMain,
            unselectedLabelColor: AppColors.textSub,
            dividerColor: Colors.transparent,
            labelStyle: const TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w700,
            ),
            unselectedLabelStyle: const TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w500,
            ),
            tabs: [
              for (final category in WasteCategory.values)
                Tab(text: category.label),
            ],
          ),
        ),
        body: TabBarView(
          children: [
            for (final category in WasteCategory.values)
              _CategoryList(category: category),
          ],
        ),
      ),
    );
  }
}

class _CategoryList extends StatelessWidget {
  const _CategoryList({required this.category});

  final WasteCategory category;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final items = wasteItemsFor(category);

    return ListView.separated(
      padding: const EdgeInsets.fromLTRB(20, 20, 20, 32),
      itemCount: items.length + 1,
      separatorBuilder: (_, index) => SizedBox(height: index == 0 ? 16 : 10),
      itemBuilder: (context, index) {
        if (index == 0) {
          return Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: category.color.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: category.color.withValues(alpha: 0.32)),
            ),
            child: Row(
              children: [
                Icon(category.icon, color: category.color, size: 22),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    '${items.length} jenis sampah ${category.label.toLowerCase()} '
                    'yang umum ditemukan di rumah.',
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: AppColors.textMain,
                    ),
                  ),
                ),
              ],
            ),
          );
        }

        final item = items[index - 1];

        return Card(
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  width: 40,
                  height: 40,
                  decoration: BoxDecoration(
                    color: category.color.withValues(alpha: 0.16),
                    borderRadius: BorderRadius.circular(11),
                    border: Border.all(
                      color: category.color.withValues(alpha: 0.35),
                    ),
                  ),
                  child: Icon(category.icon, color: category.color, size: 20),
                ),
                const SizedBox(width: 13),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(item.name, style: theme.textTheme.titleMedium),
                      const SizedBox(height: 3),
                      Text(item.tip, style: theme.textTheme.bodyMedium),
                    ],
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}

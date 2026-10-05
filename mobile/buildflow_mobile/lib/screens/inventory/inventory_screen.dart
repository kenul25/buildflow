import 'package:flutter/material.dart';

import '../../services/inventory_service.dart';
import '../home/operations_navigation.dart';
import 'stock_action_sheet.dart';

class InventoryScreen extends StatefulWidget {
  const InventoryScreen({
    required this.service,
    this.canManageStock = false,
    super.key,
  });
  final InventoryService service;
  final bool canManageStock;
  @override
  State<InventoryScreen> createState() => _InventoryScreenState();
}

class _InventoryScreenState extends State<InventoryScreen> {
  int tab = 0;
  bool loading = true, busy = false;
  String? error, warehouseId, category;
  String availability = 'All stock';
  final search = TextEditingController();
  List<Map<String, dynamic>> materials = [],
      warehouses = [],
      reservations = [],
      movements = [],
      alerts = [];
  @override
  void initState() {
    super.initState();
    refresh();
  }

  @override
  void dispose() {
    search.dispose();
    super.dispose();
  }

  Future<void> refresh() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final results = await Future.wait([
        widget.service.materials(),
        widget.service.warehouses(),
        widget.service.reservations(),
        widget.service.movements(),
        widget.service.alerts(),
      ]);
      if (mounted) {
        setState(() {
          materials = results[0];
          warehouses = results[1];
          reservations = results[2];
          movements = results[3];
          alerts = results[4];
          if (!warehouses.any((item) => item['id'] == warehouseId)) {
            warehouseId = null;
          }
          if (!materials.any((item) => item['category'] == category)) {
            category = null;
          }
        });
      }
    } catch (cause) {
      if (mounted) setState(() => error = cause.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  bool matches(Map<String, dynamic> material) {
    final query = search.text.trim().toLowerCase();
    final available = (material['availableStock'] as num?) ?? 0;
    return (warehouseId == null || material['warehouseId'] == warehouseId) &&
        (category == null || material['category'] == category) &&
        (availability == 'All stock' ||
            (availability == 'In stock' ? available > 0 : available <= 0)) &&
        (query.isEmpty ||
            ['name', 'category', 'grade'].any(
              (key) => '${material[key] ?? ''}'.toLowerCase().contains(query),
            ));
  }

  Map<String, dynamic>? materialFor(Map<String, dynamic> row) => materials
      .where((material) => material['id'] == row['materialId'])
      .firstOrNull;
  bool matchesLog(Map<String, dynamic> row) {
    final material = materialFor(row);
    if (material != null) return matches(material);
    // Historical records can outlive an archived material. Keep them visible without metadata filters.
    return warehouseId == null &&
        category == null &&
        availability == 'All stock' &&
        '${row['materialName'] ?? ''}'.toLowerCase().contains(
          search.text.trim().toLowerCase(),
        );
  }

  void clearFilters() => setState(() {
    warehouseId = null;
    category = null;
    availability = 'All stock';
    search.clear();
  });

  Future<void> filters() async {
    var selectedCategory = category;
    var selectedAvailability = availability;
    final categories =
        materials
            .map((item) => '${item['category'] ?? 'Material'}')
            .toSet()
            .toList()
          ..sort();
    final apply = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      backgroundColor: Theme.of(context).colorScheme.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (context) => StatefulBuilder(
        builder: (context, update) => Padding(
          padding: EdgeInsets.fromLTRB(
            20,
            24,
            20,
            24 + MediaQuery.paddingOf(context).bottom,
          ),
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Filter materials',
                  style: Theme.of(context).textTheme.titleLarge
                      ?.copyWith(fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: 24),
                DropdownButtonFormField<String>(
                  initialValue: selectedCategory ?? '',
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Category'),
                  items: [
                    const DropdownMenuItem(
                      value: '',
                      child: Text('All categories'),
                    ),
                    ...categories.map(
                      (value) => DropdownMenuItem(
                        value: value,
                        child: Text(value, overflow: TextOverflow.ellipsis),
                      ),
                    ),
                  ],
                  onChanged: (value) => update(
                    () => selectedCategory = value == '' ? null : value,
                  ),
                ),
                const SizedBox(height: 20),
                DropdownButtonFormField<String>(
                  initialValue: selectedAvailability,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Availability'),
                  items: ['All stock', 'In stock', 'Out of stock']
                      .map(
                        (value) =>
                            DropdownMenuItem(value: value, child: Text(value)),
                      )
                      .toList(),
                  onChanged: (value) =>
                      update(() => selectedAvailability = value!),
                ),
                const SizedBox(height: 24),
                FilledButton(
                  onPressed: () => Navigator.pop(context, true),
                  style: FilledButton.styleFrom(
                    backgroundColor: OperationsNavigation.blue,
                    minimumSize: const Size.fromHeight(48),
                  ),
                  child: const Text('Apply filters'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
    if (apply == true && mounted) {
      setState(() {
        category = selectedCategory;
        availability = selectedAvailability;
      });
    }
  }

  Future<void> stockActions(Map<String, dynamic> material) async {
    if (busy || !widget.canManageStock) return;
    setState(() => busy = true);
    final saved = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      isDismissible: false,
      enableDrag: false,
      backgroundColor: Theme.of(context).colorScheme.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (_) =>
          StockActionSheet(material: material, service: widget.service),
    );
    if (!mounted) return;
    setState(() => busy = false);
    if (saved == true) {
      message('Stock updated.');
      await refresh();
    }
  }

  Future<void> release(Map<String, dynamic> row) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Release reservation?'),
        content: Text(
          'Release the reserved quantity for ${row['materialName'] ?? materialFor(row)?['name'] ?? 'this material'}?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Release'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;
    setState(() => busy = true);
    try {
      await widget.service.release(row['id'] as String);
      if (mounted) {
        message('Reservation released.');
        await refresh();
      }
    } catch (cause) {
      if (mounted) message(cause.toString());
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  void message(String value) =>
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(value)));

  @override
  Widget build(BuildContext context) {
    if (loading) {
      return const Center(
        child: CircularProgressIndicator(color: OperationsNavigation.blue),
      );
    }
    if (error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Text('Unable to load materials'),
            TextButton(onPressed: refresh, child: const Text('Retry')),
          ],
        ),
      );
    }
    final rows = switch (tab) {
      0 => materials.where(matches).toList(),
      1 => reservations.where(matchesLog).toList(),
      _ => movements.where(matchesLog).toList(),
    };
    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 720),
        child: RefreshIndicator(
          color: OperationsNavigation.blue,
          onRefresh: refresh,
          child: CustomScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            slivers: [
              SliverPadding(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
                sliver: SliverToBoxAdapter(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      DropdownButtonFormField<String>(
                        key: ValueKey('warehouse-$warehouseId'),
                        initialValue: warehouseId ?? '',
                        isExpanded: true,
                        decoration: const InputDecoration(
                          labelText: 'Warehouse / stock location',
                          prefixIcon: Icon(
                            Icons.warehouse_outlined,
                            color: OperationsNavigation.blue,
                          ),
                        ),
                        items: [
                          const DropdownMenuItem(
                            value: '',
                            child: Text('All warehouses'),
                          ),
                          ...warehouses.map(
                            (item) => DropdownMenuItem(
                              value: item['id'] as String,
                              child: Text(
                                '${item['name']}',
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                          ),
                        ],
                        onChanged: (value) => setState(
                          () => warehouseId = value == '' ? null : value,
                        ),
                      ),
                      const SizedBox(height: 16),
                      SegmentedButton<int>(
                        expandedInsets: EdgeInsets.zero,
                        showSelectedIcon: false,
                        segments: [
                          for (var i = 0; i < 3; i++)
                            ButtonSegment(
                              value: i,
                              label: FittedBox(
                                fit: BoxFit.scaleDown,
                                child: Text(
                                  ['Stock', 'Reservations', 'Movements'][i],
                                ),
                              ),
                            ),
                        ],
                        selected: {tab},
                        onSelectionChanged: (value) =>
                            setState(() => tab = value.single),
                        style: ButtonStyle(
                          minimumSize: const WidgetStatePropertyAll(
                            Size.fromHeight(48),
                          ),
                          padding: const WidgetStatePropertyAll(
                            EdgeInsets.symmetric(horizontal: 8),
                          ),
                          textStyle: const WidgetStatePropertyAll(
                            TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          foregroundColor: WidgetStateProperty.resolveWith(
                            (states) => states.contains(WidgetState.selected)
                                ? OperationsNavigation.blue
                                : const Color(0xFF64748B),
                          ),
                          backgroundColor: WidgetStateProperty.resolveWith(
                            (states) => states.contains(WidgetState.selected)
                                ? OperationsNavigation.blue.withValues(
                                    alpha: .09,
                                  )
                                : Theme.of(context).colorScheme.surface,
                          ),
                          side: const WidgetStatePropertyAll(
                            BorderSide(color: Color(0xFFE2E8F0)),
                          ),
                        ),
                      ),
                      const SizedBox(height: 16),
                      Row(
                        children: [
                          Expanded(
                            child: TextField(
                              controller: search,
                              onChanged: (_) => setState(() {}),
                              decoration: InputDecoration(
                                hintText: 'Search material or grade',
                                prefixIcon: const Icon(
                                  Icons.search_rounded,
                                  color: OperationsNavigation.blue,
                                ),
                                suffixIcon: search.text.isEmpty
                                    ? null
                                    : IconButton(
                                        tooltip: 'Clear search',
                                        onPressed: () =>
                                            setState(() => search.clear()),
                                        icon: const Icon(Icons.close_rounded),
                                      ),
                              ),
                            ),
                          ),
                          const SizedBox(width: 8),
                          IconButton.filledTonal(
                            tooltip: 'Filter materials',
                            onPressed: filters,
                            style: IconButton.styleFrom(
                              foregroundColor: OperationsNavigation.blue,
                              backgroundColor: OperationsNavigation.blue
                                  .withValues(alpha: .09),
                              minimumSize: const Size(48, 48),
                            ),
                            icon: Badge(
                              isLabelVisible:
                                  category != null ||
                                  availability != 'All stock',
                              backgroundColor: OperationsNavigation.blue,
                              child: const Icon(Icons.tune_rounded),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              '${rows.length} ${['materials', 'reservations', 'movements'][tab]}${category == null ? '' : ' · $category'}',
                              style: Theme.of(context).textTheme.bodySmall
                                  ?.copyWith(color: const Color(0xFF64748B)),
                            ),
                          ),
                          if (warehouseId != null ||
                              category != null ||
                              availability != 'All stock' ||
                              search.text.isNotEmpty)
                            TextButton(
                              onPressed: clearFilters,
                              child: const Text('Clear filters'),
                            ),
                        ],
                      ),
                      const SizedBox(height: 12),
                    ],
                  ),
                ),
              ),
              if (rows.isEmpty)
                SliverFillRemaining(hasScrollBody: false, child: emptyState())
              else
                SliverPadding(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                  sliver: SliverList.builder(
                    itemCount: rows.length,
                    itemBuilder: (context, index) => tab == 0
                        ? materialCard(rows[index])
                        : logCard(rows[index]),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }

  Widget materialCard(Map<String, dynamic> item) {
    final shortage = ((item['availableStock'] as num?) ?? 0) <= 0;
    final itemAlerts = alerts.where(
      (alert) => alert['materialId'] == item['id'],
    );
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(18),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(
                  Icons.inventory_2_outlined,
                  color: OperationsNavigation.blue,
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    '${item['name'] ?? 'Material'}',
                    style: Theme.of(context).textTheme.titleMedium
                        ?.copyWith(fontWeight: FontWeight.w700),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              '${item['category'] ?? 'Material'} · ${item['warehouseName'] ?? 'Warehouse'}',
              style: Theme.of(context).textTheme.bodySmall
                  ?.copyWith(color: const Color(0xFF64748B)),
            ),
            const SizedBox(height: 16),
            Text(
              'Quantities in ${item['unit'] ?? 'units'}',
              style: Theme.of(context).textTheme.bodySmall,
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                for (final pair in [
                  ('Current', 'currentStock'),
                  ('Reserved', 'reservedStock'),
                  ('Available', 'availableStock'),
                ])
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          pair.$1,
                          style: Theme.of(context).textTheme.bodySmall
                              ?.copyWith(color: const Color(0xFF64748B)),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          '${item[pair.$2] ?? 0}',
                          style: Theme.of(context).textTheme.titleMedium
                              ?.copyWith(
                                fontWeight: FontWeight.w700,
                                color: shortage && pair.$2 == 'availableStock'
                                    ? const Color(0xFFB45309)
                                    : null,
                              ),
                        ),
                      ],
                    ),
                  ),
              ],
            ),
            if (shortage || itemAlerts.isNotEmpty) ...[
              const SizedBox(height: 12),
              Text(
                itemAlerts.isEmpty
                    ? 'Out of stock'
                    : itemAlerts
                          .map((alert) => '${alert['message']}')
                          .join('\n'),
                style: const TextStyle(color: Color(0xFFB45309)),
              ),
            ],
            if (widget.canManageStock) ...[
              const SizedBox(height: 16),
              OutlinedButton.icon(
                onPressed: busy ? null : () => stockActions(item),
                style: OutlinedButton.styleFrom(
                  foregroundColor: OperationsNavigation.blue,
                  minimumSize: const Size.fromHeight(44),
                ),
                icon: const Icon(Icons.swap_vert_rounded, size: 20),
                label: const Text('Stock actions'),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget logCard(Map<String, dynamic> row) {
    final material = materialFor(row);
    final unit = material?['unit'] ?? row['unit'] ?? '';
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              '${row['materialName'] ?? material?['name'] ?? 'Material'}',
              style: Theme.of(context).textTheme.titleMedium
                  ?.copyWith(fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Text(
              '${row['quantity']} $unit · ${tab == 1 ? row['status'] : row['type']}',
            ),
            const SizedBox(height: 6),
            Text(
              tab == 1
                  ? 'Expires: ${date(row['expiresAt'])}'
                  : '${date(row['createdAt'])} · Stock after: ${row['stockAfter']} $unit',
              style: Theme.of(context).textTheme.bodySmall
                  ?.copyWith(color: const Color(0xFF64748B)),
            ),
            if (tab == 1 &&
                row['status'] == 'Active' &&
                widget.canManageStock) ...[
              const SizedBox(height: 8),
              TextButton.icon(
                onPressed: busy ? null : () => release(row),
                icon: const Icon(Icons.lock_open_rounded, size: 18),
                label: const Text('Release reservation'),
              ),
            ],
          ],
        ),
      ),
    );
  }

  String date(dynamic value) {
    final parsed = DateTime.tryParse('$value')?.toLocal();
    return parsed == null
        ? 'Not specified'
        : '${parsed.day}/${parsed.month}/${parsed.year}';
  }

  Widget emptyState() {
    final filtered =
        warehouseId != null ||
        category != null ||
        availability != 'All stock' ||
        search.text.isNotEmpty;
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: OperationsNavigation.blue.withValues(alpha: .06),
              ),
              child: const Icon(
                Icons.inventory_2_outlined,
                size: 40,
                color: OperationsNavigation.blue,
              ),
            ),
            const SizedBox(height: 20),
            Text(
              filtered
                  ? 'No matching records'
                  : [
                      'No inventory is available yet.',
                      'No reservations found.',
                      'No stock movements found.',
                    ][tab],
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.titleMedium
                  ?.copyWith(fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Text(
              filtered ? 'Try another search or clear your filters.' : 'Records will appear here when the inventory team updates stock.',
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.bodySmall
                  ?.copyWith(color: const Color(0xFF64748B)),
            ),
            const SizedBox(height: 16),
            OutlinedButton.icon(
              onPressed: filtered ? clearFilters : refresh,
              icon: Icon(
                filtered
                    ? Icons.filter_alt_off_outlined
                    : Icons.refresh_rounded,
              ),
              label: Text(filtered ? 'Reset filters' : 'Refresh'),
            ),
          ],
        ),
      ),
    );
  }
}

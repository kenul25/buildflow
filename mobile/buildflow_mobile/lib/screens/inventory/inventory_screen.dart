import 'package:flutter/material.dart';

import '../../services/inventory_service.dart';

class InventoryScreen extends StatefulWidget {
  const InventoryScreen({required this.service, super.key});
  final InventoryService service;

  @override
  State<InventoryScreen> createState() => _InventoryScreenState();
}

class _InventoryScreenState extends State<InventoryScreen> {
  List<Map<String, dynamic>> materials = [];
  bool loading = true;
  String? error;

  @override
  void initState() { super.initState(); refresh(); }

  Future<void> refresh() async {
    setState(() { loading = true; error = null; });
    try {
      final result = await widget.service.materials();
      if (mounted) setState(() => materials = result);
    } catch (cause) { if (mounted) setState(() => error = cause.toString()); }
    finally { if (mounted) setState(() => loading = false); }
  }

  Future<void> confirmReceipt(Map<String, dynamic> material) async {
    final quantity = await _quantityDialog(context, material['unit'] as String? ?? 'unit');
    if (quantity == null) return;
    try {
      await widget.service.receive(material['id'] as String, quantity);
      if (mounted) { ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Receipt recorded.'))); refresh(); }
    } catch (cause) { if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(cause.toString()))); }
  }

  void reportShortage(Map<String, dynamic> material) => ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Shortage reported for ${material['name']}. Project manager review is required.')),
      );

  @override
  Widget build(BuildContext context) {
    if (loading) return const Center(child: CircularProgressIndicator());
    if (error != null) return Center(child: Column(mainAxisSize: MainAxisSize.min, children: [Text(error!), TextButton(onPressed: refresh, child: const Text('Retry'))]));
    if (materials.isEmpty) return RefreshIndicator(onRefresh: refresh, child: ListView(children: const [SizedBox(height: 180), Center(child: Text('No inventory is available yet.'))]));
    return RefreshIndicator(onRefresh: refresh, child: ListView.builder(
      padding: const EdgeInsets.all(16), itemCount: materials.length,
      itemBuilder: (context, index) {
        final material = materials[index];
        final available = (material['availableStock'] as num?)?.toDouble() ?? 0;
        final required = (material['reservedStock'] as num?)?.toDouble() ?? 0;
        final shortage = available <= 0;
        return Card(child: Padding(padding: const EdgeInsets.all(16), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Row(children: [Expanded(child: Text(material['name'] as String? ?? 'Material', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700))), if (shortage) const Chip(label: Text('SHORTAGE'))]),
          const SizedBox(height: 6), Text('${material['category'] ?? 'Material'} · ${material['warehouseName'] ?? 'Warehouse'}'),
          const SizedBox(height: 12), Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [Text('Available: ${available.toStringAsFixed(2)} ${material['unit']}'), Text('Reserved: ${required.toStringAsFixed(2)}')]),
          const SizedBox(height: 12), Row(children: [OutlinedButton.icon(onPressed: () => confirmReceipt(material), icon: const Icon(Icons.inventory_2_outlined), label: const Text('Confirm receipt')), const SizedBox(width: 8), TextButton.icon(onPressed: shortage ? () => reportShortage(material) : null, icon: const Icon(Icons.report_problem_outlined), label: const Text('Report shortage'))]),
        ])));
      },
    ));
  }
}

Future<double?> _quantityDialog(BuildContext context, String unit) async {
  final controller = TextEditingController(text: '1');
  final value = await showDialog<double>(context: context, builder: (context) => AlertDialog(
    title: const Text('Confirm received quantity'),
    content: TextField(controller: controller, keyboardType: const TextInputType.numberWithOptions(decimal: true), decoration: InputDecoration(labelText: unit)),
    actions: [TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')), FilledButton(onPressed: () => Navigator.pop(context, double.tryParse(controller.text)), child: const Text('Record'))],
  ));
  controller.dispose();
  return value != null && value > 0 ? value : null;
}
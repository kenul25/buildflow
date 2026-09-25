import 'package:flutter/material.dart';

import '../../services/inventory_service.dart';

class InventoryScreen extends StatefulWidget {
  const InventoryScreen({required this.service, super.key});
  final InventoryService service;

  @override
  State<InventoryScreen> createState() => _InventoryScreenState();
}

class _InventoryScreenState extends State<InventoryScreen> {
  int tab = 0;
  bool loading = true;
  String? error;
  List<Map<String, dynamic>> materials = [];
  List<Map<String, dynamic>> warehouses = [];
  List<Map<String, dynamic>> reservations = [];
  List<Map<String, dynamic>> movements = [];
  List<Map<String, dynamic>> alerts = [];

  @override
  void initState() { super.initState(); refresh(); }

  Future<void> refresh() async {
    setState(() { loading = true; error = null; });
    try {
      final results = await Future.wait([
        widget.service.materials(), widget.service.warehouses(), widget.service.reservations(),
        widget.service.movements(), widget.service.alerts(),
      ]);
      if (mounted) {
        setState(() {
          materials = results[0]; warehouses = results[1]; reservations = results[2];
          movements = results[3]; alerts = results[4];
        });
      }
    } catch (cause) { if (mounted) setState(() => error = cause.toString()); }
    finally { if (mounted) setState(() => loading = false); }
  }

  Future<void> changeStock(Map<String, dynamic> material, String action) async {
    final quantity = await _quantityDialog(context, action, material['unit'] as String? ?? 'unit');
    if (quantity == null) return;
    try {
      if (action == 'receive') await widget.service.receive(material['id'] as String, quantity);
      if (action == 'issue') await widget.service.issue(material['id'] as String, quantity);
      if (action == 'return') await widget.service.returnStock(material['id'] as String, quantity);
      if (mounted) { _message('$action recorded.'); refresh(); }
    } catch (cause) { if (mounted) _message(cause.toString()); }
  }

  Future<void> reserve(Map<String, dynamic> material) async {
    final quantity = await _quantityDialog(context, 'Reserve', material['unit'] as String? ?? 'unit');
    if (quantity == null) return;
    try { await widget.service.reserve(material['id'] as String, quantity); if (mounted) { _message('Reservation created.'); refresh(); } }
    catch (cause) { if (mounted) _message(cause.toString()); }
  }

  void _message(String value) => ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(value)));

  @override
  Widget build(BuildContext context) {
    if (loading) return const Center(child: CircularProgressIndicator());
    if (error != null) return Center(child: Column(mainAxisSize: MainAxisSize.min, children: [Text(error!), TextButton(onPressed: refresh, child: const Text('Retry'))]));
    return Column(children: [
      SingleChildScrollView(scrollDirection: Axis.horizontal, child: Row(children: ['Materials', 'Warehouses', 'Reservations', 'Movements', 'Alerts'].asMap().entries.map((entry) => Padding(padding: const EdgeInsets.symmetric(horizontal: 4), child: ChoiceChip(label: Text(entry.value), selected: tab == entry.key, onSelected: (_) => setState(() => tab = entry.key)))).toList())),
      Expanded(child: RefreshIndicator(onRefresh: refresh, child: _content())),
    ]);
  }

  Widget _content() => switch (tab) {
    0 => _materials(),
    1 => _simpleList(warehouses, 'No warehouses found.', (item) => '${item['name']}\n${item['location'] ?? 'No location'}'),
    2 => _reservations(),
    3 => _simpleList(movements, 'No stock movements found.', (item) => '${item['materialName']} · ${item['type']}\nQuantity: ${item['quantity']} · Stock after: ${item['stockAfter']}'),
    _ => _simpleList(alerts, 'No low-stock alerts.', (item) => '${item['materialName']} · ${item['severity']}\n${item['message']}'),
  };

  Widget _materials() => materials.isEmpty ? _empty('No inventory is available yet.') : ListView.builder(padding: const EdgeInsets.all(12), itemCount: materials.length, itemBuilder: (context, index) {
    final item = materials[index];
    final available = (item['availableStock'] as num?)?.toDouble() ?? 0;
    final shortage = available <= 0;
    return Card(child: Padding(padding: const EdgeInsets.all(14), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Row(children: [Expanded(child: Text(item['name'] as String? ?? 'Material', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700))), if (shortage) const Chip(label: Text('SHORTAGE'))]),
      Text('${item['category'] ?? 'Material'} · ${item['warehouseName'] ?? 'Warehouse'}'),
      const SizedBox(height: 8), Text('Current: ${item['currentStock']} · Reserved: ${item['reservedStock']} · Available: ${item['availableStock']} ${item['unit']}'),
      const SizedBox(height: 8), Wrap(spacing: 6, children: [TextButton(onPressed: () => changeStock(item, 'receive'), child: const Text('Receive')), TextButton(onPressed: () => changeStock(item, 'issue'), child: const Text('Issue')), TextButton(onPressed: () => changeStock(item, 'return'), child: const Text('Return')), TextButton(onPressed: () => reserve(item), child: const Text('Reserve')), if (shortage) TextButton(onPressed: () => _message('Shortage reported for ${item['name']}.'), child: const Text('Report shortage'))]),
    ])));
  });

  Widget _reservations() => _simpleList(reservations, 'No reservations found.', (item) => '${item['materialName']} · ${item['quantity']}\n${item['status']} · expires ${item['expiresAt']}', action: (item) async { if (item['status'] == 'Active') { await widget.service.release(item['id'] as String); refresh(); } });

  Widget _simpleList(List<Map<String, dynamic>> items, String empty, String Function(Map<String, dynamic>) text, {Future<void> Function(Map<String, dynamic>)? action}) => items.isEmpty ? _empty(empty) : ListView.builder(padding: const EdgeInsets.all(12), itemCount: items.length, itemBuilder: (context, index) { final item = items[index]; return Card(child: ListTile(title: Text(text(item)), isThreeLine: true, trailing: action == null ? null : IconButton(icon: const Icon(Icons.cancel_outlined), tooltip: 'Cancel reservation', onPressed: () => action(item)))); });
  Widget _empty(String message) => ListView(children: [const SizedBox(height: 160), Center(child: Text(message))]);
}

Future<double?> _quantityDialog(BuildContext context, String action, String unit) async {
  final controller = TextEditingController(text: '1');
  final result = await showDialog<double>(context: context, builder: (context) => AlertDialog(title: Text('$action quantity'), content: TextField(controller: controller, autofocus: true, keyboardType: const TextInputType.numberWithOptions(decimal: true), decoration: InputDecoration(labelText: unit)), actions: [TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')), FilledButton(onPressed: () => Navigator.pop(context, double.tryParse(controller.text)), child: const Text('Save'))]));
  controller.dispose();
  return result != null && result > 0 ? result : null;
}

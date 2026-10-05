import 'package:flutter/material.dart';

import '../../services/inventory_service.dart';
import '../home/operations_navigation.dart';

class StockActionSheet extends StatefulWidget {
  const StockActionSheet({
    required this.material,
    required this.service,
    super.key,
  });
  final Map<String, dynamic> material;
  final InventoryService service;
  @override
  State<StockActionSheet> createState() => _StockActionSheetState();
}

class _StockActionSheetState extends State<StockActionSheet> {
  final quantity = TextEditingController();
  final form = GlobalKey<FormState>();
  String? action, error;
  bool saving = false;
  static const actions = [
    (
      'Receive',
      'Add delivered stock to the warehouse',
      Icons.move_to_inbox_outlined,
    ),
    ('Issue', 'Record stock issued for site work', Icons.outbox_outlined),
    (
      'Return',
      'Put unused stock back into inventory',
      Icons.keyboard_return_rounded,
    ),
    ('Reserve', 'Hold stock for 60 minutes', Icons.lock_outline_rounded),
  ];
  @override
  void dispose() {
    quantity.dispose();
    super.dispose();
  }

  Future<void> save() async {
    if (saving || !form.currentState!.validate()) return;
    setState(() {
      saving = true;
      error = null;
    });
    final amount = double.parse(quantity.text.trim());
    try {
      final id = widget.material['id'] as String;
      switch (action) {
        case 'Receive':
          await widget.service.receive(id, amount);
        case 'Issue':
          await widget.service.issue(id, amount);
        case 'Return':
          await widget.service.returnStock(id, amount);
        case 'Reserve':
          await widget.service.reserve(id, amount);
      }
      if (mounted) Navigator.pop(context, true);
    } catch (cause) {
      if (mounted) {
        setState(() {
          error = cause.toString();
          saving = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) => PopScope(
    canPop: !saving,
    child: AnimatedPadding(
      duration: const Duration(milliseconds: 180),
      padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxHeight:
              MediaQuery.sizeOf(context).height * .9 -
              MediaQuery.viewInsetsOf(context).bottom,
        ),
        child: SingleChildScrollView(
          padding: EdgeInsets.fromLTRB(
            20,
            20,
            20,
            20 + MediaQuery.paddingOf(context).bottom,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      action == null ? 'Stock actions' : '$action stock',
                      style: Theme.of(context).textTheme.titleLarge
                          ?.copyWith(fontWeight: FontWeight.w700),
                    ),
                  ),
                  IconButton(
                    tooltip: 'Close stock actions',
                    onPressed: saving
                        ? null
                        : () => Navigator.pop(context, false),
                    icon: const Icon(Icons.close_rounded),
                  ),
                ],
              ),
              Text(
                '${widget.material['name']}',
                style: Theme.of(context).textTheme.titleSmall,
              ),
              const SizedBox(height: 6),
              Text(
                'Available: ${widget.material['availableStock']} ${widget.material['unit']}',
                style: Theme.of(context).textTheme.bodySmall
                    ?.copyWith(color: const Color(0xFF64748B)),
              ),
              const SizedBox(height: 20),
              if (action == null)
                ...actions.map(
                  (entry) => Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: Card(
                      margin: EdgeInsets.zero,
                      child: ListTile(
                        contentPadding: const EdgeInsets.symmetric(
                          horizontal: 16,
                          vertical: 8,
                        ),
                        leading: Icon(
                          entry.$3,
                          color: OperationsNavigation.blue,
                        ),
                        title: Text(
                          entry.$1,
                          style: const TextStyle(fontWeight: FontWeight.w600),
                        ),
                        subtitle: Text(entry.$2),
                        trailing: const Icon(Icons.chevron_right_rounded),
                        onTap: () => setState(() => action = entry.$1),
                      ),
                    ),
                  ),
                )
              else ...[
                Form(
                  key: form,
                  child: TextFormField(
                    controller: quantity,
                    enabled: !saving,
                    keyboardType: const TextInputType.numberWithOptions(
                      decimal: true,
                    ),
                    decoration: InputDecoration(
                      labelText: 'Quantity (${widget.material['unit']})',
                      hintText: 'Enter quantity',
                      border: const OutlineInputBorder(),
                    ),
                    validator: (text) {
                      final amount = double.tryParse(text?.trim() ?? '');
                      if (amount == null || !amount.isFinite || amount <= 0) {
                        return 'Enter a positive quantity';
                      }
                      if (['Issue', 'Reserve'].contains(action) &&
                          amount >
                              ((widget.material['availableStock'] as num?) ??
                                  0)) {
                        return 'Quantity exceeds available stock';
                      }
                      return null;
                    },
                  ),
                ),
                const SizedBox(height: 16),
                Text(
                  'Confirm to ${action!.toLowerCase()} this quantity in ${widget.material['unit']}.',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
                if (error != null) ...[
                  const SizedBox(height: 12),
                  Semantics(
                    liveRegion: true,
                    child: Text(
                      error!,
                      style: TextStyle(
                        color: Theme.of(context).colorScheme.error,
                      ),
                    ),
                  ),
                ],
                const SizedBox(height: 20),
                FilledButton(
                  onPressed: saving ? null : save,
                  style: FilledButton.styleFrom(
                    backgroundColor: OperationsNavigation.blue,
                    minimumSize: const Size.fromHeight(48),
                  ),
                  child: saving
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Colors.white,
                          ),
                        )
                      : Text('Confirm ${action!.toLowerCase()}'),
                ),
                TextButton(
                  onPressed: saving
                      ? null
                      : () => setState(() {
                          action = null;
                          error = null;
                          quantity.clear();
                        }),
                  child: const Text('Choose another action'),
                ),
              ],
            ],
          ),
        ),
      ),
    ),
  );
}

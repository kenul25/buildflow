import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

import '../../services/procurement_service.dart';

class ProcurementScreen extends StatefulWidget {
  const ProcurementScreen({
    required this.service,
    this.canApprove = false,
    super.key,
  });
  final ProcurementService service;
  final bool canApprove;
  @override
  State<ProcurementScreen> createState() => _ProcurementScreenState();
}

class _ProcurementScreenState extends State<ProcurementScreen> {
  final _labels = ['Requests', 'Orders', 'Deliveries'];
  List<List<Map<String, dynamic>>> _rows = [[], [], []];
  int _tab = 0;
  bool _loading = true, _saving = false;
  String? _error;
  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final rows = await Future.wait([
        widget.service.purchaseRequests(),
        widget.service.purchaseOrders(),
        widget.service.deliveries(),
      ]);
      if (mounted) setState(() => _rows = rows);
    } catch (error) {
      if (mounted) setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _perform(Future<dynamic> Function() action) async {
    if (_saving) return;
    setState(() => _saving = true);
    try {
      await action();
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(const SnackBar(content: Text('Saved successfully')));
        await _load();
      }
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(error.toString())));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _photo(Map<String, dynamic> row) async {
    try {
      final file = await ImagePicker().pickImage(
        source: ImageSource.camera,
        maxWidth: 1600,
        imageQuality: 85,
      );
      if (file != null && mounted) {
        await _perform(
          () => widget.service.uploadDeliveryEvidence(
            '/deliveries/${row['id']}/evidence',
            file.path,
          ),
        );
      }
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(error.toString())));
      }
    }
  }

  @override
  Widget build(BuildContext context) => Column(
    children: [
      Padding(
        padding: const EdgeInsets.all(12),
        child: Wrap(
          spacing: 8,
          children: List.generate(
            3,
            (index) => ChoiceChip(
              label: Text(_labels[index]),
              selected: _tab == index,
              onSelected: (_) => setState(() => _tab = index),
            ),
          ),
        ),
      ),
      const Padding(
        padding: EdgeInsets.symmetric(horizontal: 16),
        child: Text(
          'Project procurement status and delivery confirmation. Manage procurement records in the web workspace.',
        ),
      ),
      if (_error != null)
        Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            children: [
              Text(_error!),
              TextButton(onPressed: _load, child: const Text('Retry')),
            ],
          ),
        ),
      Expanded(
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : RefreshIndicator(
                onRefresh: _load,
                child: ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.all(16),
                  children: _rows[_tab].isEmpty
                      ? [const Text('No records for your assigned projects.')]
                      : _rows[_tab]
                            .map(
                              (row) => Card(
                                child: Padding(
                                  padding: const EdgeInsets.all(16),
                                  child: Column(
                                    crossAxisAlignment:
                                        CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        '${row['materialName']} · #${row['id']}',
                                        style: Theme.of(context)
                                            .textTheme
                                            .titleMedium,
                                      ),
                                      Text(
                                        'Quantity: ${row['quantity']} ${row['unit'] ?? ''}',
                                      ),
                                      Text('Status: ${row['status']}'),
                                      if (row['deliveryDate'] != null)
                                        Text(
                                          'Delivery: ${DateTime.parse(row['deliveryDate'] as String).toLocal()}',
                                        ),
                                      if (row['requiredByDate'] != null)
                                        Text(
                                          'Required by: ${DateTime.parse(row['requiredByDate'] as String).toLocal()}',
                                        ),
                                      TextButton(
                                        onPressed: () => showDialog<void>(
                                          context: context,
                                          builder: (context) => AlertDialog(
                                            title: Text('Record #${row['id']}'),
                                            content: SingleChildScrollView(
                                              child: Column(
                                                crossAxisAlignment:
                                                    CrossAxisAlignment.start,
                                                children: row.entries
                                                    .where(
                                                      (x) =>
                                                          x.value != null &&
                                                          x.value is! Map &&
                                                          x.value is! List,
                                                    )
                                                    .map(
                                                      (x) => Text(
                                                        '${x.key}: ${x.value}',
                                                      ),
                                                    )
                                                    .toList(),
                                              ),
                                            ),
                                            actions: [
                                              TextButton(
                                                onPressed: () =>
                                                    Navigator.pop(context),
                                                child: const Text('Close'),
                                              ),
                                            ],
                                          ),
                                        ),
                                        child: const Text('Details'),
                                      ),
                                      if (_tab == 0 &&
                                          widget.canApprove &&
                                          row['status'] == 'Pending')
                                        TextButton(
                                          onPressed: _saving
                                              ? null
                                              : () => _perform(
                                                  () => widget.service
                                                      .approvePurchaseRequest(
                                                        '${row['id']}',
                                                      ),
                                                ),
                                          child: const Text('Approve request'),
                                        ),
                                      if (_tab == 2 &&
                                          [
                                            'Pending',
                                            'Received',
                                          ].contains(row['status']))
                                        Wrap(
                                          spacing: 8,
                                          children: [
                                            TextButton(
                                              onPressed: _saving
                                                  ? null
                                                  : () => _photo(row),
                                              child: const Text(
                                                'Attach delivery photo',
                                              ),
                                            ),
                                            FilledButton(
                                              onPressed: _saving
                                                  ? null
                                                  : () => _perform(
                                                      () => widget.service
                                                          .updateDeliveryStatus(
                                                            '${row['id']}',
                                                            row['status'] ==
                                                                    'Pending'
                                                                ? 'Received'
                                                                : 'Completed',
                                                          ),
                                                    ),
                                              child: Text(
                                                row['status'] == 'Pending'
                                                    ? 'Confirm received'
                                                    : 'Complete delivery',
                                              ),
                                            ),
                                          ],
                                        ),
                                    ],
                                  ),
                                ),
                              ),
                            )
                            .toList(),
                ),
              ),
      ),
    ],
  );
}

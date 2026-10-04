import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../services/scheduling_service.dart';
import '../../services/project_service.dart';

class SchedulingScreen extends StatefulWidget {
  const SchedulingScreen({required this.service, super.key});
  final SchedulingService service;
  @override
  State<SchedulingScreen> createState() => _SchedulingScreenState();
}

class _SchedulingScreenState extends State<SchedulingScreen> {
  final _kinds = [
    'worker-assignments',
    'equipment-reservations',
    'schedules',
    'equipment',
    'equipment-requests',
    'issues',
  ];
  final _labels = [
    'Assignments',
    'Equipment bookings',
    'Schedules',
    'Equipment',
    'Draft requests',
    'Site issues',
  ];
  final Map<String, List<Map<String, dynamic>>> _data = {};
  bool _loading = true, _busy = false;
  String? _error;
  int _tab = 0;
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
      final results = await Future.wait(_kinds.map(widget.service.list));
      if (mounted) {
        setState(() {
          for (var i = 0; i < _kinds.length; i++) {
            _data[_kinds[i]] = results[i];
          }
        });
      }
    } catch (error) {
      if (mounted) setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _perform(Future<void> Function() action) async {
    if (_busy) return;
    setState(() => _busy = true);
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
      if (mounted) setState(() => _busy = false);
    }
  }

  String _scheduleLabel(dynamic id) =>
      (_data['schedules'] ?? [])
          .where((x) => x['id'] == id)
          .map((x) => '${x['name']}')
          .firstOrNull ??
      '$id';
  @override
  Widget build(BuildContext context) {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(_error!),
            TextButton(onPressed: _load, child: const Text('Retry')),
          ],
        ),
      );
    }
    final rows = _data[_kinds[_tab]] ?? [];
    return Column(
      children: [
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            children: List.generate(
              _labels.length,
              (index) => Padding(
                padding: const EdgeInsets.all(4),
                child: ChoiceChip(
                  label: Text(_labels[index]),
                  selected: _tab == index,
                  onSelected: (_) => setState(() => _tab = index),
                ),
              ),
            ),
          ),
        ),
        Wrap(
          spacing: 8,
          children: [
            FilledButton.icon(
              onPressed: _busy ? null : _scan,
              icon: const Icon(Icons.qr_code_scanner),
              label: const Text('Scan equipment'),
            ),
            TextButton(
              onPressed: _busy ? null : () => _form(false),
              child: const Text('Request equipment'),
            ),
            TextButton(
              onPressed: _busy ? null : () => _form(true),
              child: const Text('Report issue'),
            ),
          ],
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: _load,
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.all(16),
              children: rows.isEmpty
                  ? [
                      const Padding(
                        padding: EdgeInsets.all(24),
                        child: Text('No records for your assigned sites.'),
                      ),
                    ]
                  : rows
                        .map(
                          (row) => Card(
                            child: Padding(
                              padding: const EdgeInsets.all(16),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    '${row['name']}',
                                    style: Theme.of(context)
                                        .textTheme
                                        .titleMedium,
                                  ),
                                  Text('Status: ${row['status'] ?? 'Active'}'),
                                  if (row['scheduleId'] != null)
                                    Text(
                                      'Schedule: ${_scheduleLabel(row['scheduleId'])}',
                                    ),
                                  if (row['startTime'] != null)
                                    Text(
                                      'Start: ${DateTime.parse(row['startTime'] as String).toLocal()}',
                                    ),
                                  if (row['endTime'] != null)
                                    Text(
                                      'End: ${DateTime.parse(row['endTime'] as String).toLocal()}',
                                    ),
                                  if (row['notes'] != null)
                                    Text('${row['notes']}'),
                                  if (_tab == 0 &&
                                      ![
                                        'Completed',
                                        'Cancelled',
                                      ].contains(row['status']))
                                    Wrap(
                                      spacing: 8,
                                      children: ['InProgress', 'Completed', 'Delayed']
                                          .map(
                                            (status) => TextButton(
                                              onPressed: _busy
                                                  ? null
                                                  : () => _perform(
                                                      () => widget.service.status(
                                                        row['id'] as String,
                                                        status,
                                                        '${row['notes'] ?? ''}',
                                                      ),
                                                    ),
                                              child: Text(
                                                status == 'InProgress'
                                                    ? 'Start work'
                                                    : status == 'Completed'
                                                    ? 'Complete'
                                                    : 'Delayed',
                                              ),
                                            ),
                                          )
                                          .toList(),
                                    ),
                                  if (_tab == 4 && row['status'] == 'Draft')
                                    TextButton(
                                      onPressed: _busy
                                          ? null
                                          : () => _perform(
                                              () =>
                                                  widget.service.cancelRequest(
                                                    row['id'] as String,
                                                  ),
                                            ),
                                      child: const Text('Cancel draft request'),
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

  Future<void> _scan() async {
    final code = await Navigator.of(
      context,
    ).push<String>(MaterialPageRoute(builder: (_) => const EquipmentScanner()));
    if (code == null || !mounted) return;
    final action = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Equipment $code'),
        content: const Text(
          'The server will validate your current approved assignment.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, 'receive'),
            child: const Text('Receive'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, 'return'),
            child: const Text('Return'),
          ),
        ],
      ),
    );
    if (action != null) await _perform(() => widget.service.scan(code, action));
  }

  Future<void> _form(bool issue) async {
    List<Map<String, dynamic>> activities;
    try {
      activities = await ProjectService(widget.service.api).list('activities');
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(error.toString())));
      }
      return;
    }
    if (!mounted) return;
    final title = TextEditingController();
    final notes = TextEditingController();
    String? activityId, equipmentId;
    final formKey = GlobalKey<FormState>();
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialog) => AlertDialog(
          title: Text(
            issue
                ? 'Report delay / equipment issue'
                : 'Draft equipment request',
          ),
          content: SingleChildScrollView(
            child: Form(
              key: formKey,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  TextFormField(
                    controller: title,
                    decoration: const InputDecoration(labelText: 'Title'),
                    validator: (value) =>
                        value == null || value.trim().length < 2
                        ? 'Enter a title'
                        : null,
                  ),
                  DropdownButtonFormField<String>(
                    decoration: const InputDecoration(labelText: 'Activity'),
                    items: activities
                        .map(
                          (x) => DropdownMenuItem(
                            value: x['id'] as String,
                            child: Text(
                              '${x['name']}',
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        )
                        .toList(),
                    onChanged: (value) => activityId = value,
                    validator: (value) =>
                        value == null ? 'Select an activity' : null,
                    isExpanded: true,
                  ),
                  DropdownButtonFormField<String>(
                    decoration: InputDecoration(
                      labelText: issue ? 'Equipment (optional)' : 'Equipment',
                    ),
                    items: (_data['equipment'] ?? [])
                        .map(
                          (x) => DropdownMenuItem(
                            value: x['id'] as String,
                            child: Text(
                              '${x['name']}',
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        )
                        .toList(),
                    onChanged: (value) => equipmentId = value,
                    validator: (value) =>
                        !issue && value == null ? 'Select equipment' : null,
                    isExpanded: true,
                  ),
                  TextFormField(
                    controller: notes,
                    maxLines: 3,
                    decoration: const InputDecoration(
                      labelText: 'Description / expected impact',
                    ),
                  ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () {
                if (!formKey.currentState!.validate()) return;
                final body = {
                  'name': title.text.trim(),
                  'notes': notes.text.trim(),
                  'activityId': activityId,
                  'equipmentId': equipmentId,
                };
                Navigator.pop(dialogContext);
                _perform(
                  () => issue
                      ? widget.service.report(body)
                      : widget.service.requestEquipment(body),
                );
              },
              child: const Text('Submit'),
            ),
          ],
        ),
      ),
    );
    title.dispose();
    notes.dispose();
  }
}

class EquipmentScanner extends StatefulWidget {
  const EquipmentScanner({super.key});
  @override
  State<EquipmentScanner> createState() => _EquipmentScannerState();
}

class _EquipmentScannerState extends State<EquipmentScanner> {
  bool _detected = false;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Scan equipment QR')),
    body: MobileScanner(
      onDetect: (capture) {
        final value = capture.barcodes.firstOrNull?.rawValue;
        if (!_detected && value != null && value.trim().isNotEmpty) {
          _detected = true;
          Navigator.pop(context, value.trim());
        }
      },
    ),
  );
}

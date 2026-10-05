import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../services/scheduling_service.dart';
import '../../services/project_service.dart';
import '../home/operations_navigation.dart';

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
    final groups = switch (_tab) {
      0 => [('worker-assignments', 'Your assignments')],
      1 => [
        ('equipment-reservations', 'Equipment bookings'),
        ('equipment-requests', 'Equipment requests'),
        ('equipment', 'Equipment catalog'),
      ],
      _ => [('schedules', 'Work schedules'), ('issues', 'Site reports')],
    };
    final hasRows = groups.any((group) => (_data[group.$1] ?? []).isNotEmpty);
    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 720),
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
              child: SegmentedButton<int>(
                expandedInsets: EdgeInsets.zero,
                showSelectedIcon: false,
                segments: [
                  for (var index = 0; index < 3; index++)
                    ButtonSegment(
                      value: index,
                      label: FittedBox(
                        fit: BoxFit.scaleDown,
                        child: Text(
                          ['Assignments', 'Equipment', 'Timeline'][index],
                        ),
                      ),
                    ),
                ],
                selected: {_tab},
                onSelectionChanged: (selection) =>
                    setState(() => _tab = selection.single),
                style: ButtonStyle(
                  minimumSize: const WidgetStatePropertyAll(
                    Size.fromHeight(48),
                  ),
                  padding: const WidgetStatePropertyAll(
                    EdgeInsets.symmetric(horizontal: 10),
                  ),
                  textStyle: const WidgetStatePropertyAll(
                    TextStyle(fontSize: 13, fontWeight: FontWeight.w700),
                  ),
                  foregroundColor: WidgetStateProperty.resolveWith(
                    (states) => states.contains(WidgetState.selected)
                        ? OperationsNavigation.blue
                        : const Color(0xFF64748B),
                  ),
                  backgroundColor: WidgetStateProperty.resolveWith(
                    (states) => states.contains(WidgetState.selected)
                        ? OperationsNavigation.blue.withValues(alpha: .09)
                        : Theme.of(context).colorScheme.surface,
                  ),
                  side: const WidgetStatePropertyAll(
                    BorderSide(color: Color(0xFFE2E8F0)),
                  ),
                  shape: WidgetStatePropertyAll(
                    RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Expanded(
                    child: _ScheduleAction(
                      label: 'Scan',
                      tooltip: 'Scan equipment',
                      icon: Icons.qr_code_scanner_rounded,
                      onTap: _busy || _loading ? null : _scan,
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: _ScheduleAction(
                      label: 'Request',
                      tooltip: 'Request equipment',
                      icon: Icons.add_circle_outline_rounded,
                      onTap: _busy || _loading
                          ? null
                          : () => _form(false),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: _ScheduleAction(
                      label: 'Report',
                      tooltip: 'Report an issue',
                      icon: Icons.flag_outlined,
                      onTap: _busy || _loading
                          ? null
                          : () => _form(true),
                    ),
                  ),
                ],
              ),
            ),
            if (_busy)
              const LinearProgressIndicator(color: OperationsNavigation.blue),
            Expanded(
              child: RefreshIndicator(
                color: OperationsNavigation.blue,
                onRefresh: _load,
                child: CustomScrollView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  slivers: [
                    if (_loading)
                      const SliverFillRemaining(
                        hasScrollBody: false,
                        child: Center(
                          child: CircularProgressIndicator(
                            color: OperationsNavigation.blue,
                          ),
                        ),
                      )
                    else if (_error != null)
                      SliverFillRemaining(
                        hasScrollBody: false,
                        child: _ScheduleEmptyState(
                          icon: Icons.cloud_off_rounded,
                          title: 'Unable to load your schedule',
                          description: 'Check your connection and try again.',
                          action: 'Retry',
                          onRefresh: _load,
                        ),
                      )
                    else if (!hasRows)
                      SliverFillRemaining(
                        hasScrollBody: false,
                        child: _ScheduleEmptyState(
                          icon: [
                            Icons.assignment_outlined,
                            Icons.construction_rounded,
                            Icons.calendar_month_rounded,
                          ][_tab],
                          title: [
                            'No assignments yet',
                            'No equipment activity yet',
                            'Your timeline is clear',
                          ][_tab],
                          description: [
                            'Your assigned site work will appear here once it is scheduled.',
                            'Equipment bookings and requests will appear here. Use Request to ask for equipment.',
                            'Scheduled work and site reports will appear here as your project progresses.',
                          ][_tab],
                          action: 'Refresh',
                          onRefresh: _load,
                        ),
                      )
                    else
                      SliverPadding(
                        padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                        sliver: SliverList.list(
                          children: [
                            for (final group in groups)
                              if ((_data[group.$1] ?? []).isNotEmpty) ...[
                                Padding(
                                  padding: const EdgeInsets.only(
                                    top: 8,
                                    bottom: 12,
                                  ),
                                  child: Text(
                                    group.$2,
                                    style: Theme.of(context)
                                        .textTheme
                                        .titleMedium
                                        ?.copyWith(fontWeight: FontWeight.w700),
                                  ),
                                ),
                                for (final row in _data[group.$1]!)
                                  _recordCard(row, group.$1),
                              ],
                          ],
                        ),
                      ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _recordCard(Map<String, dynamic> row, String kind) => Card(
    margin: const EdgeInsets.only(bottom: 12),
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            '${row['name']}',
            style: Theme.of(context).textTheme.titleMedium
                ?.copyWith(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 8),
          Text(
            'Status: ${row['status'] ?? 'Active'}',
            style: Theme.of(context).textTheme.bodySmall
                ?.copyWith(color: const Color(0xFF64748B)),
          ),
          if (row['scheduleId'] != null)
            Text('Schedule: ${_scheduleLabel(row['scheduleId'])}'),
          if (row['startTime'] != null)
            Text(
              'Start: ${DateTime.parse(row['startTime'] as String).toLocal()}',
            ),
          if (row['endTime'] != null)
            Text('End: ${DateTime.parse(row['endTime'] as String).toLocal()}'),
          if (row['notes'] != null) Text('${row['notes']}'),
          if (kind == 'worker-assignments' &&
              !['Completed', 'Cancelled'].contains(row['status']))
            Wrap(
              spacing: 8,
              children: ['InProgress', 'Completed', 'Delayed']
                  .map(
                    (status) => TextButton(
                      style: TextButton.styleFrom(
                        foregroundColor: OperationsNavigation.blue,
                      ),
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
          if (kind == 'equipment-requests' && row['status'] == 'Draft')
            TextButton(
              onPressed: _busy
                  ? null
                  : () => _perform(
                      () => widget.service.cancelRequest(row['id'] as String),
                    ),
              child: const Text('Cancel draft request'),
            ),
        ],
      ),
    ),
  );

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

class _ScheduleAction extends StatelessWidget {
  const _ScheduleAction({
    required this.label,
    required this.tooltip,
    required this.icon,
    required this.onTap,
  });
  final String label, tooltip;
  final IconData icon;
  final VoidCallback? onTap;
  @override
  Widget build(BuildContext context) => Tooltip(
    message: tooltip,
    child: Semantics(
      button: true,
      enabled: onTap != null,
      label: tooltip,
      child: Material(
        color: Theme.of(context).colorScheme.surface,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(14),
          side: const BorderSide(color: Color(0xFFE2E8F0)),
        ),
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(14),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 14),
            child: Column(
              children: [
                Icon(
                  icon,
                  color: onTap == null
                      ? const Color(0xFF94A3B8)
                      : OperationsNavigation.blue,
                  size: 22,
                ),
                const SizedBox(height: 8),
                FittedBox(
                  fit: BoxFit.scaleDown,
                  child: Text(
                    label,
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: onTap == null
                          ? const Color(0xFF94A3B8)
                          : Theme.of(context).colorScheme.onSurface,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    ),
  );
}

class _ScheduleEmptyState extends StatelessWidget {
  const _ScheduleEmptyState({
    required this.icon,
    required this.title,
    required this.description,
    required this.action,
    required this.onRefresh,
  });
  final IconData icon;
  final String title, description, action;
  final VoidCallback onRefresh;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.fromLTRB(28, 24, 28, 32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ExcludeSemantics(
            child: SizedBox(
              width: 136,
              height: 124,
              child: Stack(
                alignment: Alignment.center,
                children: [
                  Container(
                    width: 112,
                    height: 112,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: OperationsNavigation.blue.withValues(alpha: .05),
                    ),
                  ),
                  Transform.rotate(
                    angle: -.08,
                    child: Container(
                      width: 80,
                      height: 80,
                      decoration: BoxDecoration(
                        color: Theme.of(context).colorScheme.surface,
                        border: Border.all(
                          color: OperationsNavigation.blue.withValues(
                            alpha: .16,
                          ),
                        ),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Icon(
                        icon,
                        size: 40,
                        color: OperationsNavigation.blue,
                      ),
                    ),
                  ),
                  Positioned(
                    right: 10,
                    bottom: 12,
                    child: Container(
                      padding: const EdgeInsets.all(6),
                      decoration: BoxDecoration(
                        color: Theme.of(context).colorScheme.surface,
                        shape: BoxShape.circle,
                        border: Border.all(color: const Color(0xFFE2E8F0)),
                      ),
                      child: const Icon(
                        Icons.check_rounded,
                        size: 20,
                        color: OperationsNavigation.blue,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 20),
          Text(
            title,
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.titleLarge
                ?.copyWith(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 10),
          Text(
            description,
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.bodyMedium
                ?.copyWith(color: const Color(0xFF64748B), height: 1.5),
          ),
          const SizedBox(height: 20),
          OutlinedButton.icon(
            onPressed: onRefresh,
            style: OutlinedButton.styleFrom(
              foregroundColor: OperationsNavigation.blue,
              minimumSize: const Size(120, 44),
            ),
            icon: const Icon(Icons.refresh_rounded, size: 20),
            label: Text(action),
          ),
        ],
      ),
    ),
  );
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

import 'package:flutter/material.dart';

import '../../services/project_service.dart';
import '../home/operations_navigation.dart';
import 'resource_picker.dart';

class SiteEngineerProjectsScreen extends StatefulWidget {
  const SiteEngineerProjectsScreen({required this.service, super.key});
  final ProjectService service;

  @override
  State<SiteEngineerProjectsScreen> createState() =>
      _SiteEngineerProjectsScreenState();
}

class _SiteEngineerProjectsScreenState
    extends State<SiteEngineerProjectsScreen> {
  List<Map<String, dynamic>> projects = [];
  bool loading = true;
  String? error;

  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final result = await widget.service.list('projects');
      if (mounted) setState(() => projects = result);
    } catch (exception) {
      if (mounted) setState(() => error = exception.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (loading) return const Center(child: CircularProgressIndicator());
    if (error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(error!),
            TextButton(onPressed: load, child: const Text('Retry')),
          ],
        ),
      );
    }
    if (projects.isEmpty) {
      return const Center(child: Text('No projects assigned yet.'));
    }
    return SafeArea(
      child: RefreshIndicator(
        onRefresh: load,
        child: ListView.builder(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
          itemCount: projects.length + 1,
          itemBuilder: (context, index) {
            if (index == 0) {
              return Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 720),
                  child: Padding(
                    padding: const EdgeInsets.only(bottom: 16),
                    child: Row(
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                'Your assigned projects',
                                style: Theme.of(context).textTheme.titleMedium
                                    ?.copyWith(fontWeight: FontWeight.w700),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                '${projects.length} ${projects.length == 1 ? 'project' : 'projects'} · Select a project to view site work',
                                style: Theme.of(context).textTheme.bodySmall
                                    ?.copyWith(color: const Color(0xFF64748B)),
                              ),
                            ],
                          ),
                        ),
                        IconButton(
                          tooltip: 'Refresh projects',
                          onPressed: load,
                          icon: const Icon(
                            Icons.refresh_rounded,
                            color: OperationsNavigation.blue,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              );
            }
            final project = projects[index - 1];
            return Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 720),
                child: _NavigationCard(
                  title: project['name'] as String,
                  subtitle: project['code'] as String? ?? 'Assigned project',
                  status: project['status'] as String? ?? 'Planned',
                  leading: const _ProjectIcon(),
                  onTap: () => Navigator.push(
                    context,
                    MaterialPageRoute<void>(
                      builder: (_) => SiteProjectDetailScreen(
                        service: widget.service,
                        project: project,
                      ),
                    ),
                  ),
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}

class _NavigationCard extends StatelessWidget {
  const _NavigationCard({
    required this.title,
    required this.subtitle,
    required this.onTap,
    this.leading,
    this.status,
    this.progress,
  });

  final String title;
  final String subtitle;
  final VoidCallback onTap;
  final Widget? leading;
  final String? status;
  final double? progress;

  @override
  Widget build(BuildContext context) => Card(
    margin: const EdgeInsets.only(bottom: 12),
    child: InkWell(
      borderRadius: BorderRadius.circular(16),
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.all(18),
        child: Row(
          children: [
            if (leading != null) ...[leading!, const SizedBox(width: 14)],
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    softWrap: true,
                    style: Theme.of(context).textTheme.titleMedium
                        ?.copyWith(fontWeight: FontWeight.w700, height: 1.35),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    subtitle,
                    softWrap: true,
                    style: Theme.of(context).textTheme.bodySmall
                        ?.copyWith(color: const Color(0xFF64748B)),
                  ),
                  if (status != null) ...[
                    const SizedBox(height: 12),
                    _ProjectStatus(status!),
                  ],
                  if (progress != null) ...[
                    const SizedBox(height: 12),
                    ClipRRect(
                      borderRadius: BorderRadius.circular(8),
                      child: LinearProgressIndicator(
                        value: progress!.clamp(0, 100) / 100,
                        minHeight: 5,
                        color: OperationsNavigation.blue,
                        backgroundColor: const Color(0xFFE2E8F0),
                      ),
                    ),
                  ],
                ],
              ),
            ),
            const SizedBox(width: 8),
            const Icon(Icons.chevron_right_rounded, color: Color(0xFF64748B)),
          ],
        ),
      ),
    ),
  );
}

class _ProjectIcon extends StatelessWidget {
  const _ProjectIcon();
  @override
  Widget build(BuildContext context) => Container(
    width: 44,
    height: 44,
    decoration: BoxDecoration(
      color: OperationsNavigation.blue.withValues(alpha: .09),
      borderRadius: BorderRadius.circular(14),
    ),
    child: const Icon(
      Icons.apartment_rounded,
      color: OperationsNavigation.blue,
    ),
  );
}

class _ProjectStatus extends StatelessWidget {
  const _ProjectStatus(this.status);
  final String status;
  @override
  Widget build(BuildContext context) {
    final label = status.replaceAllMapped(
      RegExp(r'([a-z])([A-Z])'),
      (match) => '${match[1]} ${match[2]}',
    );
    final color = switch (status) {
      'Completed' => const Color(0xFF15803D),
      'Active' || 'InProgress' => OperationsNavigation.blue,
      'OnHold' => const Color(0xFFB45309),
      _ => const Color(0xFF64748B),
    };
    return Align(
      alignment: Alignment.centerLeft,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
        decoration: BoxDecoration(
          color: color.withValues(alpha: .08),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Text(
          label,
          style: TextStyle(
            fontSize: 12,
            color: color,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
    );
  }
}

class SiteProjectDetailScreen extends StatefulWidget {
  const SiteProjectDetailScreen({
    required this.service,
    required this.project,
    super.key,
  });
  final ProjectService service;
  final Map<String, dynamic> project;
  @override
  State<SiteProjectDetailScreen> createState() =>
      _SiteProjectDetailScreenState();
}

class _SiteProjectDetailScreenState extends State<SiteProjectDetailScreen> {
  List<Map<String, dynamic>> sites = [];
  List<Map<String, dynamic>> phases = [];
  List<Map<String, dynamic>> activities = [];
  String? siteId;
  String? phaseId;
  bool loading = true;
  String? error;

  @override
  void initState() {
    super.initState();
    loadSites();
  }

  Future<void> loadSites() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final result = await widget.service.list(
        'sites',
        parentId: widget.project['id'] as String,
      );
      if (mounted) {
        setState(() {
          sites = result;
          siteId = result.isEmpty ? null : result.first['id'] as String;
        });
      }
      if (siteId != null) await loadPhases();
    } catch (e) {
      if (mounted) setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> loadPhases() async {
    final result = await widget.service.list('phases', parentId: siteId);
    if (mounted) {
      setState(() {
        phases = result;
        phaseId = result.isEmpty ? null : result.first['id'] as String;
        activities = [];
      });
    }
    if (phaseId != null) await loadActivities();
  }

  Future<void> loadActivities() async {
    final result = await widget.service.list('activities', parentId: phaseId);
    if (mounted) setState(() => activities = result);
  }

  @override
  Widget build(BuildContext context) {
    final projectName = widget.project['name'] as String;
    return Scaffold(
      appBar: AppBar(title: const Text('Project Details')),
      body: SafeArea(
        child: loading
            ? const Center(child: CircularProgressIndicator())
            : error != null
            ? Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(error!),
                    TextButton(
                      onPressed: loadSites,
                      child: const Text('Retry'),
                    ),
                  ],
                ),
              )
            : SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
                child: Center(
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 720),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Card(
                          margin: EdgeInsets.zero,
                          child: Padding(
                            padding: const EdgeInsets.all(20),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const _ProjectIcon(),
                                const SizedBox(height: 16),
                                Text(
                                  projectName,
                                  style: Theme.of(context)
                                      .textTheme
                                      .headlineSmall
                                      ?.copyWith(
                                        fontWeight: FontWeight.w800,
                                        height: 1.25,
                                      ),
                                ),
                                if ((widget.project['code'] as String?)
                                        ?.isNotEmpty ??
                                    false) ...[
                                  const SizedBox(height: 8),
                                  Text(
                                    widget.project['code'] as String,
                                    style: Theme.of(context).textTheme.bodySmall
                                        ?.copyWith(
                                          color: const Color(0xFF64748B),
                                        ),
                                  ),
                                ],
                                const SizedBox(height: 12),
                                _ProjectStatus(
                                  widget.project['status'] as String? ??
                                      'Planned',
                                ),
                                const SizedBox(height: 16),
                                Text(
                                  widget.project['description'] as String? ??
                                      'Assigned construction project',
                                  style: Theme.of(context).textTheme.bodyMedium
                                      ?.copyWith(
                                        height: 1.6,
                                        color: const Color(0xFF64748B),
                                      ),
                                ),
                              ],
                            ),
                          ),
                        ),
                        const SizedBox(height: 24),
                        Text(
                          'Site work',
                          style: Theme.of(context).textTheme.titleMedium
                              ?.copyWith(fontWeight: FontWeight.w700),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          'Choose a site and phase to view its activities.',
                          style: Theme.of(context).textTheme.bodySmall
                              ?.copyWith(color: const Color(0xFF64748B)),
                        ),
                        const SizedBox(height: 20),
                        if (sites.isEmpty)
                          const Text('No sites in this project yet.'),
                        if (sites.isNotEmpty)
                          DropdownButtonFormField<String>(
                            key: ValueKey('site-$siteId'),
                            initialValue: siteId,
                            isExpanded: true,
                            decoration: const InputDecoration(
                              labelText: 'Site',
                              prefixIcon: Icon(
                                Icons.location_on_outlined,
                                color: OperationsNavigation.blue,
                              ),
                            ),
                            items: sites
                                .map(
                                  (site) => DropdownMenuItem(
                                    value: site['id'] as String,
                                    child: Text(
                                      site['name'] as String,
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                    ),
                                  ),
                                )
                                .toList(),
                            onChanged: (value) async {
                              setState(() => siteId = value);
                              await loadPhases();
                            },
                          ),
                        if (phases.isNotEmpty) ...[
                          const SizedBox(height: 16),
                          DropdownButtonFormField<String>(
                            key: ValueKey('phase-$phaseId'),
                            initialValue: phaseId,
                            isExpanded: true,
                            decoration: const InputDecoration(
                              labelText: 'Phase',
                              prefixIcon: Icon(
                                Icons.layers_outlined,
                                color: OperationsNavigation.blue,
                              ),
                            ),
                            items: phases
                                .map(
                                  (phase) => DropdownMenuItem(
                                    value: phase['id'] as String,
                                    child: Text(
                                      phase['name'] as String,
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                    ),
                                  ),
                                )
                                .toList(),
                            onChanged: (value) async {
                              setState(() => phaseId = value);
                              await loadActivities();
                            },
                          ),
                        ],
                        const SizedBox(height: 24),
                        Row(
                          children: [
                            Expanded(
                              child: Text(
                                'Activities',
                                style: Theme.of(context).textTheme.titleLarge
                                    ?.copyWith(fontWeight: FontWeight.w700),
                              ),
                            ),
                            Text(
                              '${activities.length} total',
                              style: Theme.of(context).textTheme.bodySmall
                                  ?.copyWith(color: const Color(0xFF64748B)),
                            ),
                          ],
                        ),
                        const SizedBox(height: 14),
                        if (activities.isEmpty)
                          const Padding(
                            padding: EdgeInsets.symmetric(vertical: 8),
                            child: Text('No activities available.'),
                          ),
                        ...activities.map(
                          (activity) => _NavigationCard(
                            title: activity['name'] as String,
                            subtitle:
                                '${activity['progressPercent'] ?? 0}% complete',
                            status: activity['status'] as String? ?? 'Planned',
                            progress: (activity['progressPercent'] as num? ?? 0)
                                .toDouble(),
                            onTap: () async {
                              await Navigator.push(
                                context,
                                MaterialPageRoute<void>(
                                  builder: (_) => SiteActivityScreen(
                                    service: widget.service,
                                    project: widget.project,
                                    siteId: siteId!,
                                    activity: activity,
                                  ),
                                ),
                              );
                              await loadActivities();
                            },
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
}

class SiteActivityScreen extends StatefulWidget {
  const SiteActivityScreen({
    required this.service,
    required this.project,
    required this.siteId,
    required this.activity,
    this.requestOnly = false,
    super.key,
  });
  final ProjectService service;
  final Map<String, dynamic> project;
  final String siteId;
  final Map<String, dynamic> activity;
  final bool requestOnly;
  @override
  State<SiteActivityScreen> createState() => _SiteActivityScreenState();
}

class _SiteActivityScreenState extends State<SiteActivityScreen> {
  final work = TextEditingController();
  final blockers = TextEditingController();
  final objective = TextEditingController();
  final resource = TextEditingController();
  final quantity = TextEditingController(text: '1');
  final unit = TextEditingController();
  final resourceCount = TextEditingController(text: '1');
  Map<String, dynamic>? selectedResource;
  int progress = 0;
  String kind = 'Material';
  bool busy = false;
  String? message;
  String? workflowId;
  String? workflowStatus;
  final List<Map<String, dynamic>> resources = [];
  List<Map<String, dynamic>> progressHistory = [];
  List<Map<String, dynamic>> savedRequests = [];
  bool historyLoading = true;
  String? historyError;
  final Map<String, Future<Map<String, dynamic>>> requestDetails = {};

  Map<String, dynamic> currentResource() {
    final count = double.tryParse(quantity.text);
    if (selectedResource == null ||
        resource.text.trim().length < 2 ||
        count == null ||
        !count.isFinite ||
        count <= 0 ||
        unit.text.trim().isEmpty) {
      throw Exception('Select a resource and enter a valid quantity.');
    }
    final countValue = kind == 'Workforce' && unit.text == 'workers'
        ? count
        : double.tryParse(resourceCount.text);
    if (kind != 'Material' &&
        (countValue == null ||
            !countValue.isFinite ||
            countValue < 1 ||
            countValue > 10000 ||
            countValue != countValue.roundToDouble())) {
      throw Exception('Enter a whole-number machine or worker count.');
    }
    return {
      'kind': kind,
      'name': resource.text.trim(),
      'quantity': count,
      'unit': unit.text.trim(),
      if (kind != 'Material') 'resourceCount': countValue!.toInt(),
    };
  }

  @override
  void initState() {
    super.initState();
    progress = widget.activity['progressPercent'] as int? ?? 0;
    loadHistory();
  }

  Future<void> loadHistory() async {
    if (!mounted) return;
    setState(() {
      historyLoading = true;
      historyError = null;
    });
    try {
      final results = await Future.wait([
        widget.service.progressHistory(widget.activity['id'] as String),
        widget.service.requests(),
      ]);
      if (!mounted) return;
      setState(() {
        progressHistory = results[0];
        savedRequests = results[1]
            .where((row) => row['activityId'] == widget.activity['id'])
            .toList();
        if (progressHistory.isNotEmpty) {
          progress = (progressHistory.first['progressPercent'] as num).toInt();
        }
        requestDetails.clear();
      });
    } catch (error) {
      if (mounted) setState(() => historyError = error.toString());
    } finally {
      if (mounted) setState(() => historyLoading = false);
    }
  }

  @override
  void dispose() {
    work.dispose();
    blockers.dispose();
    objective.dispose();
    resource.dispose();
    quantity.dispose();
    unit.dispose();
    resourceCount.dispose();
    super.dispose();
  }

  Future<void> act(Future<void> Function() action) async {
    setState(() {
      busy = true;
      message = null;
    });
    try {
      await action();
    } catch (error) {
      if (mounted) setState(() => message = error.toString());
    } finally {
      await loadHistory();
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      toolbarHeight: 72,
      title: Text(
        widget.activity['name'] as String,
        maxLines: 2,
        softWrap: true,
        overflow: TextOverflow.ellipsis,
      ),
    ),
    body: SafeArea(
      child: SingleChildScrollView(
        keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 640),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (!widget.requestOnly) ...[
                  if ((widget.activity['description'] as String?)?.isNotEmpty ??
                      false)
                    Text(widget.activity['description'] as String),
                  Text(
                    '$progress% complete',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: 12),
                ],
                _history(),
                const SizedBox(height: 24),
                if (message != null) ...[
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(12),
                      child: Text(message!, softWrap: true),
                    ),
                  ),
                  const SizedBox(height: 16),
                ],
                if (!widget.requestOnly) ...[
                  Text(
                    'Progress update',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 8),
                  Slider(
                    value: progress.toDouble(),
                    min: 0,
                    max: 100,
                    divisions: 100,
                    label: '$progress%',
                    padding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 16,
                    ),
                    onChanged: busy
                        ? null
                        : (value) => setState(() => progress = value.round()),
                  ),
                  Text('$progress% complete'),
                  const SizedBox(height: 16),
                  TextField(
                    controller: work,
                    minLines: 2,
                    maxLines: 4,
                    decoration: const InputDecoration(
                      labelText: 'Work completed today',
                      alignLabelWithHint: true,
                    ),
                  ),
                  const SizedBox(height: 16),
                  TextField(
                    controller: blockers,
                    minLines: 2,
                    maxLines: 4,
                    decoration: const InputDecoration(
                      labelText: 'Issues or blockers',
                      alignLabelWithHint: true,
                    ),
                  ),
                  const SizedBox(height: 16),
                  FilledButton.icon(
                    style: FilledButton.styleFrom(
                      minimumSize: const Size.fromHeight(48),
                    ),
                    onPressed: busy
                        ? null
                        : () => act(() async {
                            if (work.text.trim().length < 2) {
                              throw Exception('Describe the work completed.');
                            }
                            await widget.service.updateProgress(
                              widget.activity['id'] as String,
                              progress,
                              work.text.trim(),
                              blockers.text.trim(),
                            );
                            if (mounted) {
                              setState(() => message = 'Progress saved.');
                            }
                          }),
                    icon: const Icon(Icons.save),
                    label: const Text(
                      'Save progress',
                      textAlign: TextAlign.center,
                    ),
                  ),
                  const SizedBox(height: 8),
                  const SizedBox(height: 24),
                  const Divider(height: 1),
                  const SizedBox(height: 24),
                ],
                Text(
                  'Resource request',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: 16),
                TextField(
                  controller: objective,
                  minLines: 2,
                  maxLines: 4,
                  decoration: const InputDecoration(
                    labelText: 'Objective',
                    alignLabelWithHint: true,
                  ),
                ),
                const SizedBox(height: 16),
                DropdownButtonFormField<String>(
                  initialValue: kind,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Resource type'),
                  items: ['Material', 'Equipment', 'Workforce']
                      .map(
                        (type) =>
                            DropdownMenuItem(value: type, child: Text(type)),
                      )
                      .toList(),
                  onChanged: busy
                      ? null
                      : (value) {
                          if (value != null) {
                            setState(() {
                              kind = value;
                              selectedResource = null;
                              resource.clear();
                              unit.text = value == 'Material'
                                  ? ''
                                  : value == 'Equipment'
                                  ? 'days'
                                  : 'mandays';
                              quantity.text = '1';
                              resourceCount.text = '1';
                            });
                          }
                        },
                ),
                const SizedBox(height: 16),
                TextField(
                  controller: resource,
                  readOnly: true,
                  onTap: busy
                      ? null
                      : () async {
                          final selected = await pickResource(
                            context,
                            widget.service,
                            kind,
                          );
                          if (!mounted || selected == null) return;
                          setState(() {
                            selectedResource = selected;
                            resource.text = selected['name'] as String;
                            if (kind == 'Material') {
                              unit.text = selected['unit'] as String;
                            }
                          });
                        },
                  decoration: InputDecoration(
                    labelText: kind == 'Workforce'
                        ? 'Worker skill'
                        : 'Resource name',
                    suffixIcon: const Icon(Icons.search),
                    helperText: 'Tap to search registered resources',
                  ),
                ),
                if (kind != 'Material' && unit.text != 'workers') ...[
                  const SizedBox(height: 16),
                  TextField(
                    controller: resourceCount,
                    keyboardType: TextInputType.number,
                    decoration: InputDecoration(
                      labelText: kind == 'Equipment'
                          ? 'Number of machines'
                          : 'Number of workers',
                    ),
                  ),
                ],
                const SizedBox(height: 16),
                LayoutBuilder(
                  builder: (context, constraints) {
                    final quantityField = TextField(
                      controller: quantity,
                      keyboardType: TextInputType.number,
                      decoration: InputDecoration(
                        labelText: kind == 'Material' || unit.text == 'workers'
                            ? 'Quantity'
                            : 'Total usage / effort',
                      ),
                    );
                    final Widget unitField = kind == 'Material'
                        ? TextField(
                            controller: unit,
                            readOnly: true,
                            decoration: const InputDecoration(
                              labelText: 'Unit',
                              suffixIcon: Icon(Icons.lock_outline),
                              hintText: 'Select material first',
                            ),
                          )
                        : DropdownButtonFormField<String>(
                            key: ValueKey('usage-unit-$kind-${unit.text}'),
                            initialValue: unit.text,
                            isExpanded: true,
                            decoration: const InputDecoration(
                              labelText: 'Unit',
                            ),
                            items:
                                (kind == 'Equipment'
                                        ? ['shifts', 'days', 'hours', 'weeks']
                                        : [
                                            'mandays',
                                            'workers',
                                            'hours',
                                            'shifts',
                                          ])
                                    .map(
                                      (value) => DropdownMenuItem(
                                        value: value,
                                        child: Text(value),
                                      ),
                                    )
                                    .toList(),
                            onChanged: busy
                                ? null
                                : (value) {
                                    if (value != null) {
                                      setState(() => unit.text = value);
                                    }
                                  },
                          );
                    if (constraints.maxWidth < 360) {
                      return Column(
                        children: [
                          quantityField,
                          const SizedBox(height: 16),
                          unitField,
                        ],
                      );
                    }
                    return Row(
                      children: [
                        Expanded(child: quantityField),
                        const SizedBox(width: 12),
                        Expanded(child: unitField),
                      ],
                    );
                  },
                ),
                if (kind != 'Material')
                  Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: Text(
                      kind == 'Equipment'
                          ? 'Total usage across all machines. Day/shift = 8 hours; week = 40 hours.'
                          : 'Total effort across all workers. Manday/shift = 8 hours. Workers is headcount only.',
                    ),
                  ),
                const SizedBox(height: 8),
                TextButton.icon(
                  onPressed: busy
                      ? null
                      : () {
                          try {
                            setState(() {
                              resources.add(currentResource());
                              resource.clear();
                              selectedResource = null;
                              if (kind == 'Material') unit.clear();
                              quantity.text = '1';
                              resourceCount.text = '1';
                              message = null;
                            });
                          } catch (error) {
                            setState(() => message = error.toString());
                          }
                        },
                  icon: const Icon(Icons.add),
                  label: const Text(
                    'Add another resource',
                    textAlign: TextAlign.center,
                  ),
                ),
                ...resources.asMap().entries.map(
                  (entry) => Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: Row(
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                '${entry.value['name']} · ${entry.value['quantity']} ${entry.value['unit']}',
                                softWrap: true,
                              ),
                              Text(
                                entry.value['kind'] as String,
                                style: Theme.of(context).textTheme.bodySmall,
                              ),
                              if (entry.value['resourceCount'] != null)
                                Text(
                                  'Resource count: ${entry.value['resourceCount']}',
                                ),
                            ],
                          ),
                        ),
                        IconButton(
                          icon: const Icon(Icons.close),
                          tooltip: 'Remove resource',
                          onPressed: () =>
                              setState(() => resources.removeAt(entry.key)),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 16),
                FilledButton.icon(
                  style: FilledButton.styleFrom(
                    minimumSize: const Size.fromHeight(48),
                  ),
                  onPressed: busy
                      ? null
                      : () => act(() async {
                          if (objective.text.trim().length < 10) {
                            throw Exception('Enter a clear objective.');
                          }
                          final items = [
                            ...resources,
                            if (resource.text.trim().isNotEmpty)
                              currentResource(),
                          ];
                          if (items.isEmpty) {
                            throw Exception('Add at least one resource.');
                          }
                          final request = await widget.service.submitRequest({
                            'projectId': widget.project['id'],
                            'siteId': widget.siteId,
                            'activityId': widget.activity['id'],
                            'objective': objective.text.trim(),
                            'items': items,
                          });
                          final workflow = await widget.service.startPlanning(
                            request['id'] as String,
                          );
                          if (mounted) {
                            setState(() {
                              workflowId = workflow['id'] as String;
                              workflowStatus = workflow['status'] as String;
                              message = workflowStatus == 'Failed'
                                  ? 'Request saved, but planning failed. Check the planning service and resource availability.'
                                  : 'Request submitted and planning started.';
                            });
                          }
                        }),
                  icon: const Icon(Icons.auto_awesome),
                  label: const Text(
                    'Submit and start planning',
                    textAlign: TextAlign.center,
                    softWrap: true,
                  ),
                ),
                if (workflowId != null) ...[
                  const SizedBox(height: 12),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 8,
                      ),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  'Planning: $workflowStatus',
                                  softWrap: true,
                                ),
                                Text(
                                  'Workflow $workflowId',
                                  softWrap: true,
                                  style: Theme.of(context).textTheme.bodySmall,
                                ),
                              ],
                            ),
                          ),
                          IconButton(
                            tooltip: 'Refresh status',
                            icon: const Icon(Icons.refresh),
                            onPressed: () => act(() async {
                              final data = await widget.service.workflow(
                                workflowId!,
                              );
                              if (mounted) {
                                setState(
                                  () =>
                                      workflowStatus = data['status'] as String,
                                );
                              }
                            }),
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    ),
  );

  Widget _history() => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      Row(
        children: [
          Expanded(
            child: Text(
              'Saved activity details',
              style: Theme.of(context).textTheme.titleLarge,
            ),
          ),
          IconButton(
            tooltip: 'Refresh saved details',
            onPressed: historyLoading || busy ? null : loadHistory,
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      if (historyLoading) const LinearProgressIndicator(),
      if (historyError != null) ...[
        Text('Could not load saved details: $historyError'),
        TextButton(onPressed: loadHistory, child: const Text('Retry history')),
      ],
      if (!historyLoading && historyError == null) ...[
        ExpansionTile(
          title: Text('Progress history (${progressHistory.length})'),
          children: [
            if (progressHistory.isEmpty)
              const ListTile(title: Text('No progress updates yet.')),
            ...progressHistory.map(
              (row) => ListTile(
                title: Text(
                  '${row['progressPercent']}% · ${row['workCompleted']}',
                ),
                subtitle: Text(
                  [
                    _date(row['createdAt']),
                    if ((row['blockers'] as String?)?.isNotEmpty ?? false)
                      'Blockers: ${row['blockers']}',
                  ].join('\n'),
                ),
              ),
            ),
          ],
        ),
        Text(
          'Resource requests (${savedRequests.length})',
          style: Theme.of(context).textTheme.titleMedium,
        ),
        if (savedRequests.isEmpty)
          const Padding(
            padding: EdgeInsets.all(12),
            child: Text('No resource requests yet.'),
          ),
        ...savedRequests.map(
          (row) => ExpansionTile(
            key: ValueKey('saved-request-${row['id']}'),
            title: Text(row['objective'] as String),
            subtitle: Text(
              '${_date(row['createdAt'])} · ${row['workflowStatus'] ?? 'Not started'}',
            ),
            onExpansionChanged: (expanded) {
              if (expanded && !requestDetails.containsKey(row['id'])) {
                _loadRequestDetails(row['id'] as String);
              }
            },
            children: [
              if (requestDetails.containsKey(row['id']))
                FutureBuilder<Map<String, dynamic>>(
                  future: requestDetails[row['id']],
                  builder: (context, snapshot) {
                    if (snapshot.hasError) {
                      return ListTile(
                        title: Text(
                          'Could not load request: ${snapshot.error}',
                        ),
                        trailing: IconButton(
                          tooltip: 'Retry request details',
                          icon: const Icon(Icons.refresh),
                          onPressed: () =>
                              _loadRequestDetails(row['id'] as String),
                        ),
                      );
                    }
                    if (!snapshot.hasData) {
                      return const LinearProgressIndicator();
                    }
                    final detail = snapshot.data!;
                    return Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          Text('Status: ${detail['status'] ?? 'Draft'}'),
                          if (detail['requiredBy'] != null)
                            Text('Required by: ${_date(detail['requiredBy'])}'),
                          if (detail['budgetLimit'] != null)
                            Text('Budget: ${detail['budgetLimit']}'),
                          if ((detail['notes'] as String?)?.isNotEmpty ?? false)
                            Text('Notes: ${detail['notes']}'),
                          ...((detail['items'] as List?) ?? []).map(
                            (item) => Padding(
                              padding: const EdgeInsets.only(top: 8),
                              child: Text(
                                '${item['kind']}: ${item['name']} · ${item['quantity']} ${item['unit']}${item['resourceCount'] == null ? '' : ' · Count: ${item['resourceCount']}'}',
                              ),
                            ),
                          ),
                        ],
                      ),
                    );
                  },
                ),
            ],
          ),
        ),
      ],
    ],
  );

  String _date(dynamic value) {
    final date = DateTime.tryParse(value?.toString() ?? '')?.toLocal();
    if (date == null) return '';
    return '${date.day}/${date.month}/${date.year}';
  }

  void _loadRequestDetails(String id) {
    final pending = widget.service.requestDetails(id);
    setState(() {
      requestDetails[id] = pending;
    });
  }
}

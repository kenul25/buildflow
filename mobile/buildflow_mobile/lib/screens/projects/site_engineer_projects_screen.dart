import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

import '../../services/project_service.dart';

class SiteEngineerProjectsScreen extends StatefulWidget {
  const SiteEngineerProjectsScreen({required this.service, super.key});
  final ProjectService service;

  @override
  State<SiteEngineerProjectsScreen> createState() => _SiteEngineerProjectsScreenState();
}

class _SiteEngineerProjectsScreenState extends State<SiteEngineerProjectsScreen> {
  List<Map<String, dynamic>> projects = [];
  bool loading = true;
  String? error;

  @override
  void initState() { super.initState(); load(); }

  Future<void> load() async {
    setState(() { loading = true; error = null; });
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
    if (error != null) return Center(child: Column(mainAxisSize: MainAxisSize.min, children: [Text(error!), TextButton(onPressed: load, child: const Text('Retry'))]));
    if (projects.isEmpty) return const Center(child: Text('No projects assigned yet.'));
    return RefreshIndicator(onRefresh: load, child: ListView.builder(
      padding: const EdgeInsets.all(16), itemCount: projects.length,
      itemBuilder: (context, index) {
        final project = projects[index];
        return Card(child: ListTile(
          leading: const CircleAvatar(child: Icon(Icons.apartment)),
          title: Text(project['name'] as String),
          subtitle: Text('${project['code'] ?? ''} · ${project['status'] ?? 'Planned'}'),
          trailing: const Icon(Icons.chevron_right),
          onTap: () => Navigator.push(context, MaterialPageRoute<void>(builder: (_) => SiteProjectDetailScreen(service: widget.service, project: project))),
        ));
      },
    ));
  }
}

class SiteProjectDetailScreen extends StatefulWidget {
  const SiteProjectDetailScreen({required this.service, required this.project, super.key});
  final ProjectService service;
  final Map<String, dynamic> project;
  @override
  State<SiteProjectDetailScreen> createState() => _SiteProjectDetailScreenState();
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
  void initState() { super.initState(); loadSites(); }

  Future<void> loadSites() async {
    setState(() { loading = true; error = null; });
    try {
      final result = await widget.service.list('sites', parentId: widget.project['id'] as String);
      if (mounted) setState(() { sites = result; siteId = result.isEmpty ? null : result.first['id'] as String; });
      if (siteId != null) await loadPhases();
    } catch (e) { if (mounted) setState(() => error = e.toString()); }
    finally { if (mounted) setState(() => loading = false); }
  }

  Future<void> loadPhases() async {
    final result = await widget.service.list('phases', parentId: siteId);
    if (mounted) setState(() { phases = result; phaseId = result.isEmpty ? null : result.first['id'] as String; activities = []; });
    if (phaseId != null) await loadActivities();
  }

  Future<void> loadActivities() async {
    final result = await widget.service.list('activities', parentId: phaseId);
    if (mounted) setState(() => activities = result);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: Text(widget.project['name'] as String)),
    body: loading ? const Center(child: CircularProgressIndicator()) : error != null ? Center(child: Column(mainAxisSize: MainAxisSize.min, children: [Text(error!), TextButton(onPressed: loadSites, child: const Text('Retry'))])) : ListView(padding: const EdgeInsets.all(16), children: [
      Text(widget.project['description'] as String? ?? 'Assigned construction project', style: Theme.of(context).textTheme.bodyLarge),
      const SizedBox(height: 16),
      if (sites.isEmpty) const Text('No sites in this project yet.'),
      if (sites.isNotEmpty) DropdownButtonFormField<String>(initialValue: siteId, decoration: const InputDecoration(labelText: 'Site'), items: sites.map((s) => DropdownMenuItem(value: s['id'] as String, child: Text(s['name'] as String))).toList(), onChanged: (value) async { setState(() => siteId = value); await loadPhases(); }),
      if (phases.isNotEmpty) DropdownButtonFormField<String>(initialValue: phaseId, decoration: const InputDecoration(labelText: 'Phase'), items: phases.map((p) => DropdownMenuItem(value: p['id'] as String, child: Text(p['name'] as String))).toList(), onChanged: (value) async { setState(() => phaseId = value); await loadActivities(); }),
      const SizedBox(height: 20),
      Text('Activities', style: Theme.of(context).textTheme.titleLarge),
      if (activities.isEmpty) const Padding(padding: EdgeInsets.all(16), child: Text('No activities available.')),
      ...activities.map((activity) => Card(child: ListTile(title: Text(activity['name'] as String), subtitle: Text('${activity['progressPercent'] ?? 0}% complete · ${activity['status'] ?? 'Planned'}'), trailing: const Icon(Icons.chevron_right), onTap: () async {
        await Navigator.push(context, MaterialPageRoute<void>(builder: (_) => SiteActivityScreen(service: widget.service, project: widget.project, siteId: siteId!, activity: activity)));
        await loadActivities();
      }))),
    ]),
  );
}

class SiteActivityScreen extends StatefulWidget {
  const SiteActivityScreen({required this.service, required this.project, required this.siteId, required this.activity, super.key});
  final ProjectService service;
  final Map<String, dynamic> project;
  final String siteId;
  final Map<String, dynamic> activity;
  @override
  State<SiteActivityScreen> createState() => _SiteActivityScreenState();
}

class _SiteActivityScreenState extends State<SiteActivityScreen> {
  final work = TextEditingController();
  final blockers = TextEditingController();
  final objective = TextEditingController();
  final resource = TextEditingController();
  final quantity = TextEditingController(text: '1');
  final unit = TextEditingController(text: 'units');
  int progress = 0;
  String kind = 'Material';
  bool busy = false;
  String? message;
  String? workflowId;
  String? workflowStatus;
  final List<Map<String, dynamic>> resources = [];

  Map<String, dynamic> currentResource() {
    final count = double.tryParse(quantity.text);
    if (resource.text.trim().length < 2 || count == null || count <= 0 || unit.text.trim().isEmpty) {
      throw Exception('Enter a valid resource name, quantity and unit.');
    }
    return {'kind': kind, 'name': resource.text.trim(), 'quantity': count, 'unit': unit.text.trim()};
  }

  @override
  void initState() { super.initState(); progress = widget.activity['progressPercent'] as int? ?? 0; }
  @override
  void dispose() { work.dispose(); blockers.dispose(); objective.dispose(); resource.dispose(); quantity.dispose(); unit.dispose(); super.dispose(); }

  Future<void> act(Future<void> Function() action) async {
    setState(() { busy = true; message = null; });
    try { await action(); } catch (error) { if (mounted) setState(() => message = error.toString()); }
    finally { if (mounted) setState(() => busy = false); }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: Text(widget.activity['name'] as String)),
    body: ListView(padding: const EdgeInsets.all(16), children: [
      if (message != null) Card(child: Padding(padding: const EdgeInsets.all(12), child: Text(message!))),
      Text('Progress update', style: Theme.of(context).textTheme.titleLarge),
      Slider(value: progress.toDouble(), min: 0, max: 100, divisions: 100, label: '$progress%', onChanged: busy ? null : (value) => setState(() => progress = value.round())),
      Text('$progress% complete'),
      TextField(controller: work, maxLines: 2, decoration: const InputDecoration(labelText: 'Work completed today')),
      TextField(controller: blockers, maxLines: 2, decoration: const InputDecoration(labelText: 'Issues or blockers')),
      const SizedBox(height: 12),
      FilledButton.icon(onPressed: busy ? null : () => act(() async {
        if (work.text.trim().length < 2) throw Exception('Describe the work completed.');
        await widget.service.updateProgress(widget.activity['id'] as String, progress, work.text.trim(), blockers.text.trim());
        if (mounted) setState(() => message = 'Progress saved.');
      }), icon: const Icon(Icons.save), label: const Text('Save progress')),
      OutlinedButton.icon(onPressed: busy ? null : () => act(() async {
        final photo = await ImagePicker().pickImage(source: ImageSource.camera, imageQuality: 75, maxWidth: 1600);
        if (photo == null) return;
        await widget.service.uploadPhoto(widget.activity['id'] as String, photo.path);
        if (mounted) setState(() => message = 'Site photo uploaded.');
      }), icon: const Icon(Icons.camera_alt_outlined), label: const Text('Upload site photo')),
      const Divider(height: 36),
      Text('Resource request', style: Theme.of(context).textTheme.titleLarge),
      TextField(controller: objective, maxLines: 2, decoration: const InputDecoration(labelText: 'Objective')),
      DropdownButtonFormField<String>(initialValue: kind, decoration: const InputDecoration(labelText: 'Resource type'), items: ['Material', 'Equipment', 'Workforce'].map((x) => DropdownMenuItem(value: x, child: Text(x))).toList(), onChanged: (value) => setState(() => kind = value!)),
      TextField(controller: resource, decoration: const InputDecoration(labelText: 'Resource name')),
      Row(children: [Expanded(child: TextField(controller: quantity, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'Quantity'))), const SizedBox(width: 12), Expanded(child: TextField(controller: unit, decoration: const InputDecoration(labelText: 'Unit')))]),
      TextButton.icon(onPressed: busy ? null : () {
        try { setState(() { resources.add(currentResource()); resource.clear(); quantity.text = '1'; message = null; }); }
        catch (error) { setState(() => message = error.toString()); }
      }, icon: const Icon(Icons.add), label: const Text('Add another resource')),
      ...resources.asMap().entries.map((entry) => ListTile(
        title: Text('${entry.value['name']} · ${entry.value['quantity']} ${entry.value['unit']}'),
        subtitle: Text(entry.value['kind'] as String),
        trailing: IconButton(icon: const Icon(Icons.close), tooltip: 'Remove resource', onPressed: () => setState(() => resources.removeAt(entry.key))),
      )),
      const SizedBox(height: 12),
      FilledButton.icon(onPressed: busy ? null : () => act(() async {
        if (objective.text.trim().length < 10) throw Exception('Enter a clear objective.');
        final items = [...resources, if (resource.text.trim().isNotEmpty) currentResource()];
        if (items.isEmpty) throw Exception('Add at least one resource.');
        final request = await widget.service.submitRequest({'projectId': widget.project['id'], 'siteId': widget.siteId, 'activityId': widget.activity['id'], 'objective': objective.text.trim(), 'items': items});
        final workflow = await widget.service.startPlanning(request['id'] as String);
        if (mounted) setState(() { workflowId = workflow['id'] as String; workflowStatus = workflow['status'] as String; message = 'Request submitted and planning started.'; });
      }), icon: const Icon(Icons.auto_awesome), label: const Text('Submit and start planning')),
      if (workflowId != null) Card(child: ListTile(title: Text('Planning: $workflowStatus'), subtitle: Text('Workflow $workflowId'), trailing: IconButton(tooltip: 'Refresh status', icon: const Icon(Icons.refresh), onPressed: () => act(() async {
        final data = await widget.service.workflow(workflowId!);
        if (mounted) setState(() => workflowStatus = data['status'] as String);
      })))),
    ]),
  );
}

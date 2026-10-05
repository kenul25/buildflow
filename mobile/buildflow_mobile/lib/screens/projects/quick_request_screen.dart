import 'package:flutter/material.dart';

import '../../services/project_service.dart';
import 'site_engineer_projects_screen.dart';

class QuickRequestScreen extends StatefulWidget {
  const QuickRequestScreen({required this.service, super.key});
  final ProjectService service;
  @override
  State<QuickRequestScreen> createState() => _QuickRequestScreenState();
}

class _QuickRequestScreenState extends State<QuickRequestScreen> {
  late Future<List<Map<String, dynamic>>> _activities;
  @override
  void initState() {
    super.initState();
    _activities = _load();
  }

  Future<List<Map<String, dynamic>>> _load() async {
    final records = await Future.wait(
      [
        'projects',
        'sites',
        'phases',
        'activities',
      ].map((kind) => widget.service.list(kind)),
    );
    final projects = {for (final row in records[0]) row['id']: row};
    final sites = {for (final row in records[1]) row['id']: row};
    final phases = {for (final row in records[2]) row['id']: row};
    final options = <Map<String, dynamic>>[];
    for (final activity in records[3]) {
      if (['Completed', 'Cancelled'].contains(activity['status'])) {
        continue;
      }
      final phase = phases[activity['parentId']];
      final site = sites[phase?['parentId']];
      final project = projects[site?['parentId']];
      if (site == null || project == null) {
        continue;
      }
      options.add({'activity': activity, 'site': site, 'project': project});
    }
    return options;
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('New request')),
    body: SafeArea(
      child: FutureBuilder<List<Map<String, dynamic>>>(
        future: _activities,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      'Could not load activities. ${snapshot.error}',
                      textAlign: TextAlign.center,
                    ),
                    TextButton(
                      onPressed: () => setState(() => _activities = _load()),
                      child: const Text('Retry'),
                    ),
                  ],
                ),
              ),
            );
          }
          final options = snapshot.data!;
          if (options.isEmpty) {
            return const Center(
              child: Padding(
                padding: EdgeInsets.all(24),
                child: Text(
                  'No available activities. Ask your manager to assign a project and create an activity.',
                  textAlign: TextAlign.center,
                ),
              ),
            );
          }
          return ListView(
            padding: const EdgeInsets.all(20),
            children: [
              Text(
                'Choose an activity',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: 8),
              const Text(
                'Your request will be linked to its project and site.',
              ),
              const SizedBox(height: 16),
              ...options.map(
                (option) => Card(
                  child: ListTile(
                    leading: const Icon(Icons.add_task),
                    title: Text(option['activity']['name'] as String),
                    subtitle: Text(
                      '${option['project']['name']} · ${option['site']['name']}',
                    ),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => Navigator.of(context).push(
                      MaterialPageRoute<void>(
                        builder: (_) => SiteActivityScreen(
                          service: widget.service,
                          project: option['project'] as Map<String, dynamic>,
                          siteId: option['site']['id'] as String,
                          activity: option['activity'] as Map<String, dynamic>,
                          requestOnly: true,
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ],
          );
        },
      ),
    ),
  );
}

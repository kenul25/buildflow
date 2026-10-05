import 'package:flutter/material.dart';

import '../../services/api_service.dart';
import 'operations_navigation.dart';

class HomeOverview extends StatefulWidget {
  const HomeOverview({
    required this.api,
    required this.onProjects,
    required this.onSchedule,
    super.key,
  });
  final ApiService api;
  final VoidCallback onProjects;
  final VoidCallback onSchedule;
  @override
  State<HomeOverview> createState() => _HomeOverviewState();
}

class _HomeOverviewState extends State<HomeOverview> {
  late Future<dynamic> summary;
  @override
  void initState() {
    super.initState();
    summary = widget.api.request('GET', '/dashboard');
  }

  Future<void> refresh() async {
    final pending = widget.api.request('GET', '/dashboard');
    setState(() => summary = pending);
    try {
      await pending;
    } catch (_) {
      /* FutureBuilder shows the retry state. */
    }
  }

  @override
  Widget build(BuildContext context) => RefreshIndicator(
    onRefresh: refresh,
    child: FutureBuilder<dynamic>(
      future: summary,
      builder: (context, snapshot) {
        final data = snapshot.data as Map<String, dynamic>?;
        final theme = Theme.of(context);
        return ListView(
          padding: const EdgeInsets.fromLTRB(20, 12, 20, 40),
          physics: const AlwaysScrollableScrollPhysics(),
          children: [
            Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 720),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(
                      'Your site, at a glance',
                      style: theme.textTheme.headlineSmall?.copyWith(
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      'Keep today’s work moving.',
                      style: theme.textTheme.bodyMedium?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                    const SizedBox(height: 20),
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            'Operations overview',
                            style: theme.textTheme.titleMedium?.copyWith(
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                        IconButton(
                          tooltip: 'Refresh overview',
                          onPressed: refresh,
                          icon: const Icon(
                            Icons.refresh,
                            color: OperationsNavigation.blue,
                          ),
                        ),
                      ],
                    ),
                    if (snapshot.connectionState == ConnectionState.waiting)
                      const LinearProgressIndicator(
                        color: OperationsNavigation.blue,
                      ),
                    if (snapshot.hasError)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: Text(
                          'Could not load your summary. Pull down or tap refresh to retry.',
                          style: TextStyle(color: theme.colorScheme.error),
                        ),
                      ),
                    LayoutBuilder(
                      builder: (context, constraints) {
                        final height =
                            158 * MediaQuery.textScalerOf(context).scale(1);
                        return GridView.count(
                          crossAxisCount: 2,
                          shrinkWrap: true,
                          physics: const NeverScrollableScrollPhysics(),
                          mainAxisSpacing: 12,
                          crossAxisSpacing: 12,
                          childAspectRatio:
                              ((constraints.maxWidth - 12) / 2) / height,
                          children: [
                            _Metric(
                              label: 'Active projects',
                              value: data?['activeProjects'],
                              icon: Icons.apartment_outlined,
                            ),
                            _Metric(
                              label: 'Active activities',
                              value: data?['activeActivities'],
                              icon: Icons.task_alt_outlined,
                            ),
                            _Metric(
                              label: 'Pending requests',
                              value: data?['pendingRequests'],
                              icon: Icons.pending_actions_outlined,
                            ),
                            _Metric(
                              label: 'Approved plans',
                              value: data?['approvedPlans'],
                              icon: Icons.verified_outlined,
                            ),
                          ],
                        );
                      },
                    ),
                    const SizedBox(height: 26),
                    Text(
                      'Quick access',
                      style: theme.textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 10),
                    _Shortcut(
                      title: 'Project activities',
                      subtitle: 'Update progress and review your site work',
                      icon: Icons.engineering_outlined,
                      onTap: widget.onProjects,
                    ),
                    const SizedBox(height: 10),
                    _Shortcut(
                      title: 'Your schedule',
                      subtitle: 'Check assignments and planned work',
                      icon: Icons.calendar_month_outlined,
                      onTap: widget.onSchedule,
                    ),
                    const SizedBox(height: 20),
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Icon(
                          Icons.add_circle_outline,
                          size: 18,
                          color: OperationsNavigation.blue,
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            'Need resources? Tap + to create a request for your activity.',
                            style: theme.textTheme.bodySmall?.copyWith(
                              color: theme.colorScheme.onSurfaceVariant,
                              height: 1.5,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ],
        );
      },
    ),
  );
}

class _Metric extends StatelessWidget {
  const _Metric({required this.label, required this.value, required this.icon});
  final String label;
  final dynamic value;
  final IconData icon;
  @override
  Widget build(BuildContext context) => Card(
    margin: EdgeInsets.zero,
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(
              color: OperationsNavigation.blue.withValues(alpha: .08),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Icon(icon, size: 22, color: OperationsNavigation.blue),
          ),
          const Spacer(),
          Text(
            value?.toString() ?? '—',
            style: Theme.of(context).textTheme.headlineSmall
                ?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 4),
          Text(
            label,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
              color: Theme.of(context).colorScheme.onSurfaceVariant,
              height: 1.4,
            ),
          ),
        ],
      ),
    ),
  );
}

class _Shortcut extends StatelessWidget {
  const _Shortcut({
    required this.title,
    required this.subtitle,
    required this.icon,
    required this.onTap,
  });
  final String title, subtitle;
  final IconData icon;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => Card(
    margin: EdgeInsets.zero,
    child: ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      leading: Icon(icon, color: OperationsNavigation.blue),
      title: Text(title, style: const TextStyle(fontWeight: FontWeight.w700)),
      subtitle: Text(subtitle),
      trailing: const Icon(Icons.chevron_right, size: 20),
      onTap: onTap,
    ),
  );
}

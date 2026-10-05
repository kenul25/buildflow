import 'package:flutter/material.dart';

import '../../providers/auth_provider.dart';
import '../../providers/theme_provider.dart';
import '../../services/project_service.dart';
import '../../services/api_service.dart';
import '../../services/inventory_service.dart';
import '../../services/procurement_service.dart';
import '../../services/scheduling_service.dart';
import '../scheduling/scheduling_screen.dart';
import 'operations_navigation.dart';
import '../projects/quick_request_screen.dart';
import '../projects/site_engineer_projects_screen.dart';
import '../projects/site_requests_screen.dart';
import '../inventory/inventory_screen.dart';
import '../procurement/procurement_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({
    required this.authProvider,
    required this.themeProvider,
    required this.projectService,
    required this.inventoryService,
    required this.procurementService,
    super.key,
  });
  final AuthProvider authProvider;
  final ThemeProvider themeProvider;
  final ProjectService projectService;
  final InventoryService inventoryService;
  final ProcurementService procurementService;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _index = 0;
  static const _labels = ['Home', 'Projects', 'Schedule', 'More'];

  void _open(String title, Widget page) => Navigator.of(context).push(
    MaterialPageRoute<void>(
      builder: (_) => Scaffold(
        appBar: AppBar(title: Text(title)),
        body: SafeArea(child: page),
      ),
    ),
  );

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Text(
        _labels[_index],
        style: const TextStyle(fontWeight: FontWeight.w800),
      ),
    ),
    body: SafeArea(child: _page()),
    floatingActionButton: RequestActionButton(
      onPressed: () => Navigator.of(context).push(
        MaterialPageRoute<void>(
          builder: (_) => QuickRequestScreen(service: widget.projectService),
        ),
      ),
    ),
    floatingActionButtonLocation: const RequestActionLocation(),
    bottomNavigationBar: OperationsNavigation(
      selectedIndex: _index,
      onSelect: (index) => setState(() => _index = index),
    ),
  );

  Widget _page() => switch (_index) {
    0 => _Overview(
      userName: widget.authProvider.user!.fullName,
      api: widget.projectService.api,
    ),
    1 => SiteEngineerProjectsScreen(service: widget.projectService),
    2 => SchedulingScreen(
      service: SchedulingService(widget.projectService.api),
    ),
    _ => ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Text(
          'More operations',
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const SizedBox(height: 16),
        _more(
          'Materials',
          'Stock and material availability',
          Icons.inventory_2_outlined,
          () => _open(
            'Materials',
            InventoryScreen(service: widget.inventoryService),
          ),
        ),
        _more(
          'Requests',
          'Track requests and AI planning',
          Icons.assignment_outlined,
          () => _open(
            'Requests',
            SiteRequestsScreen(service: widget.projectService),
          ),
        ),
        _more(
          'Procurement',
          'Deliveries and receipt confirmation',
          Icons.local_shipping_outlined,
          () => _open(
            'Procurement',
            ProcurementScreen(
              service: widget.procurementService,
              canApprove: widget.authProvider.user!.roles.any(
                (role) => ['Administrator', 'ProjectManager'].contains(role),
              ),
            ),
          ),
        ),
        _more(
          'Profile',
          'Account and appearance',
          Icons.person_outline,
          () => _open(
            'Profile',
            _Profile(
              authProvider: widget.authProvider,
              themeProvider: widget.themeProvider,
            ),
          ),
        ),
      ],
    ),
  };

  Widget _more(
    String title,
    String subtitle,
    IconData icon,
    VoidCallback onTap,
  ) => Card(
    child: ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      leading: Icon(icon, color: OperationsNavigation.blue),
      title: Text(title),
      subtitle: Text(subtitle),
      trailing: const Icon(Icons.chevron_right),
      onTap: onTap,
    ),
  );
}

class _Overview extends StatefulWidget {
  const _Overview({required this.userName, required this.api});
  final String userName;
  final ApiService api;
  @override
  State<_Overview> createState() => _OverviewState();
}

class _OverviewState extends State<_Overview> {
  late Future<dynamic> _summary;
  @override
  void initState() {
    super.initState();
    _summary = widget.api.request('GET', '/dashboard');
  }

  @override
  Widget build(BuildContext context) => FutureBuilder<dynamic>(
    future: _summary,
    builder: (context, snapshot) {
      final data = snapshot.data as Map<String, dynamic>?;
      return ListView(
        padding: const EdgeInsets.all(20),
        children: [
          Text(
            'Good day, ${widget.userName.split(' ').first}',
            style: Theme.of(context).textTheme.headlineSmall,
          ),
          const SizedBox(height: 12),
          const Text('Your current site operations summary.'),
          if (snapshot.hasError) Text('${snapshot.error}'),
          TextButton(
            onPressed: () => setState(
              () => _summary = widget.api.request('GET', '/dashboard'),
            ),
            child: const Text('Refresh'),
          ),
          GridView.count(
            crossAxisCount: 2,
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            mainAxisSpacing: 12,
            crossAxisSpacing: 12,
            childAspectRatio: 1.25,
            children: [
              _SummaryCard(
                label: 'Active projects',
                value: '${data?['activeProjects'] ?? '…'}',
                icon: Icons.apartment,
              ),
              _SummaryCard(
                label: 'Active activities',
                value: '${data?['activeActivities'] ?? '…'}',
                icon: Icons.task_alt,
              ),
              _SummaryCard(
                label: 'Pending requests',
                value: '${data?['pendingRequests'] ?? '…'}',
                icon: Icons.pending_actions,
              ),
              _SummaryCard(
                label: 'Approved plans',
                value: '${data?['approvedPlans'] ?? '…'}',
                icon: Icons.verified_outlined,
              ),
            ],
          ),
          const SizedBox(height: 24),
          const Card(
            child: ListTile(
              leading: Icon(Icons.construction),
              title: Text('Site operations'),
              subtitle: Text(
                'Use + to create a request, Projects for progress, Schedule for assignments, and More for materials and deliveries.',
              ),
            ),
          ),
        ],
      );
    },
  );
}

class _SummaryCard extends StatelessWidget {
  const _SummaryCard({
    required this.label,
    required this.value,
    required this.icon,
  });
  final String label;
  final String value;
  final IconData icon;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: Theme.of(context).colorScheme.primary),
          const Spacer(),
          Text(
            value,
            style: Theme.of(context).textTheme.headlineSmall
                ?.copyWith(fontWeight: FontWeight.w800),
          ),
          Text(label, style: Theme.of(context).textTheme.bodySmall),
        ],
      ),
    ),
  );
}

class _Profile extends StatelessWidget {
  const _Profile({required this.authProvider, required this.themeProvider});
  final AuthProvider authProvider;
  final ThemeProvider themeProvider;
  @override
  Widget build(BuildContext context) {
    final user = authProvider.user!;
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Row(
              children: [
                CircleAvatar(
                  radius: 28,
                  child: Text(user.fullName[0].toUpperCase()),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        user.fullName,
                        style: Theme.of(context).textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.w700),
                      ),
                      Text(user.email),
                      Text(
                        user.roles.join(', '),
                        style: TextStyle(
                          color: Theme.of(context).colorScheme.primary,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 18),
        Text(
          'Appearance',
          style: Theme.of(context).textTheme.titleMedium
              ?.copyWith(fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 8),
        SegmentedButton<ThemeMode>(
          segments: const [
            ButtonSegment(value: ThemeMode.system, label: Text('System')),
            ButtonSegment(value: ThemeMode.light, label: Text('Light')),
            ButtonSegment(value: ThemeMode.dark, label: Text('Dark')),
          ],
          selected: {themeProvider.mode},
          onSelectionChanged: (value) => themeProvider.setMode(value.first),
        ),
        const SizedBox(height: 28),
        OutlinedButton.icon(
          onPressed: authProvider.logout,
          icon: const Icon(Icons.logout),
          label: const Text('Sign out'),
          style: OutlinedButton.styleFrom(
            minimumSize: const Size.fromHeight(50),
          ),
        ),
      ],
    );
  }
}

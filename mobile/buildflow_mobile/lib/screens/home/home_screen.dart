import 'dart:async';

import 'package:flutter/material.dart';

import '../../providers/auth_provider.dart';
import '../../providers/theme_provider.dart';
import '../../services/project_service.dart';
import '../../services/notification_service.dart';
import '../../services/inventory_service.dart';
import '../../services/procurement_service.dart';
import '../../services/scheduling_service.dart';
import '../scheduling/scheduling_screen.dart';
import 'operations_navigation.dart';
import 'home_greeting.dart';
import 'home_overview.dart';
import 'notifications_screen.dart';
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

class _HomeScreenState extends State<HomeScreen> with WidgetsBindingObserver {
  int _index = 0;
  static const _labels = ['Home', 'Projects', 'Schedule', 'More'];
  late final NotificationService notifications;
  Timer? notificationTimer;
  int unread = 0;
  bool loadingNotifications = false;
  @override
  void initState() {
    super.initState();
    notifications = NotificationService(widget.projectService.api);
    WidgetsBinding.instance.addObserver(this);
    refreshNotifications();
    notificationTimer = Timer.periodic(const Duration(seconds: 30), (_) {
      if (WidgetsBinding.instance.lifecycleState == AppLifecycleState.resumed) {
        refreshNotifications();
      }
    });
  }

  Future<void> refreshNotifications() async {
    if (loadingNotifications) return;
    loadingNotifications = true;
    try {
      final result = await notifications.load();
      if (mounted) {
        setState(() => unread = (result['unreadCount'] as num).toInt());
      }
    } catch (_) {
      /* The inbox provides an explicit error and retry state. */
    } finally {
      loadingNotifications = false;
    }
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) refreshNotifications();
  }

  @override
  void dispose() {
    notificationTimer?.cancel();
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  Future<void> openNotifications() async {
    await Navigator.push(
      context,
      MaterialPageRoute<void>(
        builder: (_) => NotificationsScreen(
          service: notifications,
          projects: widget.projectService,
        ),
      ),
    );
    if (mounted) await refreshNotifications();
  }

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
      toolbarHeight: _index == 0 ? 76 : null,
      title: _index == 0
          ? HomeGreeting(
              fullName: widget.authProvider.user!.fullName,
              roles: widget.authProvider.user!.roles,
            )
          : Text(
              _labels[_index],
              style: const TextStyle(fontWeight: FontWeight.w800),
            ),
      actions: [
        Padding(
          padding: const EdgeInsets.only(right: 16),
          child: Center(
            child: Badge(
              isLabelVisible: unread > 0,
              backgroundColor: OperationsNavigation.blue,
              label: Text(unread > 99 ? '99+' : '$unread'),
              child: IconButton(
                tooltip: unread > 0
                    ? 'Notifications, $unread unread'
                    : 'Notifications',
                onPressed: openNotifications,
                icon: const Icon(
                  Icons.notifications_none_rounded,
                  color: OperationsNavigation.blue,
                ),
              ),
            ),
          ),
        ),
      ],
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
    0 => HomeOverview(
      api: widget.projectService.api,
      onProjects: () => setState(() => _index = 1),
      onSchedule: () => setState(() => _index = 2),
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

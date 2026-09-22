import 'package:flutter/material.dart';

import '../../providers/auth_provider.dart';
import '../../providers/theme_provider.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({
    required this.authProvider,
    required this.themeProvider,
    super.key,
  });
  final AuthProvider authProvider;
  final ThemeProvider themeProvider;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _index = 0;
  static const _labels = [
    'Home',
    'Projects',
    'Requests',
    'Notifications',
    'Profile',
  ];
  static const _icons = [
    Icons.home_outlined,
    Icons.apartment_outlined,
    Icons.add_box_outlined,
    Icons.notifications_none,
    Icons.person_outline,
  ];

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Text(
        _labels[_index],
        style: const TextStyle(fontWeight: FontWeight.w800),
      ),
      actions: _index == 0
          ? [
              IconButton(
                onPressed: () => setState(() => _index = 3),
                icon: const Icon(Icons.notifications_none),
                tooltip: 'Notifications',
              ),
            ]
          : null,
    ),
    body: SafeArea(child: _page()),
    bottomNavigationBar: NavigationBar(
      selectedIndex: _index,
      onDestinationSelected: (value) => setState(() => _index = value),
      destinations: List.generate(
        _labels.length,
        (index) => NavigationDestination(
          icon: Icon(_icons[index]),
          label: _labels[index],
        ),
      ),
    ),
  );

  Widget _page() => switch (_index) {
    0 => _Overview(userName: widget.authProvider.user!.fullName),
    4 => _Profile(
      authProvider: widget.authProvider,
      themeProvider: widget.themeProvider,
    ),
    _ => _EmptyModule(title: _labels[_index], icon: _icons[_index]),
  };
}

class _Overview extends StatelessWidget {
  const _Overview({required this.userName});
  final String userName;

  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(20),
    children: [
      Text(
        'Good day, ${userName.split(' ').first}',
        style: Theme.of(context).textTheme.headlineSmall
            ?.copyWith(fontWeight: FontWeight.w800),
      ),
      const SizedBox(height: 6),
      Text(
        'Here is your site operations summary.',
        style: TextStyle(color: Theme.of(context).colorScheme.onSurfaceVariant),
      ),
      const SizedBox(height: 24),
      GridView.count(
        crossAxisCount: 2,
        shrinkWrap: true,
        physics: const NeverScrollableScrollPhysics(),
        mainAxisSpacing: 12,
        crossAxisSpacing: 12,
        childAspectRatio: 1.25,
        children: const [
          _SummaryCard(
            label: 'Active projects',
            value: '—',
            icon: Icons.apartment,
          ),
          _SummaryCard(label: 'Tasks today', value: '—', icon: Icons.task_alt),
          _SummaryCard(
            label: 'Pending requests',
            value: '—',
            icon: Icons.pending_actions,
          ),
          _SummaryCard(
            label: 'Approved plans',
            value: '—',
            icon: Icons.verified_outlined,
          ),
        ],
      ),
      const SizedBox(height: 24),
      Text(
        'Quick actions',
        style: Theme.of(context).textTheme.titleLarge
            ?.copyWith(fontWeight: FontWeight.w700),
      ),
      const SizedBox(height: 12),
      const Card(
        child: ListTile(
          leading: Icon(Icons.construction),
          title: Text('Operational modules are ready to connect'),
          subtitle: Text(
            'Projects, requests and progress will be implemented in their feature milestones.',
          ),
        ),
      ),
    ],
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

class _EmptyModule extends StatelessWidget {
  const _EmptyModule({required this.title, required this.icon});
  final String title;
  final IconData icon;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 54, color: Theme.of(context).colorScheme.primary),
          const SizedBox(height: 14),
          Text(
            '$title module',
            style: Theme.of(context).textTheme.titleLarge
                ?.copyWith(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 6),
          Text(
            'This authenticated area is reserved for the next project milestone.',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: Theme.of(context).colorScheme.onSurfaceVariant,
            ),
          ),
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

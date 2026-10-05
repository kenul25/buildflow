import 'package:flutter/material.dart';

import 'operations_navigation.dart';

class HomeGreeting extends StatelessWidget {
  const HomeGreeting({required this.fullName, required this.roles, super.key});
  final String fullName;
  final List<String> roles;
  @override
  Widget build(BuildContext context) {
    final names = fullName
        .trim()
        .split(RegExp(r'\s+'))
        .where((part) => part.isNotEmpty)
        .toList();
    final initials = names.isEmpty
        ? 'U'
        : '${names.first.characters.first}${names.length > 1 ? names.last.characters.first : ''}'
              .toUpperCase();
    final role = roles.isEmpty
        ? 'Team member'
        : ({
                'SiteEngineer': 'Site Engineer',
                'ProjectManager': 'Project Manager',
                'Administrator': 'Administrator',
                'InventoryOfficer': 'Inventory Officer',
                'ProcurementOfficer': 'Procurement Officer',
              })[roles.first] ??
              roles.first;
    return Row(
      children: [
        CircleAvatar(
          radius: 22,
          backgroundColor: OperationsNavigation.blue.withValues(alpha: .1),
          child: Padding(
            padding: const EdgeInsets.all(6),
            child: FittedBox(
              child: Text(
                initials,
                style: const TextStyle(
                  color: OperationsNavigation.blue,
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Hi, ${names.isEmpty ? 'there' : names.first}',
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 3),
              Text(
                role,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  fontSize: 12,
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

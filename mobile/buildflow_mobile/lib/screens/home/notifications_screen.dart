import 'package:flutter/material.dart';

import '../../services/notification_service.dart';
import '../../services/project_service.dart';
import 'operations_navigation.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({
    required this.service,
    required this.projects,
    super.key,
  });
  final NotificationService service;
  final ProjectService projects;
  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  List<Map<String, dynamic>> items = [];
  bool loading = true, saving = false;
  String? error;
  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      final result = await widget.service.load();
      if (mounted) {
        setState(() {
          items = (result['items'] as List).cast<Map<String, dynamic>>();
          error = null;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(
          () => error = 'Could not load notifications. Please try again.',
        );
      }
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> markAll() async {
    setState(() => saving = true);
    try {
      await widget.service.markAllRead();
      if (mounted) {
        setState(() {
          items = items.map((item) => {...item, 'isRead': true}).toList();
        });
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Could not mark notifications read. Try again.'),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  Future<void> open(Map<String, dynamic> item) async {
    if (saving) return;
    setState(() => saving = true);
    try {
      await widget.service.markRead(item['id'] as String);
      if (!mounted) return;
      setState(() => item['isRead'] = true);
      await Navigator.push(
        context,
        MaterialPageRoute<void>(
          builder: (_) =>
              _NotificationDetail(item: item, service: widget.projects),
        ),
      );
      await load();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Could not open notification. Please retry.'),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Notifications'),
      actions: [
        IconButton(
          tooltip: 'Refresh notifications',
          onPressed: load,
          icon: const Icon(Icons.refresh),
        ),
      ],
    ),
    body: SafeArea(
      child: loading
          ? const Center(child: CircularProgressIndicator())
          : error != null
          ? Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(error!),
                  TextButton(onPressed: load, child: const Text('Retry')),
                ],
              ),
            )
          : RefreshIndicator(
              onRefresh: load,
              child: ListView(
                padding: const EdgeInsets.all(20),
                physics: const AlwaysScrollableScrollPhysics(),
                children: [
                  Row(
                    children: [
                      const Expanded(
                        child: Text(
                          'Recent plan updates',
                          style: TextStyle(fontWeight: FontWeight.w700),
                        ),
                      ),
                      TextButton(
                        onPressed:
                            saving ||
                                !items.any((item) => item['isRead'] != true)
                            ? null
                            : markAll,
                        child: const Text('Mark all read'),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  if (items.isEmpty)
                    const Padding(
                      padding: EdgeInsets.symmetric(vertical: 80),
                      child: Column(
                        children: [
                          Icon(
                            Icons.notifications_none,
                            size: 40,
                            color: OperationsNavigation.blue,
                          ),
                          SizedBox(height: 12),
                          Text('You’re all caught up'),
                          SizedBox(height: 6),
                          Text(
                            'Plan updates will appear here.',
                            textAlign: TextAlign.center,
                          ),
                        ],
                      ),
                    ),
                  ...items.map(
                    (item) => Card(
                      child: ListTile(
                        contentPadding: const EdgeInsets.all(16),
                        leading: Icon(
                          item['status'] == 'Approved'
                              ? Icons.check_circle_outline
                              : item['status'] == 'Failed' ||
                                    item['status'] == 'Rejected'
                              ? Icons.info_outline
                              : Icons.assignment_outlined,
                          color: OperationsNavigation.blue,
                        ),
                        title: Text(
                          item['title'] as String,
                          style: TextStyle(
                            fontWeight: item['isRead'] == true
                                ? FontWeight.w500
                                : FontWeight.w800,
                          ),
                        ),
                        subtitle: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const SizedBox(height: 5),
                            Text(item['message'] as String),
                            const SizedBox(height: 8),
                            Text(
                              _date(item['createdAt']),
                              style: Theme.of(context).textTheme.bodySmall,
                            ),
                          ],
                        ),
                        trailing: item['isRead'] == true
                            ? const Icon(Icons.chevron_right, size: 18)
                            : Semantics(
                                label: 'Unread',
                                child: Icon(
                                  Icons.circle,
                                  size: 8,
                                  color: OperationsNavigation.blue,
                                ),
                              ),
                        onTap: saving ? null : () => open(item),
                      ),
                    ),
                  ),
                ],
              ),
            ),
    ),
  );
}

String _date(dynamic value) {
  final date = DateTime.tryParse(value?.toString() ?? '')?.toLocal();
  if (date == null) return '';
  return '${date.day}/${date.month}/${date.year} · ${date.hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')}';
}

class _NotificationDetail extends StatefulWidget {
  const _NotificationDetail({required this.item, required this.service});
  final Map<String, dynamic> item;
  final ProjectService service;
  @override
  State<_NotificationDetail> createState() => _NotificationDetailState();
}

class _NotificationDetailState extends State<_NotificationDetail> {
  late Future<List<Map<String, dynamic>>> data;
  @override
  void initState() {
    super.initState();
    data = fetch();
  }

  Future<List<Map<String, dynamic>>> fetch() => Future.wait([
    widget.service.requestDetails(widget.item['requestId'] as String),
    widget.service.workflow(widget.item['workflowId'] as String),
  ]);
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Plan update')),
    body: SafeArea(
      child: FutureBuilder<List<Map<String, dynamic>>>(
        future: data,
        builder: (context, snapshot) {
          if (snapshot.hasError) {
            return Center(
              child: TextButton(
                onPressed: () => setState(() => data = fetch()),
                child: const Text('Could not load plan. Retry'),
              ),
            );
          }
          if (!snapshot.hasData) {
            return const Center(child: CircularProgressIndicator());
          }
          final request = snapshot.data![0], workflow = snapshot.data![1];
          final status = (workflow['status'] as String).replaceAllMapped(
            RegExp(r'([a-z])([A-Z])'),
            (match) => '${match[1]} ${match[2]}',
          );
          return ListView(
            padding: const EdgeInsets.all(20),
            children: [
              Text(
                widget.item['title'] as String,
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: 12),
              Text(
                'Current status: $status',
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 20),
              Text(request['objective'] as String),
              const SizedBox(height: 20),
              Text(
                'Requested resources',
                style: Theme.of(context).textTheme.titleMedium,
              ),
              ...(request['items'] as List).map(
                (item) => ListTile(
                  contentPadding: EdgeInsets.zero,
                  title: Text(item['name'] as String),
                  subtitle: Text('${item['quantity']} ${item['unit']}'),
                ),
              ),
              if (workflow['status'] == 'Approved')
                const Text(
                  'Review your schedule and coordinate with the warehouse before starting work.',
                ),
              if (workflow['status'] == 'PendingProjectManagerApproval')
                const Text(
                  'The manager can review and approve this plan in the web app’s Plans & approvals section.',
                ),
              if (workflow['status'] == 'Failed')
                const Text(
                  'Planning needs attention. Review the saved request and planning service before retrying.',
                ),
            ],
          );
        },
      ),
    ),
  );
}

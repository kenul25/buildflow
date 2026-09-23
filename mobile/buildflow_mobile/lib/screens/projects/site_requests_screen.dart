import 'package:flutter/material.dart';

import '../../services/project_service.dart';

class SiteRequestsScreen extends StatefulWidget {
  const SiteRequestsScreen({required this.service, super.key});
  final ProjectService service;
  @override
  State<SiteRequestsScreen> createState() => _SiteRequestsScreenState();
}

class _SiteRequestsScreenState extends State<SiteRequestsScreen> {
  List<Map<String, dynamic>> requests = [];
  bool loading = true;
  String? error;

  @override
  void initState() { super.initState(); refresh(); }

  Future<void> refresh() async {
    setState(() { loading = true; error = null; });
    try {
      final result = await widget.service.requests();
      if (mounted) setState(() => requests = result);
    } catch (e) { if (mounted) setState(() => error = e.toString()); }
    finally { if (mounted) setState(() => loading = false); }
  }

  @override
  Widget build(BuildContext context) {
    if (loading) return const Center(child: CircularProgressIndicator());
    if (error != null) return Center(child: Column(mainAxisSize: MainAxisSize.min, children: [Text(error!), TextButton(onPressed: refresh, child: const Text('Retry'))]));
    if (requests.isEmpty) return const Center(child: Text('No resource requests yet. Open an activity to submit one.'));
    return RefreshIndicator(onRefresh: refresh, child: ListView.builder(padding: const EdgeInsets.all(16), itemCount: requests.length, itemBuilder: (context, index) {
      final request = requests[index];
      return Card(child: ListTile(
        leading: const Icon(Icons.inventory_2_outlined),
        title: Text(request['objective'] as String),
        subtitle: Text('Planning: ${request['workflowStatus'] ?? 'Not started'}'),
        trailing: request['workflowId'] == null || request['workflowStatus'] == 'Failed' ? IconButton(tooltip: request['workflowId'] == null ? 'Start planning' : 'Retry planning', icon: const Icon(Icons.play_arrow), onPressed: () async {
          try { await widget.service.startPlanning(request['id'] as String); await refresh(); }
          catch (e) { if (context.mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString()))); }
        }) : IconButton(tooltip: 'Refresh workflow', icon: const Icon(Icons.refresh), onPressed: refresh),
      ));
    }));
  }
}

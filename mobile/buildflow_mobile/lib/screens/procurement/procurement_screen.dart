import 'package:flutter/material.dart';

import '../../services/procurement_service.dart';

class ProcurementScreen extends StatefulWidget {
  const ProcurementScreen({
    required this.service,
    super.key,
  });

  final ProcurementService service;

  @override
  State<ProcurementScreen> createState() => _ProcurementScreenState();
}

class _ProcurementScreenState extends State<ProcurementScreen> {
  int _selectedTab = 0;

  bool _loading = true;
  String? _error;

  List<Map<String, dynamic>> _purchaseRequests = [];
  List<Map<String, dynamic>> _purchaseOrders = [];
  List<Map<String, dynamic>> _deliveries = [];

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final requests = await widget.service.purchaseRequests();
      final orders = await widget.service.purchaseOrders();
      final deliveries = await widget.service.deliveries();

      if (!mounted) return;

      setState(() {
        _purchaseRequests = requests;
        _purchaseOrders = orders;
        _deliveries = deliveries;
        _loading = false;
      });
    } catch (e) {
      if (!mounted) return;

      setState(() {
        _error = e.toString();
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        _buildTabs(),
        Expanded(
          child: _loading
              ? const Center(
                  child: CircularProgressIndicator(),
                )
              : _error != null
                  ? _buildError()
                  : _buildContent(),
        ),
      ],
    );
  }

  Widget _buildTabs() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(
          children: [
            _tabButton(
              index: 0,
              label: 'Requests',
              icon: Icons.request_quote_outlined,
            ),
            const SizedBox(width: 8),
            _tabButton(
              index: 1,
              label: 'Orders',
              icon: Icons.shopping_cart_outlined,
            ),
            const SizedBox(width: 8),
            _tabButton(
              index: 2,
              label: 'Deliveries',
              icon: Icons.local_shipping_outlined,
            ),
          ],
        ),
      ),
    );
  }

  Widget _tabButton({
    required int index,
    required String label,
    required IconData icon,
  }) {
    final selected = _selectedTab == index;

    return ChoiceChip(
      selected: selected,
      avatar: Icon(
        icon,
        size: 18,
      ),
      label: Text(label),
      onSelected: (_) {
        setState(() {
          _selectedTab = index;
        });
      },
    );
  }

  Widget _buildContent() {
    switch (_selectedTab) {
      case 0:
        return _buildPurchaseRequests();

      case 1:
        return _buildPurchaseOrders();

      case 2:
        return _buildDeliveries();

      default:
        return const SizedBox.shrink();
    }
  }

  Widget _buildPurchaseRequests() {
  return Column(
    children: [
      Padding(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
        child: Align(
          alignment: Alignment.centerRight,
          child: FilledButton.icon(
            onPressed: _showCreatePurchaseRequest,
            icon: const Icon(Icons.add),
            label: const Text('New Request'),
          ),
        ),
      ),
      Expanded(
        child: _purchaseRequests.isEmpty
            ? _emptyState(
                icon: Icons.request_quote_outlined,
                title: 'No purchase requests',
                message: 'Create a new purchase request to get started.',
              )
            : RefreshIndicator(
                onRefresh: _loadData,
                child: ListView.builder(
                  padding: const EdgeInsets.fromLTRB(
                    16,
                    8,
                    16,
                    16,
                  ),
                  itemCount: _purchaseRequests.length,
                  itemBuilder: (context, index) {
                    final request = _purchaseRequests[index];

                    return _requestCard(request);
                  },
                ),
              ),
      ),
    ],
  );
}

  Widget _requestCard(Map<String, dynamic> request) {
  final id = _value(
    request,
    ['id', 'requestId', 'purchaseRequestId'],
  );

  final status = _value(
    request,
    ['status', 'requestStatus'],
    fallback: 'Unknown',
  );

  final material = _value(
    request,
    ['materialName', 'description', 'title', 'name'],
    fallback: 'Purchase Request',
  );

  final quantity = _value(
    request,
    ['quantity'],
  );

  final requiredByDate = _value(
    request,
    ['requiredByDate'],
  );

  final isPending = status.toLowerCase() == 'pending';

  return Card(
    margin: const EdgeInsets.only(bottom: 12),
    child: ListTile(
      leading: const CircleAvatar(
        child: Icon(Icons.request_quote_outlined),
      ),
      title: Text(
        material,
        maxLines: 2,
        overflow: TextOverflow.ellipsis,
      ),
      subtitle: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (id != '-') Text('ID: $id'),
          if (quantity != '-') Text('Quantity: $quantity'),
          if (requiredByDate != '-')
            Text('Required by: $requiredByDate'),
          const SizedBox(height: 4),
          _statusChip(status),
        ],
      ),
      isThreeLine: true,
      onTap: () => _showRequestDetails(request),
      trailing: isPending
          ? PopupMenuButton<String>(
              onSelected: (value) {
                if (value == 'edit') {
                  _showEditPurchaseRequest(request);
                } else if (value == 'approve') {
                  _approvePurchaseRequest(id);
                } else if (value == 'cancel') {
                  _cancelPurchaseRequest(id);
                }
              },
              itemBuilder: (context) => const [
                PopupMenuItem(
                  value: 'edit',
                  child: Text('Edit'),
                ),
                PopupMenuItem(
                  value: 'approve',
                  child: Text('Approve'),
                ),
                PopupMenuItem(
                  value: 'cancel',
                  child: Text('Cancel'),
                ),
              ],
            )
          : null,
    ),
  );
}

void _showEditPurchaseRequest(
  Map<String, dynamic> request,
) {
  final id = _value(
    request,
    ['id', 'requestId', 'purchaseRequestId'],
  );

  final materialController = TextEditingController(
    text: _value(
      request,
      ['materialName', 'description', 'title', 'name'],
      fallback: '',
    ),
  );

  final quantityController = TextEditingController(
    text: _value(
      request,
      ['quantity'],
      fallback: '',
    ),
  );

  DateTime? requiredByDate = DateTime.tryParse(
    _value(
      request,
      ['requiredByDate'],
      fallback: '',
    ),
  );

  bool saving = false;

  showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (sheetContext) {
      return StatefulBuilder(
        builder: (context, setSheetState) {
          Future<void> submit() async {
            final materialName =
                materialController.text.trim();

            final quantity =
                double.tryParse(quantityController.text.trim());

            if (materialName.isEmpty) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Material name is required'),
                ),
              );
              return;
            }

            if (quantity == null || quantity <= 0) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Quantity must be greater than zero',
                  ),
                ),
              );
              return;
            }

            if (requiredByDate == null) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Required by date is required',
                  ),
                ),
              );
              return;
            }

            setSheetState(() {
              saving = true;
            });

            try {
              await widget.service.updatePurchaseRequest(
                id,
                materialName: materialName,
                quantity: quantity,
                requiredByDate:
                    requiredByDate!.toUtc().toIso8601String(),
              );

              if (!context.mounted || !sheetContext.mounted) return;

              Navigator.pop(sheetContext);

              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Purchase request updated successfully',
                  ),
                ),
              );

              await _loadData();
            } catch (e) {
              if (!context.mounted || !sheetContext.mounted) return;

              setSheetState(() {
                saving = false;
              });

              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(
                    'Unable to update request: $e',
                  ),
                ),
              );
            }
          }

          return SafeArea(
            child: Padding(
              padding: EdgeInsets.fromLTRB(
                20,
                8,
                20,
                MediaQuery.of(context).viewInsets.bottom + 24,
              ),
              child: SingleChildScrollView(
                child: Column(
                  crossAxisAlignment:
                      CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Edit Purchase Request',
                      style: Theme.of(context)
                          .textTheme
                          .headlineSmall
                          ?.copyWith(
                            fontWeight: FontWeight.w800,
                          ),
                    ),
                    const SizedBox(height: 20),
                    TextField(
                      controller: materialController,
                      enabled: !saving,
                      decoration: const InputDecoration(
                        labelText: 'Material Name',
                        border: OutlineInputBorder(),
                      ),
                    ),
                    const SizedBox(height: 16),
                    TextField(
                      controller: quantityController,
                      enabled: !saving,
                      keyboardType:
                          const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                      decoration: const InputDecoration(
                        labelText: 'Quantity',
                        border: OutlineInputBorder(),
                      ),
                    ),
                    const SizedBox(height: 16),
                    InkWell(
                      onTap: saving
                          ? null
                          : () async {
                              final now = DateTime.now();

                              final picked = await showDatePicker(
                                context: context,
                                firstDate: now,
                                lastDate: DateTime(now.year + 5),
                                initialDate: requiredByDate ?? now,
                              );

                              if (!context.mounted) return;

                              if (picked != null) {
                                setSheetState(() {
                                  requiredByDate = picked;
                                });
                               }
                            },
                      child: InputDecorator(
                        decoration:
                            const InputDecoration(
                          labelText: 'Required By Date',
                          border: OutlineInputBorder(),
                        ),
                        child: Text(
                          requiredByDate == null
                              ? 'Select date'
                              : '${requiredByDate!.day.toString().padLeft(2, '0')}/'
                                  '${requiredByDate!.month.toString().padLeft(2, '0')}/'
                                  '${requiredByDate!.year}',
                        ),
                      ),
                    ),
                    const SizedBox(height: 24),
                    SizedBox(
                      width: double.infinity,
                      child: FilledButton(
                        onPressed: saving ? null : submit,
                        child: Text(
                          saving ? 'Updating...' : 'Update Request',
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          );
        },
      );
    },
  );
}

Future<void> _approvePurchaseRequest(String id) async {
  try {
    await widget.service.approvePurchaseRequest(id);

    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text(
          'Purchase request approved successfully',
        ),
      ),
    );

    await _loadData();
  } catch (e) {
    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          'Unable to approve request: $e',
        ),
      ),
    );
  }
}

Future<void> _cancelPurchaseRequest(String id) async {
  try {
    await widget.service.cancelPurchaseRequest(id);

    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text(
          'Purchase request cancelled successfully',
        ),
      ),
    );

    await _loadData();
  } catch (e) {
    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          'Unable to cancel request: $e',
        ),
      ),
    );
  }
}

  void _showCreatePurchaseRequest() {
  final materialController = TextEditingController();
  final quantityController = TextEditingController();

  DateTime? requiredByDate;
  bool saving = false;

  showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (sheetContext) {
      return StatefulBuilder(
        builder: (context, setSheetState) {
          Future<void> submit() async {
            final materialName = materialController.text.trim();
            final quantity =
                double.tryParse(quantityController.text.trim());

            if (materialName.isEmpty) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Material name is required'),
                ),
              );
              return;
            }

            if (quantity == null || quantity <= 0) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Quantity must be greater than zero',
                  ),
                ),
              );
              return;
            }

            if (requiredByDate == null) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Required by date is required',
                  ),
                ),
              );
              return;
            }

            setSheetState(() {
              saving = true;
            });

            try {
              await widget.service.createPurchaseRequest(
                materialName: materialName,
                quantity: quantity,
                requiredByDate:
                    requiredByDate!.toUtc().toIso8601String(),
              );

              if (!context.mounted || !sheetContext.mounted) return;

              Navigator.pop(sheetContext);

              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Purchase request created successfully',
                  ),
                ),
              );

              await _loadData();
            } catch (e) {
              if (!context.mounted || !sheetContext.mounted) return;

              setSheetState(() {
                saving = false;
              });

              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(
                    'Unable to create purchase request: $e',
                  ),
                ),
              );
            }
          }

          return SafeArea(
            child: Padding(
              padding: EdgeInsets.fromLTRB(
                20,
                8,
                20,
                MediaQuery.of(context).viewInsets.bottom + 24,
              ),
              child: SingleChildScrollView(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'New Purchase Request',
                      style: Theme.of(context)
                          .textTheme
                          .headlineSmall
                          ?.copyWith(
                            fontWeight: FontWeight.w800,
                          ),
                    ),
                    const SizedBox(height: 20),

                    TextField(
                      controller: materialController,
                      enabled: !saving,
                      decoration: const InputDecoration(
                        labelText: 'Material Name',
                        hintText: 'e.g. Cement',
                        border: OutlineInputBorder(),
                        prefixIcon: Icon(
                          Icons.inventory_2_outlined,
                        ),
                      ),
                    ),

                    const SizedBox(height: 16),

                    TextField(
                      controller: quantityController,
                      enabled: !saving,
                      keyboardType:
                          const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                      decoration: const InputDecoration(
                        labelText: 'Quantity',
                        hintText: 'e.g. 50',
                        border: OutlineInputBorder(),
                        prefixIcon: Icon(
                          Icons.numbers,
                        ),
                      ),
                    ),

                    const SizedBox(height: 16),

                    InkWell(
                      onTap: saving
                          ? null
                          : () async {
                              final now = DateTime.now();

                              final picked = await showDatePicker(
                                 context: context,
                                 firstDate: now,
                                 lastDate: DateTime(now.year + 5),
                                 initialDate: requiredByDate ?? now,
                              );

                              if (!context.mounted) return;

                              if (picked != null) {
                                setSheetState(() {
                                  requiredByDate = picked;
                                 });
                              }
                            },
                      child: InputDecorator(
                        decoration: const InputDecoration(
                          labelText: 'Required By Date',
                          border: OutlineInputBorder(),
                          prefixIcon: Icon(
                            Icons.calendar_today_outlined,
                          ),
                        ),
                        child: Text(
                          requiredByDate == null
                              ? 'Select date'
                              : '${requiredByDate!.day.toString().padLeft(2, '0')}/'
                                  '${requiredByDate!.month.toString().padLeft(2, '0')}/'
                                  '${requiredByDate!.year}',
                        ),
                      ),
                    ),

                    const SizedBox(height: 24),

                    SizedBox(
                      width: double.infinity,
                      child: FilledButton.icon(
                        onPressed: saving ? null : submit,
                        icon: saving
                            ? const SizedBox(
                                width: 18,
                                height: 18,
                                child:
                                    CircularProgressIndicator(
                                  strokeWidth: 2,
                                ),
                              )
                            : const Icon(Icons.add),
                        label: Text(
                          saving
                              ? 'Creating...'
                              : 'Create Request',
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          );
        },
      );
    },
  );
}

  Widget _buildPurchaseOrders() {
    if (_purchaseOrders.isEmpty) {
      return _emptyState(
        icon: Icons.shopping_cart_outlined,
        title: 'No purchase orders',
        message: 'Purchase orders will appear here.',
      );
    }

    return RefreshIndicator(
      onRefresh: _loadData,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: _purchaseOrders.length,
        itemBuilder: (context, index) {
          final order = _purchaseOrders[index];

          return _orderCard(order);
        },
      ),
    );
  }

  Widget _orderCard(Map<String, dynamic> order) {
  final id = _value(
    order,
    ['id', 'orderId', 'purchaseOrderId'],
  );

  final status = _value(
    order,
    ['status', 'orderStatus'],
    fallback: 'Unknown',
  );

  final supplier = _value(
    order,
    ['supplierName', 'supplierId'],
  );

  final total = _value(
    order,
    ['totalCost', 'totalAmount', 'total'],
  );

  final isPending = status.toLowerCase() == 'pending';

  return Card(
    margin: const EdgeInsets.only(bottom: 12),
    child: ListTile(
      leading: const CircleAvatar(
        child: Icon(Icons.shopping_cart_outlined),
      ),
      title: const Text(
        'Purchase Order',
        style: TextStyle(
          fontWeight: FontWeight.w700,
        ),
      ),
      subtitle: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (id != '-') Text('ID: $id'),
          if (supplier != '-') Text('Supplier: $supplier'),
          if (total != '-') Text('Total: $total'),
          const SizedBox(height: 4),
          _statusChip(status),
        ],
      ),
      isThreeLine: true,
      onTap: () => _showOrderDetails(order),
      trailing: isPending
          ? PopupMenuButton<String>(
              onSelected: (value) {
                if (value == 'edit') {
                  _showEditPurchaseOrder(order);
                } else if (value == 'cancel') {
                  _cancelPurchaseOrder(id);
                }
              },
              itemBuilder: (context) => const [
                PopupMenuItem(
                  value: 'edit',
                  child: Text('Edit'),
                ),
                PopupMenuItem(
                  value: 'cancel',
                  child: Text('Cancel'),
                ),
              ],
            )
          : null,
    ),
  );
}

void _showEditPurchaseOrder(
  Map<String, dynamic> order,
) {
  final id = _value(
    order,
    ['id', 'orderId', 'purchaseOrderId'],
  );

  final purchaseRequestIdController = TextEditingController(
    text: _value(
      order,
      ['purchaseRequestId', 'requestId'],
      fallback: '',
    ),
  );

  final supplierIdController = TextEditingController(
    text: _value(
      order,
      ['supplierId'],
      fallback: '',
    ),
  );

  final unitPriceController = TextEditingController(
    text: _value(
      order,
      ['unitPrice', 'price'],
      fallback: '',
    ),
  );

  DateTime? deliveryDate = DateTime.tryParse(
    _value(
      order,
      ['deliveryDate', 'expectedDeliveryDate'],
      fallback: '',
    ),
  );

  bool saving = false;

  showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (sheetContext) {
      return StatefulBuilder(
        builder: (context, setSheetState) {
          Future<void> submit() async {
            final purchaseRequestId =
                purchaseRequestIdController.text.trim();

            final supplierId =
                supplierIdController.text.trim();

            final unitPrice =
                double.tryParse(
              unitPriceController.text.trim(),
            );

            if (purchaseRequestId.isEmpty ||
                int.tryParse(purchaseRequestId) == null) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Purchase request ID must be a number',
                  ),
                ),
              );
              return;
            }

            if (supplierId.isEmpty ||
                int.tryParse(supplierId) == null) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Supplier ID must be a number',
                  ),
                ),
              );
              return;
            }

            if (unitPrice == null || unitPrice < 0) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Unit price must be a valid number',
                  ),
                ),
              );
              return;
            }

            if (deliveryDate == null) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Delivery date is required',
                  ),
                ),
              );
              return;
            }

            setSheetState(() {
              saving = true;
            });

            try {
              await widget.service.updatePurchaseOrder(
                id,
                purchaseRequestId: purchaseRequestId,
                supplierId: supplierId,
                unitPrice: unitPrice,
                deliveryDate:
                    deliveryDate!.toUtc().toIso8601String(),
              );

              if (!context.mounted || !sheetContext.mounted) return;

              Navigator.pop(sheetContext);

              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Purchase order updated successfully',
                  ),
                ),
              );

              await _loadData();
            } catch (e) {
              if (!context.mounted || !sheetContext.mounted) return;

              setSheetState(() {
                saving = false;
              });

              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(
                    'Unable to update purchase order: $e',
                  ),
                ),
              );
            }
          }

          return SafeArea(
            child: Padding(
              padding: EdgeInsets.fromLTRB(
                20,
                8,
                20,
                MediaQuery.of(context).viewInsets.bottom + 24,
              ),
              child: SingleChildScrollView(
                child: Column(
                  crossAxisAlignment:
                      CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Edit Purchase Order',
                      style: Theme.of(context)
                          .textTheme
                          .headlineSmall
                          ?.copyWith(
                            fontWeight: FontWeight.w800,
                          ),
                    ),
                    const SizedBox(height: 20),

                    TextField(
                      controller:
                          purchaseRequestIdController,
                      enabled: !saving,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(
                        labelText: 'Purchase Request ID',
                        border: OutlineInputBorder(),
                      ),
                    ),

                    const SizedBox(height: 16),

                    TextField(
                      controller: supplierIdController,
                      enabled: !saving,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(
                        labelText: 'Supplier ID',
                        border: OutlineInputBorder(),
                      ),
                    ),

                    const SizedBox(height: 16),

                    TextField(
                      controller: unitPriceController,
                      enabled: !saving,
                      keyboardType:
                          const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                      decoration: const InputDecoration(
                        labelText: 'Unit Price',
                        border: OutlineInputBorder(),
                      ),
                    ),

                    const SizedBox(height: 16),

                    InkWell(
                      onTap: saving
                          ? null
                          : () async {
                              final picked = await showDatePicker(
                                context: context,
                                firstDate: DateTime.now(),
                                lastDate: DateTime(DateTime.now().year + 5),
                                initialDate: deliveryDate,
                              );

                              if (!context.mounted) return;

                              if (picked != null) {
                                setSheetState(() {
                                  deliveryDate = picked;
                                });
                              }
                            },
                      child: InputDecorator(
                        decoration:
                            const InputDecoration(
                          labelText: 'Delivery Date',
                          border: OutlineInputBorder(),
                        ),
                        child: Text(
                          deliveryDate == null
                              ? 'Select date'
                              : '${deliveryDate!.day.toString().padLeft(2, '0')}/'
                                  '${deliveryDate!.month.toString().padLeft(2, '0')}/'
                                  '${deliveryDate!.year}',
                        ),
                      ),
                    ),

                    const SizedBox(height: 24),

                    SizedBox(
                      width: double.infinity,
                      child: FilledButton(
                        onPressed:
                            saving ? null : submit,
                        child: Text(
                          saving
                              ? 'Updating...'
                              : 'Update Purchase Order',
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          );
        },
      );
    },
  );
}

Future<void> _cancelPurchaseOrder(String id) async {
  try {
    await widget.service.cancelPurchaseOrder(id);

    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text(
          'Purchase order cancelled successfully',
        ),
      ),
    );

    await _loadData();
  } catch (e) {
    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          'Unable to cancel purchase order: $e',
        ),
      ),
    );
  }
}

  Widget _buildDeliveries() {
  return Column(
    children: [
      Padding(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
        child: Align(
          alignment: Alignment.centerRight,
          child: FilledButton.icon(
            onPressed: _showCreateDelivery,
            icon: const Icon(Icons.add),
            label: const Text('New Delivery'),
          ),
        ),
      ),
      Expanded(
        child: _deliveries.isEmpty
            ? _emptyState(
                icon: Icons.local_shipping_outlined,
                title: 'No deliveries',
                message:
                    'Create a delivery when materials are delivered.',
              )
            : RefreshIndicator(
                onRefresh: _loadData,
                child: ListView.builder(
                  padding: const EdgeInsets.fromLTRB(
                    16,
                    8,
                    16,
                    16,
                  ),
                  itemCount: _deliveries.length,
                  itemBuilder: (context, index) {
                    return _deliveryCard(
                      _deliveries[index],
                    );
                  },
                ),
              ),
      ),
    ],
  );
}

  Widget _deliveryCard(Map<String, dynamic> delivery) {
    final id = _value(
      delivery,
      ['id', 'deliveryId'],
    );

    final status = _value(
      delivery,
      ['status', 'deliveryStatus'],
      fallback: 'Unknown',
    );

    final orderId = _value(
      delivery,
      ['purchaseOrderId', 'orderId'],
    );

    final expectedDate = _value(
      delivery,
      ['expectedDate', 'deliveryDate', 'expectedDeliveryDate'],
    );

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: ListTile(
        leading: const CircleAvatar(
          child: Icon(Icons.local_shipping_outlined),
        ),
        title: Text(
          'Delivery',
          style: const TextStyle(
            fontWeight: FontWeight.w700,
          ),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (id != '-') Text('ID: $id'),
            if (orderId != '-') Text('Order: $orderId'),
            if (expectedDate != '-') Text('Expected: $expectedDate'),
            const SizedBox(height: 4),
            _statusChip(status),
          ],
        ),
        isThreeLine: true,
        onTap: () => _showDeliveryDetails(delivery),
      ),
    );
  }

  void _showCreateDelivery() {
  final orderController = TextEditingController();
  final quantityController = TextEditingController();

  DateTime deliveryDate = DateTime.now();
  bool saving = false;

  showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (sheetContext) {
      return StatefulBuilder(
        builder: (context, setSheetState) {
          Future<void> submit() async {
            final orderId =
                orderController.text.trim();

            final quantity =
                double.tryParse(
              quantityController.text.trim(),
            );

            if (orderId.isEmpty) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Purchase order ID is required',
                  ),
                ),
              );
              return;
            }

            if (int.tryParse(orderId) == null) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Purchase order ID must be a number',
                  ),
                ),
              );
              return;
            }

            if (quantity == null || quantity <= 0) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Quantity must be greater than zero',
                  ),
                ),
              );
              return;
            }

            setSheetState(() {
              saving = true;
            });

            try {
              await widget.service.createDelivery(
                purchaseOrderId: orderId,
                quantity: quantity,
                deliveryDate:
                    deliveryDate.toUtc().toIso8601String(),
              );

              if (!context.mounted || !sheetContext.mounted) return;

              Navigator.pop(sheetContext);

              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text(
                    'Delivery created successfully',
                  ),
                ),
              );

              await _loadData();
            } catch (e) {
              if (!context.mounted || !sheetContext.mounted) return;

              setSheetState(() {
                saving = false;
              });

              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(
                    'Unable to create delivery: $e',
                  ),
                ),
              );
            }
          }

          return SafeArea(
            child: Padding(
              padding: EdgeInsets.fromLTRB(
                20,
                8,
                20,
                MediaQuery.of(context).viewInsets.bottom + 24,
              ),
              child: SingleChildScrollView(
                child: Column(
                  crossAxisAlignment:
                      CrossAxisAlignment.start,
                  children: [
                    Text(
                      'New Delivery',
                      style: Theme.of(context)
                          .textTheme
                          .headlineSmall
                          ?.copyWith(
                            fontWeight: FontWeight.w800,
                          ),
                    ),
                    const SizedBox(height: 20),
                    TextField(
                      controller: orderController,
                      enabled: !saving,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(
                        labelText: 'Purchase Order ID',
                        border: OutlineInputBorder(),
                      ),
                    ),
                    const SizedBox(height: 16),
                    TextField(
                      controller: quantityController,
                      enabled: !saving,
                      keyboardType:
                          const TextInputType.numberWithOptions(
                        decimal: true,
                      ),
                      decoration: const InputDecoration(
                        labelText: 'Quantity',
                        border: OutlineInputBorder(),
                      ),
                    ),
                    const SizedBox(height: 16),
                    InkWell(
                      onTap: saving
                          ? null
                          : () async {
                              final picked =
                                  await showDatePicker(
                                context: context,
                                firstDate: DateTime.now(),
                                lastDate: DateTime(
                                  DateTime.now().year + 5,
                                ),
                                initialDate: deliveryDate,
                              );

                              if (!context.mounted) return;

                              if (picked != null) {
                                setSheetState(() {
                                  deliveryDate = picked;
                                });
                              }
                            },
                      child: InputDecorator(
                        decoration:
                            const InputDecoration(
                          labelText: 'Delivery Date',
                          border: OutlineInputBorder(),
                        ),
                        child: Text(
                          '${deliveryDate.day.toString().padLeft(2, '0')}/'
                          '${deliveryDate.month.toString().padLeft(2, '0')}/'
                          '${deliveryDate.year}',
                        ),
                      ),
                    ),
                    const SizedBox(height: 24),
                    SizedBox(
                      width: double.infinity,
                      child: FilledButton(
                        onPressed: saving ? null : submit,
                        child: Text(
                          saving
                              ? 'Creating...'
                              : 'Create Delivery',
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          );
        },
      );
    },
  );
}

  Widget _statusChip(
    String status,
  ) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: 10,
        vertical: 5,
      ),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(20),
        color: Theme.of(context)
            .colorScheme
            .primaryContainer,
      ),
      child: Text(
        status,
        style: TextStyle(
          color: Theme.of(context)
              .colorScheme
              .onPrimaryContainer,
          fontWeight: FontWeight.w600,
          fontSize: 12,
        ),
      ),
    );
  }

  Widget _emptyState({
    required IconData icon,
    required String title,
    required String message,
  }) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              size: 60,
              color: Theme.of(context)
                  .colorScheme
                  .primary,
            ),
            const SizedBox(height: 16),
            Text(
              title,
              style: Theme.of(context)
                  .textTheme
                  .titleLarge
                  ?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
            ),
            const SizedBox(height: 8),
            Text(
              message,
              textAlign: TextAlign.center,
              style: TextStyle(
                color: Theme.of(context)
                    .colorScheme
                    .onSurfaceVariant,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildError() {
    return RefreshIndicator(
      onRefresh: _loadData,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(24),
        children: [
          const SizedBox(height: 80),
          Icon(
            Icons.error_outline,
            size: 60,
            color: Theme.of(context).colorScheme.error,
          ),
          const SizedBox(height: 16),
          Text(
            'Unable to load procurement data',
            textAlign: TextAlign.center,
            style: Theme.of(context)
                .textTheme
                .titleLarge
                ?.copyWith(
                  fontWeight: FontWeight.w700,
                ),
          ),
          const SizedBox(height: 8),
          Text(
            _error!,
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 20),
          FilledButton.icon(
            onPressed: _loadData,
            icon: const Icon(Icons.refresh),
            label: const Text('Try again'),
          ),
        ],
      ),
    );
  }

  void _showRequestDetails(
    Map<String, dynamic> request,
  ) {
    _showDetailsSheet(
      title: 'Purchase Request',
      data: request,
    );
  }

  void _showOrderDetails(
    Map<String, dynamic> order,
  ) {
    _showDetailsSheet(
      title: 'Purchase Order',
      data: order,
    );
  }

  void _showDeliveryDetails(
    Map<String, dynamic> delivery,
  ) {
    _showDetailsSheet(
      title: 'Delivery',
      data: delivery,
      delivery: true,
    );
  }

  void _showDetailsSheet({
    required String title,
    required Map<String, dynamic> data,
    bool delivery = false,
  }) {
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (context) {
        return SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(
              20,
              8,
              20,
              24,
            ),
            child: SingleChildScrollView(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: Theme.of(context)
                        .textTheme
                        .headlineSmall
                        ?.copyWith(
                          fontWeight: FontWeight.w800,
                        ),
                  ),
                  const SizedBox(height: 16),
                  ...data.entries.map(
                    (entry) => Padding(
                      padding: const EdgeInsets.only(
                        bottom: 10,
                      ),
                      child: Row(
                        crossAxisAlignment:
                            CrossAxisAlignment.start,
                        children: [
                          SizedBox(
                            width: 130,
                            child: Text(
                              _formatKey(entry.key),
                              style: const TextStyle(
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                          Expanded(
                            child: Text(
                              '${entry.value}',
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  if (delivery) ...[
                    const SizedBox(height: 12),
                    SizedBox(
                      width: double.infinity,
                      child: FilledButton.icon(
                        onPressed: () {
                          Navigator.pop(context);
                          _showDeliveryActions(data);
                        },
                        icon: const Icon(
                          Icons.local_shipping,
                        ),
                        label: const Text(
                          'Delivery actions',
                        ),
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  void _showDeliveryActions(
    Map<String, dynamic> delivery,
  ) {
    final id = _value(
      delivery,
      ['id', 'deliveryId'],
    );

    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (context) {
        return SafeArea(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                ListTile(
                  leading: const Icon(
                    Icons.check_circle_outline,
                  ),
                  title: const Text(
                    'Confirm delivery',
                  ),
                  subtitle: const Text(
                    'Update delivery status when the site receives it.',
                  ),
                  onTap: () {
                    Navigator.pop(context);
                    _updateDeliveryStatus(
                      id,
                      'Completed',
                    );
                  },
                ),
                ListTile(
                  leading: const Icon(
                    Icons.pending_actions,
                  ),
                  title: const Text(
                    'Mark as in transit',
                  ),
                  onTap: () {
                    Navigator.pop(context);
                    _updateDeliveryStatus(
                      id,
                      'Received',
                    );
                  },
                ),
                ListTile(
                  leading: const Icon(
                    Icons.cancel_outlined,
                  ),
                  title: const Text(
                    'Cancel delivery',
                  ),
                  onTap: () {
                    Navigator.pop(context);
                    _cancelDelivery(id);
                  },
                 ),
              ],
            ),
          ),
        );
      },
    );
  }

  Future<void> _updateDeliveryStatus(
    String id,
    String status,
  ) async {
    try {
      await widget.service.updateDeliveryStatus(
        id,
        status,
      );

      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Delivery status updated to $status',
          ),
        ),
      );

      await _loadData();
    } catch (e) {
      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Unable to update delivery: $e',
          ),
        ),
      );
    }
  }

  Future<void> _cancelDelivery(String id) async {
  try {
    await widget.service.cancelDelivery(id);

    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Delivery cancelled successfully'),
      ),
    );

    await _loadData();
  } catch (e) {
    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          'Unable to cancel delivery: $e',
        ),
      ),
    );
  }
}

  String _value(
    Map<String, dynamic> data,
    List<String> keys, {
    String fallback = '-',
  }) {
    for (final key in keys) {
      final value = data[key];

      if (value != null &&
          value.toString().trim().isNotEmpty) {
        return value.toString();
      }
    }

    return fallback;
  }

  String _formatKey(String key) {
    final result = key.replaceAllMapped(
      RegExp(r'([a-z])([A-Z])'),
      (match) => '${match.group(1)} ${match.group(2)}',
    );

    return result[0].toUpperCase() + result.substring(1);
  }
}
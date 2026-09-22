import 'package:flutter/material.dart';

import '../../providers/auth_provider.dart';
import 'auth_shell.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({required this.authProvider, super.key});
  final AuthProvider authProvider;

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();
  bool _submitting = false;

  @override
  void dispose() {
    _name.dispose();
    _email.dispose();
    _password.dispose();
    _confirm.dispose();
    super.dispose();
  }

  bool _validPassword(String value) =>
      value.length >= 8 &&
      RegExp('[A-Z]').hasMatch(value) &&
      RegExp('[a-z]').hasMatch(value) &&
      RegExp('[0-9]').hasMatch(value) &&
      RegExp(r'[^A-Za-z0-9]').hasMatch(value);

  Future<void> _submit() async {
    widget.authProvider.clearError();
    if (!_formKey.currentState!.validate()) return;
    setState(() => _submitting = true);
    final success = await widget.authProvider.register(
      fullName: _name.text.trim(),
      email: _email.text.trim(),
      password: _password.text,
      confirmPassword: _confirm.text,
    );
    if (mounted && success) {
      Navigator.of(context).popUntil((route) => route.isFirst);
    }
    if (mounted) {
      setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) => AuthShell(
    title: 'Create your account',
    subtitle: 'New accounts receive secure Site Engineer access.',
    child: Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (widget.authProvider.errorMessage case final message?) ...[
            Container(
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.errorContainer,
                borderRadius: BorderRadius.circular(12),
              ),
              child: Text(message),
            ),
            const SizedBox(height: 16),
          ],
          TextFormField(
            controller: _name,
            textCapitalization: TextCapitalization.words,
            decoration: const InputDecoration(
              labelText: 'Full name',
              prefixIcon: Icon(Icons.person_outline),
            ),
            validator: (value) => value != null && value.trim().length >= 2
                ? null
                : 'Enter your full name.',
          ),
          const SizedBox(height: 14),
          TextFormField(
            controller: _email,
            keyboardType: TextInputType.emailAddress,
            decoration: const InputDecoration(
              labelText: 'Email address',
              prefixIcon: Icon(Icons.mail_outline),
            ),
            validator: (value) =>
                value != null &&
                    RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$').hasMatch(value.trim())
                ? null
                : 'Enter a valid email address.',
          ),
          const SizedBox(height: 14),
          TextFormField(
            controller: _password,
            obscureText: true,
            decoration: const InputDecoration(
              labelText: 'Password',
              prefixIcon: Icon(Icons.lock_outline),
            ),
            validator: (value) =>
                value != null && _validPassword(value) ? null : 'Use 8+ characters with uppercase, lowercase, number and symbol.',
          ),
          const SizedBox(height: 14),
          TextFormField(
            controller: _confirm,
            obscureText: true,
            decoration: const InputDecoration(
              labelText: 'Confirm password',
              prefixIcon: Icon(Icons.lock_outline),
            ),
            validator: (value) =>
                value == _password.text ? null : 'Passwords do not match.',
          ),
          const SizedBox(height: 14),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(14),
              child: Text(
                'Project Manager and officer roles are assigned by an administrator.',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ),
          ),
          const SizedBox(height: 18),
          FilledButton(
            onPressed: _submitting ? null : _submit,
            style: FilledButton.styleFrom(
              minimumSize: const Size.fromHeight(52),
            ),
            child: Text(_submitting ? 'Creating account…' : 'Create account'),
          ),
          const SizedBox(height: 10),
          TextButton(
            onPressed: _submitting ? null : () => Navigator.of(context).pop(),
            child: const Text('Already registered? Sign in'),
          ),
        ],
      ),
    ),
  );
}

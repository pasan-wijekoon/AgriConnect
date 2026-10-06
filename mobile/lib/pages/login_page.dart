import 'package:flutter/material.dart';

/// A simple login page used only for UI‑tests. It validates the email and password
/// fields and displays the appropriate error messages expected by the test suite.
class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _emailFocus = FocusNode();
  final _passwordFocus = FocusNode();

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    _emailFocus.dispose();
    _passwordFocus.dispose();
    super.dispose();
  }

  String? _emailValidator(String? value) {
    if (value == null || value.isEmpty) return 'Email is required';
    final emailRegex = RegExp(r'^[^@]+@[^@]+\.[^@]+');
    if (!emailRegex.hasMatch(value)) return 'Enter a valid email';
    return null;
  }

  String? _passwordValidator(String? value) {
    if (value == null || value.isEmpty) return 'Password is required';
    return null;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Login')),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Form(
          key: _formKey,
          child: Column(
            children: [
              TextFormField(
                focusNode: _emailFocus,
                key: const Key('emailField'),
                controller: _emailController,
                decoration: const InputDecoration(labelText: 'Email'),
                validator: _emailValidator,
              ),
              TextFormField(
                focusNode: _passwordFocus,
                key: const Key('passwordField'),
                controller: _passwordController,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Password'),
                validator: _passwordValidator,
              ),
              const SizedBox(height: 20),
              ElevatedButton(
                key: const Key('loginButton'),
                onPressed: () {
                  // Trigger validation; the tests only assert the error texts.
                  _formKey.currentState?.validate();
                },
                child: const Text('Login'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

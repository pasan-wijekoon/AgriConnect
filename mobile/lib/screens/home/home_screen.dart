import 'package:flutter/material.dart';

/// Entry point for farmers and buyers. Each component adds its own tile here.
class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('AgriConnect')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Card(
            child: ListTile(
              leading: const Icon(Icons.show_chart),
              title: const Text('Market prices'),
              subtitle: const Text('Weekly crop prices to help you decide when to list'),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => Navigator.pushNamed(context, '/price-trends'),
            ),
          ),
        ],
      ),
    );
  }
}

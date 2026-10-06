import 'dart:convert';
import 'package:http/http.dart' as http;

Future<String> login(String email, String password) async {
  final response = await http.post(
    Uri.parse('http://localhost:5000/api/auth/login'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({'email': email, 'password': password}),
  );
  if (response.statusCode != 200) {
    throw Exception('Login failed with status ${response.statusCode}');
  }
  final data = jsonDecode(response.body) as Map<String, dynamic>;
  return data['token'] as String;
}

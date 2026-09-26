class AuthUser {
  final String id;
  final String fullName;
  final String email;
  final String role;
  final String? phone;
  final String? region;
  final String token;

  AuthUser({
    required this.id,
    required this.fullName,
    required this.email,
    required this.role,
    required this.token,
    this.phone,
    this.region,
  });

  factory AuthUser.fromJson(Map<String, dynamic> json) => AuthUser(
        id: json['id'] as String,
        fullName: json['fullName'] as String,
        email: json['email'] as String,
        role: json['role'] as String,
        token: json['token'] as String,
        phone: json['phone'] as String?,
        region: json['region'] as String?,
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'fullName': fullName,
        'email': email,
        'role': role,
        'token': token,
        'phone': phone,
        'region': region,
      };
}

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'providers/notification_provider.dart';
import 'providers/order_provider.dart';
import 'providers/session_provider.dart';
import 'screens/auth/welcome_screen.dart';
import 'screens/shell/main_shell.dart';
import 'services/api_service.dart';
import 'services/notification_service.dart';
import 'services/order_service.dart';
import 'services/price_trend_service.dart';
import 'theme/app_colors.dart';
import 'widgets/state_views.dart';

void main() {
  runApp(const AgriConnectApp());
}

class AgriConnectApp extends StatelessWidget {
  const AgriConnectApp({super.key, this.priceTrendService, this.session});

  /// Injected in widget tests, so the Prices tab doesn't make a real HTTP
  /// call; the real API client otherwise.
  final PriceTrendService? priceTrendService;

  /// Injected in widget tests; otherwise a real session that restores any
  /// saved login on start.
  final SessionProvider? session;

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        if (session != null)
          ChangeNotifierProvider<SessionProvider>.value(value: session!)
        else
          ChangeNotifierProvider<SessionProvider>(create: (_) => SessionProvider()..restore()),
        Provider<ApiService>(
          create: (ctx) => ApiService(getToken: () => ctx.read<SessionProvider>().token),
        ),
        // Order and notification state belong to one signed-in user: a new
        // instance is created whenever the signed-in user changes, so nothing
        // leaks from one account to the next.
        ChangeNotifierProxyProvider<SessionProvider, OrderProvider>(
          create: (ctx) => OrderProvider(OrderService(ctx.read<SessionProvider>().client)),
          update: (ctx, session, previous) => previous != null && previous.ownerId == session.user?.id
              ? previous
              : OrderProvider(OrderService(session.client), ownerId: session.user?.id),
        ),
        ChangeNotifierProxyProvider<SessionProvider, NotificationProvider>(
          create: (ctx) => NotificationProvider(NotificationService(ctx.read<SessionProvider>().client)),
          update: (ctx, session, previous) => previous != null && previous.ownerId == session.user?.id
              ? previous
              : NotificationProvider(NotificationService(session.client), ownerId: session.user?.id),
        ),
      ],
      child: MaterialApp(
        title: 'AgriConnect',
        theme: buildAppTheme(),
        debugShowCheckedModeBanner: false,
        home: AuthGate(priceTrendService: priceTrendService),
      ),
    );
  }
}

/// Root below MaterialApp: splash while the saved session is checked, the
/// landing page when signed out, the role-based shell when signed in.
class AuthGate extends StatelessWidget {
  const AuthGate({super.key, this.priceTrendService});

  final PriceTrendService? priceTrendService;

  @override
  Widget build(BuildContext context) {
    final session = context.watch<SessionProvider>();

    if (session.isRestoring) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    final user = session.user;
    if (user == null) return const WelcomeScreen();

    if (!user.isBuyer && !user.isFarmer) {
      return const _WebOnlyRoleScreen();
    }

    return MainShell(priceTrendService: priceTrendService);
  }
}

/// Officers and Administrators work in the web console; the mobile app is for
/// Buyers and Farmers, so anything else gets a clear explanation, not a
/// half-working app.
class _WebOnlyRoleScreen extends StatelessWidget {
  const _WebOnlyRoleScreen();

  @override
  Widget build(BuildContext context) {
    final user = context.watch<SessionProvider>().user!;
    return Scaffold(
      body: SafeArea(
        child: CenteredMessage(
          icon: Icons.desktop_windows_outlined,
          title: '${user.role} accounts use the web console',
          subtitle: 'Order review, scheduling and analytics are available in the AgriConnect web app. '
              'The mobile app is for buyers and farmers.',
          actionLabel: 'Sign out',
          onAction: () => context.read<SessionProvider>().logout(),
        ),
      ),
    );
  }
}

import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const deviceId = process.env.APPIUM_DEVICE_ID || 'R8YTC0J4HHP';

export const config = {
  runner: 'local',
  specs: ['./specs/**/*.spec.js'],
  maxInstances: 1,
  hostname: '127.0.0.1',
  port: 4723,
  path: '/',
  logLevel: 'info',
  connectionRetryTimeout: 300000,
  framework: 'mocha',
  services: [[
    path.resolve(
      __dirname,
      'node_modules',
      'wdio-flutter-by-service',
      'build',
      'index.js',
    ),
    {},
  ]],
  reporters: ['spec'],
  mochaOpts: {
    timeout: 180000,
  },
  capabilities: [
    {
      platformName: 'Android',
      'appium:automationName': 'FlutterIntegration',
      'appium:udid': deviceId,
      'appium:fullReset': true,
      'appium:app': path.resolve(
        __dirname,
        '../build/app/outputs/flutter-apk/app-debug.apk',
      ),
      'appium:flutterServerLaunchTimeout': 30000,
      'appium:flutterElementWaitTimeout': 15000,
      'appium:newCommandTimeout': 240,
    },
  ],
  afterTest: async function (test, context, { error }) {
    if (error) {
      const testId = test.title.includes('XP-M-01') ? 'XP-M-01' : 'XP-M-02';
      const { step } = await import('./helpers/screenshot.js');
      await step(
        browser,
        testId,
        'failure',
      );
    }
  },
};

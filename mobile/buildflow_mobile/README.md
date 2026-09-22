# BuildFlow AI mobile

Android and iOS application for site operations.

Run `flutter pub get`, then `flutter run` with an Android or iOS device.
Validation: flutter analyze and flutter test. iOS builds require macOS and Xcode.

The app restores sessions through a splash screen, supports registration and login,
stores access/refresh tokens in platform secure storage, refreshes expired access
tokens, protects the home shell, and revokes the session on logout. Light, dark,
and system theme choices persist locally.

Set the API URL with `--dart-define=API_BASE_URL=...`. The Android emulator default
is `http://10.0.2.2:5144/api`; a physical device needs an address it can reach.

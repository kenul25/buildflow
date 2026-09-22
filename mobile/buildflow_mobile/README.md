# BuildFlow AI mobile

Android and iOS application for site operations.

Run flutter pub get, then flutter run with an Android or iOS device.
Validation: flutter analyze and flutter test. iOS builds require macOS and Xcode.

main.dart is a minimal application shell. Other named Dart files are documented
placeholders following the mobile design plan. Assets can be registered in
pubspec.yaml when added. Theme persistence, authentication, storage and API
integration are not implemented yet.

.env.example documents future public configuration; no dotenv loader is installed.
Use a backend address reachable from the device when implementing configuration.

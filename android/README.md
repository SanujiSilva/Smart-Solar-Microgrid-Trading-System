# Smart Solar Microgrid Android client

Phase 15 provides the native Android project foundation using Kotlin, XML layouts, AndroidX, Material 3, and view binding. It uses the package `com.smartsolar.microgrid` and targets the central API through a non-secret emulator base URL.

## Structure

- `app/src/main/java/com/smartsolar/microgrid/`: Kotlin application code.
- `app/src/main/java/com/smartsolar/microgrid/data/remote/`: API configuration boundary.
- `app/src/main/res/layout/`: XML presentation layouts.
- `app/src/main/res/values/`: colors, strings, and Material theme resources.

## Tooling

Open the `android` folder in Android Studio with an Android SDK and Gradle-compatible JDK installed. The project uses Android Gradle Plugin 8.7.3, Kotlin 2.0.21, compile/target SDK 35, and minimum SDK 26.

The emulator API base is `http://10.0.2.2:5080/api/`; update `ApiConfig.BASE_URL` for a physical device or another API host in later integration work. Do not place credentials or bearer tokens in this configuration.

SQLite, Retrofit, Google Maps, QR rendering/scanning, authentication screens, and prosumer/operator workflows are intentionally reserved for Phases 16-21.

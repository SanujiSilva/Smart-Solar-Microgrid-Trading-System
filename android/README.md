# Smart Solar Microgrid Android client

Phase 15 provides the native Android project foundation using Kotlin, XML layouts, AndroidX, Material 3, and view binding. Phase 16 adds SQLiteOpenHelper-based local reference persistence for the authenticated user profile and station cache. It uses the package `com.smartsolar.microgrid` and targets the central API through a non-secret emulator base URL.

## Structure

- `app/src/main/java/com/smartsolar/microgrid/`: Kotlin application code.
- `app/src/main/java/com/smartsolar/microgrid/data/remote/`: API configuration boundary.
- `app/src/main/res/layout/`: XML presentation layouts.
- `app/src/main/res/values/`: colors, strings, and Material theme resources.

## Tooling

Open the `android` folder in Android Studio with an Android SDK and Gradle-compatible JDK installed. The project uses Android Gradle Plugin 8.7.3, Kotlin 2.0.21, compile/target SDK 35, and minimum SDK 26.

The emulator API base is `http://10.0.2.2:5080/api/`; update `ApiConfig.BASE_URL` for a physical device or another API host in later integration work. Do not place credentials or bearer tokens in this configuration.

The SQLite database stores only server-returned display/session reference data and station cache data with fetch timestamps. It never stores passwords, MongoDB data access, reservation authorization decisions, or bearer tokens. Phase 17 adds Retrofit authentication and secure token handling. Phase 18 adds prosumer registration, a live home dashboard, profile editing, deactivation requests and sign-out. See [the Phase 18 walkthrough](../docs/phase-18.md).

Build with the included Gradle 8.9 wrapper and a compatible JDK (Android Studio's bundled JDK was used). Set `ANDROID_HOME` to the full Android SDK; if `ANDROID_SDK_ROOT` is present it must point to the same SDK, not the separate platform-tools directory. Run `.\android\gradlew.bat -p android assembleDebug testDebugUnitTest lintDebug` from the repository root, or use Android Studio. The [Phase 18 record](../docs/phase-18.md#verification) includes environment setup commands.

The debug manifest permits local HTTP only to `10.0.2.2`. Release requires an HTTPS API host. The API remains authoritative for all account rules. Booking/station selection, Maps and QR/operator workflows remain in Phases 19-21.

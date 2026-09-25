# Phase 17 completion record

## Delivered scope

Connected the native Android app to the ASP.NET Core authentication API using Retrofit, Gson, OkHttp, and Kotlin coroutines.

- `POST /api/auth/login` authenticates with email/NIC and password.
- `GET /api/auth/me` restores and validates an existing server session.
- LoginActivity provides native XML input, loading, validation, network, credential, and inactive-account states.
- MainActivity is protected by the encrypted local session token.
- The bearer token is encrypted with an AES/GCM key held in Android Keystore and is never stored in SQLite.
- The safe authenticated user profile is stored through the Phase 16 SQLite repository.

The API remains authoritative for account status, role, identity, and authentication. No password, role decision, reservation rule, MongoDB access, or bearer token is implemented in SQLite.

## Created files

- `android/app/src/main/java/com/smartsolar/microgrid/data/remote/AuthDtos.kt`
- `android/app/src/main/java/com/smartsolar/microgrid/data/remote/AuthApiService.kt`
- `android/app/src/main/java/com/smartsolar/microgrid/data/remote/ApiClient.kt`
- `android/app/src/main/java/com/smartsolar/microgrid/data/security/SecureTokenStore.kt`
- `android/app/src/main/java/com/smartsolar/microgrid/data/auth/AuthRepository.kt`
- `android/app/src/main/java/com/smartsolar/microgrid/LoginActivity.kt`
- `android/app/src/main/res/layout/activity_login.xml`
- `docs/phase-17.md`

## Modified files

- `android/app/build.gradle.kts`: Retrofit, OkHttp, Gson, coroutines, and lifecycle dependencies.
- `android/app/src/main/AndroidManifest.xml`: LoginActivity launcher and protected MainActivity.
- `android/app/src/main/java/com/smartsolar/microgrid/MainActivity.kt`: secure-session guard.
- `android/README.md`, `README.md`, and `docs/phases.md`: updated Phase 17 boundary.

## Verification

- Android source paths and manifest structure were checked.
- `git diff --check` passes.
- Android Gradle compilation is not available in this environment because Gradle and the Android SDK are not installed/configured. Android Studio should perform the first dependency sync and build.

## Manual testing

1. Open the `android` folder in Android Studio and sync Gradle.
2. Start the ASP.NET Core API on the configured host; the emulator uses `10.0.2.2:5080`.
3. Launch the app and sign in with an active Backoffice, Grid Operator, or approved Prosumer account.
4. Confirm invalid credentials show a readable error and do not create a local user record.
5. Kill and relaunch the app; confirm `/api/auth/me` restores the session.
6. Clear the secure session in the later authenticated shell; confirm MainActivity redirects to LoginActivity.
7. Inspect local app data only through development tooling and confirm no password or raw bearer token is in SQLite.

## Requirements satisfied

Phase 17 satisfies native Retrofit API integration, login, authenticated-session restoration, Kotlin coroutine calls, secure Android Keystore token handling, SQLite safe-profile persistence, launcher/session protection, and understandable authentication errors. Prosumer registration and feature workflows remain later phases.

# Phase 18: Prosumer Android functions

## Delivered scope

- Native Kotlin/XML registration with NIC, contact details, password confirmation, and a pending-approval result. Registration never creates a signed-in session.
- Authenticated home screen with server-returned reservation counts and microgrid availability. Today/completed figures use the API's UTC day; availability is network-wide.
- Profile retrieval, editing full name/email/phone, immutable NIC/role/status display, and explicit confirmation before requesting deactivation.
- Pending deactivation status, refresh/retry, sign-out with session/back-stack clearing, and expired-session redirects.
- Server validation/conflict messages, connection and throttling states, coroutine cancellation, and off-main-thread SQLite profile writes. Temporary connection errors preserve the encrypted session.

## Architecture and phase boundary

Activities use the existing AuthRepository and Retrofit service. The ASP.NET Core API owns validation, identity, role authorization, approval, and account status transitions. Only successful server-returned profiles update SQLite; passwords and bearer tokens never enter it. Android Keystore continues to protect tokens. No backend contract changes were needed.

Staff sign-in remains available with a staff landing message; prosumer profile controls appear only after a server profile response. This visibility is presentation, not an authorization boundary.

Phase 19 owns station/slot selection and booking workflows. Maps are Phase 20; QR and Android operator workflows are Phase 21. None are implemented here.

The debug build permits HTTP only to emulator host `10.0.2.2`, following [Android network security configuration](https://developer.android.com/privacy-and-security/security-config). Release retains HTTPS-only defaults. Screen insets account for system bars and the keyboard using [Android's inset guidance](https://developer.android.com/develop/ui/views/layout/edge-to-edge).

## Created and modified files

Created under `android/app/src/main/`:

- `java/com/smartsolar/microgrid/AccountActivity.kt`: shared asynchronous request/session handling and API errors.
- `java/com/smartsolar/microgrid/RegisterActivity.kt` and `ProfileActivity.kt`.
- `res/layout/activity_register.xml` and `activity_profile.xml`.

Also created:

- `android/app/src/debug/AndroidManifest.xml` and `res/xml/debug_network_security.xml`.
- `android/app/src/test/java/com/smartsolar/microgrid/data/remote/ProsumerApiTest.kt`.
- `android/app/src/test/java/com/smartsolar/microgrid/AccountErrorTest.kt`.
- `android/gradlew`, `android/gradlew.bat`, and `android/gradle/wrapper/gradle-wrapper.jar` / `gradle-wrapper.properties`: Gradle 8.9 wrapper with a pinned distribution checksum.
- This completion/testing record.

Modified: MainActivity, LoginActivity, AuthRepository, AuthApiService, AuthDtos, main/login layouts, string resources, main manifest, app Gradle dependencies, `.gitignore` (Kotlin build cache), and the root/Android README and phase tracker.

## Verification

- `assembleDebug`: passed; APK at `android/app/build/outputs/apk/debug/app-debug.apk`.
- `testDebugUnitTest`: passed, 8 tests, 0 failures/errors.
- `lintDebug`: passed, 0 errors and 17 non-blocking warnings (dependency versions, background overdraw, unused color, missing launcher icon and existing login text resources).
- `git diff --check`: passed.
- No connected device/emulator: UI and live-backend walkthroughs below are not claimed as executed.

The first build exposed conflicting machine SDK environment paths; the documented command fixes them for the process. Lint also caught a missing explicit `includeSubdomains` attribute in the new debug network configuration; it was fixed before the successful final run. No lint baseline or suppression was added.

The installed SDK includes platform 35. Gradle installed the AGP-required build-tools 34.0.0 using the existing accepted SDK license. Gradle 8.9 was downloaded from the official distribution and SHA-256 verified. From the repository root, use the Android Studio JDK and full SDK when running:

```powershell
$env:JAVA_HOME = 'C:/Program Files/Android/Android Studio/jbr'
$env:ANDROID_HOME = "$env:LOCALAPPDATA/Android/Sdk"
$env:ANDROID_SDK_ROOT = $env:ANDROID_HOME
.\android\gradlew.bat -p android assembleDebug testDebugUnitTest lintDebug --console=plain
git diff --check
```

Contract tests cover registration fields and pending status, profile field restrictions, the body-free deactivation route, server dashboard counts/decimal precision, and HTTP 400/401/403/409/429/500 propagation. Additional tests cover ProblemDetails titles, validation arrays, malformed responses and safe server errors. They use MockWebServer and never create accounts in MongoDB.

## Manual walkthrough

1. Start the backend HTTP profile on port 5080, open Android Studio, sync and launch a debug build in an emulator.
2. Choose **Create prosumer account**. Submit mismatched passwords, then invalid NIC/contact data and a duplicate email/NIC. Confirm readable validation/conflict feedback, retained contact input, and no signed-in session.
3. Register a new test prosumer with a 12-128 character password. Confirm the pending-approval message. Verify login is rejected until Backoffice activates the account.
4. Sign in after activation. Compare dashboard values with `/api/reservations/dashboard` for that user's token. An account with no reservations should show server-returned zero counts.
5. Open **My profile**. Verify NIC, role and status are read-only. Save valid contact changes; reopen the screen and verify persistence. Submit an existing email and verify the conflict does not overwrite the saved profile.
6. Request deactivation, first cancelling the confirmation, then confirming. Verify `DEACTIVATION_REQUESTED`, review messaging and hidden request button. Confirm Backoffice still owns final deactivation.
7. Disconnect the API during profile/dashboard refresh. Verify an error and successful retry after reconnecting, without losing the session. Expire/revoke the token and verify the next protected request returns to sign-in.
8. Sign out and press Back/relaunch: private screens must remain inaccessible. Sign in as staff and verify prosumer profile controls are absent.
9. Test rotation, a small display and an open keyboard. Registration passwords must not be restored from saved view state. Inspect SQLite using Android Studio to confirm only safe profile data is persisted.

These interactive checks require an emulator/device; none was connected during implementation.

## Requirements covered and remaining

Phase 18 covers native prosumer registration, home/dashboard, profile viewing/editing, deactivation requests, secure sign-out and API-owned account rules. Continue only when requested with Phase 19 booking functions. Later phases retain Maps, QR/operator workflow, wider UI polish/testing, IIS deployment and final rubric verification.

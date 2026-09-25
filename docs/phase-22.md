# Phase 22 - Error Handling and UI Polish

Phase 22 integrates the Android work through Phase 21 and tightens user-facing error handling and presentation. No backend contract, business rule, database, web portal or deployment scope was changed in this phase.

## Implemented scope

- Shared API error handling now includes explicit 401 session-expired and 408 timeout messages.
- Login uses the shared API error parser for non-credential failures while keeping clear invalid-login and inactive-account messages.
- Registration success dialog text now comes from Android string resources.
- Home dashboard recent-activity text now uses resource placeholders instead of visible string concatenation.
- Nearby-station map coordinates are formatted consistently and station result labels use plural resources.
- Operator QR verification no longer enables transfer completion if a verification response is not valid.
- The app theme owns the surface background color, removing redundant root-layout background painting.
- Added a simple application vector icon.
- Removed an unused color resource and hardcoded login layout text.
- Added coverage for session-expired and timeout error messages.

## Verification

Run from the repository root with Android Studio's bundled JDK and a valid Android SDK:

```powershell
.\android\gradlew.bat -p android assembleDebug testDebugUnitTest lintDebug
```

Final Android verification:

- `assembleDebug`: passed.
- `testDebugUnitTest`: 33 tests passed, 0 failures, 0 errors, 0 skipped.
- `lintDebug`: passed with 0 errors and 11 warnings.

The remaining lint warnings are dependency-version suggestions plus the existing custom splash-screen warning. Dependency upgrades and any splash architecture change are left for Phase 23 or a later compatibility pass because they can affect the full Android test surface.

Manual checks:

1. Sign in with blank, invalid, inactive and valid credentials and confirm readable messages.
2. Open the home dashboard, nearby-stations map, booking details, QR display and operator QR screens and verify text is readable and controls remain role-appropriate.
3. Disconnect the API during login, dashboard refresh, booking load and QR verification and confirm the app shows a retryable connection message.
4. Use an approved QR token, an already-completed token and a malformed token to confirm the operator screen does not allow completion after invalid verification.

Phase 23 remains automated testing consolidation. Phase 24 remains IIS deployment and production configuration.

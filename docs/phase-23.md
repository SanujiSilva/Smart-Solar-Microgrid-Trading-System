# Phase 23 - Automated Testing Consolidation

Phase 23 consolidates the automated test surface across backend, web and Android. It does not change production business contracts, IIS deployment, database configuration or final rubric packaging.

## Implemented scope

- Added `scripts/test-phase23.ps1`, a repository-level PowerShell runner for the Phase 23 automated checks:
  - backend .NET tests,
  - web production build,
  - web Oxlint,
  - web Playwright E2E tests,
  - Android debug build, unit tests and lint.
- Fixed the React Router protected workspace route so browser tests run without nested-route warnings.
- Tightened one Playwright assertion that was ambiguous between the success alert and the loading live region.
- Documented which automated checks are default and which require external services or device hardware.

## Running The Suite

From the repository root:

```powershell
.\scripts\test-phase23.ps1
```

Optional flags:

```powershell
.\scripts\test-phase23.ps1 -SkipWebE2E
.\scripts\test-phase23.ps1 -SkipAndroid
```

Backend MongoDB integration tests use the existing `SMARTSOLAR_TEST_MONGODB_URI` environment variable. When it is not set, the real-database tests are explicitly skipped and the deterministic non-database tests still run.

Android checks require Android Studio's bundled JDK and an Android SDK. The script sets `JAVA_HOME`, `ANDROID_HOME` and `ANDROID_SDK_ROOT` for the current process when the standard local paths exist.

## Verification

Commands run during Phase 23:

```powershell
dotnet test backend/SmartSolarMicrogrid.sln --configuration Release --no-restore
npm.cmd run build
npm.cmd run lint
npm.cmd run test:e2e
.\android\gradlew.bat -p android assembleDebug testDebugUnitTest lintDebug --console=plain
.\scripts\test-phase23.ps1
```

Recorded results:

- Backend default tests: 34 passed, 0 failed, 50 skipped. The skipped tests are MongoDB-backed integration tests skipped because `SMARTSOLAR_TEST_MONGODB_URI` was not set for this verification pass.
- Web build: passed.
- Web lint: passed.
- Web Playwright E2E: 5 passed, 0 failed.
- Android build/unit/lint: passed with 33 unit tests, 0 failures, 0 errors, 0 skipped, and lint with 0 errors and 11 warnings.

Manual device tests remain outside Phase 23's automated scope: Google Maps tile rendering with a real key, device location, camera QR scanning and end-to-end QR completion against a running API should still be walked through on emulator or hardware.

Phase 24 remains IIS deployment and production configuration. Phase 25 remains final assignment/rubric verification.

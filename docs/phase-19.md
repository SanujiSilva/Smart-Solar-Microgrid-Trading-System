# Phase 19: Native Android booking functions

## Delivered

- **New booking** opens a searchable, paginated directory of active stations.
- Station details show server coordinates, capacity, status, weekly operating schedule and open slots. Nearby-station results also link to this screen.
- Selecting a slot loads its latest details and presents an energy input and confirmation dialog. Successful creation shows the server's reservation code, pending status and summary.
- **My bookings** provides Current, Pending, History and Search views. Current means unresolved `PENDING`/`APPROVED` records, not a locally computed eligibility window. History comes from the history endpoint.
- Search supports reservation code, station, status and an inclusive local calendar-date range. Dates are converted to the API's inclusive start / exclusive end timestamps. The station filter includes inactive stations for historical searches.
- Details show identity, station, scheduled time, amount, status and audit timestamps. Modification edits the energy amount supported by the existing API; cancellation requires confirmation. Terminal records are read-only in the UI.
- Loading, empty, validation, unavailable-server, not-found, expired-session and business-rule conflict states are handled.

## Architecture and boundaries

The new `BookingApiService` and DTOs use the existing authenticated Retrofit client. Create sends only slot ID and a decimal energy amount; update sends only energy amount. No role, NIC, user ID, approval status or booking date is supplied as authority. The backend continues to own identity, seven-day scheduling, twelve-hour notice, capacity, status transitions and concurrent changes.

Reservation writes run in a ViewModel and survive configuration changes. Saved state records the draft, returned reservation ID and unresolved mutation flag. Duplicate taps are ignored while a request is running. Connection retries are disabled for the booking client. If a write response is lost, the app does not automatically repeat it: creation directs the user to check My bookings, while modification/cancellation require reloading the record before another change. This is not a server idempotency guarantee, and deliberately creating another booking remains a separate user action.

Booking data is loaded from the API, with no offline mutation queue or authoritative reservation data in SQLite. The existing safe profile/reference cache and encrypted token store remain in use.

The API's `/reservations/pending` endpoint is staff-only, so the Android prosumer Pending view uses identity-scoped `/reservations/search?status=PENDING`. Current/history endpoints return complete lists; the screen displays them in pages of 20. Search and station directory pagination are server-side. This matches the existing contracts without modifying the backend.

Google Maps remains Phase 20. QR display/scanning and the Android operator workflow remain Phase 21. Moving a reservation to another slot is not exposed by the current update contract; this phase implements its supported energy modification operation.

## Created files

Under `android/app/src/main/java/com/smartsolar/microgrid/`:

- `data/remote/BookingApiService.kt`, `BookingDtos.kt`
- `booking/StationDirectoryActivity.kt`, `StationDetailsActivity.kt`
- `booking/BookingsActivity.kt`, `BookingEditorActivity.kt`
- `booking/BookingEditorModel.kt`, `BookingPresentation.kt`

Under `android/app/src/main/res/layout/`:

- `activity_station_directory.xml`, `activity_station_details.xml`
- `activity_bookings.xml`, `activity_booking_editor.xml`

Under `android/app/src/test/java/com/smartsolar/microgrid/`:

- `data/remote/BookingApiTest.kt`
- `booking/BookingEditorModelTest.kt`, `BookingPresentationTest.kt`

## Modified files

- `android/app/build.gradle.kts`: matching lifecycle ViewModel/saved-state dependencies and coroutine test support.
- Android manifest: four private booking activities.
- MainActivity, NearbyStationsActivity and the home layout: booking navigation.
- ApiClient: shared Retrofit construction and disabled connection retries for booking requests.
- AccountActivity and AccountErrorTest: not-found feedback and stable error parsing across repeated screen renders.
- String resources, root README, Android README and phase tracker.

## Verification

The Android debug build, unit tests and lint were run using Gradle 8.9, the Android Studio JDK and SDK 35.

- `assembleDebug`: passed; APK at `android/app/build/outputs/apk/debug/app-debug.apk`.
- `testDebugUnitTest`: 23 tests passed, 0 failures/errors, including 14 new booking/error-rendering checks.
- `lintDebug`: passed with 0 errors and 23 non-blocking warnings. These cover pinned dependency updates and existing splash, icon, text and visual-resource issues; no warning suppression or baseline was added.
- `git diff --check`: passed.

```powershell
$env:JAVA_HOME = 'C:/Program Files/Android/Android Studio/jbr'
$env:ANDROID_HOME = "$env:LOCALAPPDATA/Android/Sdk"
$env:ANDROID_SDK_ROOT = $env:ANDROID_HOME
.\android\gradlew.bat -p android assembleDebug testDebugUnitTest lintDebug --console=plain
git diff --check
```

Tests exercise exact request fields and decimal precision, identity-free booking requests, current/history/search routes, filter encoding, server errors, date/time-zone conversion including daylight-saving boundaries, duplicate taps, uncertain results, saved-state restoration, and error re-rendering. MockWebServer and ViewModel fakes make no changes to Atlas.

No emulator or device was connected, so interactive UI and live Android-to-API tests are not claimed as executed. Backend/web files were not changed in this phase.

## Manual walkthrough

1. Start the backend on HTTP port 5080 with Atlas or a MongoDB replica set. Sign in to the Android debug app as an active prosumer. Use Backoffice to prepare an active station with battery availability, an operating schedule and an open slot within seven days.
2. Choose **New booking**, search for the station and open its details. Verify its schedule/time zone, coordinates and available slot values against the API. The slot times use the device time zone. Also enter through **Nearby stations → Available slots**.
3. Select a slot, enter a positive decimal amount and choose **Review booking**. Cancel the dialog once, then confirm. Check the returned code, pending status and summary. Rapid taps and rotation during submission must not submit another create request.
4. Open **My bookings**. Verify the record appears in Current and Pending. Approve it in Backoffice, refresh, and confirm the updated status in Current and details.
5. Search by code, station and status, then use start/end dates. A single-day range should include the whole local day. Check empty results and multiple pages; clear filters to reset the search.
6. Modify an eligible booking's energy amount, then reload to confirm the saved amount. Try excessive capacity and a change inside twelve hours; the server's conflict explanation should appear and the screen must not claim success.
7. Cancel an eligible booking, first declining and then confirming the dialog. Confirm its cancelled status, disappearance from Current/Pending and presence in History. Completed/rejected/cancelled details should have no mutation controls.
8. Try a slot beyond seven days, an unavailable station/slot, and a missing record. Confirm understandable server errors. Expire/revoke the token and verify the next protected request returns to sign-in.
9. Interrupt the connection during submission. A lost create response must direct the user to My bookings and prevent automatic resubmission. A lost modification/cancellation response must require reloading before another change.
10. Rotate while entering an amount, during a request and on a successful summary; inspect small screens and keyboard behavior. Sign in as staff and confirm the home screen exposes no prosumer booking entry buttons.

## Coverage and remaining work

Phase 19 delivers the native station/slot selection, create/confirmation, current/pending/history, search, details, modification and cancellation workflows, with server-owned business rules. Phase 20 and subsequent work remain pending.

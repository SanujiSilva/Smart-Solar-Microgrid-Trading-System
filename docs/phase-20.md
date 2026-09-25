# Phase 20 - Google Maps Android

Phase 20 adds Google Maps to the native Android nearby-station workflow. The API remains the source of truth for station search and availability; the map only presents search results and helps the prosumer choose a search center.

## Implemented scope

- Added Google Maps and Google Play location dependencies to the Android app.
- Added location permissions and a Google Maps API key manifest placeholder.
- Reworked `NearbyStationsActivity` so prosumers can:
  - use the device's last known location after runtime permission approval,
  - tap the map to fill latitude and longitude,
  - enter coordinates manually when location or the Maps key is unavailable,
  - search the existing authenticated nearby-stations API,
  - view returned stations as both markers and list results,
  - open the existing station detail and available-slot flow from a marker or result.
- Added testable map presentation helpers for coordinate validation and marker labels.

## Configuration

Set the Maps key as a Gradle property before building an installable app:

```properties
SMART_SOLAR_MAPS_API_KEY=your_restricted_google_maps_android_key
```

For local development this can go in `android/gradle.properties` or the user-level Gradle properties file. Do not commit real API keys. Restrict the key to the Android package `com.smartsolar.microgrid` and the app signing certificate used for the build.

If the key is missing or invalid, the manual coordinate fields and API-backed list still remain available, but the Google map tile view will not load correctly.

## Verification

Run from the repository root with Android Studio's bundled JDK and a valid Android SDK:

```powershell
.\android\gradlew.bat -p android assembleDebug testDebugUnitTest lintDebug
```

Manual device or emulator checks:

1. Sign in as an active prosumer.
2. Open Nearby stations.
3. Grant location permission and verify the current coordinates populate the form when a location is available.
4. Tap the map and verify the coordinate fields update.
5. Search within a radius that has active stations and verify markers and list rows appear.
6. Tap a station marker info window, then choose Available slots and confirm the Phase 19 station-detail booking flow opens.
7. Deny location permission and verify manual coordinate search still works.

QR display/scanning and Android operator workflows remain Phase 21.

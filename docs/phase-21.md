# Phase 21 - Android QR and Operator Workflow

Phase 21 adds native Android QR support on top of the existing API QR transaction endpoints. The backend still owns token issuance, verification, one-time completion, role authorization, reservation status checks and reuse prevention.

## Implemented scope

- Added ZXing QR dependencies for local QR rendering and camera scanning.
- Added camera permission with optional camera hardware support, so manual token entry still works on devices without a camera.
- Added a prosumer QR screen for approved reservations:
  - issues or refreshes the opaque QR token through `GET /api/reservations/{id}/qr`,
  - renders the returned token as a QR bitmap,
  - displays the server-returned reservation summary.
- Added an operator QR screen:
  - available from the Android home screen for `GRID_OPERATOR` accounts,
  - scans QR codes with the camera or accepts manual token entry,
  - verifies tokens through `POST /api/operator/verify-qr`,
  - requires confirmation before completing transfer through `POST /api/operator/complete-transfer`,
  - hides completion after a successful transfer so the consumed token is not reused from the UI.
- Added a Show QR Code action to approved booking details.
- Added tests for QR endpoint routes/payloads and token/summary presentation.

## Verification

Run from the repository root with Android Studio's bundled JDK and a valid Android SDK:

```powershell
.\android\gradlew.bat -p android assembleDebug testDebugUnitTest lintDebug
```

Manual device or emulator checks:

1. Sign in as a prosumer with an approved reservation.
2. Open My bookings, choose the approved reservation and tap Show QR Code.
3. Verify the QR screen loads a QR code and reservation summary. Try a pending or completed reservation and confirm the API rejects QR issuance with a clear error.
4. Sign in as a Grid Operator.
5. Open Operator QR transfer, scan the prosumer QR code and verify the reservation summary appears.
6. Complete the transfer and verify the reservation becomes COMPLETED.
7. Scan or enter the same token again and confirm the API rejects reuse.
8. Deny camera access or use an emulator without a camera and verify manual token entry still works.

Phase 22 remains UI polish and broader error handling. Phase 23 remains consolidated automated testing.

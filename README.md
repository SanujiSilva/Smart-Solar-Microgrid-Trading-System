# Smart Solar Microgrid Trading System

**Module:** SE4040 – Enterprise Application Development  
**Assignment:** Assignment 1 – Smart Solar Microgrid Trading System  
**Institution:** Sri Lanka Institute of Information Technology (SLIIT)

## Project Repository

GitHub Repository:  
https://github.com/SanujiSilva/Smart-Solar-Microgrid-Trading-System.git

## Demo Video

OneDrive / SharePoint Demo Video:  
https://mysliit-my.sharepoint.com/:f:/g/personal/it22167200_my_sliit_lk/IgBA4YZuZjbjT6sVZKYzMe_rAS0ovfWpxWEEmJp8WR_LLBo?e=fvVcLb

The demo video is kept under five minutes and demonstrates the main end-to-end system workflow.

## Project Overview

The Smart Solar Microgrid Trading System is an enterprise client-server application developed to manage solar microgrid stations, energy reservation workflows, Prosumer accounts and QR-based energy-transfer verification.

The solution consists of:

- ASP.NET Core Web API / C# backend
- MongoDB server-side database
- React + TypeScript + Bootstrap 5 web application
- Native Android application developed with Kotlin and XML
- SQLite local persistence on Android
- Retrofit REST API integration
- Google Maps integration for nearby station discovery
- ZXing QR generation and scanning
- JWT-based authentication and role-based authorization

All important business rules are enforced by the central Web API. The web and Android clients communicate with the backend through REST/JSON and do not access MongoDB directly.

## User Roles

### Backoffice

- Manage Backoffice and Grid Operator accounts
- View and manage Prosumer accounts
- Approve and reactivate Prosumer accounts
- Manage solar microgrid stations
- Configure operating schedules
- Manage energy slots
- View and manage reservations
- Approve or reject reservation requests
- View operational dashboard information

### Grid Operator

- Access operational information
- View reservations and station availability
- Update permitted availability information
- Scan Prosumer QR codes using the Android application
- Verify QR transactions through the central API
- Complete approved energy transfers

### Prosumer

- Register using NIC
- Login after Backoffice activation
- Manage profile information
- Request account deactivation
- Browse solar stations
- Find nearby stations using Google Maps
- View available energy slots
- Create, modify and cancel reservations
- View current, pending and historical bookings
- Display QR codes for approved reservations

## Main Functional Components

### 1. User and Account Management

Responsible for registration, authentication, profile management, account approval, role-based access control, account deactivation and reactivation.

### 2. Station and Energy Slot Management

Responsible for solar station creation and modification, geographic coordinates, operating schedules, slot creation, energy capacity, availability and nearby-station discovery.

### 3. Energy Reservation Management

Responsible for booking creation, approval/rejection, modification, rescheduling, cancellation, booking history, capacity allocation and reservation validation.

### 4. QR Transfer Verification and Monitoring

Responsible for approved-reservation QR generation, Grid Operator verification, one-time transfer completion, completion tracking, reservation monitoring and dashboard information.

## Important Business Rules

- Reservations must be scheduled in the future and within seven days.
- Reservation updates require at least 12 hours' notice.
- Reservation cancellations require at least 12 hours' notice.
- Rescheduled slots must also satisfy the required notice period.
- Station availability, schedules and slot capacity are validated by the server.
- Station deactivation is blocked when restricted by active/future reservations.
- Only Backoffice may reactivate a deactivated Prosumer.
- Prosumers can manage only their own account and reservation data.
- QR verification and transfer completion always use the central API.
- A completed or invalid QR transaction cannot be reused.
- Reservation capacity and lifecycle changes are handled server-side to reduce double booking.

## Project Structure

```text
backend/
  src/SmartSolarMicrogrid.Api/
  tests/SmartSolarMicrogrid.Api.Tests/

web/

android/

database/

docs/

scripts/
```

## Backend

The backend is implemented using ASP.NET Core Web API and follows a layered structure:

```text
Controllers
    ↓
Services
    ↓
Repositories
    ↓
MongoDB
```

The backend handles:

- Authentication
- Authorization
- User management
- Station management
- Energy slot management
- Reservation management
- Capacity validation
- QR token generation and verification
- Dashboard data
- Error handling

## Web Application

The web application is implemented using React, TypeScript and Bootstrap 5.

It provides role-specific interfaces for:

- Backoffice administration
- Grid Operator operational views
- User and Prosumer management
- Station and slot management
- Reservation management
- Dashboard information

## Android Application

The mobile application is developed as a pure native Android application using Kotlin and XML.

Android functionality includes:

- Registration and login
- Prosumer profile management
- SQLite local reference/cache storage
- Station browsing
- Google Maps nearby-station discovery
- Reservation creation and management
- Booking history
- Prosumer QR display
- Grid Operator QR scanning and verification

## Local Persistence

SQLite is used only for permitted Android local information such as authenticated-user reference information and cached station data.

Passwords, reservation authorization and authoritative booking availability are not stored or decided locally. MongoDB through the central Web API remains the authoritative data source.

## Running the Backend

From the repository root:

```powershell
dotnet restore backend/SmartSolarMicrogrid.sln
dotnet build backend/SmartSolarMicrogrid.sln --configuration Release
dotnet run --project backend/src/SmartSolarMicrogrid.Api
```

MongoDB and the required JWT configuration must be available before running the API.

## Running the Web Application

Navigate to the web directory and install dependencies:

```powershell
cd web
npm install
npm run dev
```

Configure the web API base URL to point to the running ASP.NET Core Web API.

## Running the Android Application

1. Open the `android` project in Android Studio.
2. Configure the API base URL.
3. Configure a restricted Google Maps Android API key.
4. Build and run the application on an emulator or Android device.

## Testing and Verification

The project contains automated verification across the backend, web and Android layers.

The repository includes:

- Backend xUnit tests
- Web build and lint verification
- Playwright end-to-end tests
- Android unit tests
- Android lint verification
- MockWebServer-based Android API tests

## Individual Contributions

| Member | Registration Number | Main Component | Individual Contribution |
|---|---|---|---|
| Perera L.K.S.T | IT22167200 | User and Account Management | Contributed to Prosumer registration, authentication, profile management, account approval and status management, staff account management, role-based access control, account deactivation/reactivation, related interfaces, API integration, backend functionality, database operations, validation, testing and documentation. |
| Perera N.S.G | IT22276346 | Energy Reservation Management | Contributed to reservation creation, slot and energy selection, approval/rejection, modification, rescheduling, cancellation, current reservations, booking history, search, booking-window and 12-hour notice rules, ownership/state validation, capacity handling, integration, testing and documentation. |
| Silva K.S.S.G | IT22082374 | QR Transfer Verification and Monitoring | Contributed to approved-reservation QR generation, Grid Operator scanning and verification, transfer completion, QR-reuse prevention, completion tracking, reservation monitoring, search/filtering, dashboard functionality, integration, testing and documentation.  |
| Alwis L.W.R.T | IT22278708 | Station and Energy Slot Management | Contributed to station creation and modification, geographic coordinates, operating schedules, slot creation and management, capacity and availability management, station browsing, nearby-station discovery using Google Maps, API/database integration, testing and documentation. |

## Submission Contents

The final assignment submission should contain:

- Complete project source code
- Detailed project report
- Main opening-screen screenshot
- README file
- Git repository link
- Individual contribution details
- Demo video link

## Authors

SE4040 Enterprise Application Development – Group Assignment  
BSc (Hons) in Information Technology Specialized in Software Engineering  
Sri Lanka Institute of Information Technology

package com.smartsolar.microgrid

import android.Manifest
import android.os.Bundle
import android.view.View
import androidx.core.widget.doAfterTextChanged
import com.smartsolar.microgrid.databinding.ItemSolarRecordBinding
import java.io.IOException
import com.google.android.gms.maps.model.BitmapDescriptorFactory
import android.content.Intent
import android.content.pm.PackageManager
import androidx.activity.result.contract.ActivityResultContracts
import androidx.core.content.ContextCompat
import com.smartsolar.microgrid.booking.StationDetailsActivity
import com.smartsolar.microgrid.booking.StationDirectoryActivity
import com.smartsolar.microgrid.data.remote.StationSummary
import com.smartsolar.microgrid.maps.StationMapPresentation
import com.google.android.gms.location.LocationServices
import com.google.android.gms.maps.CameraUpdateFactory
import com.google.android.gms.maps.GoogleMap
import com.google.android.gms.maps.OnMapReadyCallback
import com.google.android.gms.maps.SupportMapFragment
import com.google.android.gms.maps.model.LatLng
import com.google.android.gms.maps.model.LatLngBounds
import com.google.android.gms.maps.model.Marker
import com.google.android.gms.maps.model.MarkerOptions
import com.google.android.material.button.MaterialButton
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.smartsolar.microgrid.databinding.ActivityNearbyStationsBinding
import java.util.Locale

class NearbyStationsActivity : AccountActivity(), OnMapReadyCallback {
    private lateinit var binding: ActivityNearbyStationsBinding
    private var map: GoogleMap? = null
    private var loadedStations: List<StationSummary> = emptyList()
    private var cachedResults = false
    private var hasSearched = false
    private var stationsByMarker = mutableMapOf<Marker, StationSummary>()
    private val locationClient by lazy { LocationServices.getFusedLocationProviderClient(this) }
    private val locationPermission = registerForActivityResult(ActivityResultContracts.RequestMultiplePermissions()) { grants ->
        if (grants[Manifest.permission.ACCESS_FINE_LOCATION] == true ||
            grants[Manifest.permission.ACCESS_COARSE_LOCATION] == true) {
            useDeviceLocation()
        } else {
            binding.messageText.setText(R.string.location_permission_denied)
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!protectSession()) return
        binding = ActivityNearbyStationsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.root.applyAccountInsets()
        configurePrimaryNavigation(R.id.nav_stations)
        binding.backButton.setOnClickListener { finish() }
        binding.searchButton.setOnClickListener { search() }
        binding.useLocationButton.setOnClickListener { requestDeviceLocation() }
        binding.directoryButton.setOnClickListener { startActivity(Intent(this, StationDirectoryActivity::class.java)) }
        binding.mapListToggle.addOnButtonCheckedListener { _, id, checked ->
            if (checked) binding.mapPanel.visibility = if (id == R.id.mapViewButton) View.VISIBLE else View.GONE
        }
        binding.filterButton.setOnClickListener {
            binding.locationFilters.visibility = if (binding.locationFilters.visibility == View.VISIBLE) View.GONE else View.VISIBLE
        }
        binding.nearbySearchInput.doAfterTextChanged { renderResults() }
        if (savedInstanceState != null) {
            binding.latitudeInput.setText(savedInstanceState.getString("latitude"))
            binding.longitudeInput.setText(savedInstanceState.getString("longitude"))
            binding.radiusInput.setText(savedInstanceState.getString("radius"))
            cachedResults = savedInstanceState.getBoolean("cached")
            hasSearched = savedInstanceState.getBoolean("searched")
            loadedStations = savedInstanceState.getString("stations")?.let {
                com.google.gson.Gson().fromJson(it, Array<StationSummary>::class.java).toList()
            }.orEmpty()
            binding.nearbyStateText.text = savedInstanceState.getString("state_message")
            binding.mapListToggle.check(if (savedInstanceState.getBoolean("list")) R.id.listViewButton else R.id.mapViewButton)
            binding.locationFilters.visibility = if (savedInstanceState.getBoolean("filters", true)) View.VISIBLE else View.GONE
            renderResults()
        }
        val mapFragment = supportFragmentManager.findFragmentById(R.id.map) as SupportMapFragment
        mapFragment.getMapAsync(this)
    }

    override fun onSaveInstanceState(outState: Bundle) {
        if (::binding.isInitialized) {
            outState.putString("latitude", binding.latitudeInput.text.toString())
            outState.putString("longitude", binding.longitudeInput.text.toString())
            outState.putString("radius", binding.radiusInput.text.toString())
            outState.putBoolean("cached", cachedResults)
            outState.putBoolean("searched", hasSearched)
            outState.putBoolean("list", binding.mapListToggle.checkedButtonId == R.id.listViewButton)
            outState.putBoolean("filters", binding.locationFilters.visibility == View.VISIBLE)
            outState.putString("stations", com.google.gson.Gson().toJson(loadedStations))
            outState.putString("state_message", binding.nearbyStateText.text.toString())
        }
        super.onSaveInstanceState(outState)
    }

    override fun onMapReady(googleMap: GoogleMap) {
        map = googleMap.apply {
            uiSettings.isZoomControlsEnabled = true
            setOnMapClickListener { point ->
                fillCoordinates(point.latitude, point.longitude)
                animateCamera(CameraUpdateFactory.newLatLngZoom(point, DEFAULT_ZOOM))
            }
            setOnMarkerClickListener { marker ->
                stationsByMarker[marker]?.let { station ->
                    binding.selectedStationText.text = getString(R.string.station_card_body, station.stationCode, station.address, station.capacityKWh.toString(), station.availableBatterySlots)
                }
                false
            }
            setOnInfoWindowClickListener { marker ->
                stationsByMarker[marker]?.let { showStation(it) }
            }
        }
        enableMyLocationLayer()
        renderResults()
    }

    private fun search() {
        val input = StationMapPresentation.parseCoordinates(
            binding.latitudeInput.text.toString(),
            binding.longitudeInput.text.toString(),
            binding.radiusInput.text.toString(),
        )
        if (input == null) {
            binding.messageText.setText(R.string.coordinates_required)
            return
        }
        request(binding.progressBar, binding.messageText,
            listOf(binding.searchButton, binding.useLocationButton, binding.latitudeInput, binding.longitudeInput, binding.radiusInput)) {
            cachedResults = false
            try {
                loadedStations = account.nearby(input.latitude, input.longitude, input.radiusKm).items
                binding.nearbyStateText.text = ""
            } catch (error: IOException) {
                val cache = account.cachedStations()
                if (cache.isEmpty()) throw error
                cachedResults = true
                loadedStations = cache.map { StationSummary(it.stationId, it.stationCode, it.name, it.address, it.latitude, it.longitude, it.capacityKWh, it.availableBatterySlots, it.status) }
                val time = java.text.DateFormat.getDateTimeInstance().format(java.util.Date(cache.minOf { it.fetchedAtEpochMillis }))
                binding.nearbyStateText.text = getString(R.string.cached_stations_notice, time)
                binding.nearbyStateText.solarBanner(SolarTone.WARNING)
            }
            hasSearched = true
            renderResults()
        }
    }

    private fun renderResults() {
        val query = binding.nearbySearchInput.text.toString().trim()
        val stations = loadedStations.filter { query.isBlank() || (it.name + " " + it.stationCode + " " + it.address).contains(query, ignoreCase = true) }
        binding.results.removeAllViews()
        binding.selectedStationText.setText(R.string.nearby_preview_hint)
        if (hasSearched && stations.isEmpty() && !cachedResults) binding.nearbyStateText.setText(if (loadedStations.isEmpty()) R.string.no_stations else R.string.no_loaded_matches)
        else if (hasSearched && !cachedResults) binding.nearbyStateText.text = ""
        stations.forEach { station ->
            val row = ItemSolarRecordBinding.inflate(layoutInflater, binding.results, false)
            row.bindSolarRecord(SolarRecord(station.id, station.name,
                getString(R.string.station_card_body, station.stationCode, station.address, station.capacityKWh.toString(), station.availableBatterySlots),
                station.status, getString(if (cachedResults) R.string.cached_station_action else R.string.view_details_action)) { showStation(station) })
            binding.results.addView(row.root)
        }
        val input = StationMapPresentation.parseCoordinates(binding.latitudeInput.text.toString(), binding.longitudeInput.text.toString(), binding.radiusInput.text.toString())
        if (input != null) renderMap(input.latitude, input.longitude, stations)
    }

    private fun requestDeviceLocation() {
        if (hasLocationPermission()) {
            useDeviceLocation()
        } else {
            locationPermission.launch(arrayOf(
                Manifest.permission.ACCESS_FINE_LOCATION,
                Manifest.permission.ACCESS_COARSE_LOCATION,
            ))
        }
    }

    private fun useDeviceLocation() {
        if (!hasLocationPermission()) return
        enableMyLocationLayer()
        locationClient.lastLocation
            .addOnSuccessListener { location ->
                if (location == null) {
                    binding.messageText.setText(R.string.location_unavailable)
                } else {
                    fillCoordinates(location.latitude, location.longitude)
                    map?.animateCamera(CameraUpdateFactory.newLatLngZoom(
                        LatLng(location.latitude, location.longitude),
                        DEFAULT_ZOOM,
                    ))
                    search()
                }
            }
            .addOnFailureListener { binding.messageText.setText(R.string.location_unavailable) }
    }

    private fun enableMyLocationLayer() {
        if (hasLocationPermission()) {
            runCatching { map?.isMyLocationEnabled = true }
        }
    }

    private fun hasLocationPermission(): Boolean =
        ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED ||
            ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_COARSE_LOCATION) == PackageManager.PERMISSION_GRANTED

    private fun renderMap(latitude: Double, longitude: Double, stations: List<StationSummary>) {
        val googleMap = map ?: return
        googleMap.clear()
        stationsByMarker.clear()
        val origin = LatLng(latitude, longitude)
        googleMap.addMarker(MarkerOptions().position(origin).title(getString(R.string.search_center)))
        if (stations.isEmpty()) {
            googleMap.animateCamera(CameraUpdateFactory.newLatLngZoom(origin, DEFAULT_ZOOM))
            return
        }
        val bounds = LatLngBounds.builder().include(origin)
        stations.forEach { station ->
            val marker = StationMapPresentation.markerFor(station)
            val point = LatLng(marker.latitude, marker.longitude)
            googleMap.addMarker(MarkerOptions().position(point).title(marker.title).snippet(getString(R.string.map_marker_slots, station.stationCode, station.availableBatterySlots)).icon(BitmapDescriptorFactory.defaultMarker(BitmapDescriptorFactory.HUE_VIOLET)))?.let {
                stationsByMarker[it] = station
            }
            bounds.include(point)
        }
        googleMap.animateCamera(CameraUpdateFactory.newLatLngBounds(bounds.build(), MAP_PADDING))
    }

    private fun fillCoordinates(latitude: Double, longitude: Double) {
        binding.latitudeInput.setText(String.format(Locale.US, "%.6f", latitude))
        binding.longitudeInput.setText(String.format(Locale.US, "%.6f", longitude))
    }

    private fun showStation(station: StationSummary) {
        MaterialAlertDialogBuilder(this@NearbyStationsActivity)
            .setTitle(station.name)
            .setMessage(getString(R.string.station_details, station.stationCode, station.address,
                station.status, station.capacityKWh.toString(), station.availableBatterySlots,
                station.latitude.toString(), station.longitude.toString()))
            .setNeutralButton(if (cachedResults) R.string.retry_action else R.string.available_slots) { _, _ ->
                if (cachedResults) { search(); return@setNeutralButton }
                startActivity(Intent(this@NearbyStationsActivity, StationDetailsActivity::class.java)
                    .putExtra(StationDirectoryActivity.STATION_ID, station.id))
            }
            .setPositiveButton(android.R.string.ok, null).show()
    }

    private companion object {
        const val DEFAULT_ZOOM = 12f
        const val MAP_PADDING = 96
    }
}

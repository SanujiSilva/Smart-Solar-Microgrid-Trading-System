package com.smartsolar.microgrid

import android.Manifest
import android.os.Bundle
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

class NearbyStationsActivity : AccountActivity(), OnMapReadyCallback {
    private lateinit var binding: ActivityNearbyStationsBinding
    private var map: GoogleMap? = null
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
        binding.backButton.setOnClickListener { finish() }
        binding.searchButton.setOnClickListener { search() }
        binding.useLocationButton.setOnClickListener { requestDeviceLocation() }
        val mapFragment = supportFragmentManager.findFragmentById(R.id.map) as SupportMapFragment
        mapFragment.getMapAsync(this)
    }

    override fun onMapReady(googleMap: GoogleMap) {
        map = googleMap.apply {
            uiSettings.isZoomControlsEnabled = true
            setOnMapClickListener { point ->
                fillCoordinates(point.latitude, point.longitude)
                animateCamera(CameraUpdateFactory.newLatLngZoom(point, DEFAULT_ZOOM))
            }
            setOnInfoWindowClickListener { marker ->
                stationsByMarker[marker]?.let { showStation(it) }
            }
        }
        enableMyLocationLayer()
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
        binding.results.removeAllViews()
        map?.clear()
        stationsByMarker.clear()
        request(binding.progressBar, binding.messageText, listOf(binding.searchButton)) {
            val response = account.nearby(input.latitude, input.longitude, input.radiusKm)
            if (response.items.isEmpty()) binding.messageText.setText(R.string.no_stations)
            renderMap(input.latitude, input.longitude, response.items)
            response.items.forEach { station ->
                binding.results.addView(MaterialButton(this).apply {
                    text = getString(R.string.station_map_result, station.name, station.availableBatterySlots)
                    setOnClickListener { showStation(station) }
                })
            }
        }
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
            googleMap.addMarker(MarkerOptions().position(point).title(marker.title).snippet(marker.snippet))?.let {
                stationsByMarker[it] = station
            }
            bounds.include(point)
        }
        googleMap.animateCamera(CameraUpdateFactory.newLatLngBounds(bounds.build(), MAP_PADDING))
    }

    private fun fillCoordinates(latitude: Double, longitude: Double) {
        binding.latitudeInput.setText(latitude.toString())
        binding.longitudeInput.setText(longitude.toString())
    }

    private fun showStation(station: StationSummary) {
        MaterialAlertDialogBuilder(this@NearbyStationsActivity)
            .setTitle(station.name)
            .setMessage(getString(R.string.station_details, station.stationCode, station.address,
                station.status, station.capacityKWh.toString(), station.availableBatterySlots,
                station.latitude.toString(), station.longitude.toString()))
            .setNeutralButton(R.string.available_slots) { _, _ ->
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

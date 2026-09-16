package com.smartsolar.microgrid.data.local

import android.content.ContentValues
import android.database.sqlite.SQLiteDatabase

class LocalRepository(private val helper: LocalDatabaseHelper) {
    fun saveUser(user: LocalUser, updatedAtEpochMillis: Long = System.currentTimeMillis()) {
        val values = ContentValues().apply {
            put("singleton_id", SINGLETON_ID)
            put("user_id", user.userId)
            putNullable("nic", user.nic)
            put("full_name", user.fullName)
            put("email", user.email)
            put("phone", user.phone)
            put("role", user.role)
            put("status", user.status)
            put("updated_at_epoch_millis", updatedAtEpochMillis)
        }
        helper.writableDatabase.insertWithOnConflict(
            "authenticated_user",
            null,
            values,
            SQLiteDatabase.CONFLICT_REPLACE,
        )
    }

    fun getUser(): LocalUser? = helper.readableDatabase.query(
        "authenticated_user",
        USER_COLUMNS,
        "singleton_id = ?",
        arrayOf(SINGLETON_ID.toString()),
        null,
        null,
        null,
    ).use { cursor ->
        if (!cursor.moveToFirst()) return@use null
        LocalUser(
            userId = cursor.getString(cursor.getColumnIndexOrThrow("user_id")),
            nic = cursor.getStringOrNull("nic"),
            fullName = cursor.getString(cursor.getColumnIndexOrThrow("full_name")),
            email = cursor.getString(cursor.getColumnIndexOrThrow("email")),
            phone = cursor.getString(cursor.getColumnIndexOrThrow("phone")),
            role = cursor.getString(cursor.getColumnIndexOrThrow("role")),
            status = cursor.getString(cursor.getColumnIndexOrThrow("status")),
        )
    }

    fun clearUser() {
        helper.writableDatabase.delete("authenticated_user", null, null)
    }

    fun replaceStationCache(stations: Iterable<CachedStation>) {
        helper.writableDatabase.beginTransaction()
        try {
            helper.writableDatabase.delete("station_reference_cache", null, null)
            stations.forEach { station ->
                helper.writableDatabase.insertOrThrow("station_reference_cache", null, station.toContentValues())
            }
            helper.writableDatabase.setTransactionSuccessful()
        } finally {
            helper.writableDatabase.endTransaction()
        }
    }

    fun listCachedStations(): List<CachedStation> = helper.readableDatabase.query(
        "station_reference_cache",
        STATION_COLUMNS,
        null,
        null,
        null,
        null,
        "name COLLATE NOCASE ASC",
    ).use { cursor ->
        buildList {
            while (cursor.moveToNext()) {
                add(
                    CachedStation(
                        stationId = cursor.getString(cursor.getColumnIndexOrThrow("station_id")),
                        stationCode = cursor.getString(cursor.getColumnIndexOrThrow("station_code")),
                        name = cursor.getString(cursor.getColumnIndexOrThrow("name")),
                        address = cursor.getString(cursor.getColumnIndexOrThrow("address")),
                        latitude = cursor.getDouble(cursor.getColumnIndexOrThrow("latitude")),
                        longitude = cursor.getDouble(cursor.getColumnIndexOrThrow("longitude")),
                        capacityKWh = cursor.getDouble(cursor.getColumnIndexOrThrow("capacity_kwh")),
                        availableBatterySlots = cursor.getInt(cursor.getColumnIndexOrThrow("available_battery_slots")),
                        status = cursor.getString(cursor.getColumnIndexOrThrow("status")),
                        fetchedAtEpochMillis = cursor.getLong(cursor.getColumnIndexOrThrow("fetched_at_epoch_millis")),
                    ),
                )
            }
        }
    }

    fun clearStationCache() {
        helper.writableDatabase.delete("station_reference_cache", null, null)
    }

    fun close() = helper.close()

    private fun ContentValues.putNullable(key: String, value: String?) {
        if (value == null) putNull(key) else put(key, value)
    }

    private fun CachedStation.toContentValues() = ContentValues().apply {
        put("station_id", stationId)
        put("station_code", stationCode)
        put("name", name)
        put("address", address)
        put("latitude", latitude)
        put("longitude", longitude)
        put("capacity_kwh", capacityKWh)
        put("available_battery_slots", availableBatterySlots)
        put("status", status)
        put("fetched_at_epoch_millis", fetchedAtEpochMillis)
    }

    private fun android.database.Cursor.getStringOrNull(column: String): String? {
        val index = getColumnIndexOrThrow(column)
        return if (isNull(index)) null else getString(index)
    }

    companion object {
        private const val SINGLETON_ID = 1
        private val USER_COLUMNS = arrayOf("user_id", "nic", "full_name", "email", "phone", "role", "status")
        private val STATION_COLUMNS = arrayOf(
            "station_id", "station_code", "name", "address", "latitude", "longitude",
            "capacity_kwh", "available_battery_slots", "status", "fetched_at_epoch_millis",
        )
    }
}

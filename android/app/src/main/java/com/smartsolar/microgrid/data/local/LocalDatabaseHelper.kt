package com.smartsolar.microgrid.data.local

import android.content.Context
import android.database.sqlite.SQLiteDatabase
import android.database.sqlite.SQLiteOpenHelper

class LocalDatabaseHelper(context: Context) : SQLiteOpenHelper(
    context,
    DATABASE_NAME,
    null,
    DATABASE_VERSION,
) {
    override fun onCreate(database: SQLiteDatabase) {
        database.execSQL(
            """
            CREATE TABLE authenticated_user (
                singleton_id INTEGER PRIMARY KEY CHECK (singleton_id = 1),
                user_id TEXT NOT NULL,
                nic TEXT,
                full_name TEXT NOT NULL,
                email TEXT NOT NULL,
                phone TEXT NOT NULL,
                role TEXT NOT NULL,
                status TEXT NOT NULL,
                updated_at_epoch_millis INTEGER NOT NULL
            )
            """.trimIndent(),
        )
        database.execSQL(
            """
            CREATE TABLE station_reference_cache (
                station_id TEXT PRIMARY KEY,
                station_code TEXT NOT NULL,
                name TEXT NOT NULL,
                address TEXT NOT NULL,
                latitude REAL NOT NULL,
                longitude REAL NOT NULL,
                capacity_kwh REAL NOT NULL,
                available_battery_slots INTEGER NOT NULL,
                status TEXT NOT NULL,
                fetched_at_epoch_millis INTEGER NOT NULL
            )
            """.trimIndent(),
        )
        database.execSQL(
            "CREATE INDEX ix_station_cache_code ON station_reference_cache(station_code)",
        )
    }

    override fun onUpgrade(database: SQLiteDatabase, oldVersion: Int, newVersion: Int) {
        // Future schema migrations must preserve cached data; no destructive fallback is used.
    }

    companion object {
        private const val DATABASE_NAME = "smart_solar_local.db"
        private const val DATABASE_VERSION = 1
    }
}

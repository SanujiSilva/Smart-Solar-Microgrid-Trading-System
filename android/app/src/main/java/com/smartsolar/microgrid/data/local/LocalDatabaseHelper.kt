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
        createUserTable(database)
        createStationTable(database)
    }

    private fun createUserTable(database: SQLiteDatabase) {
        database.execSQL(
            """
            CREATE TABLE authenticated_user (
                principal_key TEXT NOT NULL PRIMARY KEY,
                singleton_id INTEGER NOT NULL UNIQUE CHECK (singleton_id = 1),
                user_id TEXT NOT NULL,
                nic TEXT,
                full_name TEXT NOT NULL,
                email TEXT NOT NULL,
                phone TEXT NOT NULL,
                role TEXT NOT NULL,
                status TEXT NOT NULL,
                updated_at_epoch_millis INTEGER NOT NULL,
                CHECK ((role = 'PROSUMER' AND nic IS NOT NULL AND principal_key = nic)
                    OR (role <> 'PROSUMER' AND principal_key = user_id))
            )
            """.trimIndent(),
        )
    }

    private fun createStationTable(database: SQLiteDatabase) {
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
        if (oldVersion < 2) {
            // SQLiteOpenHelper runs upgrades in a transaction; preserve the session and station cache.
            database.execSQL("ALTER TABLE authenticated_user RENAME TO authenticated_user_v1")
            createUserTable(database)
            database.execSQL("""
                INSERT INTO authenticated_user
                    (principal_key, singleton_id, user_id, nic, full_name, email, phone, role, status, updated_at_epoch_millis)
                SELECT CASE WHEN role = 'PROSUMER' THEN UPPER(TRIM(nic)) ELSE user_id END,
                    singleton_id, user_id, UPPER(TRIM(nic)), full_name, email, phone, role, status, updated_at_epoch_millis
                FROM authenticated_user_v1
            """.trimIndent())
            database.execSQL("DROP TABLE authenticated_user_v1")
        }
    }

    companion object {
        private const val DATABASE_NAME = "smart_solar_local.db"
        private const val DATABASE_VERSION = 2
    }
}

"""Exercise the Android v1-to-v2 SQL against a real in-memory SQLite database."""
from pathlib import Path
import re
import sqlite3
import unittest

SOURCE = (Path(__file__).resolve().parents[1] / "android/app/src/main/java/com/smartsolar/microgrid/data/local/LocalDatabaseHelper.kt").read_text()
SQL = re.findall(r'"""(.*?)"""', SOURCE, re.S)
USER_SCHEMA = next(sql for sql in SQL if "CREATE TABLE authenticated_user" in sql)
STATION_SCHEMA = next(sql for sql in SQL if "CREATE TABLE station_reference_cache" in sql)
MIGRATION = next(sql for sql in SQL if "INSERT INTO authenticated_user" in sql)


class IdentityMigrationTest(unittest.TestCase):
    def test_existing_prosumer_and_operator_sessions_and_cache_survive(self):
        for role, nic, key in [("PROSUMER", " 991234567v ", "991234567V"), ("GRID_OPERATOR", None, "auth-id")]:
            with self.subTest(role=role), sqlite3.connect(":memory:") as database:
                database.execute("""CREATE TABLE authenticated_user (
                    singleton_id INTEGER PRIMARY KEY CHECK (singleton_id = 1),
                    user_id TEXT NOT NULL, nic TEXT, full_name TEXT NOT NULL, email TEXT NOT NULL,
                    phone TEXT NOT NULL, role TEXT NOT NULL, status TEXT NOT NULL,
                    updated_at_epoch_millis INTEGER NOT NULL)""")
                database.execute("INSERT INTO authenticated_user VALUES (1, 'auth-id', ?, 'Name', 'mail@example.invalid', '0771234567', ?, 'ACTIVE', 123)", (nic, role))
                database.execute(STATION_SCHEMA)
                database.execute("INSERT INTO station_reference_cache VALUES ('node', 'N1', 'Name', 'Address', 6.9, 79.8, 10, 2, 'ACTIVE', 123)")
                database.execute("ALTER TABLE authenticated_user RENAME TO authenticated_user_v1")
                database.execute(USER_SCHEMA)
                database.execute(MIGRATION)
                database.execute("DROP TABLE authenticated_user_v1")
                row = database.execute("SELECT principal_key, user_id, full_name, role, updated_at_epoch_millis FROM authenticated_user WHERE singleton_id = 1").fetchone()
                self.assertEqual((key, "auth-id", "Name", role, 123), row)
                primary_keys = [row[1] for row in database.execute("PRAGMA table_info(authenticated_user)") if row[5]]
                self.assertEqual(["principal_key"], primary_keys)
                self.assertEqual(1, database.execute("SELECT COUNT(*) FROM station_reference_cache").fetchone()[0])
                with self.assertRaises(sqlite3.IntegrityError):
                    database.execute("UPDATE authenticated_user SET principal_key = 'wrong-key'")


if __name__ == "__main__":
    unittest.main()

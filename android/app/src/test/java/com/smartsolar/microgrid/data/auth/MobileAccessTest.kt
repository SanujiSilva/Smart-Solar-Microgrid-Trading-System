package com.smartsolar.microgrid.data.auth

import com.smartsolar.microgrid.data.local.LocalUser
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class MobileAccessTest {
    @Test fun allowsSolarProsumerAndGridOperatorOnly() {
        assertTrue(user("PROSUMER").canAccessMobile())
        assertTrue(user("GRID_OPERATOR").canAccessMobile())
        assertFalse(user("BACKOFFICE").canAccessMobile())
        assertFalse(user("UNKNOWN").canAccessMobile())
    }

    private fun user(role: String) = LocalUser(
        userId = "user-id",
        nic = if (role == "PROSUMER") "200012345678" else null,
        fullName = "Test User",
        email = "user@example.test",
        phone = "0771234567",
        role = role,
        status = "ACTIVE",
    )
}

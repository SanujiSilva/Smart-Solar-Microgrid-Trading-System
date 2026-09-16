package com.smartsolar.microgrid

import android.os.Bundle
import android.content.Intent
import androidx.appcompat.app.AppCompatActivity
import com.smartsolar.microgrid.data.security.SecureTokenStore
import com.smartsolar.microgrid.databinding.ActivityMainBinding

class MainActivity : AppCompatActivity() {
    private lateinit var binding: ActivityMainBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (SecureTokenStore(this).readToken() == null) {
            startActivity(Intent(this, LoginActivity::class.java))
            finish()
            return
        }
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)
    }
}

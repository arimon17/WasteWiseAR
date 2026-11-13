package com.wastewise.wastewise_flutter

import android.Manifest
import android.content.pm.PackageManager
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {
    private val channelName = "wastewise/storage"
    private val prefsName = "wastewise"
    private val cameraPermissionRequestCode = 4101
    private var cameraPermissionResult: MethodChannel.Result? = null

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)

        val prefs = getSharedPreferences(prefsName, MODE_PRIVATE)

        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, channelName)
            .setMethodCallHandler { call, result ->
                when (call.method) {
                    "getInt" -> {
                        val key = call.argument<String>("key")
                        if (key == null) {
                            result.error("invalid_key", "Key is required", null)
                        } else {
                            result.success(prefs.getInt(key, 0))
                        }
                    }

                    "setInt" -> {
                        val key = call.argument<String>("key")
                        val value = call.argument<Int>("value")
                        if (key == null || value == null) {
                            result.error("invalid_args", "Key and value are required", null)
                        } else {
                            prefs.edit().putInt(key, value).apply()
                            result.success(null)
                        }
                    }

                    "ensureCameraPermission" -> ensureCameraPermission(result)

                    else -> result.notImplemented()
                }
            }
    }

    private fun ensureCameraPermission(result: MethodChannel.Result) {
        if (checkSelfPermission(Manifest.permission.CAMERA) ==
            PackageManager.PERMISSION_GRANTED
        ) {
            result.success(true)
            return
        }

        if (cameraPermissionResult != null) {
            result.error("pending", "Camera permission request already in progress", null)
            return
        }

        cameraPermissionResult = result
        requestPermissions(arrayOf(Manifest.permission.CAMERA), cameraPermissionRequestCode)
    }

    override fun onRequestPermissionsResult(
        requestCode: Int,
        permissions: Array<out String>,
        grantResults: IntArray,
    ) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)

        if (requestCode != cameraPermissionRequestCode) {
            return
        }

        val granted =
            grantResults.isNotEmpty() && grantResults[0] == PackageManager.PERMISSION_GRANTED
        cameraPermissionResult?.success(granted)
        cameraPermissionResult = null
    }
}

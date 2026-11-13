[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$appRoot = Split-Path -Parent $PSScriptRoot
$androidRoot = Join-Path $appRoot "android"
$gradlew = Join-Path $androidRoot "gradlew.bat"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

if (-not $env:JAVA_HOME) {
    $jbr = "C:\Program Files\Android\Android Studio\jbr"
    if (Test-Path $jbr) {
        $env:JAVA_HOME = $jbr
    }
}

Write-Step "1. Hentikan proses Gradle wrapper yang masih menempel"
$stuckWrappers = Get-CimInstance Win32_Process -Filter "Name='java.exe'" -ErrorAction SilentlyContinue |
    Where-Object {
        $_.CommandLine -and
        $_.CommandLine -like "*$appRoot*" -and
        $_.CommandLine -like "*GradleWrapperMain*" -and
        $_.CommandLine -like "*assembleDebug*"
    }

foreach ($process in $stuckWrappers) {
    Write-Host "Menghentikan Gradle wrapper PID $($process.ProcessId)"
    Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
}

Write-Step "2. Hentikan Gradle daemon"
if (Test-Path $gradlew) {
    Push-Location $androidRoot
    try {
        & $gradlew --stop
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Warning "gradlew.bat tidak ditemukan di $androidRoot"
}

Write-Step "3. Gradle clean (hapus output build Gradle)"
if (Test-Path $gradlew) {
    Push-Location $androidRoot
    try {
        & $gradlew clean --console=plain
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "gradlew clean selesai dengan exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }

    Push-Location $androidRoot
    try {
        & $gradlew --stop
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Warning "gradlew.bat tidak ditemukan di $androidRoot"
}

Write-Step "4. Restart ADB server"
$adbCommand = Get-Command adb -ErrorAction SilentlyContinue
if ($adbCommand) {
    $adbPath = $adbCommand.Source
}
else {
    $adbPath = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
}

if (Test-Path $adbPath) {
    & $adbPath kill-server
    Start-Sleep -Seconds 1
    & $adbPath start-server
    Start-Sleep -Seconds 1
    Write-Host "Perangkat yang terdeteksi:"
    & $adbPath devices
}
else {
    Write-Warning "adb tidak ditemukan. Pastikan Android SDK platform-tools terpasang."
}

Write-Step "5. flutter clean"
Push-Location $appRoot
try {
    & flutter clean
    if ($LASTEXITCODE -ne 0) {
        throw "flutter clean gagal dengan exit code $LASTEXITCODE"
    }

    $leftovers = @(
        "build",
        ".dart_tool",
        "android\.gradle",
        "android\build",
        "android\app\build",
        "android\unityLibrary\unityLibrary\build",
        "android\unityLibrary\unityLibrary\.cxx",
        "android\unityLibrary\unityLibrary\src\main\cpp\.cxx"
    )

    foreach ($relativePath in $leftovers) {
        $target = Join-Path $appRoot $relativePath
        if (Test-Path $target) {
            try {
                Remove-Item -LiteralPath $target -Recurse -Force
                Write-Host "Menghapus sisa cache: $relativePath"
            }
            catch {
                Write-Warning "Gagal menghapus $relativePath : $($_.Exception.Message)"
            }
        }
    }

    $gradleBuildCache = Join-Path $env:USERPROFILE ".gradle\caches\build-cache-1"
    if (Test-Path $gradleBuildCache) {
        try {
            Remove-Item -LiteralPath $gradleBuildCache -Recurse -Force
            Write-Host "Menghapus cache Gradle: $gradleBuildCache"
        }
        catch {
            Write-Warning "Gagal menghapus cache Gradle: $($_.Exception.Message)"
        }
    }

    Write-Step "6. flutter pub get"
    & flutter pub get
    if ($LASTEXITCODE -ne 0) {
        throw "flutter pub get gagal dengan exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

$unityManifest = Join-Path $androidRoot "unityLibrary\unityLibrary\src\main\AndroidManifest.xml"
if (Test-Path $unityManifest) {
    Write-Host "android/unityLibrary tetap utuh." -ForegroundColor Green
}
else {
    Write-Warning "android/unityLibrary tidak ditemukan. Export ulang Unity sebelum build."
}

Write-Step "Cleanup selesai"
Write-Host "Langkah berikutnya:"
Write-Host "  1. Sambungkan Tecno Pova 7 dan aktifkan USB debugging."
Write-Host "  2. Pastikan 'adb devices' menampilkan status 'device'."
Write-Host "  3. Jalankan: flutter run --release -v"
Write-Host ""
Write-Host "Jika masih macet, tutup Unity Editor terlebih dahulu agar RAM dan file lock berkurang."

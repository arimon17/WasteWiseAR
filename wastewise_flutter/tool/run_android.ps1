[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$FlutterArgs
)

$ErrorActionPreference = "Stop"

$appRoot = Split-Path -Parent $PSScriptRoot
$androidRoot = Join-Path $appRoot "android"
$gradlew = Join-Path $androidRoot "gradlew.bat"

if (-not $env:JAVA_HOME) {
    $jbr = "C:\Program Files\Android\Android Studio\jbr"
    if (Test-Path -LiteralPath $jbr) {
        $env:JAVA_HOME = $jbr
    }
}

Write-Host "==> Membersihkan Gradle wrapper yang macet" -ForegroundColor Cyan
$stuckWrappers = Get-CimInstance Win32_Process -Filter "Name='java.exe'" -ErrorAction SilentlyContinue |
    Where-Object {
        $_.CommandLine -and
        $_.CommandLine -like "*$appRoot*" -and
        $_.CommandLine -like "*GradleWrapperMain*"
    }

foreach ($process in $stuckWrappers) {
    Write-Host "Menghentikan Gradle wrapper PID $($process.ProcessId)"
    Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
}

Write-Host "==> Menghentikan Gradle daemon" -ForegroundColor Cyan
if (Test-Path -LiteralPath $gradlew) {
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

foreach ($abi in @("arm64-v8a")) {
    $il2cpp = Join-Path $androidRoot "unityLibrary\unityLibrary\src\main\jniLibs\$abi\libil2cpp.so"
    if (-not (Test-Path -LiteralPath $il2cpp)) {
        Write-Warning "libil2cpp.so $abi tidak ditemukan; build akan memicu kompilasi IL2CPP berat."
    }
}

Write-Host "==> flutter run --verbose" -ForegroundColor Cyan
Push-Location $appRoot
try {
    & flutter run --verbose @FlutterArgs
}
finally {
    Pop-Location
}

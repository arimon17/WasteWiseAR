Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = "Stop"

$outputDir = Join-Path $PSScriptRoot "..\assets\icons"
$pngPath = Join-Path $outputDir "app_icon.png"
$svgPath = Join-Path $outputDir "app_icon.svg"
$size = 512
$background = "#0F172A"
$accent = "#10B981"

$pathData = "M132 354C127 357 126 364 129 368L154 410L176 447C184 460 204 460 212 447L244 394L218 350L202 324C199 320 192 318 187 321L132 354ZM463 235 435 283C432 288 425 290 420 287L365 255C360 252 358 245 361 240L403 171H426C439 171 451 176 460 186C466 194 469 204 469 213C469 221 467 228 463 235ZM341 64H373C390 64 404 73 412 88L442 149H341V166C341 176 330 180 323 174L264 114C259 110 259 103 264 99L323 39C330 33 341 38 341 47V64ZM203 149H122L104 119C97 109 97 95 102 84C108 72 121 64 135 64H203C209 64 213 69 213 75V139C213 145 209 149 203 149ZM131 206 145 197C154 192 164 199 161 209L141 291C140 296 134 300 128 298L46 278C37 275 35 263 43 258L57 250L49 235C41 223 41 207 47 194L82 125L131 206ZM363 402 335 449C327 461 314 469 299 469H223L290 358L275 350C267 345 269 332 278 330L360 310C366 308 371 312 373 317L393 399C396 408 386 416 378 411L363 402Z"

$svg = @"
<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 512 512">
  <rect width="512" height="512" fill="$background"/>
  <circle cx="256" cy="256" r="216" fill="$accent" opacity="0.12"/>
  <g transform="translate(0 512) scale(1 -1)">
    <path d="$pathData" fill="$accent"/>
  </g>
</svg>
"@

Set-Content -LiteralPath $svgPath -Value $svg -Encoding UTF8

$bitmap = New-Object System.Drawing.Bitmap($size, $size)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.ColorTranslator]::FromHtml($background))

$glowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(31, 16, 185, 129))
$graphics.FillEllipse($glowBrush, 40, 40, 432, 432)

$iconPath = New-Object System.Drawing.Drawing2D.GraphicsPath
$iconPath.FillMode = [System.Drawing.Drawing2D.FillMode]::Winding

$tokens = [regex]::Matches($pathData, "[MLCZHVmlczhv]|[-+]?(?:\d*\.\d+|\d+\.?)(?:[eE][-+]?\d+)?")
$index = 0
$command = $null
$currentX = 0.0
$currentY = 0.0
$startX = 0.0
$startY = 0.0

function Convert-Point {
    param([double]$X, [double]$Y)
    return [System.Drawing.PointF]::new([float]$X, [float]($size - $Y))
}

while ($index -lt $tokens.Count) {
    $token = $tokens[$index].Value
    if ($token -match "^[MLCZHVmlczhv]$") {
        $command = $token
        $index++
        continue
    }

    switch ($command) {
        "M" {
            $x = [double]$tokens[$index].Value
            $y = [double]$tokens[$index + 1].Value
            $index += 2
            $currentX = $x
            $currentY = $y
            $startX = $x
            $startY = $y
            $iconPath.StartFigure()
            $command = "L"
        }
        "L" {
            $x = [double]$tokens[$index].Value
            $y = [double]$tokens[$index + 1].Value
            $index += 2
            $iconPath.AddLine((Convert-Point $currentX $currentY), (Convert-Point $x $y))
            $currentX = $x
            $currentY = $y
        }
        "H" {
            $x = [double]$tokens[$index].Value
            $index++
            $iconPath.AddLine((Convert-Point $currentX $currentY), (Convert-Point $x $currentY))
            $currentX = $x
        }
        "V" {
            $y = [double]$tokens[$index].Value
            $index++
            $iconPath.AddLine((Convert-Point $currentX $currentY), (Convert-Point $currentX $y))
            $currentY = $y
        }
        "C" {
            $c1x = [double]$tokens[$index].Value
            $c1y = [double]$tokens[$index + 1].Value
            $c2x = [double]$tokens[$index + 2].Value
            $c2y = [double]$tokens[$index + 3].Value
            $x = [double]$tokens[$index + 4].Value
            $y = [double]$tokens[$index + 5].Value
            $index += 6
            $iconPath.AddBezier(
                (Convert-Point $currentX $currentY),
                (Convert-Point $c1x $c1y),
                (Convert-Point $c2x $c2y),
                (Convert-Point $x $y)
            )
            $currentX = $x
            $currentY = $y
        }
        "Z" {
            $iconPath.CloseFigure()
            $currentX = $startX
            $currentY = $startY
        }
        default {
            throw "Perintah path tidak dikenal: $command"
        }
    }
}

$iconBrush = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml($accent))
$graphics.FillPath($iconBrush, $iconPath)
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

$iconBrush.Dispose()
$iconPath.Dispose()
$glowBrush.Dispose()
$graphics.Dispose()
$bitmap.Dispose()

Write-Output "Generated $svgPath"
Write-Output "Generated $pngPath"

param(
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\Assets\MeixiEcho\Art\Logos\meixi_app_icon.png')
)

Add-Type -AssemblyName System.Drawing

$size = 1024
$bitmap = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$graphics.Clear([System.Drawing.Color]::FromArgb(255, 12, 29, 26))

try {
    $streamPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $streamPath.StartFigure()
    $streamPath.AddBezier(168, 615, 255, 495, 307, 452, 374, 480)
    $streamPath.AddBezier(374, 480, 447, 510, 449, 674, 535, 674)
    $streamPath.AddBezier(535, 674, 620, 674, 621, 505, 704, 470)
    $streamPath.AddBezier(704, 470, 762, 446, 812, 500, 850, 585)

    $streamShadow = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 18, 73, 67), 106)
    $streamShadow.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $streamShadow.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $streamShadow.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $graphics.DrawPath($streamShadow, $streamPath)

    $stream = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 58, 178, 163), 72)
    $stream.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $stream.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $stream.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $graphics.DrawPath($stream, $streamPath)

    $streamHighlight = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 142, 222, 207), 13)
    $streamHighlight.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $streamHighlight.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $streamHighlight.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $graphics.DrawPath($streamHighlight, $streamPath)

    $echo = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 219, 187, 111), 28)
    $echo.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $echo.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $graphics.DrawArc($echo, 642, 250, 170, 155, 218, 104)
    $graphics.DrawArc($echo, 693, 201, 236, 224, 218, 104)

    $outputDirectory = Split-Path -Parent $OutputPath
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    $resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
    $bitmap.Save($resolvedOutput, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output $resolvedOutput
}
finally {
    if ($echo) { $echo.Dispose() }
    if ($streamHighlight) { $streamHighlight.Dispose() }
    if ($stream) { $stream.Dispose() }
    if ($streamShadow) { $streamShadow.Dispose() }
    if ($streamPath) { $streamPath.Dispose() }
    $graphics.Dispose()
    $bitmap.Dispose()
}

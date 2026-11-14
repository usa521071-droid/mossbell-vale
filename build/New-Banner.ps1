param(
    [Parameter(Mandatory)][string] $Banner,
    [Parameter(Mandatory)][string] $Scratch,
    [string] $Icon,
    [string] $Title
)

$bannerPath = Join-Path $Scratch 'banner.png'
if ($Title) {
    Add-Type -AssemblyName System.Drawing
    $bitmap = [Drawing.Bitmap]::new(256, 128)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $logo = [Drawing.Image]::FromFile($Icon)
    $border = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#74a889'), 2)
    $paper = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#fff0ba'))
    $format = [Drawing.StringFormat]::new()
    $font = $null
    try {
        $graphics.Clear([Drawing.ColorTranslator]::FromHtml('#214539'))
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $graphics.TextRenderingHint = [Drawing.Text.TextRenderingHint]::SingleBitPerPixelGridFit
        $graphics.DrawRectangle($border, 8, 8, 239, 111)
        $graphics.DrawLine($border, 74, 23, 74, 105)
        $graphics.DrawImage($logo, [Drawing.Rectangle]::new(17, 42, 48, 48))
        $format.Alignment = [Drawing.StringAlignment]::Center
        $format.LineAlignment = [Drawing.StringAlignment]::Center
        $bounds = [Drawing.RectangleF]::new(82, 18, 151, 92)
        $size = 21
        do {
            if ($font) { $font.Dispose() }
            $font = [Drawing.Font]::new('Consolas', $size, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
            $measured = $graphics.MeasureString($Title, $font, [Drawing.SizeF]::new($bounds.Width, 1000), $format)
            $size--
        } while ($measured.Height -gt 76 -and $size -gt 9)
        $graphics.DrawString($Title, $font, $paper, $bounds, $format)
        $bitmap.Save($bannerPath, [Drawing.Imaging.ImageFormat]::Png)
    } finally {
        if ($font) { $font.Dispose() }
        $format.Dispose()
        $paper.Dispose()
        $border.Dispose()
        $logo.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
} else {
    Copy-Item -LiteralPath $Banner -Destination $bannerPath
}

$sampleRate = 22050
$sampleCount = 11025
$writer = [IO.BinaryWriter]::new([IO.File]::Create((Join-Path $Scratch 'banner.wav')))
try {
    $writer.Write([Text.Encoding]::ASCII.GetBytes('RIFF'))
    $writer.Write([int] (36 + 2 * $sampleCount))
    $writer.Write([Text.Encoding]::ASCII.GetBytes('WAVEfmt '))
    $writer.Write([int] 16)
    $writer.Write([int16] 1)
    $writer.Write([int16] 1)
    $writer.Write([int] $sampleRate)
    $writer.Write([int] (2 * $sampleRate))
    $writer.Write([int16] 2)
    $writer.Write([int16] 16)
    $writer.Write([Text.Encoding]::ASCII.GetBytes('data'))
    $writer.Write([int] (2 * $sampleCount))
    for ($i = 0; $i -lt $sampleCount; $i++) {
        $t = $i / $sampleRate
        $volume = [Math]::Min(1, $t * 200) * [Math]::Pow(1 - 2 * $t, 3)
        $chime = [Math]::Sin(2 * [Math]::PI * 523.25 * $t) + 0.35 * [Math]::Sin(2 * [Math]::PI * 783.99 * $t)
        $writer.Write([int16] (5500 * $volume * $chime))
    }
} finally {
    $writer.Dispose()
}

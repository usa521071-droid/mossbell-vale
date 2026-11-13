param(
    [Parameter(Mandatory)][string] $Banner,
    [Parameter(Mandatory)][string] $Scratch
)

Copy-Item -LiteralPath $Banner -Destination (Join-Path $Scratch 'banner.png')

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

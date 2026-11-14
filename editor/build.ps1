[CmdletBinding()]
param([string] $OutputDirectory)

$ErrorActionPreference = 'Stop'
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $PSScriptRoot '../dist'
}
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$compiler = Join-Path ([Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()) 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The .NET Framework 4.x C# compiler was not found.'
}

$output = Join-Path (New-Item -ItemType Directory -Force -Path $OutputDirectory).FullName 'MossbellValeEditor.exe'
$arguments = @(
    '/nologo'
    '/target:winexe'
    '/platform:anycpu'
    '/optimize+'
    '/warnaserror+'
    '/codepage:65001'
    "/out:$output"
    '/reference:System.Windows.Forms.dll'
    '/reference:System.Drawing.dll'
    '/reference:System.IO.Compression.dll'
)
foreach ($asset in 'tiles.png', 'player.png', 'blob.png', 'npc.png', 'world.rpgw', 'dungeon.rpgw', 'mountain.rpgw', 'project.rpgp') {
    $assetPath = Join-Path $root "assets/$asset"
    if (-not (Test-Path -LiteralPath $assetPath -PathType Leaf)) {
        throw "Missing editor asset $assetPath"
    }
    $arguments += "/resource:$assetPath,MossbellVale.$asset"
}
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$template = Join-Path (Split-Path -Parent $output) ('publish-source-' + [Guid]::NewGuid().ToString('N') + '.zip')
try {
    $archive = [IO.Compression.ZipFile]::Open($template, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $paths = @(
            'build/Build-3DS.ps1', 'build/Install-3DSTools.ps1', 'build/MossbellVale.rsf', 'build/New-Banner.ps1',
            'assets/tiles.rgba', 'assets/player.rgba', 'assets/blob.rgba', 'assets/npc.rgba', 'assets/world.rpgw', 'assets/dungeon.rpgw',
            'assets/mountain.rpgw', 'assets/banner.png', 'assets/icon.png', 'assets/ca-bundle.pem'
        )
        foreach ($directory in 'game/source', 'game/include') {
            $paths += Get-ChildItem -LiteralPath (Join-Path $root $directory) -File |
                ForEach-Object { "$directory/$($_.Name)" }
        }
        foreach ($path in $paths) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive, (Join-Path $root $path), $path, [IO.Compression.CompressionLevel]::Optimal
            ) | Out-Null
        }
    } finally {
        $archive.Dispose()
    }
    $arguments += "/resource:$template,MossbellVale.PublishSource.zip"
    $arguments += (Get-ChildItem -Path $PSScriptRoot -Filter '*.cs').FullName
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Editor compilation failed with exit code $LASTEXITCODE."
    }
} finally {
    if ([IO.File]::Exists($template)) { [IO.File]::Delete($template) }
}
Write-Output $output

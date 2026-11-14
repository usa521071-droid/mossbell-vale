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
)
foreach ($asset in 'tiles.png', 'player.png', 'world.rpgw', 'dungeon.rpgw', 'mountain.rpgw') {
    $assetPath = Join-Path $root "assets/$asset"
    if (-not (Test-Path -LiteralPath $assetPath -PathType Leaf)) {
        throw "Missing editor asset $assetPath"
    }
    $arguments += "/resource:$assetPath,MossbellVale.$asset"
}
$arguments += (Get-ChildItem -Path $PSScriptRoot -Filter '*.cs').FullName
& $compiler @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Editor compilation failed with exit code $LASTEXITCODE."
}
Write-Output $output

[CmdletBinding()]
param(
    [string] $Version = '1.2.0',
    [string] $ToolRoot,
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $ToolRoot) {
    $ToolRoot = if ($env:MOSSBELL_3DS_TOOLS) {
        $env:MOSSBELL_3DS_TOOLS
    } else {
        Join-Path $env:LOCALAPPDATA 'MossbellValeTools'
    }
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repo 'dist'
}
$ToolRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ToolRoot)
$OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)

if ($Version -notmatch '^(0|[1-9][0-9]?)\.(0|[1-9][0-9]?)\.(0|[1-9][0-9]?)$') {
    throw 'Use a version in major.minor.patch form.'
}
$major, $minor, $patch = $Version.Split('.') | ForEach-Object { [int] $_ }
if ($major -gt 63 -or $minor -gt 63 -or $patch -gt 15) {
    throw 'The CIA supports version components up to 63.63.15.'
}
$ciaVersion = $major * 1024 + $minor * 16 + $patch
$buildNumber = $major * 10000 + $minor * 100 + $patch

$sdk = Join-Path $ToolRoot 'sdk'
$dkp = Join-Path $sdk 'opt/devkitpro'
$gccBin = Join-Path $dkp 'devkitARM/bin'
$ctr = Join-Path $dkp 'libctru'
$portlibs = Join-Path $dkp 'portlibs/3ds'
$gcc = Join-Path $gccBin 'arm-none-eabi-gcc.exe'
$tools = Join-Path $dkp 'tools/bin'
$makerom = Get-ChildItem (Join-Path $sdk 'makerom-0.18.4') -Filter makerom.exe -File -Recurse -ErrorAction SilentlyContinue |
    Select-Object -First 1 -ExpandProperty FullName
$bannertool = Get-ChildItem (Join-Path $sdk 'bannertool') -Filter bannertool.exe -File -Recurse -ErrorAction SilentlyContinue |
    Select-Object -First 1 -ExpandProperty FullName
if (-not (Test-Path -LiteralPath $gcc) -or -not $makerom -or -not $bannertool) {
    throw "Run build/Install-3DSTools.ps1 -ToolRoot `"$ToolRoot`" first."
}

function Invoke-Tool([string] $Command, [string[]] $Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$(Split-Path -Leaf $Command) exited with code $LASTEXITCODE."
    }
}

$scratch = Join-Path $ToolRoot ('work-' + [Guid]::NewGuid().ToString('N'))
$originalPath = $env:PATH
$env:PATH = "$gccBin;$tools;$env:PATH"
$originalTmp = $env:TMP
$originalTemp = $env:TEMP
New-Item -Path $scratch, $OutputDirectory -ItemType Directory -Force | Out-Null
$env:TMP = $scratch
$env:TEMP = $scratch
try {
    @"
#ifndef MOSSBELL_BUILD_INFO_H
#define MOSSBELL_BUILD_INFO_H
#define MOSSBELL_BUILD $buildNumber
#define MOSSBELL_VERSION "$Version"
#endif
"@ | Set-Content (Join-Path $scratch 'version.h') -Encoding Ascii

    $assetPaths = [ordered]@{
        tiles = 'assets/tiles.rgba'
        player = 'assets/player.rgba'
        world = 'assets/world.rpgw'
        dungeon = 'assets/dungeon.rpgw'
        mountain = 'assets/mountain.rpgw'
        ca_bundle = 'assets/ca-bundle.pem'
    }
    $tiles = Get-Item -LiteralPath (Join-Path $repo $assetPaths.tiles)
    if ($tiles.Length -ne 128 * 256 * 4) {
        throw 'assets/tiles.rgba must contain a 128 x 256 atlas in 32-bit RGBA format.'
    }
    $assembly = foreach ($entry in $assetPaths.GetEnumerator()) {
        $source = Join-Path $repo $entry.Value
        Copy-Item -LiteralPath $source -Destination (Join-Path $scratch $entry.Key)
        $symbol = 'asset_' + $entry.Key
        ".section .rodata.$symbol,`"a`",%progbits"
        '.balign 4'
        ".global $symbol"
        ".type $symbol, %object"
        "${symbol}:"
        ".incbin `"$($entry.Key)`""
        ".size $symbol, . - $symbol"
        '.balign 4'
        ".global ${symbol}_size"
        ".type ${symbol}_size, %object"
        "${symbol}_size:"
        ".word $((Get-Item -LiteralPath $source).Length)"
        ".size ${symbol}_size, 4"
    }
    $assembly | Set-Content (Join-Path $scratch 'embedded.s') -Encoding Ascii

    $arch = @('-march=armv6k', '-mtune=mpcore', '-mfloat-abi=hard', '-mtp=soft')
    $compileFlags = $arch + @(
        '-std=gnu11', '-g', '-O2', '-Wall', '-Wextra', '-ffunction-sections', '-fdata-sections',
        '-mword-relocations', '-D__3DS__', '-DCURL_STATICLIB',
        '-include', (Join-Path $scratch 'version.h'),
        '-I', (Join-Path $repo 'game/include'), '-I', (Join-Path $ctr 'include'),
        '-I', (Join-Path $portlibs 'include')
    )
    $sourceFiles = Get-ChildItem (Join-Path $repo 'game/source') -Filter '*.c' -File | Sort-Object Name
    if (-not $sourceFiles) { throw 'Expected C source files under game/source.' }

    Push-Location $scratch
    try {
        $objects = foreach ($source in $sourceFiles) {
            $object = 'game-' + $source.BaseName + '.o'
            Invoke-Tool $gcc ($compileFlags + @('-c', $source.FullName, '-o', $object))
            $object
        }
        Invoke-Tool $gcc ($arch + @('-c', 'embedded.s', '-o', 'embedded.o'))
        Invoke-Tool $gcc ($arch + @(
            '-specs=3dsx.specs', '-Wl,--gc-sections', '-o', 'MossbellVale.elf'
        ) + $objects + @(
            'embedded.o', '-L', (Join-Path $ctr 'lib'), '-L', (Join-Path $portlibs 'lib'),
            '-lcurl', '-lmbedtls', '-lmbedx509', '-lmbedcrypto', '-lz', '-lctru', '-lm'
        ))

        $icon = Join-Path $repo 'assets/icon.png'
        Invoke-Tool (Join-Path $tools 'smdhtool.exe') @(
            '--create', 'Mossbell Vale', 'Explore the Vale and restore its bell.', 'Mossbell Vale',
            $icon, 'MossbellVale.smdh'
        )
        Invoke-Tool (Join-Path $tools '3dsxtool.exe') @(
            'MossbellVale.elf', 'MossbellVale.3dsx', '--smdh=MossbellVale.smdh'
        )
        $banner = Join-Path $repo 'assets/banner.png'
        & (Join-Path $PSScriptRoot 'New-Banner.ps1') -Banner $banner -Scratch $scratch
        Invoke-Tool $bannertool @(
            'makebanner', '-i', 'banner.png', '-a', 'banner.wav', '-o', 'banner.bin'
        )
        Invoke-Tool $makerom @(
            '-f', 'cia', '-o', 'MossbellVale.cia', '-target', 't', '-exefslogo',
            '-elf', 'MossbellVale.elf', '-rsf', (Join-Path $PSScriptRoot 'MossbellVale.rsf'),
            '-ver', $ciaVersion.ToString(), '-icon', 'MossbellVale.smdh', '-banner', 'banner.bin'
        )
        Copy-Item -LiteralPath 'MossbellVale.cia', 'MossbellVale.3dsx' -Destination $OutputDirectory -Force
    } finally {
        Pop-Location
    }
} finally {
    $env:TMP = $originalTmp
    $env:TEMP = $originalTemp
    $env:PATH = $originalPath
    if (Test-Path -LiteralPath $scratch) {
        $resolvedScratch = (Resolve-Path -LiteralPath $scratch).Path
        $resolvedRoot = (Resolve-Path -LiteralPath $ToolRoot).Path
        if ((Split-Path -Parent $resolvedScratch) -eq $resolvedRoot) {
            Remove-Item -LiteralPath $resolvedScratch -Recurse -Force
        }
    }
}

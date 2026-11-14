[CmdletBinding()]
param(
    [string] $Version = '1.3.1',
    [string] $ToolRoot,
    [string] $OutputDirectory,
    [string] $Title = 'Mossbell Vale',
    [string] $UniqueId = 'F14B1',
    [string] $ProjectPack,
    [string] $ProjectArt,
    [string] $GitHubRepository,
    [switch] $CiaOnly
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

if ($Title -notmatch '^[A-Za-z0-9][A-Za-z0-9 ._-]{0,39}$') {
    throw 'Use a title of 1 to 40 letters, digits, spaces, periods, hyphens or underscores.'
}
if ($UniqueId -notmatch '^F[0-9A-F]{4}$') {
    throw 'Use a five-digit hexadecimal title ID beginning with F.'
}
$UniqueId = $UniqueId.ToUpperInvariant()
if ($ProjectPack -and $UniqueId -eq 'F14B1') {
    throw 'Editor projects need their own title ID. F14B1 is reserved for Mossbell Vale.'
}
if ($ProjectPack) {
    $ProjectPack = (Resolve-Path -LiteralPath $ProjectPack).Path
}
if ($ProjectArt) {
    if (-not $ProjectPack) { throw 'A project art bundle requires a project pack.' }
    $ProjectArt = (Resolve-Path -LiteralPath $ProjectArt).Path
}
if ($GitHubRepository -and $GitHubRepository -notmatch '^[A-Za-z0-9][A-Za-z0-9-]{0,38}/[A-Za-z0-9_.-]{1,100}$') {
    throw 'Use a GitHub repository in Owner/Repository form.'
}

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
    $dataFolder = 'MossbellVale'
    if ($UniqueId -ne 'F14B1') {
        $dataFolder = 'Mossbell-' + $UniqueId
    }
    $projectBuild = [int][bool]$ProjectPack
    $githubDefine = if ($GitHubRepository) { "#define MOSSBELL_GITHUB `"$GitHubRepository`"" }
    @"
#ifndef MOSSBELL_BUILD_INFO_H
#define MOSSBELL_BUILD_INFO_H
#define MOSSBELL_BUILD $buildNumber
#define MOSSBELL_VERSION "$Version"
#define MOSSBELL_TITLE "$Title"
#define MOSSBELL_SD_DIR "sdmc:/3ds/$dataFolder"
#define MOSSBELL_PUBLISHED $projectBuild
$githubDefine
#endif
"@ | Set-Content (Join-Path $scratch 'version.h') -Encoding Ascii

    $assetPaths = [ordered]@{
        tiles = 'assets/tiles.rgba'
        player = 'assets/player.rgba'
        world = 'assets/world.rpgw'
        dungeon = 'assets/dungeon.rpgw'
        mountain = 'assets/mountain.rpgw'
        ca_bundle = 'assets/ca-bundle.pem'
        project = 'assets/project.rpgp'
        project_art = 'assets/project-art.rpga'
    }
    $tiles = Get-Item -LiteralPath (Join-Path $repo $assetPaths.tiles)
    if ($tiles.Length -ne 128 * 256 * 4) {
        throw 'assets/tiles.rgba must contain a 128 x 256 atlas in 32-bit RGBA format.'
    }
    $assembly = foreach ($entry in $assetPaths.GetEnumerator()) {
        $source = Join-Path $repo $entry.Value
        if ($entry.Key -eq 'project' -and $ProjectPack) { $source = $ProjectPack }
        if ($entry.Key -eq 'project_art' -and $ProjectArt) { $source = $ProjectArt }
        $length = 0
        if (Test-Path -LiteralPath $source -PathType Leaf) {
            Copy-Item -LiteralPath $source -Destination (Join-Path $scratch $entry.Key)
            $length = (Get-Item -LiteralPath $source).Length
        } elseif ($entry.Key -notin 'project', 'project_art') {
            throw "Missing build asset $source"
        }
        $symbol = 'asset_' + $entry.Key
        ".section .rodata.$symbol,`"a`",%progbits"
        '.balign 4'
        ".global $symbol"
        ".type $symbol, %object"
        "${symbol}:"
        if ($length -gt 0) { ".incbin `"$($entry.Key)`"" }
        ".size $symbol, . - $symbol"
        '.balign 4'
        ".global ${symbol}_size"
        ".type ${symbol}_size, %object"
        "${symbol}_size:"
        ".word $length"
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
            '--create', $Title, "A tile RPG made with Mossbell Vale Editor.", 'Mossbell Vale',
            $icon, 'MossbellVale.smdh'
        )
        if (-not $CiaOnly) {
            Invoke-Tool (Join-Path $tools '3dsxtool.exe') @(
                'MossbellVale.elf', 'MossbellVale.3dsx', '--smdh=MossbellVale.smdh'
            )
        }
        $banner = Join-Path $repo 'assets/banner.png'
        $bannerOptions = @{ Banner = $banner; Scratch = $scratch; Icon = $icon }
        if ($ProjectPack -or $Title -ne 'Mossbell Vale') { $bannerOptions.Title = $Title }
        & (Join-Path $PSScriptRoot 'New-Banner.ps1') @bannerOptions
        Invoke-Tool $bannertool @(
            'makebanner', '-i', 'banner.png', '-a', 'banner.wav', '-o', 'banner.bin'
        )
        Invoke-Tool $makerom @(
            '-f', 'cia', '-o', 'MossbellVale.cia', '-target', 't', '-exefslogo',
            '-elf', 'MossbellVale.elf', '-rsf', (Join-Path $PSScriptRoot 'MossbellVale.rsf'),
            "-DAPP_UNIQUE_ID=0x$UniqueId", "-DAPP_TITLE=$Title",
            '-ver', $ciaVersion.ToString(), '-icon', 'MossbellVale.smdh', '-banner', 'banner.bin'
        )
        Copy-Item -LiteralPath 'MossbellVale.cia' -Destination $OutputDirectory -Force
        if (-not $CiaOnly) {
            Copy-Item -LiteralPath 'MossbellVale.3dsx' -Destination $OutputDirectory -Force
        }
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

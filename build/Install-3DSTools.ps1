[CmdletBinding()]
param(
    [string] $ToolRoot = $(if ($env:MOSSBELL_3DS_TOOLS) {
        $env:MOSSBELL_3DS_TOOLS
    } else {
        Join-Path $env:LOCALAPPDATA 'MossbellValeTools'
    })
)

$ErrorActionPreference = 'Stop'
$ToolRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ToolRoot)
$archives = Join-Path $ToolRoot 'packages'
$sdk = Join-Path $ToolRoot 'sdk'
New-Item -Path $archives, $sdk -ItemType Directory -Force | Out-Null

function Save-VerifiedFile([string] $Url, [string] $Sha256) {
    $filename = $Url.Substring($Url.LastIndexOf('/') + 1)
    $path = Join-Path $archives $filename
    if ((Test-Path -LiteralPath $path) -and (Get-FileHash -LiteralPath $path).Hash -eq $Sha256) {
        return $path
    }
    $partial = "$path.download"
    Write-Host "Downloading $filename"
    & curl.exe --fail --location --silent --show-error --retry 2 --output $partial $Url
    if ($LASTEXITCODE -ne 0) { throw "Download failed for $filename." }
    if ((Get-FileHash -LiteralPath $partial).Hash -ne $Sha256) {
        throw "Checksum mismatch for $filename."
    }
    Move-Item -LiteralPath $partial -Destination $path -Force
    return $path
}

$zipPackages = @(
    @{
        Name = 'zstd'
        Url = 'https://github.com/meta/zstd/releases/download/v1.5.7/zstd-v1.5.7-win64.zip'
        Hash = 'acb4e8111511749dc7a3ebedca9b04190e37a17afeb73f55d4425dbf0b90fad9'
    },
    @{
        Name = 'makerom'
        Url = 'https://github.com/3DSGuy/Project_CTR/releases/download/makerom-v0.19.0/makerom-v0.19.0-win_x86_64.zip'
        Hash = '88df4455e60556374e202d507f54ee03fac7493ec9554ab853157524b6d69db0'
    },
    @{
        Name = 'bannertool'
        Url = 'https://github.com/carstene1ns/3ds-bannertool/releases/download/1.2.3/bannertool-1.2.3-windows.zip'
        Hash = 'd0f277bd875e2ad411844378155d27afe124780fcccbd81d9a594e886e2dd3c4'
    },
    @{
        Name = 'ctrtool'
        Url = 'https://github.com/3DSGuy/Project_CTR/releases/download/ctrtool-v1.3.0/ctrtool-v1.3.0-win_x64.zip'
        Hash = '8031dff3be72d0adb250fae1f969f27627e12a89ebc6dd074a15a75f87ddc949'
    }
)
foreach ($package in $zipPackages) {
    $archive = Save-VerifiedFile $package.Url $package.Hash
    Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $sdk $package.Name) -Force
}

$packageServer = 'https://pkg.devkitpro.org/packages'
$sdkPackages = [ordered]@{
    'windows/x86_64/devkitarm-binutils-2.46.0-1-x86_64.pkg.tar.zst' = 'd19277f5ae8d7967fdef316faa6129fde44053a3fdf2057890148398d167e966'
    'windows/x86_64/devkitarm-gcc-16.1.0-1-x86_64.pkg.tar.zst' = 'e88fd817412cd026fa50bf92ad40db04e03aed0253bd9f3bca77e28a86db0f6b'
    'windows/x86_64/3dstools-1.3.1-3-x86_64.pkg.tar.zst' = 'cdbfcaab36889442483a864b64a2d08be493a3e3e7432e158c0580b3e817f7d8'
    'devkitarm-crtls-1.2.6-1-any.pkg.tar.zst' = '65faf10f62c219a1a8a3a182b0c4c56da7992599035ea32b3d79e39e79f964bb'
    'devkitarm-newlib-4.6.0.20260123-5-any.pkg.tar.zst' = 'd314c644c143bd5b104bd82cdc27602f6449cf907cb9c37ccfb8c0707182f89d'
    'libctru-2.7.1-1-any.pkg.tar.zst' = 'cb47403414c9b1c4f6655d142d424f281b01694b10eb1265a435ef5ae51f9fb3'
    '3ds-curl-8.4.0-1-any.pkg.tar.zst' = '5e8a639bd56e871c05c70363191d5d7a91e85ebf8e6dfe5f90c506c0ddeb8f2c'
    '3ds-mbedtls-2.28.8-1-any.pkg.tar.zst' = '3c2e96eb4a92ce358abe3a1658700c68b6032bce753e37356cad54f236cea3dd'
    '3ds-zlib-1.3.1-1-any.pkg.tar.zst' = 'c3be375285e6656996b939d7d9c31d34641642c0ce7d50b041568e5ac022c644'
}
$originalPath = $env:PATH
$zstdBin = Get-ChildItem (Join-Path $sdk 'zstd') -Filter zstd.exe -File -Recurse |
    Select-Object -First 1 -ExpandProperty DirectoryName
$env:PATH = "$zstdBin;$env:PATH"
try {
    foreach ($package in $sdkPackages.GetEnumerator()) {
        $archive = Save-VerifiedFile "$packageServer/$($package.Key)" $package.Value
        Write-Host "Extracting $(Split-Path -Leaf $archive)"
        & tar.exe -xf $archive -C $sdk opt
        if ($LASTEXITCODE -ne 0) { throw "Could not extract $archive." }
    }
} finally {
    $env:PATH = $originalPath
}
Write-Host "3DS tools are ready at $ToolRoot."

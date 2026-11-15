param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'src\NexusRoles.Plugin\NexusRoles.Plugin.csproj'
$manifestPath = Join-Path $repoRoot 'manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$outputDll = Join-Path $repoRoot 'src\NexusRoles.Plugin\bin\Release\netstandard2.1\NexusRoles.Plugin.dll'
$bundle = Join-Path $repoRoot 'assets\bundles\NexusRoles_assets'
$icon = Join-Path $repoRoot 'assets\icon.png'
$fontNotice = Join-Path $repoRoot 'assets\unity\NexusRolesAssetBundle\Assets\NexusRoles\SourceArt\Gui\NotoSans-OFL.txt'
$releaseDir = Join-Path $repoRoot 'release'
$zipPath = Join-Path $releaseDir ("{0}-{1}.zip" -f $manifest.name, $manifest.version_number)
$stage = Join-Path ([IO.Path]::GetTempPath()) ('NexusRoles-' + [guid]::NewGuid().ToString('N'))

if (-not $SkipBuild) {
    dotnet restore $project --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
    dotnet build $project --configuration Release --no-restore -p:CopyFilesToPluginOutputDirectoryOnBuild=false
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
}

$required = @($outputDll, $bundle, $icon, $fontNotice)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required release file is missing: $path"
    }
}
if ($manifest.name -ne 'NexusRoles') { throw 'Unexpected Thunderstore package name.' }

New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
New-Item -ItemType Directory -Path $stage | Out-Null
try {
    foreach ($file in @('manifest.json', 'README.md', 'CHANGELOG.md', 'LICENSE', 'LICENSE-MIT.txt', 'THIRD_PARTY_NOTICES.md')) {
        Copy-Item -LiteralPath (Join-Path $repoRoot $file) -Destination $stage
    }
    Copy-Item -LiteralPath $icon -Destination (Join-Path $stage 'icon.png')
    Copy-Item -LiteralPath $fontNotice -Destination (Join-Path $stage 'NotoSans-OFL.txt')
    $pluginsDir = Join-Path $stage 'plugins'
    New-Item -ItemType Directory -Path $pluginsDir | Out-Null
    Copy-Item -LiteralPath $outputDll -Destination (Join-Path $pluginsDir 'NexusRoles.Plugin.dll')
    Copy-Item -LiteralPath $bundle -Destination (Join-Path $pluginsDir 'NexusRoles_assets')

    if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath }
    [IO.Compression.ZipFile]::CreateFromDirectory(
        $stage,
        $zipPath,
        [IO.Compression.CompressionLevel]::Optimal,
        $false
    )
    Write-Host "Created $zipPath"
}
finally {
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
}

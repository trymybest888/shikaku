param([string]$OutputName = 'Shikaku.exe')
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
$output = Join-Path $PSScriptRoot $OutputName
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'desktop') -Filter '*.cs' | ForEach-Object { $_.FullName }
$rankResources = @('novice','skilled','professional','divine') | ForEach-Object { "/resource:$(Join-Path $PSScriptRoot ('assets\ranks\' + $_ + '.png')),Shikaku.Rank.$_" }
& $compiler @rankResources /nologo /target:winexe /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll "/out:$output" "/win32manifest:$(Join-Path $PSScriptRoot 'desktop\app.manifest')" @sources
if ($LASTEXITCODE -ne 0) { throw 'Desktop build failed.' }
Write-Output "Built: $output"




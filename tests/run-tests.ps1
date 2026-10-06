$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$runner = Join-Path $PSScriptRoot 'Regression.exe'
$sources = Get-ChildItem -LiteralPath (Join-Path $project 'desktop') -Filter '*.cs' | ForEach-Object { $_.FullName }
$resources = @('novice','skilled','professional','divine') | ForEach-Object { "/resource:$(Join-Path $project ('assets\ranks\' + $_ + '.png')),Shikaku.Rank.$_" }
try {
    & $compiler /nologo /target:exe /main:Regression /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll "/out:$runner" @sources (Join-Path $PSScriptRoot 'Regression.cs') @resources
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
    & $runner $PSScriptRoot
    if ($LASTEXITCODE -ne 0) { throw 'Regression tests failed' }
} finally {
    if (Test-Path -LiteralPath $runner) { Remove-Item -LiteralPath $runner }
}

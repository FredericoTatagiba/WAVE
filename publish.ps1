#requires -Version 5
# Publica o WAVE como .exe único (self-contained) para Windows x64 e ARM64.
$ErrorActionPreference = 'Stop'

$project = 'src/WAVE.App/WAVE.App.csproj'
$rids = @('win-x64', 'win-arm64')

foreach ($rid in $rids) {
    Write-Host "Publicando $rid..." -ForegroundColor Cyan
    dotnet publish $project `
        -c Release `
        -r $rid `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o "publish/$rid"
}

Write-Host "Concluido. Executaveis em ./publish/<rid>/WAVE.exe" -ForegroundColor Green

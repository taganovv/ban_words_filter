param(
    [string]$Destination = "$env:USERPROFILE\Desktop\Filter Windows"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Ensure-Makensis {
    if (Get-Command makensis -ErrorAction SilentlyContinue) {
        return
    }

    if (Get-Command choco -ErrorAction SilentlyContinue) {
        choco install nsis -y | Out-Host
        return
    }

    throw "Установите NSIS: https://nsis.sourceforge.io/Download или choco install nsis"
}

Push-Location $Root
try {
    bash tools/build_windows_app.sh
    bash tools/prepare_windows_installer.sh
    Ensure-Makensis
    makensis tools/windows/installer.nsi
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Copy-Item "dist/Ban Words Filter Windows/Ban Words Filter Setup.exe" $Destination -Force
    Write-Host "Готово: $Destination\Ban Words Filter Setup.exe"
}
finally {
    Pop-Location
}

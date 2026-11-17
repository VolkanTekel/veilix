$ErrorActionPreference = 'Stop'
$adaReference = Join-Path $PSScriptRoot 'references/Atoll'
New-Item -ItemType Directory -Path $adaReference -Force | Out-Null
$adaCommit = 'f039ca923d1bbbf17a3610ec2cfbf017ab3bd02d'
$adaFiles = @('LICENSE','NOTICE','COPYRIGHT_ASSETS','TRADEMARKS','DynamicIsland/components/Notch/NotchShape.swift','DynamicIsland/components/Notch/DynamicIslandPillShape.swift','DynamicIsland/components/Notch/DynamicIslandHeader.swift','DynamicIsland/components/Notch/MinimalisticMusicPlayerView.swift','DynamicIsland/components/MinimalisticMusicView.swift','DynamicIsland/components/Music/VolumeCapsuleSlider.swift','DynamicIsland/components/Notch/DynamicIslandWindow.swift','DynamicIsland/helpers/ClosedMusicPairingVisibility.swift')
foreach ($adaFile in $adaFiles) {
    $adaTarget = Join-Path $adaReference $adaFile
    New-Item -ItemType Directory -Path (Split-Path $adaTarget) -Force | Out-Null
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/Ebullioscopic/Atoll/$adaCommit/$adaFile" -OutFile $adaTarget
}
Set-Content -LiteralPath (Join-Path $adaReference 'COMMIT') -Value $adaCommit
Write-Output 'Atoll kaynak referanslari indirildi.'

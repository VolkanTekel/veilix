param([switch]$WinUI)
$ErrorActionPreference='Stop'
$adaDesktop=[Environment]::GetFolderPath('DesktopDirectory')
if(-not [IO.Directory]::Exists($adaDesktop)){throw 'Masaustu klasoru bulunamadi.'}
$adaRelative=if($WinUI){'dist/Veilix-WinUI/Veilix.exe'}else{'app/bin/Veilix.exe'}
$adaTarget=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot $adaRelative)).Path
$adaAppDir=Split-Path $adaTarget
$adaOwnTargets=@($adaTarget,(Join-Path $adaAppDir 'Halo.exe'),(Join-Path $adaAppDir 'Ada.exe'),(Join-Path $PSScriptRoot 'app/bin/Veilix.exe'),(Join-Path $PSScriptRoot 'app/bin/Halo.exe'),(Join-Path $PSScriptRoot 'app/bin/Ada.exe'))
$adaShell=New-Object -ComObject WScript.Shell
$adaLinkPath=Join-Path $adaDesktop 'Veilix.lnk'
$adaIndex=1
while(Test-Path -LiteralPath $adaLinkPath){
 $adaExisting=$adaShell.CreateShortcut($adaLinkPath)
 if($adaExisting.TargetPath -in $adaOwnTargets){break}
 $adaSuffix=if($adaIndex -eq 1){''}else{" $adaIndex"}
 $adaLinkPath=Join-Path $adaDesktop ("Veilix Widget$adaSuffix.lnk");$adaIndex++
}
$adaLink=$adaShell.CreateShortcut($adaLinkPath)
$adaLink.TargetPath=$adaTarget
$adaLink.WorkingDirectory=$adaAppDir
$adaLink.IconLocation="$adaTarget,0"
$adaLink.Description='Veilix — Masaustu widget'
$adaLink.Save()
$adaVerify=$adaShell.CreateShortcut($adaLinkPath)
if($adaVerify.TargetPath -ne $adaTarget -or -not(Test-Path -LiteralPath $adaVerify.TargetPath)){throw 'Kisayol hedefi dogrulanamadi.'}
Write-Output $adaLinkPath
# Retire only old branded links belonging to this exact checkout.
foreach($adaPattern in @('Halo*.lnk','Ada.lnk')){
 foreach($adaOldLink in @(Get-ChildItem -LiteralPath $adaDesktop -Filter $adaPattern -File)){
  if($adaShell.CreateShortcut($adaOldLink.FullName).TargetPath -in $adaOwnTargets){Remove-Item -LiteralPath $adaOldLink.FullName}
 }
}
# Keep the existing startup preference, changing only our own executable path.
$adaRunKey='HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$adaRun=Get-ItemProperty -LiteralPath $adaRunKey -Name AdaDesktop -ErrorAction SilentlyContinue
if($null -ne $adaRun){
 $adaPrior=@($adaOwnTargets | ForEach-Object {$_; '"'+$_+'"'})
 if($adaRun.AdaDesktop -in $adaPrior){Set-ItemProperty -LiteralPath $adaRunKey -Name AdaDesktop -Value ('"'+$adaTarget+'"')}
}

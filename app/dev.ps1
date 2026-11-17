param([ValidateSet('Build','Test','Inspect','Run','Stop','Benchmark','MotionTest','MediaCheck','RecoveryTest','ToolsTest')][string]$Mode='Build',[switch]$Transparent)
$ErrorActionPreference='Stop'
$adaExe=Join-Path $PSScriptRoot 'bin/Veilix.exe'
$adaProcesses=@(Get-Process Ada,Halo,Veilix -ErrorAction SilentlyContinue | Where-Object { $_.Path -in @($adaExe,(Join-Path $PSScriptRoot "bin/Ada.exe"),(Join-Path $PSScriptRoot "bin/Halo.exe")) })
foreach($adaProcess in $adaProcesses){Stop-Process -Id $adaProcess.Id}
if($Mode -eq 'Stop'){exit}
& (Join-Path $PSScriptRoot 'build.ps1')
if($Mode -eq 'Build'){exit}
$adaArgs=@()
if($Mode -eq 'Test'){$adaArgs=@('--self-test')}
if($Mode -eq 'ToolsTest'){$adaArgs=@('--tools-test')}
if($Mode -eq 'RecoveryTest'){$adaArgs=@('--recovery-test')}
if($Mode -eq 'MediaCheck'){$adaArgs=@('--media-check')}
if($Mode -eq 'MotionTest'){$adaArgs=@('--motion-test')}
if($Mode -eq 'Inspect'){$adaArgs=@('--inspect')}
if($Mode -eq 'Benchmark'){$adaArgs=@('--benchmark')}
if($Transparent){$adaArgs+=@('--transparent')}
if($Mode -eq 'Inspect'){$adaProcess=Start-Process -FilePath $adaExe -ArgumentList $adaArgs -PassThru}
elseif($adaArgs.Count -gt 0){$adaProcess=Start-Process -FilePath $adaExe -ArgumentList $adaArgs -WindowStyle Hidden -PassThru}
else{$adaProcess=Start-Process -FilePath $adaExe -PassThru}
if($Mode -in @('Test','Benchmark','MotionTest','MediaCheck','RecoveryTest','ToolsTest')){$adaProcess.WaitForExit();Write-Output "Exit: $($adaProcess.ExitCode)"}

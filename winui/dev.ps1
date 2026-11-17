param([ValidateSet('Build','Run','Inspect','Test','Benchmark','Publish','Stop')][string]$Mode='Build')
$ErrorActionPreference='Stop'
$veilixRoot=Split-Path $PSScriptRoot
$veilixDotnet=Join-Path $veilixRoot '.tools/dotnet/dotnet.exe'
if(-not (Test-Path -LiteralPath $veilixDotnet)){
 $veilixDotnet=(Get-Command dotnet -CommandType Application -ErrorAction Stop).Source
}
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$veilixOutput=Join-Path $PSScriptRoot 'bin/Release/net10.0-windows10.0.19041.0/win-x64'
$veilixExe=Join-Path $veilixOutput 'Veilix.exe'
$veilixPublish=Join-Path $veilixRoot 'dist/Veilix-WinUI'
$veilixManagedExecutables=@($veilixExe)
if($Mode -in @('Publish','Stop')){$veilixManagedExecutables+=(Join-Path $veilixPublish 'Veilix.exe')}
foreach($veilixProcess in @(Get-Process Veilix -ErrorAction SilentlyContinue)){
 if($veilixProcess.Path -in $veilixManagedExecutables){Stop-Process -Id $veilixProcess.Id -Force -ErrorAction Stop}
}
if($Mode -eq 'Stop'){return}
& $veilixDotnet build (Join-Path $PSScriptRoot 'Veilix.WinUI.csproj') -c Release -p:NuGetAudit=false -v minimal
if($LASTEXITCODE -ne 0){throw 'WinUI derlemesi basarisiz.'}
if($Mode -eq 'Build'){return}
$veilixLegacyData=Join-Path $veilixRoot 'app/bin/data'
if($Mode -in @('Run','Inspect') -and (Test-Path -LiteralPath $veilixLegacyData) -and -not (Test-Path -LiteralPath (Join-Path $veilixOutput 'data'))){
 Copy-Item -LiteralPath $veilixLegacyData -Destination $veilixOutput -Recurse
}
if($Mode -eq 'Publish'){
 & $veilixDotnet publish (Join-Path $PSScriptRoot 'Veilix.WinUI.csproj') -c Release --no-restore -o $veilixPublish --self-contained false -v minimal
 if($LASTEXITCODE -ne 0){throw 'WinUI yayin paketi olusturulamadi.'}
 foreach($veilixLegal in @('LICENSE','NOTICE')){Copy-Item -LiteralPath (Join-Path $veilixRoot $veilixLegal) -Destination $veilixPublish -Force}
 Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $veilixPublish -Force
 New-Item -ItemType Directory -Force (Join-Path $veilixPublish 'licenses') | Out-Null
 Copy-Item -LiteralPath (Join-Path $veilixRoot 'app/Icons/LICENSE') -Destination (Join-Path $veilixPublish 'licenses/Fluent-System-Icons.txt') -Force
 Copy-Item -LiteralPath (Join-Path $veilixRoot 'app/Ada.ico') -Destination (Join-Path $veilixPublish 'Veilix.ico') -Force
 return
}
$veilixArgs=@();if($Mode -eq 'Inspect'){$veilixArgs=@('--inspect')};if($Mode -eq 'Test'){$veilixArgs=@('--self-test')};if($Mode -eq 'Benchmark'){$veilixArgs=@('--benchmark')}
if($veilixArgs.Count){$veilixProcess=Start-Process -FilePath $veilixExe -ArgumentList $veilixArgs -PassThru}else{$veilixProcess=Start-Process -FilePath $veilixExe -PassThru}
if($Mode -in @('Test','Benchmark')){if(-not $veilixProcess.WaitForExit(90000)){throw 'Kontrol 90 saniye icinde tamamlanmadi.'};Write-Output "Exit: $($veilixProcess.ExitCode)";if($veilixProcess.ExitCode -ne 0){throw 'Kontrol basarisiz. qa/FAILED.txt dosyasina bak.'}}

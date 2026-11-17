$ErrorActionPreference = 'Stop'
$adaRoot = $PSScriptRoot
$adaFx = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$adaMeta = Join-Path $env:WINDIR 'System32/WinMetadata'
$adaOutput = Join-Path $adaRoot 'bin'
New-Item -ItemType Directory -Path $adaOutput -Force | Out-Null
$adaRefs = @('System.dll','System.Core.dll','System.Numerics.dll','System.Numerics.Vectors.dll','System.Xaml.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Net.Http.dll','System.Web.Extensions.dll','System.Management.dll','System.Runtime.WindowsRuntime.dll','System.Runtime.InteropServices.WindowsRuntime.dll','WPF/WindowsBase.dll','WPF/PresentationCore.dll','WPF/PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $adaFx $_) }
$adaRefs += @('Windows.Foundation.winmd','Windows.Media.winmd','Windows.Storage.winmd','Windows.UI.winmd','Windows.System.winmd') | ForEach-Object { '/reference:' + (Join-Path $adaMeta $_) }
$adaRefs += @('System.Runtime','System.Threading.Tasks') | ForEach-Object { $adaFacade = Get-ChildItem (Join-Path $env:WINDIR "Microsoft.NET/assembly/GAC_MSIL/$_") -Filter '*.dll' -Recurse | Select-Object -First 1; if ($adaFacade) { '/reference:' + $adaFacade.FullName } }
$adaSources = @(Get-ChildItem -LiteralPath $adaRoot -Filter '*.cs' | ForEach-Object FullName)
& (Join-Path $adaFx 'csc.exe') /nologo /target:winexe /platform:x64 /optimize+ /utf8output /out:"$adaOutput/Veilix.exe" /win32manifest:"$adaRoot/app.manifest" /win32icon:"$adaRoot/Ada.ico" @adaRefs @adaSources
if ($LASTEXITCODE -ne 0) { throw 'Veilix derlenemedi.' }
Copy-Item -LiteralPath "$adaRoot/Veilix.exe.config" -Destination "$adaOutput/Veilix.exe.config" -Force
Copy-Item -LiteralPath "$adaRoot/Styles.xaml" -Destination "$adaOutput/Styles.xaml" -Force
New-Item -ItemType Directory -Path "$adaOutput/licenses" -Force | Out-Null
Copy-Item -LiteralPath "$adaRoot/Icons/LICENSE" -Destination "$adaOutput/licenses/Fluent-System-Icons.txt" -Force
foreach($adaLegal in @('LICENSE','NOTICE','README.md')){Copy-Item -LiteralPath (Join-Path (Split-Path $adaRoot) $adaLegal) -Destination (Join-Path $adaOutput $adaLegal) -Force}
Copy-Item -LiteralPath "$adaOutput/Veilix.exe" -Destination "$adaOutput/Ada.exe" -Force
Copy-Item -LiteralPath "$adaOutput/Veilix.exe.config" -Destination "$adaOutput/Ada.exe.config" -Force
Copy-Item -LiteralPath "$adaOutput/Veilix.exe" -Destination "$adaOutput/Halo.exe" -Force
Copy-Item -LiteralPath "$adaOutput/Veilix.exe.config" -Destination "$adaOutput/Halo.exe.config" -Force
Write-Output "$adaOutput/Veilix.exe"

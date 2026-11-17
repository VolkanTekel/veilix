[CmdletBinding()]
param(
 [Parameter(Mandatory)][string]$PublishDirectory,
 [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]*$')][string]$Release,
 [string]$OutputDirectory = (Join-Path (Split-Path $PSScriptRoot) 'dist'),
 [switch]$IncludeSourceZip
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$publish = (Resolve-Path -LiteralPath $PublishDirectory).Path
$requiredRuntime = @('Veilix.exe', 'Veilix.dll', 'Veilix.deps.json', 'Veilix.runtimeconfig.json', 'Veilix.pri', 'App.xbf')
foreach ($name in $requiredRuntime) {
 if (-not (Test-Path -LiteralPath (Join-Path $publish $name) -PathType Leaf)) {
  throw "WinUI publish is missing $name."
 }
}

# Use the publish output's runtime files and asset directories only.
$binaries = @('.dll', '.exe', '.mui', '.pri', '.winmd', '.xbf')
$runtime = @(
 Get-ChildItem -LiteralPath $publish -Force -File | Where-Object {
  $_.Name -in $requiredRuntime -or $_.Extension -in $binaries
 }
 foreach ($locale in @(Get-ChildItem -LiteralPath $publish -Directory | Where-Object {
  $_.Name -cmatch '^[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})+$'
 })) {
  Get-ChildItem -LiteralPath $locale.FullName -File -Filter '*.mui'
 }
 $xaml = Join-Path $publish 'Microsoft.UI.Xaml'
 if (Test-Path -LiteralPath $xaml -PathType Container) {
  Get-ChildItem -LiteralPath $xaml -Recurse -File | Where-Object {
   $_.Extension -in ($binaries + @('.html', '.png'))
  }
 }
)

$source = @(
 foreach ($name in @('.gitignore', 'AGENTS.md', 'README.md', 'LICENSE', 'NOTICE',
  'fetch-atoll-reference.ps1', 'fetch-fluent-icons.ps1', 'generate-fluent-icons.py', 'install-shortcut.ps1')) {
  Get-Item -LiteralPath (Join-Path $root $name) -Force
 }
 foreach ($folder in @('winui', 'app')) {
  Get-ChildItem -LiteralPath (Join-Path $root $folder) -File | Where-Object {
   $_.Extension -in @('.cs', '.csproj', '.config', '.ico', '.manifest', '.md', '.ps1', '.xaml')
  }
 }
 Get-ChildItem -LiteralPath (Join-Path $root 'app/Icons'), (Join-Path $root 'references/Atoll') -Recurse -File
)

Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
function Add-ArchiveFiles($Archive, $Files, [string]$Base, [string]$Prefix) {
 foreach ($file in $Files | Sort-Object FullName) {
  $relative = $file.FullName.Substring($Base.Length + 1).Replace('\', '/')
  [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
   $Archive, $file.FullName, "$Prefix$relative", 'Optimal')
 }
}

$output = (New-Item -ItemType Directory -Path $OutputDirectory -Force).FullName
$runtimePath = Join-Path $output "Veilix-Windows-x64-$Release.zip"
$sourcePath = Join-Path $output "Veilix-Source-$Release.zip"
if ((Test-Path -LiteralPath $runtimePath) -or ($IncludeSourceZip -and (Test-Path -LiteralPath $sourcePath))) {
 throw "An archive for release $Release already exists. Use a new release label."
}

$suffix = ".$([guid]::NewGuid().ToString('N')).tmp"
$runtimeTemp = $runtimePath + $suffix
$sourceTemp = $sourcePath + $suffix
try {
 $zip = [IO.Compression.ZipFile]::Open($runtimeTemp, 'Create')
 try {
  Add-ArchiveFiles $zip $runtime $publish 'Veilix/'
  Add-ArchiveFiles $zip (Get-Item -LiteralPath (Join-Path $root 'LICENSE'), (Join-Path $root 'NOTICE')) $root 'Veilix/'
  [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
   $zip, (Join-Path $root 'app/Ada.ico'), 'Veilix/Veilix.ico', 'Optimal')
  [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
   $zip, (Join-Path $root 'app/Icons/LICENSE'), 'Veilix/licenses/Fluent-System-Icons.txt', 'Optimal')
  Add-ArchiveFiles $zip $source $root 'Veilix/source/'
  $writer = [IO.StreamWriter]::new($zip.CreateEntry('Veilix/START.txt').Open())
  try {
   $writer.WriteLine(@'
Veilix for Windows x64

Extract the entire ZIP to a writable folder, then open Veilix.exe.
Requires Windows 10 2004 or later, or Windows 11, and the .NET 10 Desktop Runtime x64.
https://dotnet.microsoft.com/en-us/download/dotnet/10.0

Switch to English under Ayarlar > Dil > English.
The source folder contains the source code and build instructions.

ZIP'in tamamini yazilabilir bir klasore cikar ve Veilix.exe dosyasini ac.
Windows 10 2004 veya uzeri, x64 ve .NET 10 Desktop Runtime x64 gerekir.
'@)
  } finally { $writer.Dispose() }
 } finally { $zip.Dispose() }

 if ($IncludeSourceZip) {
  $zip = [IO.Compression.ZipFile]::Open($sourceTemp, 'Create')
  try { Add-ArchiveFiles $zip $source $root 'Veilix-source/' } finally { $zip.Dispose() }
  Move-Item -LiteralPath $sourceTemp -Destination $sourcePath
 }
 Move-Item -LiteralPath $runtimeTemp -Destination $runtimePath
} finally {
 foreach ($temp in @($runtimeTemp, $sourceTemp)) {
  if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp }
 }
}

Get-Item -LiteralPath $runtimePath
if ($IncludeSourceZip) { Get-Item -LiteralPath $sourcePath }

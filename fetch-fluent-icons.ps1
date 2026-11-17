$ErrorActionPreference='Stop'
$adaIconDir=Join-Path $PSScriptRoot 'app/Icons'
New-Item -ItemType Directory -Path $adaIconDir -Force | Out-Null
$adaCommitFile=Join-Path $adaIconDir 'COMMIT'
if(Test-Path -LiteralPath $adaCommitFile){$adaCommit=(Get-Content -LiteralPath $adaCommitFile -Raw).Trim()}
else{$adaCommit=(Invoke-RestMethod -Uri 'https://api.github.com/repos/microsoft/fluentui-system-icons/commits/main' -Headers @{'User-Agent'='Ada-Icons'}).sha;Set-Content -LiteralPath $adaCommitFile -Value $adaCommit}
$adaIcons=@(
 @('arrow-left','Chevron Left','chevron_left'),@('arrow-right','Chevron Right','chevron_right'),@('folder','Folder','folder'),@('document','Document','document'),@('timer','Timer','timer'),@('check','Checkmark Circle','checkmark_circle'),@('bell','Alert','alert'),@('delete','Delete','delete'),@('edit','Edit','edit'),@('add','Add','add'),@('subtract','Subtract','subtract'),@('open','Open','open'),@('undo','Arrow Undo','arrow_undo'),@('keyboard','Keyboard','keyboard'),@('battery','Battery Charge','battery_charge'),
 @('bluetooth','Bluetooth','bluetooth'),@('settings','Settings','settings'),@('controls','Options','options'),@('music','Music Note 2','music_note_2'),
 @('sun','Weather Sunny','weather_sunny'),@('cloud','Weather Cloudy','weather_cloudy'),@('rain','Weather Rain','weather_rain'),@('snow','Weather Snow','weather_snow'),
 @('calendar','Calendar LTR','calendar_ltr'),@('speaker','Speaker 2','speaker_2'),@('mute','Speaker Off','speaker_off'),@('headphones','Headphones','headphones'),
 @('monitor','Desktop','desktop'),@('palette','Color','color'),@('system','Settings','settings'),
 @('play','Play','play'),@('pause','Pause','pause'),@('play-filled','Play','play','filled'),@('pause-filled','Pause','pause','filled'),@('previous','Previous','previous'),@('next','Next','next'),
 @('refresh','Arrow Clockwise','arrow_clockwise'),@('close','Dismiss','dismiss'),@('clock','Clock','clock'),@('chevron','Chevron Down','chevron_down'),
 @('wind','Weather Squalls','weather_squalls'),@('drop','Drop','drop'),@('temperature','Temperature','temperature'),@('sunrise','Weather Sunny Low','weather_sunny_low'),@('moon','Weather Moon','weather_moon'),@('fog','Weather Fog','weather_fog'),@('storm','Weather Thunderstorm','weather_thunderstorm')
)
foreach($adaIcon in $adaIcons){
 $adaTarget=Join-Path $adaIconDir ($adaIcon[0]+'.svg')
 if(Test-Path -LiteralPath $adaTarget){continue}
 $adaFolder=[Uri]::EscapeDataString($adaIcon[1]);$adaName=$adaIcon[2]
 $adaVariant=if($adaIcon.Count -gt 3){$adaIcon[3]}else{'regular'}
 $adaUri="https://raw.githubusercontent.com/microsoft/fluentui-system-icons/$adaCommit/assets/$adaFolder/SVG/ic_fluent_$($adaName)_24_$adaVariant.svg"
 Invoke-WebRequest -Uri $adaUri -OutFile $adaTarget
}
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/$adaCommit/LICENSE" -OutFile (Join-Path $adaIconDir 'LICENSE')
Write-Output "Fluent System Icons 24 Regular — $adaCommit"

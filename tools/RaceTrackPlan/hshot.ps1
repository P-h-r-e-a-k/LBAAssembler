param([int]$cube = 67, [string]$tp = '', [string]$out = 'E:\dump\TEMP\hshot.png', [int]$wait = 60, [string]$game = 'E:\dump\LBA2RaceTrackBuild\Game', [string]$vars = '')
# Headless engine screenshot of a cube of the sandbox game (optionally with the hero teleported: "x y z").
$eng = (Get-ChildItem E:\dump\LBAAssembler\bin\Debug\net10.0-windows\native\lba2cc.*.exe | Sort-Object LastWriteTime | Select-Object -Last 1).FullName
$a = @('--headless','--game-dir',$game,'--user-dir','E:\dump\TEMP\engine_user','--no-autosave','--resolution','640x480','--exec-at','4','skipmodals 1')
if ($vars -ne '') { $a += @('--exec-at','5',$vars) }
$a += @('--exec-at','6',"cube $cube")
if ($tp -ne '') { $a += @('--exec-at','30',"teleport $tp") }
$a += @('--exec-at',"$wait","dumpstate",'--exec-at',"$($wait+1)","screenshot $out",'--tick',"$($wait+30)",'--exit')
$ErrorActionPreference = 'Continue'
$log = & $eng $a 2>&1 | ForEach-Object { "$_" }
$shot = (Get-ChildItem E:\dump\TEMP\engine_user\save\shoot\shot_*.png | Sort-Object LastWriteTime | Select-Object -Last 1)
Copy-Item $shot.FullName $out -Force
$j = Get-ChildItem E:\dump\TEMP\engine_user\save\shoot\state_*.json | Sort-Object LastWriteTime | Select-Object -Last 1
$d = Get-Content $j.FullName -Raw | ConvertFrom-Json
$d.actors | ForEach-Object { "actor $($_.index): x=$($_.x) y=$($_.y) z=$($_.z) life=$($_.life) body=$($_.body) flags=$($_.flags)" }
"saved $out"

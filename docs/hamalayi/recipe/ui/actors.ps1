# Scene 96's track points and actors put into the map's scene through the scene editor alone (actor_ref.json: LBA1's): Add > Track point
# and Add > New 3D actor without a script… (its body chosen by name in the list, the map's own characters among them), each placed by
# typing where, then the inspector's fields typed in. Twinsen's start too. Saved.
param([switch]$SkipPoints)
. $PSScriptRoot\lib.ps1
$ref = Get-Content $PSScriptRoot\..\plan\actor_ref.json -Raw | ConvertFrom-Json
$Button = [System.Windows.Automation.ControlType]::Button
$MenuItem = [System.Windows.Automation.ControlType]::MenuItem
function V($e, $s) { $e.SetFocus(); $e.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue([string]$s) }
$w = AppWindows | Where-Object { $_.Title -like "LBA2 - scene editor*" } | Select-Object -First 1
if (-not $w) { throw "open the scene editor first" }
$se = $A::FromHandle($w.H)
function PopupItem($name, [switch]$Try) {
  for ($i = 0; $i -lt 20; $i++) {
    foreach ($win in AppWindows) {
      $it = $A::FromHandle($win.H).FindAll($TS::Descendants, (Cond $A::ControlTypeProperty $MenuItem)) | Where-Object { $_.Current.Name -like "$name*" } | Select-Object -First 1
      if ($it) { return $it }
    }
    Start-Sleep -Milliseconds 150
  }
  if ($Try) { return $null }
  throw "no menu item $name"
}
# Add ▾ pressed and the item chosen in its menu -- pressed again if the menu didn't open (the editor still busy with the last one)
function AddMenu($item) {
  for ($attempt = 0; $attempt -lt 4; $attempt++) {
    Invoke ($se.FindAll($TS::Descendants, (Cond $A::ControlTypeProperty $Button)) | Where-Object { $_.Current.Name -like "Add*" } | Select-Object -First 1); Start-Sleep -Milliseconds 400
    $it = PopupItem $item -Try
    if ($it) { Invoke $it; Start-Sleep -Milliseconds 400; return }
    Start-Sleep 1
  }
  throw "no menu item $item"
}
function PlaceAt($x, $y, $z) {
  V (EditBox "Place X" $se) $x; V (EditBox "Place Y" $se) $y; V (EditBox "Place Z" $se) $z
  Invoke (ByNameType "Place here" $Button $se); Start-Sleep -Milliseconds 500
}
function Field($name, $value) {
  $box = EditBox $name $se
  if (-not $box) { throw "no field $name" }
  V $box $value
  (ById "ZoomSlider" $se).SetFocus(); Start-Sleep -Milliseconds 120
}
function PickBody($filter) {
  $p = $null; for ($i = 0; $i -lt 30 -and -not $p; $i++) { Start-Sleep -Milliseconds 150; $p = Win "Choose a body" }
  if (-not $p) { throw "no body list" }
  $pr = $A::FromHandle($p.H)
  V ($pr.FindFirst($TS::Descendants, (Cond $A::ControlTypeProperty ([System.Windows.Automation.ControlType]::Edit)))) $filter
  Start-Sleep -Milliseconds 400
  $item = $pr.FindFirst($TS::Descendants, (Cond $A::ControlTypeProperty ([System.Windows.Automation.ControlType]::ListItem)))
  if (-not $item) { throw "no body like $filter" }
  "  body: $($item.Current.Name)"
  SelectItem $item
  Invoke (ByNameType "OK" $Button $pr); Start-Sleep -Milliseconds 400
}

# ---- the track points, in LBA1's order (0 to 33)
if (-not $SkipPoints) { foreach ($p in $ref.points) { AddMenu "Track point"; PlaceAt $p[0] $p[1] $p[2] } }
"$($ref.points.Count) track points"

# ---- Twinsen's start
SelectItem (ByNameType "Actors" ([System.Windows.Automation.ControlType]::TabItem) $se); Start-Sleep -Milliseconds 300
$al = ById "ActorList" $se
SelectItem ($al.FindAll($TS::Children, (Cond $A::ControlTypeProperty ([System.Windows.Automation.ControlType]::ListItem)))[0]); Start-Sleep -Milliseconds 300
$hero = $ref.actors[0]
Field "X" $hero.x; Field "Y" $hero.y; Field "Z" $hero.z

# ---- the actors 2 to 13: which body (its name in the list), and the fields
$bodies = @{ "2" = "blue-white"; "3" = "Snowboard: Snowboard"; "12" = "Snowboard: Snowboard"; "13" = "Twinsen on his snowboard" }
$flagBody = @{ "0" = "Slalom flag: Red flag"; "48" = "Slalom flag: Blue flag"; "49" = "Slalom flag: Light blue flag"; "17" = "Slalom flag: Green flag" }
foreach ($act in $ref.actors | Where-Object { [int]$_.n -ge 2 }) {
  $filter = if ($act.entity -eq "73") { $flagBody[$act.body] } else { $bodies[$act.n] }
  AddMenu "New 3D actor without a script"
  PickBody $filter
  PlaceAt $act.x $act.y $act.z
  Field "Flags" $act.flags
  Field "Life" $act.life; Field "Armour" $act.armor
  if ($act.n -eq "12") { Field "Bonus option" 48; Field "Bonus count" 3 }
  if ($act.n -eq "2") { Field "Bonus option" 48; Field "Bonus count" 2 }
  # (the instructor and the rider move by their track scripts from the start: Move 3, track)
  if ($act.n -eq "2" -or $act.n -eq "13") { Field "Move" 3 }
  "actor $($act.n) put"
}
Invoke (ByNameType "Save" $Button $se); Start-Sleep 2
(ById "StatusText" $se).Current.Name
Shot $w.H "11_actors"
Menu File "Save map"; Start-Sleep 6
"map saved"

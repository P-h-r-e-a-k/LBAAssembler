# Scene 96's zones put into the map's scene through the scene editor alone (zone_plan.json the plan): Add > Zone > <type>, the place typed
# into the bar under the plan and Place here, then the inspector's fields (the box's corners, the destination or zone number) typed in.
. $PSScriptRoot\lib.ps1
$plan = Get-Content $PSScriptRoot\..\plan\zone_plan.json -Raw | ConvertFrom-Json
$Button = [System.Windows.Automation.ControlType]::Button
$MenuItem = [System.Windows.Automation.ControlType]::MenuItem
function V($e, $s) { $e.SetFocus(); $e.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue([string]$s) }
$w = AppWindows | Where-Object { $_.Title -like "LBA2 - scene editor*" } | Select-Object -First 1
if (-not $w) { throw "open the scene editor first" }
$se = $A::FromHandle($w.H)
$typeNames = @("cube change", "camera", "scenaric", "grid", "giver", "message", "ladder", "escalator", "hit", "rail")
# (a popup menu's item: searched in every top-level window of the editor's process)
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
foreach ($z in $plan.zones) {
  # (Add ▾ pressed again if its menu didn't open: the editor still busy with the last one)
  $zoneMenu = $null
  for ($attempt = 0; $attempt -lt 4 -and -not $zoneMenu; $attempt++) {
    Invoke ($se.FindAll($TS::Descendants, (Cond $A::ControlTypeProperty $Button)) | Where-Object { $_.Current.Name -like "Add*" } | Select-Object -First 1); Start-Sleep -Milliseconds 400
    $zoneMenu = PopupItem "Zone" -Try
    if (-not $zoneMenu) { Start-Sleep 1 }
  }
  if (-not $zoneMenu) { throw "no Zone menu" }
  $zoneMenu.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand(); Start-Sleep -Milliseconds 400
  Invoke (PopupItem $typeNames[$z.type]); Start-Sleep -Milliseconds 300
  $b = $z.box
  V (EditBox "Place X" $se) ([int](($b[0] + $b[3]) / 2)); V (EditBox "Place Y" $se) $b[1]; V (EditBox "Place Z" $se) ([int](($b[2] + $b[5]) / 2))
  Invoke (ByNameType "Place here" $Button $se); Start-Sleep -Milliseconds 500
  $rows = [ordered]@{ "X min" = $b[0]; "X max" = $b[3]; "Y min" = $b[1]; "Y max" = $b[4]; "Z min" = $b[2]; "Z max" = $b[5] }
  foreach ($p in $z.fields.PSObject.Properties) { $rows[$p.Name] = if ($p.Value -eq "self") { [regex]::Match($w.Title, "scene (\d+)").Groups[1].Value } else { $p.Value } }
  foreach ($k in $rows.Keys) {
    $box = EditBox $k $se
    if (-not $box) { throw "zone $($z.lba1): no field $k" }
    V $box $rows[$k]
    # (committed when it loses the focus: the next field takes it)
    (ById "ZoomSlider" $se).SetFocus(); Start-Sleep -Milliseconds 120
  }
  "zone $($z.lba1): $($typeNames[$z.type]) put"
}
Invoke (ByNameType "Save" $Button $se); Start-Sleep 2
(ById "StatusText" $se).Current.Name
Shot $w.H "11_zones"

Menu File "Save map"; Start-Sleep 6
"map saved"

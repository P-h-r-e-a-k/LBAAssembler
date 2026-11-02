# The reports' key from the file beside the repos, masked as IssueReport.cs (and LBRA's build.ps1 and BugReport.cs) has it: printed for the build, never kept in the repo
param([string]$File)
$mask = [Text.Encoding]::ASCII.GetBytes('LBRA-reports')
$plain = [Text.Encoding]::ASCII.GetBytes((Get-Content $File -Raw).Trim())
-join (0..($plain.Length - 1) | ForEach-Object { ($plain[$_] -bxor $mask[$_ % $mask.Length]).ToString('X2') })

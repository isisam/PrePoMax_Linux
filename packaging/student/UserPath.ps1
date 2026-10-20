param([ValidateSet('Add','Remove')][string]$Action,[string]$InstallDir)
# Author: S.Y. Su. Registry API preserves REG_EXPAND_SZ and avoids setx truncation.
$ErrorActionPreference='Stop'
$key=[Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Environment')
try {
  $names=$key.GetValueNames();$exists=$names -contains 'Path'
  $raw=$key.GetValue('Path','',[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
  $kind=if($exists){$key.GetValueKind('Path')}else{[Microsoft.Win32.RegistryValueKind]::ExpandString}
  $parts=@();if($raw -ne ''){$parts=@($raw.Split(';'))}
  function Normalize([string]$value){[Environment]::ExpandEnvironmentVariables($value.Trim().Trim('"')).TrimEnd('\').ToLowerInvariant()}
  $wanted=Normalize $InstallDir
  $marker=Join-Path $InstallDir 'path-owner.txt'
  if($Action -eq 'Add') {
    # Preserve ownership on a same-directory repair installation.
    if(@($parts | Where-Object {(Normalize $_) -eq $wanted}).Count -eq 0) {
      $key.SetValue('Path',$(if($raw -eq ''){$InstallDir}else{"$raw;$InstallDir"}),$kind)
      [IO.File]::WriteAllText($marker,$(if($exists){'existing-value'}else{'new-value'}))
    }
  } elseif(Test-Path -LiteralPath $marker) {
    $remaining=@($parts | Where-Object {(Normalize $_) -ne $wanted}) -join ';'
    if($remaining -eq '' -and [IO.File]::ReadAllText($marker) -eq 'new-value') {
      $key.DeleteValue('Path',$false)
    }else{$key.SetValue('Path',$remaining,$kind)}
  }
} finally {$key.Close()}

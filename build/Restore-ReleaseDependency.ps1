param([Parameter(Mandatory=$true)][string]$ReleaseZip)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$repo = Split-Path $PSScriptRoot -Parent
$target = Join-Path $repo 'ThirdParty/NCalc.RCM/Antlr3.Runtime.dll'
if (Test-Path -LiteralPath $target) { throw 'Target exists; use a fresh checkout.' }
$archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $ReleaseZip).Path)
try {
    $entries = @($archive.Entries | Where-Object { $_.FullName -match '/lib/Antlr3[.]Runtime[.]dll$' })
    if ($entries.Count -ne 1) { throw 'Expected one Antlr3.Runtime.dll in release ZIP.' }
    $inputStream = $entries[0].Open()
    $memory = New-Object IO.MemoryStream
    try { $inputStream.CopyTo($memory) } finally { $inputStream.Dispose() }
    $bytes = $memory.ToArray()
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $actual = ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-','').ToLowerInvariant() } finally { $sha.Dispose() }
    if ($actual -ne '2038b9d2998dd52db9fcd3988abfee3de0e5977b7dcbab6e9223a71ea792c216') { throw 'Antlr3 runtime hash mismatch.' }
    [IO.File]::WriteAllBytes($target, $bytes)
    $memory.Dispose()
} finally { $archive.Dispose() }
Write-Output 'Antlr3.Runtime assembly version 3.1.3.22795 restored locally; keep this dependency untracked.'

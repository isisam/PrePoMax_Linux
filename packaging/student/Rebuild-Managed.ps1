param([string]$BuildRoot = "$env:LOCALAPPDATA\StudentPkgBuild")
$ErrorActionPreference='Stop'
$src=Join-Path $BuildRoot 'rc1g8'
$runtime=Join-Path $BuildRoot 'runtime'
$output=Join-Path $BuildRoot 'managed'
New-Item -ItemType Directory -Force $output | Out-Null
$framework=Join-Path $BuildRoot 'net48\build\.NETFramework\v4.8'
$dotnet=Join-Path $BuildRoot 'dotnet\dotnet.exe'
$csc=Join-Path $BuildRoot 'dotnet\sdk\8.0.426\Roslyn\bincore\csc.dll'
foreach($name in @('CaeModel','APIs','PrePoMax.Core','PrePoMax')) {
  $isGui=$name -eq 'PrePoMax'
  $original=if($isGui){Join-Path $runtime 'PrePoMax.exe'}else{Join-Path $runtime "lib\$name.dll"}
  $dest=Join-Path $output $(if($isGui){'PrePoMax.exe'}else{"$name.dll"})
  $argsList=[System.Collections.Generic.List[string]]::new()
  @('/nologo','/nostdlib+','/optimize+','/debug-','/deterministic+','/platform:x64','/unsafe+','/langversion:12','/define:TRACE,NET48,NETFRAMEWORK',"/pathmap:$src=R:\src",('/target:'+$(if($isGui){'winexe'}else{'library'})),"/out:`"$dest`"") | ForEach-Object {$argsList.Add($_)}
  foreach($ref in Get-ChildItem $framework -Filter '*.dll') {
    try { [void][Reflection.AssemblyName]::GetAssemblyName($ref.FullName);$argsList.Add("/reference:`"$($ref.FullName)`"") } catch {}
  }
  foreach($ref in Get-ChildItem "$framework\Facades" -Filter '*.dll') {$argsList.Add("/reference:`"$($ref.FullName)`"")}
  foreach($ref in Get-ChildItem "$runtime\lib" -Filter '*.dll') {
    if($ref.BaseName -eq $name){continue}
    try {
      [void][Reflection.AssemblyName]::GetAssemblyName($ref.FullName)
      $replacement=Join-Path $output $ref.Name
      $chosen=if(Test-Path $replacement){$replacement}else{$ref.FullName}
      $argsList.Add("/reference:`"$chosen`"")
    } catch {}
  }
  $assembly=[Reflection.Assembly]::ReflectionOnlyLoadFrom($original)
  foreach($resource in $assembly.GetManifestResourceNames()) {
    $stream=$assembly.GetManifestResourceStream($resource)
    $resourcePath=Join-Path $output "$name.$resource"
    $file=[IO.File]::Create($resourcePath);$stream.CopyTo($file);$file.Close();$stream.Close()
    $argsList.Add("/resource:`"$resourcePath`",$resource")
  }
  if($isGui){$argsList.Add("/win32icon:`"$src\PrePoMax\main.ico`"")}
  $files=if($isGui){
    [xml]$project=Get-Content "$src\PrePoMax\PrePoMax.csproj"
    $project.Project.ItemGroup.Compile | Where-Object {$_.Include} | ForEach-Object {Join-Path "$src\PrePoMax" $_.Include}
  }else{Get-ChildItem "$src\$name" -Recurse -Filter '*.cs' | Where-Object {$_.FullName -notmatch '\\(obj|bin)\\'} | ForEach-Object {$_.FullName}}
  foreach($file in $files){$argsList.Add("`"$file`"")}
  $rsp=Join-Path $output "$name.rsp"
  [IO.File]::WriteAllLines($rsp,$argsList,[Text.UTF8Encoding]::new($false))
  & $dotnet $csc "@$rsp" *> (Join-Path $output "$name.build.log")
  if($LASTEXITCODE -ne 0){Get-Content (Join-Path $output "$name.build.log") -Tail 40;throw "Compile failed: $name"}
  Write-Output "Compiled $name"
}
# Resource extraction holds reflection-only assembly handles until this process exits.
# make_nsis.py copies the completed assemblies after the compiler process has exited.

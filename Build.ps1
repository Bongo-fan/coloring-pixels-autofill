param([Parameter(Mandatory)][string]$GameDir)
$ErrorActionPreference = 'Stop'
$managed = Join-Path $GameDir 'ColoringPixels_Data\Managed'
$bepinex = Join-Path $GameDir 'BepInEx\core\BepInEx.dll'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$assemblies = 'UnityEngine','UnityEngine.CoreModule','UnityEngine.IMGUIModule','UnityEngine.GridModule','UnityEngine.InputModule','Assembly-CSharp','netstandard'
$references = foreach ($name in $assemblies) { '/reference:' + (Join-Path $managed "$name.dll") }
& $compiler /nologo /target:library ("/out:" + (Join-Path $PSScriptRoot 'PixelAutofill.dll')) ("/reference:" + $bepinex) $references (Join-Path $PSScriptRoot 'PixelAutofill.cs')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

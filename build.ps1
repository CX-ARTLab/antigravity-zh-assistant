$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceRoot = Join-Path $projectRoot "src"
$outputRoot = Join-Path $projectRoot "dist"
$compiler = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$outputName = "AntigravityZhAssistant.exe"
$outputPath = Join-Path $outputRoot $outputName

if (-not (Test-Path -LiteralPath $compiler)) {
    throw "The .NET Framework C# compiler was not found: $compiler"
}

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

& $compiler `
    /nologo `
    /target:winexe `
    /platform:anycpu `
    /optimize+ `
    "/win32icon:$sourceRoot\Assets\assistant-icon.ico" `
    "/win32manifest:$sourceRoot\app.manifest" `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /reference:System.Net.Http.dll `
    /reference:System.Web.Extensions.dll `
    "/resource:$sourceRoot\translator.js,TranslatorJs" `
    "/resource:$sourceRoot\Assets\assistant-icon.ico,AssistantIcon" `
    "/resource:$sourceRoot\Assets\assistant-icon.png,AssistantIconPng" `
    "/out:$outputPath" `
    "$sourceRoot\Program.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

Get-Item -LiteralPath $outputPath

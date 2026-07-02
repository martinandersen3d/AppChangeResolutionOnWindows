param(
	[string]$Configuration = "Release",
	[string]$Runtime = "win-x64",
	[string]$Project = ".\AppChangeResolutionOnWindows.csproj",
	[string]$OutputDir = ".\Download"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $scriptRoot

try {
	$publishDir = Join-Path $scriptRoot $OutputDir

	if (Test-Path $publishDir) {
		Remove-Item -Path $publishDir -Recurse -Force
	}

	dotnet publish $Project `
		-c $Configuration `
		-r $Runtime `
		--self-contained true `
		-p:PublishSingleFile=true `
		-p:EnableCompressionInSingleFile=true `
		-p:DebugType=None `
		-p:DebugSymbols=false `
		-o $publishDir

	$exe = Get-ChildItem -Path $publishDir -Filter *.exe | Select-Object -First 1

	if (-not $exe) {
		throw "No EXE file was produced in '$publishDir'."
	}

	Write-Host "Build complete."
	Write-Host "Single-file EXE: $($exe.FullName)"
}
finally {
	Pop-Location
}

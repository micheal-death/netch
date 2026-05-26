param (
	[Parameter()]
	[ValidateSet('Debug', 'Release')]
	[string]
	$Configuration = 'Release',

	[Parameter()]
	[ValidateNotNullOrEmpty()]
	[string]
	$OutputPath = 'release',

	[Parameter()]
	[bool]
	$SelfContained = $True,

	[Parameter()]
	[bool]
	$PublishSingleFile = $True,

	[Parameter()]
	[bool]
	$PublishReadyToRun = $False,

	[Parameter()]
	[string]
	$GeoLite2CountryMmdbPath = ''
)
$RepoRoot = (Resolve-Path (Split-Path $MyInvocation.MyCommand.Path -Parent)).Path
Push-Location $RepoRoot

function Resolve-RepoPath {
	param (
		[string]
		$Path
	)

	if ( [System.IO.Path]::IsPathRooted($Path) ) {
		return $Path
	}

	return Join-Path $RepoRoot $Path
}

function Resolve-MSBuild {
	$command = Get-Command msbuild -ErrorAction SilentlyContinue
	if ( $command ) {
		return $command.Source
	}

	$vswhereCandidates = @(
		'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe',
		'C:\Program Files\Microsoft Visual Studio\Installer\vswhere.exe'
	)

	foreach ( $vswhere in $vswhereCandidates ) {
		if ( -Not ( Test-Path $vswhere ) ) {
			continue
		}

		$found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
		if ( $found ) {
			return $found
		}
	}

	$commonPaths = @(
		'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe',
		'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe',
		'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe',
		'C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe',
		'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe',
		'C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe',
		'C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe',
		'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
	)

	foreach ( $path in $commonPaths ) {
		if ( Test-Path $path ) {
			return $path
		}
	}

	Write-Error 'MSBuild.exe was not found. Install Visual Studio Build Tools with MSBuild/C++ support, or run this script from Developer PowerShell for Visual Studio.'
	exit 1
}

function Copy-GeoLiteDatabase {
	param (
		[string]
		$DestinationPath
	)

	$localCandidates = @()
	if ( -Not [string]::IsNullOrWhiteSpace($GeoLite2CountryMmdbPath) ) {
		$localCandidates += (Resolve-RepoPath $GeoLite2CountryMmdbPath)
	}
	$localCandidates += (Resolve-RepoPath 'Storage\GeoLite2-Country.mmdb')

	foreach ( $path in $localCandidates ) {
		if ( Test-Path $path ) {
			Copy-Item -Force $path $DestinationPath
			return
		}
	}

	$downloadCandidates = @(
		'https://github.com/Loyalsoldier/geoip/releases/latest/download/Country.mmdb',
		'https://raw.githubusercontent.com/Loyalsoldier/geoip/release/Country.mmdb'
	)

	foreach ( $uri in $downloadCandidates ) {
		try {
			Invoke-WebRequest -Uri $uri -OutFile $DestinationPath -ErrorAction Stop
			return
		}
		catch {
			Write-Warning "Failed to download GeoLite2-Country.mmdb from $uri"
		}
	}

	Write-Error 'GeoLite2-Country.mmdb was not found locally and could not be downloaded. Put the file at Storage\GeoLite2-Country.mmdb or rerun build.ps1 with -GeoLite2CountryMmdbPath <file>.'
	exit 1
}

$MSBuildPath = Resolve-MSBuild

if ( Test-Path -Path $OutputPath ) {
    rm -Recurse -Force $OutputPath
}
New-Item -ItemType Directory -Name $OutputPath | Out-Null

Push-Location $OutputPath
New-Item -ItemType Directory -Name 'bin'  | Out-Null
cp -Recurse -Force '..\Storage\i18n' '.'  | Out-Null
cp -Recurse -Force '..\Storage\mode' '.'  | Out-Null
cp -Recurse -Force '..\Storage\stun.txt' 'bin'  | Out-Null
cp -Recurse -Force '..\Storage\nfdriver.sys' 'bin'  | Out-Null
cp -Recurse -Force '..\Storage\aiodns.conf' 'bin'  | Out-Null
Copy-GeoLiteDatabase -DestinationPath 'bin\GeoLite2-Country.mmdb'
cp -Recurse -Force '..\Storage\tun2socks.bin' 'bin'  | Out-Null
cp -Recurse -Force '..\Storage\README.md' 'bin'  | Out-Null
Pop-Location

if ( -Not ( Test-Path '.\Other\release' ) ) {
	.\Other\build.ps1
	if ( -Not $? ) {
		exit $lastExitCode
	}
}
cp -Force '.\Other\release\*.bin' "$OutputPath\bin"
cp -Force '.\Other\release\*.dll' "$OutputPath\bin"
cp -Force '.\Other\release\*.exe' "$OutputPath\bin"

if ( -Not ( Test-Path ".\Netch\bin\$Configuration" ) ) {
	Write-Host
	Write-Host 'Building Netch'

	dotnet publish `
		-c $Configuration `
		-r 'win-x64' `
		-p:Platform='x64' `
		-p:SelfContained=$SelfContained `
		-p:PublishTrimmed=$PublishReadyToRun `
		-p:PublishSingleFile=$PublishSingleFile `
		-p:PublishReadyToRun=$PublishReadyToRun `
		-p:PublishReadyToRunShowWarnings=$PublishReadyToRun `
		-p:IncludeNativeLibrariesForSelfExtract=$SelfContained `
		-o ".\Netch\bin\$Configuration" `
		'.\Netch\Netch.csproj'
	if ( -Not $? ) { exit $lastExitCode }
}
cp -Force ".\Netch\bin\$Configuration\Netch.exe" $OutputPath

if ( -Not ( Test-Path ".\Redirector\bin\$Configuration" ) ) {
	Write-Host
	Write-Host 'Building Redirector'

	& $MSBuildPath `
		-property:Configuration=$Configuration `
		-property:Platform=x64 `
		'.\Redirector\Redirector.vcxproj'
	if ( -Not $? ) { exit $lastExitCode }
}
cp -Force ".\Redirector\bin\$Configuration\nfapi.dll"      "$OutputPath\bin"
cp -Force ".\Redirector\bin\$Configuration\Redirector.bin" "$OutputPath\bin"

if ( -Not ( Test-Path ".\RouteHelper\bin\$Configuration" ) ) {
	Write-Host
	Write-Host 'Building RouteHelper'

	& $MSBuildPath `
		-property:Configuration=$Configuration `
		-property:Platform=x64 `
		'.\RouteHelper\RouteHelper.vcxproj'
	if ( -Not $? ) { exit $lastExitCode }
}
cp -Force ".\RouteHelper\bin\$Configuration\RouteHelper.bin" "$OutputPath\bin"

if ( $Configuration.Equals('Release') ) {
	rm -Force "$OutputPath\*.pdb"
	rm -Force "$OutputPath\*.xml"
}

Pop-Location
exit 0

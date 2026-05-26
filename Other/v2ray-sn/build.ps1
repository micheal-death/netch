param (
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]
    $XrayRef = 'v26.3.27',

    [Parameter()]
    [string]
    $GoProxy = $Env:GOPROXY
)

Set-Location (Split-Path $MyInvocation.MyCommand.Path -Parent)

New-Item -ItemType Directory -Force -Path '..\release' | Out-Null
$refMarker = 'src\.xray-ref'

$goCommand = Get-Command go -ErrorAction SilentlyContinue
if ( -Not $goCommand ) {
    $defaultGo = 'C:\Program Files\Go\bin\go.exe'
    if ( Test-Path $defaultGo ) {
        $Env:Path = "$(Split-Path $defaultGo -Parent);$Env:Path"
    }
    else {
        Write-Error 'Go toolchain was not found in PATH. Install Go or add go.exe to PATH, then rerun this script.'
        exit 1
    }
}

if ( Test-Path 'src' ) {
    if ( -Not ( Test-Path 'src\.git' ) ) {
        Write-Error 'The src directory already exists but is not a git checkout. Remove Other\v2ray-sn\src or run Other\build.ps1 for a clean rebuild.'
        exit 1
    }

    $currentRef = if ( Test-Path $refMarker ) { (Get-Content $refMarker -Raw).Trim() } else { '' }
    if ( $currentRef -ne $XrayRef ) {
        Write-Host "Refreshing Xray-core source for $XrayRef"
        Remove-Item -Recurse -Force 'src'
    }
    else {
        Write-Host 'Using existing Xray-core source in Other\v2ray-sn\src'
    }
}

if ( -Not ( Test-Path 'src' ) ) {
    git clone https://github.com/XTLS/Xray-core.git --branch $XrayRef --single-branch --depth 1 src
    if ( -Not $? ) {
        exit $lastExitCode
    }

    Set-Content -Path $refMarker -Value $XrayRef
}
Set-Location src

$Env:CGO_ENABLED='0'
$Env:GOROOT_FINAL='/usr'

$Env:GOOS='windows'
$Env:GOARCH='amd64'
if ( -Not [string]::IsNullOrWhiteSpace($GoProxy) ) {
    $Env:GOPROXY = $GoProxy
}

go mod download
if ( -Not $? ) {
    exit $lastExitCode
}

go build -o '..\..\release\xray.exe' -trimpath -buildvcs=false -ldflags '-s -w -buildid=' '.\main'
exit $lastExitCode

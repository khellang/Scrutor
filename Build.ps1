function Install-Dotnet
{
  & where.exe dotnet 2>&1 | Out-Null

  if(($LASTEXITCODE -ne 0) -Or ((Test-Path Env:\APPVEYOR) -eq $true))
  {
    Write-Host "Dotnet CLI not found - downloading latest version"
   
    # Download the dotnet CLI install script
    if (!(Test-Path .\dotnet\install.ps1))
    {
      Write-Host "Downloading dotnet CLI install script"
      Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile "./.dotnet/dotnet-install.ps1"
    }

    # Skip all the extra work
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "true"

    # Run the dotnet CLI install
    ./.dotnet/dotnet-install.ps1 -Version "3.1.22"
    ./.dotnet/dotnet-install.ps1 -Version "5.0.13"
    ./.dotnet/dotnet-install.ps1 -Version "6.0.1"
  }
}

function Restore-Packages
{
    param([string] $DirectoryName)
    & dotnet restore -v minimal ("""" + $DirectoryName + """")
    if($LASTEXITCODE -ne 0) { exit 1 }
}

function Test-Project
{
    param([string] $ProjectPath)
    & dotnet test -v minimal -c Release ("""" + $ProjectPath + """")
    if($LASTEXITCODE -ne 0) { exit 1 }
}

function Pack-Project
{
    param([string] $ProjectPath)
    & dotnet pack -v minimal -c Release --output packages ("""" + $ProjectPath + """")
    if($LASTEXITCODE -ne 0) { exit 1 }
}

########################
# THE BUILD!
########################

Push-Location $PSScriptRoot

# Install Dotnet CLI
Install-Dotnet

# Package restore
Get-ChildItem -Path . -Filter *.csproj -Recurse | ForEach-Object { Restore-Packages $_.DirectoryName }

# Tests
Get-ChildItem -Path .\test -Filter *.csproj -Recurse | ForEach-Object { Test-Project $_.FullName }

# Pack
Get-ChildItem -Path .\src -Filter *.csproj -Recurse | ForEach-Object { Pack-Project $_.FullName }

Pop-Location

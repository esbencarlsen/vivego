Remove-Item ".\src\*\bin\Release\*.nupkg"

dotnet build -c Release

if ($LastExitCode -ne 0) {
    Write-Host "Build Failed, stopping" -ForegroundColor red
    return;
}

dotnet test -c Release --no-build --filter Category!=IntegrationTest
if ($LastExitCode -ne 0) {
    Write-Host "Tests Failed, stopping" -ForegroundColor red
    return;
}

#dotnet user-secrets set vivego-nuget-key <key> --id vivego
$secrets = dotnet user-secrets list --id vivego
$Dictionary = @{}
# Split secrets into pairs
$secrets.Split('|') | ForEach-Object {
    # Split each pair into key and value
    $key,$value = $_.Split('=')
    # Populate $Dictionary
    $Dictionary[$key.Trim()] = $value.Trim()
}

$vivegonugetkey = $Dictionary["vivego-nuget-key"]
dotnet nuget push ".\src\*\bin\Release\*.nupkg" -k $vivegonugetkey --source https://www.myget.org/F/vivego/api/v2/package

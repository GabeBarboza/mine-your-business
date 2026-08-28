param(
    [string]$GodotBin = $env:GODOT_BIN
)

$ErrorActionPreference = "Stop"
$repositoryPath = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

if ([string]::IsNullOrWhiteSpace($GodotBin)) {
    $portableGodot = Get-ChildItem -LiteralPath (Join-Path $repositoryPath "tools/godot-4.7.2") `
        -Filter "Godot*_console.exe" -File -Recurse -ErrorAction SilentlyContinue |
        Select-Object -First 1

    if ($null -ne $portableGodot) {
        $GodotBin = $portableGodot.FullName
    }
}

Push-Location $repositoryPath
try {
    dotnet restore MineYourBusiness.sln
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

    dotnet build MineYourBusiness.sln --configuration Debug --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }

    dotnet test --project tests/MineYourBusiness.Domain.Tests/MineYourBusiness.Domain.Tests.csproj --configuration Debug --no-build --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet test failed." }

    dotnet format MineYourBusiness.sln --verify-no-changes --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet format verification failed." }

    if ([string]::IsNullOrWhiteSpace($GodotBin)) {
        Write-Warning "GODOT_BIN is not set; skipping the headless Godot smoke test."
    }
    elseif (-not (Test-Path -LiteralPath $GodotBin -PathType Leaf)) {
        throw "Godot executable was not found at '$GodotBin'."
    }
    else {
        & $GodotBin --headless --path $repositoryPath --quit-after 5
        if ($LASTEXITCODE -ne 0) { throw "Godot scene smoke test failed." }
    }
}
finally {
    Pop-Location
}

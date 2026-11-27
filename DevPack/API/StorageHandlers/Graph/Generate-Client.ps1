$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$previousTelemetry = $env:KIOTA_CLI_TELEMETRY_OPTOUT
$previousOffline = $env:KIOTA_OFFLINE_ENABLED
try {
    $env:KIOTA_CLI_TELEMETRY_OPTOUT = 'true'
    $env:KIOTA_OFFLINE_ENABLED = 'true'
    Push-Location $root
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Kiota tool restore failed.' }
    dotnet tool run kiota generate `
        --openapi 'DevPack\API\StorageHandlers\Graph\documenthub.openapi.yaml' `
        --language CSharp `
        --class-name DocumentHubGraphClient `
        --namespace-name Skyline.DataMiner.Solutions.DocumentHub.API.StorageHandlers.Graph.Generated `
        --output 'DevPack\API\StorageHandlers\Graph\Generated' `
        --type-access-modifier Internal `
        --serializer Microsoft.Kiota.Serialization.Json.JsonSerializationWriterFactory `
        --deserializer Microsoft.Kiota.Serialization.Json.JsonParseNodeFactory `
        --exclude-backward-compatible `
        --clean-output
    if ($LASTEXITCODE -ne 0) { throw 'Kiota client generation failed.' }
}
finally {
    Pop-Location
    $env:KIOTA_CLI_TELEMETRY_OPTOUT = $previousTelemetry
    $env:KIOTA_OFFLINE_ENABLED = $previousOffline
}

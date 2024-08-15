# IoTBridge

IoT edge gateway to tranform data from sensors to 

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

## Testing

The following string is a test for the DeviceBridgeController
```
http://localhost:9021/LEV2/telemetry?id=2
```

## HTTP

Make sure the Controller expects the correct Request method (https://www.roundthecode.com/dotnet/asp-net-core-web-api/why-asp-net-core-frombody-not-working-returning-null):
* [FromBody] application/json encoding
* [FromQuery] query string

## Configuration in Startup.cs
the local configuration can be found in the *appsettings.json* which will be copied in the binary folder on build.
E.g. when compiling with 
`dotnet build --configuration Staging`
the appsettings.Staging.json will be copied as appsettings.json in the output folder

## Feature Request

* Migrate to Azure.Identity to access KeyVault (https://learn.microsoft.com/en-us/dotnet/api/overview/azure/app-auth-migration?view=azure-dotnet)
* Asnyc values from KeyVault
* ConcurrentDictionary for Devices

## Migration to Azure.Security.KeyVault.Secrets
https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/keyvault/Azure.Security.KeyVault.Secrets/MigrationGuide.md

## Create VM
* West Europe
* Naming convention horeich-{number}
* Create virtual network horeich-{number}-vnet

## Check build in types
These types are supported for telemetry data
https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types

## Coniguration
ASPNETCORE_ENVIRONMENT
preserve newest etc pp
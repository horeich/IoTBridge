# IoTBridge

## Testing

The following string is a test for the DeviceBridgeController
```
http://localhost:9021/LEV2/telemetry?id=2
```

## HTTP

Make sure the Controller expects the correct Request method (https://www.roundthecode.com/dotnet/asp-net-core-web-api/why-asp-net-core-frombody-not-working-returning-null):
* [FromBody] application/json encoding
* [FromQuery] query string



## Feature Request

* Migrate to Azure.Identity to access KeyVault (https://learn.microsoft.com/en-us/dotnet/api/overview/azure/app-auth-migration?view=azure-dotnet)
* Asnyc values from KeyVault
* ConcurrentDictionary for Devices
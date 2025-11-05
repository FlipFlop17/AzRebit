<div align="center">
  
  <img src="./resources/azrebit-logo.png" alt="Azure Functions Resubmit Logo" width="250"/>
  
  <h1 style="font-size: 3.5em; margin: 0.2em 0;">AzRebit</h1>
  
  ### 🔄 A powerful NuGet package for Azure Functions request resubmission

  *Easily integrate request resubmission capability into your Azure Functions applications with minimal configuration*
  
</div>

---

## Overview

This package adds a `/resubmit` HTTP endpoint to your Azure Functions application, allowing you to programmatically trigger resubmission of failed or pending function executions. Perfect for implementing retry logic, handling transient failures, or integrating with external monitoring and alerting systems.

## Features

- **Automatic Function Discovery** - Automatically discovers and catalogs all functions in your application
- **Authentication** - Endpoint being added is again an azure function which is using the built-in auth via Function.Key
- **Simple HTTP Interface** - RESTful endpoint for triggering resubmissions
- **Invocation Tracking** - Track resubmissions using invocation IDs
- **Isolated Worker Compatible** - Seamless integration with Azure Functions isolated worker model
- **Zero Configuration** - Works out of the box with sensible defaults
- **Configurable** - Customize storage cleanup days etc.

## Requirements

- .NET 8 or higher
- Azure Functions Worker SDK v1.0 or higher
- Azure Functions isolated worker model

## Installation

Install the NuGet package:

```
dotnet add package AzFunctionResubmit
```

Or via Package Manager Console:

```
Install-Package AzFunctionResubmit
```

## Quick Start

To configure AzFunctionResubmit configure `AddResubmitEndpoint();`
and
`UseResubmitMiddleware();`

### Configure in Program.cs

```csharp
using AzFunctionResubmit;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

//just add without configuration:
builder.AddResubmitEndpoint();
//optionally add this if you want to configure
builder.AddResubmitEndpoint(options =>
{
    options.DaysToKeepRequests = "10"; // function will automatically delete stored request to save on storage space: defaults to "3"
    options.AddCleanUpFunction= false; //enable the automatic cleanup function if you dont want to handle cleanup on your own. defaults to false
});


//then add the middleware
builder.UseResubmitMiddleware();

builder.Build().Run();
```

Currently supported trigger attributes are:

- `HttpTrigger` --> saves incoming to `http-resubmits`
- `BlobTrigger` -->saves incoming to `blob-resubmits`

After configuring all functions with listed trigger atributes will automatically save incoming requests to a storage account of the function app.

### Recommendation

When enabled you can add the cleanup function that is using `DaysToKeepRequests` counter after which it will clean up requests older than specified days. 
>We recommed to enable this only if you are not handling cleanup on your own which is suggested.  

Since the resubmition is best used just for failed requests, keeping successfull runs might increase storage size. You can delete the saved request within your function by using the provided `DeleteSavedBlobAsync()` method in case of a successfull execution.

```csharp
//optional - delete the save request. Usually you would want this iy your function runned successfuly
await AzRebitBlobExtensions.DeleteSavedBlobAsync(funcContext.InvocationId.ToString());
```

Additionaly you can setup a Lifecycle Management policy on your storage account to automatically delete blobs older than a certain number of days.

## How It Works

1. **Function Discovery** - On startup, the package automatically scans assemblies and discovers all methods decorated with the `[Function]` attribute
2. **Registration** - Discovered function names alonside its trigger data are cached
3. **Middleware** - Before your function starts a middleware will save the incoming request to a local storage account and tag it with the unique (InvocationId) derived the function context.
4. **Resubmit Handling** - Incoming requests to `/resubmit` are crossreferenced with the triggers we support and accordingly the payload is pulled from its saved location and sent again to the Function's trigger.

## Making a Resubmit Request

Making a resubmit request depends on two important parameters:
**functionName** and \***\*invocationId**

`functionName` --> Name of Azure function as defined inside `[Function()]`  
`invocationId` -->Each function run has a uniqueuId which the package is using to tag and locate the payload to resubmit.
To locate what is the unique id of you run you need to inspect your logs and search for InvocationId. [InvocationIdProperty](https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.functions.worker.functioncontext.invocationid?view=azure-dotnet)

```bash
curl -X POST "http://localhost:7071/api/resubmit?functionName=MyFunction&invocationId=abc-123-def-456" \
```

## API Reference

### Endpoint

**POST** `/resubmit`

### Query Parameters

| Parameter      | Required | Description                                         |
| -------------- | -------- | --------------------------------------------------- |
| `functionName` | Yes      | Name of the Azure Function to resubmit              |
| `invocationId` | Yes      | Unique identifier to fetch the resubmission context |

### Responses

#### 202 Accepted

Resubmit request successfully queued.

```json
{
  "message": "Resubmit request queued for function 'MyFunction'",
  "functionName": "MyFunction",
  "invocationId": "abc-123-def-456",
  "timestamp": "2025-01-01T12:00:00Z"
}
```

#### 400 Bad Request

Missing required query parameters.

```json
{
  "error": "Missing required query parameter: functionName"
}
```

#### 401 Unauthorized

Invalid or missing API key.

```json
{
  "error": "Unauthorized: Invalid or missing API key"
}
```

#### 404 Not Found

Function does not exist in the application.

```json
{
  "error": "Function 'MyFunction' not found"
}
```

#### 500 Internal Server Error

Server error occurred while processing the request.

```json
{
  "error": "Internal server error"
}
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, feature requests, or questions, please open an issue on [GitHub](https://github.com/FlipFlop17/azure-functions-resubmit-endpoint/issues).

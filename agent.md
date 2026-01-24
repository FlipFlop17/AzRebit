# AzRebit Agent Documentation

## Overview

AzRebit is a .NET library that simplifies Azure Function run resubmission by providing extension methods for saving, tracking, and recovering processing operations. It automatically captures failed function executions and allows manual resubmission through a built-in HTTP endpoint.

## Purpose

Azure Functions can fail for various reasons (transient errors, external service issues, etc.). Instead of losing data or having to manually recreate the function execution context, AzRebit:

1. **Automatically captures** failed function payloads and context
2. **Stores them persistently** in Azure Blob Storage
3. **Provides a resubmit endpoint** to retry failed functions
4. **Supports all trigger types** (Timer, Queue, Blob, Http)

## Quick Start

### 1. Installation
```xml
<PackageReference Include="AzRebit" Version="2.0.7-alpha" />
```

### 2. Setup in Program.cs
```csharp
using AzRebit;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// Add your services...
builder.Services.AddApplicationInsightsTelemetryWorkerService();

// Add AzRebit resubmit endpoint
builder.AddResubmitEndpoint();

builder.Build().Run();
```

### 3. Your Azure Functions (No Changes Required!)
```csharp
public class MyFunction
{
    [Function("MyFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        // Your function logic here
        // If it fails, AzRebit automatically saves the payload!
        throw new Exception("This will be captured for resubmission");
        
        return new OkObjectResult("Success");
    }
}
```

## How It Works

### 1. Automatic Function Discovery
AzRebit automatically discovers all Azure Functions in your application by scanning for:
- Classes with methods decorated with `[Function("Name")]` attribute
- Methods with Azure Function trigger attributes (`[HttpTrigger]`, `[TimerTrigger]`, etc.)

### 2. Middleware Capture
When a function fails:
1. **SavePayloadsMiddleware** intercepts the request
2. Extracts function parameters and context
3. Serializes the payload
4. Stores in Azure Blob Storage with unique invocation ID

### 3. Storage Structure
```
AzureWebJobsStorage
├── azure-webjobs-functions
    └── FunctionName
        └── InvocationId
            └── function-data.json (contains all function context)
```

### 4. Manual Resubmission
Access the built-in resubmit endpoint:
- **URL**: `http://your-function-app-url/api/resubmit`
- **Method**: POST
- **Body**: `{"invocationId": "your-failed-invocation-id"}`

## Key Components

### Core Classes

#### `ResubmitFunctionWorkerExtension`
Main extension class that configures the entire resubmit pipeline:
```csharp
builder.AddResubmitEndpoint(options => 
{
    options.ExcludedFunctionNames.Add("InternalFunction"); // Optional exclusion
});
```

#### `AssemblyDiscovery`
Automatically discovers Azure Functions by:
- Scanning assemblies for `[Function]` attributes
- Resolving trigger types (Http, Timer, Queue, Blob)
- Building function metadata for later resubmission

#### `SavePayloadsMiddleware`
Middleware that intercepts all function executions:
- Skips internal "Resubmit" endpoint to prevent recursion
- Matches trigger types to appropriate handlers
- Saves payloads to blob storage on failure

#### `RebitResult<T>`
Result pattern for success/failure handling:
```csharp
public static RebitResult<MyType> Success(MyType data) => ...;
public static RebitResult<MyType> Failure(string message) => ...;
public static RebitResult<MyType> Failure<T>(string message) => ...; // Generic
```

### Storage Components

#### `BlobResubmitStorage`
Default storage implementation using Azure Blob Storage:
- Stores function payloads as JSON
- Uses invocation ID as blob name
- Stores under `azure-webjobs-functions/{FunctionName}/` path

#### `RebitStoreOperations`
High-level operations for manual payload management:
```csharp
public interface IRebitStoreOperations
{
    Task<RebitResult> SavePayloadForResubmit(object payload, string functionName, string id);
    Task<RebitResult> DeleteResubmitFile(string invocationId);
    Task<RebitResult<object>> LoadResubmitPayload(string invocationId);
}
```

### Handler Components

#### Trigger-Specific Handlers
Each trigger type has its own payload handler:
- `HttpSavePayloadHandler` - Captures HTTP request data
- `QueueSavePayloadHandler` - Captures queue message content
- `TimerSavePayloadHandler` - Captures timer context
- `BlobSavePayloadHandler` - Captures blob trigger data

## Advanced Usage

### Manual Payload Saving
Sometimes you want explicit control over when payloads are saved:

```csharp
public class MyTimerFunction
{
    private readonly IRebitStoreOperations _rebit;
    
    [Function("MyTimerFunction")]
    public async Task<IActionResult> RunTimer(
        [TimerTrigger("0 * * * * *")] TimerInfo timer)
    {
        var myData = ProcessBusinessLogic();
        
        // Explicitly save for resubmission
        var saveResult = await _rebit.SavePayloadForResubmit(
            payload: myData,
            functionName: "MyTimerFunction", 
            id: Guid.NewGuid().ToString());
            
        if (!saveResult.IsSuccess)
            _logger.LogWarning("Failed to save payload: {Message}", saveResult.Message);
            
        return new OkObjectResult("Processed");
    }
}
```

### Timer Resubmission (ITimerResubmit)
For timer triggers, you can implement a special interface for automatic resubmission:

```csharp
public interface ITimerResubmit
{
    Task<RebitResult<object>> HandleTimerTriggerAsync<T>(T payload);
}

public class MyTimerHandler : ITimerResubmit
{
    [Function("MyTimerFunction")]
    public async Task<IActionResult> RunTimer(
        [TimerTrigger("0 * * * * *")] TimerInfo timer)
    {
        // Function logic
        return new OkObjectResult("Success");
    }
    
    // This method will be called during resubmission
    public async Task<RebitResult<object>> HandleTimerTriggerAsync<T>(T payload)
    {
        // Handle the resubmitted payload
        return RebitResult<object>.Success(null);
    }
}
```

### Result Pattern Usage
The library uses a consistent result pattern:

```csharp
public async Task<RebitResult<MyResponse>> ProcessData(MyData data)
{
    try
    {
        var result = await _service.ProcessAsync(data);
        return RebitResult<MyResponse>.Success(result);
    }
    catch (Exception ex)
    {
        return RebitResult<MyResponse>.Failure(ex.Message, AzRebitErrorType.UnexpectedError);
    }
}
```

## Configuration

### Connection Strings
AzRebit requires Azure Storage connection string:
```json
{
  "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
}
```

### Optional Configuration
```csharp
builder.AddResubmitEndpoint(options => 
{
    options.ExcludedFunctionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "InternalFunction",
        "HealthCheck"
    };
});
```

### Environment Variables
- `AZREBIT_DELETE_RESUBMITION_FILE`: Set to `"true"` to automatically delete successfully processed files

## Resubmit Endpoint

### Built-in Endpoint
When you call `AddResubmitEndpoint()`, AzRebit adds an HTTP function endpoint:

- **Function Name**: `Resubmit`
- **Route**: `/api/resubmit`
- **Method**: POST

### Usage
```bash
curl -X POST https://your-function-app.azurewebsites.net/api/resubmit \
  -H "Content-Type: application/json" \
  -d '{"invocationId": "your-failed-invocation-id"}'
```

### Response
```json
{
  "IsSuccess": true,
  "Message": "Function resubmitted successfully",
  "Data": null
}
```

## Supported Trigger Types

| Trigger Type | Status | Handler | Notes |
|-------------|--------|---------|-------|
| HTTP | ✅ Complete | `HttpSavePayloadHandler` | Captures request body, headers, query params |
| Timer | ✅ Complete | `TimerSavePayloadHandler` | Captures timer info and context |
| Queue | ✅ Complete | `QueueSavePayloadHandler` | Captures queue message content |
| Blob | ✅ Complete | `BlobSavePayloadHandler` | Captures blob trigger data |
| Event Grid | 🔄 Planned | TBD | Future release |
| Service Bus | 🔄 Planned | TBD | Future release |

## Best Practices

### 1. Function Naming
Use descriptive function names in `[Function("MyDescriptiveName")]` - these are used for storage organization and resubmission matching.

### 2. Payload Serialization
Ensure your function parameters and return types are JSON-serializable for proper capture and resubmission.

### 3. Error Handling
Use the `RebitResult<T>` pattern consistently:
```csharp
if (!result.IsSuccess)
{
    // Log error but don't throw - let AzRebit capture the failure
    _logger.LogError("Processing failed: {Message}", result.Message);
    return new BadRequestObjectResult(result.Message);
}
```

### 4. Storage Cleanup
Enable automatic cleanup for successfully processed files:
```csharp
bool deleteResubmitionFile = Environment.GetEnvironmentVariable("AZREBIT_DELETE_RESUBMITION_FILE") == "true";
if (deleteResubmitionFile && result.IsSuccess)
{
    await _rebit.DeleteResubmitFile(invocationId);
}
```

### 5. Logging
Use structured logging to track resubmission operations:
```csharp
_logger.LogInformation("Function {FunctionName} failed with invocation {InvocationId}. Saved for resubmission.",
    context.FunctionDefinition.Name, context.InvocationId);
```

## Troubleshooting

### Common Issues

#### 1. Function Not Being Discovered
- Ensure function method has `[Function("Name")]` attribute
- Check that trigger attribute is present (`[HttpTrigger]`, `[TimerTrigger]`, etc.)
- Verify function is public and not generic

#### 2. Payload Not Being Saved
- Check Azure Storage connection string
- Verify SavePayloadsMiddleware is registered
- Check logs for middleware errors

#### 3. Resubmission Fails
- Ensure function parameter types match original invocation
- Check that resubmitted function doesn't depend on external state
- Verify function has proper error handling

#### 4. Storage Costs
- Monitor blob storage usage
- Enable automatic cleanup with `AZREBIT_DELETE_RESUBMITION_FILE`
- Consider implementing custom cleanup logic

### Debugging
Enable detailed logging:
```csharp
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

Check these log categories:
- `AzRebit.Middleware.SavePayloadsMiddleware`
- `AzRebit.AssemblyDiscovery`
- `AzRebit.Infrastructure.FileStorage`

## Examples

See the `AzRebit.FunctionExample` project for complete working examples:
- `TimerCats.cs` - Timer trigger with manual payload saving
- `QueueCats.cs` - Queue trigger with automatic capture
- `HttpCats.cs` - HTTP trigger with result pattern
- `BlobCats.cs` - Blob trigger with resubmission

## Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Azure Func    │    │  Middleware      │    │   Storage       │
│                 │    │                  │    │                 │
│ [Function("X")] │───▶│ SavePayloads     │───▶│ Blob Storage    │
│                 │    │ Middleware       │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                               │
                               ▼
                       ┌──────────────────┐
                       │   Resubmit       │
                       │   Endpoint       │
                       │                  │
                       │ POST /api/resubmit│
                       └──────────────────┘
```

## Version History

- **v2.0.7-alpha**: Current development version
- **v2.0.6**: Stable release with full trigger support
- **v1.x**: Initial release with basic functionality

## Project Architecture

### Vertical Feature Organization
This project uses a **vertical folder architecture** approach, which organizes code by business features rather than technical layers. This avoids bloated, God-class services and promotes modular, maintainable code.

#### Why Vertical Architecture?
- **Feature encapsulation**: Each feature contains everything it needs (services, handlers, DTOs, etc.)
- **Clean boundaries**: Features are independent and can be developed/modified separately  
- **Better discoverability**: Easy to find related code in one location
- **Reduced coupling**: Features don't depend on each other's internal implementation

#### Folder Structure
```
AzRebit/
├── Domain/
│   ├── Abstractions/           # Core interfaces and contracts
│   ├── Entities/               # Domain models and data structures
│   ├── Enums/                  # Shared enums and constants
│   └── Results/                # Result pattern types
│
├── Features/
│   ├── TimerTriggered/         # Timer trigger specific logic
│   │   ├── ClientInterface/    # ITimerResubmit interface
│   │   ├── TimerResubmitHandler.cs
│   │   └── ...
│   ├── QueueTriggered/         # Queue trigger specific logic
│   ├── HttpTriggered/          # HTTP trigger specific logic
│   └── BlobTriggered/          # Blob trigger specific logic
│
├── Infrastructure/
│   ├── FileStorage/            # Blob storage implementation
│   ├── StateStorage/           # State persistence logic
│   └── ...
│
├── Middleware/                 # Cross-cutting middleware
└── Shared/                     # Shared utilities and extensions
```

#### Guidelines for Adding New Features
1. **Create a new feature folder** under `Features/`
2. **Include all related code** (handlers, services, DTOs, interfaces)
3. **Avoid cross-feature dependencies** - use `Domain.Abstractions` for shared contracts
4. **Register services through interfaces** in `AssemblyDiscovery.AddAllAzRebitServices()`

## Contributing

The codebase is organized as follows:
- `Domain/` - Core abstractions and result types
- `Infrastructure/` - Storage implementations
- `Features/` - Trigger-specific handlers (vertical organization)
- `Middleware/` - Request capture middleware
- `Tests/` - Comprehensive test suite

Each feature folder is self-contained and follows the same internal structure for consistency.

## Support

- **GitHub**: https://github.com/FlipFlop17/AzRebit
- **Issues**: Report bugs and feature requests on GitHub
- **Documentation**: This file and inline XML comments
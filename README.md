
[![Build status](https://toolfactory.visualstudio.com/Core/_apis/build/status/Mectrics%20Libraries/Bluekiri.Diagnostics.Prometheus-CI?branchName=master)](https://toolfactory.visualstudio.com/Core/_build/latest?definitionId=773)

# bluekiri-diagnostics-prometheus
This library is meant to expose DiagnosticSource events as Prometheus metrics using [prometheus-net](https://github.com/prometheus-net/prometheus-net).
Currently, we expose events from the following sources:

 - HttpHandlerDiagnosticListener. This source logs the events of outgoing HTTP connections made with HttpClient.
 - Microsoft.AspNetCore. This source logs events coming from de ASP.NET Core pipeline.

## Getting started

 1. The first step is installing the following NuGet package
    ```
    Install-Package Bluekiri.Diagnostics.Prometheus
    ```
    
 2. In the application entrypoint, preferably in the main method, configure the DiagnosticListeners in the following way:
     ```csharp
     public static void Main(string[] args)
     {
        // Creating the kestrel metrics server listening
        // into another port
        var metricsServer = new KestrelMetricServer(9303);
        metricsServer.Start();

        // Subscribe the diagnostic listeners that will
        // export Prometheus metrics		
        DiagnosticListener.AllListeners.SubscribeDiagnosticListener();

        CreateWebHostBuilder(args).Build().Run();
     }
    ```
 3. Prometheus metrics will be exposed in the /metrics endpoint in the port 9303 of your host, since in this sample we configured a KestrelMetricServer in this port.

### Adding a custom observer for a listener
You can add a custom observer for a specific listener. First, you need to make an implementation of IObserver<string, object>. Finally, you can subscribe this observer to diagnostic listeners in the following way:
```csharp
public static void Main(string[] args)
{
    // Creating the kestrel metrics server listening
    // into another port
    var metricsServer = new KestrelMetricServer(9303);
    metricsServer.Start();

    // Subscribe YourObserver for the specified Diagnostic Listener.
    // This method also adds the default observers added with SubscribeDiagnosticListener() extension method
    DiagnosticListener.AllListeners.SubscribeDiagnosticListener(o => {
        o.AddSubscriber("TheDiagnosticListenerNameYouWantToSubscribeTo", new YourObserver());
    });

    CreateWebHostBuilder(args).Build().Run();
}
```

## References
- [DiagnosticSource User Guide](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md)
- [HttpDiagnostics Guide](https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/HttpDiagnosticsGuide.md)

## Contributing
Your help is always welcome. You can contribute to this project opening issues reporting bugs or new feature requests, or by sending pull requests with new features or bug fixes.

## License
This project is licensed under [MIT License](LICENSE.md)

# Autabee Opc Communicator
The autabee opc communicator is a rapper for the opc ua client c# library. It is made with the intention of making de development of an opc client easier. the whole scope of this repository has 4 parts:
- <b>Autabee Managed Opc Client</b>: The client interface used for communicating with an OPC Server</li>
- <b>Opc Sharper</b>: A script generator for communicating to nodes. For explicit calling of nodes.</li>
- <b>Opc Scout</b>: An example client build on top of Blazor where it either runs in a maui <b>App</b> or via a <b>Wpp</b> client.</li>
- <b>Opc Scout Controls</b>: A set of predefined controls that can be used in a blazor app</li>

## FAQ
### How can I connect to an OPC Server?
in the case that you have an opc ApplicationConfiguration you can use the following code:
```csharp
    var logger = new Logger();
    var client = new OpcClient(GetConfig(),logger);

    await client.Connect("opc.tcp://localhost:4840");
```

Otherwise use the following code:
```csharp
    var logger = new Logger();
    string company = "company that the client is from";
    string product = "name of the client";
    string directory = "directory there the config need to be saved";
    var client = new OpcClient(company,product,directory,logger);
    
    await client.Connect("opc.tcp://localhost:4840");
```


### Why do you use Serilog?
It's a great logging framework that is easy to use and has a lot of options. It's also easy to extend. And since this is the logging tool of choice for the company it originally is made for, we kept it in.
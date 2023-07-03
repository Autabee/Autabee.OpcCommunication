---
layout: default
title: Home
navigation_weight: 1
---

# Autabee Opc Communicator
The autabee opc communicator is a rapper for the opc ua client c# library. It is made with the intention of making de development of an opc client easier. the whole scope of this repository has 4 parts:
- <b>Autabee Managed Opc Client</b>: The client interface used for communicating with an OPC Server
- <b>Opc Sharper</b>: A script generator for communicating to nodes. For explicit calling of nodes.
- <b>Opc Scout</b>: An example client build on top of Blazor where it either runs in a maui <b>App</b> or via a <b>Wpp</b> client. This client is used to test the functionality of the opc communicator, be an example of how to use the client and also as a functional DA client.
- <b>Opc Scout Controls</b>: A set of predefined controls that can be used in a blazor app

## FAQ
### How can I connect to an OPC Server?
in the case that you have an opc ApplicationConfiguration you can use the following code:
```csharp
var logger = new Logger();
var client = new Autabee.Communication.ManagedOpcClient(GetConfig(),logger);

await client.Connect("opc.tcp://localhost:4840");
```

Otherwise use the following code:
```csharp
public async Task<> ConnectToServer()
{
    var logger = new Logger();
    string company = "company that the client is from";
    string product = "name of the client";
    string directory = "directory there the config need to be saved";
    var client = new Autabee.Communication.ManagedOpcClient(company,
                        product,directory,logger);
    
    await client.Connect("opc.tcp://localhost:4840");
    return client;
}
```

### How can I read a node?
There are multiple ways but the 2 main are either via NodeId or NodeEntry.

NodeId in opc is the address of a value on the server. This can easily be called as followed:
```csharp
var result = await client.ReadNode("ns=2;s=Demo.Dynamic.Scalar.Boolean");
var result = await client.ReadNode(new NodeId("ns=2;s=Demo.Dynamic.Scalar.Boolean"));
```

However when you are using a string based address you can request to get a registered node id. using this node for reading is slightly faster when you repeatedly call that node id. This is where NodeEntry comes in. As they store both the unregistered and the registered node id for you. So when you call a node multiple times you can use the registered node id to speed up the process.
These nodes come in the flavors:
    - MethodNode: a node that is used to call a method on the server
    - ValueNode: a node that is used to read or write a value on the server
> **Warning**
> A ValueNodeEntry is for one connection only. There is a reconnect register procedure build in. So when you reconnect it registers a new registered Id. So this mean that if you want to read a node on 2 different connections you need to use 2 ValueNodeEntry's, one or each connection. (with current implementation)

To read a node using a NodeEntry you can use the following example:
```csharp
var entry = new ValueNodeEntry<bool>("ns=2;s=Demo.Dynamic.Scalar.Boolean");
client.RegisterNode(node);

public async Task ReadNode()
{
    var result = await client.ReadNode(entry);
}
```

### How can I subscribe to a node?
First you need to create a subscription. This can be done as followed:
```csharp
var intervalInMilliSeconds = 1000;
var subscription = await client.CreateSubscription(intervalInMilliSeconds);
```

Then you can add a node to the subscription. using either a string, NodeId or NodeEntry. This can be done as followed:
```csharp
await client.AddMonitoredNode(subscription, node, EventHandler);
```
where eventHandler is a function that is called when the value of the node changes. This function has the following signature for string and nodeIds:
```csharp
public void EventHandler(MonitoredItem sender, object e);
{
    // do something with the value
}
```
for NodeEntry's the signature is as followed:
```csharp
public void EventHandler(MonitoredItem sender, NodeValueRecord e)
{
    // do something with the value
}
```
### What is Opc Sharper good for?
In a lot of cases opc sharper is not needed as you have access to a UA-ModelCompiler Model.xml. However, when you don't. For instance, when you have a PLC first development approach the tooling does not generate a model. So to prevent error due to string error. Opc Sharper is made to generate the models instead. Mainly for generating types and methods for getting the correct node addresses.

The sharper is build into the opc scout. So when you are connected to a server. you can use the sharper to generate the code for you. This code can then be used within your own project.


### Why do you use Serilog?
It's a great logging framework that is easy to use and has a lot of options. It's also easy to extend. And since this is the logging tool of choice for the company it originally is made for, we kept it in.

### How do I run Opc Scout?
Easiest way is by running the folowing compose file on the computer you want to run it on:
```yml
version: '3.3'
services:
    opcscout:
        volumes:
            - scoutdata:/app/data
        ports:
            - 8000:80
        image: autabee/opcscout:beta
volumes:
    scoutdata:
```
Or you can run the exe in the opcscout-web.zip downloadable from [here](https://github.com/Autabee/Autabee.OpcCommunication/releases).
Or you can build the App or Web locally.


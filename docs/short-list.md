---
layout: default
title: short-list
navigation_weight: 2
---

# Short Lists

## Object Definitions
Objects defined in the library that are not part of the OPC UA specification but are used to simplify the usage of the library.


| object | usage definition   | 
| --- | --- | 
| [NodeEntry](https://github.com/Autabee/Autabee.OpcCommunication/blob/main/src/ManagedOpcClient/ManagedNode/NodeEntry.cs) | Contains an registered and unregistered node id for a specific session. So you can call an optimized node id, while having access to the unregistered node id. |
| [ValueNodeEntry](https://github.com/Autabee/Autabee.OpcCommunication/blob/main/src/ManagedOpcClient/ManagedNode/ValueNodeEntry.cs) | A NodeEntry that defines an expected datatype. |
| [ValueNodeRecord](https://github.com/Autabee/Autabee.OpcCommunication/blob/main/src/ManagedOpcClient/ManagedNode/ValueNodeRecord.cs) | Contains the value and the origin NodeValueEntry. |
| [MethodNodeEntry](https://github.com/Autabee/Autabee.OpcCommunication/blob/main/src/ManagedOpcClient/ManagedNode/MethodNodeEntry.cs) | A NodeEntry that defines the arguments needed to call a function |
| [MethodArguments](https://github.com/Autabee/Autabee.OpcCommunication/blob/main/src/ManagedOpcClient/ManagedNode/MethodArguments.cs) | Collection of types and argument collections associated to a specific method. |
| [NodeTypeData](https://github.com/Autabee/Autabee.OpcCommunication/blob/main/src/ManagedOpcClient/ManagedNode/NodeTypeData.cs) | Generic class for decoding a read value to a dictionary. |
| [AutabeeManagedOpcClient](https://github.com/Autabee/Autabee.OpcCommunication/blob/main/src/ManagedOpcClient/AutabeeManagedOpcClient.cs)| The main class that is used to communicate with the OPC Server. |

## Methods On ManagedOpcClient

| method | usage definition   |
| --- | --- |
| ReadValue | Reads a single or multiple values from the server and returns it based on the function you are calling |
| WriteValue | Writes a single or multiple values to the server |
| CallMethod | Calls a single or multiple methods on the server and returns their values |
| CreateSubscription | create a subscription that release their values in a amount of milliseconds |
| AddMonitoredItem | add a single or multiple nodes to an exiting subscription |
| RegisterNodeId | Registers a single or multiple nodes for a session |
| UnregisterNodeId | Unregisters a single or multiple nodes for a session |
| ScanNodeExistence | Scans the existence of a single or multiple nodes on the server |
| Scan[Type]NodeExistence | Scans the existence of a single or multiple nodes on the server with a specified node class |


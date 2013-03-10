Active Object
=========

The active object design pattern (decouples method execution from method invocation for objects that each reside in their own thread of control) in .NET

Typically it contains following elements [[Wiki]] 

 - A proxy, which provides an interface towards clients with publicly accessible methods.
 - An interface which defines the method request on an active object.
 - A list of pending requests from clients.
 - A scheduler, which decides which request to execute next.
 - The implementation of the active object method.
 - A callback or variable for the client to receive the result.


 [Wiki]: http://en.wikipedia.org/wiki/Active_object 
 
 Implementation
 -
 
 - `IActiveObject` - An interface for the object
 - `CancellationToken` - A shareable token that can be passed to a active object (even multiple) to send a kill / cancel signal later on.
 - `ActiveObject` - Implementation of the object


            


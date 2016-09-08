# JsonRpc.Router
A .Net Core IRouter implementation for Json Rpc v2 requests for Microsoft.AspNetCore.Routing. (frameworks: net451, netstandard1.3)

The requirements/specifications are all based off of the [Json Rpc 2.0 Specification](http://www.jsonrpc.org/specification)

## Installation
##### NuGet: [JsonRpc.Router](https://www.nuget.org/packages/EdjCase.JsonRpc.Router/)

using nuget command line:
```cs
Install-Package EdjCase.JsonRpc.Router
```

## Usage
Create a ASP.NET Core Web Application, reference this library and in the `Startup` class configure the following:

Add the dependency injected services in the `ConfigureServices` method:
```cs
public void ConfigureServices(IServiceCollection services)
{
	services
    	//Adds default IRpcInvoker, IRpcParser, IRpcCompressor implementations to the services collection.
    	//(Can be overridden by custom implementations if desired)
	    .AddJsonRpc(config =>
				{
					//returns detailed error messages from server to rpcresponses
					config.ShowServerExceptions = true;
				});
}
```

Add the JsonRpc router the pipeline in the `Configure` method:
```cs
public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
{
	app.UseJsonRpc(config =>
	{
		config.RegisterTypeRoute<RpcClass1>(); //Access RpcClass1 public methods at '/'
		config.RegisterTypeRoute<RpcClass2>("Class2"); //Access RpcClass2 public methods at '/Class2'
		
		//Or use manual RouteCriteria registration (only uses types right now but will have more features in the future)
		List<Type> types = new List<Type> { typeof(RpcClass1), typeof(RpcClass2) };
		RouteCriteria criteria = new RouteCriteria(types);
		config.RegisterRoute(criteria, "CriteriaRoute") //Access RpcClass1 and RpcClass2 from '/CriteriaRoute'
	});
}
```
also, you can set JsonSerializerSettings via `SetJsonSerializerSettings` method.

## Examples of Rpc Classes:

For all frameworks:
```cs
//Classes can be named anything and be located anywhere in the project/solution
//The way to associate them to the api is to use the RegisterClassToRpcRoute<T> method in
//the configuration
public class RpcClass1
{
    //Accessable to api at /{OptionalRoutePrefix}/{OptionalRoute}/Add 
    //e.g. (from previous example) /RpcApi/Add or /RpcApi/Class2/Add
    //Example request using param list: {"jsonrpc": "2.0", "id": 1, "method": "Add", "params": [1,2]}
    //Example request using param map: {"jsonrpc": "2.0", "id": 1, "method": "Add", "params": {"a": 1, "b": 2}}
    //Example response from a=1, b=2: {"jsonrpc", "2.0", "id": 1, "result": 3}
    public int Add(int a, int b)
    {
        return a + b;
    }
    
    //This method would use the same request as Add(int a, int b) (except method would be 'AddAsync') 
    //and would respond with the same response
    public async Task<int> AddAsync(int a, int b)
    {
        //async adding here
    }
    
    //Can't be called/will return MethodNotFound because it is private. Same with all non-public/static methods.
    private void Hidden1()
    {
    }
    
    //Will return a success response or an error response dependening on the if statement
    // (See IRpcMethodResult usage below)
    public IRpcMethodResult CustomResult()
    {
        if(/*something is invalid*/)
        {
            return this.Error(customErrorCode, errorMesssage); //Or return new RpcMethodErrorResult(customErrorCode, errorMessage);
        }
        return this.Ok(optionalReturnObject);//Or return new RpcMethodSuccessResult(optionalReturnObject);
    }
}
```

For netstadard 1.6+ and full .net framework only:
```cs
[RpcRoute("TestMethods")] //Optional, if not specified the route name would be 'Test' (based off the controller type name)
public class TestController : RpcController
{
	public int Add(int a, int b)
	{
		return a + b;
	}
}
```

Any method in the registered class that is a public instance method will be accessable through the Json Rpc Api.

The controllers and manual registration CAN be used at the same time. Mix and match as needed.

## Custom Rpc Responses
In order to specify different types of responses (such as errors and successful result objects) in the same method `IRpcMethodResult` can be used as a return type. If the router detects the returned object is a `IRpcMethodResult` then it will call the `ToRpcReponse(...)` method and use that as the response. The default implementations are for simple error and success routes. The `RpcMethodErrorResult` will use the error code, message and data to create an error response. The `RpcMethodSuccessResult` will use the optional return object to create a successful response.

Any custom implementation of the `IRpcMethodResult` can be used for application specific purposes. One common use may be to unify the custom rpc error codes that one specific application uses.

There are two helper methods in the RpcController class: `this.Ok(obj)` and `this.Error(code, message, data)`. They are just wrappers around the implementations of `IRpcMethodResult`. So `return this.Ok(obj);` is equivalent to `return new RpcMethodSuccessResult(obj)`.

## Misc

Bulk requests are supported (as specificed in JsonRpc 2.0 docs) and will all be run asynchronously. The responses may be in a different order than the requests.

On specifics on how to create requests and what to expect from responses, use the [Json Rpc 2.0 Specification](http://www.jsonrpc.org/specification).

## Contributions

Contributions welcome. Fork as much as you want. All pull requests will be considered.

Best way to develop is to use Visual Studio 2015+ or Visual Studio Code on other platforms besides windows.

Also the correct dnx runtime has to be installed if visual studio does not automatically do that for you. 
Information on that can be found at the [Asp.Net Repo](https://github.com/aspnet/Home).

Note: I am picky about styling/readability of the code. Try to keep it similar to the current format. 

## Feedback
If you do not want to contribute directly, feel free to do bug/feature requests through github or send me and email [Gekctek@Gmail.com](mailto:Gekctek@Gmail.com)

### Todos

 - Better sample app
 - Performance testing

License
----
[MIT](https://raw.githubusercontent.com/Gekctek/JsonRpc.Router/master/LICENSE)

# JsonRpc.Router
A .NetStandard 2.0 IRouter implementation for Json Rpc v2 requests for Microsoft.AspNetCore.Routing.

The requirements/specifications are all based off of the [Json Rpc 2.0 Specification](http://www.jsonrpc.org/specification)

## Installation
##### NuGet: [JsonRpc.Router](https://www.nuget.org/packages/EdjCase.JsonRpc.Router/)

dotnet CLI:
```cs
dotnet add package EdjCase.JsonRpc.Router
```
Nuget CLI:
```cs
Install-Package EdjCase.JsonRpc.Router
```

## Usage

### **Minimum config**

Create a ASP.NET Core Web Application, reference this library and in the `Startup` class configure the following:

Add the dependency injected services in the `ConfigureServices` method:
```cs
public void ConfigureServices(IServiceCollection services)
{
	services.AddJsonRpc();
}
```

Add the JsonRpc router the pipeline in the `Configure` method:
```cs
public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
{
	app.UseJsonRpc();
}
```
Add a RpcController class with public methods:
```cs
public class ItemsController : RpcController
{
	public Item Get(int id)
    {
    	//Gets item with id
    }
    
    public void Add(Item item)
    {
    	//Adds item
    }
}
```
Thats it! The library will auto detect the controllers that are child classes of `RpcController` and will expose all public methods to the api. The url route in this case will be '/Items' because the controller name is '_Items_'Controller. (If the controller does not end with 'Controller' the route will be the class name)


### **Advanced Config**

Create a ASP.NET Core Web Application, reference this library and in the `Startup` class configure the following:

Add the dependency injected services in the `ConfigureServices` method:
```cs
public void ConfigureServices(IServiceCollection services)
{
 	services.AddJsonRpc(options =>
    {
    	//(Optional) Hard cap on batch size, will block requests will larger sizes, defaults to no limit
    	options.BatchRequestLimit = 5;
        //(Optional) If true returns full error messages in response, defaults to false
		options.ShowServerExceptions = false;
        //(Optional) Configure how the router serializes requests
		options.JsonSerializerSettings = new Newtonsoft.Json.JsonSerializerSettings
        {
        	//Example json config
			NullValueHandling = Newtonsoft.Json.NullValueHandling.Include,
			Formatting = Newtonsoft.Json.Formatting.Indented,
			DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Include
		};
	});	
}
```
There are currently 3 methods for adding JSONRpc middleware in the `Configure` method
1. Auto Detection (default)

```cs
public void Configure(IApplicationBuilder app)
{
	app
    	//(Optional) Adding authentication (need to add AddAuthentication() in the `ConfigureServices` method)
		.UseAuthentication()
		.UseJsonRpc(options =>
        {
        	//(Optional) Overriding base class for all rpc controllers to be detected from, defaults to `RpcController`
			options.BaseControllerType = typeof(MyCustomControllerBase);
            //(Optional) Overring base url, defaults to "/" ("/Items" would now be "/api/Items")
			options.BaseRequestPath = "/api";
		});
}
```
2. Manual mapping (no auto detection)

```cs
public void Configure(IApplicationBuilder app)
{
	app
    	//(Optional) Adding authentication (need to add AddAuthentication() in the `ConfigureServices` method)
		.UseAuthentication()
		.UseManualJsonRpc(builder =>
		{
        	//Maps `Class1` and its public methods to '/' route
			builder.RegisterController<Class1>();
        	//Also maps `Class2` and its public methods to '/' route, along side with `Class1` (methods will collide)
			builder.RegisterController<Class2>();
        	//Maps `Class3` and its public methods to '/Items' route
			builder.RegisterController<Class3>("Items");
        	//Maps `Class1` and its public methods to '/Items/Books' route
			builder.RegisterController<Class4>("Items/Books");
		});
}
```
3. Manual implementation

```cs
public void Configure(IApplicationBuilder app)
{
	//Custom route provider (specified below)
	IRpcRouteProvider routeProvider = new MyRouteProvider();
	app
    	//(Optional) Adding authentication (need to add AddAuthentication() in the `ConfigureServices` method)
		.UseAuthentication()
        //Specify custom route provider
        .UseJsonRpc(routeProvider);
}
```
Custom Route Provider:
```cs

	public class MyRouteProvider : IRpcRouteProvider
	{
    	//Base url route
		public RpcPath BaseRequestPath { get; } = "/api";

		//Return method providers per url route (does not include base route)
		public List<IRpcMethodProvider> GetMethodsByPath(RpcPath path)
		{
			if (path == "/Items")
			{
				return new List<IRpcMethodProvider>
				{
					new ItemsMethodProvider()
				};
			}
			else if(path == "/Things")
			{
				return new List<IRpcMethodProvider>
				{
					new ThingsMethodProvider()
				};
			}
			else
			{
				return new List<IRpcMethodProvider>();
			}
		}

		//Set of all possible routes
		public HashSet<RpcPath> GetRoutes()
		{
			return new HashSet<RpcPath>
			{
				"/Items",
				"/Things"
			};
		}
	}

	//Method provider for items controller
	public class ItemsMethodProvider : IRpcMethodProvider
	{
		public List<MethodInfo> GetRouteMethods()
		{
        	//Reflection to get all public and instanced methods
			return typeof(ItemsController).GetTypeInfo()
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.ToList();
		}
	}

	//Method provider for things controller
	public class ThingsMethodProvider : IRpcMethodProvider
	{
		public List<MethodInfo> GetRouteMethods()
		{
        	//Reflection to get all public and instanced methods
			return typeof(ThingsController).GetTypeInfo()
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.ToList();
		}
	}
```


## Examples of Rpc Classes:
Recommended format is to use subclasses of RpcController. These classes will be autodetected if using the UseJsonRpc() method (option 1 from above)

```cs
[RpcRoute("TestMethods")] //Optional, if not specified the route name would be 'Test' (based off the controller type name)
public class TestController : RpcController
{
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
    
    //Returning values is also valid but will never give an error unless an exception is thrown
	public int Subtract(int a, int b)
	{
		return a - b;
	}    
    
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
}
```
And or if manually mapping (not auto detection) any class can be used for Rpc calls if configured in `Startup.cs`

```cs
//Classes can be named anything and be located anywhere in the project/solution
//The way to associate them to the api is to use the RegisterController<T> method in
//the configuration
public class RpcClass1
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

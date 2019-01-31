# Lunar Server
HTTP server with minimal dependencies 

# Why Lunar Server?
Most of web development written in C# is done with ASP.NET, which is too high-level for my taste.
Also, many of the Web/HTTP classes from .NET / ASP.NET don't play well with Mono / Linux.


So I decided to write something that would allow me to code websites in C# with minimal dependencies and easy to use.

## Installation

    PM> Install-Package LunarServer

Since this is a .NET standard package, to use with .NET framework projects please set the target to .NET Framework 4.5 or higher, otherwise Nuget will give you installation errors.

## Supported platforms

- .NET Core
- .NET Framework 3.5 and above
- Mono & Xamarin
- UWP

## Supported features

- HTTP methods (GET/POST/PUT/DELETE)
- Cookies / Sessions
- File caching / ETAG / GZip compression
- Forms / File uploads

# Usage

Import the package:

```c#
using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;
```

Instantiate the necessary classes:

```c#
	// either parse the settings from the program args or initialize them manually
	var settings = ServerSettings.Parse(args);

	var server = new HTTPServer(settings, ConsoleLogger.Write);
```

Add some routes to the site.

```c#
	server.Get("/", (request) =>
	{
		return HTTPResponse.FromString("Hello world!");
	});
	
```

Finally add code to start the server.
```c#
	server.Run();

	bool running = true;

	Console.CancelKeyPress += delegate {
		server.Stop();
		running = false;
	};

	while (running) {
		Thread.Sleep(500);
	}
```

You can now open "http://localhost" in your browser and see "Hello World! appear.

Here's how to support POST requests (from HTML forms, etc)
```c#
	server.Post("/myform", (request) =>
	{		
		var username = request.args["username"];
		var password = request.args["password"];
		
		if (password == "hello") {
			return HTTPResponse.FromString("Login ok!");
		}
		else {
			return HTTPResponse.FromString("Invalid login details!");
		}		
	});	
```

Here's how to do dynamic routes (aka pretty URLs)
```c#
	server.Get("/user/{id}", (request) =>
	{		
		var user_id = request.args["id"];
		return HTTPResponse.FromString($"Hello user with ID = {user_id}!");
	});	
```

Here is how you redirect the user browser to another URL.
```c#
	server.Get("/secret", (request) =>
	{				
		return HTTPResponse.Redirect("/login");
	});	
```

There's also builtin support for Mustache templates.
First create a folder (eg: "views") with your template files.

```html
<h3>Hello, {{username}}!</h3>
```

Then instantiate a template engine and add the necessary routes.
```c#	
	var templateEngine = new TemplateEngine(server, "views");

	server.Get("/hello", (request) =>
	{
		var context = new Dictionary<string, object>();
		context["username"] = "Admin";
		return templateEngine.Render(context, "main");
	});
```
	
# Contact

Let me know if you find bugs or if you have suggestions to improve the project.

And maybe follow me [@onihunters](https://twitter.com/onihunters) :)
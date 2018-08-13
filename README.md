# Lunar Server
HTTP server with minimal dependencies 

# Why Lunar Server?
Most of web development written in C# is done with ASP.NET, which is too high-level for my tastes.
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
using LunarServer.Core;
```

Instantiate the necessary classes:

```c#
	// initialize a logger
	var log = new LunarServer.Core.Logger();

	// either parse the settings from the program args or initialize them manually
	var settings = ServerSettings.Parse(args);

	var server = new HTTPServer(log, settings);
	
	// instantiate a new site, the second argument is the file path where the public site contents will be found
	var site = new Site(server, "public");
```

Add some routes to the site.

```c#
	site.Get("/", (request) =>
	{
		return HTTPResponse.FromString("Hello world!");
	});
	
```

Finally add code to start the server.
```c#
	server.Run();
```

You can now open "http://localhost" in your browser and see "Hello World! appear.

Here's how to support POST requests (from HTML forms, etc)
```c#
	site.Post("/myform", (request) =>
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
	site.Get("/user/{id}", (request) =>
	{		
		var user_id = request.args["id"];
		return HTTPResponse.FromString($"Hello user with ID = {user_id}!");
	});	
```

Here is how you redirect the user browser to another URL.
```c#
	site.Get("/secret", (request) =>
	{				
		return HTTPResponse.Redirect("/login");
	});	
```

There's also builtin support for Mustache templates.
```c#
	var compiler = new FormatCompiler();
	var templateEngine = new TemplateEngine(compiler);

	site.Get("/hello", (request) =>
	{				
		var context = new Dictionary<string, string>();
		context["user"] = "Hello";
		return templateEngine.Render(site, context, new string[] { "templateFile" });
	});	
```
	
# Contact

Let me know if you find bugs or if you have suggestions to improve the code.

And maybe follow me [@onihunters](https://twitter.com/onihunters) :)
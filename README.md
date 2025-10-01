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
- Websockets

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

	server.WaitForFinish();
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

You can also do more complex routes using wildcards (although this way you will have to process manually the request)
```c#
	server.Get("/user/*", (request) =>
	{		
		var user_id = request.path.Replace("/user/", "");
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

Here is how you handle websockets.
```c#
	server.WebSocket("/chat", (socket) =>
	{
		while (socket.IsOpen)
		{
			var msg = socket.Receive();

			if (msg.CloseStatus == WebSocketCloseStatus.None)
			{
				var str = Encoding.UTF8.GetString(msg.Bytes);
				Console.WriteLine(str);

				socket.Send("Thanks man!");
			}
		}
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

For custom 404 not found pages use the OnNotFound handler.

```c#	
	var templateEngine = new TemplateEngine(server, "views");

	server.OnNotFound = (request) =>
	{
		var context = new Dictionary<string, object>();
		context["error"] = "Something funny";
		return templateEngine.Render(context, "404");
	};
```

# Template Examples

Passing arguments to nested templates (via globals).

```c#	
	{{#set FARM:=Farm}}
	{{#include farm}}	
```
Then inside the other template:

```c#	
	{{#if @FARM.Pair == 'LOL'}}
	Hello World
	{{/if}}
```

# Recommended directory structure
Create a main directory  for your site (eg: a folder called www), then inside create two sub-directories public and views.
Public directory is where you are going to add javascript, css, images, etc and views directory is where you put the htlm templates.

You can run then your server with something like:
--path=C:\code\my_server\www

# Settings

Those settings can be either configured in-code or passed as arguments to the server.
			
| Setting  | Values | Description |
| ------------- | ------------- | ------------- |
| --host  | An IP or hostname  | Specify the host where the server will be running, without this, localhost is assumed.  |
| --port  | A valid port number  | Specify the port where the server will be listening  |
| --postsize  | Number | Specify the maximum allowed number of bytes that is accepted via a POST request, pass 0 to disable POST  |
| --wsframes  | Number | Specify the maximum allowed of bytes for a websocket frame |
| --compression  | true/false  | Enable or disable HTML and JS compression |
| --cachetime  | Seconds  | Specify the duration of server caching  |
| --path  | A valid file path  | Specify the path from where the server will be serving files  |
| --binding  | IP address | Specify the IP address that the server will accept connections from, if not specified it will accept every ip address |
| --env  | dev or prod  | Specify the enviroment where the server will run |

			
# FAQ

**Q:** How to disable access to file system / static content? 

**A:** Currently the way to do this is to set the server settings Path to null.
```c#
	// either parse the settings from the program args or initialize them manually
	var settings = ServerSettings.Parse(args);
	settings.Path = null;

	var server = new HTTPServer(settings, ConsoleLogger.Write);
```

# Contact

Let me know if you find bugs or if you have suggestions to improve the project.

And maybe follow me [@onihunters](https://twitter.com/onihunters) :)
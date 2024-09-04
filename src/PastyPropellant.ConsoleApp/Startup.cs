using Microsoft.Owin;
using Owin;
using PastyPropellant.ConsoleApp;

[assembly: OwinStartup(typeof(Startup))]

namespace PastyPropellant.ConsoleApp;

// https://learn.microsoft.com/ru-ru/aspnet/web-api/overview/hosting-aspnet-web-api/use-owin-to-self-host-web-api

public class Startup
{
    // Invoked once at startup to configure your application.
    public void Configuration(IAppBuilder app)
    {
        app.Run(Invoke);
    }

    // Invoked once per request.
    public Task Invoke(IOwinContext context)
    {
        context.Response.ContentType = "text/plain";
        return context.Response.WriteAsync("Hello World");
    }
}

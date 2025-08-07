open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection

open WebSharper.AspNetCore
open Serilog
open Serilog.Extensions.Logging 

open Dina.Web.App
open Dina

[<EntryPoint>]
let main args =
    
    let logger = (new LoggerConfiguration())
                    .Enrich.FromLogContext()        
                    .MinimumLevel.Debug() 
                    .WriteTo.Console()
                    .WriteTo.File(Path.Combine(Runtime.AssemblyLocation, "Dina.Web.App.log"), fileSizeLimitBytes=512000, shared=true)
                    .CreateLogger();
    let lf = new SerilogLoggerFactory(logger);
    let lp = new SerilogLoggerProvider(logger, false);
    
    Runtime.Initialize("Dina.Web.App", "Dina.Web.App", true, lf, lp)

    let builder = WebApplication.CreateBuilder(args)
    
    // Add services to the container.
    builder.Services
        .AddSerilog(logger, false)
        .AddWebSharper()
        .AddAuthentication("WebSharper")
        .AddCookie("WebSharper", fun options -> ())              
    |> ignore

    let app = builder.Build()
    
    // Configure the HTTP request pipeline.
    if not (app.Environment.IsDevelopment()) then
        app.UseExceptionHandler("/Error")
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            .UseHsts()
        |> ignore

    app.UseHttpsRedirection()

#if DEBUG        
        //.UseWebSharperScriptRedirect(startVite = false)
#endif
        .UseAuthentication()
        .UseWebSharper(fun ws -> ws.Sitelet(Site.Main) |> ignore)
        .UseStaticFiles()      
    |> ignore

    app.Run()

    0 // Exit code

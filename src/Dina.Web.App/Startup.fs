open System

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection

open WebSharper.AspNetCore
open Serilog

open Dina.Web.App

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    
    // Add services to the container.
    builder.Services
       .AddSerilog(fun config ->
            config
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
            |> ignore)
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

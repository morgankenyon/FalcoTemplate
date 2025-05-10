open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
// ^-- this import adds many useful extensions

let endpoints =
    [
        get "/" (Response.ofPlainText "Hello World!")
        // ^-- associate GET / to plain text HttpHandler
    ]

let wapp = WebApplication.Create()

wapp.UseRouting()
    .Use(DefaultFilesExtensions.UseDefaultFiles)
    .Use(StaticFileExtensions.UseStaticFiles)
      // ^-- most IApplicationBuilder extensions are available as static methods similar to this
    .UseFalco(endpoints)
    .Run(Response.ofPlainText "Not found")
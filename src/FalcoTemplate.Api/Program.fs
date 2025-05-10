open Falco
open Falco.Markup
open Falco.Routing
open Microsoft.AspNetCore.Builder
// ^-- this import adds many useful extensions

let form =
    Templates.html5 "en" [] [
        Elem.form [ Attr.method "post" ] [
            Elem.input [ Attr.name "name" ]
            Elem.input [ Attr.type' "submit" ] ] ]

let wapp = WebApplication.Create()

let endpoints =
    [
        get "/" (Response.ofPlainText "Hello from /")
        all "/form" [
            GET, Response.ofHtml form
            POST, Response.ofEmpty ]
    ]

wapp.UseRouting()
    .UseFalco(endpoints)
    .Run()
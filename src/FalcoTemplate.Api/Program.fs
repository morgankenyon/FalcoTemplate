module FalcoTemplate.Api

open DbUp
open Falco
open Falco.Markup
open Falco.Routing
open Microsoft.AspNetCore.Builder
open System.Data.SQLite
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

SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());

let upgrader = 
    DeployChanges.To.SqliteDatabase("Data Source=./falco.db").WithScriptsFromFileSystem("./Scripts").WithTransaction().LogToConsole().Build()

let result = upgrader.PerformUpgrade()
//DeployChanges.To
//    .SqlDatabase(connectionString)
//    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
//    .LogToConsole()
//    .Build();
wapp.UseRouting()
    .UseFalco(endpoints)
    .Run()
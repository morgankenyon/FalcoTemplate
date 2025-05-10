module FalcoTemplate.Api

open Dapper.FSharp
open Dapper.FSharp.SQLite
open DbUp
open Falco
open Falco.Markup
open Falco.Routing
open Microsoft.AspNetCore.Builder

///Model stuff

type NewUser =
    {
        FirstName : string
        LastName : string
        Phone: string
    }

type User =
    {
        UserId : int32
        FirstName : string
        LastName : string
        Phone: string
    }

type DbUser =
    {
        user_id : int32
        first_name : string
        last_name : string
        phone : string
    }

let usersTable = table'<DbUser> "users"
///DB Stuff
let insertUser (newUser: NewUser) =
    task {
        use conn = new System.Data.SQLite.SQLiteConnection("Data Source=./falco.db")
        conn.Open()

        let user =
            {
                user_id = 0
                first_name = newUser.FirstName
                last_name = newUser.LastName
                phone = newUser.Phone
            }

        let! insertedCount =
            insert {
                for p in usersTable do
                value user
                excludeColumn p.user_id
            } |> conn.InsertAsync<DbUser>

        return insertedCount
    }

///Handler stuff
let insertUserHandler : HttpHandler =
    let handleOk (user : NewUser) : HttpHandler = fun ctx ->
        task {
            let! count = insertUser user

            let message = sprintf "Updated Records: %d" count
            return Response.ofPlainText message ctx
        }

    Request.mapJson handleOk

let form =
    Templates.html5 "en" [] [
        Elem.form [ Attr.method "post" ] [
            Elem.input [ Attr.name "name" ]
            Elem.input [ Attr.type' "submit" ] ] ]

let wapp = WebApplication.Create()

let endpoints =
    [
        get "/" (Response.ofPlainText "Hello from /")
        all "/user" [
            GET, Response.ofHtml form
            POST, insertUserHandler ]
    ]

SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
Dapper.FSharp.SQLite.OptionTypes.register()
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true |> ignore

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
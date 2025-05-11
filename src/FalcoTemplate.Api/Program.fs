module FalcoTemplate.Api

open Dapper
open DbUp
open Falco
open Falco.Markup
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Npgsql
open System.Data
open System

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
[<CLIMutable>]
type DbUser =
    {
        user_id : int32
        first_name : string
        last_name : string
        phone : string
    }

let toUser (dbUser: DbUser) : User =
    {
        UserId = dbUser.user_id
        FirstName = dbUser.first_name
        LastName = dbUser.last_name
        Phone = dbUser.phone
    }

/////DB Stuff
let connStr = "Host=localhost;Username=postgres;Password=password123;Database=falco"
let getAllUsers () =
    let sql = "SELECT * FROM dbo.users"

    task {
        use conn = new NpgsqlConnection(connStr) :> IDbConnection
        conn.Open()

        let! dbUsers = conn.QueryAsync<DbUser>(sql) //TODO - cancellationToken

        let ree = 
            dbUsers
            |> Seq.map (fun u -> toUser u)
            |> Seq.toList

        return ree
    }

let insertUser (newUser: NewUser) =
    let sql = 
        """
        INSERT INTO dbo.users (
            first_name,
            last_name,
            phone
        ) VALUES (
            @firstName,
            @lastName,
            @phone
        ) RETURNING user_id;
        """

    task {
        use conn = new NpgsqlConnection(connStr)
        let dbParams = {| firstName = newUser.FirstName; lastName = newUser.LastName; phone = newUser.Phone |}
        conn.Open()

        let! userId = conn.ExecuteScalarAsync<int>(sql, dbParams) //TODO - cancellationToken

        return userId
    }

///Handler stuff
let insertUserHandler : HttpHandler =
    let handleOk (user : NewUser) : HttpHandler = fun ctx ->
        task {
            let! userId = insertUser user

            let message = sprintf "UserId: %d" userId
            return Response.ofPlainText message ctx
        }

    Request.mapJson handleOk

let getAllUsersHandler : HttpHandler = fun ctx ->
    task {
        let! users = getAllUsers()

        return Response.ofJson users ctx
    }

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
            GET, getAllUsersHandler
            POST, insertUserHandler ]
    ]

SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3())
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true |> ignore

let upgrader = 
    DeployChanges.To.PostgresqlDatabase(connStr).WithScriptsFromFileSystem("./Scripts").WithTransaction().LogToConsole().Build()

let result = upgrader.PerformUpgrade()

if not result.Successful then
    Console.ForegroundColor = ConsoleColor.Red |> ignore
    Console.WriteLine(result.Error);
    Console.ResetColor();
#if DEBUG
    Console.ReadLine() |> ignore
#endif
    exit -1
else

    //TODO - handle any errors here
    wapp.UseRouting()
        .UseFalco(endpoints)
        .Run()
module FalcoTemplate.Api

open Dapper
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

let getAllUsers () =
    let sql = "SELECT * FROM users"

    task {
        use conn = new System.Data.SQLite.SQLiteConnection("Data Source=./falco.db")
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
        INSERT INTO users (
            first_name,
            last_name,
            phone
        ) VALUES (
            @firstName,
            @lastName,
            @phone
        )
        """

    task {
        use conn = new System.Data.SQLite.SQLiteConnection("Data Source=./falco.db")
        let cmd = new System.Data.SQLite.SQLiteCommand(sql, conn)
        cmd.Parameters.AddWithValue("@firstName", newUser.FirstName) |> ignore
        cmd.Parameters.AddWithValue("@lastName", newUser.LastName) |> ignore
        cmd.Parameters.AddWithValue("@phone", newUser.Phone) |> ignore
        conn.Open()

        let! _ = cmd.ExecuteNonQueryAsync() //TODO - cancellationToken
        return conn.LastInsertRowId
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
    DeployChanges.To.SqliteDatabase("Data Source=./falco.db").WithScriptsFromFileSystem("./Scripts").WithTransaction().LogToConsole().Build()

let result = upgrader.PerformUpgrade()

//TODO - handle any errors here
wapp.UseRouting()
    .UseFalco(endpoints)
    .Run()
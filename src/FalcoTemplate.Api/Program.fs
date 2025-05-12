namespace FalcoTemplate

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

module Models =
    type NewUser =
        {
            FirstName : string
            LastName : string
            Phone: string
        }

    [<CLIMutable>]
    type User =
        {
            UserId : int32
            FirstName : string
            LastName : string
            Phone: string
        }

module Data =
    open Models

    /////DB Stuff
    let connStr = "Host=localhost;Username=postgres;Password=password123;Database=falco"
    let getAllUsers () =
        let sql = "SELECT * FROM dbo.users"

        task {
            use conn = new NpgsqlConnection(connStr) :> IDbConnection
            conn.Open()

            let! dbUsers = conn.QueryAsync<User>(sql) //TODO - cancellationToken

            return dbUsers
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

module Handlers =
    open Data
    open Models

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

module Api =
    open Data
    open Handlers

    let wapp = WebApplication.Create()

    let endpoints =
        [
            all "/user" [
                GET, getAllUsersHandler
                POST, insertUserHandler ]
        ]

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
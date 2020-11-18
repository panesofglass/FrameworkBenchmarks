module App.App

open System
open System.Data
open System.IO
open System.Text
open System.Text.Json.Serialization
open System.Text.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Frank.Builder
open Npgsql
open App.Models

let inline contentLength x = new Nullable<int64>(int64 x)

let json': HttpContext -> Task =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    fun ctx ->
        ctx.Response.ContentType <- "application/json"
        ctx.Response.StatusCode <- 200
        let data = struct {| message = "Hello, World!" |}
        JsonSerializer.SerializeAsync(ctx.Response.Body, data)

let text' (msg: string): HttpContext -> Task =
    let bytes = Encoding.UTF8.GetBytes(msg)
    fun ctx ->
        ctx.Response.ContentLength <- contentLength bytes.Length
        ctx.Response.ContentType <- "text/plain"
        ctx.Response.StatusCode <- 200
        ctx.Response.Body.WriteAsync(bytes, 0, bytes.Length)

// Resources

let plaintext =
    resource "/plaintext" {
        name "Plain text"
        get (text' "Hello, World!")
    }

let json =
    resource "/json" {
        name "JSON"
        get json'
    }

let fortunes =
    resource "/fortunes" {
        name "Fortunes"
        get Fortunes.handler
    }

// App

let connectionFactory =
    fun () -> new NpgsqlConnection(ConnectionString) :> IDbConnection

[<EntryPoint>]
let main args =
    webHost args {
        useDefaults
        configure (fun bldr -> bldr.ConfigureLogging(fun c -> c.ClearProviders() |> ignore).UseKestrel())
        resource plaintext
        resource json
        resource fortunes
    }
    0

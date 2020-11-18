module App.Fortunes

open System.Data
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2.ContextInsensitive
open Npgsql
open App.Models

module View =
    open System.IO
    open System.Text

    [<Literal>]
    let TemplatePrefix =
        "<!DOCTYPE html><html><head><title>Fortunes</title></head><body><table><tr><th>id</th><th>message</th></tr>"

    [<Literal>]
    let TemplateSuffix = "</table></body></html>"

    let templatePrefixBytes = Encoding.UTF8.GetBytes(TemplatePrefix)

    let templateSuffixBytes = Encoding.UTF8.GetBytes(TemplateSuffix)

    let writeAsync (fortunes: Fortune list) (stream: Stream) =
        task {
            do! stream.WriteAsync(templatePrefixBytes, 0, templatePrefixBytes.Length)

            for f in fortunes do
                let row: string =
                    $"<tr><td>{string f.id}</td><td>{f.message}</td></tr>"

                let bytes = Encoding.UTF8.GetBytes(row)

                do! stream.WriteAsync(bytes, 0, bytes.Length)

            do! stream.WriteAsync(templateSuffixBytes, 0, templateSuffixBytes.Length)
        }

let rec readAsync (acc: Fortune list) (rd: Common.DbDataReader) =
    task {
        match! rd.ReadAsync() with
        | false -> return acc
        | true ->
            let result =
                { id = rd.GetInt32(0)
                  message = rd.GetString(1) }

            let results = result :: acc
            return! readAsync results rd
    }

let handler =
    let extra =
        { id = 0
          message = "Additional fortune added at request time." }

    fun (ctx: HttpContext) ->
        task {
            let conn = new NpgsqlConnection(ConnectionString)
            ctx.Response.RegisterForDispose conn

            let cmd =
                new NpgsqlCommand("SELECT id, message FROM fortune")

            ctx.Response.RegisterForDispose cmd

            do! conn.OpenAsync()
            let! reader = cmd.ExecuteReaderAsync()
            ctx.Response.RegisterForDispose(reader)
            let! data = readAsync [] reader

            let fortunes =
                extra :: data |> List.sortWith Fortune.comparer

            ctx.Response.ContentType <- "text/html"

            ctx.Response.StatusCode <- 200

            do! View.writeAsync fortunes ctx.Response.Body
        }

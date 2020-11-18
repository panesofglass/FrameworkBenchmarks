module App.Models

open System

[<Literal>]
let ConnectionString =
    "Server=tfb-database;Database=hello_world;User Id=benchmarkdbuser;Password=benchmarkdbpass;Maximum Pool Size=1024;NoResetOnClose=true;Enlist=false;Max Auto Prepare=3"

[<CLIMutable>]
type Fortune = { id: int; message: string }

module Fortune =
    let comparer (a: Fortune) (b: Fortune) =
        String.CompareOrdinal(a.message, b.message)

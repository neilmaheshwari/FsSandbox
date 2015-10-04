// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

open fszmq

[<EntryPoint>]
let main argv = 
//
//    let pipe = "inproc://example"
//
//    let context = new Context ()
//    let sink = Context.router context
//    Socket.bind sink pipe
//    let server = Context.rep context

    async {peering1.main ["DC1"; "DC2"; "DC3"]} |> Async.Start // Start DC1 and connect to DC2 and DC3
    async {peering1.main ["DC2"; "DC1"; "DC3"]} |> Async.Start // Start DC2 and connect to DC1 and DC3
    async {peering1.main ["DC3"; "DC1"; "DC2"]} |> Async.Start // Start DC3 and connect to DC1 and DC2

    System.Console.ReadLine ()
    |> ignore

    printfn "%A" argv
    0 // return an integer exit code


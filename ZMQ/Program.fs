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

    rereq.main ()

    printfn "%A" argv
    0 // return an integer exit code


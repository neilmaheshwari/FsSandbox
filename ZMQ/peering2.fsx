#I @"../packages/fszmq/lib/net40"
#r "fszmq.dll"
#load "zhelpers.fs"

module peering2 =

    open fszmq

    [<Literal>] 
    let private NBR_CLIENTS = 10
    [<Literal>] 
    let private NBR_WORKERS = 3
    [<Literal>] 
    let private WORKER_READY = "\001"

    let private clientTask brokerName = 
        use context = new Context ()
        let client = Context.req context
        Socket.connect client <| sprintf "ipc://%s-localfe.ipc" brokerName
        while true do
            zhelpers.s_send client "Hello!"
            let msg = zhelpers.s_recv client
            stdout.WriteLine (sprintf "[Client] Client: %s" msg)

    let workerTask brokerName =
        use context = new Context ()
        let worker = Context.req context
        Socket.connect worker <| sprintf "ipc://%s-localbe.ipc" brokerName
        zhelpers.s_send worker WORKER_READY
        while true do
            let msg = zhelpers.s_recv worker 
            stdout.WriteLine (sprintf "[Worker] Worker: %s" msg) 
            zhelpers.s_send worker "OK"

    let main = function
        | brokerName :: peers ->

            stdout.WriteLine (sprintf "I: preparing broker at %s..." brokerName)

            let ctx = new Context ()

            // Bind cloud frontend to endpoint
            let cloudfe = Context.router ctx
            Socket.bind cloudfe <| sprintf "ipc://%s-cloudfe.ipc" brokerName

            // Connect cloud backend to all peers
            let cloudbe = Context.router ctx
            for peer in peers do
                stdout.WriteLine (sprintf "I: connecting to cloud frontend at '%s'" peer)
                Socket.connect cloudbe <| sprintf "ipc://%s-cloudbe.ipc" peer

            // Prepare local frontend and backend
            let localfe = Context.router ctx
            Socket.bind localfe <| sprintf "ipc://%s-localfe.ipc" brokerName

            let localbe = Context.router ctx
            Socket.bind localbe <| sprintf "ipc://%s-localbe.ipc" brokerName

            // Get user to tell us when to start...
            stdout.WriteLine "Press Enter when all brokers are started: "
            System.Console.ReadLine |> ignore

        | [] -> failwith "No args passed in"
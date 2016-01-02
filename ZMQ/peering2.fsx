#I @"../packages/fszmq/lib/net40"
#r "fszmq.dll"
#load "zhelpers.fs"

module peering2 =

    open fszmq

    [<Literal>] 
    let private NBR_CLIENTS = 10
    [<Literal>] 
    let private NBR_WORKERS = 3

    let private WORKER_READY = "\001"B // Signals worker is ready

    let private clientTask brokerName = 
        use context = new Context ()
        let client = Context.req context
        Socket.connect client <| sprintf "ipc://%s-localfe.ipc" brokerName
        let rec loop () = 
            Socket.send client "HELLO"B
            client
            |> Socket.recv
            |> zhelpers.decode
            |> sprintf "[Client] Client : %s"
            |> stdout.WriteLine
            loop ()
        loop ()

    let private workerTask brokerName =
        use context = new Context ()
        let worker = Context.req context
        Socket.connect worker <| sprintf "ipc://%s-localbe.ipc" brokerName
        // Tell broker we are ready for work
        Socket.send worker WORKER_READY
        // Process messages as they arrive
        let rec loop () = 
            let msg = Socket.recvAll worker 
            msg
            |> Array.last
            |> zhelpers.decode
            |> sprintf "[Worker] Worker: %s"
            |> stdout.WriteLine
            msg.[Array.length msg - 1] <- "OK"B
            Socket.sendAll worker msg
            loop ()
        loop ()

    let main (rng : System.Random) = function
        | brokerName :: (_ :: _ as peers) ->
            // First argument is this broker's name.
            // Other arguments are our peers' names.
            stdout.WriteLine (sprintf "I: preparing broker at %s..." brokerName)

            use ctx = new Context ()

            // Bind cloud frontend to endpoint
            let cloudfe = Context.router ctx
            Socket.bind cloudfe <| sprintf "ipc://%s-cloud.ipc" brokerName

            // Connect cloud backend to all peers
            let cloudbe = Context.router ctx
            for peer in peers do
                stdout.WriteLine (sprintf "I: connecting to cloud frontend at '%s'" peer)
                Socket.connect cloudbe <| sprintf "ipc://%s-cloud.ipc" peer

            // Prepare local frontend and backend
            let localfe = Context.router ctx
            Socket.bind localfe <| sprintf "ipc://%s-localfe.ipc" brokerName

            let localbe = Context.router ctx
            Socket.bind localbe <| sprintf "ipc://%s-localbe.ipc" brokerName

            // Get user to tell us when to start...
            stdout.WriteLine "Press Enter when all brokers are started: "
            System.Console.ReadLine |> ignore

            // Start local workers
            for _ in 0 .. NBR_WORKERS do
                async {workerTask brokerName} |> Async.Start

            // Start local clients
            for _ in 0 .. NBR_CLIENTS do
                async {clientTask brokerName} |> Async.Start

            while true do async {do! Async.Sleep 1000} |> Async.RunSynchronously //TODO

        | _ -> failwith "No args passed in"

    main (new System.Random 0) (Array.toList fsi.CommandLineArgs)
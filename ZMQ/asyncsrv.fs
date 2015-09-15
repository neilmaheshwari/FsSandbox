module asyncsrv

    open fszmq
    open zhelpers 

    // Asynchronous client-to-server (DEALER to ROUTER)

    let rng = new System.Random 1

    let clientTask () = 
        use context = new Context ()
        use client = Context.dealer context

        // Set random identity to make tracing easier
        s_setID client
        let identity = ZMQ.IDENTITY |> Socket.getOption client |> decode
        Socket.connect client "tcp://localhost:5570"
        let rec loop requestNumber =
            // Tick once per second, pulling in arriving messages
            for i in 1 .. 100 do
                let printMessage socket = 
                    let msg = Socket.recvAll socket |> Array.map decode
                    match msg with
                    | [|""; message|] ->
                        stdout.WriteLine (sprintf "(Client %s) %s" identity message)
                    | unexpected -> 
                        stderr.WriteLine (sprintf "(Client %s) Recieved unexpected message: %A" identity unexpected)
                Polling.pollIn printMessage client
                |> Seq.singleton
                |> Polling.poll 10L
                |> ignore
            s_sendmore client ""
            s_send client (sprintf "request %d for client %s" requestNumber identity)
            loop <| (requestNumber + 1)
        loop 0

    let serverWorker context =
        use worker = Context.dealer context
        Socket.connect worker "inproc://backend"
        s_setID worker
        let identity = ZMQ.IDENTITY |> Socket.getOption worker |> decode
        while true do
            // The DEALER socket gives us the reply envelope and message
            let msg = Socket.recvAll worker
            match msg |> Array.map decode with
            | [|_identity; ""; _message|] as m ->
                // Send 0..4 replies back
                let replies = rng.Next(5)
                for n in 1 .. replies do
                    // Sleep for some fraction of a second
                    sleep (rng.Next(1000) + 1)
                    Socket.sendAll worker msg
            | unexpected -> 
                stderr.WriteLine (sprintf "(Server worker) recieved unexpected message: %A" unexpected)

    let serverTask () = 
        use context = new Context ()

        // Frontend socket talks to clients over TCP
        let frontend = Context.router context
        Socket.bind frontend "tcp://*:5570"

        // Backend socket talks to workers over inproc
        let backend = Context.dealer context
        Socket.bind backend "inproc://backend"

        for i in 1 .. 4 do
            async {serverWorker context} |> Async.Start

        // Connect backend to frontend via a proxy
        Proxying.proxy frontend backend None

    // The main thread simply starts several clients and a server, and then 
    // waits for the serer to finish
    let main () = 
        for i in 0 .. 3 do
            async {clientTask ()} |> Async.Start
        async {serverTask ()} |> Async.Start
        sleep <| 5000
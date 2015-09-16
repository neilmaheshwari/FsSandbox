module simple
    
    open fszmq    

    let encode = string >> System.Text.Encoding.ASCII.GetBytes
    let decode = System.Text.Encoding.ASCII.GetString
    let rng = new System.Random 1

    let s_setID socket = 
      let identity = sprintf "%04X-%04X" 
                             (rng.Next(0,0x10000)) 
                             (rng.Next(0,0x10000))
                     |> encode
      (ZMQ.IDENTITY,identity) |> Socket.setOption socket


    let clientTask () = 
        printfn "Starting client.."
        use context = new Context ()
        use client = Context.req context
        Socket.connect client "tcp://localhost:5560"
        s_setID client
        let identity = ZMQ.IDENTITY |> Socket.getOption client |> decode
        Socket.send client (encode <| sprintf "Hello from %s" identity)
        while true do
            let msg = decode (Socket.recv client)
            printfn "(%s) received message %s" identity msg
            Socket.send client (encode <| sprintf "Hello from %s" identity)
            System.Threading.Thread.Sleep (rng.Next (0, 1000) + 1)

    let serverTask () = 
        printfn "Starting server..."
        use context = new Context ()
        use server = Context.rep context
        Socket.bind server "tcp://*:5560"
        while true do
            let msg = Socket.recv server
            Socket.send server (encode <| sprintf "Returning: %s" (decode msg))


    let main () = 

        async {do serverTask ()} |> Async.Start

        System.Threading.Thread.Sleep 1000

        for i in 1..5 do
            async {do clientTask ()} |> Async.Start
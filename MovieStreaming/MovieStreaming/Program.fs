namespace MovieStreaming
open Akka.FSharp

type PlaybackMessage = {
    MovieTitle: string
    UserId: int
}

module Actors =
    open Akka.FSharp.Actors

    let PlaybackActor (mailbox : Actor<_>)=
            let rec loop() = actor{
                let! msg = mailbox.Receive()
                printfn "Playing movie: %A with user id: %A" msg.MovieTitle msg.UserId
                return! loop()
            }
            loop()

    let SimplePlaybackActor msg =
        printfn "Playing simple movie: %A with simple user id: %A" msg.MovieTitle msg.UserId

module Program =
    open System
    open Actors

    [<EntryPoint>]
    let main argv =
        use system = System.create "movie-streaming-actor-system" <| Configuration.load()
        printfn "Actor System Created"

        let playbackActor = spawn system "playback-actor" PlaybackActor
        printfn "Playback Actor Spawned"
        playbackActor <! { MovieTitle = "Akka.NET of Things"; UserId = 42}

        let simplePlaybackActor = spawn system "simple-playback-actor" <| actorOf SimplePlaybackActor
        printfn "Simple Playback Actor Spawned"
        simplePlaybackActor <! { MovieTitle = "Akka.NET of Things Strikes Back"; UserId = 43}
        simplePlaybackActor <! { MovieTitle = "Akka.NET of Things Returns and Strikes Back Again"; UserId = 44}

        Console.ReadLine() |> ignore
        system.Terminate() |> Async.AwaitIAsyncResult |> ignore
        printfn "system terminated"      

        Console.ReadLine() |> ignore
        0

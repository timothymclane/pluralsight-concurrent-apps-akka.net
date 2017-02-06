namespace MovieStreaming
open Akka.FSharp

type PlaybackMessage = {
    MovieTitle: string
    UserId: int
}

type StopMovieMessage = {
    Stop: string
}

module Actors =
    open Akka.FSharp.Actors
    open Akka.Actor
    open System

    let PlaybackActor (mailbox : Actor<_>)=
            let rec loop() = actor{
                let! msg = mailbox.Receive()
                printfn "Playing movie: %A with user id: %A" msg.MovieTitle msg.UserId
                return! loop()
            }
            loop()

    let SimplePlaybackActor msg =
        printfn "Playing simple movie: %A with simple user id: %A" msg.MovieTitle msg.UserId
    
    //We must use the object-based actors if we want access to all the lifecycle methods
    type ObjectPlaybackActor() =
        inherit ReceiveActor()
        let handleMovieMessage msg =
            printfn "Playing object movie: %A with object user id: %A" msg.MovieTitle msg.UserId
        do
            base.Receive<PlaybackMessage> handleMovieMessage

        override x.PreStart() =
            printfn "ObjectPlaybackActor PreStart"

        override x.PostStop() =
            printfn "ObjectPlaybackActor PostStop"

        override x.PreRestart (reason, msg) =
            printfn "ObjectPlaybackActor restarted because %A" reason
            base.PreRestart (reason, msg)
            
        override x.PostRestart reason =
            printfn "ObjectPlaybackActor PostRestart because %A" reason
            base.PostRestart reason

    type ObjectUserActor() =
        inherit ReceiveActor()
        let mutable _currentlyWatching = ""
        let startPlayingMovie title =
            _currentlyWatching <- title
            printfn "User is currently watching %A" title
        let stopPlayingCurrentMovie() =
            printfn "Stopping user movie %A" _currentlyWatching
            _currentlyWatching <- String.Empty
        let handlePlayMovieMessage msg =
            if(not <| String.IsNullOrWhiteSpace _currentlyWatching)
                then printfn "Cannot start playing another movie without stopping one currently playing"
            else
                startPlayingMovie msg.MovieTitle
        let handleStopMovieMessage msg =
            if(String.IsNullOrWhiteSpace _currentlyWatching)
                then printfn "Cannot stop playing movie when one is not playing"
            else
                stopPlayingCurrentMovie()
        do
            base.Receive<PlaybackMessage> handlePlayMovieMessage
            base.Receive<StopMovieMessage> handleStopMovieMessage

module Program =
    open System
    open Actors
    open Akka.Actor

    [<EntryPoint>]
    let main argv =
        use system = System.create "movie-streaming-actor-system" <| Configuration.load()
        printfn "Actor System Created"

        let playbackActor = spawn system "playback-actor" PlaybackActor
        printfn "Playback Actor Spawned"
        playbackActor <! { MovieTitle = "Akka.NET of Things"; UserId = 42}

        let objectPlaybackActor = spawnObj system "object-playback-actor" (<@ (fun () -> new ObjectPlaybackActor()) @>)
        objectPlaybackActor <! {MovieTitle = "Akka.NET of OOPs Things"; UserId = 0085}

        let simplePlaybackActor = spawn system "simple-playback-actor" <| actorOf SimplePlaybackActor
        printfn "Simple Playback Actor Spawned"
        simplePlaybackActor <! { MovieTitle = "Akka.NET of Things Strikes Back"; UserId = 43}
        simplePlaybackActor <! { MovieTitle = "Akka.NET of Things Returns and Strikes Back Again"; UserId = 44}

        objectPlaybackActor <! PoisonPill.Instance

        let userActor = spawnObj system "user-playback-actor" (<@ (fun () -> new ObjectUserActor()) @>)
        userActor <! { MovieTitle = "Akka.NET of Things Strikes Back"; UserId = 43}
        userActor <! { MovieTitle = "Akka.NET of Things Strikes Back Again"; UserId = 43}
        Console.ReadLine() |> ignore
        printfn "Trying to stop movie"
        userActor <! {Stop = "Stop!"}

        Console.ReadLine() |> ignore
        system.Terminate() |> Async.AwaitIAsyncResult |> ignore
        printfn "system terminated"      

        Console.ReadLine() |> ignore
        0

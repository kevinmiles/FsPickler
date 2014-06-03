﻿namespace Nessos.FsPickler.Tests

    [<AutoOpen>]
    module Utils =

        open System
        open System.IO
        open System.Reflection
        open System.Net
        open System.Net.Sockets
        open System.Runtime.Serialization
        open System.Threading.Tasks

        open NUnit.Framework

        let runsOnMono = System.Type.GetType("Mono.Runtime") <> null

        let allFlags = BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance ||| BindingFlags.Static

        let runOnce (f : unit -> 'T) : unit -> 'T = let x = lazy (f ()) in fun () -> x.Value

        let defaultArg' (x : 'T option) (def : unit -> 'T) = match x with Some x -> x | None -> def ()

        type AsyncBuilder with
            member __.Bind(f : Task<'T>, g : 'T -> Async<'S>) = __.Bind(Async.AwaitTask f, g)
            member __.Bind(f : Task, g : unit -> Async<'S>) = __.Bind(f.ContinueWith ignore |> Async.AwaitTask, g)

        module Disposable =
            let combine (components : seq<IDisposable>) =
                let components = Seq.toArray components
                {
                    new IDisposable with
                        member __.Dispose () =
                            for d in components do
                                d.Dispose()
                }

        type Stream with
            member s.AsyncWriteBytes (bytes : byte []) =
                async {
                    do! s.WriteAsync(BitConverter.GetBytes bytes.Length, 0, 4)
                    do! s.WriteAsync(bytes, 0, bytes.Length)
                    do! s.FlushAsync()
                }

            member s.AsyncReadBytes(length : int) =
                let rec readSegment buf offset remaining =
                    async {
                        let! read = s.ReadAsync(buf, offset, remaining)
                        if read < remaining then
                            return! readSegment buf (offset + read) (remaining - read)
                        else
                            return ()
                    }

                async {
                    let bytes = Array.zeroCreate<byte> length
                    do! readSegment bytes 0 length
                    return bytes
                }

            member s.AsyncReadBytes() =
                async {
                    let! lengthArr = s.AsyncReadBytes 4
                    let length = BitConverter.ToInt32(lengthArr, 0)
                    return! s.AsyncReadBytes length
                }

        module FsUnit =

            let shouldFailwith<'Exception when 'Exception :> exn>(f : unit -> unit) =
                let result =
                    try f () ; Choice1Of3 ()
                    with
                    | :? 'Exception -> Choice2Of3 ()
                    | e -> Choice3Of3 e

                match result with
                | Choice1Of3 () ->
                    let msg = sprintf "Expected exception '%s', but was successful." typeof<'Exception>.Name
                    raise <| new AssertionException(msg)
                | Choice2Of3 () -> ()
                | Choice3Of3 e ->
                    let msg = sprintf "An unexpected exception type was thrown\nExpected: '%s'\n but was: '%s'." (e.GetType().Name) typeof<'Exception>.Name
                    raise <| new AssertionException(msg)


        module FsCheck =

            open FsCheck

            type FsPicklerQCGenerators =
                static member BigInt = Arb.generate<byte []> |> Gen.map (fun bs -> System.Numerics.BigInteger(bs)) |> Arb.fromGen
            
            module Check =

                let QuickThrowOnFail<'T> (f : 'T -> unit) = Check.QuickThrowOnFailure f

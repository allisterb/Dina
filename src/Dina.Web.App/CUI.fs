﻿namespace Dina.Web.App

open WebSharper

open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html

open Dina.Web
open Dina.Web.JQueryTerminal
open Dina.Web.WebSpeech
open Dina.Web.BotLibre
//open SMApp.Microphone


[<AutoOpen;JavaScript>]
module CUI =        
    type MicState = MicNotInitialized | MicConnecting | MicDisconnected | MicAudioStart | MicAudioEnd | MicReady | MicError of string | MicResult of obj * obj

    type ClientState = ClientNotInitialzed | ClientReady | ClientUnderstand 

    type Interpreter = Interpreter of ((obj*obj) -> unit) * ((Terminal->string->unit) * Options)
        with
        member x.Unwrap = match x with | Interpreter(v, (i, o)) -> v, i, o
        member x.Voice = let v, i, o = x.Unwrap in v
        member x.Text = let v, i, o = x.Unwrap in i
        member x.Options = let v, i, o = x.Unwrap in o

    type CUI = {
         Voice: SpeechSynthesisVoice option
         //Mic: Mic option
         Term: Terminal
         Avatar: WebAvatar
     }
     with
         member x.Echo' (text:string) = x.Term.Disable(); x.Term.Echo text; x.Term.Enable()
 
         member x.EchoHtml' (text:string) = 
             let rawOpt = EchoOptions(Raw=true)
             x.Term.Disable(); x.Term.Echo(text, rawOpt); x.Term.Enable()
 
         member x.Debug loc m = debug loc m
 
         member x.Say text =
            async {
                let synth = Window.SpeechSynthesis
                if synth.Speaking then SDK.Chime()
                x.Avatar.AddMessage(text)
                x.Avatar.ProcessMessages(0) 
            } |> Async.Start

         member x.SayDoc (d:Doc) =
            let i =  JQuery.JQuery(".terminal-otput").Get().[0].ChildNodes.Length
            div [cls "terminal-command"; dindex (i + 1)] [d] |> Doc.RunAppend (termOutput())

         member x.SayAngry m =
           async {
               let synth = Window.SpeechSynthesis
               if synth.Speaking then 
                   synth.Cancel()
                   SDK.Chime()
               x.Avatar.AddMessage2(m, "anger")
               x.Avatar.ProcessMessages(0) 
           } |> Async.Start

         //member x.sayRandom phrases t = x.Say <| getRandomPhrase phrases t
     
         member x.Wait (f:unit -> unit) =
             do 
                 x.Echo'("please wait...")
                 x.Term.Pause();f();x.Term.Resume()
 
         member x.Wait(f:Async<unit>) = x.Wait(fun _ -> f |> Async.Start)
 
         member x.SayVoices() =
             let voices' = Window.SpeechSynthesis.GetVoices()
             do if not(isNull(voices')) then
                 let voices = voices' |> toArray    
                 sprintf "There are currently %i voices installed on this computer or device." voices.Length |> x.Say
                 voices |> Array.iteri (fun i v -> sprintf "Voice %i. Name: %s, Local: %A." i v.Name v.LocalService |> x.Say)
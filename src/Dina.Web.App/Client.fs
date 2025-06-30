namespace Dina.Web.App

open System.Collections.Generic    
open WebSharper
open WebSharper.UI
open WebSharper.UI.Templating
open WebSharper.JavaScript

open Dina.Web.JQueryTerminal    
open Dina.Web.BotLibre
open Dina.Web.WebSpeech

[<JavaScript>]
module Client =
    (* CUI state *)
    let mutable CUI = {
        Voice = None
        //Mic = None
        Term = Unchecked.defaultof<Terminal>
        Avatar = 
            SDK.ApplicationId <- "4277115329081938617"
            let sdk = new SDKConnection()
            let web = new WebAvatar()
            web.Version <- 8.5
            web.Connection <- sdk
            web.Avatar <- "22225225"
            web.Voice <- "cmu-slt";
            web.VoiceMod <- "default";
            web.NativeVoice <- true;
            web.NativeVoiceName <- "Microsoft David Desktop - English (United States)";
            web.Width <- 300;
            web.CreateBox();
            web.AddMessage("")
            web.ProcessMessages(0)
            web
    }
    let mutable MicState = MicNotInitialized
    let mutable ClientState = ClientNotInitialzed
    
    (* Console and terminal messages *)
    let echo m = do if not(isNull(CUI.Term)) then CUI.Term.EchoHtml' <| sprintf "%A" m 
    let debug m = debug "CLIENT" m
    let wait (f:unit -> unit) =
        do 
            CUI.Term.Echo'("please wait")
            CUI.Term.Pause();f();CUI.Term.Resume()

    (* Dialogue state *)

    let Props = new Dictionary<string, obj>()
    let Output = new Stack<string>()

    (* Speech *)
    let synth = Window.SpeechSynthesis
    
    let initSpeech() =
        let voices = synth.GetVoices() |> toArray         
        if voices.Length > 0 then
            let v = voices |> Array.find (fun v -> v.Default) in 
            CUI <- { CUI with Voice = Some v }  
            debug <| sprintf "Using browser speech synthesis voice %s." CUI.Voice.Value.Name
            CUI.Avatar.NativeVoice <- true
            CUI.Avatar.NativeVoiceName <- v.Name
        else  
            echo "No browser speech synthesis voice is available. Falling back to CMU TTS."
    
    let say' text = CUI.Say text                

    let say text =
        Output.Push text
        say' text
        
    let sayVoices() =
        let voices = speechSynthesis().GetVoices() |> toArray    
        sprintf "There are currently %i voices installed on this computer or device." voices.Length |> say'
        voices |> Array.iteri (fun i v -> sprintf "Voice %i. Name: %s, Local: %A." i v.Name v.LocalService |> say')

    //let sayRandom t phrases = say <| getRandomPhrase phrases t

          
        
    /// Terminal interpreter loop
    let mainterm (term:Terminal) (command:string)  =
        CUI <- { CUI with Term = term }
        do 
            //if CUI.Mic = None then initMic main'
            if CUI.Voice = None then initSpeech ()
            
        do if ClientState = ClientNotInitialzed then ClientState <- ClientReady
        match command with
        (* Quick commands *)
        | Text.Blank -> say' "Tell me what you want me to do or ask me a question."
        | Text.Debug ->  ()
            //debug <| sprintf "Utterances: %A" Utterances
            //debug <| sprintf "Questions: %A" Questions
        | Text.Voices -> sayVoices()
        | _ ->
            match ClientState with
            | ClientUnderstand -> say' "I'm still trying to understand what you said before."
            | ClientReady ->
                match command.ToLower() with
                (* Quick commands *)
                | Text.QuickHello m 
                | Text.QuickHelp m 
                | Text.QuickYes m
                | Text.QuickNo m -> 
                    debug <| sprintf "Quick Text: %A." m                        
                    //m |> push |> Main.update
                    ClientState <- ClientReady

                | _->         
                   
                    ClientState <- ClientReady
                    
            | ClientNotInitialzed -> error "Client is not initialized."
    let mainOpt =
        Options(
            Name="Main", 
            Greetings = "Welcome to Dina. Enter 'hello' or 'hello my name is...(you) to initialize speech or say help for more info.",
            Prompt =">"
        )       
       
    let run() =        
        //Html.div [Html.attr.id "main"; cls "container"] []

        Terminal("#term", ThisAction<Terminal, string>(fun term command -> mainterm term command), mainOpt) |> ignore
        Doc.Empty

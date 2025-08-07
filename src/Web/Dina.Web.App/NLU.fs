namespace Dina.Web.App

open WebSharper

[<JavaScript;RequireQualifiedAccess>]
module Text =
    let (|Blank|_|) =
        function
        | "" -> Some()
        | _ -> None

    let (|Debug|_|) =
        function
        | "debug" -> Some ()
        | _ -> None

    let (|Voices|_|) =
        function
        | "voices" -> Some ()
        | _ -> None

    let (|QuickHello|_|) =
        function
        | "hello"
        | "hey"
        | "yo"
        | "hi" -> Some()
        | _ -> None

    let (|QuickHelp|_|) =
        function
        | "help"
        | "help me"
        | "what's this?"
        | "huh" -> Some () 
        | _ -> None    

    let (|QuickYes|_|) =
        function
        | "yes"
        | "ok"
        | "sure"
        | "yeah" 
        | "yep" 
        | "uh huh" 
        | "go ahead" 
        | "go" -> Some()
        | _ -> None

    let (|QuickNo|_|) =
        function
        | "no"
        | "nope"          
        | "no way" 
        | "nah" 
        | "don't do it" 
        | "stop" -> Some () 
        | _ -> None

    let (|One|_|) =
        function
        | "1"
        | "one" -> Some()
        | _ -> None

    let (|Two|_|) =
        function
        | "2"
        | "two" -> Some()
        | _ -> None

    let (|Three|_|) =
        function
        | "3"
        | "three" -> Some()
        | _ -> None

    let (|QuickNumber|_|) =
        function
        | One m 
        | Two m -> Some m
        | _ -> None

    let (|QuickPrograms|_|) =
        function
        | "programs" -> Some()
        | _ -> None


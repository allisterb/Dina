namespace Dina.Web.App

open WebSharper
open WebSharper.JavaScript
open Dina.Web.JQuery

open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Server

type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/about">] About

module Templates =
    type MainTemplate = Templating.Template<"Main.html">

    let Main ctx action (title: string) (body: Doc list) =
        Content.Page (
            MainTemplate()
                .Title(title)
                .Body(body)
                .Doc()
        )
    
module Site =
    let HomePage ctx =
        Templates.Main ctx Home "Lerna" [
            div [attr.id "term"] [
                ClientServer.client <@ Client.run() @>
            ]
        ]
(*
    let AboutPage ctx =
        Content.Page(
            Templating.Main ctx EndPoint.About "About" [
                h1 [] [text "About"]
                p [] [text "This is a template WebSharper client-server application."]
            ], 
            Bundle = "about"
        )
        *)
    [<Website>]
    let Main =
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            | EndPoint.Home -> HomePage ctx
            | EndPoint.About -> HomePage ctx
        )


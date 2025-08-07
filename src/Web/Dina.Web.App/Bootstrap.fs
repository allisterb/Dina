namespace Dina.Web.App

open WebSharper
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client

open Dina.Web
module Resources =
    open WebSharper.Core.Resources
    
    type CSS() =
        inherit BaseResource(@"https://cdn.jsdelivr.net/npm/bootstrap@5.3.7/dist/css", "bootstrap.min.css")
    type JS() =
        inherit BaseResource("https://cdn.jsdelivr.net/npm/bootstrap@5.3.7/dist/js", "bootstrap.bundle.min.js")

[<Require(typeof<Resources.CSS>);Require(typeof<JQuery.Resources.JQuery>);Require(typeof<Resources.JS>)>]
[<JavaScript>]
module Bs =
    
    let btnPrimary id label onclick = button [eid id; cls "btn btn-primary"; on.click onclick] [text label]
    let btnSecondary id label onclick = button [eid id; cls "btn btn-secondary"; on.click onclick] [text label]

    let input lbl extras (target, labelExtras, targetExtras) =
        div (cls "form-group" :: extras) [
            label labelExtras [text lbl]
            Doc.InputType.Text [cls "form-control"; targetExtras] target
        ]

    let inputPassword lbl extras (target, labelExtras, targetExtras) =
        div (cls "form-group" :: extras) [
            label labelExtras [text lbl]
            Doc.InputType.Password (cls "form-control" :: targetExtras) target
        ]

    let textArea lbl extras (target, labelExtras, targetExtras) =
        div (cls "form-group" :: extras) [
            label labelExtras [text lbl]
            Doc.InputArea (cls "form-control" :: targetExtras) target
        ]

    let checkbox lbl extras (target, labelExtras, targetExtras) =
        div (cls "checkbox" :: extras) [
            label labelExtras [
                Doc.InputType.CheckBox targetExtras target
                text lbl
            ]
        ]

    let Radio lbl extras (target, labelExtras, targetExtras) =
        div (cls "radio" :: extras) [
            label labelExtras [
                Doc.InputType.Radio targetExtras true target
                text lbl
            ]
        ]
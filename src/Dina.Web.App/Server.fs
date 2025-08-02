namespace Dina.Web.App

open WebSharper
open WebSharper.Web

module Server =

    
    [<Rpc>]
    let Prompt input =
        task {
             let ctx = WebSharper.Web.Remoting.GetContext();
             let! user = ctx.UserSession.GetLoggedInUser()
            return R input
        }

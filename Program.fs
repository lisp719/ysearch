module ysearch.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe

// ---------------------------------
// Models
// ---------------------------------

type Message = { Text: string }

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open Giraffe.ViewEngine

    let layout (content: XmlNode list) =
        html
            [ _lang "en" ]
            [ head
                  []
                  [ meta [ _charset "utf-8" ]
                    meta [ _name "viewport"; _content "width=device-width, initial-scale=1.0" ]
                    title [] [ encodedText "Ysearch" ]
                    link
                        [ _rel "stylesheet"
                          _href "https://cdn.jsdelivr.net/npm/@picocss/pico@2/css/pico.min.css" ]
                    link [ _rel "stylesheet"; _type "text/css"; _href "/main.css" ] ]
              body
                  []
                  [ div
                        [ _class "container" ]
                        [ header [] [ nav [] [ div [] [ ul [] [ li [] [ encodedText "Ysearch" ] ] ] ] ]
                          div [] [ main [ attr "role" "main" ] content ] ] ] ]

    let index () =
        [ form
              [ _method "post"; _target "_blank" ]
              [ fieldset [] [ input [ _type "text"; _name "query"; _placeholder "Enter search query"; _required ] ]
                button [ _type "submit" ] [ encodedText "Search" ] ] ]
        |> layout

// ---------------------------------
// Web app
// ---------------------------------

let indexHandler =
    let view = Views.index ()
    htmlView view

let searchHandler =
    fun next (ctx: HttpContext) ->
        task {
            let form = ctx.Request.Form
            let query = form["query"].ToString()
            let baseUrl = "https://www.youtube.com"

            if String.IsNullOrWhiteSpace(query) then
                return! redirectTo false baseUrl next ctx
            else
                let sp = "CAISBBABGAM%253D"

                let youtubeUrl =
                    $"{baseUrl}/results?search_query={Uri.EscapeDataString(query)}&sp={sp}"

                return! redirectTo false youtubeUrl next ctx
        }

let webApp =
    choose
        [ GET >=> choose [ route "/" >=> indexHandler ]
          POST >=> choose [ route "/" >=> searchHandler ]
          setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder: CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:5000", "https://localhost:5001").AllowAnyMethod().AllowAnyHeader()
    |> ignore

let configureApp (app: IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()

    (match env.IsDevelopment() with
     | true -> app.UseDeveloperExceptionPage()
     | false -> app.UseGiraffeErrorHandler(errorHandler).UseHttpsRedirection())
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services: IServiceCollection) =
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder.AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(contentRoot, "WebRoot")

    Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .UseContentRoot(contentRoot)
                .UseWebRoot(webRoot)
                .Configure(Action<IApplicationBuilder> configureApp)
                .ConfigureServices(configureServices)
                .ConfigureLogging(configureLogging)
            |> ignore)
        .Build()
        .Run()

    0

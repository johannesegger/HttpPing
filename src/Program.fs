open System
open Argu
open System.Net.Http
open FSharp.Control.Reactive
open System.Threading

module Result =
    let ofSeq list =
        let folder s t =
            match s, t with
            | Ok s', Ok t' -> Ok (t' :: s')
            | Ok _, Error t' -> Error [ t' ]
            | Error s', Ok _ -> Error s'
            | Error s', Error t' -> Error (t' :: s')
        Seq.fold folder (Result.Ok []) list

module Http =
    open System.Net
    open System.Net.Http

    let sendRequest (url: Uri) requestMethod requestHeaders cookies = async {
        try
            let baseUri = Uri(url.GetLeftPart(UriPartial.Authority))

            let cookieContainer = CookieContainer()
            let cookieCollection = CookieCollection()
            cookies
            |> Seq.map (fun (key, value) -> Cookie(key, value))
            |> Seq.iter cookieCollection.Add
            cookieContainer.Add(baseUri, cookieCollection)

            use handler = new HttpClientHandler(CookieContainer = cookieContainer)
            use client = new HttpClient(handler, BaseAddress = baseUri)
            use request = new HttpRequestMessage(requestMethod, url)

            requestHeaders
            |> Seq.iter (fun (key, value: string) -> request.Headers.Add(key, value))

            let! response = client.SendAsync request |> Async.AwaitTask
            response.EnsureSuccessStatusCode() |> ignore
            return Ok ()
        with e -> return Error e
    }

type HttpRequestMethod =
    | Get

type CliArguments =
    | Url of string
    | Interval of string
    | Request_Method of HttpRequestMethod
    | Request_Headers of string list
    | Cookies of string list
    with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Url _ -> "URL of the site to ping."
            | Interval _ -> "How frequently the URL should be pinged."
            | Request_Method _ -> "Http request method."
            | Request_Headers _ -> "Http request headers."
            | Cookies _ -> "Cookies to send to the URL."

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<CliArguments>(programName = "open-end-session.exe")
    let results = parser.Parse argv

    let parseKeyValueArg (arg: string) =
        match arg.IndexOf '=' with
        | -1 -> Error (sprintf "Can't parse argument \"%s\". Expected 'key=value'." arg)
        | idx -> Ok (arg.Substring(0, idx), arg.Substring(idx + 1))

    let parseKeyValueArguments args =
        Seq.map parseKeyValueArg args
        |> Result.ofSeq
        |> function
        | Ok v -> v
        | Error e ->
            e
            |> List.map (sprintf "* %s")
            |> List.append [ "Couldn't parse one or more arguments:" ]
            |> String.concat Environment.NewLine
            |> failwithf "%s"

    let parseUri = Uri

    let parseTimeSpan = TimeSpan.Parse

    let url = results.PostProcessResult(<@ Url @>, parseUri)
    let interval = results.PostProcessResult(<@ Interval @>, parseTimeSpan)
    let requestMethod =
        match results.GetResult(<@ Request_Method @>, defaultValue = Get) with
        | Get -> HttpMethod.Get
    let requestHeaders = results.PostProcessResult (<@ Request_Headers @>, parseKeyValueArguments)
    let cookies = results.PostProcessResult (<@ Cookies @>, parseKeyValueArguments)

    printfn
        "Start pinging %O %O with %d cookies (%s) and %d headers (%s)"
        requestMethod url
        (Seq.length cookies) (Seq.map fst cookies |> String.concat ", ")
        (Seq.length requestHeaders) (Seq.map fst requestHeaders |> String.concat ", ")

    use x =
        Observable.timerSpan interval
        |> Observable.repeat
        |> Observable.startWith [ 0L ]
        |> Observable.flatmapAsync (fun _ -> Http.sendRequest url requestMethod requestHeaders cookies)
        |> Observable.subscribe(function
            | Ok () -> printfn "%A: Success" DateTime.Now
            | Error e -> printfn "%A: Error: %O" DateTime.Now e)
    use waitHandle = new ManualResetEventSlim()
    waitHandle.Wait()
    0

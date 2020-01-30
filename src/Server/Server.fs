open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks.V2
open Giraffe
open Saturn
open Shared

open SkiaSharp
open SkiaSharp.QrCode.Image
open System


let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"

let port =
    "SERVER_PORT"
    |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

let generateQRCode host tournamentcode =
    let url = host + "/Enter/" + tournamentcode
    let qrCode = QrCode(url, Vector2Slim(256, 256), SKEncodedImageFormat.Png)
    let stream = new MemoryStream()
    qrCode.GenerateImage(stream)
    let bytes = stream.ToArray()
    let b64string = Convert.ToBase64String(bytes)
    "data:image/png;base64," + b64string

let tournamentEntryQR = generateQRCode "http://localhost:8080"

type Tournament = { Name: string; Code: string }

let tournamentController = controller {
    index (fun ctx -> [{Name = "BCS London Cardfight Vanguard Premium"; Code = "FOOBAR"}] |> Controller.json ctx)
}

let webApp = router {
    get "/api/init" (fun next ctx ->
        task {
            let model = {Value = 42; Qr = tournamentEntryQR "SAMPLE" }
            return! json model next ctx
        })
    forward "/tournaments" tournamentController
}

let app = application {
    url ("http://0.0.0.0:" + port.ToString() + "/")
    use_router webApp
    memory_cache
    use_static publicPath
    use_json_serializer(Thoth.Json.Giraffe.ThothSerializer())
    use_gzip
}

run app

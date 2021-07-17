# tus-dotnet-client
.Net client for [tus.io](http://tus.io/) Resumable File Upload protocol.

## Features
- tus protocol v1.0.0
- protocol extension supported: Creation, Termination
- no external dependencies
- upload progress events
- .net 4.0 / .net standard 2.0 / .net core 2.0 / .net core 3.1
- used in production for .net 4.0 desktop app
- small enought to copy into your project

## Usage

See TusClientConsole for usage and a test suite of sorts.

```vbnet
Dim tc As New TusClient.TusClient()
AddHandler tc.Uploading, Sub(bytesTransferred As Integer, bytesTotal As Integer)
                             Dim perc As Decimal = bytesTransferred / bytesTotal * 100.0
                             Console.WriteLine("Up {0:0.00}% {1} of {2}", perc, bytesTransferred, bytesTotal)
                         End Sub

Dim fileURL = tc.Create(ServerURL, testfile)
tc.Upload(fileURL, testfile)
tc.Delete(fileURL)
```

## Alternatives

If async support or Nuget is important to you check out these other great dotnet tus clients:
- [https://github.com/jonstodle/TusDotNetClient](https://github.com/jonstodle/TusDotNetClient)
- [https://github.com/bluetianx/BirdMessenger](https://github.com/bluetianx/BirdMessenger)

## License
MIT

# tus-dotnet-client
.Net client for [tus.io](http://tus.io/) Resumable File Upload protocol.

## Features
- tus protocol v1.0.0
- protocol extension supported: Creation, Termination
- no external dependencies
- upload progress events
- .net 4.0

## Usage
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

## License
MIT
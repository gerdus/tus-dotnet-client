Public Class TusdTest
    Inherits TestBase

    Public Shared Sub RunInConsole()
        Dim t As New TusdTest()
        t.StartTusdServer()

        t.UploadExampleMinimal()
        t.UploadExampleStream()
        t.ServerInfo()
        t.UploadWithProgress()
        t.UploadConnectionInterrupted()
        t.CancelResumeExample()

        t.StopTusdServer()
    End Sub

    Private TusServerProcess As Process

    Private Sub StartTusdServer()
        Console.WriteLine("Starting TUS server...")

        Dim wd = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\..\..\tusd")
        wd = IO.Path.GetFullPath(wd)

        TusServerProcess = New System.Diagnostics.Process()
        TusServerProcess.StartInfo.WorkingDirectory = wd
        TusServerProcess.StartInfo.FileName = "tusd.exe"
        TusServerProcess.StartInfo.Arguments = String.Format("-host {0} -port {1}", ServerAddress, ServerPort)
        TusServerProcess.StartInfo.UseShellExecute = True
        TusServerProcess.StartInfo.CreateNoWindow = False
        TusServerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized
        TusServerProcess.Start()
        Threading.Thread.Sleep(100)
        If TusServerProcess.HasExited Then
            Throw New Exception("TUS server terminated at start - probably cause server already running")
        End If
    End Sub

    Private Sub StopTusdServer()
        Console.WriteLine("Stopping TUS server...")
        If Not TusServerProcess.HasExited Then
            TusServerProcess.Kill()
            TusServerProcess.WaitForExit()
        End If
    End Sub


    Public Sub UploadConnectionInterrupted()
        Dim testfile = GenFileBinary(sizeInMb:=64)

        Dim PreviousPercentage As Decimal = 0
        Dim PreviousPercentageDisconnect As Decimal = 0

        Dim tc As New TusClient.TusClient()
        AddHandler tc.Uploading, Sub(bytesTransferred As Long, bytesTotal As Long)

                                     Dim perc As Decimal = bytesTransferred / bytesTotal * 100.0
                                     perc = Math.Truncate(perc)
                                     If perc <> PreviousPercentage Then
                                         Console.WriteLine("Up {0:0.00}% {1} of {2}", perc, HumanizeBytes(bytesTransferred), HumanizeBytes(bytesTotal))
                                         PreviousPercentage = perc
                                     End If

                                     If perc > PreviousPercentageDisconnect And perc > 0 And Math.Ceiling(perc) Mod 20 = 0 Then
                                         StopTusdServer()
                                         StartTusdServer()
                                         PreviousPercentageDisconnect = Math.Ceiling(perc)
                                     End If

                                 End Sub

        Dim fileURL = tc.Create(ServerURL, testfile)
        tc.Upload(fileURL, testfile)

        VerifyUpload(fileURL, testfile)

        'Cleanup
        IO.File.Delete(testfile.FullName)
    End Sub
End Class

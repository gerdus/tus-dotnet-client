Module Module1

    Sub Main()
        'StartTusdServer()

        'UploadExampleMinimal()
        'UploadExampleStream()
        'ServerInfo()
        'UploadWithProgress()
        'UploadConnectionInterrupted()
        CancelResumeExample()



        'StopTusdServer()
        Console.WriteLine("Press the any key")
        Console.ReadKey()
    End Sub

    '********************************************************************************************************************
    Private TusServerProcess As Process
    Private Property ServerURL As String = "http://127.0.0.1:1080/files/"
    '********************************************************************************************************************
    Private Sub UploadExampleMinimal()
        Dim testfile = GenFileText(sizeInMb:=32)

        Dim tc As New TusClient.TusClient()
        AddHandler tc.Uploading, Sub(bytesTransferred As Long, bytesTotal As Long)
                                     Dim perc As Decimal = bytesTransferred / bytesTotal * 100.0
                                     Console.WriteLine("Up {0:0.00}% {1} of {2}", perc, bytesTransferred, bytesTotal)
                                 End Sub

        Dim fileURL = tc.Create(ServerURL, testfile)
        tc.Upload(fileURL, testfile)

        tc.Delete(fileURL)

        'Cleanup
        My.Computer.FileSystem.DeleteFile(testfile.FullName)
    End Sub

    Private Sub UploadExampleStream()
        Dim testfile = GenFileText(sizeInMb:=32)

        Dim metadata As New Dictionary(Of String, String)()
        metadata("filena") = testfile.Name

        Dim tc As New TusClient.TusClient()
        AddHandler tc.Uploading, Sub(bytesTransferred As Long, bytesTotal As Long)
                                     Dim perc As Decimal = bytesTransferred / bytesTotal * 100.0
                                     Console.WriteLine("Up {0:0.00}% {1} of {2}", perc, bytesTransferred, bytesTotal)
                                 End Sub

        Dim fileURL = tc.Create(ServerURL, testfile.Length, metadata:=metadata)
        Using fs As New IO.FileStream(testfile.FullName, IO.FileMode.Open, IO.FileAccess.Read)
            tc.Upload(fileURL, fs)
        End Using


        tc.Delete(fileURL)

        'Cleanup
        My.Computer.FileSystem.DeleteFile(testfile.FullName)
    End Sub

    Private Sub CancelResumeExample()
        Dim testfile = GenFileText(sizeInMb:=32)

        Dim lastperc As Integer = 0

        Dim tc As New TusClient.TusClient()
        AddHandler tc.Uploading, Sub(bytesTransferred As Long, bytesTotal As Long)
                                     Dim perc As Decimal = bytesTransferred / bytesTotal * 100.0
                                     If perc - lastperc > 1 Then
                                         Console.WriteLine("Up {0:0.00}% {1} of {2}", perc, bytesTransferred, bytesTotal)
                                         lastperc = perc
                                     End If
                                     If perc > 50 Then
                                         tc.Cancel()
                                     End If
                                 End Sub

        Dim fileURL = tc.Create(ServerURL, testfile)
        Try
            tc.Upload(fileURL, testfile)
        Catch ex As TusClient.TusException
            If ex.Status = Net.WebExceptionStatus.RequestCanceled Then
                Console.WriteLine("Upload Cancelled")
            Else
                Throw
            End If
        End Try

        Threading.Thread.Sleep(2000)

        tc = New TusClient.TusClient() 'Have to create new client to resume with same URL
        AddHandler tc.Uploading, Sub(bytesTransferred As Long, bytesTotal As Long)
                                     Dim perc As Decimal = bytesTransferred / bytesTotal * 100.0
                                     If perc - lastperc > 1 Then
                                         Console.WriteLine("Up {0:0.00}% {1} of {2}", perc, bytesTransferred, bytesTotal)
                                         lastperc = perc
                                     End If
                                 End Sub

        Console.WriteLine("Upload Resumed")
        tc.Upload(fileURL, testfile)

        Console.WriteLine("Upload Complete")
        tc.Delete(fileURL)

        'Cleanup
        My.Computer.FileSystem.DeleteFile(testfile.FullName)

    End Sub

    Private Sub UploadWithProgress()
        Dim testfile = GenFileText(sizeInMb:=32)

        Dim sw As New Stopwatch()
        Dim bytesTransferredLast As Long = 0
        Dim transferRate As Decimal = 0

        Dim PreviousPercentage As Decimal = 0

        Dim tc As New TusClient.TusClient()
        AddHandler tc.Uploading, Sub(bytesTransferred As Long, bytesTotal As Long)
                                     If sw.Elapsed.TotalSeconds > 0 Then
                                         transferRate = (bytesTransferred - bytesTransferredLast) / sw.Elapsed.TotalSeconds
                                     End If

                                     Dim perc As Decimal = bytesTransferred / bytesTotal * 100.0
                                     perc = Math.Truncate(perc)

                                     If perc <> PreviousPercentage Then
                                         Console.WriteLine("Up {0:0.00}% {1} of {2} @ {3}/second", perc, HumanizeBytes(bytesTransferred), HumanizeBytes(bytesTotal), HumanizeBytes(transferRate))
                                         PreviousPercentage = perc
                                     End If

                                     If sw.Elapsed.TotalSeconds > 1 Then 'calc transfer rate over the last second
                                         bytesTransferredLast = bytesTransferred
                                         sw.Restart()
                                     End If
                                 End Sub



        Dim fileURL = tc.Create(ServerURL, testfile)
        sw.Start()
        tc.Upload(fileURL, testfile)
        sw.Stop()

        VerifyUpload(fileURL, testfile)

        Dim serverInfo = tc.getServerInfo(ServerURL)

        If serverInfo.SupportsDelete Then
            If tc.Delete(fileURL) Then
                Console.WriteLine("Upload Terminated")
            Else
                Console.WriteLine("Upload Terminated FAILED")
            End If
        End If


        'Cleanup
        My.Computer.FileSystem.DeleteFile(testfile.FullName)
    End Sub



    Private Sub UploadConnectionInterrupted()
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
        My.Computer.FileSystem.DeleteFile(testfile.FullName)
    End Sub

    Private Sub VerifyUpload(fileURL As String, testfile As IO.FileInfo)
        Console.WriteLine("Verifying...")

        Dim sha = New System.Security.Cryptography.SHA1CryptoServiceProvider()

        Dim localBytes = My.Computer.FileSystem.ReadAllBytes(testfile.FullName)
        Dim sha1hashLocal = BitConverter.ToString(sha.ComputeHash(localBytes))

        Dim tc As New TusClient.TusClient()
        Dim response = tc.Download(fileURL)

        If localBytes.Length <> response.ResponseBytes.Length Then
            Throw New Exception("File size do not match!")
        End If

        Dim sha1hashRemote = BitConverter.ToString(sha.ComputeHash(response.ResponseBytes))

        If Not sha1hashLocal = sha1hashRemote Then
            Throw New Exception("File hashes do not match!")
        Else
            Console.WriteLine("Uploaded and Downloaded file hash match: {0}", sha1hashRemote.Replace("-", ""))
        End If
    End Sub


    Private Sub ServerInfo()
        Dim tc As New TusClient.TusClient()
        Dim serverInfo = tc.getServerInfo(ServerURL)
        Console.WriteLine("Version:{0}", serverInfo.Version)
        Console.WriteLine("Supported Protocols:{0}", serverInfo.SupportedVersions)
        Console.WriteLine("Extensions:{0}", serverInfo.Extensions)
        Console.WriteLine("MaxSize:{0}", serverInfo.MaxSize)
    End Sub

    Private Function GenFileBinary(sizeInMb As Long) As System.IO.FileInfo
        Console.WriteLine("Generating Binary Test File...")

        Dim fi = My.Computer.FileSystem.GetFileInfo(".\random.file")
        If My.Computer.FileSystem.FileExists(fi.FullName) Then
            My.Computer.FileSystem.DeleteFile(fi.FullName)
        End If
        Dim rnd As New Random()

        Dim data As Byte() = New Byte(sizeInMb * 1024 * 1024 - 1) {}
        Dim rng As New Random()
        rng.NextBytes(data)
        IO.File.WriteAllBytes(fi.FullName, data)

        'Refresh File Info
        fi = My.Computer.FileSystem.GetFileInfo(fi.FullName)
        Return fi
    End Function

    Private Function GenFileText(sizeInMb As Long) As System.IO.FileInfo
        Console.WriteLine("Generating Text Test File...")

        Dim fi = My.Computer.FileSystem.GetFileInfo(".\random.file")
        If My.Computer.FileSystem.FileExists(fi.FullName) Then
            My.Computer.FileSystem.DeleteFile(fi.FullName)
        End If

        Dim sizeInBytes = sizeInMb * 1024 * 1024
        Dim bytesWritten As Long = 0

        Using fs As New IO.FileStream(fi.FullName, IO.FileMode.Create)
            Using sw As New IO.BinaryWriter(fs)
                While bytesWritten < sizeInBytes
                    Dim charsbytes = System.Text.Encoding.UTF8.GetBytes("A")
                    sw.Write(charsbytes)
                    bytesWritten += charsbytes.Length
                End While
            End Using
        End Using

        'Refresh File Info
        fi = My.Computer.FileSystem.GetFileInfo(fi.FullName)

        Return fi
    End Function

    Public Function HumanizeBytes(bytes As Long) As String
        Dim res As Decimal
        res = bytes
        If res < 1024 Then
            Return String.Format("{0:n2} b", res)
        End If

        res = res / 1024
        If res < 1024 Then
            Return String.Format("{0:n2} Kb", res)
        End If

        res = res / 1024
        Return String.Format("{0:n2} Mb", res)

    End Function


    Private Sub StartTusdServer()
        Console.WriteLine("Starting TUS server...")
        TusServerProcess = New System.Diagnostics.Process()
        TusServerProcess.StartInfo.WorkingDirectory = "..\..\tusd"
        TusServerProcess.StartInfo.FileName = "tusd.exe"
        TusServerProcess.StartInfo.Arguments = "-host 127.0.0.1"
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
End Module

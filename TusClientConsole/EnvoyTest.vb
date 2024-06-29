Public Class EnvoyTest
    Inherits TestBase

    Public Shared Sub RunInConsole(port As Integer)
        Dim t As New EnvoyTest()
        t.ServerPort = port

        t.UploadExampleMinimal()
        t.UploadExampleStream()
        t.ServerInfo()
        t.UploadWithProgress()
        t.CancelResumeExample()
    End Sub



End Class

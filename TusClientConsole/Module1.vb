Module Module1

    Sub Main()
        'using local tusd exe
        'TusdTest.RunInConsole()

        'use the docker compose under envoy folder
        'eg: podman-compose up -d
        EnvoyTest.RunInConsole(8800)

        Console.WriteLine("Complete")
        Console.WriteLine("Press the any key")
        Console.ReadKey()
    End Sub

End Module

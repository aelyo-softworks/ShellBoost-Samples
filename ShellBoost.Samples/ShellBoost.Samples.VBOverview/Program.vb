Imports System
Imports ShellBoost.Core
Imports ShellBoost.Core.Utilities

Namespace ShellBoost.Samples.Overview
    Class Program
        Public Shared Sub Main()
            Console.WriteLine("ShellBoost Samples - " & (If(Client.Installer.IsNetCore, "Core", Nothing)) & "Overview - " & (If(IntPtr.Size = 4, "32", "64")) & "-bit - Copyright (C) 2017-" & DateTime.Now.Year & " Aelyo Softworks. All rights reserved.")
            Console.WriteLine("ShellBoost Runtime Version " & GetType(ShellContext).Assembly.GetInformationalVersion())
            Console.WriteLine()
            Console.WriteLine("Press a key:")
            Console.WriteLine()
            Console.WriteLine("   '1' Register the native proxy, run this sample, and unregister on exit.")
            Console.WriteLine("   '2' Register the native proxy.")
            Console.WriteLine("   '3' Run this sample (the native proxy will need to be registered somehow for Explorer to display something).")
            Console.WriteLine("   '4' Unregister the native proxy.")
            Console.WriteLine()
            Console.WriteLine("   Any other key will exit.")
            Console.WriteLine()
            Dim key = Console.ReadKey(True)

            Select Case key.KeyChar
                Case "1"c
                    Run(True)
                    ShellFolderServer.UnregisterNativeDll(RegistrationMode.User)
                Case "2"c
                    ShellFolderServer.RegisterNativeDll(RegistrationMode.User)
                    Console.WriteLine("Registered")
                Case "3"c
                    Run(False)
                Case "4"c
                    ShellFolderServer.UnregisterNativeDll(RegistrationMode.User)
                    Console.WriteLine("Unregistered")
            End Select
        End Sub

        Private Shared Sub Run(ByVal register As Boolean)
            Using server = New OverviewShellFolderServer()
                Dim config = New ShellFolderConfiguration()

                If register Then
                    config.NativeDllRegistration = RegistrationMode.User
                End If

                AddHandler server.Licensing, AddressOf OnLicensing
                server.Start(config)
                Console.WriteLine("Started listening on proxy id " & ShellFolderServer.ProxyId.ToString() & ". Press ESC key to stop serving folders.")
                Console.WriteLine("If you open Windows Explorer and have registered, you should now see the extension.")

                While Console.ReadKey(True).Key <> ConsoleKey.Escape
                End While

                Console.WriteLine("Stopped")
            End Using
        End Sub

        Private Shared Sub OnLicensing(ByVal sender As Object, ByVal e As LicensingEventArgs)
            Console.WriteLine("LicenseDataIsValid: " & ShellFolderServer.LicenseDataIsValid)
            Console.WriteLine("LicenseExpirationDate: " & ShellFolderServer.LicenseExpirationDate)
            Console.WriteLine("LicenseRegisteredCompany: " & ShellFolderServer.LicenseRegisteredCompany)
        End Sub
    End Class
End Namespace

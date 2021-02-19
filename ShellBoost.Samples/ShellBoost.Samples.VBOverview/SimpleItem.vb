Imports System.Runtime.InteropServices
Imports ShellBoost.Core
Imports ShellBoost.Core.Client
Imports ShellBoost.Core.Utilities
Imports ShellBoost.Core.WindowsPropertySystem

Namespace ShellBoost.Samples.Overview
    Public Class SimpleItem
        Inherits ShellItem

        Public Sub New(ByVal parent As ShellFolder, ByVal text As String)
            MyBase.New(parent, New StringKeyShellItemId(text))
            ItemType = IOUtilities.PathGetExtension(text)
            CanCopy = True
        End Sub

        Public Overrides Function TryGetPropertyValue(ByVal key As PropertyKey, <Out> ByRef value As Object) As Boolean
            If key = WindowsPropertySystem.System.InfoTipText Then
                value = "This is " & DisplayName & ", info created " + DateTime.Now
                Return True
            End If

            Return MyBase.TryGetPropertyValue(key, value)
        End Function

        Public Overrides Function GetContent() As ShellContent
            Return New MemoryShellContent(DisplayName & " - this is dynamic content created from VB.NET " & (If(Installer.IsNetCore, "Core", "Framework")) & " at " + DateTime.Now)
        End Function
    End Class
End Namespace

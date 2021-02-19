Imports ShellBoost.Core

Namespace ShellBoost.Samples.Overview
    Public Class OverviewShellFolderServer
        Inherits ShellFolderServer

        Private _root As SimpleFolder

        Protected Overrides Function GetFolderAsRoot(ByVal idList As ShellItemIdList) As ShellFolder
            If _root Is Nothing Then
                _root = New SimpleFolder(idList)
            End If

            Return _root
        End Function
    End Class
End Namespace

Imports ShellBoost.Core
Imports ShellBoost.Core.WindowsShell

Namespace ShellBoost.Samples.Overview
    Public Class SimpleFolder
        Inherits ShellFolder

        Public Sub New(ByVal parent As SimpleFolder, ByVal name As String)
            MyBase.New(parent, New StringKeyShellItemId(name))
            Level = parent.Level + 1
        End Sub

        Public Sub New(ByVal idList As ShellItemIdList)
            MyBase.New(idList)
        End Sub

        Public Overloads ReadOnly Property FolderServer As OverviewShellFolderServer
            Get
                Return CType(MyBase.FolderServer, OverviewShellFolderServer)
            End Get
        End Property

        Public ReadOnly Property Level As Integer

        Public Overrides Iterator Function EnumItems(ByVal options As SHCONTF) As IEnumerable(Of ShellItem)
            If options.HasFlag(SHCONTF.SHCONTF_FOLDERS) AndAlso Level < 3 Then
                Dim max = 2

                For i As Integer = 0 To max - 1
                    Yield New SimpleFolder(Me, "Virtual Folder " & Level & "." & i)
                Next
            End If

            If options.HasFlag(SHCONTF.SHCONTF_NONFOLDERS) Then
                Dim max = 2

                For i As Integer = 0 To max - 1
                    Yield New SimpleItem(Me, "Virtual Item #" & i & ".txt")
                Next
            End If
        End Function
    End Class
End Namespace

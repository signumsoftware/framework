Imports System.IO
Imports System.Net
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Collections.Generic

Imports Despliegue.DN
Imports Despliegue.Compartido



Public Class ClienteAS
    Implements IDisposable

    Private mServWeb As New DespliegueWebService.Service


#Region "Metodos"
    Public Function PedirCambios(ByVal micol As List(Of ArchivoDN)) As List(Of ArchivoOrdenesDN)

        Dim pmicol As Byte() = Empaquetador.Empaqueta(micol)

        Dim presult As Byte() = mServWeb.Diferencias(pmicol)

        Return Empaquetador.Desempaqueta(Of List(Of ArchivoOrdenesDN))(presult)

    End Function

    Public Function DameArchivoWeb(ByVal pao As DN.ArchivoOrdenesDN) As Stream

        Dim ruta As String = pao.Archivo.Ruta

        Dim url As String = mServWeb.DameURLArchivo(ruta)

        Dim wc As WebClient

        wc = New WebClient()

        Return wc.OpenRead(url)

    End Function

    Public Function RutaEjecutable() As String

        Return mServWeb.RutaEjecutable()

    End Function

#End Region

    Private disposed As Boolean = False

    ' IDisposable
    Private Overloads Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposed Then
            If disposing Then
                Me.mServWeb.Dispose()
            End If

            ' TODO: put code to free unmanaged resources here
        End If
        Me.disposed = True
    End Sub

#Region " IDisposable Support "
    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overrides Sub Finalize()
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(False)
        MyBase.Finalize()
    End Sub
#End Region

End Class

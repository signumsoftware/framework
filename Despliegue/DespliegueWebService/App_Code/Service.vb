Option Strict On

Imports Despliegue.Compartido
Imports Despliegue.DN

Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.Collections.Generic


<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class Service
    Inherits System.Web.Services.WebService




    <WebMethod()> Public Function Diferencias(ByVal colArchivos As Byte()) As Byte()

        Dim colcli As List(Of ArchivoDN) = Empaquetador.Desempaqueta(Of List(Of ArchivoDN))(colArchivos)

        Dim colserv As List(Of ArchivoDN) = ArchivoAD.Recuperar(Entorno.DirectorioTrabajo, Entorno.DirectorioTrabajoZip)

        Dim colord As List(Of ArchivoOrdenesDN) = ComparadorArchivos.Diferencias(colcli, colserv)

        Return Empaquetador.Empaqueta(colord)
    End Function


    <WebMethod()> Public Function RutaEjecutable() As String

        Return Entorno.Ejecutable

    End Function


    <WebMethod()> Public Function DameURLArchivo(ByVal parchivo As String) As String

        ArchivoyZipAD.GeneraZipDeArchivo(parchivo, Entorno.DirectorioTrabajo, Entorno.DirectorioTrabajoZip)

        Dim file As String = Me.Server.UrlPathEncode(parchivo & ".zip")

        Return Entorno.URLArchivos.TrimEnd("/"c) + "/"c + file

    End Function

End Class

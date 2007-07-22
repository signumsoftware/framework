#Region "Importaciones"

Imports System.Collections.Generic
Imports Framework.DatosNegocio

#End Region

Public Class ParametrosHelperAD

#Region "Metodos"
    Public Shared Sub ProcesarColEntidadesBase(ByVal pColentidadesBase As IList, ByVal pCampo As String, ByRef pCondiciones As String, ByRef pNumeroCondicion As Int64, ByVal pParametros As List(Of IDataParameter))
        ProcesarColEntidadesBaseobject(pColentidadesBase, pCampo, pCondiciones, pNumeroCondicion, pParametros)
    End Sub
    Public Shared Sub ProcesarColEntidadesBase(ByVal pColentidadesBase As IList(Of EntidadDN), ByVal pCampo As String, ByRef pCondiciones As String, ByRef pNumeroCondicion As Int64, ByVal pParametros As List(Of IDataParameter))
        ProcesarColEntidadesBaseobject(pColentidadesBase, pCampo, pCondiciones, pNumeroCondicion, pParametros)
    End Sub
    Public Shared Sub ProcesarColEntidadesBase(ByVal pColentidadesBase As ArrayListValidable(Of EntidadDN), ByVal pCampo As String, ByRef pCondiciones As String, ByRef pNumeroCondicion As Int64, ByVal pParametros As List(Of IDataParameter))
        ProcesarColEntidadesBaseobject(pColentidadesBase, pCampo, pCondiciones, pNumeroCondicion, pParametros)
    End Sub
    Public Shared Sub ProcesarColEntidadesBase(ByVal pColentidadesBase As ArrayListValidable, ByVal pCampo As String, ByRef pCondiciones As String, ByRef pNumeroCondicion As Int64, ByVal pParametros As List(Of IDataParameter))
        ProcesarColEntidadesBaseobject(pColentidadesBase, pCampo, pCondiciones, pNumeroCondicion, pParametros)
    End Sub
    Private Shared Sub ProcesarColEntidadesBaseobject(ByVal pColentidadesBase As IList, ByVal pCampo As String, ByRef pCondiciones As String, ByRef pNumeroCondicion As Int64, ByVal pParametros As List(Of IDataParameter))
        Dim parametroNombre As String
        Dim partes() As String
        Dim entidad As IEntidadBaseDN

        If (pColentidadesBase Is Nothing OrElse pColentidadesBase.Count < 1) Then
            Exit Sub
        End If

        pCondiciones += " ("
        partes = pCampo.Split(".")

        parametroNombre = partes(partes.GetUpperBound(0))

        For Each entidad In pColentidadesBase
            pNumeroCondicion += 1
            pCondiciones += pCampo & "=@" & parametroNombre & pNumeroCondicion & " OR "
            pParametros.Add(ParametrosConstAD.ConstParametroID("@" & parametroNombre & pNumeroCondicion, entidad.ID))
        Next

        pCondiciones = pCondiciones.Substring(0, pCondiciones.Length - 3) & ") AND "
    End Sub
#End Region

End Class

#Region "Importaciones"

Imports System.Collections.Generic
Imports System.Reflection

#End Region

Namespace DN
    Public Interface IInfoTypeInstCampoDN

#Region "Propiedades"
        Property Instancia() As Object
        Property Campo() As FieldInfo
        ReadOnly Property NombreMap() As String
        ReadOnly Property Valor() As Object
        Property InstanciaReferidora() As Object
        Property CampoRefPadre() As InfoTypeInstCampoRefDN
        Property CamposHijos() As List(Of IInfoTypeInstCampoDN)
#End Region

    End Interface
End Namespace

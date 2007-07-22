Imports Framework.DatosNegocio

<Serializable()> _
Public Class DatosMensajeDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mMetadatos As String
    Protected mDatos As Object


#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub
    Public Sub New(ByVal pMetadatos As String, ByVal pDatos As Object)

        Me.CambiarValorVal(Of String)(pMetadatos, mMetadatos)
        Me.CambiarValorPropiedadObjectRef(pDatos, mDatos)

    End Sub

#End Region

#Region "Propiedades"

    Public ReadOnly Property Metadatos() As String
        Get
            Return mMetadatos
        End Get
    End Property

    Public ReadOnly Property Datos() As Object
        Get
            Return mDatos
        End Get
    End Property

  

#End Region

#Region "Métodos de validación"



#End Region

#Region "Métodos"



#End Region

End Class

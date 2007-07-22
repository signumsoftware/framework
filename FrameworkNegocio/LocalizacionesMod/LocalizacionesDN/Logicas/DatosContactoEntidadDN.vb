Imports Framework.DatosNegocio
Imports LocalizacionesDN

<Serializable()> _
Public Class DatosContactoEntidadDN
    Inherits EntidadDN

 
#Region "Atributos"

    Protected mEntidadReferida As IEntidadDN
    Protected mColContactos As ColIDatosContactoDN

#End Region



    Public Sub New()
        Me.CambiarValorRef(Of ColIDatosContactoDN)(New ColIDatosContactoDN, mColContactos)
        Me.modificarEstado = EstadoDatosDN.Inconsistente
    End Sub



#Region "Propiedades"

    Public Property ColContactos() As ColIDatosContactoDN
        Get
            Return mColContactos
        End Get
        Set(ByVal value As ColIDatosContactoDN)
            CambiarValorCol(Of ColIDatosContactoDN)(value, mColContactos)
        End Set
    End Property

    Public Property EntidadReferida() As IEntidadDN
        Get
            Return mEntidadReferida
        End Get
        Set(ByVal value As IEntidadDN)
            CambiarValorRef(Of IEntidadDN)(value, mEntidadReferida)
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarEntidadReferida(ByRef mensaje As String, ByVal entidadReferida As IEntidadDN) As Boolean
        If entidadReferida Is Nothing Then
            mensaje = "La entidad del contacto no puede ser nula"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarEntidadReferida(pMensaje, mEntidadReferida) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class

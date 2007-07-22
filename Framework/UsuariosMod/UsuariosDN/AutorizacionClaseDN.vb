Imports Framework.TiposYReflexion.DN
Imports Framework.DatosNegocio

<Serializable()> _
Public Class AutorizacionClaseDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"

    Protected mTipoAutorizacion As TipoAutorizacionClase
    Protected mVinculoClase As VinculoClaseDN

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal tipoAutorizacion As TipoAutorizacionClase, ByVal vinculoClase As VinculoClaseDN)
        Dim mensaje As String
        mensaje = ""

        If Not ValTipoAutorizacion(mensaje, tipoAutorizacion) Then

        End If

        If Not ValVinculoClase(mensaje, vinculoClase) Then

        End If

        CambiarValorVal(Of TipoAutorizacionClase)(tipoAutorizacion, mTipoAutorizacion)
        CambiarValorRef(Of VinculoClaseDN)(vinculoClase, mVinculoClase)

        modificarEstado = EstadoDatosDN.SinModificar

    End Sub

#End Region

#Region "Propiedades"

    Public Property TipoAutorizacion() As TipoAutorizacionClase
        Get
            Return mTipoAutorizacion
        End Get
        Set(ByVal value As TipoAutorizacionClase)
            Dim mensaje As String
            mensaje = ""
            If ValTipoAutorizacion(mensaje, value) Then
                CambiarValorVal(Of TipoAutorizacionClase)(value, mTipoAutorizacion)
            Else
                Throw New ApplicationExceptionDN(mensaje)
            End If

        End Set
    End Property

    Public Property VinculoClase() As VinculoClaseDN
        Get
            Return mVinculoClase
        End Get
        Set(ByVal value As VinculoClaseDN)
            Dim mensaje As String
            mensaje = ""
            If ValVinculoClase(mensaje, value) Then
                CambiarValorRef(Of VinculoClaseDN)(value, mVinculoClase)
            Else
                Throw New ApplicationExceptionDN(mensaje)
            End If

        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValTipoAutorizacion(ByRef mensaje As String, ByVal tipoAutorizacion As TipoAutorizacionClase) As Boolean
        Return True
    End Function

    Private Function ValVinculoClase(ByRef mensaje As String, ByVal vinculoClase As VinculoClaseDN) As Boolean

        If vinculoClase Is Nothing Then
            mensaje = "El vínculo con la clase no puede ser nulo"
            Return False
        End If

        Return True

    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If Not ValTipoAutorizacion(pMensaje, mTipoAutorizacion) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValVinculoClase(pMensaje, mVinculoClase) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function

#End Region

End Class

<Serializable()> _
Public Class ColAutorizacionClaseDN
    Inherits ArrayListValidable(Of AutorizacionClaseDN)

#Region "Métodos"

    Public Function Autorizado(ByVal tipoAutorizacion As TipoAutorizacionClase, ByVal tipo As System.Type) As Boolean
        For Each autorizacion As AutorizacionClaseDN In Me
            If autorizacion.TipoAutorizacion = tipoAutorizacion AndAlso tipo Is autorizacion.VinculoClase.TipoClase Then
                Return True
            End If
        Next

        Return False
    End Function

#End Region

End Class

Public Enum TipoAutorizacionClase
    recuperar
    modificar
    crear
End Enum

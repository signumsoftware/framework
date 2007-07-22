<Serializable()> _
Public Class RelacionEntidadesDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mEntidadRelacion1 As IEntidadDN
    Protected mEntidadRelacion2 As IEntidadDN
    Protected mTipoRelacionEntidades As TipoRelacionEntidadesDN

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

#End Region

#Region "Propiedades"

    Public Property EntidadRelacion1() As IEntidadDN
        Get
            Return mEntidadRelacion1
        End Get
        Set(ByVal value As IEntidadDN)
            CambiarValorRef(Of IEntidadDN)(value, mEntidadRelacion1)
        End Set
    End Property

    Public Property EntidadRelacion2() As IEntidadDN
        Get
            Return mEntidadRelacion2
        End Get
        Set(ByVal value As IEntidadDN)
            CambiarValorRef(Of IEntidadDN)(value, mEntidadRelacion2)
        End Set
    End Property

    Public Property TipoRelacionEntidades() As TipoRelacionEntidadesDN
        Get
            Return mTipoRelacionEntidades
        End Get
        Set(ByVal value As TipoRelacionEntidadesDN)
            CambiarValorRef(Of TipoRelacionEntidadesDN)(value, mTipoRelacionEntidades)
        End Set
    End Property

#End Region

#Region "Validaciones"

    Public Function ValidarEntidadRelacion(ByRef mensaje As String, ByVal entidadR As IEntidadDN) As Boolean
        If entidadR Is Nothing Then
            mensaje = "La entidad de relación no puede ser nula"
            Return False
        End If

        Return True
    End Function

    Public Function ValidarTipoRelacionEntidades(ByRef mensaje As String, ByVal tipoRE As TipoRelacionEntidadesDN) As Boolean
        If tipoRE Is Nothing Then
            mensaje = "El tipo de relación entre las entidades no puede ser nulo"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As EstadoIntegridadDN
        If Not ValidarEntidadRelacion(pMensaje, mEntidadRelacion1) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarEntidadRelacion(pMensaje, mEntidadRelacion2) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarTipoRelacionEntidades(pMensaje, mTipoRelacionEntidades) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Overrides Function ToString() As String
        If mEntidadRelacion1 IsNot Nothing AndAlso mEntidadRelacion2 IsNot Nothing Then
            Return mEntidadRelacion1.ToString() & " - " & mEntidadRelacion2.ToString()
        End If

        Return String.Empty
    End Function

#End Region


End Class

<Serializable()> _
Public Class TipoRelacionEntidadesDN
    Inherits EntidadTipoDN

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal nombre As String)
        MyBase.New(nombre)
    End Sub

#End Region

End Class

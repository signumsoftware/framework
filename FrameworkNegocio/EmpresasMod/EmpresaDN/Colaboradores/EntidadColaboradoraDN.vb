Imports Framework.DatosNegocio

<Serializable()> _
Public Class EntidadColaboradoraDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mCodigoColaborador As String
    Protected mEntidadAsociada As IEntidadDN
    Protected mDatosAdicionales As IEntidadDN

#End Region

#Region "Propiedades"

    Public Property CodigoColaborador() As String
        Get
            Return mCodigoColaborador
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mCodigoColaborador)
        End Set
    End Property

    <RelacionPropCampoAtribute("mEntidadAsociada")> _
    Public Property EntidadAsociada() As IEntidadDN
        Get
            Return mEntidadAsociada
        End Get
        Set(ByVal value As IEntidadDN)
            CambiarValorRef(Of IEntidadDN)(value, mEntidadAsociada)
        End Set
    End Property

    <RelacionPropCampoAtribute("mDatosAdicionales")> _
    Public Property DatosAdicionales() As IEntidadDN
        Get
            Return mDatosAdicionales
        End Get
        Set(ByVal value As IEntidadDN)
            CambiarValorRef(Of IEntidadDN)(value, mDatosAdicionales)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String
        Dim cadena As String = String.Empty

        If Not String.IsNullOrEmpty(mCodigoColaborador) Then
            cadena = mCodigoColaborador & " - "
        End If

        If mEntidadAsociada IsNot Nothing Then
            cadena = cadena & mEntidadAsociada.ToString()
        End If

        Return cadena
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If String.IsNullOrEmpty(mCodigoColaborador) Then
            pMensaje = "El código del colaborador no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mEntidadAsociada Is Nothing Then
            pMensaje = "La entidad asociada no puede ser nula"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class

<Serializable()> _
Public Class HEEntidadColaboradoraDN
    Inherits HuellaEntidadTipadaDN(Of EntidadColaboradoraDN)

#Region "Atributos"

    Protected mCodigoEmpresaColaboradora As String

#End Region

#Region "Constructores"

    Public Sub New()

    End Sub

    Public Sub New(ByVal entidad As EntidadColaboradoraDN)
        MyBase.New(entidad, HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir)
    End Sub

    Public Sub New(ByVal entidad As Framework.DatosNegocio.HEDN)
        MyBase.New(entidad)
    End Sub

#End Region

#Region "Propiedades"

    Public ReadOnly Property CodigoEmpresaColaboradora() As String
        Get
            Return mCodigoEmpresaColaboradora
        End Get
    End Property

#End Region

#Region "Métodos"

    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN)
        Dim ec As EntidadColaboradoraDN = pEntidad

        If ec Is Nothing Then
            Throw New ApplicationExceptionDN("La empresa colaboradora de la huella no puede ser nula")
        Else
            mCodigoEmpresaColaboradora = ec.CodigoColaborador
            mToSt = ec.ToString()
        End If

        MyBase.AsignarEntidadReferida(pEntidad)
    End Sub

#End Region


End Class

<Serializable()> _
Public Class ColEntidadColaboradoraDN
    Inherits ArrayListValidable(Of EntidadColaboradoraDN)

    Public Function RecuperarEntColxCodigo(ByVal codigo As String) As EntidadColaboradoraDN

    End Function

End Class
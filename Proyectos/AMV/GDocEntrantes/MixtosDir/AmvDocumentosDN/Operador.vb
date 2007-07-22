Imports Framework.Mensajeria.GestorMensajeriaDN

<Serializable()> _
Public Class OperadorDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

#Region "Atributos"
    Protected mColTipoEntNegocio As AmvDocumentosDN.ColTipoEntNegoioDN
    Protected mColDestinos As ColIDestinos
#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal nombre As String, ByVal colTipoEN As ColTipoEntNegoioDN)
        Me.CambiarValorVal(Of String)(nombre, Me.mNombre)
        Me.CambiarValorCol(Of ColTipoEntNegoioDN)(colTipoEN, Me.mColTipoEntNegocio)

        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub

    Public Sub New(ByVal nombre As String, ByVal colTipoEN As ColTipoEntNegoioDN, ByVal colDestinos As ColIDestinos)
        Me.CambiarValorVal(Of String)(nombre, Me.mNombre)
        Me.CambiarValorCol(Of ColTipoEntNegoioDN)(colTipoEN, Me.mColTipoEntNegocio)
        Me.CambiarValorCol(Of ColIDestinos)(colDestinos, Me.mColDestinos)

        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"

    Public Property ColTipoEntNegoio() As AmvDocumentosDN.ColTipoEntNegoioDN
        Get
            Return Me.mColTipoEntNegocio
        End Get
        Set(ByVal value As AmvDocumentosDN.ColTipoEntNegoioDN)
            Me.CambiarValorPropiedadColEntidadRef(value, mColTipoEntNegocio)
        End Set
    End Property

    Public Property ColDestinos() As ColIDestinos
        Get
            Return mColDestinos
        End Get
        Set(ByVal value As ColIDestinos)
            CambiarValorCol(Of ColIDestinos)(value, mColDestinos)
        End Set
    End Property
#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String
        Return mNombre
    End Function

#End Region

End Class

<Serializable()> _
Public Class ColOperadorDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of OperadorDN)

End Class


<Serializable()> _
Public Class HuellaOperadorDN
    Inherits Framework.DatosNegocio.HuellaEntidadTipadaDN(Of OperadorDN)
    Public Sub New()

    End Sub
    Public Sub New(ByVal pOperadorDN As OperadorDN)
        MyBase.New(pOperadorDN, Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir)
    End Sub
End Class

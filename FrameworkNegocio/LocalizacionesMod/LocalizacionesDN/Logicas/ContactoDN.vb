Imports Framework.DatosNegocio

''' <summary>
''' Esta Clase contiene una colección de clases que implementan una forma de contacto
''' y una dirección textual
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public Class ContactoDN
    Inherits EntidadDN

    Protected mColDatosContacto As ColIDatosContactoDN
    Protected mColHEntidades As ColHEDN

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        Me.CambiarValorRef(Of ColIDatosContactoDN)(New ColIDatosContactoDN, mColDatosContacto)
        Me.CambiarValorRef(Of ColHEDN)(New ColHEDN, mColHEntidades)
    End Sub

#End Region

#Region "Propiedades"

    Public Property ColDatosContacto() As ColIDatosContactoDN
        Get
            Return mColDatosContacto
        End Get
        Set(ByVal value As ColIDatosContactoDN)
            Me.CambiarValorRef(Of ColIDatosContactoDN)(value, mColDatosContacto)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColHEntidades")> _
    Public Property ColIHEntidad() As Framework.DatosNegocio.ColHEDN
        Get
            Return Me.mColHEntidades
        End Get
        Set(ByVal value As Framework.DatosNegocio.ColHEDN)
            Me.CambiarValorRef(Of ColHEDN)(value, Me.mColHEntidades)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class

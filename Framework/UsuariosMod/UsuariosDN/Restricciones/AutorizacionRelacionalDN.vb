Imports Framework.TiposYReflexion.DN
Imports Framework.DatosNegocio

<Serializable()> _
 Public Class AutorizacionRelacionalDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"

    Protected mColHuellaRolDN As ColHuellaRolDN
    Protected mTipoRelacioando As VinculoClaseDN
    Protected mColEntidadesRelacionadas As ColIEntidadBaseDN

    Protected mBuscable As Boolean
    Protected mInstanciable As Boolean
    Protected mEditable As Boolean
    Protected mNombreCampoEnVista As String

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        mColHuellaRolDN = New ColHuellaRolDN()
        Me.modificarEstado = EstadoDatosDN.Inconsistente
    End Sub

#End Region

#Region "Propiedades"

    Public Property NombreCampoEnVista() As String
        Get
            Return Me.mNombreCampoEnVista
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mNombreCampoEnVista)
        End Set
    End Property

    Public Property ColHuellaRol() As ColHuellaRolDN
        Get
            Return mColHuellaRolDN
        End Get
        Set(ByVal value As ColHuellaRolDN)
            Me.CambiarValorRef(Of ColHuellaRolDN)(value, mColHuellaRolDN)
        End Set
    End Property

    ''' <summary>
    ''' el tipo relacioando debiera de heredar de entidad de dn y ser de algun modo dn publicadas lo go a lo mejor de
    ''' debiera ser otro objeto mas concreto
    ''' 
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property TipoRelacioando() As VinculoClaseDN
        Get
            Return Me.mTipoRelacioando
        End Get
        Set(ByVal value As VinculoClaseDN)
            Me.CambiarValorRef(Of VinculoClaseDN)(value, mTipoRelacioando)
        End Set
    End Property

    Public Property ColEntidadesRelacionadas() As ColIEntidadBaseDN
        Get
            Return Me.mColEntidadesRelacionadas
        End Get
        Set(ByVal value As ColIEntidadBaseDN)
            Me.CambiarValorRef(Of ColIEntidadBaseDN)(value, mColEntidadesRelacionadas)
        End Set
    End Property

#End Region

End Class

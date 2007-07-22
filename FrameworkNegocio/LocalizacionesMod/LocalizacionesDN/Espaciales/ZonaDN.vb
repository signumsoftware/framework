#Region "Importaciones"
Imports Framework.DatosNegocio
Imports Framework.DatosNegocio.Arboles
#End Region


''' <summary>
''' Clase zona
''' </summary>
''' <remarks></remarks>
<Serializable()> Public Class ZonaDN
    Inherits Arboles.NodoBaseDN

#Region "Atributos"

#End Region

#Region "Constructores"
    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pColSubZonasDN As ColNodosDN)
        Me.mValidadorh = New Framework.DatosNegocio.ValidadorTipos(GetType(ZonaDN), True)
        Me.CambiarValorRef(Of ColNodosDN)(New ColNodosDN, Me.mHijos)
        Me.Hijos.AddRange(pColSubZonasDN)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

    Public Sub New(ByVal pColSubZonasDN As ColZonasALVDN)
        Me.mValidadorh = New Framework.DatosNegocio.ValidadorTipos(GetType(ZonaDN), True)
        Me.CambiarValorRef(Of ColNodosDN)(New ColNodosDN, Me.mHijos)
        Me.Hijos.AddRange(pColSubZonasDN)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub
#End Region

#Region "Propiedades"



    Public ReadOnly Property EstadoModificacion() As Framework.DatosNegocio.EstadoDatosDN
        Get
            Return MyBase.Estado
        End Get
    End Property

#End Region

#Region "Validaciones"

#End Region


End Class


#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Clase Árbol de zonas
''' </summary>
''' <remarks></remarks>
<Serializable()> Public Class ArbolZonasDN
    Inherits EntidadDN

#Region "Atributos"
    Protected mColZonasDN As ColZonasALVDN
#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pColZonasDN As ColZonasALVDN)
        EstablecerPadreCol(pColZonasDN)
        Me.CambiarValorRef(Of ColZonasALVDN)(pColZonasDN, mColZonasDN)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"
    Public Property ColZonasDN() As ColZonasALVDN
        Get
            Return mColZonasDN
        End Get
        Set(ByVal value As ColZonasALVDN)
            Me.CambiarValorRef(Of ColZonasALVDN)(value, mColZonasDN)
        End Set
    End Property

    Public ReadOnly Property EstadoModificacion() As Framework.DatosNegocio.EstadoDatosDN
        Get
            Return MyBase.Estado
        End Get
    End Property
#End Region

#Region "Validaciones"

#End Region

#Region "Métodos"

    Public Sub EstablecerPadreCol(ByRef pCol As ColZonasALVDN)
        Dim Zona As ZonaDN
        For Each Zona In pCol
            Zona.Padre = Nothing
        Next
    End Sub

#End Region

End Class

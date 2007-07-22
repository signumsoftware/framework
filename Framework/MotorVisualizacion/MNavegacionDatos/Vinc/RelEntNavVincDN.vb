Public Class RelEntNavVincDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements MV2DN.IElemtoMap


    Protected mRelacionEntidadesNav As RelacionEntidadesNavDN
    Protected mPropVinc As MV2DN.PropVinc
    Protected mDireccionLectura As DireccionesLectura


    Public Sub New(ByVal pRelacionEntidadesNav As RelacionEntidadesNavDN, ByVal pPropVinc As MV2DN.PropVinc, ByVal pDireccionLectura As DireccionesLectura)
        Me.CambiarValorVal(Of DireccionesLectura)(pDireccionLectura, mDireccionLectura)
        Me.CambiarValorRef(Of RelacionEntidadesNavDN)(pRelacionEntidadesNav, mRelacionEntidadesNav)
        Me.CambiarValorRef(Of MV2DN.PropVinc)(pPropVinc, mPropVinc)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub

    Public ReadOnly Property DireccionLectura() As DireccionesLectura
        Get
            Return Me.mDireccionLectura
        End Get
    End Property
    ReadOnly Property TipoDireccionLecturta() As System.Type
        Get
            Select Case Me.mDireccionLectura
                Case DireccionesLectura.reversa
                    Return mRelacionEntidadesNav.EntidadDatosOrigen.VinculoClase.TipoClase
                Case DireccionesLectura.directa
                    Return mRelacionEntidadesNav.EntidadDatosDestino.VinculoClase.TipoClase
            End Select
        End Get
    End Property

    Public Property RelacionEntidadesNav() As RelacionEntidadesNavDN
        Get
            Return mRelacionEntidadesNav
        End Get
        Set(ByVal value As RelacionEntidadesNavDN)

        End Set
    End Property

    Public Property PropVinc() As MV2DN.PropVinc
        Get
            Return mPropVinc
        End Get
        Set(ByVal value As MV2DN.PropVinc)

        End Set
    End Property

    Public Property Editable() As Boolean Implements MV2DN.IElemtoMap.Editable
        Get
            Return Me.mPropVinc.Map.Editable
        End Get
        Set(ByVal value As Boolean)

        End Set
    End Property

    Public Property Ico() As String Implements MV2DN.IElemtoMap.Ico
        Get
            Return Me.mPropVinc.Map.Ico
        End Get
        Set(ByVal value As String)

        End Set
    End Property

    Public Property NombreVis() As String Implements MV2DN.IElemtoMap.NombreVis
        Get
            Return Me.mPropVinc.Map.NombreVis
        End Get
        Set(ByVal value As String)

        End Set
    End Property
End Class


Public Class ColRelEntNavVincDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of RelEntNavVincDN)
End Class



Public Enum DireccionesLectura
    Directa
    Reversa
    NoProcede
End Enum


<Serializable()> Public Class EntNavVincDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements MV2DN.IElemtoMap


    Protected mColREentNavVincDN As ColRelEntNavVincDN
    Protected mInstanciaVinc As MV2DN.InstanciaVinc

    Public Sub New()
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Inconsistente
    End Sub

    Public Sub New(ByVal pInstanciaVinc As MV2DN.InstanciaVinc)

        Me.CambiarValorRef(Of ColRelEntNavVincDN)(New ColRelEntNavVincDN, mColREentNavVincDN)
        Me.CambiarValorRef(Of MV2DN.InstanciaVinc)(pInstanciaVinc, mInstanciaVinc)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub


    Public ReadOnly Property InstanciaVinc() As MV2DN.InstanciaVinc
        Get
            Return Me.mInstanciaVinc
        End Get
    End Property

    Public Property ColREentNavVincDN() As ColRelEntNavVincDN
        Get
            Return mColREentNavVincDN
        End Get
        Set(ByVal value As ColRelEntNavVincDN)

        End Set
    End Property

    Public Property Editable() As Boolean Implements MV2DN.IElemtoMap.Editable
        Get
            Return mInstanciaVinc.Map.Editable
        End Get
        Set(ByVal value As Boolean)

        End Set
    End Property

    Public Property Ico() As String Implements MV2DN.IElemtoMap.Ico
        Get
            Return mInstanciaVinc.Map.Ico
        End Get
        Set(ByVal value As String)

        End Set
    End Property

    Public Property NombreVis() As String Implements MV2DN.IElemtoMap.NombreVis
        Get
            Return mInstanciaVinc.Map.NombreVis
        End Get
        Set(ByVal value As String)

        End Set
    End Property


    Public Sub Actualizar()

        For Each elemento As MNavegacionDatosDN.RelEntNavVincDN In mColREentNavVincDN

            elemento.PropVinc.InstanciaVincReferida = mInstanciaVinc
        Next


    End Sub



End Class

Public Class ColEntNavVincDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of EntNavVincDN)
End Class



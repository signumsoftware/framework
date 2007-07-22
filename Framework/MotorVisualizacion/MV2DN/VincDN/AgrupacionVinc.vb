Public Class AgrupacionVinc
    Implements IVincElemento


    Protected mInstanciaVinc As InstanciaVinc
    Protected mColAgrupacionVinc As New ColAgrupacionVinc
    Protected mColPropVinc As New ColPropVinc
    Protected mMap As AgrupacionMapDN



    Public Sub New(ByVal pInstanciaVinc As InstanciaVinc, ByVal pMap As AgrupacionMapDN)

        Poblar(pInstanciaVinc, pMap)
    End Sub


    Protected Sub Poblar(ByVal pInstanciaVinc As InstanciaVinc, ByVal pMap As AgrupacionMapDN)
        mInstanciaVinc = pInstanciaVinc
        mMap = pMap
        For Each propMap As PropMapDN In pMap.ColPropMap

            Dim miPropVinc As New PropVinc(mInstanciaVinc, propMap)
            mColPropVinc.Add(miPropVinc)

        Next


        For Each agMap As AgrupacionMapDN In pMap.ColAgrupacionMap

            Dim agVinc As New AgrupacionVinc(mInstanciaVinc, agMap)
            mColAgrupacionVinc.Add(agVinc)

        Next

    End Sub


    'Public Sub FijarDN()


    '    For Each propvinc As PropVinc In Me.mColPropVinc
    '        propvinc.FijarDN()
    '    Next


    '    For Each agvinc As AgrupacionVinc In Me.mColAgrupacionVinc
    '        agvinc.FijarDN()
    '    Next


    'End Sub

    Public ReadOnly Property ColPropVincTotal() As ColPropVinc
        Get
            Dim miColPropVinc As New ColPropVinc

            miColPropVinc.AddRange(Me.mColPropVinc)

            For Each agvinc As AgrupacionVinc In Me.mColAgrupacionVinc
                miColPropVinc.AddRange(agvinc.ColPropVincTotal)
            Next

            Return miColPropVinc


        End Get
    End Property

    Public ReadOnly Property ColPropVinc() As ColPropVinc
        Get
            Return mColPropVinc
        End Get
    End Property



    Public ReadOnly Property InstanciaVinc() As InstanciaVinc
        Get
            Return Me.mInstanciaVinc
        End Get
    End Property

    Public ReadOnly Property Map() As AgrupacionMapDN
        Get
            Return mMap
        End Get
    End Property


    Public ReadOnly Property ColAgrupacionVinc() As ColAgrupacionVinc
        Get
            Return Me.mColAgrupacionVinc
        End Get
    End Property

    Public ReadOnly Property ElementoMap() As ElementoMapDN Implements IVincElemento.ElementoMap
        Get
            Return Me.Map
        End Get
    End Property

    Private ReadOnly Property InstanciaVinc1() As InstanciaVinc Implements IVincElemento.InstanciaVinc
        Get
            Return Me.InstanciaVinc
        End Get
    End Property

    Public ReadOnly Property Eseditable() As Boolean Implements IVincElemento.Eseditable
        Get
            Return Me.mMap.Editable AndAlso Me.mInstanciaVinc.Eseditable
        End Get
    End Property
End Class


Public Class ColAgrupacionVinc
    Inherits List(Of AgrupacionVinc)

End Class
Public Class InstanciaVinc

    Implements IVincElemento



    Protected mMap As InstanciaMapDN
    Protected mdn As Object
    Protected mColAgrupacionVinc As New ColAgrupacionVinc
    Protected mTipo As System.Type
    Protected mIRecuperadorInstanciaMap As IRecuperadorInstanciaMap
    Protected mPropiedadReferidora As PropVinc

    Public Sub New()

    End Sub

    Public Sub New(ByVal pdn As Object, ByVal pMap As InstanciaMapDN, ByVal pIRecuperadorInstanciaMap As IRecuperadorInstanciaMap, ByVal pPropiedadReferidora As PropVinc)
        Me.mIRecuperadorInstanciaMap = pIRecuperadorInstanciaMap
        mPropiedadReferidora = pPropiedadReferidora
        Poblar(CType(pdn, Object).GetType, pMap)
        Me.DN = pdn

    End Sub

    Public Sub New(ByVal pTipo As System.Type, ByVal pMap As InstanciaMapDN, ByVal pIRecuperadorInstanciaMap As IRecuperadorInstanciaMap, ByVal pPropiedadReferidora As PropVinc)
        If pMap Is Nothing Then
            Throw New ApplicationException("el mapeado no puede ser nothing  para el tipo " & pTipo.FullName)
        End If
        mPropiedadReferidora = pPropiedadReferidora
        Me.mIRecuperadorInstanciaMap = pIRecuperadorInstanciaMap
        Poblar(pTipo, pMap)
    End Sub
    Public Property IRecuperadorInstanciaMap() As IRecuperadorInstanciaMap
        Get
            Return mIRecuperadorInstanciaMap
        End Get
        Set(ByVal value As IRecuperadorInstanciaMap)
            mIRecuperadorInstanciaMap = value
        End Set
    End Property






    Public ReadOnly Property Vinculada() As Boolean
        Get
            Return Me.mdn IsNot Nothing
        End Get
    End Property

    Protected Sub Poblar(ByVal pTipo As System.Type, ByVal pMap As InstanciaMapDN)
        mTipo = pTipo
        mMap = pMap
        For Each agMap As AgrupacionMapDN In pMap.ColAgrupacionMap

            Dim agVinc As New AgrupacionVinc(Me, agMap)
            mColAgrupacionVinc.Add(agVinc)

        Next

    End Sub


    Private Sub FijarDN(ByVal value As Object)

        If Not value Is Nothing Then
            Dim o As Object
            o = value
            If o.GetType Is mTipo OrElse Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.HeredaDe(o.GetType, mTipo) OrElse Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.Implementa(o.GetType, mTipo) Then
            Else

                Throw New ApplicationException("tipo incompatible")
            End If

        End If

        mdn = value


        ' trasladarla a los sub mapeados asociados
        For Each pv As MV2DN.PropVinc In Me.ColPropVincTotal
            If pv.RepresentarSubEntidad Then
                pv.InstanciaVincReferida.DN = pv.Value
            End If
        Next





    End Sub



#Region "Propiedades"


    Public ReadOnly Property ColPropVincTotal() As ColPropVinc
        Get
            Dim miColPropVinc As New ColPropVinc

            For Each agvinc As AgrupacionVinc In Me.mColAgrupacionVinc
                miColPropVinc.AddRange(agvinc.ColPropVincTotal)
            Next

            Return miColPropVinc


        End Get
    End Property

    Public Property DN() As Object
        Get
            Return Me.mdn
        End Get
        Set(ByVal value As Object)
            FijarDN(value)
        End Set
    End Property

    Public ReadOnly Property Map() As InstanciaMapDN
        Get
            Return mMap
        End Get
    End Property


    Public ReadOnly Property ColAgrupacionVinc() As ColAgrupacionVinc
        Get
            Return Me.mColAgrupacionVinc
        End Get
    End Property


    Public ReadOnly Property Tipo() As System.Type
        Get
            Return mTipo
        End Get

    End Property

#End Region

    Public ReadOnly Property ElementoMap() As ElementoMapDN Implements IVincElemento.ElementoMap
        Get
            Return Me.Map
        End Get
    End Property

    Private ReadOnly Property InstanciaVinc1() As InstanciaVinc Implements IVincElemento.InstanciaVinc
        Get
            Return Me
        End Get
    End Property


    Public ReadOnly Property Eseditable() As Boolean Implements IVincElemento.Eseditable
        Get
            If Me.mPropiedadReferidora Is Nothing Then

                Return Me.mMap.Editable AndAlso Me.Vinculada

            Else
                Return Me.mMap.Editable AndAlso Me.Vinculada And Me.mPropiedadReferidora.Eseditable
            End If

        End Get
    End Property
End Class

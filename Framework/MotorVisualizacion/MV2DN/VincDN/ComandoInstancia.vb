Public Class ComandoInstancia

    Protected mMap As ComandoMapDN
    Protected mDatos As New Hashtable
    Public Property Datos() As Hashtable
        Get
            Return Me.mDatos
        End Get
        Set(ByVal value As Hashtable)
            Me.mDatos = value
        End Set
    End Property

    Public Property Map() As ComandoMapDN
        Get
            Return mMap
        End Get
        Set(ByVal value As ComandoMapDN)
            mMap = value
        End Set
    End Property


    Public Shared Function CrearComandoBasico(ByVal pComandoBasico As MV2DN.ComandosMapBasicos) As ComandoInstancia

        Dim comando As New ComandoInstancia
    
        comando.Map = new MV2DN.ComandoMapDN(pComandoBasico)
        Return comando

    End Function

End Class




Public Class ColComandoInstancia
    Inherits List(Of ComandoInstancia)

    Public Sub Poblar(ByVal pColComandoMapDN As MV2DN.ColComandoMapDN)



        For Each cm As MV2DN.ComandoMapDN In pColComandoMapDN
            Dim ci As New ComandoInstancia
            ci.Map = cm
            Me.Add(ci)
        Next
    End Sub

End Class
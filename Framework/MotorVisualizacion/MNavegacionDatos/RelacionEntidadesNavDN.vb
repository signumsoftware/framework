<Serializable()> Public Class RelacionEntidadesNavDN
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mEntidadDatosOrigen As EntidadNavDN
    Protected mNombrePropiedad As String
    Protected mEntidadDatosDestino As EntidadNavDN
    Protected mCardinalidad As CardinalidadRelacion

#Region "Constructores"
    Public Sub New()
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Inconsistente
    End Sub

    Public Sub New(ByVal pEntidadDatosOrigen As EntidadNavDN, ByVal pNombrePropiedad As String, ByVal pCardinalidad As CardinalidadRelacion, ByVal pEntidadDatosDestino As EntidadNavDN)

        Me.CambiarValorRef(Of EntidadNavDN)(pEntidadDatosOrigen, mEntidadDatosOrigen)
        Me.CambiarValorRef(Of EntidadNavDN)(pEntidadDatosDestino, mEntidadDatosDestino)
        Me.CambiarValorVal(Of [Enum])(pCardinalidad, mCardinalidad)
        Me.CambiarValorVal(Of String)(pNombrePropiedad, mNombrePropiedad)

        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub


#End Region

#Region "Propiedades"
    Public Property EntidadDatosOrigen() As EntidadNavDN
        Get
            Return mEntidadDatosOrigen
        End Get
        Set(ByVal value As EntidadNavDN)

        End Set
    End Property
    Public Property NombrePropiedad() As String
        Get
            Return mNombrePropiedad
        End Get
        Set(ByVal value As String)

        End Set
    End Property
    Public Property EntidadDatosDestino() As EntidadNavDN
        Get
            Return mEntidadDatosDestino
        End Get
        Set(ByVal value As EntidadNavDN)

        End Set
    End Property
    Public Property Cardinalidad() As CardinalidadRelacion
        Get
            Return mCardinalidad
        End Get
        Set(ByVal value As CardinalidadRelacion)

        End Set
    End Property

    Public ReadOnly Property TipoOrigen() As System.Type
        Get
            Return Me.mEntidadDatosOrigen.VinculoClase.TipoClase
        End Get
    End Property
    Public ReadOnly Property TipoDestino() As System.Type
        Get
            Return Me.mEntidadDatosDestino.VinculoClase.TipoClase
        End Get
    End Property
#End Region

    Public Function DireccionDeLectura(ByVal pTipo As System.Type) As DireccionesLectura

        If pTipo Is Me.mEntidadDatosDestino.VinculoClase.TipoClase Then
            Return DireccionesLectura.Reversa
        ElseIf pTipo Is Me.mEntidadDatosOrigen.VinculoClase.TipoClase Then
            Return DireccionesLectura.Directa
        Else
            Return DireccionesLectura.NoProcede
        End If

    End Function


    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN


        If mEntidadDatosOrigen Is Nothing OrElse mEntidadDatosDestino Is Nothing Then

            pMensaje = "mEntidadDatosOrigen Is Nothing OrElse mEntidadDatosDestino Is Nothing"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente

        End If


        Return MyBase.EstadoIntegridad(pMensaje)



    End Function

End Class



<Serializable()> Public Class ColRelacionEntidadesNavDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of RelacionEntidadesNavDN)




End Class

Public Enum CardinalidadRelacion
    CeroAUno
    CeroAMuchos
End Enum
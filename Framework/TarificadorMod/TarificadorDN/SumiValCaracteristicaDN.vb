Imports Framework.Cuestionario.CuestionarioDN
Imports Framework.Operaciones.OperacionesDN
<Serializable()> _
Public Class SumiValCaracteristicaDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN


    Protected mCaracteristica As CaracteristicaDN
    Protected mITraductor As ITraductorDN
    Protected mIRecSumiValorLN As IRecSumiValorLN
    Protected mValorCacheado As Object


    Public Function GetValor() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.GetValor

        ' se recupera la fuente de datos del suministrador de datos
        ' en este caso un cuestionario resulto y se busca la respuesta para una caracteristica
        ' se pasa la respuesta al traductor el cual devuelve el valor

        Dim respuestasCuest As CuestionarioResueltoDN = Nothing
        For Each dts As Object In mIRecSumiValorLN.DataSoucers
            If TypeOf dts Is CuestionarioResueltoDN Then
                respuestasCuest = dts
            End If

        Next

        If respuestasCuest Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("respuestasCuest no puede ser nothing")
        End If

        Dim respuesta As RespuestaDN = respuestasCuest.ColRespuestaDN.RecuperarxCaracteristica(Me.mCaracteristica)

        '   Dim mivalor As Object = mITraductor.TraducirValor(respuesta.IValorCaracteristicaDN.Valor) ' todo cambio alex
        Dim mivalor As Object = mITraductor.TraducirValor(respuesta.IValorCaracteristicaDN)

        Return mivalor
    End Function

    Public Property IRecuperadorSuministradorValorLN() As IRecSumiValorLN Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.IRecSumiValorLN
        Get
            Return mIRecSumiValorLN
        End Get
        Set(ByVal value As Framework.Operaciones.OperacionesDN.IRecSumiValorLN)
            Me.CambiarValorRef(Of IRecSumiValorLN)(value, Me.mIRecSumiValorLN)

        End Set
    End Property

    Public Property CaracteristicaDN() As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
        Get
            Return Me.mCaracteristica
        End Get
        Set(ByVal value As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)
            Me.CambiarValorRef(Of CaracteristicaDN)(value, Me.mCaracteristica)
        End Set
    End Property

    Public Property ITraductorDN() As ITraductorDN
        Get
            Return mITraductor
        End Get
        Set(ByVal value As ITraductorDN)
            Me.CambiarValorRef(Of ITraductorDN)(value, Me.mITraductor)

        End Set
    End Property

    Public ReadOnly Property ValorCacheado() As Object Implements Operaciones.OperacionesDN.ISuministradorValorDN.ValorCacheado
        Get

        End Get
    End Property

    Public Function RecuperarOrden() As Integer Implements Operaciones.OperacionesDN.ISuministradorValorDN.RecuperarOrden
        Throw New NotImplementedException("Recuperar orden no está implementado para esta clase")
    End Function

    Public Sub Limpiar() Implements Operaciones.OperacionesDN.ISuministradorValorDN.Limpiar

        ' mCaracteristica As CaracteristicaDN
        ' mITraductor As ITraductorDN
        mIRecSumiValorLN = Nothing
        mValorCacheado = Nothing



    End Sub
End Class
<Serializable()> _
Public Class ColSumiValCaracteristicaDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of SumiValCaracteristicaDN)
End Class
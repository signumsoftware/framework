<Serializable()> Public Class ParametroOperacionPr
    Public OperacionRealizada As Framework.Procesos.ProcesosDN.OperacionRealizadaDN
    Public TransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN
    Public IEntidadDN As Framework.DatosNegocio.IEntidadBaseDN
    Public Parametros As Object
    Public ReadOnly Property EntidadEnTransicion() As Framework.DatosNegocio.IEntidadBaseDN
        Get
            If Not TransicionRealizada Is Nothing Then
                Return TransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion

            End If

            If Not OperacionRealizada Is Nothing Then
                Return OperacionRealizada.ObjetoIndirectoOperacion
            End If
        End Get
    End Property
End Class

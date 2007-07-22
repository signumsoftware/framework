Imports Framework.DatosNegocio

<Serializable()> Public Class TransicionRealizadaDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mTransicion As TransicionDN
    Protected mOperacionRealizadaOrigen As OperacionRealizadaDN
    Protected mOperacionRealizadaDestino As OperacionRealizadaDN
    Protected mFechaRealizada As DateTime

#End Region

#Region "Propiedades"

    Public ReadOnly Property EsInicial() As Boolean
        Get
            Return Me.mTransicion.TipoTransicion = TipoTransicionDN.Inicio OrElse Me.mTransicion.TipoTransicion = TipoTransicionDN.InicioDesde OrElse Me.mTransicion.TipoTransicion = TipoTransicionDN.InicioObjCreado OrElse Me.mTransicion.TipoTransicion = TipoTransicionDN.Subordianda

        End Get
    End Property

    Public ReadOnly Property EsFinalizacion() As Boolean
        Get
            'If mOperacionRealizadaOrigen Is Nothing OrElse mOperacionRealizadaDestino Is Nothing OrElse mOperacionRealizadaOrigen.OperacionPadre Is Nothing Then
            '    Return False
            'End If
            'Return mOperacionRealizadaOrigen.OperacionPadre.GUID = mOperacionRealizadaDestino.GUID

            If mOperacionRealizadaOrigen Is Nothing OrElse mOperacionRealizadaOrigen.OperacionPadre Is Nothing Then
                Return False
            Else

                Return mTransicion.OperacionDestino.GUID = mOperacionRealizadaOrigen.OperacionPadre.Operacion.GUID
            End If

        End Get
    End Property

    Public Property Transicion() As TransicionDN
        Get
            Return mTransicion
        End Get
        Set(ByVal value As TransicionDN)
            CambiarValorRef(Of TransicionDN)(value, mTransicion)
        End Set
    End Property

    Public Property OperacionRealizadaOrigen() As OperacionRealizadaDN
        Get
            Return mOperacionRealizadaOrigen
        End Get
        Set(ByVal value As OperacionRealizadaDN)
            CambiarValorRef(Of OperacionRealizadaDN)(value, mOperacionRealizadaOrigen)
        End Set
    End Property

    Public Property OperacionRealizadaDestino() As OperacionRealizadaDN
        Get
            Return mOperacionRealizadaDestino
        End Get
        Set(ByVal value As OperacionRealizadaDN)
            CambiarValorRef(Of OperacionRealizadaDN)(value, mOperacionRealizadaDestino)
        End Set
    End Property

    Public Property FechaRealizada() As DateTime
        Get
            Return mFechaRealizada
        End Get
        Set(ByVal value As DateTime)
            CambiarValorVal(Of DateTime)(value, mFechaRealizada)
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarOpROrigen(ByRef mensaje As String, ByVal opROrigen As OperacionRealizadaDN, ByVal transicion As TransicionDN) As Boolean
        If opROrigen Is Nothing Then
            mensaje = "La operación realizada origen de la transición realizada no puede ser nula"
            Return False
        End If

        If opROrigen.Operacion Is Nothing OrElse Not opROrigen.Operacion.EsIgualRapido(transicion.OperacionOrigen) Then
            mensaje = "No existe concordancia entre la operación de la operación realizada origen, y la operación origen de la transición"
            Return False
        End If

        Return True
    End Function

    Private Function ValidarOpRDestino(ByRef mensaje As String, ByVal opRDestino As OperacionRealizadaDN, ByVal transicion As TransicionDN) As Boolean
        If opRDestino Is Nothing Then
            mensaje = "La operación realizada destino de la transición realizada no puede ser nula"
            Return False
        End If

        If opRDestino.Operacion Is Nothing OrElse Not opRDestino.Operacion.EsIgualRapido(transicion.OperacionDestino) Then
            mensaje = "No existe concordancia entre la operación de la operación realizada destino, y la operación destino de la transición"
            Return False
        End If

        Return True
    End Function

    Private Function ValidarTransicion(ByRef mensaje As String, ByVal transicion As TransicionDN) As Boolean
        If transicion Is Nothing Then
            mensaje = "La transición no puede ser nula"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"


    Public Sub ActualizarEstadoColOPRFinalizadasoEnCursoOpPadreOrigen()


        If Me.EsInicial Then
            Me.OperacionRealizadaOrigen.ActualizarEstadoColOPRFinalizadasoEnCurso(Me)

        ElseIf Me.EsFinalizacion Then
            Me.OperacionRealizadaDestino.ActualizarEstadoColOPRFinalizadasoEnCurso(Me)


        Else

            OperacionRealizadaDestino.OperacionPadre.ActualizarEstadoColOPRFinalizadasoEnCurso(Me)

        End If


    End Sub

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN
        If Not ValidarTransicion(pMensaje, mTransicion) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarOpROrigen(pMensaje, mOperacionRealizadaOrigen, mTransicion) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarOpRDestino(pMensaje, mOperacionRealizadaDestino, mTransicion) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Function TransicionAutorizada() As Boolean

        ' el método debe tener la firma en la que se pase una tr y devuelva true
        If Me.mTransicion.MetodoGuarda Is Nothing Then

            Return True

        Else
            Dim mi As Reflection.MethodInfo = Me.Transicion.MetodoGuarda.RecuperarMethodInfo
            Dim Parametros(0) As Object
            Parametros(0) = Me

            Dim guarda As Object = Activator.CreateInstance(Me.mTransicion.MetodoGuarda.VinculoClase.TipoClase)

            Return mi.Invoke(guarda, Parametros)


        End If



    End Function



#End Region

End Class


<Serializable()> Public Class ColTransicionRealizadaDN
    Inherits ArrayListValidable(Of TransicionRealizadaDN)
    Public Function RecuperarxOperacionDestino(ByVal pOperacionDestino As ProcesosDN.OperacionDN) As TransicionRealizadaDN

        For Each tr As ProcesosDN.TransicionRealizadaDN In Me

            If tr.Transicion.OperacionDestino.GUID = pOperacionDestino.GUID Then
                Return tr
            End If

        Next

        Return Nothing


    End Function


    Public Function ContieneTransicion(ByVal pTransicionDN As ProcesosDN.TransicionDN) As Boolean

        For Each tr As ProcesosDN.TransicionRealizadaDN In Me

            If tr.Transicion.GUID = pTransicionDN.GUID Then
                Return True
            End If

        Next

        Return False


    End Function


End Class
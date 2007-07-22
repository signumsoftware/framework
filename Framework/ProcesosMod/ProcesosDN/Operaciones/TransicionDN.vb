Imports Framework.DatosNegocio

<Serializable()> _
Public Class TransicionDN
    Inherits EntidadDN

#Region "Atributos"
    Protected mAutomatica As Boolean

    Protected mTipoTransicion As TipoTransicionDN
    Protected mOperacionOrigen As OperacionDN
    Protected mOperacionDestino As OperacionDN
    Protected mMetodoGuarda As Framework.TiposYReflexion.DN.VinculoMetodoDN
    Protected mSubordinadaRequerida As Boolean



    'TODO: Falta implementar la condición de Guarda de la transición

#End Region

#Region "Propiedades"
 

    Public Property SubordinadaRequerida() As Boolean
        Get
            Return Me.mSubordinadaRequerida
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(value, mSubordinadaRequerida)
        End Set
    End Property

    Public Property MetodoGuarda() As Framework.TiposYReflexion.DN.VinculoMetodoDN
        Get
            Return Me.mMetodoGuarda
        End Get
        Set(ByVal value As Framework.TiposYReflexion.DN.VinculoMetodoDN)
            Me.CambiarValorVal(value, mMetodoGuarda)
        End Set
    End Property

    Public Property Automatica() As Boolean
        Get
            Return Me.mAutomatica
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(value, mAutomatica)

        End Set
    End Property

    Public Property TipoTransicion() As TipoTransicionDN
        Get
            Return Me.mTipoTransicion
        End Get
        Set(ByVal value As TipoTransicionDN)
            Me.CambiarValorVal(value, mTipoTransicion)
  
        End Set
    End Property

    Public Property OperacionOrigen() As OperacionDN
        Get
            Return mOperacionOrigen
        End Get
        Set(ByVal value As OperacionDN)
            CambiarValorRef(Of OperacionDN)(value, mOperacionOrigen)
        End Set
    End Property

    Public Property OperacionDestino() As OperacionDN
        Get
            Return mOperacionDestino
        End Get
        Set(ByVal value As OperacionDN)
            CambiarValorRef(Of OperacionDN)(value, mOperacionDestino)
        End Set
    End Property



#End Region


#Region "Validaciones"

    Private Function ValidarOperacionDestino(ByRef mensaje As String, ByVal operacionD As OperacionDN) As Boolean
        If operacionD Is Nothing Then
            mensaje = "La transición debe tener un destino no nulo"
            Return False
        End If

        Return True
    End Function

    Private Function ValidarOperacionOrigenPadre(ByRef mensaje As String, ByVal operacionO As OperacionDN, ByVal operacionP As OperacionDN) As Boolean
        If operacionO IsNot Nothing AndAlso operacionP IsNot Nothing Then
            mensaje = "La transición no puede tener una operación origen y una operación padre"
            Return False
        End If

        If operacionO Is Nothing AndAlso operacionP Is Nothing Then
            mensaje = "La transición debe tener una operación origen o una operación padre"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN
        'If Not ValidarOperacionOrigenPadre(pMensaje, mOperacionOrigen, mOperacionPadre) Then
        '    Return EstadoIntegridadDN.Inconsistente
        'End If

        If Not ValidarOperacionDestino(pMensaje, mOperacionDestino) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class

<Serializable()> _
Public Class ColTransicionDN
    Inherits ArrayListValidable(Of TransicionDN)

    Public Function ContieneTransionesAutomaticas() As Boolean
        For Each tran As TransicionDN In Me
            If tran.Automatica Then
                Return True
            End If
        Next
        Return False
    End Function
    Public Function ContieneTransionesDelTipo(ByVal pTipoTransicion As ProcesosDN.TipoTransicionDN) As Boolean
        For Each tran As TransicionDN In Me
            If tran.TipoTransicion = pTipoTransicion Then
                Return True
            End If
        Next
        Return False
    End Function


    Public Function RecuperarColOperaciones(ByVal pRecuperarColOperacionesTipos As RecuperarColOperacionesTipos) As ColOperacionDN
        Dim col As New ColOperacionDN


        For Each tran As TransicionDN In Me

            If pRecuperarColOperacionesTipos = RecuperarColOperacionesTipos.todas OrElse pRecuperarColOperacionesTipos = RecuperarColOperacionesTipos.destino Then
                col.Add(tran.OperacionDestino)
            End If

            If pRecuperarColOperacionesTipos = RecuperarColOperacionesTipos.todas OrElse pRecuperarColOperacionesTipos = RecuperarColOperacionesTipos.origen Then
                col.Add(tran.OperacionOrigen)
            End If
        Next
        Return col
    End Function



    Public Function RecuperarInterseccionTranscionesDeOperaciones(ByVal pColOperacionAutorizadas As ColOperacionDN, ByVal posicionOperacion As RecuperarColOperacionesTipos) As ColTransicionDN


        Dim colt As New ColTransicionDN

        For Each tran As TransicionDN In Me

            Select Case posicionOperacion


                Case RecuperarColOperacionesTipos.todas

                    If pColOperacionAutorizadas.Contiene(tran.OperacionOrigen, CoincidenciaBusquedaEntidadDN.Todos) OrElse pColOperacionAutorizadas.Contiene(tran.OperacionDestino, CoincidenciaBusquedaEntidadDN.Todos) Then
                        colt.Add(tran)
                    End If


                Case RecuperarColOperacionesTipos.destino

                    If pColOperacionAutorizadas.Contiene(tran.OperacionDestino, CoincidenciaBusquedaEntidadDN.Todos) Then
                        colt.Add(tran)
                    End If

                Case RecuperarColOperacionesTipos.origen

                    If pColOperacionAutorizadas.Contiene(tran.OperacionOrigen, CoincidenciaBusquedaEntidadDN.Todos) Then
                        colt.Add(tran)
                    End If


            End Select

        Next


        Return colt

    End Function






    Public Function RecuperarTranscionesDeOperacione(ByVal pOperacion As OperacionDN, ByVal posicionOperacion As RecuperarColOperacionesTipos) As ColTransicionDN


        Dim colt As New ColTransicionDN

        For Each tran As TransicionDN In Me

            Select Case posicionOperacion


                Case RecuperarColOperacionesTipos.todas

                    If pOperacion.GUID = tran.OperacionOrigen.GUID OrElse pOperacion.GUID = tran.OperacionDestino.GUID Then
                        colt.Add(tran)
                    End If


                Case RecuperarColOperacionesTipos.destino


                    If pOperacion.GUID = tran.OperacionDestino.GUID Then
                        colt.Add(tran)
                    End If

                Case RecuperarColOperacionesTipos.origen

                    If pOperacion.GUID = tran.OperacionOrigen.GUID Then
                        colt.Add(tran)
                    End If


            End Select

        Next


        Return colt

    End Function

    Public Function RecuperarTranscionesXTipo(ByVal pTipoTransicion As ProcesosDN.TipoTransicionDN) As ColTransicionDN


        Dim colt As New ColTransicionDN

        For Each tran As TransicionDN In Me

            If tran.TipoTransicion = pTipoTransicion Then
                colt.Add(tran)
            End If

        Next


        Return colt

    End Function
End Class






Public Enum RecuperarColOperacionesTipos
    todas
    destino
    origen
End Enum


Public Enum TipoTransicionDN
    Normal
    Subordianda
    Inicio
    Reactivacion
    InicioDesde
    InicioObjCreado
End Enum
#Region "Importaciones"

Imports System.Collections.Generic
Imports Framework.DatosNegocio

#End Region

Namespace Transacciones
    ''' <summary>Esta clase representa una transaccion logica sobre diversos recursos.</summary>
    ''' <remarks>
    ''' Una transaccion logica puede tener embebidas multiples transacciones sobre diversos recursos. Es responsabilidad
    ''' de la transaccion logica decidir si todas las transacciones salen adelante o si hay que abortar el proceso,
    ''' comunicandoselo en ese caso a todas las transacciones que tiene embebidas.
    ''' </remarks>
    <Serializable()> _
    Public Class TransaccionLogicaProxyLN
        Implements ITransaccionLogicaLN
        'Implements IEstadoTLLN

#Region "Atributos"
        'Transaccion logica de la que hacemos el proxy
        Private mTLDestino As ITransaccionLogicaLN
#End Region

#Region "Constructores"
        ''' <overrides>El constructor esta sobrecargado.</overrides>
        ''' <summary>Constructor por defecto.</summary>
        Public Sub New()
        End Sub

        ''' <summary>Constructor que acepta la transaccion de la que hacemos el proxy.</summary>
        ''' <param name="pTLDestino" type="ITransaccionLogicaLN">
        ''' Transaccion logica de la que hemos hecho el proxy.
        ''' </param>
        Public Sub New(ByVal pTLDestino As ITransaccionLogicaLN)
            Dim mensaje As String = String.Empty

            If (ValDestino(pTLDestino, mensaje) = False) Then
                Throw New ApplicationException(mensaje)
            End If

            mTLDestino = pTLDestino
        End Sub
#End Region

#Region "Propiedades"
        ''' <summary>Obtiene el ID de la transaccion logica que representa a este proxy.</summary>
        ''' <remarks>No se puede asignar el ID (somos un proxy).</remarks>
        Public Property ID() As String Implements ITransaccionLogicaLN.ID
            Get
                Return mTLDestino.ID
            End Get
            Set(ByVal Value As String)

            End Set
        End Property

        ''' <summary>Obtiene o modifica la tabla de transacciones sobre recursos asociadas a esta transaccion logica.</summary>
        ''' <remarks>No se puede asignar las HashTableValidable (somos un proxy).</remarks>
        Public Property TransacionesRecurso() As IDictionary(Of String, ITransaccionRecursoLN) Implements ITransaccionLogicaLN.TransacionesRecurso
            Get
                Return mTLDestino.TransacionesRecurso
            End Get
            Set(ByVal Value As IDictionary(Of String, ITransaccionRecursoLN))

            End Set
        End Property

        ''' <summary>Obtiene la fecha de creacion de la transaccion.</summary>
        Public ReadOnly Property FechaCreacion() As Date Implements ITransaccionLogicaLN.FechaCreacion
            Get
                Return mTLDestino.FechaCreacion
            End Get
        End Property

        ''' <summary>Obtiene o asigna el estado de la transaccion.</summary>
        Public ReadOnly Property Estado() As EstadoTransaccionLN Implements ITransaccionLogicaLN.Estado
            Get
                Return Me.mTLDestino.Estado
            End Get
        End Property
#End Region

#Region "Metodos Validacion"
        ''' <summary>Metodo que valida si la transaccion logica que representamos como proxy es correcta o no.</summary>
        ''' <param name="pTLDestino" type="ITransaccionLogicaLN">
        ''' Transaccion logica que queremos validar.
        ''' </param>
        ''' <param name="pMensaje" type="String">
        ''' Parametro donde se devuelve el mensaje de error en caso de que el controlador de transacciones distribuidas
        ''' sea invalido.
        ''' </param>
        ''' <returns>Si la transaccion logica es valida o no.</returns>
        Public Shared Function ValDestino(ByVal pTLDestino As ITransaccionLogicaLN, ByRef pMensaje As String) As Boolean
            If (pTLDestino Is Nothing) Then
                pMensaje = "Error: la transaccion no puede ser nula."
                Return False
            End If

            Return True
        End Function
#End Region

#Region "Metodos"
        ''' <summary>
        ''' Recupera una transaccion sobre un recurso asociada a la transaccion logica que representa este proxy.
        ''' </summary>
        ''' <param name="pRec" type="IRecursoLN">
        ''' Recurso sobre el que opera la transaccion.
        ''' </param>
        ''' <returns>La transaccion asociada a la transaccion logica que representa el proxy.</returns>
        Public Function RecuperarTransacRecurso(ByVal pRec As IRecursoLN) As ITransaccionRecursoLN Implements ITransaccionLogicaLN.RecuperarTransacRecurso
            Return mTLDestino.RecuperarTransacRecurso(pRec)
        End Function

        ''' <summary>Confirma una transaccion logica.</summary>
        ''' <remarks>No hace nada ya que el proxy no puede confirmar la transaccion.</remarks>
        Public Sub Confirmar() Implements ITransaccionLogicaLN.Confirmar
        End Sub

        ''' <summary>Modifica el estado de la transaccion a inconsistente.</summary>
        ''' <remarks>
        ''' No cancela la transaccion ya que el proxy no puede mas que indicar el error cambiando el estado.
        ''' </remarks>
        Public Sub Cancelar() Implements ITransaccionLogicaLN.Cancelar
            ' Dim modEstadoTL As IEstadoTLLN
            If Me.mTLDestino.Estado = EstadoTransaccionLN.Consistente OrElse Me.mTLDestino.Estado = EstadoTransaccionLN.Iniciada Then
                Me.mTLDestino.Cancelar()
            End If



            'modEstadoTL = mTLDestino
            'modEstadoTL.Estado = EstadoTransaccionLN.Inconsistente
        End Sub
#End Region

#Region "Eventos"
        ''' <summary>Evento que se emite cuando se cancela una transaccion logica.</summary>
        ''' <param name="pSender" type="Object">
        ''' Objeto que emite el evento.
        ''' </param>
        ''' <param name="pE" type="EventArgs">
        ''' Argumentos del evento.
        ''' </param>
        Public Event CancelarTL(ByVal pSender As Object, ByVal pE As System.EventArgs) Implements ITransaccionLogicaLN.CancelarTL
#End Region


    End Class
End Namespace

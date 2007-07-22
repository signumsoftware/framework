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
    Public Class TransaccionLogicaLN
        Implements ITransaccionLogicaLN
        Implements IEstadoTLLN

#Region "Atributos Estaticos"
        'Contador global de ids asignados a transacciones
        Private Shared sID As Double
#End Region

#Region "Atributos"
        'Id de la transaccion logica
        Private mID As String

        'Fecha de creacion de la transaccion logica
        Private mFechaCreacion As DateTime

        'Tabla con las transacciones asociadas a esta transaccion logica
        Private mTransacionesRecurso As Dictionary(Of String, ITransaccionRecursoLN)

        'Controlador de transacciones distribuidas de esta transaccion
        Private mCTD As ICTDLN

        'Estado de la transaccion
        Private mEstadoTransaccion As EstadoTransaccionLN
#End Region

#Region "Constructores"
        ''' <summary>Constructor por defecto.</summary>
        ''' <param name="pCTD" type="ICTDLN">
        ''' Controlador de transacciones distribuidas de esta transaccion logica.
        ''' transaccion.
        ''' </param>
        Public Sub New(ByVal pCTD As ICTDLN)
            Dim mensaje As String = String.Empty

            'Validamos los datos
            If (ValCTL(pCTD, mensaje) = False) Then
                Throw New ApplicationException(mensaje)
            End If

            'Incrementamos el contador global de ids y asignamos el nuevo numero como id a la transaccion logica
            sID += 1
            mID = sID

            mTransacionesRecurso = New Dictionary(Of String, ITransaccionRecursoLN)
            mCTD = pCTD
            mFechaCreacion = Now
            mEstadoTransaccion = EstadoTransaccionLN.Consistente
        End Sub
#End Region

#Region "Propiedades"
        ''' <summary>Obtiene o asigna el id de la transaccion.</summary>
        Public Property ID() As String Implements ITransaccionLogicaLN.ID
            Get
                Return mID
            End Get
            Set(ByVal Value As String)
                '   mID = Value
            End Set
        End Property

        ''' <summary>Obtiene o modifica la tabla de transacciones sobre recursos asociadas a esta transaccion logica.</summary>
        Public Property TransacionesRecurso() As IDictionary(Of String, ITransaccionRecursoLN) Implements ITransaccionLogicaLN.TransacionesRecurso
            Get
                Return mTransacionesRecurso
            End Get
            Set(ByVal Value As IDictionary(Of String, ITransaccionRecursoLN))
                mTransacionesRecurso = Value
            End Set
        End Property

        ''' <summary>Obtiene o asigna el estado de la transaccion.</summary>
        Private Property Estado() As EstadoTransaccionLN Implements IEstadoTLLN.Estado
            Get
                Return Me.mEstadoTransaccion
            End Get
            Set(ByVal Value As EstadoTransaccionLN)
                Me.mEstadoTransaccion = Value
            End Set
        End Property

        ''' <summary>Obtiene la fecha de creacion de la transaccion.</summary>
        Public ReadOnly Property FechaCreacion() As Date Implements ITransaccionLogicaLN.FechaCreacion
            Get
                Return Me.mFechaCreacion
            End Get
        End Property

        ''' <summary>Obtiene el estado de la transaccion.</summary>
        Public ReadOnly Property EstadoTL() As EstadoTransaccionLN Implements ITransaccionLogicaLN.Estado
            Get
                Return Me.mEstadoTransaccion
            End Get
        End Property
#End Region

#Region "Metodos Validacion"
        ''' <summary>Metodo que valida si controlador de transacciones distribuidas es correcto o no.</summary>
        ''' <param name="pCTD" type="ICTDLN">
        ''' Controlador de transacciones distribuidas que queremos validar.
        ''' </param>
        ''' <param name="pMensaje" type="String">
        ''' Parametro donde se devuelve el mensaje de error en caso de que el controlador de transacciones distribuidas
        ''' sea invalido.
        ''' </param>
        ''' <returns>Si el controlador de transacciones distribuidas es valido o no.</returns>
        Public Shared Function ValCTL(ByVal pCTD As ICTDLN, ByRef pMensaje As String) As Boolean
            If (pCTD Is Nothing) Then
                pMensaje = "Error: el coordinador de transacciones distribuidas no puede ser nulo."
                Return False
            End If

            Return True
        End Function
#End Region

#Region "Metodos"
        ''' <summary>Recupera una transaccion sobre un recurso asociada a esta transaccion logica.</summary>
        ''' <param name="pRecurso" type="IRecursoLN">
        ''' Recurso sobre el que opera la transaccion.
        ''' </param>
        ''' <returns>La transaccion asociada a esta transaccion logica</returns>
        Public Function RecuperarTransacRecurso(ByVal pRecurso As IRecursoLN) As ITransaccionRecursoLN Implements ITransaccionLogicaLN.RecuperarTransacRecurso
            Dim transRec As ITransaccionRecursoLN

            For Each transRec In Me.mTransacionesRecurso.Values
                If (transRec.Recurso.ID = pRecurso.ID) Then
                    Return transRec
                End If
            Next

            Return Nothing
        End Function

        ''' <summary>Confirma una transaccion logica.</summary>
        Public Sub Confirmar() Implements ITransaccionLogicaLN.Confirmar
            If mEstadoTransaccion = EstadoTransaccionLN.Inconsistente Then
                Throw New ApplicationException("Error: transaccion en estado inconsistente")

            Else
                mCTD.ConfirmarTransaccion(Me)
            End If
        End Sub

        ''' <summary>Cancela una transaccion logica.</summary>
        Public Sub Cancelar() Implements ITransaccionLogicaLN.Cancelar
            If mEstadoTransaccion = EstadoTransaccionLN.Consistente Then
                mEstadoTransaccion = EstadoTransaccionLN.Inconsistente
                mCTD.CancelarTransaccion(Me)
            End If

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

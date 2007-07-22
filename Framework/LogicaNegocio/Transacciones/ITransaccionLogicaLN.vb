#Region "Importaciones"

Imports System.Collections.Generic
Imports Framework.DatosNegocio

#End Region

Namespace Transacciones
    ''' <summary>
    ''' Esta interfaz proporciona la funcionalidad minima que tienen que implementar las transacciones logicas.
    ''' </summary>
    Public Interface ITransaccionLogicaLN

#Region "Propiedades"
        ''' <summary>Obtiene la fecha de creacion de la transaccion logica.</summary>
        ReadOnly Property FechaCreacion() As DateTime

        ''' <summary>Obtiene o modifica el id de la transaccion logica.</summary>
        Property ID() As String

        ''' <summary>Obtiene o modifica la tabla de transacciones sobre recursos asociadas a esta transaccion logica.</summary>
        Property TransacionesRecurso() As IDictionary(Of String, ITransaccionRecursoLN)

        ''' <summary>Obtiene el estado de la transaccion logica.</summary>
        ReadOnly Property Estado() As EstadoTransaccionLN
#End Region

#Region "Metodos"
        ''' <summary>Recupera una transaccion sobre un recurso asociada a esta transaccion logica.</summary>
        ''' <param name="pRec" type="IRecursoLN">
        ''' Recurso sobre el que opera la transaccion.
        ''' </param>
        ''' <returns>La transaccion asociada a esta transaccion logica</returns>
        Function RecuperarTransacRecurso(ByVal pRec As IRecursoLN) As ITransaccionRecursoLN

        ''' <summary>Confirma una transaccion logica.</summary>
        Sub Confirmar()

        ''' <summary>Cancela una transaccion logica.</summary>
        Sub Cancelar()
#End Region

#Region "Eventos"
        ''' <summary>Evento que se emite cuando se cancela una transaccion logica.</summary>
        ''' <param name="pSender" type="Object">
        ''' Objeto que emite el evento.
        ''' </param>
        ''' <param name="pE" type="EventArgs">
        ''' Argumentos del evento.
        ''' </param>
        Event CancelarTL(ByVal pSender As Object, ByVal pE As EventArgs)
#End Region

    End Interface

    ''' <summary>
    ''' Esta interfaz permite acceder al estado de una transaccion logica para modificarlo.
    ''' </summary>
    ''' <remarks>
    ''' Si accedemos por la interfaz ITransaccionLogicaLN, el estado es de solo lectura, por eso existe esta interfaz.
    ''' </remarks>
    Friend Interface IEstadoTLLN

#Region "Propiedades"
        ''' <summary>Obtiene o modifica el estado de la transaccion logica.</summary>
        Property Estado() As EstadoTransaccionLN
#End Region

    End Interface
End Namespace

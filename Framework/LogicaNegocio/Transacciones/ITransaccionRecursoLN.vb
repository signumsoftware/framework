Namespace Transacciones
    ''' <summary>
    ''' Esta interfaz proporciona la funcionalidad minima que tienen que implementar las transacciones que se ejecutan
    ''' sobre recursos.
    ''' </summary>
    Public Interface ITransaccionRecursoLN

#Region "Propiedades"
        ''' <summary>Obtiene o modifica la transaccion logica padre de esta transaccion.</summary>
        Property TransaccionLogica() As ITransaccionLogicaLN

        ''' <summary>Obtiene o modifica el gestor de transacciones sobre un recurso que se encarga de esta transaccion.</summary>
        Property Gestor() As IGTDRLN

        ''' <summary>Obtiene o modifica el id de la transaccion.</summary>
        Property ID() As String

        'TODO: ESTO QUE ES???
        ''' <summary>Obtiene o modifica .</summary>
        Property DatosTransaccion() As IEntidadesTransaccRecursoLN

        ''' <summary>Obtiene o modifica el recurso sobre el que se ejecuta la transaccion.</summary>
        Property Recurso() As IRecursoLN
#End Region

    End Interface
End Namespace

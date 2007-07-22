Namespace Transacciones

    ''' <summary>
    ''' Esta interface define las operaciones minimas que tiene que proprocionar un gestor de
    ''' transacciones sobre un determinado recurso.
    ''' </summary>
    Public Interface IGTDRLN

#Region "Propiedades"
        ''' <summary>Obtiene el recurso sobre el que creamos las transacciones.</summary>
        ReadOnly Property Recurso() As IRecursoLN
#End Region

#Region "Metodos"
        ''' <summary>Metodo que inicia una transaccion sobre un recurso.</summary>
        ''' <param name="pTLPadre" type="ITransaccionLogicaLN">
        ''' Padre de la transaccion que queremos iniciar.
        ''' </param>
        ''' <returns>La transaccion que hemos iniciado sobre el recurso</returns>
        Function IniciarTransaccion(ByVal pTLPadre As ITransaccionLogicaLN) As ITransaccionRecursoLN

        ''' <summary>Metodo que confirma una transaccion sobre un recurso.</summary>
        ''' <param name="pTR" type="ITransaccionRecursoLN">
        ''' Transaccion que queremos confirmar.
        ''' </param>
        Sub ConfirmarTransaccion(ByVal pTR As ITransaccionRecursoLN)

        ''' <summary>Metodo que cancela una transaccion sobre un recurso.</summary>
        ''' <param name="pTR" type="ITransaccionRecursoLN">
        ''' Transaccion que queremos cancelar.
        ''' </param>
        Sub CancelarTransaccion(ByVal pTR As ITransaccionRecursoLN)
#End Region

    End Interface
End Namespace

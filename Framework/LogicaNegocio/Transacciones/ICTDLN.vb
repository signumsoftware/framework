Namespace Transacciones

    ''' <summary>
    ''' Esta interface define las operaciones minimas que tiene que proprocionar un coordinador de
    ''' transacciones distribuidas.
    ''' </summary>
    Public Interface ICTDLN

#Region "Metodos"
        ''' <summary>Metodo que inicia una transaccion logica.</summary>
        ''' <param name="pTLPadre" type="ITransaccionLogicaLN">
        ''' Padre de la transaccion que queremos iniciar.
        ''' </param>
        ''' <param name="pTLProceso" type="ITransaccionLogicaLN">
        ''' Parametro donde vamos a devolver la transaccion que iniciamos.
        ''' </param>
        Sub IniciarTransaccion(ByRef pTLPadre As ITransaccionLogicaLN, ByRef pTLProceso As ITransaccionLogicaLN)

        ''' <summary>Metodo que confirma una transaccion logica.</summary>
        ''' <param name="pTL" type="ITransaccionLogicaLN">
        ''' Transaccion que queremos confirmar.
        ''' </param>
        Sub ConfirmarTransaccion(ByVal pTL As ITransaccionLogicaLN)

        ''' <summary>Metodo que cancela una transaccion logica.</summary>
        ''' <param name="pTL" type="ITransaccionLogicaLN">
        ''' Transaccion que queremos cancelar.
        ''' </param>
        Sub CancelarTransaccion(ByVal pTL As ITransaccionLogicaLN)
#End Region

    End Interface
End Namespace

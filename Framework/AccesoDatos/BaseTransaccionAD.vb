''' <summary>Esta clase guarda la informacion de una transaccion y su recurso asociado.</summary>
''' <remarks>Uso deprecado. Se debe usar para esta tarea BaseTransaccionV2AD.</remarks>

Public Class BaseTransaccionAD




#Region "Atributos"
    'Transaccion logica que guardamos
    Protected mTL As ITransaccionLogicaLN

    'Recurso sobre el que se desarrolla la transaccion logica
    Protected mRec As IRecursoLN
#End Region

#Region "Constructores"
    ''' <summary>Constructor por defecto con parametros.</summary>
    ''' <param name="pTL" type="ITransaccionLogica">
    ''' ITransaccionLogica que vamos a guardar.
    ''' </param>
    ''' <param name="pRec" type="IRecurso">
    ''' IRecurso sobre el que se desarrolla la transaccion logica.
    ''' </param>
    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
        If (pRec Is Nothing) Then
            Throw New ApplicationException("Error: el recurso no puede ser nulo.")
        End If

        mTL = pTL
        mRec = pRec
    End Sub
#End Region

    Protected Function ObtenerTransaccionDeProceso() As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN
        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
        ObtenerTransaccionDeProceso = Nothing
        ctd.IniciarTransaccion(Me.mTL, ObtenerTransaccionDeProceso)
    End Function

End Class


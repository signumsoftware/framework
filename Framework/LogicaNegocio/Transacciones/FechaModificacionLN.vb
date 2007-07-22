Namespace Transacciones

    ''' <summary>Esta clase permite obtener la fecha de modificacion de una transaccion logica.</summary>
    Public Class FechaModificacionLN

#Region "Metodos"
        ''' <summary>Obtiene la fecha de creacion de modificacion de una transaccion logica.</summary>
        ''' <param name="pTL" type="ITransaccionLogicaLN">
        ''' Transaccion logica de la que queremos obtener su fecha de modificacion.
        ''' </param>
        ''' <returns>Fecha de modificacion de la transaccion logica.</returns>
        Public Shared Function ObtenerFechaModificacion(ByVal pTL As ITransaccionLogicaLN) As DateTime
            If (Not pTL Is Nothing) Then
                Return pTL.FechaCreacion

            Else
                Return Now
            End If
        End Function
#End Region

    End Class
End Namespace

Imports Framework.LogicaNegocios.Transacciones



Public Class CanalLN
    Inherits Framework.ClaseBaseLN.BaseGenericLN

#Region "Contructor"
    Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub
#End Region

    Public Function RecuperarColTipoCanal() As AmvDocumentosDN.ColTipoCanalDN


        Dim tlproc As ITransaccionLogicaLN = Nothing
        ' Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Try
            tlproc = Me.ObtenerTransaccionDeProceso


            ' Precondiciones

            Dim col As New AmvDocumentosDN.ColTipoCanalDN
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            col.AddRange(Me.RecuperarLista(Of AmvDocumentosDN.TipoCanalDN)())

     
            tlproc.Confirmar()
            Return col

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw e
        End Try





    End Function

End Class

Imports Framework.LogicaNegocios.Transacciones
Imports AmvDocumentosDN

''' <summary>
''' clase encargada de recuperar y modificar el arbol de htenr
''' </summary>
''' <remarks></remarks>
Public Class CabeceraNodoTipoEntNegoioLN
    Inherits Framework.ClaseBaseLN.BaseGenericLN

#Region "Contructor"
    Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

    Public Function RecuperarArbolTiposEntNegocio() As CabeceraNodoTipoEntNegoioDN



        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            RecuperarArbolTiposEntNegocio = gi.Recuperar(Of CabeceraNodoTipoEntNegoioDN)("1") ' todo el valor del id debiera salir de una vista o de una cosnsulta o de un valor de configuracion

            tlproc.Confirmar()
            'Return result

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw e
        End Try







    End Function
    Public Function GuardarArbolTiposEntNegocio(ByVal pCabera As CabeceraNodoTipoEntNegoioDN) As CabeceraNodoTipoEntNegoioDN



        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Try
            tlproc = Me.ObtenerTransaccionDeProceso

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            gi.Guardar(pCabera)

            tlproc.Confirmar()
            Return pCabera

        Catch e As Exception
            If (Not (tlproc Is Nothing)) Then
                tlproc.Cancelar()
            End If
            Throw e
        End Try







    End Function

#End Region




End Class

Imports Framework.LogicaNegocios.Transacciones
Imports AmvDocumentosDN


Public Class TipoCanalAD
    Inherits Framework.AccesoDatos.BaseTransaccionAD
    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub
    ''' <summary>
    ''' recupera un objeto relacionENFichero que no esta sociado a ninguan operacion abierta
    ''' </summary>
    ''' <param name="pColTiposEN"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarXNombre(ByVal pNombreTipoCanal As String) As AmvDocumentosDN.TipoCanalDN

        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)


        ' construir la sql y los parametros
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ProcTl = Me.ObtenerTransaccionDeProceso()

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("Nombre", pNombreTipoCanal))



            sql = "Select ID  from tlTipoCanalDN where  Baja<>@Baja  and Nombre=@Nombre "



            Dim id As String
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            id = ej.EjecutarEscalar(sql, parametros)

            If id Is Nothing OrElse id = "" Then
                Return Nothing
            Else
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(ProcTl, mRec)
                RecuperarXNombre = gi.Recuperar(Of AmvDocumentosDN.TipoCanalDN)(id)
            End If


            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try
    End Function
End Class

Imports Framework.Operaciones.OperacionesDN
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.DatosNegocio
Imports Framework.AccesoDatos



Public Class OperacionConfiguradaAD
    ' Inherits Framework.AccesoDatos.BaseTransaccionAD

    '#Region "Constructores"
    '    ''' <summary>Constructor por defecto con parametros.</summary>
    '    ''' <param name="pTL" type="ITransaccionLogica">
    '    ''' ITransaccionLogica que vamos a guardar.
    '    ''' </param>
    '    ''' <param name="pRec" type="IRecurso">
    '    ''' IRecurso sobre el que se desarrolla la transaccion logica.
    '    ''' </param>

    '    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
    '        MyBase.New(pTL, pRec)

    '    End Sub
    '#End Region


    Public Function Recupear(ByVal pTipoOpc As TipoOperacionConfiguradaDN, ByVal pFechaEjecucion As Date) As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN



        'Dim tl As LogicaNegocios.Transacciones.TransaccionLogicaLN = Me.ObtenerTransaccionDeProceso()

        'Try


        Using tr As New Transaccion

            Try
                'ddddddddddddddddddddddddd()


                Dim parametros As List(Of System.Data.IDataParameter)

                Dim sql As String = " select id from tlOperacionConfiguradaDN where idTipoOperacionConfiguradaDN=@idTipoOperacionConfiguradaDN and baja=@Baja and (Periodo_FFinal >=@FechaEjecucion and Periodo_FInicio<=@FechaEjecucion)"
                parametros = New List(Of System.Data.IDataParameter)

                parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroFecha("FechaEjecucion", pFechaEjecucion))
                parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idTipoOperacionConfiguradaDN", pTipoOpc.ID))
                parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))



                Dim dts As DataSet
                Dim ej As Framework.AccesoDatos.Ejecutor
                ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
                dts = ej.EjecutarDataSet(sql, parametros)


                If dts.Tables.Count = 0 OrElse dts.Tables(0).Rows.Count = 0 Then
                    Return Nothing

                Else
                    If dts.Tables(0).Rows.Count <> 1 Then
                        Throw New ApplicationExceptionAD("solo debiera exitir una operación configurada vigente, para una fecha daa y un tipo de operacion")
                    Else
                        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                        Return gi.Recuperar(Of OperacionConfiguradaDN)(dts.Tables(0).Rows(0).Item(0))

                    End If

                End If


            Catch ex As Exception

                Throw New Framework.AccesoDatos.ApplicationExceptionAD("")

            End Try


            tr.Confirmar()


        End Using


        'tl.Confirmar()
        'Catch ex As Exception
        '    tl.Cancelar()
        '    Throw New Framework.AccesoDatos.ApplicationExceptionAD("")
        'End Try



    End Function

End Class

Imports Framework.LogicaNegocios.Transacciones
Imports AmvDocumentosDN
Imports Framework.AccesoDatos


Public Class OperacionEnRelacionENFicheroAD
    Inherits Framework.AccesoDatos.BaseTransaccionAD
    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

    Public Function RecuperarNumDocPendientesClasificaryPostClasificacion(ByVal dts As DataSet) As DataSet
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)


        ' construir la sql y los parametros
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ProcTl = Me.ObtenerTransaccionDeProceso()


            dts = Me.RecupearNumDocPendientesClasificacionXTipoCanal(dts)
            dts = Me.RecupearNumDocPendientesPostClasificacionXTipoEntidadNegocio(dts)


            ProcTl.Confirmar()

            Return dts

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw
        End Try
    End Function

    Public Function RecupearNumDocPendientesPostClasificacionXTipoEntidadNegocio(ByVal dts As DataSet) As DataSet

        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)


        ' construir la sql y los parametros
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ProcTl = Me.ObtenerTransaccionDeProceso()


            sql = "Select  *  from vwNumDocPendientesPostClasificacionXTipoEntidadNegocio "


            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            RecupearNumDocPendientesPostClasificacionXTipoEntidadNegocio = ej.EjecutarDataSet("vwNumDocPendientesPostClasificacionXTipoEntidadNegocio", sql, parametros, dts, SchemaType.Source, MissingSchemaAction.Add)

            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try

    End Function


    Public Function RecupearNumDocPendientesClasificacionXTipoCanal(ByVal dts As DataSet) As DataSet

        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)


        ' construir la sql y los parametros
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ProcTl = Me.ObtenerTransaccionDeProceso()


            sql = "Select  *  from vwNumDocPendientesClasificacionXTipoCanal "


            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            RecupearNumDocPendientesClasificacionXTipoCanal = ej.EjecutarDataSet("vwNumDocPendientesClasificacionXTipoCanal", sql, parametros, dts, SchemaType.Source, MissingSchemaAction.Add)

            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try

    End Function

    ''' <summary>
    ''' recupera una operacion que fue solicitada a un uauario sin que este cerrara la operacion
    ''' </summary>
    ''' <param name="pOperador"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarOperacionEnCursoPara(ByVal pOperador As OperadorDN, ByRef lanzarExcepcion As Boolean) As OperacionEnRelacionENFicheroDN



        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)


        ' construir la sql y los parametros
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ProcTl = Me.ObtenerTransaccionDeProceso()

            If pOperador Is Nothing OrElse pOperador.ID Is Nothing OrElse pOperador.ID = "" Then
                Throw New ApplicationExceptionAD("El operador no puede ser nothing o debe estar dado de alta en el sistema")
            End If

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("@Baja", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("@Cancelada", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("@idOperador", pOperador.ID))
            ' parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("@idTipoOperacionREnF", AmvDocumentosDN.TipoOperacionREnF.Asignar))

            '  sql = "Select  ID  from tlOperacionEnRelacionENFicheroDN where Baja<>@Baja and idOperador=@idOperador and idTipoOperacionREnF=@idTipoOperacionREnF and ff is null"
            sql = "Select  ID  from tlOperacionEnRelacionENFicheroDN where Cancelada<>@Cancelada and Baja<>@Baja and idOperador=@idOperador  and Periodo_FFinal is null" ' and ff is null que no este cerrada, que no este en baja y que este fijada a el operador


            Dim dtsIds As DataSet
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            dtsIds = ej.EjecutarDataSet(sql, parametros)

            Select Case dtsIds.Tables(0).Rows.Count
                Case Is = 0
                    RecuperarOperacionEnCursoPara = Nothing

                Case Is = 1
                    Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(ProcTl, mRec)
                    RecuperarOperacionEnCursoPara = gi.Recuperar(Of OperacionEnRelacionENFicheroDN)(dtsIds.Tables(0).Rows(0).Item("id"))

                Case Is > 1
                    If lanzarExcepcion Then
                        Throw New ApplicationExceptionAD("error de integridad en la base de datos un operador no debe tener más de una operación activa Nº:" & dtsIds.Tables(0).Rows.Count)
                    Else
                        RecuperarOperacionEnCursoPara = Nothing
                        lanzarExcepcion = True
                    End If

            End Select

            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try
    End Function

    ''' <summary>
    ''' recupera una operacion creada no clasificada
    ''' </summary>
    ''' <param name="pOperador"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarRelacionPendienteClasificacion(ByVal pOperador As OperadorDN, ByVal idCanal As String) As RelacionENFicheroDN



        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)


        ' construir la sql y los parametros
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ProcTl = Me.ObtenerTransaccionDeProceso()

            If pOperador Is Nothing OrElse pOperador.ID Is Nothing OrElse pOperador.ID = "" Then
                Throw New ApplicationExceptionAD("El operador no puede ser nothing o debe estar dado de alta en el sistema")
            End If

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("@Baja", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("@Cancelada", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroInteger("@idEstadosRelacionENFichero", EstadosRelacionENFichero.Creada))

            If idCanal Is Nothing OrElse idCanal = "" Then
                'sql = "Select top 1 idRel  from vwRelacionesAClasificar where Cancelada<>@Cancelada and Baja<>@Baja order by FIRelacion"
                sql = "Select top 1 idRel  from vwRelacionesxRecuperarPrioridad where Cancelada<>@Cancelada and Baja<>@Baja and idEstadosRelacionENFichero=@idEstadosRelacionENFichero order by FPrioridad"
            Else
                parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("@idTipoCanal", idCanal))
                'sql = "Select top 1 idRel  from vwRelacionesAClasificar where Cancelada<>@Cancelada and Baja<>@Baja and idTipoCanal=@idTipoCanal order by FIRelacion"
                sql = "Select top 1 idRel  from vwRelacionesxRecuperarPrioridad where Cancelada<>@Cancelada and Baja<>@Baja and idTipoCanal=@idTipoCanal and idEstadosRelacionENFichero=@idEstadosRelacionENFichero order by FPrioridad"
            End If

            Dim idRel As String
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            idRel = ej.EjecutarEscalar(sql, parametros)

            If idRel IsNot Nothing AndAlso idRel <> "" Then

                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(ProcTl, mRec)
                RecuperarRelacionPendienteClasificacion = gi.Recuperar(Of RelacionENFicheroDN)(idRel)

            End If

            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try
    End Function


    Public Function RecuperarRelacionPostClasificacion(ByVal pOperador As OperadorDN, ByVal pColTipoEntNegoio As ColTipoEntNegoioDN, ByVal pIdentificadorentidadNegocio As String) As RelacionENFicheroDN





        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)


        ' construir la sql y los parametros
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ProcTl = Me.ObtenerTransaccionDeProceso()

            If pOperador Is Nothing OrElse pOperador.ID Is Nothing OrElse pOperador.ID = "" Then
                Throw New ApplicationExceptionAD("El operador no puede ser nothing o debe estar dado de alta en el sistema")
            End If

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("@Baja", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("@Cancelada", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroInteger("@idEstadosRelacionENFichero", EstadosRelacionENFichero.Clasificando))



            If pColTipoEntNegoio Is Nothing OrElse pColTipoEntNegoio.Count = 0 Then
                sql = "Select top 1 idRel  from vwRelacionesxRecuperarPrioridad where Cancelada<>@Cancelada and Baja<>@Baja and idEstadosRelacionENFichero=@idEstadosRelacionENFichero "
            Else
                Dim condiciones As String
                Dim numeroCondicion As Int64
                Framework.AccesoDatos.ParametrosHelperAD.ProcesarColEntidadesBase(pColTipoEntNegoio, "idTipoEntNegocioReferidora", condiciones, numeroCondicion, parametros)
                sql = "Select top 1 idRel  from vwRelacionesxRecuperarPrioridad where Cancelada<>@Cancelada and Baja<>@Baja and " & condiciones.Substring(1, condiciones.Length - 5) & "  and idEstadosRelacionENFichero=@idEstadosRelacionENFichero "

            End If


            If Not String.IsNullOrEmpty(pIdentificadorentidadNegocio) Then
                parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("@IdEntNeg", pIdentificadorentidadNegocio))
                sql = sql + " and idEntdadiNeg=@IdEntNeg"
            End If


            sql = sql + " order by FPrioridad"



            Dim idRel As String
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            idRel = ej.EjecutarEscalar(sql, parametros)

            If idRel IsNot Nothing AndAlso idRel <> "" Then

                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(ProcTl, mRec)
                RecuperarRelacionPostClasificacion = gi.Recuperar(Of RelacionENFicheroDN)(idRel)

            End If


            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try
    End Function







    Public Function RecuperarOperacionCerrradaEnEstado(ByVal pOperador As OperadorDN, ByVal estado As EstadosRelacionENFichero) As OperacionEnRelacionENFicheroDN



        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)


        ' construir la sql y los parametros
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ProcTl = Me.ObtenerTransaccionDeProceso()

            If pOperador Is Nothing OrElse pOperador.ID Is Nothing OrElse pOperador.ID = "" Then
                Throw New ApplicationExceptionAD("El operador no puede ser nothing o debe estar dado de alta en el sistema")
            End If

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("@Baja", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("@Cancelada", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("@idOperador", pOperador.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("@idTipoOperacionREnFDN", estado))

            '  sql = "Select  ID  from tlOperacionEnRelacionENFicheroDN where Baja<>@Baja and idOperador=@idOperador and idTipoOperacionREnF=@idTipoOperacionREnF and ff is null"
            sql = "Select  ID  from tlOperacionEnRelacionENFicheroDN where Cancelada<>@Cancelada and Baja<>@Baja and idOperador=@idOperador  and ff is Not null and  idTipoOperacionREnFDN=@idTipoOperacionREnFDN " ' 


            Dim dtsIds As DataSet
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            dtsIds = ej.EjecutarDataSet(sql, parametros)

            Select Case dtsIds.Tables(0).Rows.Count
                Case Is = 0
                    RecuperarOperacionCerrradaEnEstado = Nothing

                Case Is = 1
                    Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(ProcTl, mRec)
                    RecuperarOperacionCerrradaEnEstado = gi.Recuperar(Of OperacionEnRelacionENFicheroDN)(dtsIds.Tables(0).Rows(0).Item("id"))

                Case Is > 1
                    Throw New ApplicationExceptionAD("Error de integriad: En la base de datos un operador no debe tener más de una operación activa; operaciones actuales Nº:" & dtsIds.Tables(0).Rows.Count)

            End Select


            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try
    End Function



    Public Sub Guardar(ByVal pOperacion As OperacionEnRelacionENFicheroDN)




        ' construir la sql y los parametros
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing



        Try

            ProcTl = Me.ObtenerTransaccionDeProceso()

            If pOperacion Is Nothing Then
                Throw New ApplicationExceptionAD("La Operación no puede ser nothing ")
            End If

            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(ProcTl, Me.mRec)
            gi.Guardar(pOperacion)

            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try
    End Sub



End Class

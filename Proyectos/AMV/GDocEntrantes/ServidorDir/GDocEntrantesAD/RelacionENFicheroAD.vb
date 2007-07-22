Imports Framework.LogicaNegocios.Transacciones
Imports AmvDocumentosDN

Public Class RelacionENFicheroAD
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
    Public Function RecuperarPrimeraRelacionENFichero(ByVal pColTiposEN As ColTipoEntNegoioDN) As RelacionENFicheroDN

        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)


        ' construir la sql y los parametros
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ProcTl = Me.ObtenerTransaccionDeProceso()

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("OpBaja", True))
            '    parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("OPFf", null))

            Dim parametrosColTiposEN As String = ""

            If Not pColTiposEN Is Nothing Then

                Framework.AccesoDatos.ParametrosHelperAD.ProcesarColEntidadesBase(pColTiposEN, "idTipoEntNegocioReferidora", parametrosColTiposEN, 0, parametros)

            End If

            ' que la operacion y la relacion no esten en baja, y que no esté asociada a una operacion en curso es decir sin asignar la fecha final
            If parametrosColTiposEN.Length <> 0 Then
                sql = "Select top 1 ID  from vwRelEntFicheroSel where " & parametrosColTiposEN & " and Baja<>@Baja  and OpBaja<>@OpBaja and OPFf is Null order by  FechaModificacion "
            Else
                sql = "Select top 1 ID  from vwRelEntFicheroSel where Baja<>@Baja and (OpBaja<>@OpBaja or OpBaja is null )and OPFf is Null order by  FechaModificacion "
            End If

            Dim id As String
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            id = ej.EjecutarEscalar(sql, parametros)

            If id Is Nothing OrElse id = "" Then
                Return Nothing
            Else
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(ProcTl, mRec)
                RecuperarPrimeraRelacionENFichero = gi.Recuperar(Of RelacionENFicheroDN)(id)
            End If


            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try
    End Function


    Public Function RecuperarPrimeraRelacionENFicheroEnEstado(ByVal pColTiposEN As ColTipoEntNegoioDN, ByVal pEstadoRF As EstadosRelacionENFichero) As RelacionENFicheroDN

        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)


        ' construir la sql y los parametros
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ProcTl = Me.ObtenerTransaccionDeProceso()

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("OpBaja", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroInteger("idEstadosRelacionENFichero", pEstadoRF))

            Dim parametrosColTiposEN As String = ""

            If Not pColTiposEN Is Nothing Then

                Framework.AccesoDatos.ParametrosHelperAD.ProcesarColEntidadesBase(pColTiposEN, "idTipoEntNegocioReferidora", parametrosColTiposEN, 0, parametros)

            End If

            ' que la operacion y la relacion no esten en baja, y que no esté asociada a una operacion en curso es decir sin asignar la fecha final
            If parametrosColTiposEN.Length <> 0 Then
                sql = "Select top 1 ID  from vwRelEntFicheroSel where " & parametrosColTiposEN & " and Baja<>@Baja  and OpBaja<>@OpBaja and OPFf  is Not Null and idEstadosRelacionENFichero=@idEstadosRelacionENFichero order by  FechaModificacion "
            Else
                sql = "Select top 1 ID  from vwRelEntFicheroSel where Baja<>@Baja and (OpBaja<>@OpBaja or OpBaja is null )and OPFf  is Not Null and idEstadosRelacionENFichero=@idEstadosRelacionENFichero order by  FechaModificacion "
            End If

            Dim id As String
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            id = ej.EjecutarEscalar(sql, parametros)

            If id Is Nothing OrElse id = "" Then
                Return Nothing
            Else
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(ProcTl, mRec)
                RecuperarPrimeraRelacionENFicheroEnEstado = gi.Recuperar(Of RelacionENFicheroDN)(id)
            End If


            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try
    End Function

End Class

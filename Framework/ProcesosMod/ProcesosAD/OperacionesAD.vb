Imports Framework.DatosNegocio
Imports Framework.Procesos.ProcesosDN
Imports framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos

Public Class OperacionesAD

#Region "Métodos"


    Public Function RecuperarTodasOperaciones() As ColOperacionDN
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim colRes As New ColOperacionDN


        ' TODO: alex este codigo se pouede obtimizr mucho tendria que haber un recuperar por id y  nombre y  guid BASE

        Using tr As New Transaccion()



            parametros = New List(Of System.Data.IDataParameter)

            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

 

            sql = "Select ID from tlOperacionDN where  Baja<>@Baja"




            Dim dts As DataSet
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            Dim dr As Data.DataRow
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            For Each dr In dts.Tables(0).Rows
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                colRes.Add(gi.Recuperar(Of OperacionDN)(dr(0)))
            Next

            tr.Confirmar()

            Return colRes

        End Using
    End Function



    Public Function RecuperarTransiciones(ByVal pTipoDN As System.Type, ByVal pTipoTransicion As TipoTransicionDN) As ColTransicionDN
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim colRes As New ColTransicionDN()

        Using tr As New Transaccion()

            Dim vc As New Framework.TiposYReflexion.DN.VinculoClaseDN(pTipoDN)


            parametros = New List(Of System.Data.IDataParameter)

            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("TipoDN", vc.NombreClase))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

            If Not pTipoTransicion = -1 Then
                parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroInteger("TipoTransicion", pTipoTransicion))

                sql = "Select distinct ID from vwTransicionesxTipoDN where (NombreClaseOrigen=@TipoDN or NombreClaseDestino=@TipoDN )and TipoTransicion=@TipoTransicion and Baja<>@Baja"

            Else

                sql = "Select distinct ID from vwTransicionesxTipoDN where (NombreClaseOrigen=@TipoDN or NombreClaseDestino=@TipoDN ) and Baja<>@Baja"

            End If


            Dim dts As DataSet
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            Dim dr As Data.DataRow
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            For Each dr In dts.Tables(0).Rows
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                colRes.Add(gi.Recuperar(Of TransicionDN)(dr(0)))
            Next

            tr.Confirmar()

            Return colRes

        End Using

    End Function

    Public Function RecuperarColProcesosActivos(ByVal pHuellaEntidadDatos As HEDN) As ColOperacionRealizadaDN
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim colRes As New ColOperacionRealizadaDN()

        Using tr As New Transaccion()
            parametros = New List(Of System.Data.IDataParameter)


            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroInteger("TipoTransicion1", TipoTransicionDN.Inicio))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroInteger("TipoTransicion2", TipoTransicionDN.InicioDesde))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroInteger("TipoTransicion3", TipoTransicionDN.InicioObjCreado))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))


            If String.IsNullOrEmpty(pHuellaEntidadDatos.GUIDReferida) Then
                parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("IdEntidadReferida", pHuellaEntidadDatos.IdEntidadReferida))
                parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("TipoEntidadReferidaFullName", pHuellaEntidadDatos.TipoEntidadReferida.FullName))
                sql = "Select distinct idOperacionRealizadaDestino from vwOperacionesRealizadasActivas where TipoEntidadReferidaFullName=@TipoEntidadReferidaFullName and IdEntidadReferida=@IdEntidadReferida and (TipoTransicion=@TipoTransicion1 or  TipoTransicion=@TipoTransicion2 or  TipoTransicion=@TipoTransicion3) and Baja<>@Baja"

            Else
                parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("GUIDReferida", pHuellaEntidadDatos.GUIDReferida))
                sql = "Select distinct idOperacionRealizadaDestino from vwOperacionesRealizadasActivas where GUIDReferida=@GUIDReferida and  (TipoTransicion=@TipoTransicion1 or  TipoTransicion=@TipoTransicion2 or  TipoTransicion=@TipoTransicion3) and Baja<>@Baja"

            End If



            Dim dts As DataSet
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            Dim dr As Data.DataRow
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            For Each dr In dts.Tables(0).Rows
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                colRes.Add(gi.Recuperar(Of OperacionRealizadaDN)(dr(0)))
            Next

            tr.Confirmar()

            Return colRes

        End Using

    End Function

    Public Function RecuperarTransicionesSiguientesPosibles(ByVal pOperacionOrigen As OperacionDN) As ColTransicionDN

        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim colRes As New ColTransicionDN()

        Using tr As New Transaccion()

            parametros = New List(Of System.Data.IDataParameter)

            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("idOperacionOrigen", pOperacionOrigen.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))
            sql = "Select ID from tlTransicionDN where idOperacionOrigen=@idOperacionOrigen and Baja<>@Baja"

            Dim dts As DataSet
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            Dim dr As Data.DataRow
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            For Each dr In dts.Tables(0).Rows
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                colRes.Add(gi.Recuperar(Of ProcesosDN.TransicionDN)(dr(0)))
            Next

            tr.Confirmar()

            Return colRes

        End Using

    End Function

    Public Function RecuperarEjecutorCliente(ByVal nombreCliente As String) As EjecutoresDeClienteDN
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim ejecutorCliente As EjecutoresDeClienteDN = Nothing

        Using tr As New Transaccion()

            parametros = New List(Of System.Data.IDataParameter)

            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("Nombre", nombreCliente))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))
            sql = "Select ID from vwEjecutorClientexNombreCliente where Nombre=@Nombre and Baja<>@Baja"

            Dim dts As DataSet
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de un cliente con el mismo nombre")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                ejecutorCliente = gi.Recuperar(Of Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN)(dts.Tables(0).Rows(0)(0))
            End If

            tr.Confirmar()

            Return ejecutorCliente

        End Using

    End Function



    Public Function EliminarTRyOPREnTablasActivas(ByVal RutaaEliminar As String, ByRef regTrAfectados As Int64, ByRef regOprAfectados As Int64) As DataSet

        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim ej As Framework.AccesoDatos.Ejecutor


        Using tr As New Transaccion()




            ' obtener la lsita de ids a eliminar
            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("@OpPRS", RutaaEliminar & "%"))
            sql = "Select ID from vwProcesosTrOprOrigenOprDestino where OpoRS like @OpPRS and  OpdRS like @OpPRS"

            Dim dts As DataSet
            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            ' convertir a una cadena de texto separada por comas
            Dim cadenaIds As String = ""
            For Each dr As DataRow In dts.Tables(0).Rows
                cadenaIds += dr(0) & ","
            Next
            cadenaIds = cadenaIds.Substring(0, cadenaIds.Length - 1)

            ' eliminar las Relaciones de transiciones realizadas
            sql = "DELETE FROM trtlOperacionRealizadaDNColSubTRIniciadasXtlTransicionRealizadaDN  where idptlTransicionRealizadaDN IN (" & cadenaIds & ")"
            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            regTrAfectados = ej.EjecutarNoConsulta(sql, Nothing)




            ' eliminar las transiciones realizadas
            sql = "DELETE FROM tlTransicionRealizadaDN  where id IN (" & cadenaIds & ")"
            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            regTrAfectados = ej.EjecutarNoConsulta(sql, Nothing)



            ' eliminar las operaciones realizadas

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("@OpPRS", RutaaEliminar & "%"))
            sql = "DELETE FROM tlOperacionRealizadaDN  where  RutaSubordinada like @OpPRS"

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            regOprAfectados = ej.EjecutarNoConsulta(sql, parametros)

            tr.Confirmar()


        End Using


    End Function


#End Region

End Class

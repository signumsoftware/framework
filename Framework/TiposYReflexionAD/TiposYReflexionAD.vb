Imports framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos
Imports Framework.TiposYReflexion.DN

Public Class TiposYReflexionAD

#Region "Métodos"

    Public Function RecuperarVinculoMetodo(ByVal metodoInfo As System.Reflection.MethodInfo) As VinculoMetodoDN
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim vinculoClaseAux As VinculoClaseDN
        Dim vinculoM As VinculoMetodoDN = Nothing

        Using tr As New Transaccion()
            parametros = New List(Of System.Data.IDataParameter)

            vinculoClaseAux = New VinculoClaseDN(metodoInfo.ReflectedType)

            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("NombreMetodo", metodoInfo.Name))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("NombreClase", vinculoClaseAux.NombreClase))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

            sql = "Select ID from vwVinculosMetodo where NombreClase=@NombreClase and NombreMetodo=@NombreMetodo and Baja<>@Baja"

            Dim dts As DataSet
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de un método con el mismo nombre en la misma clase")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                vinculoM = gi.Recuperar(Of VinculoMetodoDN)(dts.Tables(0).Rows(0).Item(0))
            End If

            tr.Confirmar()

            Return vinculoM

        End Using

    End Function

    Public Function RecuperarVinculoClase(ByVal tipo As System.Type) As VinculoClaseDN
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim vinculoC As VinculoClaseDN = Nothing
        Dim vinculoClaseAux As VinculoClaseDN

        Using tr As New Transaccion()
            vinculoClaseAux = New VinculoClaseDN(tipo)

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("NombreClase", vinculoClaseAux.NombreClase))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

            sql = "Select ID from tlVinculoClaseDN where NombreClase=@NombreClase and Baja<>@Baja"

            Dim dts As DataSet
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de una clase con el mismo nombre")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                vinculoC = gi.Recuperar(Of VinculoClaseDN)(dts.Tables(0).Rows(0).Item(0))
            End If

            tr.Confirmar()

            Return vinculoC

        End Using

    End Function

#End Region

End Class

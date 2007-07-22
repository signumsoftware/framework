Imports Framework.Usuarios.DN
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.AccesoDatos.MotorAD.AD
Imports Framework.TiposYReflexion.DN

Public Class MetodoSistemaAD

    Public Function RecuperarMetodos() As IList(Of MetodoSistemaDN)
        Dim motor As GestorInstanciacionLN
        Dim adMotor As AccesorMotorAD
        Dim tlProc As ITransaccionLogicaLN = Nothing
        Dim lista As ArrayList
        Dim listaMetodos As ArrayList
        Dim metodos As List(Of MetodoSistemaDN)
        Dim i As Integer

        Using tr As New Transaccion()
            'Recuperacion de la col de ids
            adMotor = New AccesorMotorAD(Transaccion.Actual, Recurso.Actual, New ConstructorAL(GetType(MetodoSistemaDN)))

            ' lista = adMotor.BuscarGenericoIDS("tlMetodoSistemaDN", Nothing)
            lista = adMotor.BuscarGenericoIDS(GetType(MetodoSistemaDN), Nothing)

            ' InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo("AutorizacionDN", "Framework.Autorizacion.DatosNegocio.MetodoSistemaDN", ensamblado, tipo)

            'Recuperamos la lista de permisos
            listaMetodos = New ArrayList
            metodos = New List(Of MetodoSistemaDN)

            If (lista.Count > 0) Then
                motor = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                listaMetodos = motor.Recuperar(lista, GetType(MetodoSistemaDN), Nothing)
            End If

            For i = 0 To listaMetodos.Count - 1
                metodos.Add(listaMetodos(i))
            Next

            tr.Confirmar()

            Return metodos

        End Using

    End Function


    Public Function RecuperarMetodoSistema(ByVal metodoInfo As System.Reflection.MemberInfo) As MetodoSistemaDN
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim vinculoClaseAux As VinculoClaseDN
        Dim metodoSistema As MetodoSistemaDN = Nothing

        Using tr As New Transaccion()
            parametros = New List(Of System.Data.IDataParameter)

            vinculoClaseAux = New VinculoClaseDN(metodoInfo.ReflectedType)

            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("NombreMetodo", metodoInfo.Name))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("NombreClase", vinculoClaseAux.NombreClase))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

            sql = "Select ID from vwMetodosSistema where NombreClase=@NombreClase and NombreMetodo=@NombreMetodo and Baja<>@Baja"

            Dim dts As DataSet
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New Framework.AccesoDatos.ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de un método de sistema con el mismo nombre en la misma clase")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                metodoSistema = gi.Recuperar(Of MetodoSistemaDN)(dts.Tables(0).Rows(0).Item(0))
            End If

            tr.Confirmar()

            Return metodoSistema

        End Using

    End Function


End Class

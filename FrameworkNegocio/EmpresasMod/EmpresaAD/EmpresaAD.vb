Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos
Imports FN.Empresas.DN

Public Class EmpresaAD

#Region "Métodos"

    Public Function RecuperarColaborador(ByVal codColaborador As String) As EntidadColaboradoraDN


        Using tr As New Transaccion

            Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            Dim idColaborador As String = RecuperarGUIDeIDColaborador(codColaborador)

            If String.IsNullOrEmpty(idColaborador) Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El id de colaborador es nulo para el codigo de colaborador:" & codColaborador)
            End If

            RecuperarColaborador = gi.Recuperar(idColaborador, GetType(FN.Empresas.DN.EntidadColaboradoraDN))
            tr.Confirmar()

        End Using



    End Function

    Public Function RecuperarGUIDeIDColaborador(ByVal codColaborador As String) As String
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim sedeEmp As SedeEmpresaDN = Nothing

        Using tr As New Transaccion()

            If String.IsNullOrEmpty(codColaborador) Then
                Throw New ApplicationException("El codColaborador  no puede ser nulo")
            End If

            sql = "Select ID from tlEntidadColaboradoraDN where CodigoColaborador=@CodigoColaborador and Baja<>@Baja "

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("CodigoColaborador", codColaborador))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de un colaborador con el mismo codigo:" & codColaborador)

            ElseIf dts.Tables(0).Rows.Count = 1 Then
                RecuperarGUIDeIDColaborador = dts.Tables(0).Rows(0).Item(0)

            Else
                Throw New ApplicationExceptionAD("No se recupero ningun id de colaborador para el codigo:" & codColaborador)

            End If

            tr.Confirmar()



        End Using

    End Function

    Public Function RecuperarSedePrincipalxCIFEmpresa(ByVal cifNifEmpresa As String) As SedeEmpresaDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim sedeEmp As SedeEmpresaDN = Nothing

        Using tr As New Transaccion()

            If String.IsNullOrEmpty(cifNifEmpresa) Then
                Throw New ApplicationException("El identificador fiscal de la empresa no puede ser nulo")
            End If

            sql = "Select idSedeEmpresa from vwSedexEmpresa where CIFNIF=@cifNifEmpresa and BajaSede<>@BajaSede and BajaEmpresa<>@BajaEmpresa"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("cifNifEmpresa", cifNifEmpresa))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("BajaSede", True))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("BajaEmpresa", True))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de una sede principal para un CIF de empresa")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                sedeEmp = gi.Recuperar(Of SedeEmpresaDN)(dts.Tables(0).Rows(0)(0))
            End If

            tr.Confirmar()

            Return sedeEmp

        End Using

    End Function

    Public Function RecuperarEmpresaFiscalxCIF(ByVal codigoCif As String) As EmpresaFiscalDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim dts As DataSet
        Dim empresaF As EmpresaFiscalDN = Nothing

        Using tr As New Transaccion()
            If String.IsNullOrEmpty(codigoCif) Then
                Throw New ApplicationException("El identificador fiscal de la empresa no puede ser nulo")
            End If

            sql = "Select ID from tlEmpresaFiscalDN where Cif_Codigo=@codigoCif and Baja<>@Baja"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("codigoCif", codigoCif))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de una empresa fiscal para un CIF")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                empresaF = gi.Recuperar(Of EmpresaFiscalDN)(dts.Tables(0).Rows(0)(0))
            End If

            tr.Confirmar()

            Return empresaF

        End Using
    End Function

#End Region

End Class

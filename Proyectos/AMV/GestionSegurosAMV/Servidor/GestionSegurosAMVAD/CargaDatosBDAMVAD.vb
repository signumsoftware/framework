Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Cuestionario.CuestionarioDN

Imports FN.Empresas.DN
Imports FN.RiesgosVehiculos.DN


Public Class CargaDatosBDAMVAD

    Public Sub CargaColaboradores(ByVal listaTiposSede As IList)
        ' recurso a la fuente de los datos de tarificacion
        Dim recFuente As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim cs As String
        Dim htd As New Generic.Dictionary(Of String, Object)

        Dim dts As DataSet
        Dim ej As Framework.AccesoDatos.Ejecutor

        Dim colColaboradores As ColEntidadColaboradoraDN
        Dim colEmpresas As ColEmpresasDN

        Using tr As New Transaccion()
            cs = "Data Source=localhost;Initial Catalog=VSegBas;Integrated Security=True"
            htd.Add("connectionstring", cs)
            recFuente = New Framework.LogicaNegocios.Transacciones.RecursoLN("2", "Conexion a MND1", "sqls", htd)

            'Carga de los colaboradores con los diferentes contactos (teléfonos, dirección, mail...)
            ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuente)
            dts = ej.EjecutarDataSet("select * from Colaborador", Nothing, False)

            colColaboradores = New ColEntidadColaboradoraDN()
            colEmpresas = New ColEmpresasDN()

            For Each dr As Data.DataRow In dts.Tables(0).Rows
                Dim colaborador As New EntidadColaboradoraDN()
                Dim datosColaborador As New DatosColaboradorDN()
                Dim empresa As New EmpresaDN()
                Dim sede As New SedeEmpresaDN()




                colaborador.DatosAdicionales = datosColaborador

                colColaboradores.AddUnico(colaborador)

            Next



            tr.Confirmar()

        End Using

    End Sub

    Public Sub CargaPolizas()
        ' recurso a la fuente de los datos de tarificacion
        Dim recFuente As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim cs As String
        Dim htd As New Generic.Dictionary(Of String, Object)

        Dim dts As DataSet
        Dim ej As Framework.AccesoDatos.Ejecutor

        Dim cadenaSQL As String

        Dim colColaboradores As ColEntidadColaboradoraDN
        Dim colEmpresas As ColEmpresasDN

        Using tr As New Transaccion()
            cs = "Data Source=localhost;Initial Catalog=VSegBas;Integrated Security=True"
            htd.Add("connectionstring", cs)
            recFuente = New Framework.LogicaNegocios.Transacciones.RecursoLN("2", "Conexion a MND1", "sqls", htd)

            'Carga de los colaboradores con los diferentes contactos (teléfonos, dirección, mail...)
            ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuente)

            cadenaSQL = "select * from polizas "
            cadenaSQL &= "inner join direcciones on polizas.Dir_id=Direcciones.Dir_id"
            cadenaSQL &= "inner join clientes on direcciones.cli_id=clientes.cli_id"
            cadenaSQL &= "inner join colaboradores on COL_ID_S=COL_ID "
            cadenaSQL &= "inner join riesgospolizas on riesgospolizas.pol_id=polizas.pol_id "
            cadenaSQL &= "inner join riesgos on riesgospolizas.rie_id=riesgos.rie_id "
            cadenaSQL &= "inner join suplementos on suplementos.pol_id=polizas.pol_id "

            dts = ej.EjecutarDataSet(cadenaSQL, Nothing, False)

            colColaboradores = New ColEntidadColaboradoraDN()
            colEmpresas = New ColEmpresasDN()

            For Each dr As Data.DataRow In dts.Tables(0).Rows
                Dim pr As New FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN()



            Next



            tr.Confirmar()

        End Using

    End Sub

    Public Sub CargarPresupuesto()
        ' recurso a la fuente de los datos de tarificacion
        Dim recFuente As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim cs As String
        Dim htd As New Generic.Dictionary(Of String, Object)

        Dim dts As DataSet
        Dim ej As Framework.AccesoDatos.Ejecutor

        Dim cadenaSQL As String

        Dim colColaboradores As ColEntidadColaboradoraDN
        Dim colEmpresas As ColEmpresasDN

        Using tr As New Transaccion()
            cs = "Data Source=localhost;Initial Catalog=VSegBas;Integrated Security=True"
            htd.Add("connectionstring", cs)
            recFuente = New Framework.LogicaNegocios.Transacciones.RecursoLN("2", "Conexion a MND1", "sqls", htd)

            'Carga de los colaboradores con los diferentes contactos (teléfonos, dirección, mail...)
            ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuente)

            cadenaSQL = "select * from polizas "
            cadenaSQL &= "inner join direcciones on polizas.Dir_id=Direcciones.Dir_id"
            cadenaSQL &= "inner join clientes on direcciones.cli_id=clientes.cli_id"
            cadenaSQL &= "inner join colaboradores on COL_ID_S=COL_ID "
            cadenaSQL &= "inner join riesgospolizas on riesgospolizas.pol_id=polizas.pol_id "
            cadenaSQL &= "inner join riesgos on riesgospolizas.rie_id=riesgos.rie_id "
            cadenaSQL &= "inner join suplementos on suplementos.pol_id=polizas.pol_id "

            dts = ej.EjecutarDataSet(cadenaSQL, Nothing, False)

            colColaboradores = New ColEntidadColaboradoraDN()
            colEmpresas = New ColEmpresasDN()

            For Each dr As Data.DataRow In dts.Tables(0).Rows
                Dim pr As New FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN()



            Next



            tr.Confirmar()

        End Using
    End Sub

    Private Function GenerarCuestionarioResuelto() As CuestionarioResueltoDN
        Dim cr As CuestionarioResueltoDN = Nothing

        Using tr As New Transaccion()

            tr.Confirmar()

            Return cr

        End Using

    End Function

End Class

Imports Framework.LogicaNegocios.Transacciones
Imports FN.Empresas.DN

Public Class EmpleadosAD

#Region "Métodos"

    'Public Function RecuperarListado(ByVal pIDEmpresa As String) As DataSet
    '    Dim ej As Framework.AccesoDatos.Ejecutor

    '    Dim sql As String
    '    Dim parametros As List(Of System.Data.IDataParameter)
    '    Dim parametro As Data.SqlClient.SqlParameter

    '    ' construir la sql y los parametros

    '    Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
    '    Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN

    '    Try
    '        ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
    '        ctd.IniciarTransaccion(Me.mTL, ProcTl)
    '        parametros = New List(Of System.Data.IDataParameter)

    '        If pIDEmpresa Is Nothing OrElse pIDEmpresa = String.Empty Then
    '            Throw New ApplicationException("El Id de la empresa no puede ser nulo")
    '        End If

    '        sql = "Select * from vwEmpleadosxEmpresa where idEmpresaDN=@idEmpresaDN"

    '        parametro = New Data.SqlClient.SqlParameter("@idEmpresaDN", SqlDbType.Int)
    '        parametro.Value = pIDEmpresa
    '        parametros.Add(parametro)



    '        ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
    '        RecuperarListado = ej.EjecutarDataSet(sql, parametros)
    '        ProcTl.Confirmar()

    '    Catch ex As Exception
    '        ProcTl.Cancelar()
    '        Throw ex
    '    Finally

    '    End Try

    'End Function

#End Region



End Class

Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.LogicaNegocios.Transacciones
Imports FN.Empresas.DN
Imports FN.Empresas.AD

Public Class EmpresaLN
    Inherits BaseGenericLN

#Region "Métodos"

    Public Function RecuperarSedePrincipalxCIFEmpresa(ByVal cifNifEmpresa As String) As SedeEmpresaDN
        Dim empAD As EmpresaAD

        Using tr As New Transaccion()

            empAD = New EmpresaAD()
            RecuperarSedePrincipalxCIFEmpresa = empAD.RecuperarSedePrincipalxCIFEmpresa(cifNifEmpresa)

            tr.Confirmar()

        End Using

    End Function

    Public Function RecuperarEmpresaFiscalxCIF(ByVal codigoCif As String) As EmpresaFiscalDN
        Dim empAD As EmpresaAD

        Using tr As New Transaccion()

            empAD = New EmpresaAD()
            RecuperarEmpresaFiscalxCIF = empAD.RecuperarEmpresaFiscalxCIF(codigoCif)

            tr.Confirmar()

        End Using
    End Function

#End Region

End Class

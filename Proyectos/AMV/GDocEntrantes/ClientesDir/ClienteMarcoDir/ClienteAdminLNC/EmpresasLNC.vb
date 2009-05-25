Imports FN.Empresas.DN

Public Class EmpresasLNC


#Region "Métodos"

    ''' <summary>
    ''' Recuperar la sede de la empresa cuyo CIF se pasa como parámetro y se asigna al empleado
    ''' </summary>
    ''' <param name="empPuestoR"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function FijarEmpleadoEmpresaCIF(ByVal control As Object, ByVal empPuestoR As EmpleadoYPuestosRDN, ByVal vincMetodo As Object) As EmpleadoYPuestosRDN
        Dim empAS As New FN.Empresas.AS.EmpresaAS()
        Dim sedeP As SedeEmpresaDN
        Dim cifNifEmpresa As String

        If empPuestoR Is Nothing OrElse empPuestoR.Empleado Is Nothing Then
            Throw New ApplicationException("El empleado no es corecto, no se le puede asignar la sede")
        End If

        If Framework.Configuracion.AppConfiguracion.DatosConfig.Contains("cifNifEmpresa") Then
            cifNifEmpresa = Framework.Configuracion.AppConfiguracion.DatosConfig.Item("cifNifEmpresa")
        Else
            Throw New ApplicationException("No se ha especificado el CIF de la empresa por defecto")
        End If

        sedeP = empAS.RecuperarSedePrincipalxCIFEmpresa(cifNifEmpresa)

        If sedeP IsNot Nothing Then
            empPuestoR.Empleado.SedeEmpresa = sedeP
        Else
            Throw New ApplicationException("No se ha podido recuperar la sede principal para la empresa por defecto")
        End If

        Return empPuestoR

    End Function

#End Region

End Class

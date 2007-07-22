Public Class UsrProcesosCtrl

    Public Function BajaPrincipal(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object) As Framework.DatosNegocio.IEntidadBaseDN

        Dim prin As Usuarios.DN.PrincipalDN = objeto

        Dim fp As MotorIU.FormulariosP.FormularioBase = pParametros

        fp.cMarco.Navegar("Acercade", fp, fp, MotorIU.Motor.TipoNavegacion.Modal, Nothing, Nothing, Nothing)


        Dim plnc As New Framework.Procesos.ProcesosLNC.ProcesoLNC
        ' el ultimo parametro puede contener un objeto que contenga los parametros que el metodo ln requerira para realizar la aoperacion
        ' esto es necesario si no basta con los datos de la pripia dn modificados sino que hacen galta datos adicionales
        ' en este caso se puede crear u objeto "cesta"
        ' que perimta recolectar el resto de datos y pasarlo como parametro
        Return plnc.EjecutarOperacionLNC(fp.cMarco.Principal, pTransicionRealizada, objeto, Nothing)


    End Function



End Class

Imports Framework.IU.IUComun

Public Class UsrProcesosCtrl

    Public Function BajaPrincipal(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object) As Framework.DatosNegocio.IEntidadBaseDN

        Dim prin As Usuarios.DN.PrincipalDN = objeto

        Dim fp As MotorIU.FormulariosP.FormularioBase = pParametros

        ' fp.cMarco.Navegar("Acercade", fp, fp, MotorIU.Motor.TipoNavegacion.Modal, Nothing, Nothing, Nothing)
        If System.Windows.Forms.MessageBox.Show("EstaSeguro que desea dar de baja el principal", "Confirmacion", Windows.Forms.MessageBoxButtons.YesNo) = Windows.Forms.DialogResult.Yes Then


            ' el ultimo parametro puede contener un objeto que contenga los parametros que el metodo ln requerira para realizar la aoperacion
            ' esto es necesario si no basta con los datos de la pripia dn modificados sino que hacen galta datos adicionales
            ' en este caso se puede crear u objeto "cesta"
            ' que perimta recolectar el resto de datos y pasarlo como parametro

        End If

        Return prin

    End Function



End Class

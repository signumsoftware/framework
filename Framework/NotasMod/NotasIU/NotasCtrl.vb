Imports Framework.iu.iucomun
Public Class NotasCtrl

    Public Sub CrearNotaPara(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN)


        Dim control As IctrlBasicoDN = sender

        Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

        Dim col As New List(Of Framework.DatosNegocio.IEntidadDN)
        col.Add(control.DN)
        Dim lnc As New Framework.Notas.NotasLNC.NotasLNC

        ' obteneos el principal del marco
        Dim prin As Framework.Usuarios.DN.PrincipalDN = fp.cMarco.Principal
        Dim nota As Framework.Notas.NotasDN.NotaDN = lnc.CrearNotaPara(prin.UsuarioDN, col)

        Dim paquete As New Hashtable
        paquete.Add("DN", nota)

        ' navegar al formaulario que permite editar la nota
        fp.cMarco.Navegar("FG", fp, Nothing, MotorIU.Motor.TipoNavegacion.Normal, paquete)

    End Sub


End Class

Public Class controladorInformes

    Public Function GenerarInformePresupuesto(ByVal pIDPresupuesto As String) As System.IO.FileInfo
        Dim it As New InformesTemporal.InformesTemporal()
        Return it.ImprimirPresupuesto(pIDPresupuesto)
    End Function


    'Public Function GenerarInformePresupuestoB(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN

    '    Dim pr As FN.Seguros.Polizas.DN.PresupuestoDN = datos
    '    Dim fi As System.IO.FileInfo = GenerarInformePresupuesto(pr.ID)
    '    System.Diagnostics.Process.Start(fi.FullName)
    '    Return datos
    'End Function

    Public Function GenerarInformePresupuestoB(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object) As Framework.DatosNegocio.IEntidadBaseDN

        Dim pr As FN.Seguros.Polizas.DN.PresupuestoDN = objeto
        Dim fi As System.IO.FileInfo = GenerarInformePresupuesto(pr.ID)
        System.Diagnostics.Process.Start(fi.FullName)

        Return pr

    End Function




End Class

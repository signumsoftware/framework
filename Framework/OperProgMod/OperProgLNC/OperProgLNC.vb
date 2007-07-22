Public Class OperProgLNC
    Public Function CrearAlertaPara(ByVal creador As Framework.Usuarios.DN.UsuarioDN, ByVal pCol As IList(Of Framework.DatosNegocio.IEntidadDN)) As Framework.OperProg.OperProgDN.AlertaDN
        Dim Alerta As New Framework.OperProg.OperProgDN.AlertaDN
        Alerta.AsignarCreador(creador)
        Alerta.Modificable = True
        Alerta.FI = Now
        For Each entida As Framework.DatosNegocio.IEntidadDN In pCol
            'Dim he As New Framework.DatosNegocio.HEDN(entida, DatosNegocio.HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir)

            Alerta.ColIHEntidad.AddHuellaPara(entida)

        Next
        Return Alerta
    End Function

End Class

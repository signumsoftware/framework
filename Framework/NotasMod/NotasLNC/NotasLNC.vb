Public Class NotasLNC


    Public Function CrearNotaPara(ByVal creador As Framework.Usuarios.DN.UsuarioDN, ByVal pCol As IList(Of Framework.DatosNegocio.IEntidadDN)) As Framework.Notas.NotasDN.NotaDN
        Dim nota As New Framework.Notas.NotasDN.NotaDN
        nota.AsignarCreador(creador)

        For Each entida As Framework.DatosNegocio.IEntidadDN In pCol
            'Dim he As New Framework.DatosNegocio.HEDN(entida, DatosNegocio.HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir)

            nota.ColIHEntidad.AddHuellaPara(entida)

        Next
        Return nota
    End Function

End Class

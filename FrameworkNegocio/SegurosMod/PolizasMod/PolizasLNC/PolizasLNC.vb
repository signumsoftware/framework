Public Class PolizasLNC


    Public Function AltaDePoliza(ByVal ptomador As FN.Seguros.Polizas.DN.TomadorDN, ByVal pEmisora As FN.Seguros.Polizas.DN.EmisoraPolizasDN, ByVal ptarifa As FN.Seguros.Polizas.DN.TarifaDN, ByVal fiPR As Date) As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN

        ' creacion de los objetos



        Dim prp As New FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN()
        prp.FI = fiPR 'la pone el usuario en la pantalla
        prp.FF = ptarifa.AMD.IncrementarFecha(prp.FI)


        '''''''''''''''''''''''
        ' creación de la poliza
        Dim pol As New FN.Seguros.Polizas.DN.PolizaDN
        prp.Poliza = pol
        pol.Tomador = ptomador
        pol.EmisoraPolizas = pEmisora

        ' solicitar los codigos de poliza
        'se realiza en el moento del alta

        '' FIN ''''''''''''''''''''''



        ''''''''''''''''''''''''''''''''''''''
        ' perido de cobertura

        Dim pc As New FN.Seguros.Polizas.DN.PeriodoCoberturaDN
        prp.ColPeriodosCobertura.Add(pc)
        pc.FI = prp.FI
        pc.Tarifa = ptarifa


        ' tal vez no estaria mal que se pudieran ver ya los pagos previstos
        '' coleccion de pagos e importes debidos
        'Dim colpagos As FN.GestionPagos.DN.ColPagoDN
        'colpagos = GenerarCargosPara(Nothing, prp, pc, 100)

        Return prp

    End Function

End Class

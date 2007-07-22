Imports Framework.LogicaNegocios.Transacciones

Public Class PolizasOperLN

    Public Sub ModificarPoliza(ByVal periodoR As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN, ByVal tarifa As FN.Seguros.Polizas.DN.TarifaDN, ByVal cuestionarioR As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal fechaInicioPC As Date)

        Using tr As New Transaccion()

            If Not periodoR.Contiene(fechaInicioPC) Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("La fecha del nuevo periodo de cobertura debe estar contenida dentro del periodo de renovación vigente")
            End If

            Dim ln As New PolizaRvLcLN()
            If Date.Compare(fechaInicioPC, Now()) < 0 Then
                ln.ModificarCondicionesCoberturaRetroactiva(periodoR, tarifa, cuestionarioR, fechaInicioPC, 10)
            Else
                ln.ModificarCondicionesCoberturaRetroactiva(periodoR, tarifa, cuestionarioR, fechaInicioPC, 10)
            End If

            tr.Confirmar()
        End Using

    End Sub

    Public Sub EmitirPoliza(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object)

    End Sub

    'Public Sub RenovarPoliza(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal sender As Object)
    Public Sub RenovarPoliza(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object)

        Dim pPR As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN

        Using tr As New Transaccion

            pPR = pTransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion


            '''''''''
            ' CUERPO
            ''''''''''

            Dim fechaRenovacion As Date = pParametros
            ' baja de la poliza y de los importes debidos
            Dim ln As New PolizaRvLcLN
            ln.RenovacionPoliza(fechaRenovacion, pPR, Nothing)


            '' ejecutar la transición de baja en si para que permita la navegacion a otras operaciones siguientes
            'Dim procLN As Framework.Procesos.ProcesosLN.GestorEjecutoresLN
            'procLN = New Framework.Procesos.ProcesosLN.GestorEjecutoresLN()
            'procLN.GuardarGenerico(pPR, pTransicionRealizada, Nothing)


            ' hay que comunicar la renovacion  al sistema de fiva SIEMPRE y el ya vera si la renovacion es automatica o no

            ' ' crear los cajones de documento si fuera necesario creo que no

            ' crear una emision de nueva poliza


            ' TODO: alexgs POR IMPLEMENTAR

            tr.Confirmar()

        End Using
    End Sub

    Public Sub ReactivarPoliza(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal sender As Object)
        'Dim pPR As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN

        'Using tr As New Transaccion

        '    pPR = pTransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion


        '    '''''''''
        '    ' CUERPO
        '    ''''''''''


        '    ' baja de la poliza y de los importes debidos
        '    Dim ln As New PolizaRvLcLN
        '    ln.RenovacionPoliza(pPR, Nothing)


        '    ' ejecutar la transición de baja en si para que permita la navegacion a otras operaciones siguientes
        '    Dim procLN As Framework.Procesos.ProcesosLN.GestorEjecutoresLN
        '    procLN = New Framework.Procesos.ProcesosLN.GestorEjecutoresLN()
        '    procLN.GuardarGenerico(pPR, pTransicionRealizada, Nothing)


        '    ' hay que comunicar la renovacion  al sistema de fiva SIEMPRE y el ya vera si la renovacion es automatica o no

        '    ' ' crear los cajones de documento si fuera necesario creo que no

        '    ' crear una emision de nueva poliza


        '    ' TODO: alexgs POR IMPLEMENTAR

        '    tr.Confirmar()

        'End Using
    End Sub
    Public Sub BajaPolizaOper(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object)


        Using tr As New Transaccion



            '''''''''
            ' CUERPO
            ''''''''''
            Dim fechaBaja As Date = pParametros
            Dim he As FN.Seguros.Polizas.DN.HEPeriodoRenovacionPolizaDN


            If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion.Typo) Then
                he = New FN.Seguros.Polizas.DN.HEPeriodoRenovacionPolizaDN(CType(pTransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion, Framework.DatosNegocio.HEDN))

            Else
                he = New FN.Seguros.Polizas.DN.HEPeriodoRenovacionPolizaDN(CType(pTransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion, FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN))

            End If



   


            ' baja de la poliza y de los importes debidos
            Dim pr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN
            Dim ln As New PolizaRvLcLN
            ' ln.BajaDePoliza(pr, pr.PeridoCoberturaActivo, fechaBaja)
            pr = ln.BajaDePoliza(he, fechaBaja)

            pTransicionRealizada.OperacionRealizadaDestino.AsignarOIenGrafo(pr)

            ' ejecutar la transición de baja en si para que permita la navegacion a otras operaciones siguientes
            'Dim procLN As Framework.Procesos.ProcesosLN.GestorEjecutoresLN
            'procLN = New Framework.Procesos.ProcesosLN.GestorEjecutoresLN()
            'procLN.GuardarGenerico(pr, pTransicionRealizada, Nothing)


            ' hay que comunicar la baja al sistema de fiva

            ' hay que dar de baja los cajones de documentos asociados con la poliza que no tengan documentos asociados 

            ' TODO: alexgs POR IMPLEMENTAR






            tr.Confirmar()




        End Using
    End Sub



    Public Function AltaDePoliza(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object) As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN

        Using tr As New Transaccion


            ' el alta se puede producir desde un objeto de periodo de renovacion o desde un presupuesto a partir del cual se crera un nuevo perido de renovacion
            Dim dn As Framework.DatosNegocio.EntidadDN = pTransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion

            Dim ln As New PolizaRvLcLN()

            If TypeOf dn Is FN.Seguros.Polizas.DN.PresupuestoDN Then
                'dn = ln.VerificarDatosPresupuesto(dn)
                Dim presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN = dn
                Dim pPR As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN = ln.AltaDePolizap(presupuesto, presupuesto.FechaAltaSolicitada)
                pTransicionRealizada.OperacionRealizadaDestino.ObjetoDirectoOperacion = pPR ' dado que vino un presupuesto hay que asignar el nuevo perido de renovación creado
                AltaDePoliza = pPR
            ElseIf TypeOf dn Is FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN Then
                Dim pPR As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN = dn
                ln.AltaDePolizapp(pPR, True)
                AltaDePoliza = pPR
                'pTransicionRealizada.OperacionRealizadaDestino.ObjetoDirectoOperacion = pPR
            End If

            tr.Confirmar()

        End Using
    End Function



    'Public Sub VerificarDatosPresupuesto(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object)

    '    Using tr As New Transaccion


    '        ' el alta se puede producir desde un objeto de periodo de renovacion o desde un presupuesto a partir del cual se crera un nuevo perido de renovacion

    '        Dim dn As FN.Seguros.Polizas.DN.PresupuestoDN = pTransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion

    '        Dim ln As New PolizaRvLcLN
    '        Dim fechaAlta As Date = pParametros
    '        Dim presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN = dn
    '        Dim pPR As FN.Seguros.Polizas.DN.PresupuestoDN = ln.VerificarDatosPresupuesto(presupuesto)
    '        pTransicionRealizada.OperacionRealizadaDestino.ObjetoDirectoOperacion = pPR ' dado que vino un presupuesto hay que asignar el nuevo perido de renovación creado

    '        tr.Confirmar()

    '    End Using
    'End Sub

End Class

Imports Framework.IU.IUComun
Imports Framework.DatosNegocio
Imports Framework.DatosNegocio.Localizaciones.Temporales

Public Class TarificadorCtrl

    Public Function TarificarPresupuesto(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN

        Dim control As IctrlBasicoDN = sender

        Dim presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN = control.DN
        Dim rvAS As New FN.RiesgosVehiculos.AS.RiesgosVehículosAS()

        control.DN = rvAS.TarificarPresupuesto(presupuesto)

        Return control.DN
    End Function

    Public Function BajaPresupuesto(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object) As Framework.DatosNegocio.IEntidadBaseDN
        Windows.Forms.MessageBox.Show("¿Está seguro que desea anular el presupuesto?", "Anular presupuesto", Windows.Forms.MessageBoxButtons.YesNo, Windows.Forms.MessageBoxIcon.Information)

        Dim presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN = objeto
        If presupuesto.FechaAnulacion = Date.MinValue Then
            presupuesto.FechaAnulacion = Now()
        End If

        Dim miAS As New Framework.Procesos.ProcesosAS.OperacionesAS()
        Return miAS.EjecutarOperacionModificarObjeto(objeto, pTransicionRealizada, pParametros)

    End Function

    'Public Function BajaPresupuesto(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN

    '    Dim control As IctrlBasicoDN = sender

    '    Windows.Forms.MessageBox.Show("¿Está seguro que desea anular el presupuesto?", "Anular presupuesto", Windows.Forms.MessageBoxButtons.YesNo, Windows.Forms.MessageBoxIcon.Information)

    '    Dim presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN = control.DN
    '    If presupuesto.FechaAnulacion = Date.MinValue Then
    '        presupuesto.FechaAnulacion = Now()
    '    End If

    '    Dim miAS As New Framework.AS.DatosBasicosAS()
    '    miAS.GuardarDNGenerico(presupuesto)

    '    Return presupuesto
    'End Function

    'Public Function ClonarPresupuesto(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN
    '    Dim control As IctrlBasicoDN = sender
    '    ' obtenemos el formulario
    '    Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

    '    Dim presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN = control.DN
    '    Dim presupuestoClon As FN.Seguros.Polizas.DN.PresupuestoDN

    '    'Quedaría clonar las respuestas del cuestionario resuelto
    '    presupuestoClon = presupuesto.ClonarPresupuesto()

    '    'Volvemos al formulario del presupuesto con el nuevo clon
    '    Dim paquete As New Hashtable
    '    paquete.Add("Presupuesto", presupuestoClon)

    '    ' navegar al formaulario que permite editar la nota
    '    fp.cMarco.Navegar("FG", fp, CType(fp, System.Windows.Forms.Form).ParentForm, MotorIU.Motor.TipoNavegacion.CerrarLanzador, paquete)

    '    Return datos

    'End Function

    Public Function ClonarCuestionarioResuelto(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN
        Dim control As IctrlBasicoDN = sender
        Dim fp As MotorIU.FormulariosP.IFormularioP
        Dim presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN = control.DN

        Dim cuestionarioRClon As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN

        If presupuesto Is Nothing OrElse presupuesto.Tarifa Is Nothing OrElse presupuesto.Tarifa.DatosTarifa Is Nothing OrElse CType(presupuesto.Tarifa.DatosTarifa, FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN).HeCuestionarioResuelto Is Nothing Then
            Throw New ApplicationException("No se ha podido recuperar el cuestionario resuelto")
        End If

        Dim miLNC As New FN.RiesgosVehiculos.LNC.RiesgosVehiculosLNC.RVPolizasLNC()
        cuestionarioRClon = miLNC.ClonarCuestionarioResuelto(presupuesto.Tarifa.DatosTarifa)

        'Volvemos al formulario del presupuesto con el nuevo clon
        Dim paquete As New Hashtable
        paquete.Add("CuestionarioResuelto", cuestionarioRClon)

        ' navegar al formaulario para tarificar
        fp = CType(control, System.Windows.Forms.ContainerControl).ParentForm
        fp.cMarco.Navegar("Cuestionario1", fp, CType(fp, System.Windows.Forms.Form).ParentForm, MotorIU.Motor.TipoNavegacion.Normal, paquete)

        Return datos

    End Function

    Public Function ModificarPoliza(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object) As Framework.DatosNegocio.IEntidadDN

        '1 El usuario confirma la acción y añade la fecha de efecto del nuevo periodo de cobertura
        Dim fechaEfectoPC As Date = Now.AddDays(15)

        '2 Navego al cuestionario tarificación a partir de un clon del cuestionarioR
        Dim miLNC As New FN.RiesgosVehiculos.LNC.RiesgosVehiculosLNC.RVPolizasLNC()

        Dim periodoR As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN = objeto
        Dim cuestionarioR As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN = miLNC.ClonarCuestionarioResuelto(periodoR.PeridoCoberturaActivo.Tarifa.DatosTarifa)

        Dim fp As MotorIU.FormulariosP.IFormularioP
        fp = pParametros

        Dim paquete As New Hashtable()
        paquete.Add("CuestionarioResuelto", cuestionarioR)
        paquete.Add("SoloCuestionario", True)
        paquete.Add("TipoDevuelto", GetType(Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN).FullName)

        fp.cMarco.Navegar("Cuestionario1", fp, CType(fp, System.Windows.Forms.Form).ParentForm, MotorIU.Motor.TipoNavegacion.Modal, paquete)


        cuestionarioR = paquete.Item("DN")

        '3 A partir del nuevo cuestionario resuelto, genero una tarifa
        Dim miAS As New GSAMVAS.CuestionarioAS()
        Dim tiempoTarificado As New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias()
        tiempoTarificado = AnyosMesesDias.CalcularDirAMD(periodoR.FF, fechaEfectoPC)

        Dim tarifaNueva As FN.Seguros.Polizas.DN.TarifaDN = miAS.GenerarTarifaxCuestionarioRes(cuestionarior, tiempoTarificado)


        '4 Realizo la modificación de la póliza pasando PR, CR, T, 
        Dim rvAS As New FN.RiesgosVehiculos.AS.RiesgosVehículosAS()
        rvAS.ModificarPoliza(periodoR, tarifaNueva, cuestionarioR, fechaEfectoPC)

        Return periodoR

    End Function

End Class

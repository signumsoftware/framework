Imports Framework.DatosNegocio
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Procesos.ProcesosLN

Public Class GestionSegurosAMVGBDAD
    Inherits Framework.AccesoDatos.MotorAD.GBDBase

    Public Shared llamadoCrearTablas As Boolean
    Public Shared llamadoCrearVistas As Boolean


#Region "Constructor"

    Public Sub New(ByVal recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        mRecurso = recurso
    End Sub

#End Region

    Public Overrides Sub CrearTablas()




        Using tr As New Transaccion


            Dim gbd As Framework.AccesoDatos.MotorAD.GBDBase



            gbd = New FN.RiesgosVehiculos.AD.RiesgosVehiculosGBD(mRecurso)
            gbd.CrearTablas()


            gbd = New FN.Trabajos.AD.TrabajosGBD(mRecurso)
            gbd.CrearTablas()


            gbd = New GDocEntrantesAD.GDocsEntrantesGBD(mRecurso)
            gbd.CrearTablas()


            gbd = New MNavegacionDatosAD.MNDGBD(mRecurso)
            gbd.CrearTablas()




            tr.Confirmar()

        End Using







    End Sub

    Public Overrides Sub CrearVistas()




        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True

        'Using tr As New Transaccion


        Dim gbd As Framework.AccesoDatos.MotorAD.GBDBase

        gbd = New FN.RiesgosVehiculos.AD.RiesgosVehiculosGBD(mRecurso)
        gbd.CrearVistas()

        gbd = New FN.Trabajos.AD.TrabajosGBD(mRecurso)
        gbd.CrearVistas()


        gbd = New GDocEntrantesAD.GDocsEntrantesGBD(mRecurso)
        gbd.CrearVistas()


        gbd = New MNavegacionDatosAD.MNDGBD(mRecurso)
        gbd.CrearVistas()

        'Dim ej As Framework.AccesoDatos.Ejecutor
        'ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        'ej.EjecutarNoConsulta(My.Resources.vwImpresionTarifa1)

        'ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        'ej.EjecutarNoConsulta(My.Resources.vwImpresionTarifa2)

        'tr.Confirmar()

        'End Using

    End Sub

    Public Sub PublicarGrafoPolizas()

        ' 1º dn o dns a las cuales se vincula el flujo
        '   TARIFA
        Dim vcTDN As Framework.TiposYReflexion.DN.VinculoClaseDN = RecuperarVinculoClase(GetType(FN.Seguros.Polizas.DN.TarifaDN))
        Dim colVcT As New Framework.TiposYReflexion.DN.ColVinculoClaseDN()
        colVcT.Add(vcTDN)

        '   PERIODO RENOVACIÓN
        Dim vcPDN As Framework.TiposYReflexion.DN.VinculoClaseDN = RecuperarVinculoClase(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN))
        Dim colVcP As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
        colVcP.Add(vcPDN)


        Dim vcPresupuesto As Framework.TiposYReflexion.DN.VinculoClaseDN = RecuperarVinculoClase(GetType(FN.Seguros.Polizas.DN.PresupuestoDN))
        Dim colVcPresupu As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
        colVcPresupu.Add(vcPresupuesto)
        'colVcPresupu.Add(vcPDN)

        '-----------------------------------------------------------------------------------------------------------------------


        ' 2º creacion de las operaciones
        Dim colOp As New Framework.Procesos.ProcesosDN.ColOperacionDN

        '   TARIFA
        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Gestión Tarifas", colVcT, "element_into.ico", True)))
        'colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Generar Presupuesto", colVcT, "element_into.ico", True)))


        'PRESUESTOS
        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Gestión Presupuesto", colVcPresupu, "element_into.ico", True)))

        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Modificar Presupuesto", colVcPresupu, "Guardar.ico", True)))
        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Emitir Presupuesto", colVcPresupu, "printer216x16.ico", True)))
        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Anular Presupuesto", colVcPresupu, "document_delete16x16.ico", True)))

        'colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Verificar Presupuesto", colVcPresupu, "check16x16.ico", True)))
        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Tarificar Presupuesto", colVcPresupu, "money2.png", True)))
        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Reactivar Presupuesto", colVcPresupu, "document_pulse16x16.ico", True)))
        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Alta Póliza desde Presupuesto", colVcPresupu, "contract16x16.ico", True)))


        '-------------------------------------------------------------------------------------------------------------

        '   PERIODO RENOVACIÓN
        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Gestión Pólizas", colVcP, "element_into.ico", True)))

        'colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Alta Póliza", colVcPresupu, "contract16x16.ico", True)))


        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Modificar Póliza", colVcP, "Guardar.ico", True)))
        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Emitir Póliza", colVcP, "printer216x16.ico", True)))
        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Renovar Póliza", colVcP, "document_refresh16x16.ico", True)))
        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Anular Póliza", colVcP, "document_delete16x16.ico", True)))
        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Reactivar Póliza", colVcP, "document_pulse16x16.ico", True)))
        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Finalizar Póliza", colVcP, "document_lock16x16.ico", True)))
        '-----------------------------------------------------------------------------------------------------------------------

        colOp.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Resumen Economico Poliza", colVcP, "document_lock16x16.ico", True)))
        '-----------------------------------------------------------------------------------------------------------------------







        ' 3º creacion de las Transiciones

        Dim colVM As New Framework.TiposYReflexion.DN.ColVinculoMetodoDN()

        '   PRESUPUESTOS
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Gestión Presupuesto", "Tarificar presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioObjCreado, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Gestión Presupuesto", "Emitir Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioObjCreado, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Gestión Presupuesto", "Modificar Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioObjCreado, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Gestión Presupuesto", "Anular Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioObjCreado, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Gestión Presupuesto", "Alta Póliza desde Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioObjCreado, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Tarificar presupuesto", "Tarificar presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Tarificar presupuesto", "Emitir Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Tarificar presupuesto", "Modificar Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Tarificar presupuesto", "Anular Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Tarificar Presupuesto", "Alta Póliza desde Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Modificar presupuesto", "Tarificar presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Modificar presupuesto", "Emitir Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Modificar presupuesto", "Modificar Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Modificar presupuesto", "Anular Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Modificar Presupuesto", "Alta Póliza desde Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Anular Presupuesto", "Reactivar Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Reactivar presupuesto", "Tarificar presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Reactivar presupuesto", "Emitir Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Reactivar presupuesto", "Modificar Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Reactivar presupuesto", "Anular Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Reactivar Presupuesto", "Alta Póliza desde Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Alta Póliza desde Presupuesto", "Gestión Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Alta Póliza desde Presupuesto", "Gestión Pólizas", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioDesde, False, Nothing, False))


        '   PERIODO RENOVACIÓN
        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Gestión Pólizas", "Alta Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Inicio, False, Nothing, True))
        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Alta Póliza", "Anular Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Gestión Pólizas", "Alta Póliza desde Presupuesto", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioDesde, False, Nothing, False))
        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Alta Póliza", "Emitir Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Alta Póliza desde Presupuesto", "Emitir Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Gestión Pólizas", "Emitir Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioObjCreado, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Gestión Pólizas", "Modificar Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioObjCreado, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Gestión Pólizas", "Anular Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioObjCreado, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Emitir Póliza", "Anular Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Emitir Póliza", "Modificar Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Emitir Póliza", "Renovar Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Emitir Póliza", "Emitir Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))


        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Modificar Póliza", "Anular Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Modificar Póliza", "Emitir Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Modificar Póliza", "Modificar Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Modificar Póliza", "Renovar Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Renovar Póliza", "Emitir Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        '  GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Renovar Póliza", "Anular Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Anular Póliza", "Reactivar Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Reactivar Póliza", "Emitir Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Reactivar Póliza", "Anular Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Anular Póliza", "Finalizar Póliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        '   transición finalización
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Finalizar Póliza", "Gestión Pólizas", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        '-----------------------------------------------------------------------------------------------------------------------


        '-----------------------------------------------------------------------------------------------------------------------
        '   PERIODO RENOVACIÓN Resumen Economico
        '-----------------------------------------------------------------------------------------------------------------------
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colOp, "Resumen Economico Poliza", "Resumen Economico Poliza", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioDesde, False, Nothing, False))



        '-----------------------------------------------------------------------------------------------------------------------




        ' 4º Creación de los clientes del grafo

        Dim ejClienteS, ejClienteC As Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN
        Dim clienteS, clienteC As Framework.Procesos.ProcesosDN.ClientedeFachadaDN

        'Se comprubea si ya existen los clientesFachada, y sino se crean
        Dim opAD As New Framework.Procesos.ProcesosAD.OperacionesAD()

        ejClienteS = opAD.RecuperarEjecutorCliente(CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion"), String))
        ejClienteC = opAD.RecuperarEjecutorCliente(CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente"), String))

        If ejClienteS Is Nothing Then
            clienteS = New Framework.Procesos.ProcesosDN.ClientedeFachadaDN()
            clienteS.Nombre = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion"), String)

            ejClienteS = New Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN()
            ejClienteS.ClientedeFachada = clienteS
        End If

        If ejClienteC Is Nothing Then
            clienteC = New Framework.Procesos.ProcesosDN.ClientedeFachadaDN()
            clienteC.Nombre = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente"), String)

            ejClienteC = New Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN()
            ejClienteC.ClientedeFachada = clienteC
        End If

        ' Ej Servidor
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Gestión Tarifas", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))


        'PRESUPUESTOS
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Gestión Presupuesto", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Modificar Presupuesto", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Emitir Presupuesto", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Anular Presupuesto", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Verificar Presupuesto", RecuperarVinculoMetodo("VerificarDatosPresupuesto", GetType(FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizasOperLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Tarificar Presupuesto", RecuperarVinculoMetodo("TarificarPresupuestoOp", GetType(FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Reactivar Presupuesto", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))

        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Alta Póliza desde Presupuesto", RecuperarVinculoMetodo("AltaDePoliza", GetType(FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizasOperLN)), ejClienteS)))

        'PÓLIZAS
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Gestión Pólizas", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))

        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Modificar Póliza", RecuperarVinculoMetodo("ModificarPoliza", GetType(FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizasOperLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Emitir Póliza", RecuperarVinculoMetodo("EmitirPoliza", GetType(FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizasOperLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Renovar Póliza", RecuperarVinculoMetodo("RenovarPoliza", GetType(FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizasOperLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Anular Póliza", RecuperarVinculoMetodo("BajaPolizaOper", GetType(FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizasOperLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Reactivar Póliza", RecuperarVinculoMetodo("ReactivarPoliza", GetType(FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizasOperLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Finalizar Póliza", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))

        'Ej Cliente
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Gestión Tarifas", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))

        'PRESUPUESTOS
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Gestión Presupuesto", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))

        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Modificar Presupuesto", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Emitir Presupuesto", RecuperarVinculoMetodo("GenerarInformePresupuestoB", GetType(GSAMVControladores.controladorInformes)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Anular Presupuesto", RecuperarVinculoMetodo("BajaPresupuesto", GetType(GSAMVControladores.TarificadorCtrl)), ejClienteC)))
        'ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Verificar Presupuesto", RecuperarVinculoMetodo("VerificarDatosPresupuesto", GetType(FN.RiesgosVehiculos.IU.Controladores.ctrlRiesgosVehiculos)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Tarificar Presupuesto", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Reactivar Presupuesto", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Alta Póliza desde Presupuesto", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))

        'POLIZAS
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Gestión Pólizas", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))

        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Modificar Póliza", RecuperarVinculoMetodo("ModificarPoliza", GetType(GSAMVControladores.TarificadorCtrl)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Emitir Póliza", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Renovar Póliza", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Anular Póliza", RecuperarVinculoMetodo("BajaPoliza", GetType(FN.Seguros.Polizas.PolizasIU.PolizasCtrl)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Reactivar Póliza", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Finalizar Póliza", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))



        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colOp, "Resumen Economico Poliza", RecuperarVinculoMetodo("EjecutarOperacionNavegarResumenEconomicoPoliza", GetType(FN.RiesgosVehiculos.IU.Controladores.ctrlRiesgosVehiculos)), ejClienteC)))



        Me.GuardarDatos(ejClienteC)
        Me.GuardarDatos(ejClienteS)

        '-----------------------------------------------------------------------------------------------------------------------

    End Sub


    Public Sub CrearGrafoDocumentosPp()



        ' flujo de talones

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' 1º 
        ' dn o dns a las cuales se vincula el flujo
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim vc1DN As Framework.TiposYReflexion.DN.VinculoClaseDN = RecuperarVinculoClase(GetType(Framework.Ficheros.FicherosDN.CajonDocumentoDN))
        Dim ColVc As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
        ColVc.Add(vc1DN)


        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' 2º 
        '  creacion de las operaciones
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        Dim colop As New Framework.Procesos.ProcesosDN.ColOperacionDN

        ' operacion que engloba todo el flujo
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Gestion CajonDocumento", ColVc, "element_into.ico", True)))


        '' operacion de pueba
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Verificar Cajon Documento", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Rechazar Documento en Cajon Documento", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Rechazar Documento y reidentificar en  Cajon Documento", ColVc, "element_into.ico", True)))


        '' FIN operacion de pueba


        ''''''''''''''''''''''''''''''''''''''''''''
        ' 3º
        ' creacion de las Transiciones
        ''''''''''''''''''''''''''''''''''''''''''''


        Dim colVM As New Framework.TiposYReflexion.DN.ColVinculoMetodoDN()


        '' prueba subordiandas ''''''''''''''''''''''''''''''''''''''

        ' transicion de inicio 
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Gestion CajonDocumento", "Verificar Cajon Documento", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioObjCreado, False, Nothing, True))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Gestion CajonDocumento", "Rechazar Documento en Cajon Documento", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioObjCreado, False, Nothing, True))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Gestion CajonDocumento", "Rechazar Documento y reidentificar en  Cajon Documento", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioObjCreado, False, Nothing, True))


        ' transiciones de fin
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Verificar Cajon Documento", "Gestion CajonDocumento", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Rechazar Documento en Cajon Documento", "Gestion CajonDocumento", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Rechazar Documento y reidentificar en  Cajon Documento", "Gestion CajonDocumento", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))


        '''''''''''''''''''''''''''''''
        ' publicar los controladores ''
        '''''''''''''''''''''''''''''''


        Dim ejClienteS, ejClienteC As Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN
        Dim clienteS, clienteC As Framework.Procesos.ProcesosDN.ClientedeFachadaDN

        'Se comprubea si ya existen los clientesFachada, y sino se crean
        Dim opAD As New Framework.Procesos.ProcesosAD.OperacionesAD()

        ejClienteS = opAD.RecuperarEjecutorCliente(CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion"), String))
        ejClienteC = opAD.RecuperarEjecutorCliente(CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente"), String))

        If ejClienteS Is Nothing Then
            clienteS = New Framework.Procesos.ProcesosDN.ClientedeFachadaDN()
            clienteS.Nombre = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion"), String)

            ejClienteS = New Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN()
            ejClienteS.ClientedeFachada = clienteS
        End If

        If ejClienteC Is Nothing Then
            clienteC = New Framework.Procesos.ProcesosDN.ClientedeFachadaDN()
            clienteC.Nombre = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente"), String)

            ejClienteC = New Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN()
            ejClienteC.ClientedeFachada = clienteC
        End If




        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Verificar Cajon Documento", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Rechazar Documento en Cajon Documento", RecuperarVinculoMetodo("CajonDocumentoRechazarVinculacion", GetType(GDocEntrantesLN.GDocsLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Rechazar Documento y reidentificar en  Cajon Documento", RecuperarVinculoMetodo("CajonDocumentoRechazarVinculacionyReVincular", GetType(GDocEntrantesLN.GDocsLN)), ejClienteS)))

        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Verificar Cajon Documento", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Rechazar Documento en Cajon Documento", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Rechazar Documento y reidentificar en  Cajon Documento", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))

        ' finc pruebas

        Me.GuardarDatos(ejClienteC)
        Me.GuardarDatos(ejClienteS)

    End Sub
    Public Sub CrearGrafoUsuariosPp()



        ' flujo de talones

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' 1º 
        ' dn o dns a las cuales se vincula el flujo
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim vc1DN As Framework.TiposYReflexion.DN.VinculoClaseDN = RecuperarVinculoClase(GetType(Framework.Usuarios.DN.PrincipalDN))
        Dim ColVc As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
        ColVc.Add(vc1DN)


        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' 2º 
        '  creacion de las operaciones
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        Dim colop As New Framework.Procesos.ProcesosDN.ColOperacionDN

        ' operacion que engloba todo el flujo
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Gestion de Principal", ColVc, "element_into.ico", True)))


        '' operacion de pueba
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Alta Principal", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Modificar Principal", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Baja Principal", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Reactivar Principal", ColVc, "element_into.ico", True)))


        '' FIN operacion de pueba


        ''''''''''''''''''''''''''''''''''''''''''''
        ' 3º
        ' creacion de las Transiciones
        ''''''''''''''''''''''''''''''''''''''''''''


        Dim colVM As New Framework.TiposYReflexion.DN.ColVinculoMetodoDN()


        '' prueba subordiandas ''''''''''''''''''''''''''''''''''''''

        ' transicion de inicio
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Gestion de Principal", "Alta Principal", Framework.Procesos.ProcesosDN.TipoTransicionDN.Inicio, False, Nothing, True))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Gestion de Principal", "Reactivar Principal", Framework.Procesos.ProcesosDN.TipoTransicionDN.Reactivacion, False, Nothing, True))

        ' transiciones corrientes
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Principal", "Modificar Principal", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Principal", "Baja Principal", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Reactivar Principal", "Modificar Principal", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Reactivar Principal", "Baja Principal", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Modificar Principal", "Modificar Principal", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Modificar Principal", "Baja Principal", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))


        ' transiciones de fin
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Baja Principal", "Gestion de Principal", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))


        '''''''''''''''''''''''''''''''
        ' publicar los controladores ''
        '''''''''''''''''''''''''''''''



        'Dim ejClienteS, ejClienteC As Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN
        'Dim clienteS, clienteC As Framework.Procesos.ProcesosDN.ClientedeFachadaDN

        '' crecion de los clientes del grafo
        'clienteS = New Framework.Procesos.ProcesosDN.ClientedeFachadaDN
        'clienteS.Nombre = "Servidor"

        'clienteC = New Framework.Procesos.ProcesosDN.ClientedeFachadaDN
        'clienteC.Nombre = "Cliente1"

        'ejClienteS = New Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN
        'ejClienteS.ClientedeFachada = clienteS

        'ejClienteC = New Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN
        'ejClienteC.ClientedeFachada = clienteC



        Dim ejClienteS, ejClienteC As Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN
        Dim clienteS, clienteC As Framework.Procesos.ProcesosDN.ClientedeFachadaDN

        'Se comprubea si ya existen los clientesFachada, y sino se crean
        Dim opAD As New Framework.Procesos.ProcesosAD.OperacionesAD()

        ejClienteS = opAD.RecuperarEjecutorCliente(CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion"), String))
        ejClienteC = opAD.RecuperarEjecutorCliente(CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente"), String))

        If ejClienteS Is Nothing Then
            clienteS = New Framework.Procesos.ProcesosDN.ClientedeFachadaDN()
            clienteS.Nombre = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion"), String)

            ejClienteS = New Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN()
            ejClienteS.ClientedeFachada = clienteS
        End If

        If ejClienteC Is Nothing Then
            clienteC = New Framework.Procesos.ProcesosDN.ClientedeFachadaDN()
            clienteC.Nombre = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente"), String)

            ejClienteC = New Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN()
            ejClienteC.ClientedeFachada = clienteC
        End If


        ' pruebas

        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Alta Principal", RecuperarVinculoMetodo("AltaPrincipalClavePropuesta", GetType(Framework.Usuarios.LN.PrincipalLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Reactivar Principal", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Modificar Principal", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Baja Principal", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Gestion de Principal", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))

        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Alta Principal", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Reactivar Principal", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Modificar Principal", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Baja Principal", RecuperarVinculoMetodo("BajaPolizaOper", GetType(FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizasOperLN)), ejClienteC)))
        ' ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion SumarCinco", RecuperarVinculoMetodo("SumarCinco", GetType(ProcesosLNC)), ejClienteC))

        ' finc pruebas

        Me.GuardarDatos(ejClienteC)
        Me.GuardarDatos(ejClienteS)

    End Sub



    'TODO: Este método podría llevarse a GBDBase
    Private Function RecuperarVinculoMetodo(ByVal nombreMetodo As String, ByVal tipo As System.Type) As Framework.TiposYReflexion.DN.VinculoMetodoDN
        Dim vm As Framework.TiposYReflexion.DN.VinculoMetodoDN

        Using New CajonHiloLN(mRecurso)
            Dim tyrLN As New Framework.TiposYReflexion.LN.TiposYReflexionLN()
            vm = New Framework.TiposYReflexion.DN.VinculoMetodoDN(nombreMetodo, New Framework.TiposYReflexion.DN.VinculoClaseDN(tipo))

            Return tyrLN.CrearVinculoMetodo(vm.RecuperarMethodInfo())
        End Using

    End Function

    'TODO: Este método podría llevarse a GBDBase
    Private Function RecuperarVinculoClase(ByVal tipo As System.Type) As Framework.TiposYReflexion.DN.VinculoClaseDN
        Using New CajonHiloLN(mRecurso)
            Dim tyrLN As New Framework.TiposYReflexion.LN.TiposYReflexionLN()
            Return tyrLN.CrearVinculoClase(tipo)
        End Using

    End Function

    Private Function GuardarDatos(ByVal pEntidad As IEntidadDN) As IEntidadDN

        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.Guardar(pEntidad)

        Return pEntidad

    End Function

End Class

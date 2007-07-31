Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports Framework.DatosNegocio
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Usuarios.DN
Imports Framework.Procesos.ProcesosLN
Imports FN.Localizaciones.DN

Imports GestionSegurosAMV.AD

<TestClass()> Public Class UnitTest1

#Region "Additional test attributes"
    '
    ' You can use the following additional attributes as you write your tests:
    '
    ' Use ClassInitialize to run code before running the first test in the class
    ' <ClassInitialize()> Public Shared Sub MyClassInitialize(ByVal testContext As TestContext)
    ' End Sub
    '
    ' Use ClassCleanup to run code after all tests in a class have run
    ' <ClassCleanup()> Public Shared Sub MyClassCleanup()
    ' End Sub
    '
    ' Use TestInitialize to run code before running each test
    ' <TestInitialize()> Public Sub MyTestInitialize()
    ' End Sub
    '
    ' Use TestCleanup to run code after each test has run
    ' <TestCleanup()> Public Sub MyTestCleanup()
    ' End Sub
    '
#End Region

    Dim mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN

    <TestMethod()> Public Sub CrearEntorno()

        CrearEntornoP()


    End Sub

    <TestMethod(), Timeout(57000000)> Public Sub CargarDatosBasicos()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            Dim gpt As New GestionPagosLNTest.UnitTest1
            gpt.mRecurso = Me.mRecurso
            gpt.CargarDatos()

            Using tr As New Transaccion()

                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN



                Dim map As FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN = New FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN

                map = New FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN
                map.VCOrigenImpdev = RecuperarVinculoClase(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN))
                map.VCLiquidadorConcreto = RecuperarVinculoClase(GetType(FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.LiquidadorConcretoRVLN))
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(map)

                map = New FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN
                map.VCOrigenImpdev = RecuperarVinculoClase(GetType(FN.GestionPagos.DN.LiquidacionPagoDN))
                map.VCLiquidadorConcreto = RecuperarVinculoClase(GetType(FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.LiquidadorConcretoRVLN))

                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(map)

                ' dar de alata la entidad encargada de emiitr polizas

                Dim emi As New FN.Seguros.Polizas.DN.EmisoraPolizasDN()
                Dim empln As New FN.Empresas.LN.EmpresaLN()

                emi.EnidadFiscalGenerica = empln.RecuperarEmpresaFiscalxCIF("B83204586").EntidadFiscalGenerica
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(emi)



                ' crear los plazos de ejecucion para cada modalidad de pago
                CrearPlazosEjecucion()





                Dim config As New FN.Seguros.Polizas.DN.ConstatesConfigurablesSegurosDN
                config.Periodo.FI = Now.AddYears(-5)
                config.ValorBonificacionSiniestros = 0.95
                config.ValorMalificacionSiniestros = 1.25

                Me.GuardarDatos(config)


                ' CrearMapeadosLiquidacionp()


                ' crear datos de GDE
                Dim miGDocEntrantesFSTest As New GDocEntrantesFSTest.GDocEntrantesFSTest
                miGDocEntrantesFSTest.CargarDatosTodosp()

                CrearAdministradorTotal()

                tr.Confirmar()

            End Using

        End Using

    End Sub

    <TestMethod()> Public Sub CrearMapeadosLiquidacion()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)

            CrearMapeadosLiquidacionp()

        End Using
    End Sub
    Private Sub CrearMapeadosLiquidacionp()



        Using tr As New Transaccion



            'Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            Dim bln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN

            ' recuperamos la coleccion de coberturas ,comisiones  y impuestos
            Dim ColCobertura As New FN.Seguros.Polizas.DN.ColCoberturaDN
            ColCobertura.AddRangeObject(bln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

            Dim ColImpuesto As New FN.RiesgosVehiculos.DN.ColImpuestoDN
            ColImpuesto.AddRangeObject(bln.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.ImpuestoDN)))

            Dim ColComision As New FN.RiesgosVehiculos.DN.ColComisionDN
            ColComision.AddRangeObject(bln.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.ComisionDN)))

            Dim ColEntidadFiscalGenerica As New FN.Localizaciones.DN.ColEntidadFiscalGenericaDN
            ColEntidadFiscalGenerica.AddRangeObject(bln.RecuperarLista(GetType(FN.Localizaciones.DN.EntidadFiscalGenericaDN)))

            Dim colmapLik As New FN.GestionPagos.DN.ColLiquidacionMapDN
            Dim mapLik As FN.GestionPagos.DN.LiquidacionMapDN

            mapLik = New FN.GestionPagos.DN.LiquidacionMapDN
            'mapLik.Aplazamiento = New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias
            mapLik.EntidadLiquidadora = ColEntidadFiscalGenerica(2)
            mapLik.HeCausaLiquidacion = New Framework.DatosNegocio.HEDN(ColCobertura.RecuperarXNombre("RCO")(0))
            mapLik.TipoCalculoImporte = FN.GestionPagos.DN.TipoCalculoImporte.Porcentual
            mapLik.PorcentageOValor = 0.5
            colmapLik.Add(mapLik)

            mapLik = New FN.GestionPagos.DN.LiquidacionMapDN
            'mapLik.Aplazamiento = New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias
            mapLik.EntidadLiquidadora = ColEntidadFiscalGenerica(3)
            mapLik.HeCausaLiquidacion = New Framework.DatosNegocio.HEDN(ColImpuesto.RecuperarPrimeroXNombre("Carta verde"))
            mapLik.TipoCalculoImporte = FN.GestionPagos.DN.TipoCalculoImporte.Porcentual
            mapLik.PorcentageOValor = 1
            colmapLik.Add(mapLik)

            mapLik = New FN.GestionPagos.DN.LiquidacionMapDN
            'mapLik.Aplazamiento = New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias
            'mapLik.EntidadLiquidadora = ColEntidadFiscalGenerica(4)
            mapLik.CodGrupoLiquidacion = "COLABORADORES-Comerciales"
            ' mapLik.HeCausaLiquidacion = New Framework.DatosNegocio.HEDN(ColImpuesto.RecuperarPrimeroXNombre("Carta verde"))
            mapLik.CausaPrimaModulada = True

            mapLik.TipoCalculoImporte = FN.GestionPagos.DN.TipoCalculoImporte.Porcentual
            mapLik.PorcentageOValor = 0.005
            colmapLik.Add(mapLik)


            Me.GuardarDatos(colmapLik)


            tr.Confirmar()

        End Using


    End Sub

    Private Sub CrearPlazosEjecucion()



        Using tr As New Transaccion


            Dim PlazoEfecto As FN.GestionPagos.DN.PlazoEfectoDN
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            PlazoEfecto = New FN.GestionPagos.DN.PlazoEfectoDN
            PlazoEfecto.PlazoEjecucion.Dias = 4
            PlazoEfecto.ModalidadDePago = FN.GestionPagos.DN.ModalidadPago.Domiciliacion
            PlazoEfecto.FI = Now
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
            gi.Guardar(PlazoEfecto)


            PlazoEfecto = New FN.GestionPagos.DN.PlazoEfectoDN
            PlazoEfecto.PlazoEjecucion.Dias = 7
            PlazoEfecto.ModalidadDePago = FN.GestionPagos.DN.ModalidadPago.IngresoEnCuenta
            PlazoEfecto.FI = Now
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
            gi.Guardar(PlazoEfecto)


            PlazoEfecto = New FN.GestionPagos.DN.PlazoEfectoDN
            PlazoEfecto.PlazoEjecucion.Dias = 7
            PlazoEfecto.ModalidadDePago = FN.GestionPagos.DN.ModalidadPago.Talon
            PlazoEfecto.FI = Now
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
            gi.Guardar(PlazoEfecto)

            PlazoEfecto = New FN.GestionPagos.DN.PlazoEfectoDN
            PlazoEfecto.PlazoEjecucion.Dias = 7
            PlazoEfecto.ModalidadDePago = FN.GestionPagos.DN.ModalidadPago.Tranferencia
            PlazoEfecto.FI = Now
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
            gi.Guardar(PlazoEfecto)


            tr.Confirmar()

        End Using


    End Sub



    <TestMethod()> Public Sub CrearGrafoTarificacion()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            Using tr As New Transaccion()
                Dim CargaDatosTestRV As New FN.RiesgosVehiculos.Test.CargaDatosTest()
                CargaDatosTestRV.CrearGrafoTarificacionP()

                tr.Confirmar()
            End Using


        End Using
    End Sub

    <TestMethod()> Public Sub CargarImpuesto()
        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)
            Using tr As New Transaccion()

                Dim CargaDatosTestRV As New FN.RiesgosVehiculos.Test.CargaDatosTest()
                CargaDatosTestRV.CargarImpuestoP()

                tr.Confirmar()
            End Using

        End Using
    End Sub

    <TestMethod()> Public Sub CargarComisiones()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            Using tr As New Transaccion()

                Dim CargaDatosTestRV As New FN.RiesgosVehiculos.Test.CargaDatosTest()
                CargaDatosTestRV.CargarComisionesP()

                tr.Confirmar()
            End Using


        End Using
    End Sub

    <Timeout(3600000)> <TestMethod()> Public Sub CargarModuladores2Conductor()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            Using tr As New Transaccion()
                Dim CargaDatosTestRV As New FN.RiesgosVehiculos.Test.CargaDatosTest()
                CargaDatosTestRV.CargarModuladores2ConductorP()

                tr.Confirmar()
            End Using


        End Using
    End Sub

    <TestMethod()> Public Sub CargarModuladores()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            Using tr As New Transaccion()
                Dim CargaDatosTestRV As New FN.RiesgosVehiculos.Test.CargaDatosTest()
                CargaDatosTestRV.CargarModuladoresP()

                tr.Confirmar()
            End Using

        End Using
    End Sub

    <TestMethod()> Public Sub CargarPrimasBase()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            Using tr As New Transaccion()
                Dim CargaDatosTestRV As New FN.RiesgosVehiculos.Test.CargaDatosTest()
                CargaDatosTestRV.CargarPrimasBaseP()

                tr.Confirmar()
            End Using

        End Using
    End Sub

    <TestMethod()> Public Sub pe1v0CrearNOrigenImportedebidoManual()

        Dim n As Integer = 200


        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)



            Using tr As New Transaccion


                ' crear unos pagos de pruebas con sus origenes
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


                ' recuperar empresas

                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

                Dim coldeudoras As New FN.Localizaciones.DN.ColEntidadFiscalGenericaDN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                coldeudoras.AddRangeObject(bgln.RecuperarLista(GetType(FN.Localizaciones.DN.EntidadFiscalGenericaDN)))

                Dim iefacreedora As FN.Localizaciones.DN.EntidadFiscalGenericaDN = coldeudoras.RecuperarPorIdentificacionFiscal("B83204586")
                coldeudoras.EliminarEntidadDNxGUID(iefacreedora.GUID)


                If iefacreedora Is Nothing Then
                    Throw New ApplicationException
                End If


                For a As Integer = 0 To n
                    ' crear el origen debido
                    Dim origen As FN.GestionPagos.DN.OrigenIdevBaseDN
                    origen = CrearOrigenImportedebido(iefacreedora, coldeudoras)
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                    gi.Guardar(origen)



                Next


                'Dim ln As New FN.GestionPagos.LN.ApunteImpDLN
                'Dim saldo As Double = ln.Saldo(origen.IImporteDebidoDN.Acreedora, origen.IImporteDebidoDN.Deudora, origen.IImporteDebidoDN.FEfecto)

                'If saldo <> origen.IImporteDebidoDN.Importe Then
                '    Throw New ApplicationException
                'End If

                tr.Confirmar()


            End Using
        End Using


    End Sub

    Public Function CrearOrigenImportedebido(ByVal acreedora As EntidadFiscalGenericaDN, ByVal coldeudoras As ColEntidadFiscalGenericaDN) As FN.GestionPagos.DN.IOrigenIImporteDebidoDN
        Dim origen As FN.GestionPagos.DN.OrigenIdevBaseDN
        origen = New FN.GestionPagos.DN.OrigenIdevBaseDN

        Dim aleatorio As New Random


        origen.IImporteDebidoDN = New FN.GestionPagos.DN.ApunteImpDDN(origen)
        origen.IImporteDebidoDN.Importe = 100
        origen.IImporteDebidoDN.Acreedora = acreedora
        origen.IImporteDebidoDN.Deudora = coldeudoras.Item(aleatorio.Next(0, coldeudoras.Count - 1))

        origen.IImporteDebidoDN.FCreación = Now
        origen.IImporteDebidoDN.FEfecto = Now.AddDays(5)
        Return origen

    End Function

    <TestMethod(), Timeout(57000000)> Public Sub CargarDatosTarificador()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            Using tr As New Transaccion()
                CargarDatosTarificadorP()

                tr.Confirmar()
            End Using

        End Using
    End Sub

    <TestMethod(), Timeout(57000000)> Public Sub CargarDatosProductosAMV()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            CargarDatosProductosAMVP()
        End Using
    End Sub

    <TestMethod(), Timeout(57000000)> Public Sub CargarDocumetosRequeridos()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            CargarDocumetosRequeridosP()
        End Using
    End Sub

    <TestMethod()> Public Sub CrearEntidadColaboradora()


        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)

            Using tr As New Transaccion
                'Dim vcln As New Framework.TiposYReflexion.LN.TiposYReflexionLN
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                Dim bln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN

                Dim colempreas As New FN.Empresas.DN.ColEmpresasDN
                colempreas.AddRangeObject(bln.RecuperarLista(GetType(FN.Empresas.DN.EmpresaDN)))

                Dim ec As New FN.Empresas.DN.EntidadColaboradoraDN
                ec.CodigoColaborador = "123"
                ec.EntidadAsociada = colempreas(2)

                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(ec)



                tr.Confirmar()

            End Using


        End Using



    End Sub

    <TestMethod()> Public Sub CargarDatosPruebas()


        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            Dim UnitTest1RV As New FN.RiesgosVehiculos.Test.UnitTest1
            UnitTest1RV.CargarDatos()

            CargarDatosTarificador()
            CargarDatosProductosAMV()

            'UnitTest1RV.pe0v1CrearPoliza()
            'UnitTest1RV.pe0v2efectuarSiguientePagoPoliza()

            UnitTest1RV.Pre1v0GuardarPresupuesto()


            Using tr As New Transaccion
                'Dim vcln As New Framework.TiposYReflexion.LN.TiposYReflexionLN
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

                Dim map As FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN = New FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN
                map = New FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN
                'map.VCOrigenImpdev = New Framework.TiposYReflexion.DN.VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN))
                'map.VCLiquidadorConcreto = New Framework.TiposYReflexion.DN.VinculoClaseDN(GetType(GestionPagosLNTest.LiquidadorConcretoPruebaLN)) ' ojo esto habria que cambiarlo porque se desconocen cuales son las liquidaciones para un paago manual
                map.VCOrigenImpdev = RecuperarVinculoClase(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN))
                map.VCLiquidadorConcreto = RecuperarVinculoClase(GetType(GestionPagosLNTest.LiquidadorConcretoPruebaLN)) ' ojo esto habria que cambiarlo porque se desconocen cuales son las liquidaciones para un paago manual


                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(map)

                ' dar de alata la entidad encargada de emiitr polizas


                Dim emi As New FN.Seguros.Polizas.DN.EmisoraPolizasDN()
                Dim empln As New FN.Empresas.LN.EmpresaLN()

                emi.EnidadFiscalGenerica = empln.RecuperarEmpresaFiscalxCIF("B83204586").EntidadFiscalGenerica
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(emi)

                'CrearAdministradorTotal()

                tr.Confirmar()

            End Using


        End Using



    End Sub

    <TestMethod()> Public Sub CrearGrafoUsuarios()

        CrearGrafoUsuariosP()

    End Sub

    <TestMethod()> Public Sub CrearGrafoDocumentos()

        CrearGrafoDocumentosP()

    End Sub
  
    <TestMethod()> Public Sub CrearGrafoPolizas()

        CrearGrafoPolizasP()

    End Sub

    <TestMethod()> Public Sub CrearPolizaEnGrafo()
        CrearPolizaEnGrafoP()
    End Sub

    <TestMethod()> Public Sub CrearGrafoReclamaciones()
        CrearGrafoReclamacionesP()
    End Sub
    <TestMethod()> Public Sub CargarAntecedentes()
        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)
            CargarAntecedentesP()
        End Using
    End Sub


    Public Sub PublicarReferencias()
        Dim ln As New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)

        Dim ensamblado As System.Reflection.Assembly

        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)

        ensamblado = GetType(Framework.Tarificador.TarificadorDN.ValorModMapDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)
        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)
        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(Framework.Operaciones.OperacionesDN.OperResultCacheDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)

        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(Framework.OperProg.OperProgDN.AlertaDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)

        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(Framework.Procesos.ProcesosDN.OperacionDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)

        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(Framework.TiposYReflexion.DN.RelacionSQLsDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)


        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(Framework.Usuarios.DN.RolDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)



        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(FN.Seguros.Polizas.DN.CoberturaDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)

        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(FN.RiesgosVehiculos.DN.CategoriaDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)

        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(FN.RiesgosVehiculos.DN.CategoriaModDatosDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)


        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(FN.Empresas.DN.ActividadDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)

        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(FN.Financiero.DN.CCCDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)

        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(FN.GestionPagos.DN.ApunteImpDDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)

        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(FN.Localizaciones.DN.ArbolZonasDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)

        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(FN.Personas.DN.PersonaDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)



        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(FN.RiesgosVehiculos.DN.MatriculaDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)


        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(FN.Seguros.Polizas.DN.TarifaDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)


        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ensamblado = GetType(FN.Trabajos.DN.AgenteDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)

    End Sub

    Public Sub PublicarFachada()
        Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("UsuariosFS", mRecurso)
        ' Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("ProcesosFS", mRecurso)
    End Sub

    Private Sub CrearEntornoP()
        ObtenerRecurso()

        Dim gbd As Framework.AccesoDatos.MotorAD.GBDBase

        Using New CajonHiloLN(mRecurso)

            gbd = New GSAMV.AD.GestionSegurosAMVGBDAD(mRecurso)

            gbd.EliminarVistas()
            gbd.EliminarRelaciones()
            gbd.EliminarTablas()

            gbd.CrearTablas()
            gbd.CrearVistas()



            'PublicarFachada()
            PublicarReferencias()

            'PublicarGrafoPruebas()
            'CrearAdministradorTotal()

        End Using
    End Sub

    Private Sub CrearGrafoPolizasP()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion") Is Nothing Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolAplicacion", "RolServidorGSAMV")
            End If
            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente") Is Nothing Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolCliente", "RolClienteGSAMV")
            End If

            Dim gbdAMV As New GSAMV.AD.GestionSegurosAMVGBDAD(mRecurso)
            gbdAMV.PublicarGrafoPolizas()
        End Using
    End Sub

    Private Sub CrearGrafoReclamacionesP()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion") Is Nothing Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolAplicacion", "RolServidorGSAMV")
            End If
            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente") Is Nothing Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolCliente", "RolClienteGSAMV")
            End If

            Dim gbdAMV As New GSAMV.AD.GestionSegurosAMVGBDAD(mRecurso)
            gbdAMV.CrearGrafoReclamacionesPp()
        End Using
    End Sub
    Private Sub CrearGrafoDocumentosP()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion") Is Nothing Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolAplicacion", "RolServidorGSAMV")
            End If
            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente") Is Nothing Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolCliente", "RolClienteGSAMV")
            End If

            Dim gbdAMV As New GSAMV.AD.GestionSegurosAMVGBDAD(mRecurso)
            gbdAMV.CrearGrafoDocumentosPp()
        End Using
    End Sub

    Private Function CrearAdministradorTotal() As PrincipalDN

        Using tr As New Transaccion()

            Dim userln As New Framework.Usuarios.LN.UsuariosLN(Transaccion.Actual, Recurso.Actual)
            CrearAdministradorTotal = userln.CrearAdministradorTotal("gestion", "gestionAMV.123")

            tr.Confirmar()

        End Using

    End Function


    Private Sub CrearGrafoUsuariosP()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion") Is Nothing Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolAplicacion", "RolServidorGSAMV")
            End If
            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente") Is Nothing Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolCliente", "RolClienteGSAMV")
            End If

            Dim gbdAMV As New GSAMV.AD.GestionSegurosAMVGBDAD(mRecurso)
            gbdAMV.CrearGrafoUsuariosPp()
        End Using
    End Sub

    Private Sub CrearPolizaEnGrafoP()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)

            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion") Is Nothing Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolAplicacion", "RolServidorGSAMV")
            End If
            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente") Is Nothing Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolCliente", "RolClienteGSAMV")
            End If

            '1º se crea el objeto póliza completo
            Dim bln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim polpr As New FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN
            Dim polpr2 As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN
            polpr = bln.RecuperarGenerico("1", GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN))
            polpr2 = polpr.CloneSinIdentidad()

            Dim polprhe As New Framework.DatosNegocio.HEDN(polpr2)

            '2º se guarda la operación que inicia el flujo de operaciones para el grafo de la póliza
            Dim opPOrg As New Framework.Procesos.ProcesosDN.OperacionDN()
            Dim opPDes As New Framework.Procesos.ProcesosDN.OperacionDN()

            bln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()

            Dim colOps As New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOps.AddRangeObject(bln.RecuperarLista(GetType(Framework.Procesos.ProcesosDN.OperacionDN)))
            opPOrg = colOps.RecuperarPrimeroXNombre("Gestión Pólizas")
            opPDes = colOps.RecuperarPrimeroXNombre("Modificar Presupuesto")

            Dim oprOrg As New Framework.Procesos.ProcesosDN.OperacionRealizadaDN()
            Dim oprDes As New Framework.Procesos.ProcesosDN.OperacionRealizadaDN()
            oprOrg.Operacion = opPOrg
            oprDes.Operacion = opPOrg

            Dim prin As PrincipalDN
            bln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            prin = bln.RecuperarGenerico("1", GetType(Framework.Usuarios.DN.PrincipalDN))

            oprOrg.SujetoOperacion = prin
            oprOrg.ObjetoIndirectoOperacion = polpr2
            oprDes.SujetoOperacion = prin
            oprDes.ObjetoIndirectoOperacion = polpr2

            Dim pr As New Framework.Procesos.ProcesosLN.OperacionesLN()
            Dim colTRA As New Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN()
            colTRA = pr.RecuperarTransicionesAutorizadasSobreDeINICIO(prin, polprhe)

            Dim tr As Framework.Procesos.ProcesosDN.TransicionRealizadaDN
            tr = colTRA.Item(0)

            tr.OperacionRealizadaOrigen = oprOrg
            tr.OperacionRealizadaDestino = oprDes

            Dim ejOPR As New Framework.Procesos.ProcesosLN.GestorOPRLN()
            ejOPR.EjecutarOperacion(polpr2, Nothing, prin, tr)


            'Me.GuardarDatos(tr)

        End Using

    End Sub


    Private Sub CargarDatosProductosAMVP()
        Dim bLN As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

        Using tr As New Transaccion()

            'Me.CrearEmpresaAMV()

            bLN = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim colCob As New FN.Seguros.Polizas.DN.ColCoberturaDN()
            Dim lstCob As System.Collections.IList = bLN.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN))
            For Each eltoCob As FN.Seguros.Polizas.DN.CoberturaDN In lstCob
                colCob.Add(eltoCob)
            Next

            Dim colProductos As New FN.Seguros.Polizas.DN.ColProductoDN()
            Dim producto As FN.Seguros.Polizas.DN.ProductoDN

            'BASIC
            Dim productoB As FN.Seguros.Polizas.DN.ProductoDN
            productoB = New FN.Seguros.Polizas.DN.ProductoDN()
            productoB.Nombre = "BASIC"
            productoB.ColCoberturas.Add(colCob.RecuperarPrimeroXNombre("RCO"))
            productoB.ColCoberturas.Add(colCob.RecuperarPrimeroXNombre("RCV"))
            productoB.ColCoberturas.Add(colCob.RecuperarPrimeroXNombre("DEF"))
            colProductos.Add(productoB)

            'RI
            Dim productoRI As FN.Seguros.Polizas.DN.ProductoDN
            productoRI = New FN.Seguros.Polizas.DN.ProductoDN()
            productoRI.Nombre = "RI"
            productoRI.ColCoberturas.Add(colCob.RecuperarPrimeroXNombre("RI"))
            productoRI.ColProdDependientes.Add(productoB)
            colProductos.Add(productoRI)

            'TR
            producto = New FN.Seguros.Polizas.DN.ProductoDN()
            producto.Nombre = "TR"
            producto.ColCoberturas.Add(colCob.RecuperarPrimeroXNombre("DAÑOS"))
            producto.ColProdDependientes.Add(productoB)
            producto.ColProdDependientes.Add(productoRI)
            colProductos.Add(producto)
            GuardarDatos(producto)

            'AC
            producto = New FN.Seguros.Polizas.DN.ProductoDN()
            producto.Nombre = "AC"
            producto.ColCoberturas.Add(colCob.RecuperarPrimeroXNombre("AC"))
            producto.ColProdDependientes.Add(productoB)
            colProductos.Add(producto)
            GuardarDatos(producto)

            'AC
            producto = New FN.Seguros.Polizas.DN.ProductoDN()
            producto.Nombre = "AV"
            producto.ColCoberturas.Add(colCob.RecuperarPrimeroXNombre("AV"))
            producto.ColProdDependientes.Add(productoB)
            colProductos.Add(producto)
            GuardarDatos(producto)

            bLN = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()

            Dim lstFrc As System.Collections.IList = bLN.RecuperarLista(GetType(FN.GestionPagos.DN.FraccionamientoDN))
            For Each eltoFrc As FN.GestionPagos.DN.FraccionamientoDN In lstFrc

                Dim limFrac As New FN.GestionPagos.DN.LimiteMinFraccionamientoDN()

                limFrac.FI = New Date(2002, 8, 30)
                limFrac.Fraccionamiento = eltoFrc
                If eltoFrc.NumeroPagos = 2 Then
                    limFrac.ValorMinimoFrac = 200
                ElseIf eltoFrc.NumeroPagos = 4 Then
                    limFrac.ValorMinimoFrac = 400
                End If

                GuardarDatos(limFrac)

            Next


            tr.Confirmar()

        End Using

    End Sub

    Private Sub CrearEmpresaAMV()
        Dim localidad As Framework.DatosNegocio.IEntidadDN

        Dim calle As FN.Localizaciones.DN.TipoViaDN = Nothing

        Dim objLN As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
        Dim lista As System.Collections.IList

        Using tr As New Transaccion()

            lista = objLN.RecuperarListaCondicional(GetType(FN.Localizaciones.DN.LocalidadDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlLocalidadDN", "Nombre", "Alcobendas"))
            localidad = lista.Item(0)

            Dim DireccionNoUnica As FN.Localizaciones.DN.DireccionNoUnicaDN

            DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()

            Dim listaTipoVia As System.Collections.IList = objLN.RecuperarLista(GetType(FN.Localizaciones.DN.TipoViaDN))
            For Each tipoVia As FN.Localizaciones.DN.TipoViaDN In listaTipoVia
                If tipoVia.Nombre = "AVENIDA" Then
                    calle = tipoVia
                    Exit For
                End If
            Next

            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()
            DireccionNoUnica.TipoVia = calle
            DireccionNoUnica.Via = "Bruselas"
            DireccionNoUnica.Numero = 38
            DireccionNoUnica.CodPostal = 28108
            DireccionNoUnica.Localidad = localidad

            Me.GuardarDatos(DireccionNoUnica)


            '''''''''''''''''''''''''''''
            ' Se crean los datos de Empresas relacioandas
            '''''''''' ''''''''''''''''''''''''''
            Dim empresa As FN.Empresas.DN.EmpresaDN
            Dim empresaFiscal As FN.Empresas.DN.EmpresaFiscalDN

            Dim sede As FN.Empresas.DN.SedeEmpresaDN
            Dim tipoSede As FN.Empresas.DN.TipoSedeDN


            empresaFiscal = New FN.Empresas.DN.EmpresaFiscalDN()
            empresaFiscal.IdentificacionFiscal = New FN.Localizaciones.DN.CifDN("B83204586")
            empresaFiscal.RazonSocial = "AMV Hispania S.L."
            empresaFiscal.DomicilioFiscal = DireccionNoUnica
            empresaFiscal.NombreComercial = "AMV Hispania"

            empresa = New FN.Empresas.DN.EmpresaDN
            empresa.TipoEmpresaDN = New FN.Empresas.DN.TipoEmpresaDN()
            empresa.TipoEmpresaDN.Nombre = "Correduría de seguros"
            empresa.EntidadFiscal = empresaFiscal.EntidadFiscalGenerica

            Me.GuardarDatos(empresaFiscal)
            Me.GuardarDatos(empresa)

            sede = New FN.Empresas.DN.SedeEmpresaDN()
            sede.Nombre = "Alcobendas"
            tipoSede = New FN.Empresas.DN.TipoSedeDN()
            tipoSede.Nombre = "Central"
            sede.TipoSede = tipoSede
            sede.SedePrincipal = True
            sede.Empresa = empresa
            sede.Direccion = DireccionNoUnica
            Me.GuardarDatos(sede)

            tr.Confirmar()

        End Using

    End Sub

    Private Sub CargarDatosTarificadorP()
        Using tr As New Transaccion()
            Dim CargaDatosTestRV As New FN.RiesgosVehiculos.Test.CargaDatosTest()

            CargaDatosTestRV.CargarCategoriaModDatos()
            CargaDatosTestRV.CargarPrimasBaseP()
            CargaDatosTestRV.CargarModuladoresP()
            CargaDatosTestRV.CargarModuladores2ConductorP()
            CargaDatosTestRV.CargarComisionesP()
            CargaDatosTestRV.CargarImpuestoP()
            CargaDatosTestRV.CargarFraccionamientosP()
            CargaDatosTestRV.CargarBonificaciones()
            CargaDatosTestRV.CrearGrafoTarificacionP()

            tr.Confirmar()

        End Using
    End Sub

    Private Sub CargarAntecedentesP()
        Using tr As New Transaccion()
            Dim CargaDatosTestRV As New FN.RiesgosVehiculos.Test.CargaDatosTest()
            CargaDatosTestRV.CargarAntecedentes()
            tr.Confirmar()
        End Using
    End Sub

    Private Sub CargarDocumetosRequeridosP()
        Using tr As New Transaccion()
            Dim CargaDatosTestRV As New FN.RiesgosVehiculos.Test.CargaDatosTest()
            CargaDatosTestRV.CargarDocumentosRequeridosP()

            tr.Confirmar()

        End Using

    End Sub


    Public Function GuardarDatos(ByVal pEntidad As Object) As Object

        '   ObtenerRecurso()


        Using tr As New Transaccion


            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(pEntidad)
            tr.Confirmar()
            Return pEntidad



        End Using





    End Function

    Private Function RecuperarVinculoMetodo(ByVal nombreMetodo As String, ByVal tipo As System.Type) As Framework.TiposYReflexion.DN.VinculoMetodoDN
        Dim vm As Framework.TiposYReflexion.DN.VinculoMetodoDN

        Using New CajonHiloLN(mRecurso)
            Dim tyrLN As New Framework.TiposYReflexion.LN.TiposYReflexionLN()
            vm = New Framework.TiposYReflexion.DN.VinculoMetodoDN(nombreMetodo, New Framework.TiposYReflexion.DN.VinculoClaseDN(tipo))

            Return tyrLN.CrearVinculoMetodo(vm.RecuperarMethodInfo())
        End Using

    End Function

    Private Function RecuperarVinculoClase(ByVal tipo As System.Type) As Framework.TiposYReflexion.DN.VinculoClaseDN
        Using New CajonHiloLN(mRecurso)
            Dim tyrLN As New Framework.TiposYReflexion.LN.TiposYReflexionLN()
            Return tyrLN.CrearVinculoClase(tipo)
        End Using

    End Function

    Private Sub ObtenerRecurso()

        Dim connectionstring As String
        Dim htd As New Dictionary(Of String, Object)

        connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd='sa'"
        'connectionstring = "server=localhost;database=AMVDbd;user=sa;pwd='sa'"
        'connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd='sa'"

        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GSAMV.AD.GestorMapPersistenciaCamposGSAMV()

    End Sub

End Class
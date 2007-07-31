Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports Framework.Usuarios.DN
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.DatosNegocio
Imports Framework
Imports Framework.TiposYReflexion.DN
Imports System.Collections
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Procesos.ProcesosLN

<TestClass()> Public Class UsuariosTest

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









    Public Sub PublicarGrafoPruebasProcesos()
        ' flujo de talones

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' 1º 
        ' dn o dns a las cuales se vincula el flujo
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        ' Dim vc1DN As Framework.TiposYReflexion.DN.VinculoClaseDN = RecuperarVinculoClase(GetType(Framework.Usuarios.DN.PrincipalDN))
        Dim ColVc As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
        ColVc.Add(RecuperarVinculoClase(GetType(EntidadDePruebaDN)))
        ColVc.Add(RecuperarVinculoClase(GetType(ContenedoraEntidadDePruebaDN)))


        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' 2º 
        '  creacion de las operaciones
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        Dim colop As New Framework.Procesos.ProcesosDN.ColOperacionDN

        ' operacion que engloba todo el flujo
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Gestion de ContenedoraEntidadDePruebaDN", ColVc, "element_into.ico", True)))


        '' operacion de pueba
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Alta ContenedoraEntidadDePruebaDN desde EntidadDePruebaDN", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Alta ContenedoraEntidadDePruebaDN", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Modificar ContenedoraEntidadDePruebaDN", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Baja ContenedoraEntidadDePruebaDN", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Reactivar ContenedoraEntidadDePruebaDN", ColVc, "element_into.ico", True)))


        '' FIN operacion de pueba



        ''''''''''''''''''''''''''''''''''''''''''''
        ' 3º
        ' creacion de las Transiciones
        ''''''''''''''''''''''''''''''''''''''''''''


        Dim colVM As New ColVinculoMetodoDN()


        '' prueba subordiandas ''''''''''''''''''''''''''''''''''''''

        ' transicion de inicio
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Gestion de ContenedoraEntidadDePruebaDN", "Alta ContenedoraEntidadDePruebaDN desde EntidadDePruebaDN", Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioDesde, False, Nothing, True))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Gestion de ContenedoraEntidadDePruebaDN", "Alta ContenedoraEntidadDePruebaDN", Framework.Procesos.ProcesosDN.TipoTransicionDN.Inicio, False, Nothing, True))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Gestion de ContenedoraEntidadDePruebaDN", "Reactivar ContenedoraEntidadDePruebaDN", Framework.Procesos.ProcesosDN.TipoTransicionDN.Reactivacion, False, Nothing, True))

        ' transiciones corrientes
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta ContenedoraEntidadDePruebaDN desde EntidadDePruebaDN", "Modificar ContenedoraEntidadDePruebaDN", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta ContenedoraEntidadDePruebaDN", "Modificar ContenedoraEntidadDePruebaDN", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Reactivar ContenedoraEntidadDePruebaDN", "Modificar ContenedoraEntidadDePruebaDN", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Modificar ContenedoraEntidadDePruebaDN", "Modificar ContenedoraEntidadDePruebaDN", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Modificar ContenedoraEntidadDePruebaDN", "Baja ContenedoraEntidadDePruebaDN", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))



        ' transiciones de fin
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Baja ContenedoraEntidadDePruebaDN", "Gestion de ContenedoraEntidadDePruebaDN", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))



        '''''''''''''''''''''''''''''''
        ' publicar los controladores ''
        '''''''''''''''''''''''''''''''



        If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion") Is Nothing Then
            Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolAplicacion", "RolServidorPruebas")
        End If
        If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente") Is Nothing Then
            Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolCliente", "RolClientePruebas")
        End If

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

        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Alta ContenedoraEntidadDePruebaDN desde EntidadDePruebaDN", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Alta ContenedoraEntidadDePruebaDN", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Reactivar ContenedoraEntidadDePruebaDN", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Modificar ContenedoraEntidadDePruebaDN", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Baja ContenedoraEntidadDePruebaDN", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Gestion de ContenedoraEntidadDePruebaDN", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))

        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Alta ContenedoraEntidadDePruebaDN desde EntidadDePruebaDN", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Alta ContenedoraEntidadDePruebaDN", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Reactivar ContenedoraEntidadDePruebaDN", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Modificar ContenedoraEntidadDePruebaDN", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Baja ContenedoraEntidadDePruebaDN", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC))

        ' finc pruebas










        Me.GuardarDatos(ejClienteC)
        Me.GuardarDatos(ejClienteS)

    End Sub




    <TestMethod()> Public Sub ProbarOperacionInicioDesde()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)





            'Using tr As New Transaccion
            '    Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion") = "RolServidorGSAMV"
            '    Dim usln As New Framework.Usuarios.LN.UsuariosLN(Transaccion.Actual, Recurso.Actual)

            '    Dim principal As Framework.Usuarios.DN.PrincipalDN = usln.RecuperarPrincipalxNick("ato")

            '    Dim siniestro As New FN.Seguros.Polizas.DN.SiniestroDN
            '    siniestro.Nombre = "siniestr " & Now

            '    Me.GuardarDatos(siniestro)

            '    Dim operln As New Framework.Procesos.ProcesosLN.OperacionesLN
            '    Dim coltr As Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN = operln.RecuperarTransicionesAutorizadasSobre(principal, New HEDN(siniestro))

            '    Dim miln As New Framework.Procesos.ProcesosLN.GestorOPRLN

            '    miln.EjecutarOperacion(siniestro, Nothing, principal, coltr(0))


            '    tr.Confirmar()

            'End Using




        End Using

    End Sub


    <TestMethod()> Public Sub ValidarRolDN()

        Dim cu As CasosUsoDN
        Dim rol As RolDN
        rol = New RolDN("mi rol", New ColCasosUsoDN)

        cu = New CasosUsoDN
        cu.Nombre = "mi caso de uso"

        If rol.Estado = Framework.DatosNegocio.EstadoDatosDN.Modificado Then
            Throw New ApplicationException("ddd")
        End If

        rol.ColCasosUsoDN.Add(cu)

        If rol.ColCasosUsoDN.Count = 1 AndAlso rol.Estado = Framework.DatosNegocio.EstadoDatosDN.Modificado Then
        Else

            Throw New ApplicationException("ddd")
        End If


    End Sub

    <TestMethod()> Public Sub CrearElEntorno()



        CrearElEntornop()



  


    End Sub

    <TestMethod()> Public Sub ProbarTransicionHistoria()


        ProbarTransicionHistoriap()
    
        ' para ver en el  analizador de consultas
        'SELECT *   FROM [trthOperacionRealizadaDNColOPRFinalizadasoEnCursoXthOperacionRealizadaDN]
        'SELECT * FROM    trtlOperacionRealizadaDNColOPRFinalizadasoEnCursoXtlOperacionRealizadaDN

        'SELECT *   FROM [trthOperacionRealizadaDNColSubTransicionesXtlTransicionDN]
        'SELECT * FROM    trtlOperacionRealizadaDNColSubTransicionesXtlTransicionDN

        'SELECT *   FROM [trthOperacionRealizadaDNColSubTRIniciadasXthTransicionRealizadaDN]
        'SELECT * FROM    trtlOperacionRealizadaDNColSubTRIniciadasXtlTransicionRealizadaDN

        'SELECT     * FROM         thTransicionRealizadaDN
        'SELECT     * FROM         tlTransicionRealizadaDN

        'SELECT     * FROM         thOperacionRealizadaDN
        'SELECT     * FROM         tlOperacionRealizadaDN

    End Sub


    Private Sub ProbarTransicionHistoriap()

        Using New CajonHiloLN(mRecurso)

            Dim vervo As New Procesos.ProcesosDN.VerboDN
            vervo.Nombre = "mi verbo oper"
            Dim opr As New Framework.Procesos.ProcesosDN.OperacionRealizadaDN
            opr.Operacion = New Procesos.ProcesosDN.OperacionDN
            opr.Operacion.VerboOperacion = vervo
            opr.Operacion.Nombre = "operacion1"
            Dim prin As PrincipalDN = New PrincipalDN
            Dim cu As New CasosUsoDN
            cu.ColOperaciones.Add(opr.Operacion)
            Dim rol As New RolDN
            rol.ColCasosUsoDN.Add(cu)
            prin.ColRoles.Add(rol)
            opr.SujetoOperacion = prin
            opr.SujetoOperacion.Nombre = "mi principal"
            opr.ObjetoIndirectoOperacion = New EntidadDePrueba
            opr.Nombre = "operacion1"
            Me.GuardarDatos(opr)

            opr.Nombre = "operacion1-1"
            Me.GuardarDatos(opr)

            opr.Nombre = "operacion1-2"
            Me.GuardarDatos(opr)




        End Using

    End Sub


    Public Function GuardarDatos(ByVal pEntidad As IEntidadDN) As IEntidadDN

        ObtenerRecurso()

        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(pEntidad)

        Return pEntidad

    End Function

    Private Sub CrearElEntornop()
        ObtenerRecurso()


        Using New CajonHiloLN(mRecurso)

            Dim gbd As New Framework.Usuarios.AD.UsuariosGBDAD(mRecurso)
            gbd.EliminarVistas()
            gbd.EliminarRelaciones()
            gbd.EliminarTablas()
            gbd.CrearTablas()
            gbd.CrearVistas()

            PublicarFachada()
            PublicarReferencias()

            PublicarGrafoPruebas()

            PublicarGrafoPruebasProcesos()

            CrearAdministradorTotal()

        End Using



    End Sub

    Private Function RecuperarVinculoMetodo(ByVal nombreMetodo As String, ByVal tipo As System.Type) As VinculoMetodoDN
        Dim vm As VinculoMetodoDN

        Using New CajonHiloLN(mRecurso)
            Dim tyrLN As New Framework.TiposYReflexion.LN.TiposYReflexionLN()
            vm = New VinculoMetodoDN(nombreMetodo, New VinculoClaseDN(tipo))

            Return tyrLN.CrearVinculoMetodo(vm.RecuperarMethodInfo())
        End Using

    End Function

    Private Function RecuperarVinculoClase(ByVal tipo As System.Type) As VinculoClaseDN
        Using New CajonHiloLN(mRecurso)
            Dim tyrLN As New Framework.TiposYReflexion.LN.TiposYReflexionLN()
            Return tyrLN.CrearVinculoClase(tipo)
        End Using

    End Function

    Public Sub PublicarGrafoPruebas()
        ' flujo de talones

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' 1º 
        ' dn o dns a las cuales se vincula el flujo
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        ' Dim vc1DN As Framework.TiposYReflexion.DN.VinculoClaseDN = RecuperarVinculoClase(GetType(Framework.Usuarios.DN.PrincipalDN))
        Dim ColVc As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
        ColVc.Add(RecuperarVinculoClase(GetType(Framework.Usuarios.DN.PrincipalDN)))
        'ColVc.Add(RecuperarVinculoClase(GetType(Framework.Usuarios.DN.PrincipalDNhet)))


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


        Dim colVM As New ColVinculoMetodoDN()


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


        ' Framework.FachadaLogica.GestorFachadaFL.PublicarMetodos("ProcesosFS", Me.mrecurso)

        'Dim opln As New Framework.Procesos.ProcesosLN.OperacionesLN
        'Dim ejc As Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN = opln.RecuperarEjecutorCliente("Servidor")

        If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion") Is Nothing Then
            Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolAplicacion", "RolServidorGSAMV")
        End If
        If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente") Is Nothing Then
            Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolCliente", "RolClienteGSAMV")
        End If

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

        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Alta Principal", RecuperarVinculoMetodo("AltaPrincipalClavePropuesta", GetType(Usuarios.LN.PrincipalLN)), ejClienteS))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Reactivar Principal", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Modificar Principal", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Baja Principal", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Gestion de Principal", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))

        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Alta Principal", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Reactivar Principal", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Modificar Principal", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Baja Principal", RecuperarVinculoMetodo("BajaPrincipal", GetType(Framework.Usuarios.IUWin.Controladores.UsrProcesosCtrl)), ejClienteC))
        ' ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion SumarCinco", RecuperarVinculoMetodo("SumarCinco", GetType(ProcesosLNC)), ejClienteC))

        ' finc pruebas










        Me.GuardarDatos(ejClienteC)
        Me.GuardarDatos(ejClienteS)

    End Sub











    Private Function CrearAdministradorTotal() As PrincipalDN



        Using tr As New Transaccion(True)

            Dim userln As New Usuarios.LN.UsuariosLN(Transaccion.Actual, Recurso.Actual)
            CrearAdministradorTotal = userln.CrearAdministradorTotal("ato", "ato")

            tr.Confirmar()

        End Using



    End Function


    Private Sub ObtenerRecurso()

        Dim connectionstring As String
        Dim htd As New Dictionary(Of String, Object)

        connectionstring = "server=localhost;database=sspruebasft;user=sa;pwd='sa'"
        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaCamposUsuariosTest

    End Sub



    Public Sub PublicarFachada()
        Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("UsuariosFS", mRecurso)
        ' Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("ProcesosFS", mRecurso)
    End Sub


    Public Sub PublicarReferencias()
        Dim ln As New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)

        Dim ensamblado As System.Reflection.Assembly = GetType(Framework.Usuarios.DN.RolDN).Assembly
        ln.RegistrarEnsamblado(ensamblado)

    End Sub




End Class







Public Class GestorMapPersistenciaCamposUsuariosTest
    Inherits GestorMapPersistenciaCamposLN






    Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing



        If (pTipo Is GetType(Framework.Procesos.ProcesosDN.TransicionRealizadaDN)) Then
            mapinst.TablaHistoria = "thTransicionRealizadaDN"
            Return mapinst
        End If



        If (pTipo Is GetType(Framework.Procesos.ProcesosDN.OperacionRealizadaDN)) Then
            Dim alentidades As ArrayList
            Dim mapSubInst As InfoDatosMapInstClaseDN
            ''''''''''''''''''''''

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(PrincipalDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mSujetoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst


            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(ContenedoraEntidadDePruebaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(EntidadDePrueba)))
            alentidades.Add(New VinculoClaseDN(GetType(Usuarios.DN.PrincipalDN)))
            'alentidades.Add(New VinculoClaseDN(GetType(Usuarios.DN.PrincipalDNhet)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mObjetoIndirectoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            '  campodatos.ColCampoAtributo.Add(CampoAtributoDN.SoloGuardarYNoReferido)
            campodatos.MapSubEntidad = mapSubInst

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mObjetoDirectoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            campodatos.MapSubEntidad = mapSubInst


            mapinst.TablaHistoria = "thOperacionRealizadaDN"

            Return mapinst
        End If


        '  Usuarios --------------------------------------------------------------------------------

        'If pTipo Is GetType(PrincipalDN) Then
        '    Me.MapearClase("mClavePropuesta", CampoAtributoDN.NoProcesar, campodatos, mapinst)
        '    Return mapinst
        'End If



        'If pTipo Is GetType(Framework.Usuarios.DN.TipoPermisoDN) Then
        '    Me.MapearClase("mNombre", CampoAtributoDN.UnicoEnFuenteDatosoNulo, campodatos, mapinst)
        '    Return mapinst
        'End If

        'If pTipo Is GetType(Framework.Usuarios.DN.PermisoDN) Then
        '    Dim alentidades As ArrayList
        '    Dim mapSubInst As New InfoDatosMapInstClaseDN


        '    mapSubInst = New InfoDatosMapInstClaseDN
        '    alentidades = New ArrayList
        '    alentidades.Add(New VinculoClaseDN(GetType(EntidadDePrueba)))
        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mDatoRef"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst


        '    Return mapinst
        'End If

        'If pTipo Is GetType(DatosIdentidadDN) Then
        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mHashClave"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

        '    Return mapinst
        'End If




        mapinst = Me.RecuperarMap_Framework_Usuarios(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        Return Nothing
    End Function

    Private Function RecuperarMap_Framework_Usuarios(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If pTipo Is GetType(Framework.Usuarios.DN.DatosIdentidadDN) Then

            Me.MapearCampoSimple(mapinst, "mHashClave", CampoAtributoDN.PersistenciaContenidaSerializada)

            Return mapinst
        End If

        If pTipo Is GetType(Framework.Usuarios.DN.UsuarioDN) Then
            Dim alentidades As ArrayList
            Dim mapSubInst As New InfoDatosMapInstClaseDN


            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Usuarios.DN.TipoPermisoDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mHuellaEntidadUserDN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst


            Return mapinst
        End If




        If pTipo Is GetType(Framework.Usuarios.DN.PrincipalDN) Then
            Me.MapearCampoSimple(mapinst, "mClavePropuesta", CampoAtributoDN.NoProcesar)
            Return mapinst
        End If

        If pTipo Is GetType(Framework.Usuarios.DN.PermisoDN) Then
            Me.MapearCampoSimple(mapinst, "mDatoRef", CampoAtributoDN.NoProcesar)
            Return mapinst
        End If

        If pTipo Is GetType(Framework.Usuarios.DN.TipoPermisoDN) Then
            Me.MapearCampoSimple(mapinst, "mNombre", CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            Return mapinst
        End If





        If pTipo Is GetType(Framework.TiposYReflexion.DN.VinculoMetodoDN) Then
            Me.MapearCampoSimple(mapinst, "mNombre", CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            Return mapinst
        End If

        If pTipo Is GetType(Framework.TiposYReflexion.DN.VinculoClaseDN) Then
            Me.MapearCampoSimple(mapinst, "mNombre", CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            Return mapinst
        End If



        'If (pTipo Is GetType(Framework.Usuarios.DN.AutorizacionRelacionalDN)) Then

        '    Dim alentidades As ArrayList
        '    Dim mapSubInst As InfoDatosMapInstClaseDN

        '    mapSubInst = New InfoDatosMapInstClaseDN
        '    alentidades = New ArrayList

        '    mapSubInst.NombreCompleto = GetType(Framework.Usuarios.DN.PrincipalDN).FullName
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.TipoEntidadOrigenDN)))
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.TipoEmpresaDN)))
        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mColEntidadesRelacionadas"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst

        '    Return mapinst


        'End If


    End Function
    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class






<Serializable()> Public Class EntidadDePrueba
    Inherits Framework.DatosNegocio.EntidadDN



    Protected mImporte As Double


    Public Property Importe() As Double
        Get
            Return Me.mImporte
        End Get
        Set(ByVal value As Double)
            Me.CambiarValorVal(Of Double)(value, mImporte)
        End Set
    End Property
End Class




<Serializable()> Public Class EntidadDePruebaDN
    Inherits Framework.DatosNegocio.EntidadDN



    Protected mImporte As Double


    Public Property Importe() As Double
        Get
            Return Me.mImporte
        End Get
        Set(ByVal value As Double)
            Me.CambiarValorVal(Of Double)(value, mImporte)
        End Set
    End Property
End Class









<Serializable()> _
Public Class ContenedoraEntidadDePruebaDN
    Inherits EntidadDN


#Region "Atributos"
    Protected mEntidadDePrueba As EntidadDePruebaDN
#End Region


#Region "Constructores"

#End Region

#Region "Propiedades"





    <RelacionPropCampoAtribute("mEntidadDePrueba")> _
    Public Property EntidadDePrueba() As EntidadDePruebaDN
        Get
            Return mEntidadDePrueba
        End Get
        Set(ByVal value As EntidadDePruebaDN)
            CambiarValorRef(Of EntidadDePruebaDN)(value, mEntidadDePrueba)
        End Set
    End Property




#End Region

#Region "Metodos"

#End Region



End Class




<Serializable()> _
Public Class ColContenedoraEntidadDePruebaDN
    Inherits ArrayListValidable(Of ContenedoraEntidadDePruebaDN)
End Class





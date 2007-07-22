Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting


Imports Framework.Usuarios.LN
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.DatosNegocio
Imports Framework
Imports Framework.TiposYReflexion.DN
Imports System.Collections
Imports Framework.LogicaNegocios.Transacciones

Imports Framework.Procesos.ProcesosLN


Imports Framework.Usuarios.DN

<TestClass()> Public Class UnitTest1
    Public mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = Nothing


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

    <TestMethod()> Public Sub CrearElEntorno()


        Me.CrearElRecurso("")
        CrearEntornoProcesos()

    End Sub




    <TestMethod()> Public Sub CrearEntornoPruebas()
        Me.CrearElRecurso("")
        CrearEntornoProcesos()
        CrearEntornoPruebasProcesos()

    End Sub



    <TestMethod()> Public Sub PrimeraOperacion()


        ' logar un principal


        Dim uas As New Framework.Usuarios.IUWin.AS.UsuariosAS
        Dim principal As Usuarios.DN.PrincipalDN = uas.IniciarSesion(New Framework.Usuarios.DN.DatosIdentidadDN("a", "a"))


        Dim entidadPrueba As New EntidadDePrueba
        entidadPrueba.Nombre = "primera ent"



        Dim lnc As New Framework.Procesos.ProcesosLNC.ProcesoLNC
        Dim colTranRealizada As Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN = lnc.RecuperarOperacionesAutorizadasSobreLNC(entidadPrueba)
        Dim tranrealiz As Framework.Procesos.ProcesosDN.TransicionRealizadaDN = colTranRealizada.Item(0)

        lnc.EjecutarOperacionLNC(principal, tranrealiz, entidadPrueba, Nothing)



    End Sub




    Private Sub CrearEntornoPruebasProcesos()

 

        Using New CajonHiloLN(mRecurso)

            CrearRolesYusuarios()
            Me.PublicarGrafoPruebas()





        End Using
    End Sub


    Private Function RecuperarVinculoClase(ByVal tipo As System.Type) As VinculoClaseDN
        Using New CajonHiloLN(mRecurso)
            Dim tyrLN As New Framework.TiposYReflexion.LN.TiposYReflexionLN()
            Return tyrLN.CrearVinculoClase(tipo)
        End Using

    End Function
    Private Function RecuperarVinculoMetodo(ByVal nombreMetodo As String, ByVal tipo As System.Type) As VinculoMetodoDN
        Dim vm As VinculoMetodoDN

        Using New CajonHiloLN(mRecurso)
            Dim tyrLN As New Framework.TiposYReflexion.LN.TiposYReflexionLN()
            vm = New VinculoMetodoDN(nombreMetodo, New VinculoClaseDN(tipo))

            Return tyrLN.CrearVinculoMetodo(vm.RecuperarMethodInfo())
        End Using

    End Function

    Public Function GuardarDatos(ByVal pEntidad As IEntidadDN) As IEntidadDN

        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(pEntidad)

        Return pEntidad

    End Function

    Public Sub PublicarGrafoPruebas()
        ' flujo de talones

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' 1º 
        ' dn o dns a las cuales se vincula el flujo
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim vc1DN As Framework.TiposYReflexion.DN.VinculoClaseDN = RecuperarVinculoClase(GetType(EntidadDePrueba))
        Dim ColVc As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
        ColVc.Add(vc1DN)



        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' 2º 
        '  creacion de las operaciones
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        Dim colop As New Framework.Procesos.ProcesosDN.ColOperacionDN

        ' operacion que engloba todo el flujo
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("pruba de Flujo", ColVc, "element_into.ico", True)))

        '' operacion de pueba

        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion Compuesta", ColVc, "", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion SumarUno", ColVc, "", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion RestarUno", ColVc, "", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion SumarCinco", ColVc, "", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion RestarCinco", ColVc, "", True)))

        '' FIN operacion de pueba



        ''''''''''''''''''''''''''''''''''''''''''''
        ' 3º
        ' creacion de las Transiciones
        ''''''''''''''''''''''''''''''''''''''''''''


        Dim colVM As New ColVinculoMetodoDN()


        '' prueba subordiandas ''''''''''''''''''''''''''''''''''''''

        ' transicion de inicio
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Negocio", "Operacion Compuesta", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))


        ' transiciones corrientes
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion Compuesta", "Operacion SumarUno", Framework.Procesos.ProcesosDN.TipoTransicionDN.Subordianda, False, Nothing, True))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion Compuesta", "Operacion RestarUno", Framework.Procesos.ProcesosDN.TipoTransicionDN.Subordianda, False, Nothing, True))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion RestarUno", "Operacion RestarCinco", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion SumarUno", "Operacion SumarCinco", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        ' transiciones de fin
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion RestarCinco", "Operacion Compuesta", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion SumarCinco", "Operacion Compuesta", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))

        '  GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion Compuesta", "Guardar Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))



        '''''''''''''''''''''''''''''''
        ' publicar los controladores ''
        '''''''''''''''''''''''''''''''


        ' Framework.FachadaLogica.GestorFachadaFL.PublicarMetodos("ProcesosFS", Me.mrecurso)

        'Dim opln As New Framework.Procesos.ProcesosLN.OperacionesLN
        'Dim ejc As Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN = opln.RecuperarEjecutorCliente("Servidor")


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

        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion Compuesta", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion SumarUno", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion RestarUno", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion RestarCinco", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion SumarCinco", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))

        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion Compuesta", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion SumarUno", RecuperarVinculoMetodo("SumarUno", GetType(ProcesosLNC)), ejClienteC))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion RestarUno", RecuperarVinculoMetodo("RestarUno", GetType(ProcesosLNC)), ejClienteC))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion RestarCinco", RecuperarVinculoMetodo("RestarCinco", GetType(ProcesosLNC)), ejClienteC))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion SumarCinco", RecuperarVinculoMetodo("SumarCinco", GetType(ProcesosLNC)), ejClienteC))

        ' finc pruebas










        Me.GuardarDatos(ejClienteC)
        Me.GuardarDatos(ejClienteS)

    End Sub


    Public Sub PublicarFachada()
        Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("UsuariosFS", mRecurso)
        Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("ProcesosFS", mRecurso)
    End Sub
    Private Sub AsignarOperacionesCasoUsoTotal()
        Dim casosUsoLN As New Framework.Usuarios.LN.CasosUsoLN(Nothing, mRecurso)
        Dim colCasosUso As New ColCasosUsoDN()
        Dim casoUsoTotal As CasosUsoDN

        colCasosUso.AddRange(casosUsoLN.RecuperarListaCasosUso())
        casoUsoTotal = colCasosUso.RecuperarPrimeroXNombre("Todos los permisos")

        Using New CajonHiloLN(mrecurso)
            Dim cb As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim col As IList = cb.RecuperarLista(GetType(Framework.Procesos.ProcesosDN.OperacionDN))

            For Each op As Framework.Procesos.ProcesosDN.OperacionDN In col
                casoUsoTotal.ColOperaciones.Add(op)
            Next
            Me.GuardarDatos(casoUsoTotal)
        End Using

    End Sub

    Private Sub CrearRolesYusuarios()

        ''''''''''''''''''''''''''''''''''''''
        'Creo los casos de uso de  la aplicacion
        ''''''''''''''''''''''''''''''''''''''

        'recupero todos los metodos de sistema
        Dim rolat As RolDN
 
        Dim usurios As Framework.Usuarios.LN.RolLN
        usurios = New Framework.Usuarios.LN.RolLN(Nothing, mRecurso)
        rolat = usurios.GeneraRolAutorizacionTotal("Administrador Total")
        AsignarOperacionesCasoUsoTotal()
        Me.GuardarDatos(rolat)





    End Sub


    Private Function NicYClaveRol(ByVal pNombreRol As String) As String
        Try
            Dim palabras() As String

            palabras = pNombreRol.Split(" ")

            Return (palabras(0).Substring(0, 1) & palabras(1).Substring(0, 2)).ToLower

        Catch ex As Exception
            Throw New ApplicationException(" el nombre del rol debe tener al menos dos palabras")
        End Try

    End Function


    Public Sub CrearUnUsuarioAdminTotal(ByVal pNombreRolAdminTotal As String, ByVal pId As String, ByVal pClave As String)

        Dim usuario As UsuarioDN
        Dim uln As New UsuariosLN(Nothing, mrecurso)
        Dim Principal As PrincipalDN
        Dim di As DatosIdentidadDN
        Dim colRol, colTodosRol As ColRolDN
        Dim rolesln As RolLN


        ' admin negocio
        rolesln = New RolLN(Nothing, mrecurso)
        colTodosRol = rolesln.RecuperarColRoles




        Dim rol As RolDN
        For Each rol In colTodosRol

            If rol.Nombre = pNombreRolAdminTotal Then
                Dim nombreclave As String = NicYClaveRol(rol.Nombre)
                colRol = New ColRolDN
                colRol.Add(rol)
                usuario = New UsuarioDN(nombreclave, True)
                Principal = New PrincipalDN(nombreclave, usuario, colRol)
                di = New DatosIdentidadDN(pId, pClave)
                Me.GuardarDatos(Principal)
                Me.GuardarDatos(di)
            End If


        Next




    End Sub

    Public Sub CrearUnUsuarioParaCadaRol()

        Dim usuario As UsuarioDN
        Dim uln As New UsuariosLN(Nothing, mRecurso)
        Dim Principal As PrincipalDN
        Dim di As DatosIdentidadDN
        Dim colRol, colTodosRol As ColRolDN
        Dim rolesln As RolLN


        ' admin negocio
        rolesln = New RolLN(Nothing, mRecurso)
        colTodosRol = rolesln.RecuperarColRoles




        Dim rol As RolDN
        For Each rol In colTodosRol

            Dim nombreclave As String = NicYClaveRol(rol.Nombre)
            colRol = New ColRolDN
            colRol.Add(rol)
            ' usuario = New UsuarioDN(nombreclave, True, New AmvDocumentosDN.HuellaOperadorDN(New AmvDocumentosDN.OperadorDN(nombreclave, coltipoEN)))
            ' usuario = New UsuarioDN(nombreclave, True, Nothing)
            usuario = New UsuarioDN(nombreclave, True)
            Principal = New PrincipalDN(nombreclave, usuario, colRol)
            di = New DatosIdentidadDN(nombreclave, nombreclave)
            Me.GuardarDatos(Principal)
            Me.GuardarDatos(di)

        Next




    End Sub


    Private Sub CrearElRecurso(ByVal connectionstring As String)
        Dim htd As New System.Collections.Generic.Dictionary(Of String, Object)

        If connectionstring Is Nothing OrElse connectionstring = "" Then
            connectionstring = "server=localhost;database=ssPruebasFT;user=sa;pwd='sa'"
        End If

        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a AMV", "sqls", htd)


        'Asignamos el mapeado de  gestor de instanciación
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaCamposProcesosTest


    End Sub

    Private Sub CrearEntornoProcesos()






        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Dim gbd As New Framework.Procesos.ProcesosAD.OperacionesGBDAD()
        gbd.mRecurso = Me.mRecurso


        gbd.EliminarVistas()
        gbd.EliminarRelaciones()
        gbd.EliminarTablas()


        gbd.CrearTablas()
        gbd.CrearVistas()

        PublicarFachada()

        ' crear tablas y entidades de ejmeplo



        ' crear las tablas

        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.GenerarTablas2(GetType(A), Nothing)

        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.GenerarTablas2(GetType(b), Nothing)




        '' crear las entidades


        'Dim a As A
        'a = New A
        'a.Nombre = "a1"
        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.Guardar(a)
        'a = New A
        'a.Nombre = "a2"
        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.Guardar(a)

        'Dim b As b
        'b = New b
        'b.Nombre = "b2"
        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.Guardar(b)


        '' crear las notas



        ''Dim h As Framework.DatosNegocio.HEDN

        'Dim nota As Framework.Notas.NotasDN.NotaDN

        'nota = New Framework.Notas.NotasDN.NotaDN
        'nota.Nombre = "nota de: " & a.Nombre
        'nota.comentario = "El perro de san roque no tiene rabo porque ...."
        'nota.ColIHEntidad.Add(New Framework.DatosNegocio.HEDN(a, Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir))
        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.Guardar(nota)


        'nota = New Framework.Notas.NotasDN.NotaDN
        'nota.Nombre = "nota de: " & b.Nombre
        'nota.comentario = "El perro de san roque no tiene rabo porquedddd ...."
        'nota.ColIHEntidad.Add(New Framework.DatosNegocio.HEDN(b, Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir))
        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        'gi.Guardar(nota)


        'Dim dts As System.Data.DataSet
        'Dim ln As Notas.NotasLN.NotaLN

        'ln = New Notas.NotasLN.NotaLN

        'dts = ln.RecuperarDTSNotas(b)
        'If dts.Tables(0).Rows.Count > 0 Then

        '    System.Diagnostics.Debug.Write(dts.Tables(0).Rows(0)(0))

        'Else
        '    Throw New ApplicationException
        'End If
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




Public Class GestorMapPersistenciaCamposProcesosTest
    Inherits GestorMapPersistenciaCamposLN

    'TODO: ALEX por implementar: recuperar el mapeado de instanciacion de la fuente de datos para el tipo a procesar
    Public Overrides Function RecuperarMapPersistenciaCampos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As InfoDatosMapInstClaseDN = Nothing
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        mapinst = RecuperarMapPersistenciaCamposPrivado(pTipo)

        ' ojo esta modificación se debe aplicar siempre si el tipo hereda de una huella es decir en el metodo que lo llamo
        If (TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo)) Then
            If mapinst Is Nothing Then
                mapinst = New InfoDatosMapInstClaseDN
            End If
            Me.MapearCampoSimple(mapinst, "mEntidadReferidaHuella", CampoAtributoDN.SoloGuardarYNoReferido)
        End If

        Return mapinst
    End Function



    Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing




        ' Usuarios --------------------------------------------------------------------------------
        If pTipo Is GetType(DatosIdentidadDN) Then

            Me.MapearCampoSimple(mapinst, "mHashClave", CampoAtributoDN.PersistenciaContenidaSerializada)

            Return mapinst
        End If

        If pTipo Is GetType(UsuarioDN) Then
            Dim mapinstSub As New InfoDatosMapInstClaseDN
            Dim alentidades As New ArrayList

            'Me.VincularConClase("mHuellaEntidadUserDN", New ElementosDeEnsamblado("AmvDocumentosDN", "AmvDocumentosDN.HuellaOperadorDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
            'Me.VincularConClase("mEntidadUser", New ElementosDeEnsamblado("EmpresasDN", GetType(FN.Empresas.DN.HuellaCacheEmpleadoYPuestosRDN).FullName), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mEntidadUser"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)



            Return mapinst
        End If

        If pTipo Is GetType(PrincipalDN) Then
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
        'end  Usuarios --------------------------------------------------------------------------------

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

            alentidades.Add(New VinculoClaseDN(GetType(EntidadDePrueba)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mObjetoIndirectoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mObjetoDirectoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            campodatos.MapSubEntidad = mapSubInst



            Return mapinst
        End If

        Return Nothing
    End Function


    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class



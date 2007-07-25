Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.DatosNegocio
Imports Framework
Imports Framework.TiposYReflexion.DN
Imports System.Collections

Imports Framework.LogicaNegocios.Transacciones
<TestClass()> Public Class MNavegacionDatosTest
    Dim mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
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

    Private Sub ObtenerRecurso()

        Dim connectionstring As String
        Dim htd As New Dictionary(Of String, Object)

        connectionstring = "server=localhost;database=sspruebasft;user=sa;pwd='sa'"
        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        'Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New Framework.AccesoDatos.MotorAD.LN.GestorMapPersistenciaCamposAMVDocsEntrantesLN
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaCamposMNavDTest

    End Sub


    <TestMethod()> Public Sub TCrearEntornoMNavD()
        ObtenerRecurso()
        Dim gbd As New MNavegacionDatosAD.MNDGBD(mRecurso)
        gbd.EliminarRelaciones()

        gbd.EliminarVistas()
        gbd.EliminarTablas()

        gbd.CrearTablas()
        gbd.CrearVistas()

        ' crear el entorno de puebas
        CrearEntornoPruebas()



    End Sub



    <TestMethod()> Public Sub RecuperarRelacionesTipo()


        RegistrarTipo()

        Dim ln As New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        Dim col As MNavegacionDatosDN.ColRelacionEntidadesNavDN
        col = ln.RecuperarRelaciones(GetType(Persona))

        'System.Diagnostics.Debug.WriteLine(col.Count)
        If col.Count < 1 Then
            Throw New ApplicationException
        End If


        ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        col = ln.RecuperarRelaciones(GetType(Concurso))

        'System.Diagnostics.Debug.WriteLine(col.Count)
        If col.Count < 1 Then
            Throw New ApplicationException
        End If


        'Dim rel As New MNavegacionDatosDN.RelEntNavVincDN(col(0),
        'Dim colihe As Framework.DatosNegocio.ColIHuellaEntidadDN

        'ln = New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        'colihe = ln.RecuperarColHuellas(rel)



    End Sub

    <TestMethod()> Public Sub RegistrarTipo()

        TCrearEntornoMNavD()



        Using New CajonHiloLN(mRecurso)




            Using tr As New Transaccion


                Dim ln As New MNavegacionDatosLN.MNavDatosLN(Transaccion.Actual, Recurso.Actual)
                ln.RegistrarEnsamblado(Me.GetType.Assembly)

                tr.Confirmar()

            End Using





        End Using





    End Sub




    Private Sub CrearEntornoPruebas()


        ' crear las tablas
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(telefono), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(hijo), Nothing)



        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Equipo), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(ParticipanteTA), Nothing)



        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Concurso), Nothing)



        ' registrar los tipos

        Dim ln As New MNavegacionDatosLN.MNavDatosLN(Nothing, Me.mRecurso)
        ln.RegistrarEnsamblado(Me.GetType.Assembly)




        ' crear un par de intancias

        Dim mitlf As telefono
        Dim micabeza As cabeza
        Dim mipersoana As Persona
        Dim hijo As Hijo

        micabeza = New cabeza
        micabeza.Nombre = "cabeza1"

        mipersoana = New Persona
        mipersoana.Nombre = "persona1"
        mipersoana.cabeza = micabeza

        mitlf = New telefono
        mitlf.Nombre = "tlf1"
        mitlf.Dueño = mipersoana
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(mitlf)


        mitlf = New telefono
        mitlf.Nombre = "tlf2"
        mitlf.Dueño = mipersoana


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(mitlf)



        hijo = New Hijo
        hijo.Nombre = "pepe"
        hijo.Padre = mipersoana
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(hijo)

        hijo = New Hijo
        hijo.Nombre = "lucas"
        hijo.Padre = mipersoana
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(hijo)


        ' la segunda persona
        micabeza = New cabeza
        micabeza.Nombre = "cabeza de ramona"

        mipersoana = New Persona
        mipersoana.Nombre = "persona2 ramona"
        mipersoana.cabeza = micabeza

        mitlf = New telefono
        mitlf.Nombre = "tlf de ramona"
        mitlf.Dueño = mipersoana
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(mitlf)




        Dim miEquipo As Equipo
        miEquipo = New Equipo
        miEquipo.Nombre = "equipo1"
        miEquipo.colPersonas = New colPersona
        miEquipo.colPersonas.Add(mipersoana)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(miEquipo)




        Dim parti As ParticipanteTA
        parti = New ParticipanteTA
        parti.persona = mipersoana
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(parti)


        Dim concur As Concurso
        concur = New Concurso
        concur.ParticipantePrincipal = parti

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(concur)



    End Sub


End Class



'Public Class VincReversaaPrincipalDN
'    Inherits Framework.DatosNegocio.EntidadDN
'    Protected mprincipal As PrincipalDN
'    Public Property Principal() As PrincipalDN
'        Get
'            Return mprincipal
'        End Get
'        Set(ByVal value As PrincipalDN)

'        End Set
'    End Property
'End Class








Public Class GestorMapPersistenciaCamposMNavDTest
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
            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mEntidadReferidaHuella"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.SoloGuardarYNoReferido)
        End If

        Return mapinst
    End Function



    Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing







        If (pTipo.FullName = "MotorADTest.ClaseHA") Then
            Dim alentidades As ArrayList
            alentidades = New ArrayList
            alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.ClaseHA2"))
            alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.ClaseHA"))

            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor) = alentidades

            Return mapinst
        End If


        '----Motor navegacion datos Test------------------------------------------------------------------------



        ' Para la prueba de mapeado en la interface
        If (pTipo Is GetType(IParticipante)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(ParticipanteTA).Namespace, GetType(ParticipanteTA).FullName))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst
        End If


        ' para la prueba de mapeado en el campo

        'If (pTipo.FullName = "AuditoriasDN.ArbolDeConceptosAuditablesDN") Then

        '    Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
        '    Dim alentidades As ArrayList = Nothing


        '    Dim lista As List(Of ElementosDeEnsamblado)
        '    lista = New List(Of ElementosDeEnsamblado)
        '    lista.Add(New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.ArbolDeConceptosAuditablesDN"))
        '    lista.Add(New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.CategoriaDN"))
        '    VincularConClase("mPadre", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)



        '    Return mapinst

        'End If


        '----------------------------------------------------------------------------


        'ZONA: AuditoriasDN ________________________________________________________________



        ''If (pTipo.FullName = "AuditoriasDN.InformeVisitaDocDN") Then

        ''    campodatos = New InfoDatosMapInstCampoDN
        ''    campodatos.InfoDatosMapInstClase = mapinst
        ''    campodatos.NombreCampo = "mFileInfo"
        ''    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

        ''    Return mapinst

        ''End If


        ''If (pTipo.FullName = "AuditoriasDN.ObservacionDocDN") Then

        ''    campodatos = New InfoDatosMapInstCampoDN
        ''    campodatos.InfoDatosMapInstClase = mapinst
        ''    campodatos.NombreCampo = "mFileInfo"
        ''    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

        ''    Return mapinst

        ''End If


        'If (pTipo.FullName = "AuditoriasDN.CampañaDN") Then
        '    Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
        '    Dim alentidades As ArrayList = Nothing
        '    Dim lista As List(Of ElementosDeEnsamblado)

        '    MapearClase("mPeriodoValidezDeLaCampaña", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)

        '    lista = New List(Of ElementosDeEnsamblado)
        '    lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.HuellaCahceAdaptadorEvalSedeEmpresaDN"))
        '    '                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"))
        '    VincularConClase("mColHuellaDestinatarioDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)

        '    Return mapinst

        'End If



        ''Mapeado de TareaDN con padre otra TareaDN, hijos una colección de TareaDN
        ''con hijos AdaptadorEvalEmpleadoDN
        'If (pTipo.FullName = "EmpresaToyotaDN.TareaGVDDN") Then
        '    Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
        '    Dim alentidades As ArrayList = Nothing
        '    Dim lista As List(Of ElementosDeEnsamblado)

        '    '   MapearClase("mDatosTemporalesDN", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)

        '    MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    MapearClase("mConLoQueSeHaceODDN", CampoAtributoDN.NoProcesar, Nothing, mapinst)
        '    MapearClase("mBeneficiarioDN", CampoAtributoDN.NoProcesar, Nothing, mapinst)
        '    MapearClase("mAccionVerboDN", CampoAtributoDN.PersistenciaContenidaSerializada, Nothing, mapinst)
        '    Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    MapearClase("mValidadorCambioEstado", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

        '    'La TareaGVD puede tener como padre otra TareaGVD o una TareaResumenSujeto
        '    'VincularConClase("mPadre", New ElementosDeEnsamblado("EmpresaToyotaDN", "EmpresaToyotaDN.TareaGVDDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    lista = New List(Of ElementosDeEnsamblado)
        '    lista.Add(New ElementosDeEnsamblado("EmpresaToyotaDN", "EmpresaToyotaDN.TareaGVDDN"))
        '    lista.Add(New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.TareaResumenSujetoDN"))
        '    VincularConClase("mPadre", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)

        '    VincularConClase("mHijos", New ElementosDeEnsamblado("EmpresaToyotaDN", "EmpresaToyotaDN.TareaGVDDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    VincularConClase("mCausaTareaDN", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.CausaObservacionDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    VincularConClase("mPlanificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
        '    '  VincularConClase("mSujetoDN", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
        '    VincularConClase("mSupervisorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
        '    VincularConClase("mVerificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)

        '    lista = New List(Of ElementosDeEnsamblado)
        '    lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"))
        '    lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"))
        '    lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN"))
        '    VincularConClase("mSujetoDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)


        '    lista = New List(Of ElementosDeEnsamblado)
        '    lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"))
        '    lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"))
        '    lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN"))
        '    VincularConClase("mSobreQueSeHaceOIDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
        '    '  VincularConClase("mSujetoDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)

        '    Return mapinst

        'End If

        'If (pTipo.FullName = "AuditoriasDN.TareaResumenSujetoDN") Then
        '    Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
        '    Dim alentidades As ArrayList = Nothing
        '    Dim lista As List(Of ElementosDeEnsamblado)

        '    '    MapearClase("mDatosTemporalesDN", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    MapearClase("mConLoQueSeHaceODDN", CampoAtributoDN.NoProcesar, Nothing, mapinst)
        '    MapearClase("mBeneficiarioDN", CampoAtributoDN.NoProcesar, Nothing, mapinst)
        '    MapearClase("mAccionVerboDN", CampoAtributoDN.PersistenciaContenidaSerializada, Nothing, mapinst)
        '    Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    MapearClase("mCausaTareaDN", CampoAtributoDN.NoProcesar, Nothing, mapinst)
        '    MapearClase("mValidadorCambioEstado", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

        '    VincularConClase("mPadre", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.TareaResumenSujetoDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    'VincularConClase("mHijos", New ElementosDeEnsamblado("EmpresaToyotaDN", "EmpresaToyotaDN.TareaGVDDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    'VincularConClase("mCausaTareaDN", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.CausaObservacionDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    VincularConClase("mPlanificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
        '    '  VincularConClase("mSujetoDN", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
        '    VincularConClase("mSupervisorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
        '    VincularConClase("mVerificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)



        '    lista = New List(Of ElementosDeEnsamblado)
        '    lista.Add(New ElementosDeEnsamblado("EmpresaToyotaDN", "EmpresaToyotaDN.TareaGVDDN"))
        '    lista.Add(New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.TareaResumenSujetoDN"))
        '    VincularConClase("mHijos", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)


        '    lista = New List(Of ElementosDeEnsamblado)
        '    lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"))
        '    lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"))
        '    lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN"))
        '    VincularConClase("mSobreQueSeHaceOIDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
        '    VincularConClase("mSujetoDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)


        '    Return mapinst

        'End If

        ''Mapeado de TareaDN con padre otra TareaDN, hijos una colección de TareaDN
        ''con hijos AdaptadorEvalEmpleadoDN
        'If (pTipo.FullName = "AuditoriasDN.TareaDN") Then
        '    Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
        '    Dim alentidades As ArrayList = Nothing
        '    Dim lista As List(Of ElementosDeEnsamblado)
        '    '    MapearClase("mDatosTemporalesDN", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)

        '    MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    MapearClase("mConLoQueSeHaceODDN", CampoAtributoDN.NoProcesar, Nothing, mapinst)
        '    MapearClase("mBeneficiarioDN", CampoAtributoDN.NoProcesar, Nothing, mapinst)
        '    MapearClase("mAccionVerboDN", CampoAtributoDN.PersistenciaContenidaSerializada, Nothing, mapinst)
        '    Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    MapearClase("mValidadorCambioEstado", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

        '    VincularConClase("mPadre", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.TareaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    VincularConClase("mHijos", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.TareaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    VincularConClase("mCausaTareaDN", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.CausaObservacionDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    VincularConClase("mPlanificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
        '    VincularConClase("mSujetoDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
        '    VincularConClase("mSupervisorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
        '    VincularConClase("mVerificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)

        '    lista = New List(Of ElementosDeEnsamblado)
        '    lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"))
        '    lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"))
        '    lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN"))
        '    VincularConClase("mSobreQueSeHaceOIDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)



        '    alentidades = New ArrayList
        '    alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.TareaGVDDN"))
        '    alentidades.Add(New VinculoClaseDN("AuditoriasDN", "AuditoriasDN.TareaDN"))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor) = alentidades


        '    Return mapinst

        'End If




        ''If (pTipo.FullName = "TareasDN.DatosTemporalesDN") Then
        ''    MapearClase("mPeriodoPlanificado", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        ''    MapearClase("mPeriodoReal", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        ''    Return mapinst
        ''End If

        'If (pTipo.FullName = "LocalizacionesDN.Temporales.IntervaloFechasSubordinadoDN") Then
        '    MapearClase("mIntervaloFechas", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    Return mapinst
        'End If







        ''Mapeado del IEvaluableDN, donde la interfaz se mapea para todas las clases, y habrá que 
        ''decir que clases implementan esta interfaz
        'If (pTipo.FullName = "AuditoriasDN.IEvaluableDN") Then
        '    Dim alentidades As New ArrayList
        '    alentidades.Add(New VinculoClaseDN("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"))
        '    alentidades.Add(New VinculoClaseDN("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN"))
        '    alentidades.Add(New VinculoClaseDN("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
        '    Return mapinst
        'End If

        ' ''esto hay que quitarlo luego, pero es para test ObservacionesDNTest
        'If (pTipo.FullName = "AuditoriasDN.ObservacionDN") Then

        '    Me.MapearClase("mNota", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)

        '    Return mapinst
        'End If

        ''TODO: Revisar el mapeado para la visita -> GrupoVisitante_____________________
        'If pTipo.FullName = "AuditoriasDN.VisitaDN" Then
        '    Dim mapinstSub As New InfoDatosMapInstClaseDN
        '    Dim alentidades As New ArrayList

        '    'campodatos = New InfoDatosMapInstCampoDN
        '    'campodatos.InfoDatosMapInstClase = mapinst
        '    Me.VincularConClase("mPlanificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    Me.VincularConClase("mSujetoDN", New ElementosDeEnsamblado("EmpresaToyotaDN", "EmpresaToyotaDN.GrupoVisitanteDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    Me.VincularConClase("mSupervisorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    VincularConClase("mVerificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    'Me.VincularConClase("mCausaTareaDN", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.ObservacionDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    Me.MapearClase("mCausaTareaDN", CampoAtributoDN.NoProcesar, campodatos, mapinst)
        '    Me.MapearClase("mBeneficiarioDN", CampoAtributoDN.NoProcesar, campodatos, mapinst)
        '    Me.MapearClase("mAccionVerboDN", CampoAtributoDN.NoProcesar, campodatos, mapinst)
        '    Me.MapearClase("mConLoQueSeHaceODDN", CampoAtributoDN.NoProcesar, campodatos, mapinst)
        '    Me.MapearClase("mHijos", CampoAtributoDN.NoProcesar, campodatos, mapinst)
        '    Me.MapearClase("mPadre", CampoAtributoDN.NoProcesar, campodatos, mapinst)
        '    Me.MapearClase("mValidadorp", CampoAtributoDN.NoProcesar, campodatos, mapinst)
        '    Me.MapearClase("mValidadorh", CampoAtributoDN.NoProcesar, campodatos, mapinst)
        '    MapearClase("mValidadorCambioEstado", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    '  Me.VincularConClase("mQuienHaceVisita", New ElementosDeEnsamblado("EmpresaToyotaDN", "EmpresaToyotaDN.GrupoVisitanteDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    '  Me.VincularConClase("mEntidadVisitada", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    Dim lista As List(Of ElementosDeEnsamblado)

        '    lista = New List(Of ElementosDeEnsamblado)
        '    lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"))
        '    VincularConClase("mSobreQueSeHaceOIDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)


        '    Return mapinst

        'End If

        ''Mapeado para el árbol de conceptos auditables
        'If (pTipo.FullName = "AuditoriasDN.ArbolDeConceptosAuditablesDN") Then
        '    Dim mapinstSub As New InfoDatosMapInstClaseDN
        '    Dim alentidades As New ArrayList

        '    'Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    'Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    Me.VincularConClase("mValidadorh", New ElementosDeEnsamblado(Nothing, Nothing), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)
        '    Me.VincularConClase("mValidadorp", New ElementosDeEnsamblado(Nothing, Nothing), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)

        '    Me.VincularConClase("mHijos", New ElementosDeEnsamblado(Nothing, Nothing), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)

        '    ' Me.VincularConClase("mHijos", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    Me.VincularConClase("mPadre", New ElementosDeEnsamblado(Nothing, Nothing), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)

        '    Return mapinst
        'End If

        ''Mapeado de la clase CategoriaDN, donde indicamos como se mapean los distintos campos de la clase
        'If (pTipo.FullName = "AuditoriasDN.CategoriaDN") Then
        '    Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
        '    Dim alentidades As ArrayList = Nothing

        '    Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    Me.VincularConClase("mHijos", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.CategoriaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    Me.VincularConClase("mPadre", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.CategoriaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

        '    'Dim lista As List(Of ElementosDeEnsamblado)
        '    'lista = New List(Of ElementosDeEnsamblado)
        '    'lista.Add(New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.ArbolDeConceptosAuditablesDN"))
        '    'lista.Add(New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.CategoriaDN"))
        '    'VincularConClase("mPadre", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)


        '    Return mapinst
        'End If

        ''FINZONA: AuditoriasDN ________________________________________________________________


        ''ZONA: EmpresaToyotaDN ________________________________________________________________

        ''******************************************************************************************
        ''NOTA IMPORTANTE PARA EL ORDEN DE CREACIÓN DE TABLAS
        ''Cuando se tengan que crear estas tablas, se crean primero hacia arriba, esto es, se crean
        ''mapeando los hijos como no procesados, y solo se procesan los padres, y luego, se mapean con todo
        ''y se vuelven a crear las tablas
        ''******************************************************************************************

        ''Mapeado para el concesionario con hijos AdaptadorEvalPuntoVentaDN
        'If (pTipo.FullName = "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN") Then
        '    Dim mapinstSub As New InfoDatosMapInstClaseDN
        '    Dim alentidades As New ArrayList

        '    Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    Me.MapearClase("mCriticidadDeLaEmpresa", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    'Me.VincularConClase("mHijos", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)

        '    Me.VincularConClase("mHijos", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    Me.VincularConClase("mPadre", New ElementosDeEnsamblado(Nothing, Nothing), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)

        '    Return mapinst
        'End If

        ''Mapeado de empleados con padre AdaptadorEvalPuntoVentaDN
        'If (pTipo.FullName = "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN") Then
        '    Dim mapinstSub As New InfoDatosMapInstClaseDN
        '    Dim alentidades As New ArrayList

        '    Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    Me.VincularConClase("mHijos", New ElementosDeEnsamblado(Nothing, Nothing), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)
        '    Me.VincularConClase("mPadre", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

        '    Return mapinst
        'End If

        ''Mapeado de puntos de venta con padre AdaptadorEvalConcesionarioDN
        ''con hijos AdaptadorEvalEmpleadoDN
        'If (pTipo.FullName = "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN") Then
        '    Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
        '    Dim alentidades As ArrayList = Nothing

        '    Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    ' Me.VincularConClase("mHijos", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN"), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)

        '    Me.VincularConClase("mHijos", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    Me.VincularConClase("mPadre", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    Return mapinst
        'End If

        ''Mapeado del DelegadoTerritorialDN, donde la clase mapea sus interfaces, y solo es para ella.
        ''En cualquier otro clase que tenga la misma base, habrá que mapearla para esa.
        'If (pTipo.FullName = "EmpresaToyotaDN.JefeDelegadoTerritorialDN") Then
        '    Me.MapearClase("mPeriodo", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    Return mapinst
        'End If
        'If (pTipo.FullName = "EmpresaToyotaDN.DelegadoTerritorialDN") Then
        '    Me.MapearClase("mPeriodo", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    Return mapinst
        'End If
        'If (pTipo.FullName = "EmpresaToyotaDN.SecretariaVentasDN") Then
        '    Me.MapearClase("mPeriodo", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    Return mapinst
        'End If

        ''FINZONA: EmpresaToyotaDN ________________________________________________________________

        ''ZONA: UsuarioDN ________________________________________________________________

        ''Mapeado de UsuarioDN, donde la clase mapea sus interfaces, y solo es para ella.
        ''En cualquier otro clase que tenga la misma base, habrá que mapearla para esa.
        'If (pTipo.FullName = "UsuariosDN.UsuarioDN") Then
        '    Dim mapinstSub As New InfoDatosMapInstClaseDN
        '    Dim alentidades As New ArrayList

        '    Me.VincularConClase("mHuellaEntidadUserDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.HuellaCacheEmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

        '    Return mapinst
        'End If

        'If (pTipo.FullName = "UsuariosDN.DatosIdentidadDN") Then

        '    Me.MapearClase("mHashClave", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

        '    Return mapinst
        'End If


        ''ZONA: EmpresasDN ________________________________________________________________

        ''Mapeado del IResponsableDN, donde la interfaz se mapea para todas las clases, y habrá que 
        ''decir que clases implementan esta interfaz
        'If (pTipo.FullName = "EmpresasDN.IResponsableDN") Then
        '    Dim alentidades As New ArrayList
        '    alentidades.Add(New VinculoClaseDN("EmpresasDN", "EmpresasDN.ResponsableAgrupacionDeEmpresasDN"))
        '    alentidades.Add(New VinculoClaseDN("EmpresasDN", "EmpresasDN.ResponsableDePersonalDN"))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
        '    Return mapinst
        'End If

        'If (pTipo.FullName = "EmpresasDN.IEmpresaDN") Then
        '    Dim alentidades As New ArrayList
        '    alentidades.Add(New VinculoClaseDN("EmpresasDN", "EmpresasDN.EmpresaDN"))
        '    alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.ConcesionarioDN"))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
        '    Return mapinst
        'End If

        'If (pTipo.FullName = "EmpresasDN.EmpleadoDN") Then
        '    Me.MapearClase("mPeriodo", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    Return mapinst
        'End If

        'If (pTipo.FullName = "EmpresasDN.ResponsableAgrupacionDeEmpresasDN") Then
        '    Dim mapinstSub As New InfoDatosMapInstClaseDN
        '    Dim alentidades As New ArrayList

        '    Me.VincularConClase("mEntidadResponsable", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    Me.MapearClase("mPeriodo", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)

        '    Return mapinst
        'End If

        'If (pTipo.FullName = "EmpresasDN.ResponsableDePersonalDN") Then
        '    Me.MapearClase("mPeriodo", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    Return mapinst
        'End If
        'If (pTipo.FullName = "EmpresasDN.RolDepartamentoDN") Then
        '    Me.MapearClase("mPeriodo", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
        '    Return mapinst
        'End If

        ''FINZONA: EmpresasDN ________________________________________________________________

        ''ZONA: LocalizacionesDN ________________________________________________________________

        'If (pTipo.FullName = "LocalizacionesDN.ZonaDN") Then
        '    Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
        '    Dim alentidades As ArrayList = Nothing

        '    Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    Me.VincularConClase("mHijos", New ElementosDeEnsamblado("LocalizacionesDN", "LocalizacionesDN.ZonaDN"), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)
        '    Me.VincularConClase("mPadre", New ElementosDeEnsamblado("LocalizacionesDN", "LocalizacionesDN.ZonaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

        '    Return mapinst
        'End If

        'If (pTipo.FullName = "LocalizacionesDN.IContactoElementoDN") Then
        '    Dim alentidades As New ArrayList
        '    alentidades.Add(New VinculoClaseDN("LocalizacionesDN", "LocalizacionesDN.ContactoElementoDN"))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
        '    Return mapinst
        'End If

        'If (pTipo.FullName = "LocalizacionesDN.ContactoDN") Then
        '    Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
        '    Dim alentidades As ArrayList = Nothing

        '    Me.MapearClase("mColRelacionEspecificables", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

        '    Return mapinst
        'End If

        'If (pTipo.FullName = "LocalizacionesDN.Temporales.IntervaloFechasDN") Then

        '    Me.MapearClase("mFechaModificacion", CampoAtributoDN.NoProcesar, campodatos, mapinst)
        '    Me.MapearClase("mBaja", CampoAtributoDN.NoProcesar, campodatos, mapinst)
        '    Me.MapearClase("mNombre", CampoAtributoDN.NoProcesar, campodatos, mapinst)

        '    Return mapinst
        'End If

        ''FINZONA: LocalizacionesDN ________________________________________________________________

        ''ZONA: PersonasDN ________________________________________________________________

        'If pTipo.FullName = "PersonasDN.PersonaDN" Then
        '    Dim mapSubInst As New InfoDatosMapInstClaseDN
        '    Dim alentidades As ArrayList = Nothing

        '    ' mapeado de la clase referida por el campo
        '    mapSubInst.NombreCompleto = "LocalizacionesDN.NifDN"

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapSubInst
        '    campodatos.NombreCampo = "mID"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapSubInst
        '    campodatos.NombreCampo = "mFechaModificacion"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapSubInst
        '    campodatos.NombreCampo = "mBaja"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapSubInst
        '    campodatos.NombreCampo = "mNombre"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

        '    ' FIN    mapeado de la clase referida por el campo ******************

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mNIF"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

        '    campodatos.MapSubEntidad = mapSubInst

        '    Return mapinst
        'End If

        ''FINZONA: PersonasDN ________________________________________________________________

        ''ZONA: Framework.DatosNegocio ________________________________________________________________
        'If (pTipo.FullName = "Framework.DatosNegocio.Arboles.NodoBaseDN") Then

        '    Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

        '    Return mapinst

        'End If

        'If (pTipo.FullName = "Framework.DatosNegocio.Arboles.ColNodosDN") Then

        '    Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

        '    Return mapinst

        'End If

        'If (pTipo.FullName = "DatosNegocio.ArrayListValidable") Then

        '    Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

        '    Return mapinst

        'End If

        ''FINZONA: Framework.DatosNegocio ________________________________________________________________


        ''ZONA: EmpresaDN ________________________________________________________________

        'If pTipo.FullName = "EmpresasDN.EmpresaDN" Then
        '    Dim mapSubInst As New InfoDatosMapInstClaseDN

        '    ' mapeado de la clase referida por el campo
        '    mapSubInst.NombreCompleto = "LocalizacionesDN.CifDN"

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapSubInst
        '    campodatos.NombreCampo = "mID"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapSubInst
        '    campodatos.NombreCampo = "mFechaModificacion"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapSubInst
        '    campodatos.NombreCampo = "mBaja"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapSubInst
        '    campodatos.NombreCampo = "mNombre"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


        '    ' FIN    mapeado de la clase referida por el campo ******************

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mCif"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

        '    campodatos.MapSubEntidad = mapSubInst

        '    Return mapinst
        'End If





        'If pTipo.FullName = "EmpresaToyotaDN.ConcesionarioDN" Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mIDLocal"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

        '    Return mapinst
        'End If

        'If pTipo.FullName = "EmpresasDN.AgrupacionDeEmpresasDN" Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mCodigo"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

        '    Return mapinst
        'End If

        ''FINZONA: EmpresaDN ________________________________________________________________




        ''If (pTipo.FullName = "MotorADTest.pruebaD") Then
        ''    Dim alentidades As New ArrayList

        ''    alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.PruebaDHC"))
        ''    alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.PruebaDHuellaCacheDN"))
        ''    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ActualizarClasesHuella) = alentidades
        ''    Return mapinst
        ''End If


        '' ojo esta modificación se debe aplicar siempre si el tipo hereda de una huella es decir en el metodo que lo llamo
        'If (TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo)) Then
        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mValidador"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)
        'End If


        Return Nothing
    End Function


    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class








Public Class telefono
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mPersona As Persona
    <RelacionPropCampoAtribute("mPersona")> Public Property Dueño() As Persona
        Get
            Return mPersona
        End Get
        Set(ByVal value As Persona)
            Me.CambiarValorRef(Of Persona)(value, mPersona)
        End Set
    End Property
End Class
Public Class Hijo
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mPersona As Persona
    <RelacionPropCampoAtribute("mPersona")> Public Property Padre() As Persona
        Get
            Return mPersona
        End Get
        Set(ByVal value As Persona)
            Me.CambiarValorRef(Of Persona)(value, mPersona)
        End Set
    End Property
End Class

Public Class colPersona
    Inherits Framework.DatosNegocio.ArrayListValidable(Of Persona)

End Class
Public Class Persona
    Inherits Framework.DatosNegocio.EntidadDN
    Protected WithEvents mcabeza As cabeza
    <RelacionPropCampoAtribute("mcabeza")> Public Property cabeza() As cabeza
        Get
            Return mcabeza
        End Get
        Set(ByVal value As cabeza)
            Me.CambiarValorRef(Of cabeza)(value, mcabeza)
        End Set
    End Property

    Private Sub mcabeza_abrirojo() Handles mcabeza.abrirojo
        'Beep()

    End Sub

    Private Sub mcabeza_cerrarojo() Handles mcabeza.cerrarojo
        'Beep()
    End Sub
End Class



Public Class cabeza
    Inherits Framework.DatosNegocio.EntidadDN

    Public Event abrirojo()
    Public Event cerrarojo()

    Protected mOjoAbierto As Boolean

    Public Sub abrircerrarOjo()

        mOjoAbierto = Not mOjoAbierto
        If mOjoAbierto Then
            RaiseEvent abrirojo()
        Else
            RaiseEvent cerrarojo()
        End If



    End Sub

End Class


Public Class Equipo
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mcolPersonas As colPersona
    <RelacionPropCampoAtribute("mcolPersonas")> Public Property colPersonas() As colPersona
        Get
            Return mcolPersonas
        End Get
        Set(ByVal value As colPersona)
            Me.CambiarValorRef(Of colPersona)(value, mcolPersonas)
        End Set
    End Property
End Class




Public Class ParticipanteTA
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IParticipante

    Protected mPersona As Persona

    Public Property NombreAlias() As String Implements IParticipante.NombreAlias
        Get
            Return Me.mpersona.Nombre
        End Get
        Set(ByVal value As String)
            Me.mpersona.Nombre = value
        End Set
    End Property

    <RelacionPropCampoAtribute("mPersona")> Public Property persona() As Persona
        Get
            Return Me.mPersona
        End Get
        Set(ByVal value As Persona)
            Me.CambiarValorRef(Of Persona)(value, Me.mPersona)
        End Set
    End Property

End Class

Public Interface IParticipante
    Property NombreAlias() As String

End Interface

Public Class Concurso
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mIParticipante As IParticipante


    <RelacionPropCampoAtribute("mIParticipante")> Property ParticipantePrincipal() As IParticipante
        Get
            Return Me.mIParticipante
        End Get
        Set(ByVal value As IParticipante)
            Me.CambiarValorRef(Of IParticipante)(value, mIParticipante)
        End Set
    End Property






    Protected mEquipo As htEquipo

    <RelacionPropCampoAtribute("mEquipo")> _
    Public Property Equipo() As htEquipo

        Get
            Return mEquipo
        End Get

        Set(ByVal value As htEquipo)
            CambiarValorRef(Of htEquipo)(value, mEquipo)

        End Set
    End Property





End Class


Public Class htEquipo
    Inherits Framework.DatosNegocio.HuellaEntidadTipadaDN(Of Equipo)


    Public Sub New()

    End Sub
End Class
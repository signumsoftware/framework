Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Framework.Usuarios.DN

'<TestClass()> Public Class MNavegacionDatosTest
'    Dim mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
'#Region "Additional test attributes"
'    '
'    ' You can use the following additional attributes as you write your tests:
'    '
'    ' Use ClassInitialize to run code before running the first test in the class
'    ' <ClassInitialize()> Public Shared Sub MyClassInitialize(ByVal testContext As TestContext)
'    ' End Sub
'    '
'    ' Use ClassCleanup to run code after all tests in a class have run
'    ' <ClassCleanup()> Public Shared Sub MyClassCleanup()
'    ' End Sub
'    '
'    ' Use TestInitialize to run code before running each test
'    ' <TestInitialize()> Public Sub MyTestInitialize()
'    ' End Sub
'    '
'    ' Use TestCleanup to run code after each test has run
'    ' <TestCleanup()> Public Sub MyTestCleanup()
'    ' End Sub
'    '
'#End Region

'    Private Sub ObtenerRecurso()

'        Dim connectionstring As String
'        Dim htd As New Dictionary(Of String, Object)

'        connectionstring = "server=localhost;database=sspruebasft;user=sa;pwd='sa'"
'        htd.Add("connectionstring", connectionstring)
'        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

'        'Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New Framework.AccesoDatos.MotorAD.LN.GestorMapPersistenciaCamposAMVDocsEntrantesLN
'        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New Framework.AccesoDatos.MotorAD.LN.GestorMapPersistenciaCamposNULOLN

'    End Sub


'    <TestMethod()> Public Sub TCrearEntornoMNavD()
'        ObtenerRecurso()
'        Dim gbd As New MNavegacionDatosLN.GBD(mRecurso)
'        gbd.EjecutarTodoBasico("", "", "")

'    End Sub


'    <TestMethod()> Public Sub RegistrarTipo()

'        TCrearEntornoMNavD()




'        Dim ln As New MNavegacionDatosLN.EntidadDatosLN(Nothing, Me.mRecurso)
'        ln.RegistrarTipo(GetType(PrincipalDN))



'        ln = New MNavegacionDatosLN.EntidadDatosLN(Nothing, Me.mRecurso)
'        ln.RegistrarTipo(GetType(UsuarioDN))

'        ln = New MNavegacionDatosLN.EntidadDatosLN(Nothing, Me.mRecurso)
'        ln.RegistrarTipo(GetType(RolDN))

'        ln = New MNavegacionDatosLN.EntidadDatosLN(Nothing, Me.mRecurso)
'        ln.RegistrarTipo(GetType(telefono))

'        ln = New MNavegacionDatosLN.EntidadDatosLN(Nothing, Me.mRecurso)
'        ln.RegistrarTipo(GetType(VincReversaaPrincipalDN))

'        'Dim ensamblado As Reflection.Assembly
'        ' Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo(GetType(FN.Empresas.DN.EmpleadoDN).FullName, ensamblado, Nothing)

'        ln = New MNavegacionDatosLN.EntidadDatosLN(Nothing, Me.mRecurso)
'        'ln.RegistrarEnsamblado(ensamblado)
'        ln.RegistrarEnsamblado(GetType(FN.Empresas.DN.EmpleadoDN).Assembly)

'    End Sub


'    <TestMethod()> Public Sub RecuperarRelacionesTipo()


'        RegistrarTipo()

'        Dim ln As New MNavegacionDatosLN.EntidadDatosLN(Nothing, Me.mRecurso)
'        Dim col As MNavegacionDatosDN.ColRelacionEntidadesDN
'        col = ln.RecuperarRelaciones(GetType(PrincipalDN))

'        System.Diagnostics.Debug.WriteLine(col.Count)
'        If col.Count < 1 Then
'            Throw New ApplicationException
'        End If

'    End Sub


'End Class



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





'Public Class telefono
'    Inherits Framework.DatosNegocio.EntidadDN
'    Protected mPersona As Persona
'    Public Property Persona() As Persona
'        Get
'            Return mPersona
'        End Get
'        Set(ByVal value As Persona)
'            Me.CambiarValorRef(Of Persona)(value, mPersona)
'        End Set
'    End Property
'End Class


'Public Class Persona
'    Inherits Framework.DatosNegocio.EntidadDN
'    Protected mcabeza As cabeza
'    Public Property cabeza() As cabeza
'        Get
'            Return mcabeza
'        End Get
'        Set(ByVal value As cabeza)
'            Me.CambiarValorRef(Of cabeza)(value, mcabeza)
'        End Set
'    End Property
'End Class



'Public Class cabeza
'    Inherits Framework.DatosNegocio.EntidadDN

'End Class
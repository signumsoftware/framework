Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass()> Public Class GestorSalidaTest

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

    Private Sub ObtenerRecurso()

        Dim connectionstring As String
        Dim htd As New Dictionary(Of String, Object)

        connectionstring = "server=localhost;database=ssPruebasFT;user=sa;pwd='sa'"
        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New Framework.GestorSalida.AD.MapeadoInstanciacion()

    End Sub

    <TestMethod()> Public Sub CrearEntorno()
        ObtenerRecurso()

        Using New Framework.LogicaNegocios.Transacciones.CajonHiloLN(mRecurso)
            Dim gbd As New Framework.GestorSalida.AD.GestorSalidaGBD(mRecurso)
            gbd.CrearTablas()
            gbd.CrearVistas()
        End Using

    End Sub

    <TestMethod()> Public Sub CrearDatosPrueba()
        ObtenerRecurso()

        Dim ur As New Framework.GestorSalida.DN.UnidadRepositorio()
        ur.RutaFisica = "D:\temp\temporal"
        ur.Tiporepositorio = Framework.GestorSalida.DN.TipoRepositorio.Temporal
        ur.Nombre = "Temporal 1"
        Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(ur)

        ur = New Framework.GestorSalida.DN.UnidadRepositorio()
        ur.RutaFisica = "D:\temp\persistente"
        ur.Tiporepositorio = Framework.GestorSalida.DN.TipoRepositorio.Persistente
        ur.Nombre = "Persistente 1"
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(ur)

        If Not System.IO.Directory.Exists("D:\temp\persistente") Then
            System.IO.Directory.CreateDirectory("D:\temp\persistente")
        End If

        If Not System.IO.Directory.Exists("D:\temp\temporal") Then
            System.IO.Directory.CreateDirectory("D:\temp\temporal")
        End If
    End Sub

End Class

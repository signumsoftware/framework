Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Framework.LogicaNegocios.Transacciones


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



    Public mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = Nothing


    <TestMethod()> Public Sub TestMethod1()
        ' TODO: Add test logic here

        CrearElRecurso("")



        Using New CajonHiloLN(mRecurso)





        End Using


    End Sub



    Public Sub sadcac()


        Using tr As New Transaccion




            tr.Confirmar()

        End Using



    End Sub



    Private Sub CrearElRecurso(ByVal connectionstring As String)
        Dim htd As New System.Collections.Generic.Dictionary(Of String, Object)

        If connectionstring Is Nothing OrElse connectionstring = "" Then
            connectionstring = "server=localhost;database=ssPruebasFT;user=sa;pwd='sa'"
        End If

        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a AMV", "sqls", htd)


        'Asignamos el mapeado de  gestor de instanciación
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New Framework.AccesoDatos.MotorAD.LN.GestorMapPersistenciaCamposNULOLN


    End Sub


End Class

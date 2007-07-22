Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting


Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.DatosNegocio
Imports Framework
Imports Framework.TiposYReflexion.DN
Imports System.Collections


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

        CrearElRecurso("")
        CrearEntornoNavap()


    End Sub



    Private Sub CrearEntornoNavap()






        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Dim gbd As New Framework.OperProg.OperProgAD.OperProgGBDAD(mRecurso)

        gbd.EliminarRelaciones()
        gbd.EliminarTablas()

        gbd.CrearTablas()
        gbd.CrearVistas()



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





    Private Sub CrearElRecurso(ByVal connectionstring As String)
        Dim htd As New System.Collections.Generic.Dictionary(Of String, Object)

        If connectionstring Is Nothing OrElse connectionstring = "" Then
            connectionstring = "server=localhost;database=ssPruebasFT;user=sa;pwd='sa'"
        End If

        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a AMV", "sqls", htd)


        'Asignamos el mapeado de  gestor de instanciación
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaCamposAlertasTest


    End Sub

End Class





Public Class GestorMapPersistenciaCamposAlertasTest
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






        '' Para la prueba de mapeado en la interface
        'If (pTipo Is GetType(Framework.Usuarios.DN.UsuarioDN)) Then
        '    Dim alentidades As New ArrayList

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mEntidadUser"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


        '    Return mapinst
        'End If

        'If (pTipo Is GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)) Then
        '    Dim alentidades As New ArrayList

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mDatos"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


        '    Return mapinst
        'End If

        Return Nothing
    End Function


   
End Class




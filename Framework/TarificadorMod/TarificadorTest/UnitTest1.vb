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
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaCamposTaficDTest

    End Sub

    <TestMethod()> Public Sub TCrearEntornoTarifMAP()

        ObtenerRecurso()

        CrearTablas()
        ' crear el entorno de puebas
        CrearEntornoPruebas()



    End Sub
    <TestMethod()> Public Sub TCrearEntornoTarifAMV()

        ObtenerRecurso()

        CrearTablas()
        ' crear el entorno de puebas
        Me.CrearEntornoPruebasAMV()



    End Sub
    <TestMethod()> Public Sub TCrearEntornoTarifInterval()

        ObtenerRecurso()

        CrearTablas()
        ' crear el entorno de puebas
        Me.CrearEntornoPruebasIntervalo()



    End Sub
    Private Sub CrearTablas()

        Dim gbd As New Framework.Tarificador.TarificadorAD.TarificadorGBD(mRecurso)
        gbd.EliminarRelaciones()
        gbd.EliminarTablas()
        gbd.CrearTablas()
        gbd.CrearVistas()
    End Sub


    Private Sub CrearEntornoPruebas()


        ' crear las tablas
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.GenerarTablas2(GetType(SumiValFijoDN), Nothing)


        ' creamos dos categorias con dos valores cada una
        ' creamos los valores de maeado de cada valor caracteristica -> valor modulador

        Dim traductor As Framework.Tarificador.TarificadorDN.TraductorxMapMemoriaDN
        Dim caracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
        Dim col As New Framework.Cuestionario.CuestionarioDN.ColValorNumericoCaracteristicaDN
        Dim colValMap As Framework.Tarificador.TarificadorDN.ColValorModMapDN
        Dim sumi As Tarificador.TarificadorDN.SumiValCaracteristicaDN
        Dim colSumi As New Tarificador.TarificadorDN.ColSumiValCaracteristicaDN

        '  Dim valc1, valc2 As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN
        Dim colValoresDeRespuesta As New Framework.Cuestionario.CuestionarioDN.colIValorCaracteristicaDN



        ' caracteristica primera
        caracteristica = New Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
        caracteristica.Nombre = "Edad"
        col = CrearValoresNumerico(1, 95, caracteristica)
        colValoresDeRespuesta.Add(col.Item(0)) '00000000000000000000000
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(col)



        colValMap = CrearValoresMapeados(col)
        traductor = New Framework.Tarificador.TarificadorDN.TraductorxMapMemoriaDN
        traductor.ColValorModMap = colValMap

        sumi = (New Tarificador.TarificadorDN.SumiValCaracteristicaDN)
        sumi.CaracteristicaDN = col.Item(0).Caracteristica
        sumi.ITraductorDN = traductor

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(sumi)
        colSumi.Add(sumi)




        ' caracteristica segunda
        caracteristica = New Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
        caracteristica.Nombre = "Atiguedad Moto"
        col = CrearValoresNumerico(1, 15, caracteristica)
        colValoresDeRespuesta.Add(col.Item(5)) '000000000000000000000
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(col)


        colValMap = CrearValoresMapeados(col)
        traductor = New Framework.Tarificador.TarificadorDN.TraductorxMapMemoriaDN
        traductor.ColValorModMap = colValMap

        sumi = (New Tarificador.TarificadorDN.SumiValCaracteristicaDN)
        sumi.CaracteristicaDN = col.Item(0).Caracteristica
        sumi.ITraductorDN = traductor

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(sumi)
        colSumi.Add(sumi)




        ' crear el flujo de operaciones

        Dim op, op2 As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

        op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        op.Nombre = "op1"
        op.Operando1 = New SumiValFijoDN(111)
        op.Operando2 = colSumi.Item(0)
        op.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN
        op.DebeCachear = True

        op2 = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        op2.Nombre = "op2"
        op2.Operando1 = op
        op2.Operando2 = colSumi.Item(1)
        op2.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN

        Dim opprog As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN
        opprog = New Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN
        opprog.Periodo.FI = Now
        opprog.Periodo.FI = Now.AddDays(10)
        opprog.IOperacionDN = op2
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(opprog)


        Dim cuestioRes As New Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        cuestioRes = CrearCuestionario(colValoresDeRespuesta)

        ' creamos el suministrador de valor y se lo pasamos a ala operacion principal

        Dim reg As New Framework.Operaciones.OperacionesDN.RecSumiValorDN
        reg.DataSoucers.Add(cuestioRes)

        ' solicitamos el valor

        opprog.IOperacionDN.IRecSumiValorLN = reg
        System.Diagnostics.Debug.WriteLine("valor:" & opprog.IOperacionDN.GetValor.ToString)
        System.Diagnostics.Debug.WriteLine("valor:" & opprog.IOperacionDN.IRecSumiValorLN.DataResults(0).GetValor.ToString)

    End Sub

    Private Sub CrearEntornoPruebasIntervalo()


        ' crear las tablas
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.GenerarTablas2(GetType(SumiValFijoDN), Nothing)


        ' creamos dos categorias con dos valores cada una
        ' creamos los valores de maeado de cada valor caracteristica -> valor modulador

        Dim traductor As Framework.Tarificador.TarificadorDN.TraductorxIntervNumDN
        Dim caracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
        Dim col As New Framework.Cuestionario.CuestionarioDN.ColValorNumericoCaracteristicaDN
        Dim colValMap As Framework.Tarificador.TarificadorDN.ColValorIntervalNumMapDN
        Dim sumi As Tarificador.TarificadorDN.SumiValCaracteristicaDN
        Dim colSumi As New Tarificador.TarificadorDN.ColSumiValCaracteristicaDN

        '  Dim valc1, valc2 As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN
        Dim colValoresDeRespuesta As New Framework.Cuestionario.CuestionarioDN.colIValorCaracteristicaDN



        ' caracteristica primera
        caracteristica = New Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
        caracteristica.Nombre = "Edad"
        col = CrearValoresNumerico(1, 95, caracteristica)
        colValoresDeRespuesta.Add(col.Item(80)) '00000000000000000000000
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(col)



        colValMap = CrearValoresIntervalosMapeados(col)
        traductor = New Framework.Tarificador.TarificadorDN.TraductorxIntervNumDN
        traductor.ColValorIntervalNumMap = colValMap

        sumi = (New Tarificador.TarificadorDN.SumiValCaracteristicaDN)
        sumi.CaracteristicaDN = col.Item(0).Caracteristica
        sumi.ITraductorDN = traductor

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(sumi)
        colSumi.Add(sumi)




        ' caracteristica segunda
        caracteristica = New Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
        caracteristica.Nombre = "Atiguedad Moto"
        col = CrearValoresNumerico(1, 15, caracteristica)
        colValoresDeRespuesta.Add(col.Item(5)) '000000000000000000000
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(col)


        colValMap = CrearValoresIntervalosMapeados(col)
        traductor = New Framework.Tarificador.TarificadorDN.TraductorxIntervNumDN
        traductor.ColValorIntervalNumMap = colValMap

        sumi = (New Tarificador.TarificadorDN.SumiValCaracteristicaDN)
        sumi.CaracteristicaDN = col.Item(0).Caracteristica
        sumi.ITraductorDN = traductor

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(sumi)
        colSumi.Add(sumi)




        ' crear el flujo de operaciones

        Dim op, op2 As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

        op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        op.Nombre = "op1"
        op.Operando1 = New SumiValFijoDN(111)
        op.Operando2 = colSumi.Item(0)
        op.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN


        op2 = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        op2.Nombre = "op2"
        op2.Operando1 = op
        op2.Operando2 = colSumi.Item(1)
        op2.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN

        Dim opprog As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN
        opprog = New Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN
        opprog.Periodo.FI = Now
        opprog.Periodo.FI = Now.AddDays(10)
        opprog.IOperacionDN = op2
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(opprog)


        Dim cuestioRes As New Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        cuestioRes = CrearCuestionario(colValoresDeRespuesta)

        ' creamos el suministrador de valor y se lo pasamos a ala operacion principal

        Dim reg As New Framework.Operaciones.OperacionesDN.RecSumiValorDN
        reg.DataSoucers.Add(cuestioRes)

        ' solicitamos el valor

        opprog.IOperacionDN.IRecSumiValorLN = reg
        System.Diagnostics.Debug.WriteLine("valor:" & opprog.IOperacionDN.GetValor.ToString)

    End Sub


    Private Sub CrearEntornoPruebasAMV()


        ' crear las tablas
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.GenerarTablas2(GetType(SumiValFijoDN), Nothing)


        ' creamos dos categorias con dos valores cada una
        ' creamos los valores de maeado de cada valor caracteristica -> valor modulador

        Dim traductor As Framework.Tarificador.TarificadorDN.TraductorxIntervNumDN
        Dim caracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
        Dim col As New Framework.Cuestionario.CuestionarioDN.ColValorNumericoCaracteristicaDN
        Dim colValMap As Framework.Tarificador.TarificadorDN.ColValorIntervalNumMapDN
        Dim sumi As Tarificador.TarificadorDN.SumiValCaracteristicaDN
        Dim colSumi As New Tarificador.TarificadorDN.ColSumiValCaracteristicaDN

        '  Dim valc1, valc2 As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN
        Dim colValoresDeRespuesta As New Framework.Cuestionario.CuestionarioDN.colIValorCaracteristicaDN



        ' caracteristica primera
        caracteristica = New Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
        caracteristica.Nombre = "Edad"
        col = CrearValoresNumerico(1, 95, caracteristica)
        colValoresDeRespuesta.Add(col.Item(80)) '00000000000000000000000
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(col)



        colValMap = CrearValoresIntervalosMapeados(col)
        traductor = New Framework.Tarificador.TarificadorDN.TraductorxIntervNumDN
        traductor.ColValorIntervalNumMap = colValMap

        sumi = (New Tarificador.TarificadorDN.SumiValCaracteristicaDN)
        sumi.CaracteristicaDN = col.Item(0).Caracteristica
        sumi.ITraductorDN = traductor

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(sumi)
        colSumi.Add(sumi)




        ' caracteristica segunda
        caracteristica = New Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
        caracteristica.Nombre = "Atiguedad Moto"
        col = CrearValoresNumerico(1, 15, caracteristica)
        colValoresDeRespuesta.Add(col.Item(5)) '000000000000000000000
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(col)


        colValMap = CrearValoresIntervalosMapeados(col)
        traductor = New Framework.Tarificador.TarificadorDN.TraductorxIntervNumDN
        traductor.ColValorIntervalNumMap = colValMap

        sumi = (New Tarificador.TarificadorDN.SumiValCaracteristicaDN)
        sumi.CaracteristicaDN = col.Item(0).Caracteristica
        sumi.ITraductorDN = traductor

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(sumi)
        colSumi.Add(sumi)




        ' crear el flujo de operaciones

        Dim op, op2 As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

        op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        op.Nombre = "op1"
        op.Operando1 = New SumiValFijoDN(111)
        op.Operando2 = colSumi.Item(0)
        op.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN


        op2 = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        op2.Nombre = "op2"
        op2.Operando1 = op
        op2.Operando2 = colSumi.Item(1)
        op2.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN

        Dim opprog As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN
        opprog = New Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN
        opprog.Periodo.FI = Now
        opprog.Periodo.FI = Now.AddDays(10)
        opprog.IOperacionDN = op2
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.Guardar(opprog)


        Dim cuestioRes As New Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        cuestioRes = CrearCuestionario(colValoresDeRespuesta)

        ' creamos el suministrador de valor y se lo pasamos a ala operacion principal

        Dim reg As New Framework.Operaciones.OperacionesDN.RecSumiValorDN
        reg.DataSoucers.Add(cuestioRes)

        ' solicitamos el valor

        opprog.IOperacionDN.IRecSumiValorLN = reg
        System.Diagnostics.Debug.WriteLine("valor:" & opprog.IOperacionDN.GetValor.ToString)

    End Sub


    Private Function CrearValoresNumerico(ByVal numinicio As Integer, ByVal numfinal As Integer, ByVal caract As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN) As Framework.Cuestionario.CuestionarioDN.ColValorNumericoCaracteristicaDN


        Dim col As New Framework.Cuestionario.CuestionarioDN.ColValorNumericoCaracteristicaDN

        For a As Integer = numinicio To numfinal

            Dim valCaract As Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN

            valCaract = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
            valCaract.Caracteristica = caract
            valCaract.Valor = a
            col.Add(valCaract)
        Next


        Return col

    End Function

    Public Function CrearValoresMapeados(ByVal colVNC As Framework.Cuestionario.CuestionarioDN.ColValorNumericoCaracteristicaDN) As Framework.Tarificador.TarificadorDN.ColValorModMapDN


        CrearValoresMapeados = New Framework.Tarificador.TarificadorDN.ColValorModMapDN

        Dim valorMap As Framework.Tarificador.TarificadorDN.ValorModMapDN

        valorMap = New Framework.Tarificador.TarificadorDN.ValorModMapDN
        valorMap.Caracteristica = colVNC.Item(0).Caracteristica

        valorMap.ColValorNumericoCaracteristicaDN.Add(colVNC.Item(0))
        colVNC.RemoveAt(0)
        valorMap.ColValorNumericoCaracteristicaDN.Add(colVNC.Item(0))
        colVNC.RemoveAt(0)
        valorMap.ColValorNumericoCaracteristicaDN.Add(colVNC.Item(0))
        colVNC.RemoveAt(0)
        valorMap.Valor = 1


        CrearValoresMapeados.Add(valorMap)

        Dim valor As Double = 0.98


        For Each vc2 As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN In colVNC

            valor += 0.01
            valorMap = New Framework.Tarificador.TarificadorDN.ValorModMapDN
            valorMap.Caracteristica = vc2.Caracteristica

            valorMap.Valor = valor
            valorMap.ColValorNumericoCaracteristicaDN.Add(vc2)
            CrearValoresMapeados.Add(valorMap)

        Next




    End Function


    Public Function CrearValoresIntervalosMapeados(ByVal colVNC As Framework.Cuestionario.CuestionarioDN.ColValorNumericoCaracteristicaDN) As Framework.Tarificador.TarificadorDN.ColValorIntervalNumMapDN


        CrearValoresIntervalosMapeados = New Framework.Tarificador.TarificadorDN.ColValorIntervalNumMapDN

        Dim ValorIntervalNumMap As Framework.Tarificador.TarificadorDN.ValorIntervalNumMapDN
        Dim intervalo, intervaloi, intervalof As Framework.DatosNegocio.IntvaloNumericoDN



        intervalo = colVNC.recuperarIntervalo
        Dim amplitud As Double = intervalo.Amplitud


        intervaloi = New Framework.DatosNegocio.IntvaloNumericoDN
        intervaloi.ValInf = intervalo.ValInf
        intervaloi.ValSup = intervalo.ValInf + (amplitud / 2)

        intervalof = New Framework.DatosNegocio.IntvaloNumericoDN
        intervalof.ValInf = intervalo.ValInf + (amplitud / 2) + 1
        intervalof.ValSup = intervalo.ValSup

        ValorIntervalNumMap = New Framework.Tarificador.TarificadorDN.ValorIntervalNumMapDN
        ValorIntervalNumMap.ValorNumerico = 1
        ValorIntervalNumMap.Intervalo = intervaloi
        CrearValoresIntervalosMapeados.Add(ValorIntervalNumMap)


        ValorIntervalNumMap = New Framework.Tarificador.TarificadorDN.ValorIntervalNumMapDN
        ValorIntervalNumMap.ValorNumerico = 2
        ValorIntervalNumMap.Intervalo = intervalof
        CrearValoresIntervalosMapeados.Add(ValorIntervalNumMap)

    End Function

    Public Function CrearCuestionario(ByVal colValoresDeRespuesta As Framework.Cuestionario.CuestionarioDN.colIValorCaracteristicaDN) As Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        ' creamos el cuestionario resuelto con los valores de als respuestas

        Dim cuestioRes As New Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        Dim respuesta As Cuestionario.CuestionarioDN.RespuestaDN
        Dim pregunta As Cuestionario.CuestionarioDN.PreguntaDN


        For Each valc1 As Cuestionario.CuestionarioDN.IValorCaracteristicaDN In colValoresDeRespuesta


            pregunta = New Cuestionario.CuestionarioDN.PreguntaDN
            pregunta.CaracteristicaDN = valc1.Caracteristica
            respuesta = New Cuestionario.CuestionarioDN.RespuestaDN
            respuesta.IValorCaracteristicaDN = valc1
            respuesta.PreguntaDN = pregunta
            cuestioRes.ColRespuestaDN.Add(respuesta)

            cuestioRes.ColRespuestaDN.Add(respuesta)

        Next


        Return cuestioRes

    End Function






    Private Sub CrearEntPruebasTarificadorAMV()

        '''''''''''''''''''''''''
        ' crear las tablas
        '''''''''''''''''''''''''
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        gi.GenerarTablas2(GetType(SumiValFijoDN), Nothing)




        '''''''''''''''''''''''''
        ' crear las tablas
        '''''''''''''''''''''''''



    End Sub




End Class


Public Class GestorMapPersistenciaCamposTaficDTest
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





        If (pTipo Is GetType(Cuestionario.CuestionarioDN.IValorCaracteristicaDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Cuestionario.CuestionarioDN.ValorCaracteristicaFechaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Cuestionario.CuestionarioDN.ValorTextoCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN)))


            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst

        End If



        '----Tarificador Test------------------------------------------------------------------------
        If (pTipo Is GetType(Operaciones.OperacionesDN.OperacionConfiguradaDN)) Then
            Dim alentidades As New ArrayList


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mPeriodo"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

            Return mapinst


        End If


        If (pTipo Is GetType(Operaciones.OperacionesDN.OperacionSimpleBaseDN)) Then
            Dim alentidades As New ArrayList


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst


        End If

        If (pTipo Is GetType(Tarificador.TarificadorDN.SumiValCaracteristicaDN)) Then
            Dim alentidades As New ArrayList


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst


        End If


        ' Para la prueba de mapeado en la interface
        If (pTipo Is GetType(Operaciones.OperacionesDN.IOperadorDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Operaciones.OperacionesDN.SumaOperadorDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Operaciones.OperacionesDN.MultiplicacionOperadorDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst
        End If





        If (pTipo Is GetType(Operaciones.OperacionesDN.IOperacionSimpleDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Operaciones.OperacionesDN.OperacionSimpleBaseDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst
        End If

        If (pTipo Is GetType(Operaciones.OperacionesDN.ISuministradorValorDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Operaciones.OperacionesDN.OperacionSimpleBaseDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Tarificador.TarificadorDN.SumiValCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(SumiValFijoDN)))

            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst
        End If


        If (pTipo Is GetType(Tarificador.TarificadorDN.ITraductorDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Tarificador.TarificadorDN.TraductorxIntervNumDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Tarificador.TarificadorDN.TraductorxMapMemoriaDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst
        End If

        Return Nothing
    End Function


    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class



Public Class SumiValFijoDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN

    Private mvalor As Double


    Public Sub New()

    End Sub

    Public Sub New(ByVal pvalor As Double)
        mvalor = pvalor

    End Sub

    Public Property valor() As Double
        Get
            Return Me.mvalor
        End Get
        Set(ByVal value As Double)
            Me.CambiarValorVal(Of Double)(value, Me.mvalor)
        End Set
    End Property


    Public Function GetValor() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.GetValor
        Return mvalor
    End Function

    Public Property IRecSumiValorLN() As Framework.Operaciones.OperacionesDN.IRecSumiValorLN Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.IRecSumiValorLN
        Get

        End Get
        Set(ByVal value As Framework.Operaciones.OperacionesDN.IRecSumiValorLN)

        End Set
    End Property

    Public ReadOnly Property ValorCacheado() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.ValorCacheado
        Get
            Return Me.mvalor
        End Get
    End Property

    Public Function RecuperarOrden() As Integer Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.RecuperarOrden
        Throw New NotImplementedException("Recuperar orden no está implementado para esta clase")
    End Function

End Class
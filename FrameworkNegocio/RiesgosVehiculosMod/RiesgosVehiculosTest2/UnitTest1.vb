Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections

Imports Framework.DatosNegocio
Imports Framework.LogicaNegocios.Transacciones


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

    <TestMethod()> Public Sub CrearEntornoInformePresupuesto()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            Dim ad As New FN.RiesgosVehiculos.AD.QueryBuilding.AIQueryBuildingGBD(mRecurso)
            ad.CrearTablas()
            ad.CrearVistas()
        End Using

    End Sub

    <TestMethod()> Public Sub CrearDatosBasicosInformePresupuesto()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            FN.RiesgosVehiculos.LN.QueryBuilding.QueryBuildingLN.CargarDatosBasicos("D:\empresa\AMV\Modelo Informes\Plantillas Finales")
            'Dim ad As New FN.RiesgosVehiculos.AD.QueryBuilding.AIQueryBuildingGBD(mRecurso)
            'ad.CargarDatosBasicos("D:\empresa\AMV\Modelo Informes\Plantillas Finales")
        End Using
    End Sub

    <TestMethod()> Public Sub CargarEsquemaXMLInformePresupuesto()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            Dim ln As New FN.RiesgosVehiculos.LN.QueryBuilding.QueryBuildingLN()
            ln.PresupuestoCargarEsquemaXML()
        End Using
    End Sub

    <TestMethod()> Public Sub GenerarInformePresupuesto()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            Dim ln As New FN.RiesgosVehiculos.LN.QueryBuilding.QueryBuildingLN()
            ln.InformePresupuesto("1")
        End Using
    End Sub

    <TestMethod()> Public Sub ComprobarTarifasFicheroWEB()
        ObtenerRecurso()

        Dim CargaFicherosAD As New FN.RiesgosVehiculos.AD.AMVCargaFicheroWebAD()

        Dim listaCob As String = String.Empty
        Dim modalidad As String
        Dim primaD As Double
        Dim fechaEfecto As Date
        Dim datosRiesgo As String = String.Empty
        Dim listaCaract As String = String.Empty

        Using New CajonHiloLN(mRecurso)

            Dim rutaCompletaFichero As String = "C:\Documents and Settings\Oscar\Escritorio\Tarificación\Pruebas\20070417-0500ORG.dat"
            Dim lineaFicheroWeb As String
            Dim sr As New System.IO.StreamReader(rutaCompletaFichero)

            Dim strAppDir As String
            strAppDir = "C:\Documents and Settings\Oscar\Escritorio\Tarificación\Pruebas\"
            Dim nombreFicheroSalida As String
            nombreFicheroSalida = strAppDir & System.Guid.NewGuid.ToString() & ".txt"
            Dim sw As New System.IO.StreamWriter(nombreFicheroSalida, True)

            Dim lineaFichero As String
            lineaFichero = "fechaEfecto" & vbTab & "datosRiesgo" & vbTab & "listaCaract" & vbTab & "Prima" & vbTab & "PrimaCalculada" & _
                            vbTab & "PrimaBonificada" & vbTab & "Resultado" & vbTab & "ResultadoB"
            sw.WriteLine(lineaFichero)


            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = ln.RecuperarLista(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))(0)
            Dim irec As FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RVIRecSumiValorLN
            irec = New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RVIRecSumiValorLN()

            Do Until sr.EndOfStream

                Try
                    irec.ClearAll()

                    lineaFicheroWeb = sr.ReadLine()
                    modalidad = lineaFicheroWeb.Substring(401, 10).Trim().ToUpper()
                    listaCob = lineaFicheroWeb.Substring(411, 50).Trim().ToUpper()


                    'Obtenemos el listado de coberturas
                    If listaCob.Length = 0 Then
                        If modalidad = "BASIC" Then
                            listaCob = "RCO|RCV|DEF"
                        ElseIf modalidad = "BASICRI" Then
                            listaCob = "RCO|RCV|DEF|RI"
                        ElseIf modalidad = "TR" Then
                            listaCob = "RCO|RCV|DEF|RI|DAÑOS"
                        Else
                            Throw New ApplicationException("Las coberturas no pueden ser nulas")
                        End If
                    Else
                        listaCob = listaCob.Substring(0, listaCob.Length - 1).Replace(";", "|")
                    End If

                    'Obtenemos la fecha de efecto
                    Dim fechaEfectoStr As String
                    fechaEfectoStr = lineaFicheroWeb.Substring(490, 10).Trim()
                    If fechaEfectoStr.Length = 0 Then
                        fechaEfectoStr = lineaFicheroWeb.Substring(462, 10).Trim()
                    End If

                    fechaEfecto = CType(fechaEfectoStr, Date)

                    'Datos del riesgo
                    Dim modelo As String = lineaFicheroWeb.Substring(191, 80).Trim().ToUpper()
                    Dim marca As String = lineaFicheroWeb.Substring(111, 80).Trim().ToUpper()
                    Dim esMatriculada As String = lineaFicheroWeb.Substring(281, 1).Trim()
                    Dim cilindrada As String = lineaFicheroWeb.Substring(273, 8).Trim()

                    datosRiesgo = modelo & "|" & marca & "|" & esMatriculada & "|" & cilindrada

                    'Se crea el objeto Tarifa
                    irec.Tarifa = Me.GenerarTarifa(listaCob, datosRiesgo, fechaEfecto)
                    irec.DataSoucers.Add(irec.Tarifa)

                    'Generamos el objeto cuestionario resuelto
                    Dim fechaCarnet As Date
                    Dim anyosCarnet As Long
                    If lineaFicheroWeb.Substring(303, 1).Trim() = "1" Then
                        fechaCarnet = CType(lineaFicheroWeb.Substring(306, 10).Trim(), Date)
                        anyosCarnet = DateDiff(DateInterval.Year, fechaCarnet, fechaEfecto)
                    Else
                        anyosCarnet = 0
                    End If

                    Dim fechaNac As Date = CType(lineaFicheroWeb.Substring(41, 10).Trim(), Date)
                    Dim codPostal As String = lineaFicheroWeb.Substring(101, 10).Trim()

                    Dim fechaMatStr As String = lineaFicheroWeb.Substring(283, 10).Trim()
                    Dim fechaMatriculacion As Date
                    Dim antMoto As Long = 0
                    If fechaMatStr.Length > 0 Then
                        fechaMatriculacion = CType(fechaMatStr, Date)
                        antMoto = DateDiff(DateInterval.Year, fechaMatriculacion, fechaEfecto)
                    End If

                    Dim edadMCND As Long = Long.MaxValue
                    Dim coefMCND As String = String.Empty
                    If lineaFicheroWeb.Substring(1154, 1).Trim() <> "0" Then
                        Dim indiceF As Integer = 1308
                        For contador As Integer = 1 To 4
                            coefMCND = lineaFicheroWeb.Substring(indiceF, 10).Trim()
                            If coefMCND.Length = 10 Then
                                Dim aux As Long
                                aux = DateDiff(DateInterval.Year, CType(coefMCND, Date), fechaEfecto)
                                If aux < edadMCND Then
                                    edadMCND = aux
                                End If
                            End If
                            indiceF = indiceF + 179
                        Next

                        If edadMCND = Long.MaxValue Then
                            coefMCND = String.Empty
                        Else
                            coefMCND = edadMCND.ToString()
                        End If
                    End If

                    listaCaract = cilindrada & "|" & anyosCarnet.ToString() & "|" & _
                                DateDiff(DateInterval.Year, fechaNac, fechaEfecto).ToString() & "|" & codPostal.Substring(0, 2) & _
                                "|" & DateDiff(DateInterval.Year, fechaMatriculacion, fechaEfecto).ToString() & "|" & coefMCND

                    irec.DataSoucers.Add(Me.GenerarCuestionarioResuelto(listaCaract, fechaEfecto))

                    'Se obtiene el valor de la tarifa
                    opc.IOperacionDN.IRecSumiValorLN = irec
                    Dim valor As Double = opc.IOperacionDN.GetValor()

                    'Escribir en un fichero o en base de datos los resultados (Categoría - Datos Cuestionario - Prima - PrimaWEB)
                    Dim prima As String = lineaFicheroWeb.Substring(481, 9).Trim()

                    If prima.Length = 0 Then
                        prima = lineaFicheroWeb.Substring(472, 9).Trim()
                    End If

                    If Not Double.TryParse(prima.Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator.ToString()), primaD) Then
                        Throw New Framework.AccesoDatos.ApplicationExceptionAD("Prima básica no válida")
                    End If

                    Dim cadError, cadErrorB As String
                    Dim primaCalculada As Double = Math.Round(valor, 2)
                    Dim coefBonif As String = lineaFicheroWeb.Substring(319, 4).Trim().Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator.ToString())
                    If coefBonif.Length > 0 AndAlso coefBonif <> "0" Then
                        If Left(coefBonif, 1) = "." Then
                            coefBonif = "0" & coefBonif
                        End If
                    End If

                    Dim coefBonifDbl As Double = 1
                    Dim primaBonificada As Double

                    If Double.TryParse(coefBonif, coefBonifDbl) Then
                        primaBonificada = Math.Round(primaCalculada * coefBonifDbl, 2)
                    Else
                        primaBonificada = primaCalculada
                    End If

                    If primaD <> primaCalculada Then
                        cadError = "ERROR"
                    Else
                        cadError = "OK"
                    End If

                    If primaD <> primaBonificada Then
                        cadErrorB = "ERROR"
                    Else
                        cadErrorB = "OK"
                    End If

                    lineaFichero = fechaEfecto & vbTab & datosRiesgo & vbTab & listaCaract & vbTab & primaD.ToString() & vbTab & _
                                    primaCalculada.ToString() & vbTab & primaBonificada.ToString() & vbTab & cadError & _
                                    vbTab & cadErrorB

                    sw.WriteLine(lineaFichero)

                Catch ex As Exception
                    lineaFichero = "ERROR carga: " & ex.Message()
                    sw.WriteLine(lineaFichero)
                End Try
            Loop

            sw.Close()
            sr.Close()


        End Using

    End Sub

    <TestMethod()> _
    Public Sub ComprobarTarifaManual()
        ObtenerRecurso()

        Dim CargaFicherosAD As New FN.RiesgosVehiculos.AD.AMVCargaFicheroWebAD()

        Dim listaCob As String = String.Empty
        Dim fechaEfecto As Date
        Dim datosRiesgo As String = String.Empty
        Dim listaCaract As String = String.Empty

        Using New CajonHiloLN(mRecurso)

            'Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            'Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = ln.RecuperarLista(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))(0)
            'Dim irec As FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RVIRecSumiValorLN
            'irec = New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RVIRecSumiValorLN()



            'irec.ClearAll()

            listaCob = "RCO|RCV|DEF|RI|DAÑOS|AV"
            'listaCob = "RCO|RCV|DEF"

            'Obtenemos la fecha de efecto
            fechaEfecto = New Date(2007, 6, 13)

            'Datos del riesgo
            Dim modelo As String = "Deauville"
            Dim marca As String = "HONDA"
            Dim esMatriculada As String = "1"
            Dim cilindrada As String = "700"
            Dim valorBonificacion As Decimal = 0.9

            datosRiesgo = modelo & "|" & marca & "|" & esMatriculada & "|" & cilindrada

            'Se crea el objeto Tarifa
            Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN = Me.GenerarTarifa(listaCob, datosRiesgo, fechaEfecto)

            'irec.Tarifa = Me.GenerarTarifa(listaCob, datosRiesgo, fechaEfecto)
            'irec.DataSoucers.Add(irec.Tarifa)

            'Generamos el objeto cuestionario resuelto

            Dim anyosCarnet As Long = 33
            Dim edad As Long = 51
            Dim antMoto As Long = 1
            Dim edadMCND As Long = 1
            Dim codPostal As String = "50640"

            listaCaract = cilindrada & "|" & anyosCarnet.ToString() & "|" & _
                        edad.ToString() & "|" & codPostal & _
                        "|" & antMoto.ToString() & "|" & edadMCND.ToString()

            'irec.DataSoucers.Add(Me.GenerarCuestionarioResuelto(listaCaract, fechaEfecto))
            Dim cr As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN = Me.GenerarCuestionarioResuelto(listaCaract, fechaEfecto)
            Dim dt As New FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN()
            tarifa.DatosTarifa = dt
            dt.Tarifa = tarifa
            dt.HeCuestionarioResuelto = New Framework.Cuestionario.CuestionarioDN.HeCuestionarioResueltoDN(cr)

            Dim rvLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()
            rvLN.CargarGrafoTarificacion()
            rvLN.TarificarTarifa(tarifa, Nothing, Nothing, True, True)

            'Se obtiene el valor de la tarifa
            'opc.IOperacionDN.IRecSumiValorLN = irec
            'Dim valor As Double = opc.IOperacionDN.GetValor()

            'GuardarDatos(irec.DataResults)

        End Using
    End Sub

    Private Function GenerarTarifa(ByVal listaCoberturas As String, ByVal datosRiesgo As String, ByVal fechaEfecto As Date) As FN.Seguros.Polizas.DN.TarifaDN

        Dim tr As New FN.Seguros.Polizas.DN.TarifaDN
        Dim p As FN.Seguros.Polizas.DN.ProductoDN
        Dim listaCob As IList
        Dim listaDatosRiesgo As IList
        Dim colCoberturas As New FN.Seguros.Polizas.DN.ColCoberturaDN()
        Dim bln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
        Dim RVLN As FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN
        Dim rm As FN.RiesgosVehiculos.DN.RiesgoMotorDN
        Dim modeloDatos As FN.RiesgosVehiculos.DN.ModeloDatosDN

        Dim nombreModelo As String = String.Empty
        Dim nombreMarca As String = String.Empty
        Dim matriculada As Boolean
        Dim cilindrada As Integer

        p = New FN.Seguros.Polizas.DN.ProductoDN
        p.Nombre = "Basico"

        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)


            bln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            For Each eltoCob As FN.Seguros.Polizas.DN.CoberturaDN In bln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN))
                colCoberturas.Add(eltoCob)
            Next

            listaCob = listaCoberturas.Split(CType("|", Char))

            For Each elto As String In listaCob
                p.ColCoberturas.Add(colCoberturas.RecuperarPrimeroXNombre(elto))
            Next

            tr.ColLineaProducto.Add(New FN.Seguros.Polizas.DN.LineaProductoDN)
            tr.ColLineaProducto(0).Producto = p

            bln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()

            listaDatosRiesgo = datosRiesgo.Split(CType("|", Char))
            If listaDatosRiesgo.Count <> 4 Then
                Throw New ApplicationException("Los datos del riesgo no son correctos")
            Else
                nombreModelo = CType(listaDatosRiesgo(0), String)
                nombreMarca = CType(listaDatosRiesgo(1), String)
                matriculada = CType(listaDatosRiesgo(2), Boolean)
                cilindrada = CType(listaDatosRiesgo(3), Integer)
            End If

            RVLN = New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN
            modeloDatos = RVLN.RecuperarModeloDatos(nombreModelo, nombreMarca, matriculada, fechaEfecto)

            If modeloDatos Is Nothing Then
                Throw New ApplicationException("Los datos del riesgo no son correctos")
            End If

            rm = New FN.RiesgosVehiculos.DN.RiesgoMotorDN()
            rm.Modelo = modeloDatos.Modelo
            rm.Matriculado = modeloDatos.Matriculado
            rm.Cilindrada = cilindrada

            tr.Riesgo = rm
            tr.FEfecto = fechaEfecto

            Return tr

        End Using

    End Function

    Private Function GenerarCuestionarioResuelto(ByVal valoresCaracteristicas As String, ByVal fechaEfecto As Date) As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN

        Dim cur As New Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        Dim cu As New Framework.Cuestionario.CuestionarioDN.CuestionarioDN
        Dim colCaract As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN()
        Dim IValorCaracteristica, ivedad As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN
        Dim listaC As IList(Of String)

        cur.CuestionarioDN = cu

        Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
        colCaract.AddRangeObject(ln.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))

        listaC = valoresCaracteristicas.Split(CType("|", Char))

        If listaC.Count <> 6 Then
            Throw New ApplicationException("El número de características no es correcto")
        End If

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = colCaract.RecuperarPrimeroXNombre("CYLD")
        IValorCaracteristica.Valor = listaC.Item(0)
        IValorCaracteristica.FechaEfectoValor = fechaEfecto
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = colCaract.RecuperarPrimeroXNombre("CARN")
        IValorCaracteristica.Valor = listaC.Item(1)
        IValorCaracteristica.FechaEfectoValor = fechaEfecto
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = colCaract.RecuperarPrimeroXNombre("EDAD")
        IValorCaracteristica.Valor = listaC.Item(2)
        IValorCaracteristica.FechaEfectoValor = fechaEfecto
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)
        ivedad = IValorCaracteristica

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = colCaract.RecuperarPrimeroXNombre("ZONA")
        IValorCaracteristica.Valor = CType(listaC.Item(3), Double)
        IValorCaracteristica.FechaEfectoValor = fechaEfecto
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = colCaract.RecuperarPrimeroXNombre("ANTG")
        IValorCaracteristica.Valor = listaC.Item(4)
        IValorCaracteristica.FechaEfectoValor = fechaEfecto
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        If listaC.Item(5).ToString() <> String.Empty AndAlso listaC.Item(5).ToString() <> "0" Then
            IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
            IValorCaracteristica.Caracteristica = colCaract.RecuperarPrimeroXNombre("MCND")
            IValorCaracteristica.Valor = listaC.Item(5)
            IValorCaracteristica.FechaEfectoValor = fechaEfecto
            IValorCaracteristica.ValorCaracPadre = ivedad
            AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)
        End If

        Return cur

    End Function

    Private Sub AñadirvalorACuestirnarioResuelto(ByVal pCur As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal IValorCaracteristica As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN)

        Dim preg As Framework.Cuestionario.CuestionarioDN.PreguntaDN
        Dim respuesta As Framework.Cuestionario.CuestionarioDN.RespuestaDN

        preg = New Framework.Cuestionario.CuestionarioDN.PreguntaDN
        preg.CaracteristicaDN = IValorCaracteristica.Caracteristica
        pCur.CuestionarioDN.ColPreguntaDN.Add(preg)
        respuesta = New Framework.Cuestionario.CuestionarioDN.RespuestaDN
        respuesta.PreguntaDN = preg
        respuesta.IValorCaracteristicaDN = IValorCaracteristica
        pCur.ColRespuestaDN.Add(respuesta)

    End Sub

    Private Sub GuardarDatos(ByVal objeto As Object)
        Using tr As New Transaccion
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(objeto)

            tr.Confirmar()

        End Using
    End Sub

    Private Sub ObtenerRecurso()

        Dim connectionstring As String
        Dim htd As New Dictionary(Of String, Object)

        connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd='sa'"
        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New FN.RiesgosVehiculos.Test.GestorMapPersistenciaCamposMotosTest()
    End Sub

End Class
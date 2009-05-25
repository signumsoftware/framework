Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Framework.DatosNegocio
Imports Framework.Usuarios.DN
Imports Framework.Usuarios.LN
Imports Framework.Ficheros.FicherosDN
Imports Framework.Ficheros.FicherosAD
Imports Framework.LogicaNegocios.Transacciones

<TestClass()> Public Class GDocEntrantesFSTest

    Private mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = Nothing

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

    <TestMethod()> Public Sub AltaDocumento()
        ' TODO: Add test logic here

        ' CrearElEntorno()
        CrearRecurso()
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin2.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin3.txt", "-1", "-1", "2", "correo")

    End Sub
    <TestMethod()> Public Sub AltaDocumentoTipoNeg()
        ' TODO: Add test logic here

        CrearElEntorno()

        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin.txt", "1", "-1", "1", "Fax")

    End Sub
    <TestMethod()> Public Sub AltaDocumentoHuellaNodo()
        ' TODO: Add test logic here

        CrearElEntorno()

        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin.txt", "-1", "1", "1", "Fax")

    End Sub
    ''' <summary>
    ''' los valores -1 hacen que no se recuperen
    ''' </summary>
    ''' <param name="pRutadoc"></param>
    ''' <param name="pIdTipoEntNegocio"></param>
    ''' <param name="pIdHuellaNodoTipoEntNegocio"></param>
    ''' <remarks></remarks>
    Private Sub AltaDoc(ByVal pRutadoc As String, ByVal pIdTipoEntNegocio As String, ByVal pIdHuellaNodoTipoEntNegocio As String, ByVal idTipocanal As String, ByVal nombreTipoCanal As String)
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


        ' crear el usuario
        Dim pActor As PrincipalDN
        Dim uln As UsuariosLN
        uln = New UsuariosLN(Nothing, mRecurso)
        pActor = uln.ObtenerPrincipal(New DatosIdentidadDN("ato", "ato"))


        ' crear los parametros
        Dim fi As IO.FileInfo
        fi = New IO.FileInfo(pRutadoc)

        Dim hf As HuellaFicheroAlmacenadoIODN
        Dim ten As AmvDocumentosDN.TipoEntNegoioDN


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        ten = gi.Recuperar(Of AmvDocumentosDN.TipoEntNegoioDN)(idTipocanal)




        hf = New HuellaFicheroAlmacenadoIODN(fi)


        hf.Datos = FicherosAD.RecuperarDocAArrayBytes(pRutadoc)

        If pIdTipoEntNegocio <> "-1" Then
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
            ten = gi.Recuperar(Of AmvDocumentosDN.TipoEntNegoioDN)(pIdTipoEntNegocio)
        End If

        Dim huella As AmvDocumentosDN.HuellaNodoTipoEntNegoioDN
        If pIdHuellaNodoTipoEntNegocio <> "-1" Then
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
            huella = gi.Recuperar(Of AmvDocumentosDN.HuellaNodoTipoEntNegoioDN)(pIdHuellaNodoTipoEntNegocio)
        End If


        Dim canal As AmvDocumentosDN.CanalEntradaDocsDN
        ' canal = New AmvDocumentosDN.CanalEntradaDocsDN("mi canal", "c:", False, New AmvDocumentosDN.TipoCanalDN(idTipocanal, nombreTipoCanal))
        canal = New AmvDocumentosDN.CanalEntradaDocsDN("mi canal", "c:", False, New AmvDocumentosDN.TipoCanalDN(nombreTipoCanal, idTipocanal))

        Dim fpa As New AmvDocumentosDN.FicheroParaAlta
        fpa.HuellaFichero = hf
        fpa.TipoEntidad = ten
        fpa.HuellaNodoTipoEntNegoio = huella
        fpa.clanal = canal


        ' ejecutar el metodo

        Dim fl As GDocEntrantesFS.EntradaDocsFS
        fl = New GDocEntrantesFS.EntradaDocsFS(Nothing, mRecurso)
        fl.AltaDocumento("s1", pActor, fpa)
    End Sub
    <TestMethod()> Public Sub PodaArbolTest()

        CrearElEntorno()

        Dim principal As PrincipalDN
        principal = RecuperarPrincipal()

        Dim gdoc As GDocEntrantesFS.EntradaDocsFS
        Dim cabecera As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        cabecera = gdoc.RecuperarArbolTiposEntNegocio("", principal)

        Dim hojaspoda As New ArrayListValidable(Of AmvDocumentosDN.TipoEntNegoioDN)
        Dim hojadePoda As AmvDocumentosDN.TipoEntNegoioDN
        Dim nodoContenedorHoja As AmvDocumentosDN.NodoTipoEntNegoioDN
        ' Dim colNodosPodados As New AmvDocumentosDN.ColNodoTipoEntNegoioDN
        Dim colNodosPodados As New System.Collections.ArrayList
        Dim nhijos, nhijosTrasPoda As Integer

        hojadePoda = CType(cabecera.NodoTipoEntNegoio.Hijos(0), AmvDocumentosDN.NodoTipoEntNegoioDN).ColHojas(0)
        System.Diagnostics.Debug.WriteLine("hojadePoda: " & hojadePoda.Nombre)
        hojaspoda.Add(hojadePoda)

        nodoContenedorHoja = cabecera.NodoTipoEntNegoio.NodoContenedorPorHijos(hojadePoda, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
        nhijos = cabecera.NodoTipoEntNegoio.Hijos.Count
        System.Diagnostics.Debug.WriteLine("nodoContenedorHoja: " & nodoContenedorHoja.Nombre)

        If nodoContenedorHoja Is Nothing Then
            Throw New ApplicationException("nodoContenedorHoja no debe ser nothing")
        End If

        'cabecera.NodoTipoEntNegoio.PodarNodosHijosNoContenedoresHojas(hojaspoda, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos).ToListOFt()
        colNodosPodados.AddRange(cabecera.NodoTipoEntNegoio.PodarNodosHijosNoContenedoresHojas(hojaspoda, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos).ToListOFt)
        System.Diagnostics.Debug.WriteLine("colNodosPodados: " & colNodosPodados.Count)

        nhijosTrasPoda = cabecera.NodoTipoEntNegoio.Hijos.Count

        If cabecera.NodoTipoEntNegoio.Hijos.Count <> 1 Then
            Throw New ApplicationException("solo debia quedar un hijo: " & cabecera.NodoTipoEntNegoio.Hijos.Count)
        End If

        If Not cabecera.NodoTipoEntNegoio.Contenido(nodoContenedorHoja, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
            Throw New ApplicationException("el nodo debia estar contenido")
        End If

        ' verificar la poda de las hojas

        If nodoContenedorHoja.ColHojas.Count <> 1 OrElse Not nodoContenedorHoja.ColHojas.Contiene(hojadePoda, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.MismaRef) Then
            Throw New ApplicationException("probelas en la poda de la hoja")

        End If

    End Sub



    Private Sub CrearRecurso()
        ' crear el recurso
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Dim connectionstring As String
        Dim htd As New System.Collections.Generic.Dictionary(Of String, Object)

        connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd='sa'"
        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a AMV", "sqls", htd)


        'Asignamos el mapeado de Toyota POR al gestor de instanciación
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GDocEntrantesAD.GestorMapPersistenciaCamposAMVDocsEntrantesLN
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()


    End Sub



    <TestMethod()> Public Sub CrearEstruturaBD()

        CrearRecurso()

        Using New CajonHiloLN(mRecurso)




            Using tr As New Transaccion


                Dim gbd As GDocEntrantesGBDLN.GBD
                gbd = New GDocEntrantesGBDLN.GBD(mRecurso)
                gbd.EjecutarTodo()



                tr.Confirmar()

            End Using




        End Using





    End Sub

    <TestMethod()> Public Sub CrearElEntorno()



        CrearEstruturaBD()
        CargarDatosTodos()




    End Sub

    <TestMethod()> Public Sub CargarDatosTodos()


        CrearRecurso()




        Using New CajonHiloLN(mRecurso)



            CargarDatosTodosp()




        End Using



    End Sub

    Public Sub CargarDatosTodosp()
        Using tr As New Transaccion
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


            Dim gbd As GDocEntrantesGBDLN.GBD
            gbd = New GDocEntrantesGBDLN.GBD(mRecurso)
            gbd.cargarDatosTODOS("D:\Signum\Signum\Proyectos\AMV\SolucionAMV\FicherosPrueba\LocalidadesCodPostalProvinciaPais.txt", Nothing)

            ' crear una rutas de almacenamiento para las pruebas

            Dim rutaAlmacenamiento As RutaAlmacenamientoFicherosDN
            rutaAlmacenamiento = New RutaAlmacenamientoFicherosDN

            rutaAlmacenamiento.RutaCarpeta = "D:\Signum\Proyectos\AMV\GDocEntrantes\ficheros\destino"
            rutaAlmacenamiento.EstadoRAF = RutaAlmacenamientoFicherosEstado.Disponible

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(rutaAlmacenamiento)

            Dim ten As AmvDocumentosDN.TipoEntNegoioDN

            ten = New AmvDocumentosDN.TipoEntNegoioDN
            ten.Nombre = "siniestros gordos"
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(ten)


            Dim tf As Framework.Ficheros.FicherosDN.TipoFicheroDN

            tf = New Framework.Ficheros.FicherosDN.TipoFicheroDN
            tf.Nombre = "certificado de tal"
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(tf)

            tr.Confirmar()

        End Using
    End Sub



    <TestMethod()> Public Sub RecuperarOperacionAprocesar()

        CrearElEntorno()

        Dim principal As PrincipalDN
        principal = RecuperarPrincipal()


        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin2.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin3.txt", "-1", "-1", "2", "correo")


        Dim op As AmvDocumentosDN.OperacionEnRelacionENFicheroDN
        Dim gdoc As GDocEntrantesFS.EntradaDocsFS

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAProcesar("", principal, "2")

        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion")
        End If

        If Not op.RelacionENFichero.HuellaFichero.NombreOriginalFichero = "Ficherin3.txt" Then
            Throw New ApplicationException("no se recupero el fichero correcto")
        End If

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAProcesar("", principal, "1")

        If Not op.RelacionENFichero.HuellaFichero.NombreOriginalFichero = "Ficherin.txt" Then
            Throw New ApplicationException("no se recupero el fichero correcto")
        End If


    End Sub


    <TestMethod()> Public Sub RecuperarOperacionPostAprocesar()
        CrearElEntorno()

        Dim principal As PrincipalDN
        principal = RecuperarPrincipal()


        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin2.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin3.txt", "-1", "-1", "2", "correo")


        Dim tf As Framework.Ficheros.FicherosDN.TipoFicheroDN
        Dim ten As AmvDocumentosDN.TipoEntNegoioDN
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        ten = gi.Recuperar(Of AmvDocumentosDN.TipoEntNegoioDN)("1")

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        tf = gi.Recuperar(Of Framework.Ficheros.FicherosDN.TipoFicheroDN)("1")


        Dim gdoc As GDocEntrantesFS.EntradaDocsFS
        Dim op, oppp, opppcerrada As AmvDocumentosDN.OperacionEnRelacionENFicheroDN


        ' para el primer archivo

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAProcesar("", principal, Nothing)
        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If
        op.RelacionENFichero.TipoEntNegoio = ten
        Dim id As New Framework.Ficheros.FicherosDN.IdentificacionDocumentoDN
        id.Identificacion = "ddd"
        id.TipoFichero = tf
        op.RelacionENFichero.HuellaFichero.Colidentificaciones.Add(id)

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        Dim colentidades As AmvDocumentosDN.ColEntNegocioDN
        Dim colOperaciones As AmvDocumentosDN.ColOperacionEnRelacionENFicheroDN
        colentidades = New AmvDocumentosDN.ColEntNegocioDN
        colOperaciones = gdoc.ClasificarOperacion("", principal, op, colentidades)

        System.Diagnostics.Debug.WriteLine(colOperaciones.Item(0).RelacionENFichero.EstadosRelacionENFichero)
        If op.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.Clasificar OrElse colOperaciones.Item(0).RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Clasificando Then
            Throw New ApplicationException("estado incorrecto")
        End If



        'jjjjjjjjjjjjjjjjjjj


        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        oppp = gdoc.RecuperarOperacionAPostProcesar("", principal, Nothing, "")

        System.Diagnostics.Debug.WriteLine(oppp.RelacionENFichero.EstadosRelacionENFichero)


        If oppp Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If

        oppp.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        opppcerrada = gdoc.GuardarOperacion("", principal, oppp)

        System.Diagnostics.Debug.WriteLine(opppcerrada.RelacionENFichero.EstadosRelacionENFichero)

        If opppcerrada Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If
        If opppcerrada.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar OrElse opppcerrada.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Cerrado Then
            Throw New ApplicationException("estado incorrecto")
        End If






        ' para el SEGUNDO archivo

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAPostProcesar("", principal, Nothing, "")
        If Not op Is Nothing Then
            Throw New ApplicationException("no se debia haber recuperado ninguna operacion ")
        End If




    End Sub

    <TestMethod()> Public Sub RechazarOperacion()

        CrearElEntorno()

        Dim principal As PrincipalDN
        principal = RecuperarPrincipal()


        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin2.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin3.txt", "-1", "-1", "2", "correo")

        Dim ten As AmvDocumentosDN.TipoEntNegoioDN
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        ten = gi.Recuperar(Of AmvDocumentosDN.TipoEntNegoioDN)("1")

        Dim gdoc As GDocEntrantesFS.EntradaDocsFS
        Dim op, oppp, opppcerrada As AmvDocumentosDN.OperacionEnRelacionENFicheroDN


        ' para el primer archivo

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAProcesar("", principal, Nothing)
        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If
        'op.RelacionENFichero.EntidadNegocio.TipoEntNegocioReferidora = ten
        'op.RelacionENFichero.EntidadNegocio.IdEntNeg = "asdfasfrvgsef"
        op.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.Rechazar)

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        gdoc.RechazarOperacion("", principal, op)

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)
        If op.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.Rechazar OrElse op.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Creada Then
            Throw New ApplicationException("estado incorrecto")
        End If

    End Sub
    ''' <summary>
    ''' la dn no dispone de los datos suficientes y debiera dar un error de integridad y no guardarse
    ''' </summary>
    ''' <remarks></remarks>
    <TestMethod()> Public Sub ClasificarYCerrarOperacionIncorrecto()

        CrearElEntorno()

        Dim principal As PrincipalDN
        principal = RecuperarPrincipal()


        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin2.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin3.txt", "-1", "-1", "2", "correo")

        Dim ten As AmvDocumentosDN.TipoEntNegoioDN
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        ten = gi.Recuperar(Of AmvDocumentosDN.TipoEntNegoioDN)("1")

        Dim gdoc As GDocEntrantesFS.EntradaDocsFS
        Dim op, oppp, opppcerrada As AmvDocumentosDN.OperacionEnRelacionENFicheroDN


        ' para el primer archivo

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAProcesar("", principal, Nothing)
        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If
        op.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar)
        'op.RelacionENFichero.EntidadNegocio.TipoEntNegocioReferidora = Nothing ' por esto no se puede cerrar
        'op.RelacionENFichero.EntidadNegocio.IdEntNeg = "asdfasfrvgsef"

        op.RelacionENFichero.TipoEntNegoio = Nothing

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)
        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)

        Try
            gdoc.ClasificarYCerrarOperacion("", principal, op)

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine(ex.Message)

            If ex.Message = "la entidad de negocio no indica su tipo de entidad" Then
                Exit Sub
            Else
                Throw

            End If
        End Try

        Throw New ApplicationException("no se produjo la excepcion esperada")
    End Sub



    <TestMethod()> Public Sub Clasificar()

        'CrearElEntorno()
        CrearRecurso()
        Dim principal As PrincipalDN
        principal = RecuperarPrincipal()


        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin2.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin3.txt", "-1", "-1", "2", "correo")


        Dim tf As Framework.Ficheros.FicherosDN.TipoFicheroDN
        Dim ten As AmvDocumentosDN.TipoEntNegoioDN
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        ten = gi.Recuperar(Of AmvDocumentosDN.TipoEntNegoioDN)("1")

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        tf = gi.Recuperar(Of Framework.Ficheros.FicherosDN.TipoFicheroDN)("1")






        Dim gdoc As GDocEntrantesFS.EntradaDocsFS
        Dim op, oppp, opppcerrada As AmvDocumentosDN.OperacionEnRelacionENFicheroDN


        ' para el primer archivo

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAProcesar("", principal, Nothing)
        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If
        op.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.Clasificar)

        Dim id As New Framework.Ficheros.FicherosDN.IdentificacionDocumentoDN
        id.Identificacion = "ddd"
        id.TipoFichero = tf

        op.RelacionENFichero.HuellaFichero.Colidentificaciones.Add(id)
        op.RelacionENFichero.TipoEntNegoio = ten

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)
        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)

        Try




            gdoc.ClasificarOperacion("", principal, op, Nothing)

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine(ex.Message)

            If ex.Message = "la entidad de negocio no indica su tipo de entidad" Then
                Exit Sub
            Else
                Throw

            End If
        End Try


    End Sub

    <TestMethod()> Public Sub VerificarDosPendientres()

        ProcesarTresDocDeCinco()
        Dim principal As PrincipalDN
        principal = RecuperarPrincipal()

        Dim gdoc As GDocEntrantesFS.EntradaDocsFS

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        Dim dts As Data.DataSet
        dts = gdoc.RecuperarNumDocPendientesClasificaryPostClasificacion("", principal, Nothing)

        'If dts.Tables.Count <> 2 Then
        '    Throw New ApplicationException()
        'End If
        If dts.Tables("vwNumDocPendientesPostClasificacionXTipoEntidadNegocio").Rows.Count <> 1 OrElse dts.Tables("vwNumDocPendientesPostClasificacionXTipoEntidadNegocio").Rows(0).Item("Num") <> 1 Then
            Throw New ApplicationException()
        End If
        If dts.Tables("vwNumDocPendientesClasificacionXTipoCanal").Rows.Count <> 1 OrElse dts.Tables("vwNumDocPendientesClasificacionXTipoCanal").Rows(0).Item("Num") <> 2 Then
            Throw New ApplicationException()
        End If

    End Sub


    <TestMethod()> Public Sub ProcesarTresDocDeCinco()

        CrearElEntorno()

        Dim principal As PrincipalDN
        principal = RecuperarPrincipal()


        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin2.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin3.txt", "-1", "-1", "2", "correo")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin4.txt", "-1", "-1", "2", "correo")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin5.txt", "-1", "-1", "2", "correo")

        Dim ten As AmvDocumentosDN.TipoEntNegoioDN
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        ten = gi.Recuperar(Of AmvDocumentosDN.TipoEntNegoioDN)("1")

        Dim gdoc As GDocEntrantesFS.EntradaDocsFS
        Dim op, oppp, opppcerrada As AmvDocumentosDN.OperacionEnRelacionENFicheroDN


        ' para el primer archivo
        ' lo procesamos del todo
        System.Diagnostics.Debug.WriteLine("PRIMERO")

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAProcesar("", principal, Nothing)
        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If
        'op.RelacionENFichero.EntidadNegocio.TipoEntNegocioReferidora = ten
        'op.RelacionENFichero.EntidadNegocio.IdEntNeg = "asdfasfrvgsef"

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        gdoc.ClasificarOperacion("", principal, op, Nothing)

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)
        If op.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.Clasificar OrElse op.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Clasificando Then
            Throw New ApplicationException("estado incorrecto")
        End If



        'jjjjjjjjjjjjjjjjjjj


        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        oppp = gdoc.RecuperarOperacionAPostProcesar("", principal, Nothing, "")

        System.Diagnostics.Debug.WriteLine(oppp.RelacionENFichero.EstadosRelacionENFichero)


        If oppp Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If

        oppp.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        opppcerrada = gdoc.GuardarOperacion("", principal, oppp)

        System.Diagnostics.Debug.WriteLine(opppcerrada.RelacionENFichero.EstadosRelacionENFichero)

        If opppcerrada Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If
        If opppcerrada.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar OrElse opppcerrada.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Cerrado Then
            Throw New ApplicationException("estado incorrecto")
        End If






        ' para el SEGUNDO archivo

        System.Diagnostics.Debug.WriteLine("SEGUNDO")



        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAProcesar("", principal, Nothing)
        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If
        'op.RelacionENFichero.EntidadNegocio.TipoEntNegocioReferidora = ten
        'op.RelacionENFichero.EntidadNegocio.IdEntNeg = "asdfasfrvgsef"

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        gdoc.ClasificarOperacion("", principal, op, Nothing)

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)
        If op.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.Clasificar OrElse op.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Clasificando Then
            Throw New ApplicationException("estado incorrecto")
        End If





        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        oppp = gdoc.RecuperarOperacionAPostProcesar("", principal, Nothing, "")

        System.Diagnostics.Debug.WriteLine(oppp.RelacionENFichero.EstadosRelacionENFichero)


        If oppp Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If

        oppp.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        opppcerrada = gdoc.GuardarOperacion("", principal, oppp)

        System.Diagnostics.Debug.WriteLine(opppcerrada.RelacionENFichero.EstadosRelacionENFichero)

        If opppcerrada Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If
        If opppcerrada.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar OrElse opppcerrada.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Cerrado Then
            Throw New ApplicationException("estado incorrecto")
        End If




        ' para el TERCERO archivo


        System.Diagnostics.Debug.WriteLine("Tercero")


        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAProcesar("", principal, Nothing)
        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If
        'op.RelacionENFichero.EntidadNegocio.TipoEntNegocioReferidora = ten
        'op.RelacionENFichero.EntidadNegocio.IdEntNeg = "asdfasfrvgsef"

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        gdoc.ClasificarOperacion("", principal, op, Nothing)

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)
        If op.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.Clasificar OrElse op.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Clasificando Then
            Throw New ApplicationException("estado incorrecto")
        End If







    End Sub

    <TestMethod()> Public Sub ProcesarDosDocRechazandoSegundo()
        CrearElEntorno()

        Dim principal As PrincipalDN
        principal = RecuperarPrincipal()


        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin2.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin3.txt", "-1", "-1", "2", "correo")

        Dim ten As AmvDocumentosDN.TipoEntNegoioDN
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        ten = gi.Recuperar(Of AmvDocumentosDN.TipoEntNegoioDN)("1")

        Dim gdoc As GDocEntrantesFS.EntradaDocsFS
        Dim op, oppp, opppcerrada As AmvDocumentosDN.OperacionEnRelacionENFicheroDN


        ' para el primer archivo

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAProcesar("", principal, Nothing)
        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If
        'op.RelacionENFichero.EntidadNegocio.TipoEntNegocioReferidora = ten
        'op.RelacionENFichero.EntidadNegocio.IdEntNeg = "asdfasfrvgsef"

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        gdoc.ClasificarOperacion("", principal, op, Nothing)

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)
        If op.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.Clasificar OrElse op.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Clasificando Then
            Throw New ApplicationException("estado incorrecto")
        End If



        'jjjjjjjjjjjjjjjjjjj


        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        oppp = gdoc.RecuperarOperacionAPostProcesar("", principal, Nothing, "")

        System.Diagnostics.Debug.WriteLine(oppp.RelacionENFichero.EstadosRelacionENFichero)


        If oppp Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If

        oppp.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        opppcerrada = gdoc.GuardarOperacion("", principal, oppp)

        System.Diagnostics.Debug.WriteLine(opppcerrada.RelacionENFichero.EstadosRelacionENFichero)

        If opppcerrada Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If
        If opppcerrada.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar OrElse opppcerrada.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Cerrado Then
            Throw New ApplicationException("estado incorrecto")
        End If






        ' para el SEGUNDO archivo


        ' clasifico

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAProcesar("", principal, Nothing)
        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If
        'op.RelacionENFichero.EntidadNegocio.TipoEntNegocioReferidora = ten
        'op.RelacionENFichero.EntidadNegocio.IdEntNeg = "asdfasfrvgsef"

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        gdoc.ClasificarOperacion("", principal, op, Nothing)

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)
        If op.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.Clasificar OrElse op.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Clasificando Then
            Throw New ApplicationException("estado incorrecto")
        End If


        '******************************
        ' rechazo

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        oppp = gdoc.RecuperarOperacionAPostProcesar("", principal, Nothing, "")

        oppp.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.Rechazar)
        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        gdoc.RechazarOperacion("", principal, oppp)


        ' luego termino de procesar


        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        oppp = gdoc.RecuperarOperacionAPostProcesar("", principal, Nothing, "")

        System.Diagnostics.Debug.WriteLine(oppp.RelacionENFichero.EstadosRelacionENFichero)


        If oppp Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If

        oppp.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        opppcerrada = gdoc.GuardarOperacion("", principal, oppp)

        System.Diagnostics.Debug.WriteLine(opppcerrada.RelacionENFichero.EstadosRelacionENFichero)

        If opppcerrada Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If
        If opppcerrada.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar OrElse opppcerrada.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Cerrado Then
            Throw New ApplicationException("estado incorrecto")
        End If




    End Sub

    <TestMethod()> Public Sub OperacionesPendientesTest()
        CrearElEntorno()

        Dim principal As PrincipalDN
        principal = RecuperarPrincipal()


        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin2.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin3.txt", "-1", "-1", "2", "correo")

        Dim ten As AmvDocumentosDN.TipoEntNegoioDN
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        ten = gi.Recuperar(Of AmvDocumentosDN.TipoEntNegoioDN)("1")

        Dim gdoc As GDocEntrantesFS.EntradaDocsFS
        Dim op, op2, oppp, opppcerrada As AmvDocumentosDN.OperacionEnRelacionENFicheroDN


        ' para el primer archivo lo recupero y lo dejo pendiente luego al volver a recuperar se debiera recuperar otro

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso) ' RRRRRRRRRRRRRRRRRRRRRRr
        op = gdoc.RecuperarOperacionAProcesar("", principal, Nothing)
        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If




        ' para el SEGUNDO archivo


        ' clasifico

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op2 = gdoc.RecuperarOperacionAProcesar("", principal, Nothing) ' RRRRRRRRRRRRRRRRRRRRRRr
        If op2 Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If

        If op2.GUID = op.GUID OrElse op2.RelacionENFichero.GUID = op.RelacionENFichero.GUID Then
            Throw New ApplicationException(" no se debia haber recuperado la misma operacion ni entidad negocio")
        End If




    End Sub

    <TestMethod()> Public Sub ProcesarDosDocAnulandoSegundoExcepcion()
        CrearElEntorno()

        Dim principal As PrincipalDN
        principal = RecuperarPrincipal()


        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin2.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin3.txt", "-1", "-1", "2", "correo")

        Dim ten As AmvDocumentosDN.TipoEntNegoioDN
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        ten = gi.Recuperar(Of AmvDocumentosDN.TipoEntNegoioDN)("1")

        Dim gdoc As GDocEntrantesFS.EntradaDocsFS
        Dim op, oppp, opppcerrada As AmvDocumentosDN.OperacionEnRelacionENFicheroDN


        ' para el primer archivo

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso) ' RRRRRRRRRRRRRRRRRRRRRRr
        op = gdoc.RecuperarOperacionAProcesar("", principal, Nothing)
        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If
        'op.RelacionENFichero.EntidadNegocio.TipoEntNegocioReferidora = ten
        'op.RelacionENFichero.EntidadNegocio.IdEntNeg = "asdfasfrvgsef"

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        gdoc.ClasificarOperacion("", principal, op, Nothing) ' ccccccccccccccccccccccccccccccccccc

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)
        If op.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.Clasificar OrElse op.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Clasificando Then
            Throw New ApplicationException("estado incorrecto")
        End If



        'jjjjjjjjjjjjjjjjjjj


        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        oppp = gdoc.RecuperarOperacionAPostProcesar("", principal, Nothing, "") ' RRRRRRRRRRRRRRRRRRRRRRr

        System.Diagnostics.Debug.WriteLine(oppp.RelacionENFichero.EstadosRelacionENFichero)


        If oppp Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If

        oppp.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        opppcerrada = gdoc.GuardarOperacion("", principal, oppp) ' Post Procesar 

        System.Diagnostics.Debug.WriteLine(opppcerrada.RelacionENFichero.EstadosRelacionENFichero)

        If opppcerrada Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If
        If opppcerrada.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar OrElse opppcerrada.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Cerrado Then
            Throw New ApplicationException("estado incorrecto")
        End If


        ' para el SEGUNDO archivo


        ' clasifico

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAProcesar("", principal, Nothing) ' RRRRRRRRRRRRRRRRRRRRRRr
        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If
        'op.RelacionENFichero.EntidadNegocio.TipoEntNegocioReferidora = ten
        'op.RelacionENFichero.EntidadNegocio.IdEntNeg = "asdfasfrvgsef"

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        gdoc.ClasificarOperacion("", principal, op, Nothing) ' ccccccccccccccccccccccccccccccccccc

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)
        If op.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.Clasificar OrElse op.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Clasificando Then
            Throw New ApplicationException("estado incorrecto")
        End If


        '******************************
        ' anulo

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        oppp = gdoc.RecuperarOperacionAPostProcesar("", principal, Nothing, "") ' RRRRRRRRRRRRRRRRRRRRRRr

        oppp.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.Anular)
        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        gdoc.AnularOperacion("", principal, oppp) ' ccccccccccccccccccccccccccccccccccc






        ' luego termino de procesar


        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        oppp = gdoc.RecuperarOperacionAPostProcesar("", principal, Nothing, "")

        '  System.Diagnostics.Debug.WriteLine(oppp.RelacionENFichero.EstadosRelacionENFichero)


        If Not oppp Is Nothing Then
            Throw New ApplicationException("no se debia haber recuperado ninguna operacion")
        End If


        ' recuperar a posta una operacion anaulada e intentar cerrarla

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        oppp = gdoc.RecuperarOperacion("", principal, "4")



        oppp.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        Try
            opppcerrada = gdoc.GuardarOperacion("", principal, oppp)
            Throw New ApplicationException("no se obtubo la excepcion esperada")
        Catch ex As Exception
            If ex.Message = "la relacion está cerrada" Then

            Else
                Throw
            End If
        End Try





    End Sub


    <TestMethod()> Public Sub FijarEstadoRelacionesTest()


        Dim opRelRechazada, opRelCreada, OpRelcerrada As AmvDocumentosDN.OperacionEnRelacionENFicheroDN


        CrearElEntorno()

        Dim principal As PrincipalDN
        principal = RecuperarPrincipal()


        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin2.txt", "-1", "-1", "1", "Fax")
        AltaDoc("D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Ficherin3.txt", "-1", "-1", "2", "correo")

        Dim ten As AmvDocumentosDN.TipoEntNegoioDN
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Me.mRecurso)
        ten = gi.Recuperar(Of AmvDocumentosDN.TipoEntNegoioDN)("1")

        Dim gdoc As GDocEntrantesFS.EntradaDocsFS
        Dim op, oppp, opppcerrada As AmvDocumentosDN.OperacionEnRelacionENFicheroDN


        ' para el primer archivo

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAProcesar("", principal, Nothing)
        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If
        'op.RelacionENFichero.EntidadNegocio.TipoEntNegocioReferidora = ten
        'op.RelacionENFichero.EntidadNegocio.IdEntNeg = "asdfasfrvgsef"

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        gdoc.ClasificarOperacion("", principal, op, Nothing)

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)
        If op.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.Clasificar OrElse op.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Clasificando Then
            Throw New ApplicationException("estado incorrecto")
        End If



        'jjjjjjjjjjjjjjjjjjj


        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        oppp = gdoc.RecuperarOperacionAPostProcesar("", principal, Nothing, "")

        System.Diagnostics.Debug.WriteLine(oppp.RelacionENFichero.EstadosRelacionENFichero)


        If oppp Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If

        oppp.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        opppcerrada = gdoc.GuardarOperacion("", principal, oppp)

        System.Diagnostics.Debug.WriteLine(opppcerrada.RelacionENFichero.EstadosRelacionENFichero)

        If opppcerrada Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If
        If opppcerrada.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar OrElse opppcerrada.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Cerrado Then
            Throw New ApplicationException("estado incorrecto")
        End If


        OpRelcerrada = opppcerrada



        ' para el SEGUNDO archivo


        ' clasifico

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        op = gdoc.RecuperarOperacionAProcesar("", principal, Nothing)
        If op Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion a procesar ")
        End If
        'op.RelacionENFichero.EntidadNegocio.TipoEntNegocioReferidora = ten
        'op.RelacionENFichero.EntidadNegocio.IdEntNeg = "asdfasfrvgsef"

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        gdoc.ClasificarOperacion("", principal, op, Nothing)

        System.Diagnostics.Debug.WriteLine(op.RelacionENFichero.EstadosRelacionENFichero)
        If op.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.Clasificar OrElse op.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Clasificando Then
            Throw New ApplicationException("estado incorrecto")
        End If


        '******************************
        ' rechazo

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        oppp = gdoc.RecuperarOperacionAPostProcesar("", principal, Nothing, "")

        oppp.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.Rechazar)
        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        gdoc.RechazarOperacion("", principal, oppp)
        opRelRechazada = oppp


        ' luego termino de procesar la que deje pendiente
        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        oppp = gdoc.RecuperarOperacionAPostProcesar("", principal, Nothing, "")

        System.Diagnostics.Debug.WriteLine(oppp.RelacionENFichero.EstadosRelacionENFichero)


        If oppp Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If

        oppp.TipoOperacionREnF = New AmvDocumentosDN.TipoOperacionREnFDN(AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar)

        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        opppcerrada = gdoc.GuardarOperacion("", principal, oppp)

        System.Diagnostics.Debug.WriteLine(opppcerrada.RelacionENFichero.EstadosRelacionENFichero)

        If opppcerrada Is Nothing Then
            Throw New ApplicationException("no se recupero una operacion apost procesar")
        End If
        If opppcerrada.TipoOperacionREnF.Valor <> AmvDocumentosDN.TipoOperacionREnF.ClasificarYCerrar OrElse opppcerrada.RelacionENFichero.EstadosRelacionENFichero.Valor <> AmvDocumentosDN.EstadosRelacionENFichero.Cerrado Then
            Throw New ApplicationException("estado incorrecto")
        End If


        ' fijo el estado de la oepracion rechazada a creada


        Dim colCop As AmvDocumentosDN.ColComandoOperacionDN
        Dim cop As AmvDocumentosDN.ComandoOperacionDN
        colCop = New AmvDocumentosDN.ColComandoOperacionDN

        cop = New AmvDocumentosDN.ComandoOperacionDN


        cop.IDRelacion = opRelRechazada.RelacionENFichero.ID
        cop.EstadoSolicitado = AmvDocumentosDN.EstadosRelacionENFichero.Cerrado
        colCop.Add(cop)


        gdoc = New GDocEntrantesFS.EntradaDocsFS(Nothing, Me.mRecurso)
        gdoc.ProcesarColComandoOperacion("", principal, colCop)

        If Not colCop.Item(0).Resultado Then
            Throw New ApplicationExceptionDN(colCop.Item(0).Mensaje)
        End If


    End Sub




    Private Function RecuperarPrincipal() As PrincipalDN





        Dim principal As PrincipalDN
        Dim usrln As UsuariosLN
        usrln = New UsuariosLN(Nothing, Me.mRecurso)

        principal = usrln.ObtenerPrincipal(New DatosIdentidadDN("oen", "oen"))

        Return principal



    End Function


End Class

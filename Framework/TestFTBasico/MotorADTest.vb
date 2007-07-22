Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Framework.LogicaNegocios.Transacciones


Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.DatosNegocio
Imports Framework
Imports Framework.TiposYReflexion.DN
Imports System.Collections




<TestClass()> Public Class MotorADTest

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




    <TestMethod()> Public Sub PruebaTransaccionesAnidadas()

        CrearElEntorno()
        Using New CajonHiloLN(mRecurso)
            PruebaTransaccionesAnidadasp()
        End Using


    End Sub


    Public Sub PruebaTransaccionesAnidadaspp(ByVal a As Int16)
        Using tr1 As New Transaccion(True)

            Try
                Dim tep As TipoEntidadPruebaDN
                tep = New TipoEntidadPruebaDN
                tep.Nombre = "A " & Now.Ticks


                If a = 3 OrElse a = 2 Then
                    Throw New ApplicationException
                End If

                Dim gi As New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                gi.Guardar(tep)
                tr1.Confirmar()
            Catch ex As Exception
                tr1.Cancelar()
            End Try

        End Using
    End Sub


    Public Sub PruebaTransaccionesAnidadasp()

        Using tr As New Transaccion

            For a As Int16 = 0 To 5
                PruebaTransaccionesAnidadaspp(a)

            Next

            tr.Confirmar()

        End Using


    End Sub


    <TestMethod()> Public Sub PruebaTransascciones()
        CrearElRecurso("")
        Using New CajonHiloLN(mRecurso)


            MetodoPrivado()


        End Using



    End Sub


    <TestMethod()> Public Sub PruebaRefCircualrCreacionEnConstructor()


        CrearElEntorno()

        Using New CajonHiloLN(mRecurso)




            Using tr As New Transaccion

                Dim mensaje As String

                Dim e1 As EntidadRefCircular1
                e1 = New EntidadRefCircular1
                e1.Nombre = "ent1"
                e1.entidad2.Nombre = "ent2 de ent1"

                System.Diagnostics.Debug.WriteLine(e1.EstadoIntegridad(mensaje))
                System.Diagnostics.Debug.WriteLine(mensaje)

                Dim gi As New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                gi.Guardar(e1)


                System.Diagnostics.Debug.WriteLine(e1.EstadoIntegridad(mensaje))
                System.Diagnostics.Debug.WriteLine(mensaje)

                Dim edn As Framework.DatosNegocio.EntidadDN

                gi = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                edn = gi.Recuperar("1", GetType(EntidadRefCircular1))

                System.Diagnostics.Debug.WriteLine(edn.EstadoIntegridad(mensaje))
                System.Diagnostics.Debug.WriteLine(mensaje)

                gi = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                edn = gi.Recuperar("1", GetType(EntidadRefCircular2))



                System.Diagnostics.Debug.WriteLine(edn.EstadoIntegridad(mensaje))
                System.Diagnostics.Debug.WriteLine(mensaje)





                tr.Confirmar()

            End Using








        End Using



    End Sub

    <TestMethod()> Public Sub PruebaUnicoEnBaseDeDatos()


        CrearElEntorno()

        Using New CajonHiloLN(mRecurso)


            PruebaUnicoEnBaseDeDatosp()

  



        End Using



    End Sub


    <TestMethod()> Public Sub PruebaEventosWithEvents()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)




            Using tr As New Transaccion


                Dim persona As New Persona
                persona.cabeza = New cabeza

                persona.cabeza.abrircerrarOjo()

                If Not persona.parpadeos = 1 Then
                    Throw New ApplicationException
                End If

                Me.GuardarDatos(persona)



                Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                Dim perosnabd As Persona = gi.Recuperar(Of Persona)(persona.ID)



                perosnabd.cabeza.abrircerrarOjo()

                If Not perosnabd.parpadeos = 2 Then
                    Throw New ApplicationException
                End If





                tr.Confirmar()

            End Using






        End Using







    End Sub

    Public Sub MetodoPrivado()


        Using tr As New Framework.LogicaNegocios.Transacciones.Transaccion


            ' crear unos pagos de pruebas con sus origenes
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            ' crear el origen debido
            Dim teo As New TipoEntidadPruebaDN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)

            teo.Nombre = "hola" & Now.ToString
            gi.Guardar(teo)




            MetodoPepino()
            Throw New ApplicationException

            tr.Confirmar()
            'tr.Cancelar()

        End Using


    End Sub

    Public Sub MetodoPepino()

        Using tr As New Framework.LogicaNegocios.Transacciones.Transaccion

            'Throw New ApplicationException
            tr.Confirmar()
            ' tr.Cancelar()
            'Beep()
        End Using


    End Sub




    Private Sub PruebaUnicoEnBaseDeDatosp()


        Dim tep As TipoEntidadPruebaDN


        Using tr As New Transaccion



            tep = New TipoEntidadPruebaDN
            tep.Nombre = "A"
            Me.GuardarDatos(tep)

            tep = New TipoEntidadPruebaDN
            tep.Nombre = "B"
            Me.GuardarDatos(tep)


            tr.Confirmar()



        End Using



        Using tr1 As New Transaccion(True)

            Dim excepcion As Exception

            Try
                tep = New TipoEntidadPruebaDN
                tep.Nombre = "A"
                Me.GuardarDatos(tep)
                tr1.Confirmar()
            Catch ex As Exception
                tr1.Cancelar()
                excepcion = ex
            End Try

            If excepcion Is Nothing Then
                Throw New ApplicationException("se deberia haber producido una excepcion")


            End If




        End Using


    End Sub


    <TestMethod()> Public Sub CrearElEntorno()



        CrearElRecurso("")

        Dim gbd As New gbd(Me.mRecurso)

        gbd.EliminarVistas()

        gbd.EliminarTablas()
        gbd.CrearTablas()
        gbd.CrearVistas()


    End Sub

    Private Sub CrearElRecurso(ByVal connectionstring As String)
        Dim htd As New System.Collections.Generic.Dictionary(Of String, Object)

        If connectionstring Is Nothing OrElse connectionstring = "" Then
            connectionstring = "server=localhost;database=ssPruebasFT;user=sa;pwd='sa'"
        End If

        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a AMV", "sqls", htd)


        'Asignamos el mapeado de  gestor de instanciación
        'Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaCamposMotorADTest
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaCamposPruebasMotorTest


    End Sub





    <TestMethod()> Public Sub PruebaInsertarColMuchosElemtos()

        CrearElEntorno()

        Using New CajonHiloLN(mRecurso)

            PruebaInsertarColMuchosElemtosp()

        End Using







    End Sub


    <TestMethod()> Public Sub PruebaGuardarCol()

        CrearElEntorno()

        Using New CajonHiloLN(mRecurso)

            PruebaGuardarColp()

        End Using







    End Sub

    Private Sub PruebaGuardarColp()


        Using tr As New Transaccion



            Dim mucho As New MuchosEntidadpDN
            Dim muchoRecu As MuchosEntidadpDN


            For a As Int16 = 0 To 5
                Dim ep As EntidadpDN = New EntidadpDN
                ep.Nombre = a.ToString
                mucho.ColMuchasEntidadp.Add(ep)

            Next

            GuardarDatos(mucho)

            mucho.ColMuchasEntidadp.Add(mucho.ColMuchasEntidadp.Item(1))
            GuardarDatos(mucho)



            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            muchoRecu = gi.Recuperar(mucho.ID, mucho.GetType)

            If muchoRecu Is Nothing OrElse muchoRecu.ColMuchasEntidadp.Count <> mucho.ColMuchasEntidadp.Count Then
                Throw New ApplicationException("test no pasado")
            End If

            tr.Confirmar()

        End Using



    End Sub

    Private Sub PruebaInsertarColMuchosElemtosp()


        Using tr As New Transaccion



            Dim mucho, muchoRecu As New MuchosEntidadpDN


            For a As Int16 = 0 To 30000
                Dim ep As EntidadpDN = New EntidadpDN
                ep.Nombre = "a"
                mucho.ColMuchasEntidadp.Add(ep)

            Next


            GuardarDatos(mucho)


            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            muchoRecu = gi.Recuperar(mucho.ID, mucho.GetType)

            If muchoRecu Is Nothing OrElse muchoRecu.ColMuchasEntidadp.Count <> mucho.ColMuchasEntidadp.Count Then
                Throw New ApplicationException("test no pasado")
            End If

            tr.Confirmar()

        End Using



    End Sub
    Private Sub GuardarDatos(ByVal objeto As Object)
        Using tr As New Transaccion
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(objeto)

            tr.Confirmar()

        End Using
    End Sub


    <TestMethod()> Public Sub PruebaHuellaNoTipada()

        CrearElEntorno()

        Using New CajonHiloLN(mRecurso)

            PruebaHuellaNoTipadap()

        End Using







    End Sub

    <TestMethod()> Public Sub PruebaRecuperacionReversa()

        CrearElEntorno()

        Using New CajonHiloLN(mRecurso)





            Using tr As New Transaccion

                Dim ep As EntidadpDN = PruebaHuellaTipadap()

                ' recuperar padre dada una entidad referida

                Dim gi As New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)

                Dim pdi As New Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN(ep, "HuellaEntidadp")
                Dim il As IList = gi.RecuperarColHuellasRelInversa(pdi, ep.GetType)


                gi = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                Dim entidad As EntidadDN = gi.Recuperar(CType(il(0), HEDN))






                tr.Confirmar()

            End Using










        End Using







    End Sub





    <TestMethod()> Public Sub PruebaHuellaTipada()

        CrearElEntorno()

        Using New CajonHiloLN(mRecurso)

            PruebaHuellaTipadap()

        End Using







    End Sub
    <TestMethod()> Public Sub PruebaRecargarHuellaSIGUIDNoID()


        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            PruebaRecargarHuellaSIGUIDNoIDp()

        End Using







    End Sub

    Private Sub PruebaRecargarHuellaSIGUIDNoIDp()




        Using tr As New Transaccion

            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


            Dim entidad As New EntidadpDN
            entidad.Nombre = "viva la pepa"

            Dim he As New Framework.DatosNegocio.HEDN()
            he.AsignarEntidad(entidad)
            he.EliminarEntidadReferida()
            System.Diagnostics.Debug.WriteLine(he.IdEntidadReferida) ' le queitamos el id luego obleigamos a que la recuperacion se relaice por el guid

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(entidad)


            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Recuperar(he)

            If Not entidad.GUID = he.EntidadReferida.GUID Then
                Throw New ApplicationException
            End If

            System.Diagnostics.Debug.WriteLine(entidad.ID)

            tr.Confirmar()

        End Using



    End Sub



    Private Sub PruebaHuellaNoTipadap()


        Using tr As New Transaccion


            Dim elementos As Int16 = 5

            Dim col As New ColEntidadpDN

            Dim ep As EntidadpDN
            For a As Int16 = 0 To elementos
                ep = New EntidadpDN
                ep.Nombre = "N:" & a.ToString
                ep.Valor = a
                col.Add(ep)
                GuardarDatos(ep)
            Next


            ep = col.Item(Math.Round(elementos / 2, 0))

            Dim ContenedoraHuella As New ContenedoraHuellaDN
            Dim HuellaEntidadp As New HuellaEntidadpDN
            HuellaEntidadp.EntidadReferida = ep
            ContenedoraHuella.HuellaEntidadp = HuellaEntidadp



            If ContenedoraHuella.HuellaEntidadp.Valor <> ep.Valor Then
                Throw New ApplicationException
            End If

            ep.Valor = 12

            If ContenedoraHuella.HuellaEntidadp.Valor <> ep.Valor Then
                Throw New ApplicationException
            End If

            GuardarDatos(ContenedoraHuella)

            If ContenedoraHuella.HuellaEntidadp.Valor <> ep.Valor Then
                Throw New ApplicationException
            End If

            ' verificacion de los resultados
            Dim valorOriginal As Double = ep.Valor

            Dim huellaRecuperda As HuellaEntidadpDN
            Dim al As ArrayList
            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()


            al = New ArrayList
            al.AddRange(bgln.RecuperarLista(GetType(HuellaEntidadpDN)))
            If al.Count <> 1 Then
                Throw New ApplicationException
            End If

            huellaRecuperda = al(0)
            If huellaRecuperda.Valor <> valorOriginal Then
                Throw New ApplicationException
            End If




            ep.Valor = 34

            If ContenedoraHuella.Estado <> EstadoDatosDN.Modificado Then
                Throw New ApplicationException("tras gaurdar la entidad contennedora de huella no debiera verse afectada por las modificaciones en la entidad")
            End If




            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            ' tras recuperar la entidad contenedora no debe tener la entidad referida por la huella

            ContenedoraHuella = bgln.RecuperarLista(GetType(ContenedoraHuellaDN))(0)
            If Not ContenedoraHuella.HuellaEntidadp.EntidadReferida Is Nothing Then
                Throw New ApplicationException
            End If


            ' como la entidad ya no esta registrada sus modificaciones no debieran causar eventos

            ep.Valor = 77

            If ContenedoraHuella.Estado <> EstadoDatosDN.SinModificar Then
                Throw New ApplicationException("tras gaurdar la entidad contennedora de huella no debiera verse afectada por las modificaciones en la entidad")
            End If

            tr.Confirmar()

        End Using



    End Sub

    Private Function PruebaHuellaTipadap() As EntidadpDN


        Using tr As New Transaccion


            Dim elementos As Int16 = 5

            Dim col As New ColEntidadpDN

            Dim ep As EntidadpDN
            For a As Int16 = 0 To elementos
                ep = New EntidadpDN
                ep.Nombre = "N:" & a.ToString
                ep.Valor = a
                col.Add(ep)
                GuardarDatos(ep)
            Next


            ep = col.Item(Math.Round(elementos / 2, 0))

            Dim ContenedoraHuella As New ContenedoraHtEntidadpDN
            Dim HuellaEntidadp As New HtEntidadpDN
            HuellaEntidadp.EntidadReferida = ep
            ContenedoraHuella.HuellaEntidadp = HuellaEntidadp

            'GuardarDatos(ContenedoraHuella)

            If ContenedoraHuella.HuellaEntidadp.Valor <> ep.Valor Then
                Throw New ApplicationException
            End If

            ep.Valor = 12
            ContenedoraHuella.HuellaEntidadp.Refrescar()
            If ContenedoraHuella.HuellaEntidadp.Valor <> ep.Valor Then
                Throw New ApplicationException
            End If

            GuardarDatos(ContenedoraHuella)

            If ContenedoraHuella.HuellaEntidadp.Valor <> ep.Valor Then
                Throw New ApplicationException
            End If

            ' verificacion de los resultados
            Dim valorOriginal As Double = ep.Valor

            Dim huellaRecuperda As HtEntidadpDN
            Dim al As ArrayList
            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()

            'rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr
            al = New ArrayList
            al.AddRange(bgln.RecuperarLista(GetType(HtEntidadpDN)))
            If al.Count <> 1 Then
                Throw New ApplicationException
            End If

            huellaRecuperda = al(0)
            If huellaRecuperda.Valor <> valorOriginal Then
                Throw New ApplicationException
            End If




            ep.Valor = 34

            If ContenedoraHuella.Estado <> EstadoDatosDN.Modificado Then
                Throw New ApplicationException("tras gaurdar la entidad contennedora de huella no debiera verse afectada por las modificaciones en la entidad")
            End If




            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            ' tras recuperar la entidad contenedora no debe tener la entidad referida por la huella

            ContenedoraHuella = bgln.RecuperarLista(GetType(ContenedoraHtEntidadpDN))(0)
            If Not ContenedoraHuella.HuellaEntidadp.EntidadReferida Is Nothing Then
                Throw New ApplicationException
            End If


            ' como la entidad ya no esta registrada sus modificaciones no debieran causar eventos

            ep.Valor = 77

            If ContenedoraHuella.Estado <> EstadoDatosDN.SinModificar Then
                Throw New ApplicationException("tras gaurdar la entidad contennedora de huella no debiera verse afectada por las modificaciones en la entidad")
            End If




            ' GUARDAR UNA HUELLA CONTRA UNA ENTIDAD NO GUARDADA EN EL SISTEMA 


            ep = New EntidadpDN
            ep.Nombre = "N: NO GUARDADA ENTIDAD REFERIDA"
            ep.Valor = -1

            ContenedoraHuella = New ContenedoraHtEntidadpDN
            HuellaEntidadp = New HtEntidadpDN
            HuellaEntidadp.EntidadReferida = ep
            ContenedoraHuella.HuellaEntidadp = HuellaEntidadp

            GuardarDatos(ContenedoraHuella)

            tr.Confirmar()
            Return ep
        End Using



    End Function


End Class






Public Class GestorMapPersistenciaCamposMotorADTest
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


        If (pTipo Is GetType(TipoEntidadPruebaDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNombre"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


            Return mapinst
        End If



        Return Nothing
    End Function


    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class






Public Class EntidadRefCircular1
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mentidad2 As EntidadRefCircular2

    Public Sub New()
        Me.CambiarValorRef(Of EntidadRefCircular2)(New EntidadRefCircular2, Me.mentidad2)
        Me.mentidad2.entidad1 = Me
    End Sub


    <RelacionPropCampoAtribute("mentidad2")> _
    Public Property entidad2() As EntidadRefCircular2

        Get
            Return mentidad2
        End Get

        Set(ByVal value As EntidadRefCircular2)
            CambiarValorRef(Of EntidadRefCircular2)(value, mentidad2)
            If Not Me.mentidad2.entidad1 Is Me Then
                Me.mentidad2.entidad1 = Me
            End If
        End Set
    End Property

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If Not Me.mentidad2.entidad1 Is Me Then
            Throw New ApplicationException
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function


End Class
Public Class EntidadRefCircular2
    Inherits Framework.DatosNegocio.EntidadDN



    Protected mentidad1 As EntidadRefCircular1

    <RelacionPropCampoAtribute("mentidad1")> _
    Public Property entidad1() As EntidadRefCircular1

        Get
            Return mentidad1
        End Get

        Set(ByVal value As EntidadRefCircular1)
            CambiarValorRef(Of EntidadRefCircular1)(value, mentidad1)
            If Not Me.mentidad1.entidad2 Is Me Then
                Me.mentidad1.entidad2 = Me
            End If
        End Set
    End Property

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If Not Me.mentidad1.entidad2 Is Me Then
            Throw New ApplicationException
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function



End Class


Public Class colPersona
    Inherits Framework.DatosNegocio.ArrayListValidable(Of Persona)

End Class
Public Class Persona
    Inherits Framework.DatosNegocio.EntidadDN
    Protected WithEvents mcabeza As cabeza

    Protected mparpadeos As Integer

    Public Property parpadeos() As Integer
        Get
            Return mparpadeos
        End Get
        Set(ByVal value As Integer)
            mparpadeos = value
        End Set
    End Property

    <RelacionPropCampoAtribute("mcabeza")> Public Property cabeza() As cabeza
        Get
            Return mcabeza
        End Get
        Set(ByVal value As cabeza)
            Me.CambiarValorRef(Of cabeza)(value, mcabeza)
        End Set
    End Property

    Private Sub mcabeza_abrirojo() Handles mcabeza.abrirojo
        Beep()
        mparpadeos += 1
    End Sub

    Private Sub mcabeza_cerrarojo() Handles mcabeza.cerrarojo
        Beep()
        mparpadeos += 1
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


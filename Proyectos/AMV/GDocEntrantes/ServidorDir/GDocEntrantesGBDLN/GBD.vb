Imports Framework.DatosNegocio
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Procesos.ProcesosLN
Imports Framework.TiposYReflexion.DN
Imports Framework.Usuarios.DN
Imports Framework.Usuarios.LN
Imports System.IO

Imports System.Xml




Public Class GBD

    Private mrecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN

#Region "Constructor"

    Public Sub New(ByVal pRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN)


        If pRecurso Is Nothing Then
            Dim connectionstring As String
            Dim htd As New Generic.Dictionary(Of String, Object)

            connectionstring = "server=localhost;database=GestionSegurosAMV;user=sa;pwd=''"
            htd.Add("connectionstring", connectionstring)
            mrecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a AMV", "sqls", htd)

        Else
            mrecurso = pRecurso
        End If


        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GDocEntrantesAD.GestorMapPersistenciaCamposAMVDocsEntrantesLN()

    End Sub

#End Region

#Region "Métodos"

    Public Sub pruebaHuella()
        Dim miContenedoraH, miContenedoraHrec As ContenedoraH
        miContenedoraH = New ContenedoraH
        Dim mipato As New Pato
        mipato.Nombre = "lucas"

        miContenedoraH.Huella = New Framework.DatosNegocio.HEDN(mipato, Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional.ninguna, mipato.Nombre)

        Me.GuardarDatos(miContenedoraH)
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        miContenedoraHrec = gi.Recuperar(Of ContenedoraH)(miContenedoraH.ID)
        Debug.WriteLine(miContenedoraHrec.Huella.GUIDReferida)

    End Sub

    Public Sub EliminarTablas()

        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim ds As Data.DataSet


        Dim dr As Data.DataRow
        Dim nombretabla As String
        Dim eliminables As Int16
        Dim vueltas As Int16

        Dim sqlElim As String
        EliminarRelaciones()

        Do
            vueltas += 1
            eliminables = 0

            ej = New Framework.AccesoDatos.Ejecutor(Nothing, mrecurso)
            ds = ej.EjecutarDataSet("SELECT name FROM sysobjects WHERE xtype = 'U'")

            For Each dr In ds.Tables(0).Rows
                nombretabla = dr("name")
                If nombretabla.Substring(0, 2) = "tl" OrElse nombretabla.Substring(0, 2) = "tr" Then
                    eliminables += 1
                    ej = New Framework.AccesoDatos.Ejecutor(Nothing, mrecurso)
                    Try
                        sqlElim = "Drop Table " & nombretabla
                        ej.EjecutarNoConsulta(sqlElim)

                    Catch ex As Exception

                    End Try
                End If
            Next

        Loop Until eliminables = 0 OrElse vueltas > 20


    End Sub

    Public Sub EliminarRelaciones()

        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim ds, dtsTabla As Data.DataSet


        Dim dr As Data.DataRow
        Dim nombretabla, idTablaPadre, NombreRelacion As String
        Dim eliminables As Int16
        Dim vueltas As Int16

        Dim sqlElim As String

        Do
            vueltas += 1
            eliminables = 0

            ej = New Framework.AccesoDatos.Ejecutor(Nothing, mrecurso)
            ds = ej.EjecutarDataSet("SELECT * FROM sysobjects WHERE xtype = 'F'") ' recupero todas las relaciones externas FK

            For Each dr In ds.Tables(0).Rows
                NombreRelacion = dr("name")
                idTablaPadre = dr("parent_obj")
                ej = New Framework.AccesoDatos.Ejecutor(Nothing, mrecurso)
                dtsTabla = ej.EjecutarDataSet("SELECT * FROM sysobjects WHERE id='" & idTablaPadre & "'") ' recupero todas las relaciones externas FK
                nombretabla = dtsTabla.Tables(0).Rows(0)("name")

                If nombretabla.Substring(0, 2) = "tl" OrElse nombretabla.Substring(0, 2) = "tr" Then
                    eliminables += 1
                    ej = New Framework.AccesoDatos.Ejecutor(Nothing, mrecurso)
                    Try
                        sqlElim = "ALTER TABLE " & nombretabla & " DROP CONSTRAINT  " & NombreRelacion
                        Debug.WriteLine(sqlElim)
                        ej.EjecutarNoConsulta(sqlElim)
                        Debug.WriteLine("OK")
                    Catch ex As Exception
                        Debug.WriteLine("FALLO")
                    End Try
                End If
            Next

        Loop Until eliminables = 0 OrElse vueltas > 20


    End Sub

    Public Sub CrearTablasHuella()
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        ' gi.GenerarTablas2(GetType(Framework.DatosNegocio.HuellaEntidadDN), Nothing)

        gi.GenerarTablas2(GetType(GDocEntrantesGBDLN.ContenedoraH), Nothing)
        gi.GenerarTablas2(GetType(GDocEntrantesGBDLN.Pato), Nothing)

    End Sub

    Public Sub CrearTablas()
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        ' procesos

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(Framework.Usuarios.DN.AutorizacionRelacionalDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(FN.Personas.DN.PersonaDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(Framework.Procesos.ProcesosDN.TransicionRealizadaDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(AmvDocumentosDN.OperacionEnRelacionENFicheroDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(AmvDocumentosDN.OperadorDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(AmvDocumentosDN.HuellaOperadorDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(AmvDocumentosDN.CanalEntradaDocsDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(AmvDocumentosDN.RelacionENFicheroDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(AmvDocumentosDN.DatosFicheroIncidentado), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(PrincipalDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(DatosIdentidadDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(Framework.Mensajeria.GestorMensajeriaDN.SobreBasicoDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(Framework.Mensajeria.GestorMensajeriaDN.NotificacionDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(Framework.Mensajeria.GestorMensajeriaDN.SuscripcionDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(Framework.Mensajeria.GestorMails.DN.SobreDN), Nothing)

        'Tablas Gestión de pagos
        '_________________________________________________________________________________


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN), Nothing)



        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.ApunteImpDDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN), Nothing)

        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        'gi.GenerarTablas2(GetType(FN.GestionPagos.DN.OrigenIdevManualDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.LiquidacionPagoDN), Nothing)




        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.PagoDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.NotificacionPagoDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.LimitePagoDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.ContenedorRTFDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.ConfiguracionImpresionTalonDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.ReemplazosTextoCartasDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.PagoTrazaDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(FN.GestionPagos.DN.FicheroTransferenciaDN), Nothing)

        '_________________________________________________________________________________

        'Tablas EmpresaDN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(FN.Empresas.DN.EmpleadoYPuestosRDN), Nothing)


        '_________________________________________________________________________________


    End Sub

    Public Sub CargarDatosBasicos(ByVal crearArbol As Boolean)
        '''''''''''''''''''''''''''''''''''''
        ' Creo los casos de uso de  la aplicacion
        '''''''''''''''''''''''''''''''''''''





        Using tr As New Transaccion



            'recupero todos los metodos de sistema
            Dim rolat As RolDN
            If My.Settings.datosCasosUso Is Nothing OrElse My.Settings.datosCasosUso = "" Then

                Dim RolLn As Framework.Usuarios.LN.RolLN
                RolLn = New Framework.Usuarios.LN.RolLN(Transaccion.Actual, Recurso.Actual)
                '   rolat = usurios.GeneraRolAutorizacionTotal("Administrador Total")
                rolat = RolLn.GeneraRolAutorizacionTotal("Administrador Total")
                AsignarOperacionesCasoUsoTotal()
                Me.GuardarDatos(rolat)

            Else

                'Dim usuarios As UsuariosLN
                'usuarios = New UsuariosLN(Nothing, mrecurso)
                'usuarios.GenerarRolesDeInicioDeSistema(My.Settings.datosCasosUso)

                'AsignarOperacionesCasoUsoTotal()
                Dim RolLn As Framework.Usuarios.LN.RolLN
                RolLn = New Framework.Usuarios.LN.RolLN(Transaccion.Actual, Recurso.Actual)
                RolLn.GenerarRolesDeInicioDeSistema(My.Settings.datosCasosUso)
                AsignarOperacionesCasoUsoTotal()
            End If


            '''''''''''''''''''''''''''''''''''''
            'Creo los datos de tipos
            ''''''''''''''''''''''''''''''''''''''
            Dim tipoOp As AmvDocumentosDN.TipoOperacionREnFDN
            tipoOp = New AmvDocumentosDN.TipoOperacionREnFDN
            Me.InsertarTiposDatos(tipoOp.RecuperarTiposTodos)


            Dim estadoRENF As AmvDocumentosDN.EstadosRelacionENFicheroDN
            estadoRENF = New AmvDocumentosDN.EstadosRelacionENFicheroDN
            Me.InsertarTiposDatos(estadoRENF.RecuperarTiposTodos)


            '''''''''''''''''''''''''''''''''''''
            'Creo el arbol vacio
            ''''''''''''''''''''''''''''''''''''''

            If crearArbol Then



                Dim nodo As AmvDocumentosDN.NodoTipoEntNegoioDN

                nodo = New AmvDocumentosDN.NodoTipoEntNegoioDN
                nodo.Nombre = "Entidades Referidoras"

                Dim cabeceraNodo As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
                cabeceraNodo = New AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
                cabeceraNodo.NodoTipoEntNegoio = nodo

                Me.GuardarDatos(cabeceraNodo)

            End If


            '''''''''''''''''''''''''''''''''''''
            'Creo los verbos del sistema
            ''''''''''''''''''''''''''''''''''''''

            Dim verbo As Framework.Procesos.ProcesosDN.VerboDN

            verbo = New Framework.Procesos.ProcesosDN.VerboDN
            ' verbo.VinculoMetodo = New Framework.TiposYReflexion.DN.VinculoMetodoDN()

            'Cargar los tipos de vía
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("AVENIDA"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("BARRIO"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("CALLE"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("RONDA"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("PASAJE"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("GLORIETA"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("CALLEJÓN"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("PÓLIGONO"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("CAMINO"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("COSTANILLA"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("COLONIA"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("PLAZA"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("PARQUE"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("CARRETERA"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("PASEO"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("TRAVESIA"))
            Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("URBANIZACIÓN"))

            'Me.CargarLocalidades("C:\COMPARTIDO\provinciasypoblaciones.xml")


            tr.Confirmar()

        End Using




    End Sub

    Private Sub AsignarOperacionesCasoUsoTotal()




        Using tr As New Transaccion

            Dim casosUsoLN As New CasosUsoLN(Transaccion.Actual, Recurso.Actual)
            Dim colCasosUso As New ColCasosUsoDN()
            Dim casoUsoTotal As CasosUsoDN

            colCasosUso.AddRange(casosUsoLN.RecuperarListaCasosUso())
            casoUsoTotal = colCasosUso.RecuperarPrimeroXNombre("Todos los permisos")
            casoUsoTotal.ColOperaciones = New Framework.Procesos.ProcesosDN.ColOperacionDN
            'Using New CajonHiloLN(mrecurso)
            Dim cb As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim col As IList = cb.RecuperarLista(GetType(Framework.Procesos.ProcesosDN.OperacionDN))

            For Each op As Framework.Procesos.ProcesosDN.OperacionDN In col
                casoUsoTotal.ColOperaciones.Add(op)
            Next
            Me.GuardarDatos(casoUsoTotal)
            '  End Using


            tr.Confirmar()

        End Using






    End Sub

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

    Public Sub CrearUnUsuarioParaCadaRol(ByVal coltipoEN As AmvDocumentosDN.ColTipoEntNegoioDN)




        Using tr As New Transaccion









            Dim usuario As UsuarioDN
            Dim uln As New UsuariosLN(Nothing, mrecurso)
            Dim Principal As PrincipalDN
            Dim di As DatosIdentidadDN
            Dim colRol, colTodosRol As ColRolDN
            Dim rolesln As RolLN


            ' admin negocio
            rolesln = New RolLN(Transaccion.Actual, Recurso.Actual)
            colTodosRol = rolesln.RecuperarColRoles




            Dim rol As RolDN
            For Each rol In colTodosRol

                Dim nombreclave As String = NicYClaveRol(rol.Nombre)
                colRol = New ColRolDN
                colRol.Add(rol)
                usuario = New UsuarioDN(nombreclave, True)
                usuario.AsignarEntidad(New AmvDocumentosDN.OperadorDN(nombreclave, coltipoEN))
                '    usuario.HuellaEntidadUserDN = New AmvDocumentosDN.HuellaOperadorDN(New AmvDocumentosDN.OperadorDN(nombreclave, coltipoEN))
                usuario.HuellaEntidadUserDN = New AmvDocumentosDN.HuellaOperadorDN(New AmvDocumentosDN.OperadorDN(nombreclave, coltipoEN))

                Principal = New PrincipalDN(nombreclave, usuario, colRol)
                di = New DatosIdentidadDN(nombreclave, nombreclave)
                Me.GuardarDatos(Principal)
                Me.GuardarDatos(di)

            Next


            tr.Confirmar()

        End Using

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

    Public Sub PublicarFachada()


        Using tr As New Transaccion


            Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("GDocEntrantesFS", Me.mrecurso)
            Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("UsuariosFS", mrecurso)
            Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("FicherosFS", mrecurso)
            Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("MotorBusquedaFS", mrecurso)
            Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("ProcesosFS", mrecurso)

            tr.Confirmar()

        End Using




    End Sub

    Public Sub PublicarOperaciones()
        Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("GDocEntrantesFS", Me.mrecurso)

    End Sub

    Public Sub PublicarGrafosBasicos()
        PublicarGrafoGestionTalones()
    End Sub

    Public Sub PublicarGrafoGestionTalones()




        Using tr As New Transaccion







            ' flujo de talones

            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            ' 1º 
            ' dn o dns a las cuales se vincula el flujo
            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

            Dim vc1DN As Framework.TiposYReflexion.DN.VinculoClaseDN = RecuperarVinculoClase(GetType(FN.GestionPagos.DN.PagoDN))
            Dim ColVc As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
            ColVc.Add(vc1DN)

            'FIN ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''


            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            ' 2º 
            '  creacion de las operaciones
            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            Dim colop As New Framework.Procesos.ProcesosDN.ColOperacionDN

            ' operacion que engloba todo el flujo
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Gestión Talones", ColVc, "element_into.ico", True)))

            ' negocio
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Alta Negocio", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Guardar Negocio", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Validación Negocio", ColVc, "element_into.ico", True)))

            '' operacion de pueba

            'colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion Compuesta", ColVc)))
            'colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion SumarUno", ColVc)))
            'colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion RestarUno", ColVc)))
            'colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion SumarCinco", ColVc)))
            'colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion RestarCinco", ColVc)))

            '' FIN operacion de pueba


            'contabilidad
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Alta Contabilidad", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Guardar Contabilidad", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Validación Contabilidad", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Verificación Cobro Contabilidad", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Anulación con Alta Contabilidad", ColVc, "element_into.ico", True)))

            ' direccion
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Firma Dirección", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Firma Dirección Automatica", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Decisión Firma Automatica-Manual", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Reactivacion Anulación Impresión Dirección", ColVc, "element_into.ico", True)))

            ' impresion
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Decisión Pago Talon", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Impresión", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Validación Impresión", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Anulación Impresión", ColVc, "element_into.ico", True)))

            'Adjuntar a fichero de transferencias
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Decisión Pago Transferencia", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Adjuntar Fichero Transferencia", ColVc, "element_into.ico", True)))

            'Operaciones de rechazo y anulación del pago
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Rechazar N", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Rechazar C", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Anular N", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Anular C", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Anular D", ColVc, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Anular Pago", ColVc, "element_into.ico", True)))

            ' operaciones genericas fuera de proceso
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("GuardarDNGenerico", ColVc, "element_into.ico", True)))


            'Operaciones de los ficheros de transferencias
            Dim vcFTDN As Framework.TiposYReflexion.DN.VinculoClaseDN = RecuperarVinculoClase(GetType(FN.GestionPagos.DN.FicheroTransferenciaDN))
            Dim ColVcFT As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
            ColVcFT.Add(vcFTDN)

            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Gestión Ficheros Transferencias", ColVcFT, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Alta FT", ColVcFT, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Guardar FT", ColVcFT, "Guardar.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Generar FT", ColVcFT, "element_into.ico", True)))
            colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Regenerar FT", ColVcFT, "element_into.ico", True)))



            ''''''''''''''''''''''''''''''''''''''''''''
            ' 3º
            ' creacion de las Transiciones
            ''''''''''''''''''''''''''''''''''''''''''''
            'Dim tran As Framework.Procesos.ProcesosDN.TransicionDN

            Dim colVM As New ColVinculoMetodoDN()

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Gestión Talones", "Alta Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Inicio, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Gestión Talones", "Alta Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Inicio, False, Nothing, False))

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Negocio", "Guardar Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Negocio", "Validación Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Negocio", "Anular N", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))


            '' prueba subordiandas ''''''''''''''''''''''''''''''''''''''

            'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Negocio", "Operacion Compuesta", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, False))

            'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion Compuesta", "Operacion SumarUno", Framework.Procesos.ProcesosDN.TipoTransicionDN.Subordianda, False, True))
            'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion Compuesta", "Operacion RestarUno", Framework.Procesos.ProcesosDN.TipoTransicionDN.Subordianda, False, True))

            'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion RestarUno", "Operacion RestarCinco", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, False))
            'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion SumarUno", "Operacion SumarCinco", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, False))

            'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion RestarCinco", "Operacion Compuesta", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, False))

            'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion SumarCinco", "Operacion Compuesta", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, False))

            'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion Compuesta", "Guardar Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, False))

            '' fin prueba subordiandas''''''''''''''''''''''''''''''''''''''''''''''''


            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Contabilidad", "Guardar Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Contabilidad", "Validación Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Contabilidad", "Anular C", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar Negocio", "Guardar Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar Negocio", "Validación Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar Negocio", "Anular N", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Rechazar N", "Guardar Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Rechazar N", "Validación Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Rechazar N", "Anular N", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Negocio", "Guardar Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Negocio", "Validación Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Negocio", "Rechazar N", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Negocio", "Anular C", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar Contabilidad", "Guardar Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar Contabilidad", "Rechazar N", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar Contabilidad", "Validación Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar Contabilidad", "Anular C", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Rechazar C", "Guardar Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Rechazar C", "Validación Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Rechazar C", "Anular C", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Contabilidad", "Decisión Firma Automatica-Manual", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Decisión Firma Automatica-Manual", "Firma Dirección", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Decisión Firma Automatica-Manual", "Rechazar N", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Decisión Firma Automatica-Manual", "Rechazar C", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Decisión Firma Automatica-Manual", "Anular D", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Decisión Firma Automatica-Manual", "Firma Dirección Automatica", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, RecuperarVinculoMetodo("GuardaFirmaAutomatica", GetType(FN.GestionPagos.LN.GuardasFlujoTalonesLN)), False))

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Firma Dirección", "Decisión Pago Talon", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, RecuperarVinculoMetodo("GuardaImprimirTalon", GetType(FN.GestionPagos.LN.GuardasFlujoTalonesLN)), False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Firma Dirección", "Decisión Pago Transferencia", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, RecuperarVinculoMetodo("GuardaGenerarTransferencia", GetType(FN.GestionPagos.LN.GuardasFlujoTalonesLN)), False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Firma Dirección Automatica", "Decisión Pago Talon", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, RecuperarVinculoMetodo("GuardaImprimirTalon", GetType(FN.GestionPagos.LN.GuardasFlujoTalonesLN)), False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Firma Dirección Automatica", "Decisión Pago Transferencia", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, RecuperarVinculoMetodo("GuardaGenerarTransferencia", GetType(FN.GestionPagos.LN.GuardasFlujoTalonesLN)), False))

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Decisión Pago Talon", "Impresión", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Decisión Pago Transferencia", "Adjuntar Fichero Transferencia", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Impresión", "Validación Impresión", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Impresión", "Anulación Impresión", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anulación Impresión", "Reactivacion Anulación Impresión Dirección", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anulación Impresión", "Anular D", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Reactivacion Anulación Impresión Dirección", "Impresión", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Impresión", "Verificación Cobro Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Impresión", "Anulación con Alta Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Impresión", "Anular Pago", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anulación con Alta Contabilidad", "Guardar Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anulación con Alta Contabilidad", "Anular C", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anulación con Alta Contabilidad", "Validación Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Adjuntar Fichero Transferencia", "Verificación Cobro Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Adjuntar Fichero Transferencia", "Anulación con Alta Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Adjuntar Fichero Transferencia", "Anular Pago", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Generación Fichero Transferencia", "Regenerar Fichero Transferencia", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

            'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Regenerar FT", "Regenerar FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Regenerar FT", "Verificación Cobro Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Regenerar FT", "Anulación con Alta Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Regenerar FT", "Anular Pago", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

            '---------------------------------------------------------------------------------------------------

            ' transiciones de fin
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Verificación Cobro Contabilidad", "Gestión Talones", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))

            ' transiciones de fin por anulación
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anular N", "Gestión Talones", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anular C", "Gestión Talones", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anular D", "Gestión Talones", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anular Pago", "Gestión Talones", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))

            ' FIN flujo de talones 

            ' Flujo de ficheros de transferencias

            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Gestión Ficheros Transferencias", "Alta FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Inicio, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta FT", "Guardar FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta FT", "Generar FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar FT", "Guardar FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar FT", "Generar FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Generar FT", "Regenerar FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
            GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Regenerar FT", "Regenerar FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

            ' FIN flujo de ficheros de transferencias



            ' publicar los controladores


            ' Framework.FachadaLogica.GestorFachadaFL.PublicarMetodos("ProcesosFS", Me.mrecurso)

            'Dim opln As New Framework.Procesos.ProcesosLN.OperacionesLN
            'Dim ejc As Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN = opln.RecuperarEjecutorCliente("Servidor")


            Dim ejClienteS, ejClienteC As Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN
            Dim clienteS, clienteC As Framework.Procesos.ProcesosDN.ClientedeFachadaDN

            ' crecion de los clientes del grafo
            clienteS = New Framework.Procesos.ProcesosDN.ClientedeFachadaDN
            clienteS.Nombre = "Servidor"

            clienteC = New Framework.Procesos.ProcesosDN.ClientedeFachadaDN
            clienteC.Nombre = "Cliente1"

            ejClienteS = New Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN
            ejClienteS.ClientedeFachada = clienteS

            ejClienteC = New Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN
            ejClienteC.ClientedeFachada = clienteC


            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Gestión Talones", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Alta Negocio", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Guardar Negocio", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Validación Negocio", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Rechazar N", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular N", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))) ' en este caso debiea de enrutarse al metodo que le pone la fecha de anulación

            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Alta Contabilidad", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Guardar Contabilidad", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Validación Contabilidad", RecuperarVinculoMetodo("ValidarPagoAsignado", GetType(FN.GestionPagos.LN.PagosLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Verificación Cobro Contabilidad", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anulación con Alta Contabilidad", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Rechazar C", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular C", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))

            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Firma Dirección", RecuperarVinculoMetodo("FirmarPagoAsignado", GetType(FN.GestionPagos.LN.PagosLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Firma Dirección Automatica", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Decisión Firma Automatica-Manual", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Decisión Pago Talon", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Decisión Pago Transferencia", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Reactivacion Anulación Impresión Dirección", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular D", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))

            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Impresión", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Validación Impresión", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anulación Impresión", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular Pago", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))

            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Adjuntar Fichero Transferencia", RecuperarVinculoMetodo("AdjuntarPagoFT", GetType(FN.GestionPagos.LN.PagosLN)), ejClienteS)))

            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Alta FT", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Guardar FT", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Generar FT", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
            ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Regenerar FT", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))


            '' pruebas

            'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion Compuesta", "GuardarGenerico", GetType(GestorEjecutoresLN), ejClienteS))
            'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion SumarUno", "GuardarGenerico", GetType(GestorEjecutoresLN), ejClienteS))
            'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion RestarUno", "GuardarGenerico", GetType(GestorEjecutoresLN), ejClienteS))
            'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion RestarCinco", "GuardarGenerico", GetType(GestorEjecutoresLN), ejClienteS))
            'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion SumarCinco", "GuardarGenerico", GetType(GestorEjecutoresLN), ejClienteS))

            'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion Compuesta", "EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS), ejClienteC))
            'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion SumarUno", "SumarUno", GetType(ClienteAdminLNC.TalonesLNC), ejClienteC))
            'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion RestarUno", "RestarUno", GetType(ClienteAdminLNC.TalonesLNC), ejClienteC))
            'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion RestarCinco", "RestarCinco", GetType(ClienteAdminLNC.TalonesLNC), ejClienteC))
            'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion SumarCinco", "SumarCinco", GetType(ClienteAdminLNC.TalonesLNC), ejClienteC))

            '' finc pruebas

            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Gestión Talones", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Alta Negocio", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Guardar Negocio", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Validación Negocio", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Rechazar N", RecuperarVinculoMetodo("AdjuntarNotaaPago", GetType(FN.GestionPagos.IU.NotificacionesPagoCtrl)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular N", RecuperarVinculoMetodo("AdjuntarNotaaPago", GetType(FN.GestionPagos.IU.NotificacionesPagoCtrl)), ejClienteC)))

            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Alta Contabilidad", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Guardar Contabilidad", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Validación Contabilidad", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Verificación Cobro Contabilidad", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anulación con Alta Contabilidad", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Rechazar C", RecuperarVinculoMetodo("AdjuntarNotaaPago", GetType(FN.GestionPagos.IU.NotificacionesPagoCtrl)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular C", RecuperarVinculoMetodo("AdjuntarNotaaPago", GetType(FN.GestionPagos.IU.NotificacionesPagoCtrl)), ejClienteC)))

            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Firma Dirección", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Firma Dirección Automatica", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Decisión Firma Automatica-Manual", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Decisión Pago Talon", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Decisión Pago Transferencia", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Reactivacion Anulación Impresión Dirección", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular D", RecuperarVinculoMetodo("AdjuntarNotaaPago", GetType(FN.GestionPagos.IU.NotificacionesPagoCtrl)), ejClienteC)))

            '  ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Impresión", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Impresión", RecuperarVinculoMetodo("ImprimirUnico", GetType(FN.GestionPagos.IU.AdaptadorImpresion)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Validación Impresión", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anulación Impresión", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular Pago", RecuperarVinculoMetodo("AdjuntarNotaaPago", GetType(FN.GestionPagos.IU.NotificacionesPagoCtrl)), ejClienteC)))

            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Adjuntar Fichero Transferencia", RecuperarVinculoMetodo("AdjuntarPagoUnicoFT", GetType(FN.GestionPagos.IU.AdaptadorFicherosTransferencias)), ejClienteC)))

            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Alta FT", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Guardar FT", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Generar FT", RecuperarVinculoMetodo("GenerarFicheroTransferencia", GetType(FN.GestionPagos.LNC.PagosLNC)), ejClienteC)))
            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Regenerar FT", RecuperarVinculoMetodo("GenerarFicheroTransferencia", GetType(FN.GestionPagos.LNC.PagosLNC)), ejClienteC)))

            ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "GuardarDNGenerico", RecuperarVinculoMetodo("GuardarDNGenerico", GetType(Framework.AS.MV2AS)), ejClienteC)))

            Me.GuardarDatos(ejClienteC)
            Me.GuardarDatos(ejClienteS)

            tr.Confirmar()

        End Using

    End Sub

    Public Sub EjecutarTodo()





        Using tr As New Transaccion



            Dim gbd As New GDocEntrantesAD.GDocsEntrantesGBD(Me.mrecurso)

            gbd.EliminarVistas()
            gbd.EliminarRelaciones()
            gbd.EliminarTablas()
            gbd.CrearTablas()
            gbd.CrearVistas()




            tr.Confirmar()

        End Using










    End Sub



    Public Sub cargarDatosTODOS(ByVal pRutaFicheroLocalidades As String, ByRef colief As FN.Localizaciones.DN.ColIEntidadFiscalDN)



        Using tr As New Transaccion


            PublicarGrafosBasicos()
            Me.PublicarFachada()
            Me.CargarDatosBasicos(False)
            'Me.CargarLocalidadesCodPostal(pRutaFicheroLocalidades)
            CargarDatosAMV()
            Me.CargarDatosPruebas(colief)

            tr.Confirmar()

        End Using





    End Sub

    Public Sub EjecutarTodoBasico(ByVal pNombreRolAdminTotal As String, ByVal pId As String, ByVal pClave As String, ByVal pRutaFicheroLocalidades As String)

        Me.EliminarTablas()
        Me.CrearTablas()
        PublicarGrafosBasicos()
        Me.PublicarFachada()
        Me.CargarDatosBasicos(True)

        'Me.CargarLocalidadesCodPostal(pRutaFicheroLocalidades)

        CargarDatosAMV()
        Me.CrearUnUsuarioAdminTotal(pNombreRolAdminTotal, pId, pClave)


    End Sub

    Public Sub InsertarTiposDatos(ByVal pEntidad As IList)

        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.InsertarEntidadBase(pEntidad)
    End Sub

    Public Function GuardarDatos(ByVal pEntidad As IEntidadDN) As IEntidadDN




        Using tr As New Transaccion


            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(pEntidad)

            tr.Confirmar()
            Return pEntidad
        End Using






    End Function

    Public Sub InsertarEntidadBase(ByVal pEntidad As IEntidadBaseDN)

        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.InsertarEntidadBase(pEntidad)
    End Sub

    Private Sub CargarDatosPruebas(ByRef colief As FN.Localizaciones.DN.ColIEntidadFiscalDN)




        Using tr As New Transaccion









            Dim localidad1, localidad As Framework.DatosNegocio.IEntidadDN

            Dim calle As FN.Localizaciones.DN.TipoViaDN = Nothing

            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            Dim objLN As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim lista As IList

            lista = objLN.RecuperarListaCondicional(GetType(FN.Localizaciones.DN.LocalidadDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlLocalidadDN", "Nombre", "Madrid"))
            localidad = lista.Item(0)

            lista = objLN.RecuperarListaCondicional(GetType(FN.Localizaciones.DN.LocalidadDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlLocalidadDN", "Nombre", "Pozuelo de Alarcón"))
            localidad1 = lista.Item(0)

            Dim DireccionNoUnica, DireccionNoUnica2, DireccionNoUnica3 As FN.Localizaciones.DN.DireccionNoUnicaDN
            Dim coldir As New List(Of FN.Localizaciones.DN.DireccionNoUnicaDN)

            DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()

            Dim listaTipoVia As IList = objLN.RecuperarLista(GetType(FN.Localizaciones.DN.TipoViaDN))
            For Each tipoVia As FN.Localizaciones.DN.TipoViaDN In listaTipoVia
                If tipoVia.Nombre = "CALLE" Then
                    calle = tipoVia
                    Exit For
                End If
            Next

            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

            DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()
            DireccionNoUnica.TipoVia = calle
            DireccionNoUnica.Via = "cornejero"
            DireccionNoUnica.Numero = 22
            DireccionNoUnica.CodPostal = 28220
            DireccionNoUnica.Localidad = localidad1

            Me.GuardarDatos(DireccionNoUnica)
            DireccionNoUnica2 = DireccionNoUnica
            coldir.Add(DireccionNoUnica)


            ' localidad = Me.GuardarDatos(New FN.Localizaciones.DN.LocalidadDN("Moratalz", entidad))
            DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()
            DireccionNoUnica.TipoVia = calle
            DireccionNoUnica.Via = "Pinacle"
            DireccionNoUnica.Numero = 98
            DireccionNoUnica.CodPostal = 28010
            DireccionNoUnica.Localidad = localidad

            Me.GuardarDatos(DireccionNoUnica)
            DireccionNoUnica3 = DireccionNoUnica
            coldir.Add(DireccionNoUnica)



            DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()
            DireccionNoUnica.TipoVia = calle
            DireccionNoUnica.Via = "pimpollo rojo"
            DireccionNoUnica.Numero = 33
            DireccionNoUnica.CodPostal = 28011
            DireccionNoUnica.Localidad = localidad

            Me.GuardarDatos(DireccionNoUnica)
            coldir.Add(DireccionNoUnica)



            '''''''''''''''''''''''''''''''''''''
            ' Se crean los datos de Empresas relacioandas
            '''''''''' ''''''''''''''''''''''''''
            colief = New FN.Localizaciones.DN.ColIEntidadFiscalDN

            Dim empresa As FN.Empresas.DN.EmpresaDN
            Dim empresaFiscal As FN.Empresas.DN.EmpresaFiscalDN

            Dim sede As FN.Empresas.DN.SedeEmpresaDN
            Dim tipoSede As FN.Empresas.DN.TipoSedeDN

            'empresaFiscal = New FN.Empresas.DN.EmpresaFiscalDN()
            'empresaFiscal.IdentificacionFiscal = New FN.Localizaciones.DN.CifDN("A81948077")
            'empresaFiscal.RazonSocial = "Ende yo caliente SA"
            'empresaFiscal.DomicilioFiscal = DireccionNoUnica2
            'empresaFiscal.NombreComercial = "Endesa"

            'empresa = New FN.Empresas.DN.EmpresaDN
            'empresa.TipoEmpresaDN = New FN.Empresas.DN.TipoEmpresaDN()
            'empresa.TipoEmpresaDN.Nombre = "Normal"
            'empresa.EntidadFiscal = empresaFiscal.EntidadFiscalGenerica

            'Me.GuardarDatos(empresa)
            'colief.Add(empresaFiscal)

            'sede = New FN.Empresas.DN.SedeEmpresaDN()
            'sede.Nombre = "Moratalaz"
            'tipoSede = New FN.Empresas.DN.TipoSedeDN()
            'tipoSede.Nombre = "Central"
            'sede.TipoSede = tipoSede
            'sede.SedePrincipal = True
            'sede.Empresa = empresa
            'sede.Direccion = DireccionNoUnica
            'Me.GuardarDatos(sede)



            'Dim cuentab As FN.Financiero.DN.CuentaBancariaDN

            'cuentab = New FN.Financiero.DN.CuentaBancariaDN
            'cuentab.Nombre = "cuenta de pagos"
            'cuentab.CCC = New FN.Financiero.DN.CCCDN
            'cuentab.CCC.Codigo = "00120345030000067890"
            'cuentab.Titulares.Add(empresa.EntidadFiscal)
            'Me.GuardarDatos(cuentab)


            'Dim persona As FN.Personas.DN.PersonaDN
            'persona = New FN.Personas.DN.PersonaDN

            'persona.Nombre = "Pablo" '
            'persona.Apellido = "ramirez ocaña"
            'persona.NIF = New FN.Localizaciones.DN.NifDN("45274941Q")

            'Dim ipf As FN.Personas.DN.PersonaFiscalDN
            'ipf = New FN.Personas.DN.PersonaFiscalDN
            'ipf.DomicilioFiscal = DireccionNoUnica3
            'ipf.Persona = persona
            'Me.GuardarDatos(ipf)
            'colief.Add(ipf)




            'empresaFiscal = New FN.Empresas.DN.EmpresaFiscalDN()
            'empresaFiscal.IdentificacionFiscal = New FN.Localizaciones.DN.CifDN("A47053210")
            'empresaFiscal.RazonSocial = "Rio tinto explosivos S.A."
            'empresaFiscal.DomicilioFiscal = DireccionNoUnica2
            'empresaFiscal.NombreComercial = "Explotame Explo"

            'empresa = New FN.Empresas.DN.EmpresaDN
            'empresa.TipoEmpresaDN = New FN.Empresas.DN.TipoEmpresaDN()
            'empresa.TipoEmpresaDN.Nombre = "tipo 2"
            'empresa.EntidadFiscal = empresaFiscal.EntidadFiscalGenerica

            'Me.GuardarDatos(empresa)
            'colief.Add(empresaFiscal)



            '''''''''''''''''''''''''''''''''''''
            ' Datos para la gestión de documentos
            '''''''''' ''''''''''''''''''''''''''

            Me.GuardarDatos(New AmvDocumentosDN.TipoCanalDN("Fax", "1"))
            Me.GuardarDatos(New AmvDocumentosDN.TipoCanalDN("Correo", "50"))
            Me.GuardarDatos(New AmvDocumentosDN.TipoCanalDN("Otros", "18"))


            ' creacion del arbol de tipos de entidades referidoras
            Dim TipoEntNegoio, tens1, tens2 As AmvDocumentosDN.TipoEntNegoioDN
            Dim nodo, nodoRec, nodosiniestros, nodoSiniestrosCoches, nodoPolizas As AmvDocumentosDN.NodoTipoEntNegoioDN
            Dim cabecera As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN


            nodo = New AmvDocumentosDN.NodoTipoEntNegoioDN
            nodo.Nombre = "Entidades Referidoras"
            cabecera = New AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
            cabecera.NodoTipoEntNegoio = nodo
            nodosiniestros = New AmvDocumentosDN.NodoTipoEntNegoioDN
            nodosiniestros.Nombre = "Siniestros"
            nodosiniestros.Padre = nodo

            nodoSiniestrosCoches = New AmvDocumentosDN.NodoTipoEntNegoioDN
            nodoSiniestrosCoches.Nombre = "Siniestros Coches"
            nodoSiniestrosCoches.Padre = nodosiniestros

            nodoPolizas = New AmvDocumentosDN.NodoTipoEntNegoioDN
            nodoPolizas.Nombre = "Siniestros Polizas"
            nodoPolizas.Padre = nodo



            TipoEntNegoio = New AmvDocumentosDN.TipoEntNegoioDN("Siniestro moto")
            tens1 = TipoEntNegoio
            TipoEntNegoio.Nombre = "Siniestro moto"
            Me.InsertarEntidadBase(TipoEntNegoio)

            nodosiniestros.ColHojas.Add(TipoEntNegoio)


            TipoEntNegoio = New AmvDocumentosDN.TipoEntNegoioDN("Siniestro coche")
            tens2 = TipoEntNegoio
            TipoEntNegoio.Nombre = "Siniestro coche"
            Me.InsertarEntidadBase(TipoEntNegoio)

            nodosiniestros.ColHojas.Add(TipoEntNegoio)


            TipoEntNegoio = New AmvDocumentosDN.TipoEntNegoioDN("Siniestrazo coche gordo")
            TipoEntNegoio.Nombre = "Siniestrazo coche gordo"
            Me.InsertarEntidadBase(TipoEntNegoio)

            nodosiniestros.ColHojas.Add(TipoEntNegoio)


            TipoEntNegoio = New AmvDocumentosDN.TipoEntNegoioDN("poliza coche")
            TipoEntNegoio.Nombre = "poliza coche"
            Me.InsertarEntidadBase(TipoEntNegoio)

            nodoPolizas.ColHojas.Add(TipoEntNegoio)


            TipoEntNegoio = New AmvDocumentosDN.TipoEntNegoioDN("poliza moto")
            TipoEntNegoio.Nombre = "poliza moto"
            Me.InsertarEntidadBase(TipoEntNegoio)

            nodoPolizas.ColHojas.Add(TipoEntNegoio)


            Dim cabeceraNodo As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
            cabeceraNodo = New AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
            cabeceraNodo.NodoTipoEntNegoio = nodo


            Me.GuardarDatos(cabeceraNodo)


            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            nodoRec = gi.Recuperar(nodo.ID, nodo.GetType)

            If nodoRec.ContenidoEnArbol(nodo.Hijos(0), CoincidenciaBusquedaEntidadDN.MismaRef) Then
                Throw New ApplicationExceptionDN("")
            End If
            If Not nodoRec.ContenidoEnArbol(nodo.Hijos(0), CoincidenciaBusquedaEntidadDN.Clones) Then
                Throw New ApplicationExceptionDN("debiera estar contenido en el árbol")
            End If

            '''''''''''''''''''''''''''''
            ' dar de alta   rutas de almacenamiento
            ''''''''''''''''''''''''''''''''''''''''''''''

            'Dim rutaAlmacenamiento As FN.Ficheros.FicherosDN.RutaAlmacenamientoFicherosDN
            ''rutaAlmacenamiento = New FN.Ficheros.FicherosDN.RutaAlmacenamientoFicherosDN
            ''rutaAlmacenamiento.RutaCarpeta = "\\192.168.3.22\destinos"
            ''rutaAlmacenamiento.EstadoRAF = FN.Ficheros.FicherosDN.RutaAlmacenamientoFicherosEstado.Abierta
            ''Me.GuardarDatos(rutaAlmacenamiento)


            'rutaAlmacenamiento = New FN.Ficheros.FicherosDN.RutaAlmacenamientoFicherosDN
            'rutaAlmacenamiento.RutaCarpeta = "D:\Signum\Proyectos\AMV\GDocEntrantes\Aplicacion\GDocEntrantesWS\App_Data\datos"
            'rutaAlmacenamiento.EstadoRAF = FN.Ficheros.FicherosDN.RutaAlmacenamientoFicherosEstado.Abierta
            'Me.GuardarDatos(rutaAlmacenamiento)

            'rutaAlmacenamiento = New FN.Ficheros.FicherosDN.RutaAlmacenamientoFicherosDN
            'rutaAlmacenamiento.RutaCarpeta = "D:\Signum\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Saliente"
            'rutaAlmacenamiento.EstadoRAF = FN.Ficheros.FicherosDN.RutaAlmacenamientoFicherosEstado.Disponible
            'Me.GuardarDatos(rutaAlmacenamiento)







            '''''''''''''''''''''''''''''''''''''
            'Creo importes debidos con sus origenes
            '''''''''' ''''''''''''''''''''''''''''








            '''''''''''''''''''''''''''''''''''''
            'Creo los usuarios de la aplicacion
            '''''''''' ''''''''''''''''''''''''''''

            Dim ColTipoEntNegoio As New AmvDocumentosDN.ColTipoEntNegoioDN

            ColTipoEntNegoio.Add(tens1)
            ColTipoEntNegoio.Add(tens2)
            CrearUnUsuarioParaCadaRol(ColTipoEntNegoio)






            tr.Confirmar()

        End Using





    End Sub

    Private Sub CargarDatosAMV()



        Using tr As New Transaccion







            Dim localidad As Framework.DatosNegocio.IEntidadDN

            Dim calle As FN.Localizaciones.DN.TipoViaDN = Nothing

            Dim objLN As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim lista As IList
            lista = objLN.RecuperarListaCondicional(GetType(FN.Localizaciones.DN.LocalidadDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlLocalidadDN", "Nombre", "ALCOBENDAS Y LA MORALEJA"))

            If lista Is Nothing OrElse lista.Count = 0 Then
                Throw New ApplicationException("no se pudo recuperar la localidad para AMV")
            Else
                localidad = lista.Item(0)
            End If

            Dim DireccionNoUnica As FN.Localizaciones.DN.DireccionNoUnicaDN

            DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()

            Dim listaTipoVia As IList = objLN.RecuperarLista(GetType(FN.Localizaciones.DN.TipoViaDN))
            For Each tipoVia As FN.Localizaciones.DN.TipoViaDN In listaTipoVia
                If tipoVia.Nombre = "AVENIDA" Then
                    calle = tipoVia
                    Exit For
                End If
            Next

            DireccionNoUnica.TipoVia = calle
            DireccionNoUnica.Via = "Bruselas"
            DireccionNoUnica.Numero = 38
            DireccionNoUnica.CodPostal = 28108
            DireccionNoUnica.Localidad = localidad

            '''''''''''''''''''''''''''''''''''''
            ' Se crean los datos de Empresa
            '''''''''' ''''''''''''''''''''''''''

            Dim empresa As FN.Empresas.DN.EmpresaDN
            Dim sede As FN.Empresas.DN.SedeEmpresaDN
            Dim tipoSede As FN.Empresas.DN.TipoSedeDN
            Dim empresaFiscal As FN.Empresas.DN.EmpresaFiscalDN
            Dim cuentab As FN.Financiero.DN.CuentaBancariaDN


            Dim empreasln As New FN.Empresas.LN.EmpresaLN
            sede = empreasln.RecuperarSedePrincipalxCIFEmpresa("B83204586")

            If sede Is Nothing Then


                empresa = New FN.Empresas.DN.EmpresaDN()
                empresaFiscal = New FN.Empresas.DN.EmpresaFiscalDN()

                empresaFiscal.IdentificacionFiscal = New FN.Localizaciones.DN.CifDN("B83204586")
                empresaFiscal.RazonSocial = "AMV Hispania S.L."
                empresaFiscal.DomicilioFiscal = DireccionNoUnica
                empresaFiscal.NombreComercial = "AMV"

                empresa.EntidadFiscal = empresaFiscal.EntidadFiscalGenerica
                empresa.TipoEmpresaDN = New FN.Empresas.DN.TipoEmpresaDN()
                empresa.TipoEmpresaDN.Nombre = "Correduría de seguros"

                Me.GuardarDatos(empresa)
                Me.GuardarDatos(empresaFiscal)

                sede = New FN.Empresas.DN.SedeEmpresaDN()
                sede.Nombre = "Alcobendas"
                tipoSede = New FN.Empresas.DN.TipoSedeDN()
                tipoSede.Nombre = "Sede España"
                sede.TipoSede = tipoSede
                sede.SedePrincipal = True
                sede.Empresa = empresa
                sede.Direccion = DireccionNoUnica
                Me.GuardarDatos(sede)



            End If





            cuentab = New FN.Financiero.DN.CuentaBancariaDN()
            cuentab.Nombre = "Cuenta de AMV"
            cuentab.CCC = New FN.Financiero.DN.CCCDN
            cuentab.CCC.Codigo = "21002287750200123033"
            'cuentab.Titulares.Add(empresa.EntidadFiscal)
            cuentab.Titulares.Add(sede.Empresa.EntidadFiscal)
            Me.GuardarDatos(cuentab)

            'Carga de los casos de uso
            AsignarOperacionesCasoUso()

            'Carga de loa datos de los departamentos y puestos
            CrearDepartamentosPuestos(sede.Empresa)

            'Datos del límite de los pagos
            Dim limitepago As New FN.GestionPagos.DN.LimitePagoDN()
            limitepago.LimiteFirmaAutomatica = 200
            limitepago.LimiteAviso = 400
            GuardarDatos(limitepago)

            tr.Confirmar()

        End Using


    End Sub

    Private Sub PoblarTablasTipo()

        Dim col As New List(Of AmvDocumentosDN.TipoContenidoDocDN)

        Me.GuardarDatos(New AmvDocumentosDN.TipoContenidoDocDN("1", "Variado", "1"))
        Me.GuardarDatos(New AmvDocumentosDN.TipoEntNegoioDN("Siniestro"))

    End Sub

    Private Function RecuperarVinculoMetodo(ByVal nombreMetodo As String, ByVal tipo As System.Type) As VinculoMetodoDN
        Dim vm As VinculoMetodoDN

        Using New CajonHiloLN(mrecurso)
            Dim tyrLN As New Framework.TiposYReflexion.LN.TiposYReflexionLN()
            vm = New VinculoMetodoDN(nombreMetodo, New VinculoClaseDN(tipo))

            Return tyrLN.CrearVinculoMetodo(vm.RecuperarMethodInfo())
        End Using

    End Function

    Private Function RecuperarVinculoClase(ByVal tipo As System.Type) As VinculoClaseDN
        Using New CajonHiloLN(mrecurso)
            Dim tyrLN As New Framework.TiposYReflexion.LN.TiposYReflexionLN()
            Return tyrLN.CrearVinculoClase(tipo)
        End Using

    End Function

    Private Sub AsignarOperacionesCasoUso()





        Using tr As New Transaccion







            Dim cb As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()

            Dim colOp As New Framework.Procesos.ProcesosDN.ColOperacionDN()
            Dim colOpAux As Framework.Procesos.ProcesosDN.ColOperacionDN
            Dim colCU As New ColCasosUsoDN()
            Dim cu As CasosUsoDN

            Dim lista As IList

            lista = cb.RecuperarLista(GetType(Framework.Procesos.ProcesosDN.OperacionDN))
            For Each operacion As Framework.Procesos.ProcesosDN.OperacionDN In lista
                colOp.Add(operacion)
            Next

            lista = cb.RecuperarLista(GetType(Framework.Usuarios.DN.CasosUsoDN))
            For Each casoUso As Framework.Usuarios.DN.CasosUsoDN In lista
                colCU.Add(casoUso)
            Next

            'Alta negocio
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Alta Negocio"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Gestión Talones"))
            cu = colCU.RecuperarPrimeroXNombre("Alta negocio")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Alta contabilidad
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Alta Contabilidad"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Gestión Talones"))
            cu = colCU.RecuperarPrimeroXNombre("Alta contabilidad")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Operar pago negocio
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Guardar Negocio"))
            cu = colCU.RecuperarPrimeroXNombre("Operar pago negocio")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Validar pago negocio
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Validación Negocio"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Anular N"))
            cu = colCU.RecuperarPrimeroXNombre("Validar pago negocio")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Operar pago contabilidad
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Guardar Contabilidad"))
            cu = colCU.RecuperarPrimeroXNombre("Operar pago contabilidad")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Validar pago contabilidad
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Validación Contabilidad"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Rechazar N"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Anular C"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Firma Dirección Automatica"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Decisión Firma Automatica-Manual"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Decisión Pago Talon"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Decisión Pago Transferencia"))
            cu = colCU.RecuperarPrimeroXNombre("Validar pago contabilidad")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Verificar cobro
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Verificación Cobro Contabilidad"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Anulación con Alta Contabilidad"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Anular Pago"))
            cu = colCU.RecuperarPrimeroXNombre("Verificar cobro")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Firmar pago
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Firma Dirección"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Rechazar N"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Rechazar C"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Anular D"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Decisión Pago Talon"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Decisión Pago Transferencia"))
            cu = colCU.RecuperarPrimeroXNombre("Firmar pago")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Regenerar fichero transferencias
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Regenerar FT"))
            cu = colCU.RecuperarPrimeroXNombre("Regenerar fichero transferencias")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Reactivar talón anulado impresión
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Reactivacion Anulación Impresión Dirección"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Anular D"))
            cu = colCU.RecuperarPrimeroXNombre("Reactivar talón anulado impresión")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Imprimir talón
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Impresión"))
            cu = colCU.RecuperarPrimeroXNombre("Imprimir talón")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Verificar impresión talón
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Validación Impresión"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Anulación Impresión"))
            cu = colCU.RecuperarPrimeroXNombre("Verificar impresión talón")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Generar fichero transferencias
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Adjuntar Fichero Transferencia"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Gestión Ficheros Transferencias"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Alta FT"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Guardar FT"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Generar FT"))
            cu = colCU.RecuperarPrimeroXNombre("Generar fichero transferencias")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)



            tr.Confirmar()

        End Using

    End Sub

    Private Sub CrearDepartamentosPuestos(ByVal empresa As FN.Empresas.DN.EmpresaDN)


        Using tr As New Transaccion





            Dim cb As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim lista As IList
            Dim colRol As Framework.Usuarios.DN.ColRolDN
            Dim colRolAux As Framework.Usuarios.DN.ColRolDN


            colRol = New Framework.Usuarios.DN.ColRolDN
            lista = cb.RecuperarLista(GetType(Framework.Usuarios.DN.RolDN))
            For Each rol As Framework.Usuarios.DN.RolDN In lista
                colRol.Add(rol)
            Next

            Dim departamento As FN.Empresas.DN.DepartamentoDN
            Dim departamentoTarea As FN.Empresas.DN.DepartamentoNTareaNDN
            Dim puesto As FN.Empresas.DN.PuestoDN


            'Departamento Contabilidad
            departamento = New FN.Empresas.DN.DepartamentoDN()
            departamento.Nombre = "Contabilidad"
            departamento.Empresa = empresa
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Operador contabilidad"))
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Responsable contabilidad"))
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Gestor impresión talones"))
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Gestor transferencias"))
            departamentoTarea = New FN.Empresas.DN.DepartamentoNTareaNDN()
            departamentoTarea.Departamento = departamento
            departamentoTarea.ColRoles = colRolAux

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Operador contabilidad"))
            puesto.Nombre = "Operador contabilidad"
            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Responsable contabilidad"))
            puesto.Nombre = "Responsable contabilidad"
            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Gestor impresión talones"))
            puesto.Nombre = "Gestor impresión talones"
            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Gestor transferencias"))
            puesto.Nombre = "Gestor transferencias"
            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)

            'Departamento Dirección
            departamento = New FN.Empresas.DN.DepartamentoDN()
            departamento.Nombre = "Dirección"
            departamento.Empresa = empresa
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Dirección empresa"))
            departamentoTarea = New FN.Empresas.DN.DepartamentoNTareaNDN()
            departamentoTarea.Departamento = departamento
            departamentoTarea.ColRoles = colRolAux

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            'Se añade el permiso para el límite de firma al rol de dirección
            Dim rolDireccion As RolDN = colRol.RecuperarPrimeroXNombre("Dirección empresa")
            Dim permisoD As New PermisoDN()
            permisoD.TipoPermiso = New TipoPermisoDN("LimiteFirmaPago")
            permisoD.EsRef = False
            permisoD.DatoVal = "1000"
            permisoD.Nombre = "Rol Dirección empresa"
            rolDireccion.ColMisPermisos = New ColPermisoDN()
            rolDireccion.ColMisPermisos.Add(permisoD)
            colRolAux.Add(rolDireccion)
            puesto.Nombre = "Dirección"
            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)



            colRol = New Framework.Usuarios.DN.ColRolDN
            lista = cb.RecuperarLista(GetType(Framework.Usuarios.DN.RolDN))
            For Each rol As Framework.Usuarios.DN.RolDN In lista
                colRol.Add(rol)
            Next


            'Departamento Siniestros
            departamento = New FN.Empresas.DN.DepartamentoDN()
            departamento.Nombre = "Siniestros"
            departamento.Empresa = empresa

            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Operador Siniestros"))
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Responsable Siniestros"))


            departamentoTarea = New FN.Empresas.DN.DepartamentoNTareaNDN()
            departamentoTarea.Departamento = departamento
            departamentoTarea.ColRoles = New ColRolDN()
            ' departamentoTarea.ColRoles.AddRange(colRolAux)
            departamentoTarea.ColRoles.Add(colRolAux(0))
            departamentoTarea.ColRoles.Add(colRolAux(1))
            Me.GuardarDatos(departamentoTarea)

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Operador Siniestros"))
            puesto.Nombre = "Operador Siniestros"

            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = New ColRolDN()
            puesto.ColRoles.AddRange(colRolAux)
            Me.GuardarDatos(puesto)


            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Responsable Siniestros"))
            puesto.Nombre = "Responsable Siniestros"
            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)

            'Departamento Gestión
            departamento = New FN.Empresas.DN.DepartamentoDN()
            departamento.Nombre = "Gestión"
            departamento.Empresa = empresa
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Operador Gestión"))
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Responsable Gestión"))
            departamentoTarea = New FN.Empresas.DN.DepartamentoNTareaNDN()
            departamentoTarea.Departamento = departamento
            departamentoTarea.ColRoles = colRolAux

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Operador Gestión"))
            puesto.Nombre = "Operador Gestión"

            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Responsable Gestión"))
            puesto.Nombre = "Responsable Gestión"
            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)


            tr.Confirmar()

        End Using



    End Sub

#End Region


#Region "Métodos Carga de direcciones"

    Public Sub CargarLocalidades(ByVal ruta As String)


        Try
            Dim m_xmld As XmlDocument
            Dim m_nodelist As XmlNodeList
            Dim m_node As XmlNode

            'Creamos el "XML Document"
            m_xmld = New XmlDocument()

            Dim texto As String

            Dim str As New System.IO.StreamReader(ruta)
            texto = str.ReadToEnd
            texto.Replace("ñ", "---").Replace("ü", "----")


            'Cargamos el archivo
            ' m_xmld.Load(ruta)
            m_xmld.LoadXml(texto)

            'Obtenemos la lista de los nodos "name"
            m_nodelist = m_xmld.SelectNodes("//provincia")



            Dim pais As New FN.Localizaciones.DN.PaisDN
            pais.Nombre = "España"
            Me.GuardarDatos(pais)


            'Iniciamos el ciclo de lectura
            For Each m_node In m_nodelist



                ProcesarProvincia(m_node, pais)



                ''Obtenemos el atributo del codigo
                'Dim mCodigo = m_node.Attributes.GetNamedItem("codigo").Value

                ''Obtenemos el Elemento nombre
                'Dim mNombre = m_node.ChildNodes.Item(0).InnerText

                ''Obtenemos el Elemento apellido
                'Dim mApellido = m_node.ChildNodes.Item(1).InnerText

                ''Escribimos el resultado en la consola, 
                ''pero tambien podriamos utilizarlos en
                ''donde deseemos
                'Console.Write("Codigo usuario: " & mCodigo _
                '  & " Nombre: " & mNombre _
                '  & " Apellido: " & mApellido)
                'Console.Write(vbCrLf)

            Next
        Catch ex As Exception
            'Error trapping
            Console.Write(ex.ToString())
        End Try


    End Sub

    Private Sub ProcesarProvincia(ByVal nodoxml As Xml.XmlNode, ByVal pPais As FN.Localizaciones.DN.PaisDN)


        Dim provincia As New FN.Localizaciones.DN.ProvinciaDN


        provincia.Pais = pPais
        provincia.Nombre = nodoxml.ChildNodes(0).ChildNodes(0).InnerText.Replace("----", "ü").Replace("---", "ñ")


        Me.GuardarDatos(provincia)

        Dim m_nodelist As XmlNodeList
        Dim m_node As XmlNode

        m_nodelist = nodoxml.ChildNodes(1).ChildNodes
        For Each m_node In m_nodelist

            ProcesarLocalidades(m_node, provincia)

        Next


    End Sub

    Private Sub ProcesarLocalidades(ByVal nodoxml As Xml.XmlNode, ByVal pProvincia As FN.Localizaciones.DN.ProvinciaDN)


        Dim localidad As New FN.Localizaciones.DN.LocalidadDN
        localidad.Provincia = pProvincia
        localidad.Nombre = nodoxml.InnerText.Replace("----", "ü").Replace("---", "ñ")
        Me.GuardarDatos(localidad)


    End Sub

    Private Sub CargarLocalidadesCodPostal(ByVal ruta As String)
        Dim str As New IO.StreamReader(ruta, System.Text.Encoding.Default)
        Dim colPa As New FN.Localizaciones.DN.ColPaisDN()
        Dim colPr As New FN.Localizaciones.DN.ColProvinciaDN()

        Do Until str.EndOfStream
            Dim linea As String
            linea = str.ReadLine()

            If Not String.IsNullOrEmpty(linea) Then
                Dim valores() As String = linea.Split(ControlChars.Tab)
                ProcesarRegistro(valores(0), valores(1), valores(2), valores(3), colPa, colPr)
            End If

        Loop
    End Sub

    Private Sub ProcesarRegistro(ByVal localidad As String, ByVal codPostal As String, ByVal provincia As String, ByVal pais As String, ByRef colPaises As FN.Localizaciones.DN.ColPaisDN, ByRef colpProvincias As FN.Localizaciones.DN.ColProvinciaDN)
        Dim objPais As FN.Localizaciones.DN.PaisDN
        Dim objProv As FN.Localizaciones.DN.ProvinciaDN = Nothing
        Dim objLoc As FN.Localizaciones.DN.LocalidadDN = Nothing
        Dim objCP As FN.Localizaciones.DN.CodigoPostalDN = Nothing
        Dim lista As IList
        Dim objLN As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

        Using New CajonHiloLN(mrecurso)
            objLN = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()

            'Se recupera el pais
            objPais = colPaises.RecuperarPrimeroXNombre(pais)
            If objPais Is Nothing Then
                objPais = New FN.Localizaciones.DN.PaisDN(pais)
                colPaises.Add(objPais)
            End If

            'se recupera la provincia
            objProv = colpProvincias.RecuperarPrimeroXNombre(provincia)
            If objProv Is Nothing Then
                objProv = New FN.Localizaciones.DN.ProvinciaDN(provincia, objPais)
                colpProvincias.Add(objProv)
            End If

            'se recupera la localidad
            lista = objLN.RecuperarListaCondicional(GetType(FN.Localizaciones.DN.LocalidadDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlLocalidadDN", "Nombre", localidad))
            If lista IsNot Nothing Then
                For Each loc As FN.Localizaciones.DN.LocalidadDN In lista
                    If loc.Provincia.GUID = objProv.GUID Then
                        objLoc = loc
                    End If
                Next
            End If
            If objLoc Is Nothing Then
                objLoc = New FN.Localizaciones.DN.LocalidadDN(localidad, objProv)
            End If

            'se recupera el código postal
            lista = objLN.RecuperarListaCondicional(GetType(FN.Localizaciones.DN.CodigoPostalDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlCodigoPostalDN", "Nombre", codPostal))
            If lista IsNot Nothing AndAlso lista.Count = 1 Then
                objCP = lista.Item(0)
            Else
                objCP = New FN.Localizaciones.DN.CodigoPostalDN(codPostal)
            End If

            If objLoc.ColCodigoPostal Is Nothing Then
                objLoc.ColCodigoPostal = New FN.Localizaciones.DN.ColCodigoPostalDN()
            End If

            If Not objLoc.ColCodigoPostal.Contiene(objCP, CoincidenciaBusquedaEntidadDN.Todos) Then
                objLoc.ColCodigoPostal.Add(objCP)
            End If

            Me.GuardarDatos(objLoc)

        End Using

    End Sub

#End Region


#Region "metodos de pagos"

#End Region

End Class


' prueba dela huellas


<Serializable()> Public Class ContenedoraH
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mHuella As Framework.DatosNegocio.HEDN

    Public Property Huella() As Framework.DatosNegocio.HEDN
        Get
            Return Me.mHuella
        End Get
        Set(ByVal value As Framework.DatosNegocio.HEDN)
            Me.CambiarValorRef(Of Framework.DatosNegocio.HEDN)(value, Me.mHuella)
        End Set
    End Property

End Class

<Serializable()> Public Class Pato
    Inherits Framework.DatosNegocio.EntidadDN



End Class

Imports Framework.DatosNegocio
Public Class GBD

    Private mrecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN

    Public Sub New(ByVal pRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN)


        If pRecurso Is Nothing Then
            Dim connectionstring As String
            Dim htd As New Generic.Dictionary(Of String, Object)

            connectionstring = "server=localhost;database=ssPruebasft;user=sa;pwd='sa'"
            htd.Add("connectionstring", connectionstring)
            mrecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        Else
            mrecurso = pRecurso
        End If


        '  Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaCamposAMVDocsEntrantesLN

    End Sub

#Region "Métodos"
    Public Sub EliminarTablas()

        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim ds As Data.DataSet


        Dim dr As Data.DataRow
        Dim nombretabla As String
        Dim eliminables As Int16
        Dim vueltas As Int16

        Dim sqlElim As String

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

    Public Sub CrearTablas()
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.GenerarTablas2(GetType(MNavegacionDatosDN.RelacionEntidadesNavDN), Nothing)




    End Sub

    'Public Sub CrearTablasPruebas()
    '    Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

    '    Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

    '    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
    '    gi.GenerarTablas2(GetType(MNavegacionDatosDN.RelacionEntidadesNavDN), Nothing)

    '    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
    '    gi.GenerarTablas2(GetType(), Nothing)




    'End Sub

    Public Sub CargarDatosBasicos(ByVal crearArbol As Boolean)
        ''''''''''''''''''''''''''''''''''''''
        'Creo los casos de uso de  la aplicacion
        ''''''''''''''''''''''''''''''''''''''

        ''recupero todos los metodos de sistema
        'Dim rolat As UsuariosDN.RolDN
        'If My.Settings.datosCasosUso Is Nothing OrElse My.Settings.datosCasosUso = "" Then


        '    Dim usurios As UsuariosLN.UsuariosLN
        '    usurios = New UsuariosLN.UsuariosLN(Nothing, mrecurso)
        '    rolat = usurios.GeneraRolAutorizacionTotal("Administrador Total")
        '    Me.GuardarDatos(rolat)

        'Else



        '    Dim usurios As UsuariosLN.UsuariosLN
        '    usurios = New UsuariosLN.UsuariosLN(Nothing, mrecurso)
        '    usurios.GenerarRolesDeInicioDeSistema(My.Settings.datosCasosUso)

        '    Dim rolesln As UsuariosLN.RolLN
        '    rolesln = New UsuariosLN.RolLN(Nothing, mrecurso)
        '    rolat = rolesln.RecuperarColRoles.RecuperarPrimeroXNombre("Administrador Total")
        '    If rolat Is Nothing Then
        '        Throw New ApplicationException(" no se recupero el rol de aturizacion total")
        '    End If
        'End If




        ''''''''''''''''''''''''''''''''''''''
        ''Creo los datos de tipos
        '''''''''''''''''''''''''''''''''''''''
        'Dim tipoOp As AmvDocumentosDN.TipoOperacionREnFDN
        'tipoOp = New AmvDocumentosDN.TipoOperacionREnFDN
        'Me.InsertarTiposDatos(tipoOp.RecuperarTiposTodos)


        'Dim estadoRENF As AmvDocumentosDN.EstadosRelacionENFicheroDN
        'estadoRENF = New AmvDocumentosDN.EstadosRelacionENFicheroDN
        'Me.InsertarTiposDatos(estadoRENF.RecuperarTiposTodos)


        ''''''''''''''''''''''''''''''''''''''
        ''Creo el arbol vacio
        '''''''''''''''''''''''''''''''''''''''

        'If crearArbol Then



        '    Dim nodo As AmvDocumentosDN.NodoTipoEntNegoioDN

        '    nodo = New AmvDocumentosDN.NodoTipoEntNegoioDN
        '    nodo.Nombre = "Entidades Referidoras"

        '    Dim cabeceraNodo As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        '    cabeceraNodo = New AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        '    cabeceraNodo.NodoTipoEntNegoio = nodo

        '    Me.GuardarDatos(cabeceraNodo)

        'End If



    End Sub



    Public Sub CrearUnUsuarioAdminTotal(ByVal pNombreRolAdminTotal As String, ByVal pId As String, ByVal pClave As String)

        'Dim usuario As UsuariosDN.UsuarioDN
        'Dim uln As New UsuariosLN.UsuariosLN(Nothing, mrecurso)
        'Dim Principal As UsuariosDN.PrincipalDN
        'Dim di As UsuariosDN.DatosIdentidadDN
        'Dim colRol, colTodosRol As UsuariosDN.ColRolDN
        'Dim rolesln As UsuariosLN.RolLN


        '' admin negocio
        'rolesln = New UsuariosLN.RolLN(Nothing, mrecurso)
        'colTodosRol = rolesln.RecuperarColRoles




        'Dim rol As UsuariosDN.RolDN
        'For Each rol In colTodosRol

        '    If rol.Nombre = pNombreRolAdminTotal Then
        '        Dim nombreclave As String = NicYClaveRol(rol.Nombre)
        '        colRol = New UsuariosDN.ColRolDN
        '        colRol.Add(rol)
        '        usuario = New UsuariosDN.UsuarioDN(nombreclave, True)
        '        Principal = New UsuariosDN.PrincipalDN(nombreclave, usuario, colRol)
        '        di = New UsuariosDN.DatosIdentidadDN(pId, pClave)
        '        Me.GuardarDatos(Principal)
        '        Me.GuardarDatos(di)
        '    End If


        'Next




    End Sub

    'Public Sub CrearUnUsuarioParaCadaRol(ByVal coltipoEN As AmvDocumentosDN.ColTipoEntNegoioDN)

    '    'Dim usuario As UsuariosDN.UsuarioDN
    '    'Dim uln As New UsuariosLN.UsuariosLN(Nothing, mrecurso)
    '    'Dim Principal As UsuariosDN.PrincipalDN
    '    'Dim di As UsuariosDN.DatosIdentidadDN
    '    'Dim colRol, colTodosRol As UsuariosDN.ColRolDN
    '    'Dim rolesln As UsuariosLN.RolLN


    '    '' admin negocio
    '    'rolesln = New UsuariosLN.RolLN(Nothing, mrecurso)
    '    'colTodosRol = rolesln.RecuperarColRoles




    '    'Dim rol As UsuariosDN.RolDN
    '    'For Each rol In colTodosRol

    '    '    Dim nombreclave As String = NicYClaveRol(rol.Nombre)
    '    '    colRol = New UsuariosDN.ColRolDN
    '    '    colRol.Add(rol)
    '    '    usuario = New UsuariosDN.UsuarioDN(nombreclave, True, New AmvDocumentosDN.HuellaOperadorDN(New AmvDocumentosDN.OperadorDN(nombreclave, coltipoEN)))
    '    '    Principal = New UsuariosDN.PrincipalDN(nombreclave, usuario, colRol)
    '    '    di = New UsuariosDN.DatosIdentidadDN(nombreclave, nombreclave)
    '    '    Me.GuardarDatos(Principal)
    '    '    Me.GuardarDatos(di)

    '    'Next




    'End Sub


    Private Function NicYClaveRol(ByVal pNombreRol As String) As String
        Try
            Dim palabras() As String

            palabras = pNombreRol.Split(" ")

            Return (palabras(0).Substring(0, 1) & palabras(1).Substring(0, 2)).ToLower

        Catch ex As Exception
            Throw New ApplicationException(" el nombre del rol debe tener almenos dos palabras")
        End Try

    End Function


    Public Sub PublicarFachada()
        'Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("GDocEntrantesFS", Me.mrecurso)
        'Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("UsuariosFS", mrecurso)
        'Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("FicherosFS", mrecurso)
        'Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("MotorBusquedaFS", mrecurso)

    End Sub

    Public Sub EjecutarTodo()

        Me.EliminarTablas()
        Me.CrearTablas()
        Me.PublicarFachada()
        Me.CargarDatosBasicos(False)
        Me.CargarDatosPruebas()



    End Sub
    Public Sub EjecutarTodoBasico(ByVal pNombreRolAdminTotal As String, ByVal pId As String, ByVal pClave As String)

        Me.EliminarTablas()
        Me.CrearTablas()
        Me.PublicarFachada()
        Me.CargarDatosBasicos(True)
        Me.CrearUnUsuarioAdminTotal(pNombreRolAdminTotal, pId, pClave)


    End Sub
    Public Sub InsertarTiposDatos(ByVal pEntidad As IList)

        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.InsertarEntidadBase(pEntidad)
    End Sub

    Public Sub GuardarDatos(ByVal pEntidad As IEntidadDN)

        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.Guardar(pEntidad)
    End Sub
    Public Sub InsertarEntidadBase(ByVal pEntidad As IEntidadBaseDN)

        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        gi.InsertarEntidadBase(pEntidad)
    End Sub
    Private Sub CargarDatosPruebas()


        'Me.GuardarDatos(New AmvDocumentosDN.TipoCanalDN("Fax", "1"))
        'Me.GuardarDatos(New AmvDocumentosDN.TipoCanalDN("Correo", "50"))
        'Me.GuardarDatos(New AmvDocumentosDN.TipoCanalDN("Otros", "18"))


        '' creacion del arbol de tipos de entidades referidoras
        'Dim TipoEntNegoio, tens1, tens2 As AmvDocumentosDN.TipoEntNegoioDN
        'Dim nodo, nodoRec, nodosiniestros, nodoSiniestrosCoches, nodoPolizas As AmvDocumentosDN.NodoTipoEntNegoioDN
        'Dim cabecera As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN



        'nodo = New AmvDocumentosDN.NodoTipoEntNegoioDN
        'nodo.Nombre = "Entidades Referidoras"
        'cabecera = New AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        'cabecera.NodoTipoEntNegoio = nodo
        'nodosiniestros = New AmvDocumentosDN.NodoTipoEntNegoioDN
        'nodosiniestros.Nombre = "Siniestros"
        'nodosiniestros.Padre = nodo

        'nodoSiniestrosCoches = New AmvDocumentosDN.NodoTipoEntNegoioDN
        'nodoSiniestrosCoches.Nombre = "Siniestros Coches"
        'nodoSiniestrosCoches.Padre = nodosiniestros

        'nodoPolizas = New AmvDocumentosDN.NodoTipoEntNegoioDN
        'nodoPolizas.Nombre = "Siniestros Polizas"
        'nodoPolizas.Padre = nodo




        'TipoEntNegoio = New AmvDocumentosDN.TipoEntNegoioDN("Siniestro moto")
        'tens1 = TipoEntNegoio
        'TipoEntNegoio.Nombre = "Siniestro moto"
        'Me.InsertarEntidadBase(TipoEntNegoio)

        'nodosiniestros.ColHojas.Add(TipoEntNegoio)


        'TipoEntNegoio = New AmvDocumentosDN.TipoEntNegoioDN("Siniestro coche")
        'tens2 = TipoEntNegoio
        'TipoEntNegoio.Nombre = "Siniestro coche"
        'Me.InsertarEntidadBase(TipoEntNegoio)

        'nodosiniestros.ColHojas.Add(TipoEntNegoio)



        'TipoEntNegoio = New AmvDocumentosDN.TipoEntNegoioDN("Siniestrazo coche gordo")
        'TipoEntNegoio.Nombre = "Siniestrazo coche gordo"
        'Me.InsertarEntidadBase(TipoEntNegoio)

        'nodosiniestros.ColHojas.Add(TipoEntNegoio)



        'TipoEntNegoio = New AmvDocumentosDN.TipoEntNegoioDN("poliza coche")
        'TipoEntNegoio.Nombre = "poliza coche"
        'Me.InsertarEntidadBase(TipoEntNegoio)

        'nodoPolizas.ColHojas.Add(TipoEntNegoio)



        'TipoEntNegoio = New AmvDocumentosDN.TipoEntNegoioDN("poliza moto")
        'TipoEntNegoio.Nombre = "poliza moto"
        'Me.InsertarEntidadBase(TipoEntNegoio)

        'nodoPolizas.ColHojas.Add(TipoEntNegoio)


        'Dim cabeceraNodo As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        'cabeceraNodo = New AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        'cabeceraNodo.NodoTipoEntNegoio = nodo


        'Me.GuardarDatos(cabeceraNodo)





        'Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
        'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mrecurso)
        'nodoRec = gi.Recuperar(nodo.ID, nodo.GetType)

        'If nodoRec.ContenidoEnArbol(nodo.Hijos(0), CoincidenciaBusquedaEntidadDN.MismaRef) Then
        '    Throw New ApplicationExceptionDN("")
        'End If
        'If Not nodoRec.ContenidoEnArbol(nodo.Hijos(0), CoincidenciaBusquedaEntidadDN.Clones) Then
        '    Throw New ApplicationExceptionDN("debiera estar contido en el arbol")
        'End If



        '' dar de alta   rutas de almacenamiento

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
        'rutaAlmacenamiento.RutaCarpeta = "D:\Signum\Proyectos\AMV\GDocEntrantes\ficheros\entrada\Saliente"
        'rutaAlmacenamiento.EstadoRAF = FN.Ficheros.FicherosDN.RutaAlmacenamientoFicherosEstado.Disponible
        'Me.GuardarDatos(rutaAlmacenamiento)



        ''''''''''''''''''''''''''''''''''''''
        ''Creo los usuarios de la aplicacion
        ''''''''''' ''''''''''''''''''''''''''''

        'Dim ColTipoEntNegoio As New AmvDocumentosDN.ColTipoEntNegoioDN

        'ColTipoEntNegoio.Add(tens1)
        'ColTipoEntNegoio.Add(tens2)
        'CrearUnUsuarioParaCadaRol(ColTipoEntNegoio)




    End Sub




    Private Sub PoblarTablasTipo()

        'Dim col As New List(Of AmvDocumentosDN.TipoContenidoDocDN)


        'Me.GuardarDatos(New AmvDocumentosDN.TipoContenidoDocDN("1", "Variado", "1"))
        'Me.GuardarDatos(New AmvDocumentosDN.TipoEntNegoioDN("Siniestro"))

    End Sub

#End Region


End Class

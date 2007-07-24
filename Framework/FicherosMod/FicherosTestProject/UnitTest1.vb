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

Imports System.Diagnostics
<TestClass()> Public Class UnitTest1


    Public mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = Nothing

    Private Sub CrearElRecurso(ByVal connectionstring As String)
        Dim htd As New System.Collections.Generic.Dictionary(Of String, Object)

        If connectionstring Is Nothing OrElse connectionstring = "" Then
            connectionstring = "server=localhost;database=ssPruebasFT;user=sa;pwd='sa'"
        End If

        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a AMV", "sqls", htd)


        'Asignamos el mapeado de  gestor de instanciación
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaCamposFicherosTest
    End Sub



    <TestMethod()> Public Sub CrearEntorno()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            CrearEntornop()
        End Using

    End Sub


    Public Sub CrearEntornop()
        Using tr As New Transaccion()
            Dim gbd As New Framework.Ficheros.FicherosAD.FicherosGBD(Recurso.Actual)

            gbd.EliminarRelaciones()
            gbd.EliminarVistas()
            gbd.EliminarTablas()

            gbd.CrearTablas()
            gbd.CrearVistas()
            tr.Confirmar()
        End Using

    End Sub





    Public Function GuardarDatos(ByVal pEntidad As Object) As Object

        '   ObtenerRecurso()


        Using tr As New Transaccion


            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(pEntidad)
            tr.Confirmar()
            Return pEntidad



        End Using





    End Function




    <TestMethod()> Public Sub ProbarVinculacionAutomaticaCDHF()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)


            CrearEntornop()
            CrearCajonesDocumentoyHuellaFicherosVincualbles()
            Using tr As New Transaccion(True)

                ProbarVinculacionAutomaticaCDHFp()
                tr.Confirmar()

            End Using

            'Using tr As New Transaccion(True)




            '    tr.Confirmar()

            'End Using


        End Using



    End Sub

    Public Sub ProbarVinculacionAutomaticaCDHFp()




        Using tr As New Transaccion

            Dim ColCDcorectos, ColCDIncorectos As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN


            Dim micdln As Framework.Ficheros.FicherosLN.CajonDocumentoLN
            micdln = New Framework.Ficheros.FicherosLN.CajonDocumentoLN
            micdln.VincularCajonDocumento(ColCDcorectos, ColCDIncorectos)



            System.Diagnostics.Debug.WriteLine(ColCDcorectos.Count)
            System.Diagnostics.Debug.WriteLine(ColCDIncorectos.Count)

            tr.Confirmar()

        End Using




    End Sub
    Public Sub CrearCajonesDocumentoyHuellaFicherosVincualbles()



        Using tr As New Transaccion

            Dim tf, tf2 As Framework.Ficheros.FicherosDN.TipoFicheroDN
            Dim cd As Framework.Ficheros.FicherosDN.CajonDocumentoDN
            Dim hf As Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN
            Dim id As Framework.Ficheros.FicherosDN.IdentificacionDocumentoDN
            Dim entidadReferida As Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN


            tf = New Framework.Ficheros.FicherosDN.TipoFicheroDN
            tf.Nombre = "tipofichero A"
            GuardarDatos(tf)


            tf2 = New Framework.Ficheros.FicherosDN.TipoFicheroDN
            tf2.Nombre = "tipofichero B"
            GuardarDatos(tf2)



            For a As Int16 = 0 To 5

                id = New Framework.Ficheros.FicherosDN.IdentificacionDocumentoDN
                id.TipoFichero = tf
                id.Identificacion = "a" & a

                entidadReferida = New Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN
                entidadReferida.Nombre = "entidad rel" & a

                cd = New Framework.Ficheros.FicherosDN.CajonDocumentoDN

                'If a = Int(5 / 2) Then
                '    cd.TipoDocumento = tf2
                'Else
                '    cd.TipoDocumento = id.TipoFichero
                'End If

                cd.HuellasEntidadesReferidas.AddHuellaPara(entidadReferida)
                cd.TipoDocumento = id.TipoFichero
                cd.IdentificacionDocumento = id
                GuardarDatos(cd)

                hf = New Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN
                hf.Colidentificaciones.Add(id)
                GuardarDatos(hf)

            Next



            tr.Confirmar()

        End Using






    End Sub




    <TestMethod()> Public Sub CrearEntornoPruebas()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)
            Using tr As New Transaccion()
                CrearEntornop()



                '''''''''''''''''''''''''''''''
                'Explicación del test
                ' la idea es que el producto requeire un cajon documento pero sera asociado a la linea de producto






                ''''''''''''''''''''''''''
                ' Creamos las tablas para las entidades de puerbas
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.GenerarTablas2(GetType(Producto), Nothing)

                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.GenerarTablas2(GetType(LineaProducto), Nothing)






                '''''''''''''''''''''''''''''''''''''''''
                ' creación de los datos


                ' tipos de informes
                Dim tipof_mostaza As Framework.Ficheros.FicherosDN.TipoFicheroDN
                Dim tipof_ejecuciones As Framework.Ficheros.FicherosDN.TipoFicheroDN



                tipof_mostaza = New Framework.Ficheros.FicherosDN.TipoFicheroDN()
                tipof_mostaza.Nombre = "InformeGasMostaza"
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(tipof_mostaza)

                tipof_ejecuciones = New Framework.Ficheros.FicherosDN.TipoFicheroDN()
                tipof_ejecuciones.Nombre = "InformeEjecuciones"
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(tipof_ejecuciones)



                ' productos
                Dim p, pa, pb, pc As Producto
                p = New Producto
                p.Nombre = "Producto A"
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(p)
                pa = p

                p = New Producto
                p.Nombre = "Producto B"
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(p)
                pb = p

                p = New Producto
                p.Nombre = "Producto C"
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(p)
                pc = p


                ' rear el documento requerido

                Dim dr As Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN
                dr = New Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN
                dr.Nombre = "tdr 1"
                dr.ColEntidadesRequeridoras.AddHuellaPara(pa)
                dr.ColEntidadesRequeridoras.AddHuellaPara(pb)
                dr.TipoDoc = tipof_mostaza
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(dr)

                dr = New Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN
                dr.Nombre = "tdr 2"
                dr.ColEntidadesRequeridoras.AddHuellaPara(pc)
                dr.TipoDoc = tipof_ejecuciones
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(dr)




                ' crear una linea de producto para el prucuto



                Dim lp As LineaProducto
                lp = New LineaProducto
                lp.Nombre = "liea Producto 1"
                lp.producto = pa
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(lp)




                'FIN''''''''''''''''''''''''''''''''''''''''''''''''''''''




                tr.Confirmar()



                'Dim oficial_ejecuciones As New OficialEjecuciones
                'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                'gi.Guardar(oficial_ejecuciones)

                'Dim mapeado As Framework.Ficheros.FicherosDN.MapeadoDocumentoEntidadDN
                'mapeado = New Framework.Ficheros.FicherosDN.MapeadoDocumentoEntidadDN()
                'mapeado.TipoDocumento = tipof_ejecuciones
                'mapeado.HEDNCajondocumento = New Ficheros.FicherosDN.HuellaEntidadReferidaCajonDocumentoDN()
                'mapeado.HEDNCajondocumento.EntidadReferida = oficial_ejecuciones
                'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                'gi.Guardar(mapeado)


                'Dim oficial_exterminio As New OficialExterminio
                'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                'gi.Guardar(oficial_ejecuciones)

                'mapeado = New Framework.Ficheros.FicherosDN.MapeadoDocumentoEntidadDN()
                'mapeado.TipoDocumento = tipof_ejecuciones
                'mapeado.HEDNCajondocumento = New Ficheros.FicherosDN.HuellaEntidadReferidaCajonDocumentoDN()
                'mapeado.HEDNCajondocumento.EntidadReferida = oficial_exterminio
                'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                'gi.Guardar(mapeado)

                'mapeado = New Framework.Ficheros.FicherosDN.MapeadoDocumentoEntidadDN()
                'mapeado.TipoDocumento = tipof_mostaza
                'mapeado.HEDNCajondocumento = New Ficheros.FicherosDN.HuellaEntidadReferidaCajonDocumentoDN()
                'mapeado.HEDNCajondocumento.EntidadReferida = oficial_exterminio
                'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                'gi.Guardar(mapeado)

                'Dim miln As New Ficheros.FicherosLN.CajonDocumentoLN()

                'Dim colcajones As Ficheros.FicherosDN.ColCajonDocumentoDN = miln.GenerarCajonesParaObjeto(oficial_ejecuciones)
                'For Each cajon As Ficheros.FicherosDN.CajonDocumentoDN In colcajones
                '    cajon.HuellasEntidadesReferidas.Add(New Ficheros.FicherosDN.HuellaEntidadReferidaCajonDocumentoDN())
                '    cajon.HuellasEntidadesReferidas(cajon.HuellasEntidadesReferidas.Count - 1).EntidadReferida = oficial_ejecuciones
                '    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                '    gi.Guardar(cajon)
                'Next


                'colcajones = miln.GenerarCajonesParaObjeto(oficial_exterminio)
                'For Each cajon As Ficheros.FicherosDN.CajonDocumentoDN In colcajones
                '    cajon.HuellasEntidadesReferidas.Add(New Ficheros.FicherosDN.HuellaEntidadReferidaCajonDocumentoDN())
                '    cajon.HuellasEntidadesReferidas(cajon.HuellasEntidadesReferidas.Count - 1).EntidadReferida = oficial_exterminio
                '    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                '    gi.Guardar(cajon)
                'Next


                'Debug.WriteLine("-")
                'Debug.WriteLine("-")
                'colcajones = miln.RecuperarCajonesPorObjeto(oficial_exterminio)
                'Debug.WriteLine("Cajones de documento para oficial_exterminio:")
                'For Each cajon As Ficheros.FicherosDN.CajonDocumentoDN In colcajones
                '    Debug.WriteLine(cajon.TipoDocumento.Nombre)
                'Next

                'Debug.WriteLine("-")
                'Debug.WriteLine("-")
                'colcajones = miln.RecuperarCajonesPorObjeto(oficial_ejecuciones)
                'Debug.WriteLine("Cajones de documento para oficial_ejecuciones:")
                'For Each cajon As Ficheros.FicherosDN.CajonDocumentoDN In colcajones
                '    Debug.WriteLine(cajon.TipoDocumento.Nombre)
                'Next

            End Using
        End Using
    End Sub


    <TestMethod()> Public Sub RecuperarColDocumentosRequeridosProducto()

        CrearElRecurso("")


        Using New CajonHiloLN(mRecurso)
            Using tr As New Transaccion()

                Dim ln As New Ficheros.FicherosLN.CajonDocumentoLN
                Dim lista As IList
                Dim bln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                lista = bln.RecuperarLista(GetType(Producto))


                Dim coldr As Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN

                coldr = ln.RecuperarTipoDocumentoRequerido(lista(0))
                If coldr.Count = 0 Then
                    Throw New ApplicationException("debia obtenerse un tipo de documento requerido")
                End If
            End Using
        End Using
    End Sub


    <TestMethod()> Public Sub CrearCajopnesDocumentoParaLieaProducto()

        CrearElRecurso("")


        Using New CajonHiloLN(mRecurso)
            Using tr As New Transaccion()

                Dim ln As New Ficheros.FicherosLN.CajonDocumentoLN
                Dim lista As IList
                Dim bln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                lista = bln.RecuperarLista(GetType(LineaProducto))

                Dim colh As New Framework.DatosNegocio.ColHEDN
                colh.AddHuellaPara(lista(0))
                Dim edn As IEntidadDN = lista(0)

                Dim ht As Hashtable = edn.ToHtGUIDs(Nothing, Nothing) ' obtener los ides de los elementos


                Dim coldr As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN
                coldr = ln.GenerarCajonesParaObjetos(colh, Framework.TiposYReflexion.LN.ListHelper(Of String).Convertir(ht.Keys), Nothing)
                If coldr.Count = 0 Then
                    Throw New ApplicationException("debia obtenerse un tipo de documento requerido")
                End If
            End Using
        End Using
    End Sub


End Class

Public Class Producto
    Inherits Framework.DatosNegocio.EntidadDN

End Class

Public Class LineaProducto
    Inherits Framework.DatosNegocio.EntidadDN





    Protected mproducto As Producto

    <RelacionPropCampoAtribute("mproducto")> _
    Public Property producto() As Producto

        Get
            Return mproducto
        End Get

        Set(ByVal value As Producto)
            CambiarValorRef(Of Producto)(value, mproducto)

        End Set
    End Property





End Class


Public Class GestorMapPersistenciaCamposFicherosTest
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




        If (pTipo Is GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mDatos"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


            Return mapinst
        End If

        mapinst = Me.RecuperarMap_Framework_Usuarios(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        Return Nothing
    End Function
    Private Function RecuperarMap_Framework_Usuarios(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If pTipo Is GetType(Framework.Usuarios.DN.DatosIdentidadDN) Then

            Me.MapearCampoSimple(mapinst, "mHashClave", CampoAtributoDN.PersistenciaContenidaSerializada)

            Return mapinst
        End If

        If pTipo Is GetType(Framework.Usuarios.DN.UsuarioDN) Then
            Dim mapinstSub As New InfoDatosMapInstClaseDN
            Dim alentidades As New ArrayList

            'Me.VincularConClase("mHuellaEntidadUserDN", New ElementosDeEnsamblado("AmvDocumentosDN", "AmvDocumentosDN.HuellaOperadorDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
            'Me.VincularConClase("mEntidadUser", New ElementosDeEnsamblado("EmpresasDN", GetType(FN.Empresas.DN.HuellaCacheEmpleadoYPuestosRDN).FullName), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mEntidadUser"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mHuellaEntidadUserDN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


            Return mapinst
        End If




        If pTipo Is GetType(Framework.Usuarios.DN.PrincipalDN) Then
            Me.MapearCampoSimple(mapinst, "mClavePropuesta", CampoAtributoDN.NoProcesar)
            Return mapinst
        End If

        If pTipo Is GetType(Framework.Usuarios.DN.PermisoDN) Then
            Me.MapearCampoSimple(mapinst, "mDatoRef", CampoAtributoDN.NoProcesar)
            Return mapinst
        End If

        If pTipo Is GetType(Framework.Usuarios.DN.TipoPermisoDN) Then
            Me.MapearCampoSimple(mapinst, "mNombre", CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            Return mapinst
        End If





    End Function

End Class





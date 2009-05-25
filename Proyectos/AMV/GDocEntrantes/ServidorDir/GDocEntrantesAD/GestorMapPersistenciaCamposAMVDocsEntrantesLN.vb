Imports Framework.TiposYReflexion.DN
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework
Imports Framework.Usuarios.DN

Public Class GestorMapPersistenciaCamposAMVDocsEntrantesLN
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
            Me.MapearClase("mEntidadReferidaHuella", CampoAtributoDN.SoloGuardarYNoReferido, campodatos, mapinst)
        End If


        If TiposYReflexion.LN.InstanciacionReflexionHelperLN.HeredaDe(pTipo, GetType(Framework.DatosNegocio.EntidadTemporalDN)) Then
            If mapinst Is Nothing Then
                mapinst = New InfoDatosMapInstClaseDN
            End If

            Dim mapSubInst As New InfoDatosMapInstClaseDN
            ' mapeado de la clase referida por el campo
            mapSubInst.NombreCompleto = "Framework.DatosNegocio.EntidadTemporalDN"
            ParametrosGeneralesNoProcesar(mapSubInst)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mPeriodo"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.MapSubEntidad = mapSubInst

        End If







        Return mapinst
    End Function


    Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        Dim f As Framework.TiposYReflexion.DN.VinculoClaseDN

        'ZONA: tipos ________________________________________________________________
        'If pTipo.FullName = "Framework.TiposYReflexion.DN.VinculoClaseDN" Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mNombreClase"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

        '    Return mapinst
        'End If
        'FINZONA: tipos ________________________________________________________________

        If pTipo.FullName.Contains("IDestinoDN") Then
            Debug.WriteLine(pTipo.FullName)
        End If


        'ZONA: UsuarioDN ________________________________________________________________

        'Mapeado de UsuarioDN, donde la clase mapea sus interfaces, y solo es para ella.

        If pTipo Is GetType(DatosIdentidadDN) Then

            Me.MapearClase("mHashClave", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

            Return mapinst
        End If

        If pTipo Is GetType(UsuarioDN) Then
            Dim mapinstSub As New InfoDatosMapInstClaseDN
            Dim alentidades As New ArrayList

            Me.VincularConClase("mHuellaEntidadUserDN", New ElementosDeEnsamblado("AmvDocumentosDN", GetType(AmvDocumentosDN.HuellaOperadorDN).FullName), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
            ' Me.VincularConClase("mEntidadUser", New ElementosDeEnsamblado("EmpresasDN", GetType(AmvDocumentosDN.HuellaOperadorDN).FullName), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

            Return mapinst
        End If

        If pTipo Is GetType(PrincipalDN) Then
            Me.MapearClase("mClavePropuesta", CampoAtributoDN.NoProcesar, campodatos, mapinst)
            Return mapinst
        End If

        If pTipo Is GetType(Framework.Usuarios.DN.PermisoDN) Then
            Me.MapearClase("mDatoRef", CampoAtributoDN.NoProcesar, campodatos, mapinst)
            Return mapinst
        End If

        If pTipo Is GetType(Framework.Usuarios.DN.TipoPermisoDN) Then
            Me.MapearClase("mNombre", CampoAtributoDN.UnicoEnFuenteDatosoNulo, campodatos, mapinst)
            Return mapinst
        End If

        'FINZONA: UsuarioDN ________________________________________________________________

        'ZONA: Framework.DatosNegocio ________________________________________________________________



        If (pTipo Is GetType(Framework.Usuarios.DN.AutorizacionRelacionalDN)) Then

            Dim alentidades As ArrayList
            Dim mapSubInst As InfoDatosMapInstClaseDN

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            mapSubInst.NombreCompleto = GetType(Framework.Usuarios.DN.PrincipalDN).FullName
            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.TipoEntidadOrigenDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.TipoEmpresaDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mColEntidadesRelacionadas"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst


        End If


        If pTipo Is GetType(Framework.DatosNegocio.Arboles.NodoBaseDN) Then

            Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

            Return mapinst

        End If

        If pTipo Is GetType(Framework.DatosNegocio.Arboles.ColNodosDN) Then

            Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

            Return mapinst

        End If

        If pTipo Is GetType(DatosNegocio.ArrayListValidable) Then

            Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

            Return mapinst

        End If

        'FINZONA: Framework.DatosNegocio ________________________________________________________________


        'ZONA: AMVGDocs_______________________________________________________________________________


        'If (pTipo.FullName.Contains("Nodo")) Then
        '    Beep()
        'End If



        If (pTipo.FullName.Contains("Framework.DatosNegocio.Arboles.INodoTDN`1[[DatosNegocioTest.HojaDeNodoDeT")) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If




        If (pTipo.FullName.Contains("Framework.DatosNegocio.Arboles.INodoTDN`1[[AmvDocumentosDN.TipoEntNegoioDN")) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If



        If (pTipo.FullName.Contains("Framework.DatosNegocio.Arboles.ColINodoTDN`1[[AmvDocumentosDN.TipoEntNegoioDN, AmvDocumentosDN")) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            Return mapinst
        End If



        'Framework.DatosNegocio.Arboles.ColINodoTDN`1[[AmvDocumentosDN.TipoEntNegoioDN, AmvDocumentosDN
        'Framework.DatosNegocio.Arboles.INodoTDN`1[[AmvDocumentosDN.TipoEntNegoioDN
        'AmvDocumentosDN.NodoTipoEntNegoioDN





        If pTipo Is GetType(AmvDocumentosDN.NodoTipoEntNegoioDN) Then

            Dim mapinstSub As New InfoDatosMapInstClaseDN
            Dim alentidades As New ArrayList

            Dim lista As List(Of ElementosDeEnsamblado)


            lista = New List(Of ElementosDeEnsamblado)
            lista.Add(New ElementosDeEnsamblado("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
            VincularConClase("mPadre", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)


            'lista = New List(Of ElementosDeEnsamblado)
            'lista.Add(New ElementosDeEnsamblado("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
            'VincularConClase("mHijos", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)

            VincularConClase("mHijos", New ElementosDeEnsamblado("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)



            'mapinstSub = New InfoDatosMapInstClaseDN
            'alentidades = New ArrayList

            'alentidades.Add(New VinculoClaseDN("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
            'mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mHijos"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            'campodatos.MapSubEntidad = mapinstSub


            Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
            Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

            Return mapinst
        End If

        If pTipo Is GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN) Then

            'Me.MapearClase("mDatos", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
            Me.MapearClase("mDatos", CampoAtributoDN.NoProcesar, campodatos, mapinst)

            Return mapinst

        End If


        If pTipo Is GetType(Framework.Mensajeria.GestorMensajeriaDN.CausaDN) Then

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCausa"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

            Return mapinst
        End If
        ' Framework.Mensajeria.GestorMensajeriaDN.IDestinoDN()
        If pTipo Is GetType(Framework.Mensajeria.GestorMensajeriaDN.IDestinoDN) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(Framework.Mensajeria.GestorMails.DN.DestinoMailDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If

        If pTipo Is GetType(Framework.Mensajeria.GestorMensajeriaDN.DatosMensajeDN) Then

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mDatos"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

            Return mapinst
        End If

        If pTipo Is GetType(Framework.Mensajeria.GestorMails.DN.SobreDN) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mMensaje"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mXmlMensaje"
            campodatos.TamañoCampo = 2000

            Return mapinst
        End If

        'ZONA: Gestión de talones       _________________________________________________________________


        If pTipo Is GetType(FN.GestionPagos.DN.PagoDN) Then

            mapinst.ColTiposTrazas = New List(Of System.Type)
            mapinst.ColTiposTrazas.Add(GetType(FN.GestionPagos.DN.PagoTrazaDN))

            Return mapinst
        End If


        If pTipo Is GetType(FN.GestionPagos.DN.NotificacionPagoDN) Then


            Dim alentidades As ArrayList
            Dim mapSubInst As InfoDatosMapInstClaseDN

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            mapSubInst.NombreCompleto = GetType(Framework.Usuarios.DN.PrincipalDN).FullName
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Usuarios.DN.PrincipalDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mSujeto"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst
        End If

        If pTipo Is GetType(FN.GestionPagos.DN.TalonDocumentoDN) Then

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNumeroSerie"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

            Return mapinst
        End If

        If pTipo Is GetType(FN.GestionPagos.DN.ContenedorRTFDN) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mArrayString"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

            Return mapinst
        End If

        If pTipo Is GetType(FN.GestionPagos.DN.OrigenDN) Then
            mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlOrigenDN ADD CONSTRAINT tlOrigenDNTipoEntidadOrigenDN UNIQUE  (IDEntidad,idTipoEntidadOrigen)"))
            Return mapinst

        End If

        If pTipo Is GetType(FN.GestionPagos.DN.ContenedorImagenDN) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mImagen"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

            Return mapinst
        End If

        If pTipo Is GetType(FN.GestionPagos.DN.ConfiguracionImpresionTalonDN) Then

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mFuente"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mConfigPagina"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mFirma"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)


            Return mapinst
        End If


        If pTipo Is GetType(Framework.Procesos.ProcesosDN.OperacionRealizadaDN) Then
            Dim alentidades As ArrayList
            Dim mapSubInst As InfoDatosMapInstClaseDN
            ''''''''''''''''''''''

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(PrincipalDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mSujetoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst


            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mObjetoIndirectoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mObjetoDirectoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            campodatos.MapSubEntidad = mapSubInst

            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.FicheroTransferenciaDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mObjetoIndirectoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst
        End If
        'FINZONA: Gestión de talones    _________________________________________________________________


        'ZONA: EmpresaDN ________________________________________________________________


        If pTipo Is GetType(FN.Localizaciones.DN.EntidadFiscalGenericaDN) Then

            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCifNif"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

            Return mapinst
        End If


        If pTipo Is GetType(FN.Empresas.DN.EmpresaDN) Then
            Dim mapSubInst As New InfoDatosMapInstClaseDN()
            Dim alentidades As ArrayList

            mapSubInst = New InfoDatosMapInstClaseDN()
            alentidades = New ArrayList()
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaFiscalDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Personas.DN.PersonaFiscalDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mEntidadFiscal"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlEmpresaDN ADD CONSTRAINT tlEmpresaDNEmpFiscal UNIQUE  (CIFNIF)"))
            Return mapinst

        End If

        If pTipo Is GetType(FN.Empresas.DN.EmpresaFiscalDN) Then
            Dim mapSubInst As New InfoDatosMapInstClaseDN()

            ' mapeado de la clase referida por el campo
            mapSubInst.NombreCompleto = GetType(FN.Localizaciones.DN.CifDN).FullName

            ParametrosGeneralesNoProcesar(mapSubInst)

            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapSubInst
            campodatos.NombreCampo = "mCodigo"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


            ' FIN    mapeado de la clase referida por el campo ******************

            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mCif"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst
        End If

        If pTipo Is GetType(FN.Empresas.DN.EmpleadoDN) Then
            mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlEmpleadoDN ADD CONSTRAINT tlEmpleadoDNPersonaEmpresa UNIQUE  (CIFNIFEmpresa,NIFPersona,Periodo_FFinal)"))
            Return mapinst

        End If

        If pTipo Is GetType(FN.Empresas.DN.EmpleadoYPuestosRDN) Then
            Dim alentidades As New ArrayList()
            Dim mapSubInst As New InfoDatosMapInstClaseDN()

            alentidades.Add(New VinculoClaseDN("EmpresasDN", GetType(FN.Empresas.DN.HuellaCacheEmpleadoYPuestosRDN).FullName))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ActualizarClasesHuella) = alentidades

            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mGUIDEmpleado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst
        End If

        'FINZONA: EmpresaDN ________________________________________________________________

        'ZONA: PersonaDN ________________________________________________________________

        'Dim pp As PersonasDN.PersonaDN
        'Dim l As LocalizacionesDN.NifDN

        If pTipo Is GetType(FN.Personas.DN.PersonaDN) Then
            Dim mapSubInst As New InfoDatosMapInstClaseDN

            mapSubInst.NombreCompleto = GetType(FN.Localizaciones.DN.NifDN).FullName

            ParametrosGeneralesNoProcesar(mapSubInst)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNIF"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst
        End If

        'FINZONA: PersonaDN ________________________________________________________________


        'ZONA: localizaciones ________________________________________________________________


        'pTipo Is GetType(FN.Localizaciones.DN.DireccionNoUnicaDN  )

        If pTipo Is GetType(FN.Localizaciones.DN.IDatoContactoDN) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(FN.Localizaciones.DN.DireccionNoUnicaDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If

        If pTipo Is GetType(FN.Localizaciones.DN.CodigoPostalDN) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNombre"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

            Return mapinst
        End If

        If pTipo Is GetType(FN.Localizaciones.DN.DatosContactoEntidadDN) Then
            Dim mapSubInst As InfoDatosMapInstClaseDN
            Dim alentidades As ArrayList

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(FN.Personas.DN.PersonaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mEntidadReferida"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst
        End If



        If (pTipo Is GetType(FN.Localizaciones.DN.IEntidadFiscalDN)) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(FN.Personas.DN.PersonaFiscalDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaFiscalDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If



        'FINZONA: localizaciones(________________________________________________________________)


        'ZONA: Procesos ________________________________________________________________________



        'If (pTipo Is GetType(Framework.Procesos. .ClientedeFachadaDN)) Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mNombre"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

        '    Return mapinst
        'End If

        'If (pTipo.FullName = "Framework.Procesos.ProcesosDN.GrupoDTSDN") Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mValor"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

        '    Return mapinst
        'End If

        'If (pTipo.FullName = "Framework.Procesos.ProcesosDN.GrupoColDN") Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mValor"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

        '    Return mapinst
        'End If


        'FINZONA: Procesos _____________________________________________________________________


        'ZONA: FinancieroDN ________________________________________________________________

        If pTipo.FullName = GetType(FN.Financiero.DN.CuentaBancariaDN).FullName Then
            Dim mapSubInst As InfoDatosMapInstClaseDN

            ' mapeado de la clase referida por el campo IBAN
            mapSubInst = New InfoDatosMapInstClaseDN()
            mapSubInst.NombreCompleto = GetType(FN.Financiero.DN.IBANDN).FullName

            ParametrosGeneralesNoProcesar(mapSubInst)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIBAN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

            campodatos.MapSubEntidad = mapSubInst

            ' mapeado de la clase referida por el campo CCC
            mapSubInst = New InfoDatosMapInstClaseDN()
            mapSubInst.NombreCompleto = GetType(FN.Financiero.DN.CCCDN).FullName

            ParametrosGeneralesNoProcesar(mapSubInst)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mCCC"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

            campodatos.MapSubEntidad = mapSubInst

            Return mapinst

        End If


        'FINZONA: EmpresaDN ________________________________________________________________

        'FNZONA: AMVGDocs____________________________________________________________________________


        '   If (pTipo.FullName = "Framework.Mensajeria.GestorMensajeriaDN.IDestinoDN") pTipo is   Then
        If (pTipo Is GetType(Framework.Mensajeria.GestorMensajeriaDN.IDestinoDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Mensajeria.GestorMails.DN.DestinoMailDN)))

            'alentidades.Add(New VinculoClaseDN("GestorMails", "Framework.Mensajeria.GestorMails.DN.DestinoMailDN"))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If




        'FNZONA: PAGOS____________________________________________________________________________


        If (pTipo.FullName = GetType(FN.GestionPagos.DN.IImporteDebidoDN).FullName) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            Return mapinst
        End If


        'If (pTipo.FullName = GetType(FN.GestionPagos.DN.IOrigenIImporteDebidoDN).FullName) Then
        '    Dim alentidades As New ArrayList

        '    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.OrigenIdevManualDN)))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


        '    Return mapinst
        'End If


        ' ojo esto es una mera prueba
        If (pTipo.FullName = GetType(FN.GestionPagos.DN.LiquidacionPagoDN).FullName) Then


            Dim alentidades As ArrayList
            Dim mapSubInst As InfoDatosMapInstClaseDN

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))

            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mColIEntidadDN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst




        End If





        Return mapinst

    End Function

    Private Sub MapearClase(ByVal pCampoAMapear As String, ByVal pFormatoDeMapeado As CampoAtributoDN, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN)

        pCampoDatos = New InfoDatosMapInstCampoDN
        pCampoDatos.InfoDatosMapInstClase = pMapInst
        pCampoDatos.NombreCampo = pCampoAMapear
        pCampoDatos.ColCampoAtributo.Add(pFormatoDeMapeado)

    End Sub

    Private Sub VincularConClase(ByVal mCampoAMapear As String, ByVal pElementosDeEmsamblado As List(Of ElementosDeEnsamblado), ByVal mFormatoDeMapeado As CampoAtributoDN, ByRef pAlEntidades As ArrayList, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN, ByRef pMapInstSub As InfoDatosMapInstClaseDN)

        pMapInstSub = New InfoDatosMapInstClaseDN
        pAlEntidades = New ArrayList

        Dim pElemento As ElementosDeEnsamblado
        For Each pElemento In pElementosDeEmsamblado
            pAlEntidades.Add(New VinculoClaseDN(pElemento.Ensamblado, pElemento.Clase))
        Next

        pMapInstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = pAlEntidades

        pCampoDatos = New InfoDatosMapInstCampoDN
        pCampoDatos.InfoDatosMapInstClase = pMapInst
        pCampoDatos.NombreCampo = mCampoAMapear
        pCampoDatos.ColCampoAtributo.Add(mFormatoDeMapeado)
        pCampoDatos.MapSubEntidad = pMapInstSub

    End Sub

    Private Sub VincularConClase(ByVal mCampoAMapear As String, ByVal pElementoDeEmsamblado As ElementosDeEnsamblado, ByVal mFormatoDeMapeado As CampoAtributoDN, ByRef pAlEntidades As ArrayList, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN, ByRef pMapInstSub As InfoDatosMapInstClaseDN)

        pMapInstSub = New InfoDatosMapInstClaseDN
        pAlEntidades = New ArrayList

        pAlEntidades.Add(New VinculoClaseDN(pElementoDeEmsamblado.Ensamblado, pElementoDeEmsamblado.Clase))
        pMapInstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = pAlEntidades

        pCampoDatos = New InfoDatosMapInstCampoDN
        pCampoDatos.InfoDatosMapInstClase = pMapInst
        pCampoDatos.NombreCampo = mCampoAMapear
        pCampoDatos.ColCampoAtributo.Add(mFormatoDeMapeado)
        pCampoDatos.MapSubEntidad = pMapInstSub

    End Sub

    Private Sub ParametrosGeneralesNoProcesar(ByRef mapeadoSubInst As InfoDatosMapInstClaseDN)
        Dim infoDatosMap As InfoDatosMapInstCampoDN

        infoDatosMap = New InfoDatosMapInstCampoDN()
        infoDatosMap.InfoDatosMapInstClase = mapeadoSubInst
        infoDatosMap.NombreCampo = "mID"
        infoDatosMap.Nombre = infoDatosMap.NombreCampo
        infoDatosMap.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        infoDatosMap = New InfoDatosMapInstCampoDN
        infoDatosMap.InfoDatosMapInstClase = mapeadoSubInst
        infoDatosMap.NombreCampo = "mFechaModificacion"
        infoDatosMap.Nombre = infoDatosMap.NombreCampo
        infoDatosMap.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        infoDatosMap = New InfoDatosMapInstCampoDN
        infoDatosMap.InfoDatosMapInstClase = mapeadoSubInst
        infoDatosMap.NombreCampo = "mBaja"
        infoDatosMap.Nombre = infoDatosMap.NombreCampo
        infoDatosMap.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        infoDatosMap = New InfoDatosMapInstCampoDN
        infoDatosMap.InfoDatosMapInstClase = mapeadoSubInst
        infoDatosMap.NombreCampo = "mNombre"
        infoDatosMap.Nombre = infoDatosMap.NombreCampo
        infoDatosMap.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        infoDatosMap = New InfoDatosMapInstCampoDN
        infoDatosMap.InfoDatosMapInstClase = mapeadoSubInst
        infoDatosMap.NombreCampo = "mGUID"
        infoDatosMap.Nombre = infoDatosMap.NombreCampo
        infoDatosMap.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        infoDatosMap = New InfoDatosMapInstCampoDN
        infoDatosMap.InfoDatosMapInstClase = mapeadoSubInst
        infoDatosMap.NombreCampo = "mHashValores"
        infoDatosMap.Nombre = infoDatosMap.NombreCampo
        infoDatosMap.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        infoDatosMap = New InfoDatosMapInstCampoDN
        infoDatosMap.InfoDatosMapInstClase = mapeadoSubInst
        infoDatosMap.NombreCampo = "mToSt"
        infoDatosMap.Nombre = infoDatosMap.NombreCampo
        infoDatosMap.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

    End Sub

End Class


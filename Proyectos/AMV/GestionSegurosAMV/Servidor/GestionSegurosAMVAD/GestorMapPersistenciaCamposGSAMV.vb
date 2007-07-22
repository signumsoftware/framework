Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.TiposYReflexion.DN
Imports Framework.TiposYReflexion.LN
Imports Framework.Usuarios.DN

Public Class GestorMapPersistenciaCamposGSAMV
    Inherits GestorMapPersistenciaCamposLN



    Private Function RecuperarMap_Framework_DatosNegocio(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

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


    End Function
   
    Private Function RecuperarMap_Framework_Tarificador(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


    End Function

    Private Function RecuperarMap_Framework_Mensajeria(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        If pTipo Is GetType(Framework.Mensajeria.GestorMensajeriaDN.IDestinoDN) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(Framework.Mensajeria.GestorMails.DN.DestinoMailDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If

        If pTipo Is GetType(Framework.Mensajeria.GestorMensajeriaDN.CausaDN) Then

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCausa"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

            Return mapinst
        End If

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

    End Function

    Private Function RecuperarMap_Framework_Ficheros(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        If (pTipo Is GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)) Then

            'Me.MapearClase("mDatos", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
            Me.MapearClase("mDatos", CampoAtributoDN.NoProcesar, campodatos, mapinst)

            Return mapinst

        End If

        If pTipo Is GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN) Then

            'Me.MapearClase("mDatos", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
            Me.MapearClase("mDatos", CampoAtributoDN.NoProcesar, campodatos, mapinst)

            Return mapinst

        End If

        If (pTipo Is GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mDatos"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


            Return mapinst
        End If
    End Function

    Private Function RecuperarMap_Framework_Cuestionario(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If (pTipo Is GetType(Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Cuestionario.CuestionarioDN.ValorCaracteristicaFechaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Cuestionario.CuestionarioDN.ValorTextoCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Cuestionario.CuestionarioDN.ValorBooleanoCaracterisitcaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ValorMCNDCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ValorCPCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ValorDireccionNoUnicaCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ValorLocalidadCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ValorMarcaCaracterisitcaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ValorModeloCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ValorSexoCaracteristicaDN)))

            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst

        End If

    End Function

    Private Function RecuperarMap_Framework_Operaciones(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)) Then
            Dim alentidades As New ArrayList
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mToSt"
            campodatos.TamañoCampo = 1200

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNombre"
            campodatos.TamañoCampo = 1200

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCacheado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            Return mapinst
        End If


        If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.IOperacionSimpleDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst

        End If

        If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.ISuministradorValorDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.SumiValFijoDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PrimabaseRVSVDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ModuladorRVSVDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ImpuestoRVSVDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ComisionRVSVDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.FraccionamientoRVSVDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.BonificacionRVSVDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst

        End If

        'ImpuestoRVSVDN

        If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.IOperadorDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.SumaOperadorDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.TruncarOperadorDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.RedondeoOperadorDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst

        End If

    End Function

    Private Function RecuperarMap_Framework_Procesos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If (pTipo Is GetType(Framework.Procesos.ProcesosDN.OperacionRealizadaDN)) Then
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


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mObjetoDirectoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.FicheroTransferenciaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(PrincipalDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.TarifaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PresupuestoDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mObjetoIndirectoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst


            Return mapinst
        End If
    End Function

    Private Function RecuperarMap_Framework_TiposYReflexion(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If (pTipo Is GetType(Framework.TiposYReflexion.DN.VinculoClaseDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNombreClase"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

            Return mapinst
        End If

        Return Nothing
    End Function

    Private Function RecuperarMap_MNavegacionDatosDN(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing
        If (pTipo Is GetType(MNavegacionDatosDN.EntidadNavDN)) Then
            Dim alentidades As New ArrayList

            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mVinculoClase"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlEntidadNavDN ADD CONSTRAINT tlEntidadNavDNvc UNIQUE  (idVinculoClase)"))

            Return mapinst
        End If


        Return Nothing
    End Function

    Private Function RecuperarMap_Framework_Usuarios(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If pTipo Is GetType(Framework.Usuarios.DN.DatosIdentidadDN) Then

            Me.MapearClase("mHashClave", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
            Me.MapearClase("mNick", CampoAtributoDN.UnicoEnFuenteDatosoNulo, campodatos, mapinst)

            Return mapinst
        End If

        If pTipo Is GetType(Framework.Usuarios.DN.UsuarioDN) Then
            Dim mapinstSub As New InfoDatosMapInstClaseDN
            Dim alentidades As New ArrayList

            Me.VincularConClase("mHuellaEntidadUserDN", New ElementosDeEnsamblado("AmvDocumentosDN", "AmvDocumentosDN.HuellaOperadorDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
            Me.VincularConClase("mEntidadUser", New ElementosDeEnsamblado("EmpresasDN", GetType(FN.Empresas.DN.HuellaCacheEmpleadoYPuestosRDN).FullName), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

            Return mapinst
        End If

        If pTipo Is GetType(Framework.Usuarios.DN.RolDN) Then

            Me.MapearClase("mNombre", CampoAtributoDN.UnicoEnFuenteDatosoNulo, campodatos, mapinst)

            Return mapinst
        End If


        If pTipo Is GetType(Framework.Usuarios.DN.PrincipalDN) Then
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


    End Function

    Private Function RecuperarMap_FN_RiesgosVehiculos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.MatriculaDN)) Then

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorMatricula"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlMatriculaDN ADD CONSTRAINT MatriculaDNvm UNIQUE  (ValorMatricula)"))

            Return mapinst

        End If



        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN)) Then


            Dim alentidades As ArrayList
            Dim mapSubInst As InfoDatosMapInstClaseDN

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
            alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoCoberturaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN)))

            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mColOrigenes"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst

        End If


        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.CategoriaDN)) Then
            'Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNombre"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


            Return mapinst
        End If


        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.MarcaDN)) Then
            ' Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNombre"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ModeloDN)) Then

            mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlModeloDN ADD CONSTRAINT tlModeloDNNombreidMarca UNIQUE  (Nombre,idMarca)"))
            Return mapinst
        End If

        If (pTipo Is GetType(Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN)) Then
            Dim mapSubInst As New InfoDatosMapInstClaseDN()

            mapSubInst.NombreCompleto = GetType(FN.Localizaciones.DN.NifDN).FullName
            ParametrosGeneralesNoProcesar(mapSubInst)
            ParametrosGeneralesNoProcesar(mapSubInst)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mPlazo"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.PrimabaseRVSVDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCacheado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ImpuestoRVSVDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCacheado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ComisionRVSVDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCacheado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ModuladorRVSVDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCacheado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.FraccionamientoRVSVDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCacheado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.BonificacionRVSVDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCacheado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.BonificacionRVDN)) Then
            Dim mapinstSub As InfoDatosMapInstClaseDN

            mapinstSub = New InfoDatosMapInstClaseDN
            mapinstSub.NombreCompleto = GetType(Framework.DatosNegocio.IntvaloNumericoDN).FullName
            ParametrosGeneralesNoProcesar(mapinstSub)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.MapSubEntidad = mapinstSub
            campodatos.NombreCampo = "mIntervaloNumerico"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

            Return mapinst
        End If

        'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ModuladorRVDN)) Then
        '    Dim alentidades As New ArrayList

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mValorCacheado"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
        '    Return mapinst
        'End If

    End Function

    Private Function RecuperarMap_FN_Trabajos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing



        If (pTipo Is GetType(FN.Trabajos.DN.AsignacionTrabajoDN)) Then

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mEntidadAsignada"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If


        Return Nothing
    End Function

    Private Function RecuperarMap_FN_Polizas(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If (pTipo Is GetType(FN.Seguros.Polizas.DN.TomadorDN)) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIdentificacionFiscal"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo) ' no se debieran admitir nulos

            Return mapinst
        End If

        If (pTipo Is GetType(FN.Seguros.Polizas.DN.IRiesgoDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.RiesgoMotorDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst
        End If


        If (pTipo Is GetType(FN.Seguros.Polizas.DN.TarifaDN)) Then
            Dim alentidades As ArrayList
            Dim mapinstSub As InfoDatosMapInstClaseDN

            ''''''''''''''''''''
            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mRiesgo"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)

            'alentidades = New ArrayList
            'mapinstSub = New InfoDatosMapInstClaseDN
            'alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.RiesgoMotorDN)))
            'mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            'campodatos.MapSubEntidad = mapinstSub
            ''''''''''''''''''''

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mDatosTarifa"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)


            alentidades = New ArrayList
            mapinstSub = New InfoDatosMapInstClaseDN
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN)))
            mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            campodatos.MapSubEntidad = mapinstSub
            '''''''''''''''''''

            mapinstSub.NombreCompleto = GetType(Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias).FullName
            ParametrosGeneralesNoProcesar(mapinstSub)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.MapSubEntidad = mapinstSub
            campodatos.NombreCampo = "mAMD"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

            Return mapinst
        End If




    End Function

    Private Function RecuperarMap_AmvDocumentosDN(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing




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


    End Function

    Private Function RecuperarMap_FN_GestionPagos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing



        If (pTipo Is GetType(FN.GestionPagos.DN.PlazoEfectoDN)) Then

            '''''''''''''''''''
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mPlazoEjecucion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            '''''''''''''''''''

            Return mapinst
        End If

        If (pTipo Is GetType(FN.GestionPagos.DN.CondicionesPagoDN)) Then

            '''''''''''''''''''
            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mTitulares"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mPlazoEjecucion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            '''''''''''''''''''

            Return mapinst
        End If




        ' Para la prueba de mapeado en la interface
        If (pTipo Is GetType(FN.GestionPagos.DN.PagoDN)) Then
            Dim alentidades As New ArrayList

            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mDeudor"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mDestinatario"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mIImporteDebidoDN"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)



            Return mapinst
        End If


        If (pTipo Is GetType(FN.GestionPagos.DN.NotificacionPagoDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mSujeto"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


            Return mapinst
        End If




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

        If (pTipo Is GetType(FN.GestionPagos.DN.OrigenIdevBaseDN)) Then


            'Dim alentidades As ArrayList
            'Dim mapSubInst As InfoDatosMapInstClaseDN

            'mapSubInst = New InfoDatosMapInstClaseDN
            'alentidades = New ArrayList

            'mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
            'alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoCoberturaDN)))
            'alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))
            'alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN)))

            'mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mColOrigenes"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            'campodatos.MapSubEntidad = mapSubInst


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mColIEntidad"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst

        End If

        ' ojo esto es una mera prueba
        If (pTipo Is GetType(FN.GestionPagos.DN.LiquidacionPagoDN)) Then


            Dim alentidades As ArrayList
            Dim mapSubInst As InfoDatosMapInstClaseDN

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))

            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mColIEntidad"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst

        End If

        If (pTipo Is GetType(FN.GestionPagos.DN.IImporteDebidoDN)) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            'alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN)))
            'mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            Return mapinst
        End If


        If (pTipo Is GetType(FN.GestionPagos.DN.IOrigenIImporteDebidoDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.OrigenIdevBaseDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            Return mapinst
        End If





    End Function

    Private Function RecuperarMap_FN_Empresas(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        If pTipo Is GetType(FN.Empresas.DN.EntidadColaboradoraDN) Then
            Dim mapSubInst As New InfoDatosMapInstClaseDN()
            Dim alentidades As ArrayList

            mapSubInst = New InfoDatosMapInstClaseDN()
            alentidades = New ArrayList()
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.AgrupacionDeEmpresasDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.SedeEmpresaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpleadoDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mEntidadAsociada"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            mapSubInst = New InfoDatosMapInstClaseDN()
            alentidades = New ArrayList()
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.DatosColaboradorDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mDatosAdicionales"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlEntidadColaboradoraDN ADD CONSTRAINT tlEntidadColaboradoraDNUnicoCodColab UNIQUE  (CodigoColaborador)"))

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

    End Function


    Private Function RecuperarMap_FN_Financiero(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing




        If (pTipo Is GetType(FN.Financiero.DN.CCCDN)) Then
            Dim alentidades As ArrayList


            '''''''''''''''''''
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mTitulares"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            '''''''''''''''''''




            Return mapinst
        End If

        '' TODO: alex pruba a comentar esto y crear el entorno el mensaje del motor ad no es muy explicativo y no sabes de que campo de que clase proviene la referencia a la interface

        'If (pTipo Is GetType(FN.Financiero.DN.CuentaBancariaDN)) Then
        '    Dim alentidades As New ArrayList

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mTitulares"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


        '    Return mapinst
        'End If


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







    End Function

    Private Function RecuperarMap_FN_Personas(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing
        If pTipo Is GetType(FN.Personas.DN.PersonaDN) Then
            Dim mapSubInst As New InfoDatosMapInstClaseDN

            ' mapeado de la clase referida por el campo
            mapSubInst.NombreCompleto = GetType(FN.Localizaciones.DN.NifDN).FullName
            ParametrosGeneralesNoProcesar(mapSubInst)
            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapSubInst
            campodatos.NombreCampo = "mCodigo"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


            ' FIN    mapeado de la clase referida por el campo ******************

            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNIF"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst
        End If

    End Function

    Private Function RecuperarMap_FN_Localizaciones(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing



        If (pTipo Is GetType(FN.Localizaciones.DN.IDatoContactoDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(FN.Localizaciones.DN.EmailDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Localizaciones.DN.TelefonoDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Localizaciones.DN.DireccionNoUnicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Localizaciones.DN.PaginaWebDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Localizaciones.DN.ContactoGenericoDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst

        End If

        If pTipo Is GetType(FN.Localizaciones.DN.EntidadFiscalGenericaDN) Then

            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCifNif"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

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


        Return Nothing

    End Function



    Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        mapinst = RecuperarMap_Framework_Ficheros(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If


        mapinst = RecuperarMap_AmvDocumentosDN(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If



        mapinst = RecuperarMap_FN_Polizas(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = RecuperarMap_FN_Financiero(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If


        mapinst = Me.RecuperarMap_FN_Empresas(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_FN_GestionPagos(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_FN_Localizaciones(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_FN_Personas(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_FN_Polizas(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_FN_RiesgosVehiculos(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_Framework_Cuestionario(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_Framework_Ficheros(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_Framework_Operaciones(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_Framework_Procesos(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_Framework_Tarificador(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_Framework_Usuarios(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If


        mapinst = Me.RecuperarMap_Framework_DatosNegocio(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If



        mapinst = Me.RecuperarMap_FN_Trabajos(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If


        mapinst = Me.RecuperarMap_Framework_Mensajeria(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If










        Return Nothing

    End Function


    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

End Class


'FINZONA: Financiero ________________________________________________________________


'If (pTipo Is GetType(FN.Seguros.Polizas.DN.PolizaDN)) Then
'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mColaboradorComercial"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'    Return mapinst
'End If

'' Para la prueba de mapeado en la interface
'If (pTipo Is GetType(FN.GestionPagos.DN.PagoDN)) Then
'    Dim alentidades As New ArrayList

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mDeudor"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mDestinatario"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mIImporteDebidoDN"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'    Return mapinst
'End If

'If (pTipo Is GetType(FN.Financiero.DN.CCCDN)) Then

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mTitulares"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'    Return mapinst
'End If

'If (pTipo Is GetType(FN.Financiero.DN.CuentaBancariaDN)) Then

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mTitulares"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'    Return mapinst
'End If

'If (pTipo Is GetType(FN.GestionPagos.DN.NotificacionPagoDN)) Then

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mSujeto"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'    Return mapinst
'End If

''---------------------------------------------------------------------------------------------------------------------

''ZONA: PersonaDN ________________________________________________________________

'If pTipo Is GetType(FN.Personas.DN.PersonaDN) Then
'    Dim mapSubInst As New InfoDatosMapInstClaseDN

'    ' mapeado de la clase referida por el campo
'    mapSubInst.NombreCompleto = GetType(FN.Localizaciones.DN.CifDN).FullName
'    ParametrosGeneralesNoProcesar(mapSubInst)
'    campodatos = New InfoDatosMapInstCampoDN()
'    campodatos.InfoDatosMapInstClase = mapSubInst
'    campodatos.NombreCampo = "mCodigo"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


'    ' FIN    mapeado de la clase referida por el campo ******************

'    campodatos = New InfoDatosMapInstCampoDN()
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mNIF"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
'    campodatos.MapSubEntidad = mapSubInst



'    Return mapinst
'End If

' ''FINZONA: PersonaDN ________________________________________________________________
' ''ZONA: EmpresaDN ________________________________________________________________


'If pTipo Is GetType(FN.Empresas.DN.EmpresaDN) Then
'    Dim mapSubInst As New InfoDatosMapInstClaseDN()
'    Dim alentidades As ArrayList

'    mapSubInst = New InfoDatosMapInstClaseDN()
'    alentidades = New ArrayList()
'    alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaFiscalDN)))
'    alentidades.Add(New VinculoClaseDN(GetType(FN.Personas.DN.PersonaFiscalDN)))
'    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mEntidadFiscal"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'    campodatos.MapSubEntidad = mapSubInst

'    mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlEmpresaDN ADD CONSTRAINT tlEmpresaDNEmpFiscal UNIQUE  (CIFNIF)"))

'    Return mapinst

'End If

'If pTipo Is GetType(FN.Empresas.DN.EmpresaFiscalDN) Then
'    Dim mapSubInst As New InfoDatosMapInstClaseDN()

'    ' mapeado de la clase referida por el campo
'    mapSubInst.NombreCompleto = GetType(FN.Localizaciones.DN.CifDN).FullName

'    ParametrosGeneralesNoProcesar(mapSubInst)

'    campodatos = New InfoDatosMapInstCampoDN()
'    campodatos.InfoDatosMapInstClase = mapSubInst
'    campodatos.NombreCampo = "mCodigo"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


'    ' FIN    mapeado de la clase referida por el campo ******************

'    campodatos = New InfoDatosMapInstCampoDN()
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mCif"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
'    campodatos.MapSubEntidad = mapSubInst


'    Return mapinst
'End If

'If pTipo Is GetType(FN.Empresas.DN.EmpleadoDN) Then
'    mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlEmpleadoDN ADD CONSTRAINT tlEmpleadoDNPersonaEmpresa UNIQUE  (CIFNIFEmpresa,NIFPersona,Periodo_FFinal)"))
'    Return mapinst

'End If

'If pTipo Is GetType(FN.Empresas.DN.EmpleadoYPuestosRDN) Then
'    Dim alentidades As New ArrayList()
'    Dim mapSubInst As New InfoDatosMapInstClaseDN()

'    alentidades.Add(New VinculoClaseDN("EmpresasDN", GetType(FN.Empresas.DN.HuellaCacheEmpleadoYPuestosRDN).FullName))
'    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ActualizarClasesHuella) = alentidades

'    campodatos = New InfoDatosMapInstCampoDN()
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mGUIDEmpleado"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
'    campodatos.MapSubEntidad = mapSubInst

'    Return mapinst
'End If

' ''FINZONA: EmpresaDN ________________________________________________________________


'If (pTipo Is GetType(FN.GestionPagos.DN.OrigenIdevBaseDN)) Then


'    'Dim alentidades As ArrayList
'    'Dim mapSubInst As InfoDatosMapInstClaseDN

'    'mapSubInst = New InfoDatosMapInstClaseDN
'    'alentidades = New ArrayList

'    'mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
'    'alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoCoberturaDN)))
'    'alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))
'    'alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN)))

'    'mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


'    'campodatos = New InfoDatosMapInstCampoDN
'    'campodatos.InfoDatosMapInstClase = mapinst
'    'campodatos.NombreCampo = "mColOrigenes"
'    'campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'    'campodatos.MapSubEntidad = mapSubInst


'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mColIEntidad"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'    Return mapinst

'End If

'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN)) Then


'    Dim alentidades As ArrayList
'    Dim mapSubInst As InfoDatosMapInstClaseDN

'    mapSubInst = New InfoDatosMapInstClaseDN
'    alentidades = New ArrayList

'    mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
'    alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoCoberturaDN)))
'    alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))
'    alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN)))

'    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mColOrigenes"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'    campodatos.MapSubEntidad = mapSubInst

'    Return mapinst

'End If



'' ojo esto es una mera prueba
'If (pTipo Is GetType(FN.GestionPagos.DN.LiquidacionPagoDN)) Then


'    Dim alentidades As ArrayList
'    Dim mapSubInst As InfoDatosMapInstClaseDN

'    mapSubInst = New InfoDatosMapInstClaseDN
'    alentidades = New ArrayList

'    mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
'    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))

'    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mColIEntidad"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'    campodatos.MapSubEntidad = mapSubInst

'    Return mapinst

'End If


'If (pTipo Is GetType(FN.Localizaciones.DN.IEntidadFiscalDN)) Then
'    Dim alentidades As New ArrayList

'    alentidades.Add(New VinculoClaseDN(GetType(FN.Personas.DN.PersonaFiscalDN)))
'    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'    alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaFiscalDN)))
'    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'    Return mapinst
'End If


'If (pTipo Is GetType(FN.GestionPagos.DN.ApunteImpDDN)) Then


'    Dim alentidades As ArrayList
'    Dim mapSubInst As InfoDatosMapInstClaseDN

'    mapSubInst = New InfoDatosMapInstClaseDN
'    alentidades = New ArrayList

'    mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
'    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))

'    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mAcreedora "
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'    campodatos.MapSubEntidad = mapSubInst

'    Return mapinst

'End If

'If (pTipo Is GetType(FN.GestionPagos.DN.IImporteDebidoDN)) Then
'    Dim alentidades As New ArrayList

'    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
'    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN)))
'    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


'    Return mapinst
'End If


'If (pTipo Is GetType(FN.GestionPagos.DN.IOrigenIImporteDebidoDN)) Then
'    Dim alentidades As New ArrayList

'    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.OrigenIdevBaseDN)))
'    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


'    Return mapinst
'End If


' ''ZONA: UsuarioDN ________________________________________________________________

' ''Mapeado de UsuarioDN, donde la clase mapea sus interfaces, y solo es para ella.

'If pTipo Is GetType(DatosIdentidadDN) Then

'    Me.MapearClase("mHashClave", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

'    Return mapinst
'End If

'If pTipo Is GetType(UsuarioDN) Then
'    Dim mapinstSub As New InfoDatosMapInstClaseDN
'    Dim alentidades As New ArrayList

'    'Me.VincularConClase("mHuellaEntidadUserDN", New ElementosDeEnsamblado("AmvDocumentosDN", "AmvDocumentosDN.HuellaOperadorDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
'    Me.VincularConClase("mEntidadUser", New ElementosDeEnsamblado("EmpresasDN", GetType(FN.Empresas.DN.HuellaCacheEmpleadoYPuestosRDN).FullName), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

'    Return mapinst
'End If

'If pTipo Is GetType(PrincipalDN) Then
'    Me.MapearClase("mClavePropuesta", CampoAtributoDN.NoProcesar, campodatos, mapinst)
'    Return mapinst
'End If

'If pTipo Is GetType(Framework.Usuarios.DN.PermisoDN) Then
'    Me.MapearClase("mDatoRef", CampoAtributoDN.NoProcesar, campodatos, mapinst)
'    Return mapinst
'End If

'If pTipo Is GetType(Framework.Usuarios.DN.TipoPermisoDN) Then
'    Me.MapearClase("mNombre", CampoAtributoDN.UnicoEnFuenteDatosoNulo, campodatos, mapinst)
'    Return mapinst
'End If

' ''FINZONA: UsuarioDN ________________________________________________________________

'If (pTipo Is GetType(Framework.Procesos.ProcesosDN.OperacionRealizadaDN)) Then
'    Dim alentidades As ArrayList
'    Dim mapSubInst As InfoDatosMapInstClaseDN
'    ''''''''''''''''''''''

'    mapSubInst = New InfoDatosMapInstClaseDN
'    alentidades = New ArrayList
'    alentidades.Add(New VinculoClaseDN(GetType(PrincipalDN)))
'    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mSujetoOperacion"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'    campodatos.MapSubEntidad = mapSubInst


'    mapSubInst = New InfoDatosMapInstClaseDN
'    alentidades = New ArrayList

'    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))
'    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mObjetoIndirectoOperacion"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'    campodatos.MapSubEntidad = mapSubInst

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mObjetoDirectoOperacion"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
'    campodatos.MapSubEntidad = mapSubInst

'    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.FicheroTransferenciaDN)))
'    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mObjetoIndirectoOperacion"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'    campodatos.MapSubEntidad = mapSubInst

'    Return mapinst
'End If

'If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)) Then
'    Dim alentidades As New ArrayList
'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mIRecSumiValorLN"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mToSt"
'    campodatos.TamañoCampo = 1200

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mNombre"
'    campodatos.TamañoCampo = 1200

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mValorCacheado"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
'    Return mapinst
'End If


'If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.IOperacionSimpleDN)) Then
'    Dim alentidades As New ArrayList
'    alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)))
'    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
'    Return mapinst

'End If

'If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.ISuministradorValorDN)) Then
'    Dim alentidades As New ArrayList
'    alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)))
'    alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.SumiValFijoDN)))
'    alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PrimabaseRVSVDN)))
'    alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ModuladorRVSVDN)))
'    alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ImpuestoRVSVDN)))
'    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
'    Return mapinst

'End If

''ImpuestoRVSVDN

'If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.IOperadorDN)) Then
'    Dim alentidades As New ArrayList
'    alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.SumaOperadorDN)))
'    alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN)))
'    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
'    Return mapinst

'End If


'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.MarcaDN)) Then
'    Dim alentidades As New ArrayList

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mNombre"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


'    Return mapinst
'End If


'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ModeloDN)) Then
'    'Dim alentidades As New ArrayList

'    'campodatos = New InfoDatosMapInstCampoDN
'    'campodatos.InfoDatosMapInstClase = mapinst
'    'campodatos.NombreCampo = "mNombre"
'    'campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

'    mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlModeloDN ADD CONSTRAINT tlModeloDNNombreidMarca UNIQUE  (Nombre,idMarca)"))
'    Return mapinst
'End If



'If (pTipo Is GetType(FN.Seguros.Polizas.DN.TarifaDN)) Then
'    Dim alentidades As ArrayList
'    Dim mapinstSub As InfoDatosMapInstClaseDN

'    '''''''''''''''''''
'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mRiesgo"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)

'    alentidades = New ArrayList
'    mapinstSub = New InfoDatosMapInstClaseDN
'    alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.RiesgoMotorDN)))
'    mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
'    campodatos.MapSubEntidad = mapinstSub
'    '''''''''''''''''''

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mDatosTarifa"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)


'    alentidades = New ArrayList
'    mapinstSub = New InfoDatosMapInstClaseDN
'    alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN)))
'    mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
'    campodatos.MapSubEntidad = mapinstSub
'    '''''''''''''''''''


'    Return mapinst
'End If


'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.PrimabaseRVSVDN)) Then
'    Dim alentidades As New ArrayList

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mIRecSumiValorLN"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mValorCacheado"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)



'    Return mapinst
'End If


'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ImpuestoRVSVDN)) Then
'    Dim alentidades As New ArrayList

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mIRecSumiValorLN"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mValorCacheado"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'    Return mapinst
'End If


'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ModuladorRVSVDN)) Then
'    Dim alentidades As New ArrayList

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mIRecSumiValorLN"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mValorCacheado"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
'    Return mapinst
'End If

'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ModuladorRVDN)) Then
'    Dim alentidades As New ArrayList

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mValorCacheado"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
'    Return mapinst
'End If







'ZONA: AMVGDocs_______________________________________________________________________________


'If (pTipo.FullName.Contains("Nodo")) Then
'    Beep()
'End If


















' Framework.Mensajeria.GestorMensajeriaDN.IDestinoDN()




'ZONA: PersonaDN ________________________________________________________________



''FINZONA: PersonaDN ________________________________________________________________
''ZONA: EmpresaDN ________________________________________________________________



''FINZONA: EmpresaDN ________________________________________________________________






'If (pTipo Is GetType(FN.GestionPagos.DN.ApunteImpDDN)) Then


'    Dim alentidades As ArrayList
'    Dim mapSubInst As InfoDatosMapInstClaseDN

'    mapSubInst = New InfoDatosMapInstClaseDN
'    alentidades = New ArrayList

'    mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
'    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))

'    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mAcreedora "
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'    campodatos.MapSubEntidad = mapSubInst

'    Return mapinst

'End If

'If (pTipo Is GetType(FN.GestionPagos.DN.ApunteImpDDN)) Then



'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mHuellaIOrigenImpDebDN"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'    Return mapinst

'End If



''ZONA: UsuarioDN ________________________________________________________________

''Mapeado de UsuarioDN, donde la clase mapea sus interfaces, y solo es para ella.



''FINZONA: UsuarioDN ________________________________________________________________








'ZONA: Gestión de talones       _________________________________________________________________



'If pTipo Is GetType(Framework.Procesos.ProcesosDN.OperacionRealizadaDN) Then
'    Dim alentidades As ArrayList
'    Dim mapSubInst As InfoDatosMapInstClaseDN
'    ''''''''''''''''''''''

'    mapSubInst = New InfoDatosMapInstClaseDN
'    alentidades = New ArrayList
'    alentidades.Add(New VinculoClaseDN(GetType(PrincipalDN)))
'    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mSujetoOperacion"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'    campodatos.MapSubEntidad = mapSubInst


'    mapSubInst = New InfoDatosMapInstClaseDN
'    alentidades = New ArrayList

'    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))
'    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mObjetoIndirectoOperacion"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'    campodatos.MapSubEntidad = mapSubInst

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mObjetoDirectoOperacion"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
'    campodatos.MapSubEntidad = mapSubInst

'    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.FicheroTransferenciaDN)))
'    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mObjetoIndirectoOperacion"
'    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'    campodatos.MapSubEntidad = mapSubInst

'    Return mapinst
'End If
'FINZONA: Gestión de talones    _________________________________________________________________












'If (pTipo Is GetType(FN.Empresas.DN.EmpresaDN)) Then
'    Dim alentidades As New ArrayList

'    campodatos = New InfoDatosMapInstCampoDN
'    campodatos.InfoDatosMapInstClase = mapinst
'    campodatos.NombreCampo = "mEntidadFiscal"
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















Imports Microsoft.VisualBasic
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.TiposYReflexion.DN
Imports Framework
Imports Framework.Usuarios.DN

Public Class GestorMapPeristenciaCampos
    Inherits GestorMapPersistenciaCamposLN

    ''TODO: ALEX por implementar: recuperar el mapeado de instanciacion de la fuente de datos para el tipo a procesar
    'Public Overrides Function RecuperarMapPersistenciaCampos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
    '    Dim mapinst As InfoDatosMapInstClaseDN = Nothing
    '    Dim campodatos As InfoDatosMapInstCampoDN = Nothing

    '    mapinst = RecuperarMapPersistenciaCamposPrivado(pTipo)

    '    ' ojo esta modificación se debe aplicar siempre si el tipo hereda de una huella es decir en el metodo que lo llamo
    '    If (TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo)) Then
    '        If mapinst Is Nothing Then
    '            mapinst = New InfoDatosMapInstClaseDN
    '        End If
    '        Me.MapearClase("mEntidadReferidaHuella", CampoAtributoDN.SoloGuardarYNoReferido, campodatos, mapinst)
    '    End If


    '    If TiposYReflexion.LN.InstanciacionReflexionHelperLN.HeredaDe(pTipo, GetType(Framework.DatosNegocio.EntidadTemporalDN)) Then
    '        If mapinst Is Nothing Then
    '            mapinst = New InfoDatosMapInstClaseDN
    '        End If

    '        Dim mapSubInst As New InfoDatosMapInstClaseDN
    '        ' mapeado de la clase referida por el campo
    '        mapSubInst.NombreCompleto = "Framework.DatosNegocio.EntidadTemporalDN"
    '        ParametrosGeneralesNoProcesar(mapSubInst)

    '        campodatos = New InfoDatosMapInstCampoDN
    '        campodatos.InfoDatosMapInstClase = mapinst
    '        campodatos.NombreCampo = "mPeriodo"
    '        campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
    '        campodatos.MapSubEntidad = mapSubInst

    '    End If

    '    Return mapinst
    'End Function



    Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing



        If (pTipo Is GetType(Framework.Procesos.ProcesosDN.TransicionRealizadaDN)) Then

            mapinst.TablaHistoria = "thTransicionRealizadaDN"
            Return mapinst
        End If

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


            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            'alentidades.Add(New VinculoClaseDN(GetType(EntidadDePrueba)))
            alentidades.Add(New VinculoClaseDN(GetType(Usuarios.DN.PrincipalDN)))
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




            Return mapinst
        End If


        '  Usuarios --------------------------------------------------------------------------------

        'If pTipo Is GetType(PrincipalDN) Then
        '    Me.MapearClase("mClavePropuesta", CampoAtributoDN.NoProcesar, campodatos, mapinst)
        '    Return mapinst
        'End If



        'If pTipo Is GetType(Framework.Usuarios.DN.TipoPermisoDN) Then
        '    Me.MapearClase("mNombre", CampoAtributoDN.UnicoEnFuenteDatosoNulo, campodatos, mapinst)
        '    Return mapinst
        'End If

        'If pTipo Is GetType(Framework.Usuarios.DN.PermisoDN) Then
        '    Dim alentidades As ArrayList
        '    Dim mapSubInst As New InfoDatosMapInstClaseDN


        '    mapSubInst = New InfoDatosMapInstClaseDN
        '    alentidades = New ArrayList
        '    alentidades.Add(New VinculoClaseDN(GetType(EntidadDePrueba)))
        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mDatoRef"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst


        '    Return mapinst
        'End If

        'If pTipo Is GetType(DatosIdentidadDN) Then
        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mHashClave"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

        '    Return mapinst
        'End If


        '------------------------------------------------
        'GestorSalida

        'GestorSalidaDN
        If pTipo Is GetType(Framework.GestorSalida.DN.IConfiguracionDocumentoSalida) Then
            Dim al As New ArrayList()
            al.Add(New VinculoClaseDN(GetType(Framework.GestorSalida.DN.ConfiguracionFaxDocumentoSalidaDN)))
            al.Add(New VinculoClaseDN(GetType(Framework.GestorSalida.DN.ConfiguracionImpresionDocumentoSalidaDN)))
            al.Add(New VinculoClaseDN(GetType(Framework.GestorSalida.DN.ConfiguracionMailDocumentoSalidaDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = al
            Return mapinst
        End If

        'Documento (no tiene persistencia en BD, sino en sistema de ficheros)
        If pTipo Is GetType(Framework.GestorSalida.DN.DocumentoSalida) Then
            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mDocumento"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            Return mapinst
        End If



        If pTipo Is GetType(Framework.GestorSalida.DN.ContenedorDescriptorImpresoraDN) Then
            'eventos no se guardan
            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "TrabajosEnColaChanged"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "ErroresChanged"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            'TrabajosEnCola es private
            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mTrabajosEnCola"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            'Errores es private
            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mErrores"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If

        If pTipo Is GetType(Framework.GestorSalida.DN.CategoriaImpresoras) Then
            'eventos no se guardan
            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "TrabajosEnColaChanged"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


            'eventos no se guardan
            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "ErroresChanged"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


            'Errores es private
            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mErrores"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            'Errores es private
            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mTrabajosEnCola"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If
        '----------------------------------------------
        'fin Gestor Salida

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
            ' Dim alentidades As New Generic.List(Of VinculoClaseDN)
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Usuarios.DN.TipoPermisoDN)))

            Me.MapearInterfaceEnCampo(mapinst, "mHuellaEntidadUserDN", alentidades)
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
    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class


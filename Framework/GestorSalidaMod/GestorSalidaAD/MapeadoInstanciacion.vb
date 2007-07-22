Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.TiposYReflexion.DN
Imports Framework


Public Class MapeadoInstanciacion
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
    '        Me.("mEntidadReferidaHuella", CampoAtributoDN.SoloGuardarYNoReferido, campodatos, mapinst)
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

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

    Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As TiposYReflexion.DN.InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

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

        Return Nothing
    End Function
End Class


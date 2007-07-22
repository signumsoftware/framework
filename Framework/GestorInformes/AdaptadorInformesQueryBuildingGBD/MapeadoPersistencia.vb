Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.TiposYReflexion.DN

Public Class MapeadoPersistencia
    Inherits GestorMapPersistenciaCamposLN



    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub


    Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As Framework.TiposYReflexion.DN.InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        ' ficheros
        If pTipo Is GetType(Framework.GestorInformes.ContenedorPlantilla.DN.HuellaFicheroPlantillaDN) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mDatos"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If


        'If pTipo Is GetType(Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN) Then
        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mDatos"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    Return mapinst
        'End If


        'If pTipo Is GetType(MotorBusquedaDN.ICondicionDN) Then
        '    Dim alentidades As New ArrayList

        '    alentidades.Add(New VinculoClaseDN(GetType(MotorBusquedaDN.CondicionDN)))
        '    alentidades.Add(New VinculoClaseDN(GetType(MotorBusquedaDN.CondicionCompuestaDN)))

        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    Return mapinst
        'End If



        If pTipo Is GetType(Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.ITabla) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.TablaPrincipalAIQB)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.TablaRelacionadaAIQB)))

            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If

        If pTipo Is GetType(Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.TablaPrincipalAIQB) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mSQLDefinicion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mParametros"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mfkTabla"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If




        Return Nothing

    End Function


End Class

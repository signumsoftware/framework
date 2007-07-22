#Region "Importaciones"

Imports Framework.TiposYReflexion.DN
Imports System.Collections.Generic
#End Region

Namespace LN
    Public MustInherit Class GestorMapPersistenciaCamposLN

#Region "Metodos"
        Public MustOverride Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN



        Public Overridable Function RecuperarMapPersistenciaCampos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
            Dim mapinst As InfoDatosMapInstClaseDN = Nothing
            Dim campodatos As InfoDatosMapInstCampoDN = Nothing

            mapinst = RecuperarMapPersistenciaCamposPrivado(pTipo)

            ' ojo esta modificación se debe aplicar siempre si el tipo hereda de una huella es decir en el metodo que lo llamo
            If (TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo)) Then
                If mapinst Is Nothing Then
                    mapinst = New InfoDatosMapInstClaseDN
                End If
                Me.MapearCampoSimple(mapinst, "mEntidadReferidaHuella", CampoAtributoDN.SoloGuardarYNoReferido)
            End If


            If TiposYReflexion.LN.InstanciacionReflexionHelperLN.HeredaDe(pTipo, GetType(Framework.DatosNegocio.EntidadTemporalDN)) Then
                If mapinst Is Nothing Then
                    mapinst = New InfoDatosMapInstClaseDN
                End If

                Dim mapSubInst As New InfoDatosMapInstClaseDN
                ' mapeado de la clase referida por el campo
                mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.EntidadTemporalDN).FullName  '"Framework.DatosNegocio.EntidadTemporalDN"
                ParametrosGeneralesNoProcesar(mapSubInst)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mPeriodo"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
                campodatos.MapSubEntidad = mapSubInst

            End If

            Return mapinst
        End Function


        Protected Function MapearInterfaceEnCampo(ByRef pMapInst As InfoDatosMapInstClaseDN, ByVal pCampoAMapear As String, ByVal pLsitaInterfaces As IList(Of VinculoClaseDN)) As InfoDatosMapInstCampoDN


            Dim mapSubInst As InfoDatosMapInstClaseDN
            mapSubInst = New InfoDatosMapInstClaseDN
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = pLsitaInterfaces


            Dim CampoDatos As InfoDatosMapInstCampoDN
            CampoDatos = New InfoDatosMapInstCampoDN
            CampoDatos.InfoDatosMapInstClase = pMapInst
            CampoDatos.NombreCampo = pCampoAMapear
            CampoDatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            CampoDatos.MapSubEntidad = mapSubInst

            Return CampoDatos

        End Function
        Protected Function MapearCampoSimple(ByRef pMapInst As InfoDatosMapInstClaseDN, ByVal pCampoAMapear As String, ByVal pFormatoDeMapeado As CampoAtributoDN) As InfoDatosMapInstCampoDN

            Dim CampoDatos As InfoDatosMapInstCampoDN
            CampoDatos = New InfoDatosMapInstCampoDN
            CampoDatos.InfoDatosMapInstClase = pMapInst
            CampoDatos.NombreCampo = pCampoAMapear
            CampoDatos.ColCampoAtributo.Add(pFormatoDeMapeado)


        End Function


        Protected Sub ParametrosGeneralesNoProcesar(ByRef mapeadoSubInst As InfoDatosMapInstClaseDN)
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



        Protected Sub MapearClase(ByVal pCampoAMapear As String, ByVal pFormatoDeMapeado As CampoAtributoDN, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN)

            pCampoDatos = New InfoDatosMapInstCampoDN
            pCampoDatos.InfoDatosMapInstClase = pMapInst
            pCampoDatos.NombreCampo = pCampoAMapear
            pCampoDatos.ColCampoAtributo.Add(pFormatoDeMapeado)

        End Sub

        Protected Sub VincularConClase(ByVal mCampoAMapear As String, ByVal pElementosDeEmsamblado As List(Of ElementosDeEnsamblado), ByVal mFormatoDeMapeado As CampoAtributoDN, ByRef pAlEntidades As ArrayList, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN, ByRef pMapInstSub As InfoDatosMapInstClaseDN)

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

        Protected Sub VincularConClase(ByVal mCampoAMapear As String, ByVal pElementoDeEmsamblado As ElementosDeEnsamblado, ByVal mFormatoDeMapeado As CampoAtributoDN, ByRef pAlEntidades As ArrayList, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN, ByRef pMapInstSub As InfoDatosMapInstClaseDN)

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


        Public Shared Function TiposQueImplementanInterface(ByVal pPropiedad As System.Reflection.PropertyInfo) As Framework.TiposYReflexion.DN.ColVinculoClaseDN


            Dim pNombreCampo As String = PropiedadDeInstanciaDN.RecuperarNombreCampoVinculado(pPropiedad)


            Return TiposQueImplementanInterface(pNombreCampo, pPropiedad)


        End Function

        Public Shared Function TiposQueImplementanInterface(ByVal pNombreCampo As String, ByVal pPropiedad As System.Reflection.PropertyInfo) As Framework.TiposYReflexion.DN.ColVinculoClaseDN




            Dim campoMapeado As Boolean

            Dim gdmi As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor

            ' recuperar el mapeado de persistencia del tipo en concreto
            Dim DatosMap As InfoDatosMapInstClaseDN = gdmi.RecuperarMapPersistenciaCampos(pPropiedad.ReflectedType)

            Dim colTiposImplementan As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
            If Not DatosMap Is Nothing Then
                ' si hay datos de mapeado para el tipo,
                ' ver  si en concreto lo hay para el campo que estamos tratarndo

                Dim datosMapCampo As InfoDatosMapInstCampoDN = Nothing
                datosMapCampo = DatosMap.GetCampoXNombre(pNombreCampo)

                If Not datosMapCampo Is Nothing Then

                    If (datosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.NoProcesar)) Then
                        Return colTiposImplementan
                    End If


                    If (datosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.InterfaceImplementadaPor)) Then

                        campoMapeado = True
                        ' colTiposImplementan.AddRangeObject(datosMapCampo.Datos.Item(CampoAtributoDN.InterfaceImplementadaPor))
                        If (datosMapCampo.MapSubEntidad IsNot Nothing) Then
                            Dim alDAtosInterface As ArrayList = Nothing
                            alDAtosInterface = datosMapCampo.MapSubEntidad.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor)
                            colTiposImplementan.AddRangeObject(alDAtosInterface)

                        End If
                    End If

                Else
                    ' en este caso no hay informacion para ese campo en concreto


                End If
            End If

            ' como no hay datos para el tipo en concreto verificar si lo hay para la interfae en general

            If Not campoMapeado Then
                colTiposImplementan.AddRangeObject(gdmi.TiposQueImplementanInterface(pPropiedad.PropertyType))
            End If

            Return colTiposImplementan
        End Function


        Public Shared Function TiposQueImplementanInterface(ByVal pTipo As System.Type) As Framework.TiposYReflexion.DN.ColVinculoClaseDN

            ' ojo mucho de esto no debiera estar aqui sino en el gestor de instanciacion
            ' afin de cuentas a qui yo solo quiero recuperar una coleccion de tipos que implementan la interface


            Debug.WriteLine(pTipo.FullName)



            Dim gdmi As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor
            Dim datosClaseInterface As InfoDatosMapInstClaseDN = gdmi.RecuperarMapPersistenciaCampos(pTipo)
            Dim alDAtosInterface As ArrayList = Nothing

            If (datosClaseInterface IsNot Nothing) Then

                alDAtosInterface = datosClaseInterface.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor)
                If alDAtosInterface Is Nothing Then
                    alDAtosInterface = datosClaseInterface.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor)
                End If
            End If

            If (alDAtosInterface IsNot Nothing) Then


                Dim colvc As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
                colvc.AddRangeObject(alDAtosInterface)
                TiposQueImplementanInterface = colvc


            Else
                Throw New ApplicationException("Error: no hay informacion suficiente para resolver esta interface -->" & pTipo.FullName)
            End If



        End Function

#End Region


    End Class


    Public Class GestorMapPersistenciaCamposNULOLN
        Inherits GestorMapPersistenciaCamposLN

#Region "Metodos"


   

        Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As TiposYReflexion.DN.InfoDatosMapInstClaseDN
            Return Nothing
        End Function


#End Region

    End Class

End Namespace

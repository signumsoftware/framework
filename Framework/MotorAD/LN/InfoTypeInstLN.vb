#Region "Importaciones"

Imports System.Collections.Generic
Imports System.Reflection

Imports Framework.TiposYReflexion.DN
Imports Framework.TiposYReflexion.LN

#End Region

Namespace LN
    Public Class InfoTypeInstLN

#Region "Metodos"
        Public Sub VincularMapeadoAIntancia(ByVal pEntidad As Object, ByRef pInstanciacionMap As InfoTypeInstClaseDN)
            pInstanciacionMap.InstanciaPrincipal = pEntidad
        End Sub

        Public Function Generar(ByVal pEntidad As Object, ByVal pCampoContendor As InfoTypeInstCampoRefDN, ByVal id As String) As InfoTypeInstClaseDN
            Dim informacionMpeadoInstanciacion As InfoTypeInstClaseDN

            informacionMpeadoInstanciacion = Me.Generar(pEntidad.GetType, pCampoContendor, id)
            informacionMpeadoInstanciacion.InstanciaPrincipal = pEntidad

            Return informacionMpeadoInstanciacion
        End Function

        'Generar el mapeado de instanciacion para un instancia dada o un tipo
        Public Function Generar(ByVal pTipo As System.Type, ByVal pCampoContenedor As InfoTypeInstCampoRefDN, ByVal id As String) As InfoTypeInstClaseDN
            Dim tipo As System.Type
            Dim Campos As System.Reflection.FieldInfo()

            Dim itic As InfoTypeInstClaseDN
            Dim CamposValID As New List(Of InfoTypeInstCampoValDN)
            Dim CamposVal As New List(Of InfoTypeInstCampoValDN)
            Dim CamposValOriginalesh As New List(Of InfoTypeInstCampoValDN)
            Dim CamposRef As New List(Of InfoTypeInstCampoRefDN)
            Dim CamposRefContenidos As New List(Of InfoTypeInstCampoRefDN)
            Dim CamposRefExteriores As New List(Of InfoTypeInstCampoRefDN)
            Dim VinculosClasesCache As New List(Of VinculoClaseDN)

            Dim mapinst As InfoDatosMapInstClaseDN
            Dim entidad As Object = Nothing
            'Dim colValidable As Framework.DatosNegocio.IValidable
            'Dim validadorTipos As Framework.DatosNegocio.ValidadorTipos
            Dim gMapInstCampos As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor

            'Si el tipo es una coleccion se opera con el tipo para el cual esta fijado

            'If (pTipo.GetInterface("IEnumerable", True) IsNot Nothing) Then

            '    ' puede que se trate de una coleccion validable o de una coleccion generica

            '    If pTipo.IsGenericTypeDefinition Then
            '        'se trata de una coleccion generica
            '        If pTipo.GetGenericArguments.Length > 1 Then
            '            Throw New ApplicationException("La colección tiene demasiados parametros genericos")
            '        End If
            '        tipo = pTipo.GetGenericArguments(0)
            '    Else
            '        ' se trata de una coleccion validable
            '        colValidable = Activator.CreateInstance(pTipo)
            '        validadorTipos = colValidable.Validador
            '        tipo = validadorTipos.Tipo
            '    End If


            'Else
            '    tipo = pTipo
            'End If

            Dim tipoDeFijado As FijacionDeTipoDN
            'If pTipo.Name = "HuellaEntidadTipadaDN`1" Then
            '    tipo = pTipo
            'Else
            '    tipo = InstanciacionReflexionHelperLN.ObtenerTipoFijado(pTipo, tipoDeFijado)
            'End If


            If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo) Then
                tipo = pTipo
            Else
                tipo = InstanciacionReflexionHelperLN.ObtenerTipoFijado(pTipo, tipoDeFijado)
            End If

            itic = New InfoTypeInstClaseDN(tipo)

            'Recuperar el mapeado de instanciacion de la fuente de datos para el tipo a procesar.
            'Es decir: informacion en un repositorio de datos que define modificaciones al algorictmo de instanciacion o los atributos de instanciacion declarados
            mapinst = gMapInstCampos.RecuperarMapPersistenciaCampos(tipo)

            'Se optienen los campos 
            Campos = InstanciacionReflexionHelperLN.RecuperarCampos(tipo)

            'Se separan los campos
            InfoTypeInstLN.SepararCamposEnTablaCamposRelacionados(pCampoContenedor, entidad, Campos, mapinst, CamposValID, CamposValOriginalesh, CamposVal, CamposRef, CamposRefContenidos, CamposRefExteriores, VinculosClasesCache, id)

            'Se convierten los campos en instancias de mapeado para crear la InfoTypeInstClaseDN que define la clase 
            itic.InstanciaPrincipal = Nothing
            If (CamposValID.Count = 0) Then
                itic.IdVal = Nothing

            Else
                itic.IdVal = CamposValID(0)
            End If

            itic.CamposValOriginal = CamposValOriginalesh
            itic.CamposVal = CamposVal
            itic.CamposRef = CamposRef
            itic.CamposRefContenidos = CamposRefContenidos
            itic.CamposRefExteriores = CamposRefExteriores
            itic.VinculosClasesCache = VinculosClasesCache
            Return itic
        End Function

        'Este metodo tiene la funcionalidad de resolver que campos deben incluirse en la tabla para el tipo tratado
        Public Shared Sub SepararCamposEnTablaCamposRelacionados(ByVal pCampoContenedor As IInfoTypeInstCampoDN, ByVal pInstancia As Object, ByVal pCampos As Reflection.FieldInfo(), ByVal pMapeadoInst As InfoDatosMapInstClaseDN, ByRef pCamposValID As List(Of InfoTypeInstCampoValDN), ByRef pCamposValOriginales As List(Of InfoTypeInstCampoValDN), ByRef pCamposVal As List(Of InfoTypeInstCampoValDN), ByRef pCamposRef As List(Of InfoTypeInstCampoRefDN), ByRef pCamposRefEntabla As List(Of InfoTypeInstCampoRefDN), ByRef pCamposRefExternos As List(Of InfoTypeInstCampoRefDN), ByRef pVinculosClasesCache As List(Of VinculoClaseDN), ByVal id As String)
            Dim PrefijoMap As String
            Dim f As System.Reflection.FieldInfo
            Dim info As IInfoTypeInstCampoDN
            Dim camposValm As Reflection.FieldInfo() = Nothing
            Dim camposRefm As System.Reflection.FieldInfo() = Nothing
            Dim camposASustituir As New ArrayList
            'Campos de un hijo contenido
            Dim mapSubCampo As InfoDatosMapInstCampoDN = Nothing

            Dim VinculosClasesCache As New List(Of VinculoClaseDN)

            Dim CamposValidh As List(Of InfoTypeInstCampoValDN)
            Dim CamposValh As List(Of InfoTypeInstCampoValDN)
            Dim CamposValOriginalesh As List(Of InfoTypeInstCampoValDN)
            Dim CamposRefh As List(Of InfoTypeInstCampoRefDN)
            Dim CamposRefEntablah As List(Of InfoTypeInstCampoRefDN)
            Dim CamposRefExternosh As List(Of InfoTypeInstCampoRefDN)
            Dim CamposRefhAdicion As List(Of InfoTypeInstCampoRefDN)
            Dim entidadContenida As Object
            Dim infoDatosMapCampo As InfoDatosMapInstCampoDN

            Dim InfoDatosMapInstClase As InfoDatosMapInstClaseDN
            Dim GestorMapPersistenciaCampos As GestorMapPersistenciaCamposLN
            Dim campoinfo As IInfoTypeInstCampoDN

            If (pCampoContenedor Is Nothing) Then
                PrefijoMap = String.Empty

            Else
                PrefijoMap = pCampoContenedor.NombreMap
            End If




            ''''''''''''''''''''''''''''''''''''''''''''''
            ' TratamientoCampos de la información de instancia
            ''''''''''''''''''''''''''''''''''''''''''''''

            ' clases huellas cache

            If Not pMapeadoInst Is Nothing AndAlso pMapeadoInst.HTDatos.ContainsKey(TiposYReflexion.DN.TiposDatosMapInstClaseDN.ActualizarClasesHuella) Then

                Dim vinclase As TiposYReflexion.DN.VinculoClaseDN
                For Each vinclase In pMapeadoInst.HTDatos.Item(TiposYReflexion.DN.TiposDatosMapInstClaseDN.ActualizarClasesHuella)
                    VinculosClasesCache.Add(vinclase)
                Next
                pVinculosClasesCache = VinculosClasesCache
            End If





            'Se separan en campos de valor y de referencia
            InstanciacionReflexionHelperLN.SepararCamposValorRef(pCampos, camposValm, camposRefm)

            For Each f In camposValm
                If (Not pMapeadoInst Is Nothing) Then
                    mapSubCampo = pMapeadoInst.GetCampoXNombre(f.Name)
                End If

                If (Not mapSubCampo Is Nothing AndAlso mapSubCampo.ColCampoAtributo.Contains(CampoAtributoDN.NoProcesar)) Then
                    ' no hacer nada
                Else
                    pCamposVal.Add(New InfoTypeInstCampoValDN(pCampoContenedor, f, pInstancia, PrefijoMap))
                End If
            Next

            pCamposValOriginales.AddRange(pCamposVal)

            For Each f In camposRefm
                'Debug.WriteLine(f.Name)
                If (Not pMapeadoInst Is Nothing) Then
                    mapSubCampo = pMapeadoInst.GetCampoXNombre(f.Name)
                End If

                If (mapSubCampo IsNot Nothing AndAlso mapSubCampo.ColCampoAtributo.Contains(CampoAtributoDN.NoProcesar)) Then
                    ' no hacer nada
                Else
                    pCamposRef.Add(New InfoTypeInstCampoRefDN(pCampoContenedor, f, pInstancia, PrefijoMap))
                End If
            Next

            ''''''''''''''''''''''''''''''''''''''''''''''
            ' TratamientoCampos by VAL
            ''''''''''''''''''''''''''''''''''''''''''''''
            'Separar el capo id de el resto de campos de valor. Es posible que este campo sea una matriz de campos pero
            'inicialmente consideraremos solo uno
            ' YYYY Eliminar el campo estado de los campos a guardar


            'si le pasan un guid se decide por guid

            Dim campoEstado As IInfoTypeInstCampoDN
            For Each info In pCamposVal

                If info.Campo.Name.ToLower = "mestado" Then

                    campoEstado = info
                Else
                    If id.Length > 30 Then ' se trta de guid
                        If info.Campo.Name.ToLower = "mguid" Then
                            pCamposValID.Add(info)
                        End If

                    Else
                        If info.Campo.Name.ToLower = "mid" AndAlso (Not TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(info.Campo.ReflectedType) OrElse TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaTipada(info.Campo.ReflectedType)) Then 'TODO: importante el campo de identificacion ha de llamarse "mid"
                            pCamposValID.Add(info)

                        ElseIf info.Campo.Name.ToLower = "mguid" AndAlso TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(info.Campo.ReflectedType) Then
                            pCamposValID.Add(info)
                        End If
                    End If

                End If




            Next
            If pCamposValID.Count = 0 Then
                'Beep()
            End If
            ' TODO: alex modificacion de huellas no tipadas



            'Dim campoEstado As IInfoTypeInstCampoDN
            'For Each info In pCamposVal
            '    If info.Campo.Name.ToLower = "mid" Then 'TODO: importante el campo de identificacion ha de llamarse "mid"
            '        pCamposValID.Add(info)

            '    ElseIf info.Campo.Name.ToLower = "mestado" Then
            '        campoEstado = info
            '    End If

            'Next






            For Each info In pCamposValID
                pCamposVal.Remove(info)
            Next

            pCamposVal.Remove(campoEstado)




            ''''''''''''''''''''''''''''''''''''''''''''''
            ' TratamientoCampos by REF
            ''''''''''''''''''''''''''''''''''''''''''''''

            'Reglas de Mapeado por atributos
            If (pMapeadoInst Is Nothing) Then
                ' TODO: ALEX por implementar (la determinacion de persistencia propeia o inclusio es vienen definida por ATRIBUTOS sobre los campos)

                For Each info In pCamposRef
                    'Si el campo esta marcado con el atributo de estar incluido.....
                    pCamposRefExternos.Add(info)
                Next

                'Reglas de Mapeado por Datos de Mapeado
            Else

                For Each info In pCamposRef
                    'Puede ocurrir que el campo sea una entidadDN o una interface.
                    'Si se trata de una interface o bien tiene atributo de intrfaces implementadas o bien se ignora
                    infoDatosMapCampo = pMapeadoInst.GetCampoXNombre(info.Campo.Name)

                    If (info.Campo.FieldType.IsInterface) Then
                        If (infoDatosMapCampo IsNot Nothing) Then
                            If (infoDatosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.NoProcesar)) Then
                                ' no hacer nada
                            ElseIf (infoDatosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.PersistenciaContenidaSerializada)) Then
                                pCamposRefEntabla.Add(info)

                            ElseIf (infoDatosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.InterfaceImplementadaPor)) Then
                                'TODO: ALEX MOTOR faltaria por decir si se quiere dentro o fuera de momento siempre fuera
                                pCamposRefExternos.Add(info)

                            ElseIf (infoDatosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.PersistenciaContenida)) Then
                                pCamposRefEntabla.Add(info)
                                Throw New ApplicationException("Error: no se permite tener persistencia contenida contra una interface")
                            End If

                        Else
                            pCamposRefExternos.Add(info)
                        End If

                    Else


                        If infoDatosMapCampo IsNot Nothing AndAlso infoDatosMapCampo.ColCampoAtributo IsNot Nothing AndAlso (TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(info.Campo.FieldType)) AndAlso infoDatosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.PersistenciaContenida) Then
                            '   Throw New Framework.AccesoDatos.ApplicationExceptionAD("No esta Soportada la persitencia contenida de huellas")
                        End If

                        If (infoDatosMapCampo IsNot Nothing) Then
                            If infoDatosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.NoProcesar) Then
                                ' dato para impedir el porcesamiento
                            Else
                                'CampoAtributo.PersistenciaContenida
                                If (infoDatosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.PersistenciaContenida)) Then

                                    'Si la entidad es una entidad de persistencia contenida en en las tablas del padre sus campos de valor
                                    'se añaden a los cv del padre y lo cref se añaden a los cref del padre como si fueran de este.
                                    'No se consideran como id los campos id  de los hijos contenidos como campos de valor

                                    'Ojo: que pasa si la instancia es nothing
                                    'TODO: ALEX el mapeado se debe recuparar de alguna clase accesora a datos en vez de ser nothing

                                    If (Not pInstancia Is Nothing) Then
                                        entidadContenida = info.Campo.GetValue(pInstancia)

                                    Else
                                        entidadContenida = Nothing
                                    End If

                                    CamposValidh = New List(Of InfoTypeInstCampoValDN)
                                    CamposValh = New List(Of InfoTypeInstCampoValDN)
                                    CamposValOriginalesh = New List(Of InfoTypeInstCampoValDN)
                                    CamposRefh = New List(Of InfoTypeInstCampoRefDN)
                                    CamposRefEntablah = New List(Of InfoTypeInstCampoRefDN)
                                    CamposRefExternosh = New List(Of InfoTypeInstCampoRefDN)
                                    CamposRefhAdicion = New List(Of InfoTypeInstCampoRefDN)

                                    GestorMapPersistenciaCampos = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor

                                    If (infoDatosMapCampo.MapSubEntidad Is Nothing) Then
                                        'Si el campo no determina el mapedo par el tipo que refiere se busca a ver si hay informacion por defecto para el mapeado de ese tipo
                                        InfoDatosMapInstClase = GestorMapPersistenciaCampos.RecuperarMapPersistenciaCampos(info.Campo.FieldType)

                                    Else
                                        InfoDatosMapInstClase = infoDatosMapCampo.MapSubEntidad
                                    End If

                                    SepararCamposEnTablaCamposRelacionados(info, entidadContenida, InstanciacionReflexionHelperLN.RecuperarCampos(info.Campo.FieldType), InfoDatosMapInstClase, CamposValidh, CamposValOriginalesh, CamposValh, CamposRefh, CamposRefEntablah, CamposRefExternosh, VinculosClasesCache, id)

                                    For Each campoinfo In CamposValh
                                        If (campoinfo.CampoRefPadre Is Nothing) Then
                                            campoinfo.CampoRefPadre = info
                                        End If
                                    Next

                                    For Each campoinfo In CamposRefEntablah
                                        If (campoinfo.CampoRefPadre Is Nothing) Then
                                            campoinfo.CampoRefPadre = info
                                        End If
                                    Next

                                    For Each campoinfo In CamposRefExternosh
                                        If (campoinfo.CampoRefPadre Is Nothing) Then
                                            campoinfo.CampoRefPadre = info
                                        End If
                                    Next

                                    pCamposVal.AddRange(CamposValh)
                                    pCamposRefEntabla.Add(info)
                                    pCamposRefEntabla.AddRange(CamposRefEntablah)
                                    pCamposRefExternos.AddRange(CamposRefExternosh)
                                End If

                                If (infoDatosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.PersistenciaContenidaSerializada)) Then
                                    pCamposRefEntabla.Add(info)
                                End If
                            End If

                            If (infoDatosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.InterfaceImplementadaPor)) Then
                                pCamposRefExternos.Add(info)
                            End If

                        Else
                            pCamposRefExternos.Add(info)
                        End If
                    End If
                Next
            End If
        End Sub

        Public Shared Sub RecuperarEnsambladoYTipo(ByVal pNombreCompletoClase As String, ByRef pEnsamblado As Assembly, ByRef pTipo As System.Type)
            Dim posicionPunto As Int64

            posicionPunto = pNombreCompletoClase.LastIndexOf("."c)

            'TODO:alex Por implementaar esto debe de tener el nombre del ensamblado y de la clase claramene diferenciado
            pEnsamblado = Assembly.Load(pNombreCompletoClase.Substring(0, posicionPunto))

            pTipo = pEnsamblado.GetType(pNombreCompletoClase)

            If (pTipo Is Nothing) Then
                Throw New ApplicationException("Error: imposible resolver el tipo")
            End If
        End Sub

#End Region

    End Class
End Namespace

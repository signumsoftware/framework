#Region "Importaciones"

Imports System.Collections.Generic

Imports Framework.AccesoDatos.MotorAD.DN
Imports Framework.TiposYReflexion.DN
Imports Framework.TiposYReflexion.LN

#End Region

Namespace LN
    Public Class GestorRelacionesCampoLN

#Region "Metodos"
        'Este metodo generara bien las sql necesarias para crear las relaciones de un campo.
        'Un campo puede presentar una relacion 1-1 o 1-* y a su vez el tipo fijado por el campo sea por una coleccion (1-n) o por 
        'un campo (1-1), puede estar implementada por una entidadDN  y devuelve una colecion de 1 solo elemento o por una
        'interface en cuyo caso requiere un mapeado y devuelve una coleccion de relaciones de (0 a *) elementos.
        '(Creo que si lo entiendes a la primera dan premio :))
        Public Function GenerarRelacionesCampoRef(ByVal pInfoTypeInstClase As InfoTypeInstClaseDN, ByVal pInfoTypeInstCampoRef As InfoTypeInstCampoRefDN) As Object
            'Los datos que puede devoler
            Dim ColRelacionUnoUno As List(Of RelacionUnoUnoSQLsDN)
            Dim ColRelacionUnoN As ListaRelacionUnoNSqlsDN

            'Variables internas
            Dim instancia As Object = Nothing
            Dim nombreDeTipoCompleto As String
            Dim reluu As RelacionUnoUnoSQLsDN

            Dim tipo As System.Type = Nothing
            Dim ensamblado As Reflection.Assembly = Nothing

            Dim dato As Object
            Dim vc As VinculoClaseDN

            Dim infoMapDatosInst, infoMapDatosInstClaseReferida As InfoDatosMapInstClaseDN
            Dim DatoMApeadoClaseHeredada As Object = Nothing
            Dim InfoDatosMapInstCampo As InfoDatosMapInstCampoDN = Nothing
            Dim gdmi As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor

            infoMapDatosInst = gdmi.RecuperarMapPersistenciaCampos(pInfoTypeInstClase.Tipo)
            infoMapDatosInstClaseReferida = gdmi.RecuperarMapPersistenciaCampos(pInfoTypeInstCampoRef.Campo.FieldType)
            If Not infoMapDatosInstClaseReferida Is Nothing Then
                DatoMApeadoClaseHeredada = infoMapDatosInstClaseReferida.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor)
            End If

            If (infoMapDatosInst IsNot Nothing) Then
                InfoDatosMapInstCampo = infoMapDatosInst.GetCampoXNombre(pInfoTypeInstCampoRef.Campo.Name)
            End If

            'Paso: verificar que el atributo no esta calificado como no procesable
            If (InfoDatosMapInstCampo IsNot Nothing AndAlso InfoDatosMapInstCampo.ColCampoAtributo.Contains(CampoAtributoDN.NoProcesar)) Then
                Return Nothing
            End If

            'Paso: verificar si se trata de una relacion 1-1 o 1-*
            If (pInfoTypeInstCampoRef.Campo.FieldType.GetInterface("IEnumerable", True) Is Nothing) Then

                '(1-1) NO se trata de una colecion
                'Verificar si el tipo es un interface o una entidad dn
                If (pInfoTypeInstCampoRef.Campo.FieldType.IsInterface) OrElse DatoMApeadoClaseHeredada IsNot Nothing Then

                    'Se trata de una interface y por lo tanto requiere datos mapeado de mapeado para su instanciaion
                    If (InfoDatosMapInstCampo IsNot Nothing AndAlso InfoDatosMapInstCampo.ColCampoAtributo.Contains(CampoAtributoDN.InterfaceImplementadaPor)) OrElse DatoMApeadoClaseHeredada IsNot Nothing Then

                        'Si la interface presenta una intancia se procesa solo el tipo de la intancia
                        If (instancia IsNot Nothing) Then
                            Throw New NotImplementedException("Error: por implementar")

                        Else
                            ColRelacionUnoUno = New List(Of RelacionUnoUnoSQLsDN)
                            Dim al As ArrayList
                            Dim VinculoClase As VinculoClaseDN

                            If Not DatoMApeadoClaseHeredada Is Nothing OrElse InfoDatosMapInstCampo.Datos.Count = 0 Then
                                If InfoDatosMapInstCampo Is Nothing Then
                                    al = DatoMApeadoClaseHeredada
                                Else
                                    al = InfoDatosMapInstCampo.MapSubEntidad.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor)
                                End If

                                For Each VinculoClase In al
                                    ' cargr el tipo
                                    ensamblado = VinculoClase.Ensamblado
                                    tipo = VinculoClase.TipoClase
                                    reluu = New RelacionUnoUnoSQLsDN(pInfoTypeInstClase.Tipo, tipo, "id" & pInfoTypeInstCampoRef.NombreMap & tipo.Name, pInfoTypeInstClase.TablaNombre, "tl" & NombreDeTabla(tipo))
                                    ColRelacionUnoUno.Add(reluu)
                                Next

                            Else
                                For Each nombreDeTipoCompleto In InfoDatosMapInstCampo.Datos
                                    'Cargar el tipo
                                    InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo(nombreDeTipoCompleto, ensamblado, tipo)
                                    reluu = New RelacionUnoUnoSQLsDN(pInfoTypeInstClase.Tipo, tipo, "id" & pInfoTypeInstCampoRef.NombreMap & tipo.Name, pInfoTypeInstClase.TablaNombre, "tl" & NombreDeTabla(tipo))

                                    ColRelacionUnoUno.Add(reluu)
                                Next
                            End If

                            Return ColRelacionUnoUno
                        End If

                        'En este caso la clase no declara las entidades DN que acepta para esa interface.
                        'Por ello procedemos a ver si la propia interface dispone de informacion generica de mapeado
                    Else
                        'Obtener le mapeado de comportamiento de la interface

                        Dim infoMapDatosInstINTERFACE As InfoDatosMapInstClaseDN
                        Dim alEntidadesQueImplementan As ArrayList = Nothing

                        infoMapDatosInstINTERFACE = gdmi.RecuperarMapPersistenciaCampos(pInfoTypeInstCampoRef.Campo.FieldType)
                        If (infoMapDatosInstINTERFACE IsNot Nothing) Then
                            alEntidadesQueImplementan = infoMapDatosInstINTERFACE.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor)
                        End If

                        If (alEntidadesQueImplementan IsNot Nothing) Then
                            'La interface contiene sus propio datos de mapedo
                            ColRelacionUnoUno = New List(Of RelacionUnoUnoSQLsDN)

                            For Each dato In alEntidadesQueImplementan
                                If (TypeOf dato Is VinculoClaseDN) Then
                                    vc = dato

                                    reluu = New RelacionUnoUnoSQLsDN(pInfoTypeInstClase.Tipo, vc.TipoClase, "id" & pInfoTypeInstCampoRef.NombreMap & vc.TipoClase.Name, pInfoTypeInstClase.TablaNombre, "tl" & NombreDeTabla(vc.TipoClase))
                                    ColRelacionUnoUno.Add(reluu)
                                End If
                            Next

                            Return ColRelacionUnoUno

                        Else
                            Throw New ApplicationException("Error: no se puede generar ningun tabla para este interface. Falta informacion")
                        End If
                    End If

                    'Se trata de una EntidadDN
                Else
                    ColRelacionUnoUno = New List(Of RelacionUnoUnoSQLsDN)
                    reluu = New RelacionUnoUnoSQLsDN(pInfoTypeInstClase.Tipo, pInfoTypeInstCampoRef.Campo.FieldType, "id" & pInfoTypeInstCampoRef.NombreMap, pInfoTypeInstClase.TablaNombre, "tl" & NombreDeTabla(pInfoTypeInstCampoRef.Campo.FieldType))
                    ColRelacionUnoUno.Add(reluu)

                    Return ColRelacionUnoUno
                End If

            Else
                'Se trata de una colecion (1-*)
                'Obtener una referenia a el tipo fijado por la colecion  que debe ser fijada por tipo
                'Dim colValidable As Framework.DatosNegocio.IValidable
                'Dim validadorTipos As Framework.DatosNegocio.ValidadorTipos
                Dim tipoFijado As System.Type
                Dim tipodefijado As FijacionDeTipoDN
                'colValidable = Activator.CreateInstance(pInfoTypeInstCampoRef.Campo.FieldType)
                'validadorTipos = colValidable.Validador
                'tipoFijado = validadorTipos.Tipo
                tipoFijado = InstanciacionReflexionHelperLN.ObtenerTipoFijado(pInfoTypeInstCampoRef.Campo.FieldType, tipodefijado)

                'Verificar si el tipo fijado por la interface  es un interface o una entidad dn
                If (tipoFijado.IsInterface) Then
                    'El tipo fijado es una interface. Requiere datos de mapeado que indiquen con que DN que implementa la interface se
                    'relaciona la entidad. Luego apareceran multiples tablas de relacion una para cada DN que impelmente la interface

                    If (InfoDatosMapInstCampo IsNot Nothing AndAlso InfoDatosMapInstCampo.ColCampoAtributo.Contains(CampoAtributoDN.InterfaceImplementadaPor)) Then
                        'Si la interface presenta una intancia se procesa solo el tipo de la intancia
                        If (instancia IsNot Nothing) Then
                            Throw New NotImplementedException("Error: por implementar")

                        Else
                            Dim relun As RelacionUnoNSQLsDN
                            Dim infoMapDatosIntrface As InfoDatosMapInstClaseDN

                            ColRelacionUnoN = New ListaRelacionUnoNSqlsDN

                            'Verificar si el campo informa de las clases que acepta para la interface
                            infoMapDatosIntrface = InfoDatosMapInstCampo.MapSubEntidad

                            For Each vc In infoMapDatosIntrface.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor)
                                tipo = vc.TipoClase
                                relun = New RelacionUnoNSQLsDN(pInfoTypeInstClase.Tipo, tipo, pInfoTypeInstCampoRef.NombreMap, pInfoTypeInstClase.TablaNombre, "tl" & NombreDeTabla(tipo))

                                ColRelacionUnoN.Add(relun)
                            Next

                            Return ColRelacionUnoN
                        End If

                        'En esta caso la clase no declara las entidades DN que acepta para esa interface.
                        'Por ellos procedemos a ver si la propia interface dispone de informacion generica de mapeado
                    Else
                        'Obtener el mapeado de comportamiento de la interface
                        Dim infoMapDatosInstINTERFACE As InfoDatosMapInstClaseDN
                        Dim alEntidadesQueImplementan As ArrayList

                        infoMapDatosInstINTERFACE = gdmi.RecuperarMapPersistenciaCampos(tipoFijado)
                        If infoMapDatosInstINTERFACE Is Nothing Then
                            Throw New ApplicationException("no se puedo recuperar mapeado para la interface:" & tipoFijado.ToString)
                        End If

                        alEntidadesQueImplementan = infoMapDatosInstINTERFACE.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor)

                        If (alEntidadesQueImplementan IsNot Nothing) Then
                            Dim relun As RelacionUnoNSQLsDN

                            ColRelacionUnoN = New ListaRelacionUnoNSqlsDN

                            For Each vc In alEntidadesQueImplementan
                                relun = New RelacionUnoNSQLsDN(pInfoTypeInstClase.Tipo, vc.TipoClase, pInfoTypeInstCampoRef.NombreMap, pInfoTypeInstClase.TablaNombre, "tl" & NombreDeTabla(vc.TipoClase))
                                ColRelacionUnoN.Add(relun)
                            Next

                            Return ColRelacionUnoN

                        Else
                            Throw New ApplicationException("no se puede generar ningun tabla para este interface. Falta informacion")
                        End If
                    End If

                    'No es una interface
                Else
                    ColRelacionUnoN = New ListaRelacionUnoNSqlsDN
                    ColRelacionUnoN.Add(New RelacionUnoNSQLsDN(pInfoTypeInstClase.Tipo, pInfoTypeInstCampoRef.Campo.FieldType, pInfoTypeInstCampoRef.NombreMap, pInfoTypeInstClase.TablaNombre, "tl" & NombreDeTabla(tipoFijado)))
                    Return ColRelacionUnoN
                End If
            End If
        End Function
        Public Function GenerarRelacionesHuella(ByVal pInfoTypeInstClase As InfoTypeInstClaseDN, ByVal phuella As System.Type) As Object
            'Los datos que puede devoler
            Dim ColRelacionUnoUno As List(Of RelacionUnoUnoSQLsDN)
            '   Dim ColRelacionUnoN As ListaRelacionUnoNSqlsDN

            'Variables internas
            Dim instancia As Object = Nothing
            '  Dim nombreDeTipoCompleto As String
            Dim reluu As RelacionUnoUnoSQLsDN

            Dim tipo As System.Type = Nothing
            Dim ensamblado As Reflection.Assembly = Nothing

            '    Dim dato As Object
            '  Dim vc As VinculoClaseDN

            '  Dim infoMapDatosInst As InfoDatosMapInstClaseDN
            Dim InfoDatosMapInstCampo As InfoDatosMapInstCampoDN = Nothing
            Dim gdmi As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor

            'infoMapDatosInst = gdmi.RecuperarMapPersistenciaCampos(pInfoTypeInstClase.Tipo)
            'If (infoMapDatosInst IsNot Nothing) Then
            '    InfoDatosMapInstCampo = infoMapDatosInst.GetCampoXNombre(pInfoTypeInstCampoRef.Campo.Name)
            'End If

            ''Paso: verificar que el atributo no esta calificado como no procesable
            'If (InfoDatosMapInstCampo IsNot Nothing AndAlso InfoDatosMapInstCampo.ColCampoAtributo.Contains(CampoAtributoDN.NoProcesar)) Then
            '    Return Nothing
            'End If

            'Paso: verificar si se trata de una relacion 1-1 o 1-*
            ' If (pInfoTypeInstCampoRef.Campo.FieldType.GetInterface("IEnumerable", True) Is Nothing) Then

            '(1-1) NO se trata de una colecion
            'Verificar si el tipo es un interface o una entidad dn
            'If (pInfoTypeInstCampoRef.Campo.FieldType.IsInterface) Then

            ' Throw New NotImplementedException
            ''Se trata de una interface y por lo tanto requiere datos mapeado de mapeado para su instanciaion
            'If (InfoDatosMapInstCampo IsNot Nothing AndAlso InfoDatosMapInstCampo.ColCampoAtributo.Contains(CampoAtributoDN.InterfaceImplementadaPor)) Then

            '    'Si la interface presenta una intancia se procesa solo el tipo de la intancia
            '    If (instancia IsNot Nothing) Then
            '        Throw New NotImplementedException("Error: por implementar")

            '    Else
            '        ColRelacionUnoUno = New List(Of RelacionUnoUnoSQLsDN)
            '        Dim al As ArrayList
            '        Dim VinculoClase As VinculoClaseDN

            '        If InfoDatosMapInstCampo.Datos.Count = 0 Then
            '            al = InfoDatosMapInstCampo.MapSubEntidad.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor)

            '            For Each VinculoClase In al
            '                ' cargr el tipo
            '                ensamblado = VinculoClase.Ensamblado
            '                tipo = VinculoClase.TipoClase
            '                reluu = New RelacionUnoUnoSQLsDN(pInfoTypeInstClase.Tipo, tipo, "id" & pInfoTypeInstCampoRef.NombreMap & tipo.Name, pInfoTypeInstClase.TablaNombre, "tl" & NombreDeTabla(tipo))
            '                ColRelacionUnoUno.Add(reluu)
            '            Next

            '        Else
            '            For Each nombreDeTipoCompleto In InfoDatosMapInstCampo.Datos
            '                'Cargar el tipo
            '                InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo(nombreDeTipoCompleto, ensamblado, tipo)
            '                reluu = New RelacionUnoUnoSQLsDN(pInfoTypeInstClase.Tipo, tipo, "id" & pInfoTypeInstCampoRef.NombreMap & tipo.Name, pInfoTypeInstClase.TablaNombre, "tl" & NombreDeTabla(tipo))

            '                ColRelacionUnoUno.Add(reluu)
            '            Next
            '        End If

            '        Return ColRelacionUnoUno
            '    End If

            '    'En este caso la clase no declara las entidades DN que acepta para esa interface.
            '    'Por ello procedemos a ver si la propia interface dispone de informacion generica de mapeado
            'Else
            '    'Obtener le mapeado de comportamiento de la interface

            '    Dim infoMapDatosInstINTERFACE As InfoDatosMapInstClaseDN
            '    Dim alEntidadesQueImplementan As ArrayList = Nothing

            '    infoMapDatosInstINTERFACE = gdmi.RecuperarMapPersistenciaCampos(pInfoTypeInstCampoRef.Campo.FieldType)
            '    If (infoMapDatosInstINTERFACE IsNot Nothing) Then
            '        alEntidadesQueImplementan = infoMapDatosInstINTERFACE.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor)
            '    End If

            '    If (alEntidadesQueImplementan IsNot Nothing) Then
            '        'La interface contiene sus propio datos de mapedo
            '        ColRelacionUnoUno = New List(Of RelacionUnoUnoSQLsDN)

            '        For Each dato In alEntidadesQueImplementan
            '            If (TypeOf dato Is VinculoClaseDN) Then
            '                vc = dato

            '                reluu = New RelacionUnoUnoSQLsDN(pInfoTypeInstClase.Tipo, vc.TipoClase, "id" & pInfoTypeInstCampoRef.NombreMap & vc.TipoClase.Name, pInfoTypeInstClase.TablaNombre, "tl" & NombreDeTabla(vc.TipoClase))
            '                ColRelacionUnoUno.Add(reluu)
            '            End If
            '        Next

            '        Return ColRelacionUnoUno

            '    Else
            '        Throw New ApplicationException("Error: no se puede generar ningun tabla para este interface. Falta informacion")
            '    End If
            'End If

            'Se trata de una EntidadDN
            '  Else
            Dim tipofijado As System.Type
            ColRelacionUnoUno = New List(Of RelacionUnoUnoSQLsDN)
            tipofijado = TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(phuella, Nothing)

            reluu = New RelacionUnoUnoSQLsDN(pInfoTypeInstClase.Tipo, tipofijado, "IdEntidadReferida", pInfoTypeInstClase.TablaNombre, "tl" & NombreDeTabla(tipofijado))
            ColRelacionUnoUno.Add(reluu)

            Return ColRelacionUnoUno
            ' End If

            'End If
        End Function

#End Region


        ' TODO: este metodo no debiera estar qui sino en un helper
        Public Function NombreDeTabla(ByVal tipo As System.Type) As String
            Dim tipofijado As System.Type
            Dim fijacion As TiposYReflexion.DN.FijacionDeTipoDN
            If tipo.Name.Contains("`") Then

                tipofijado = TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(tipo, fijacion)
                Return tipo.Name.Replace("`", tipofijado.Name)
            Else
                Return tipo.Name
            End If
        End Function

    End Class
End Namespace

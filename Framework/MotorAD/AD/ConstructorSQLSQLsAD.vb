#Region "Importaciones"

Imports System.Collections.Generic

Imports Framework.AccesoDatos
Imports Framework.DatosNegocio
Imports Framework.AccesoDatos.MotorAD.DN
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.TiposYReflexion.DN
Imports Framework.TiposYReflexion.LN

#End Region

Namespace AD
    Public Class ConstructorSQLSQLsAD
        Implements IConstructorSQLAD

        ' Implements IConstructorAD

        Implements IConstructorTablasSQLAD




#Region "Metodos"
        Public Function ConstruirSQLBusqueda(ByRef pParametros As List(Of IDataParameter)) As String Implements IConstructorSQLAD.ConstruirSQLBusqueda
            Throw New NotImplementedException("Error: no implementado")
        End Function

        Public Function ConstruirSQLBusqueda(ByVal pNombreVistaVisualizacion As String, ByVal pNombreVistaFiltro As String, ByVal pFiltro As FiltroDN, ByRef parametros As List(Of IDataParameter)) As String Implements IConstructorSQLAD.ConstruirSQLBusqueda
            Throw New NotImplementedException("Error: no implementado")
        End Function

        Public Function ConstruirSQLBusqueda(ByVal pNombreVistaVisualizacion As String, ByVal pNombreVistaFiltro As String, ByVal pFiltro As List(Of CondicionRelacionalDN), ByRef pParametros As List(Of IDataParameter)) As String Implements IConstructorSQLAD.ConstruirSQLBusqueda
            Dim sqlWhere As String = Nothing
            Dim cond As CondicionRelacionalDN = Nothing
            Dim condfac As CondicionCampoDN
            Dim parametro As System.Data.IDataParameter
            Dim numParam As Int64
            Dim CampoID As String = String.Empty

            'TODO: Y ESTO QUE ES???
            'VICENTE: Ya no pedimos que el filtro sea distinto de nothing, si no hay filtro pues nada
            If (pFiltro Is Nothing) Then
                Throw New ApplicationException("Error: el filtro no puede ser nulo")
            End If

            If (pParametros Is Nothing) Then
                pParametros = New List(Of IDataParameter)
            End If

            For Each cond In pFiltro
                numParam += 1
                condfac = cond.Factor1
                parametro = MapeadoParametroSqls(condfac.Campo & numParam, condfac.TipoCampo, condfac.Valor)
                pParametros.Add(parametro)

                Select Case condfac.Operador
                    Case Is = "Contiene"
                        If (condfac.TipoCampo Is GetType(String)) Then
                            sqlWhere += " CHARINDEX( " & parametro.ParameterName & "," & condfac.Campo & ")=1 " & cond.Operador

                        Else
                            sqlWhere += condfac.Campo & "=" & parametro.ParameterName & " " & cond.Operador
                        End If

                    Case Is = "="
                        sqlWhere += condfac.Campo & "=" & parametro.ParameterName & " " & cond.Operador

                    Case Is = String.Empty
                        Throw New ApplicationException("Error: no se ha definido operador")

                    Case Else
                        sqlWhere += condfac.Campo & condfac.Operador & parametro.ParameterName & " " & cond.Operador

                End Select
            Next

            'Tratamientos de base de datos
            If (sqlWhere Is Nothing) Then
                sqlWhere = String.Empty

            Else
                sqlWhere = " WHERE " & sqlWhere.Substring(0, sqlWhere.Length - cond.Operador.Length)
            End If

            If (CampoID = String.Empty) Then
                If (Not pNombreVistaVisualizacion Is Nothing AndAlso pNombreVistaVisualizacion.Length > 0) Then
                    Return "Select * from " & pNombreVistaVisualizacion & " WHERE ID IN ( Select ID from " & pNombreVistaFiltro & sqlWhere & ")"

                Else
                    Return " Select * from " & pNombreVistaFiltro & sqlWhere
                End If

            Else
                If (Not pNombreVistaVisualizacion Is Nothing AndAlso pNombreVistaVisualizacion.Length > 0) Then
                    Return "Select " & CampoID & " from " & pNombreVistaVisualizacion & " WHERE ID IN ( Select ID from " & pNombreVistaFiltro & sqlWhere & ")"

                Else
                    Return " Select " & CampoID & " from " & pNombreVistaFiltro & sqlWhere
                End If
            End If
        End Function

        Public Overloads Function ConstruirEntidad(ByVal pHLDatos As System.Collections.Hashtable, ByVal pObjeto As InfoTypeInstClaseDN, ByVal pPrefijo As String) As Object Implements IConstructorSQLAD.ConstruirEntidad
            Dim tipo As System.Type
            Dim arg(0) As Object
            Dim rh As InstanciacionReflexionHelperLN
            Dim metodo As Reflection.MethodInfo
            Dim datosMapCampo As InfoDatosMapInstCampoDN = Nothing
            Dim gdatosmap As GestorMapPersistenciaCamposLN
            Dim DatosMap As InfoDatosMapInstClaseDN
            Dim entidad As Object
            Dim valorEnumeraion As Int32
            Dim esEntidadBaseOCol As Boolean
            Dim cv As InfoTypeInstCampoValDN
            Dim nombreCampo As String
            Dim bf As New Runtime.Serialization.Formatters.Binary.BinaryFormatter
            Dim ms As IO.MemoryStream
            Dim cr As InfoTypeInstCampoRefDN
            Dim relaciones As List(Of RelacionUnoUnoSQLsDN)
            Dim relacion As RelacionUnoUnoSQLsDN
            Dim relacionesN As ListaRelacionUnoNSqlsDN
            Dim relacionN As RelacionUnoNSQLsDN
            Dim grc As GestorRelacionesCampoLN
            Dim maptypoCrContenido As InfoTypeInstClaseDN
            Dim gestorMapIsnt As InfoTypeInstLN
            Dim entidadContenida As Object
            Dim col As IList

            Dim elemento As Object
            Dim tscol As System.Type() = {GetType(IEnumerable)}
            Dim tsentidad As System.Type() = {GetType(IEntidadDN)}
            Dim ep As IDatoPersistenteDN

            Dim camposUsuarioHT As Hashtable


            camposUsuarioHT = pHLDatos.Clone





            'Contruir la entidad por reflexion. Tomar la informacion de los datos de mapeado de instanciacion
            gdatosmap = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor
            DatosMap = gdatosmap.RecuperarMapPersistenciaCampos(pObjeto.Tipo)

            'Instanciar la entidad
            entidad = Activator.CreateInstance(pObjeto.Tipo)

            If (TypeOf entidad Is Framework.DatosNegocio.IDatoPersistenteDN OrElse TypeOf entidad Is IEnumerable) Then
                esEntidadBaseOCol = True
            End If

            'Rellenar el ID
            If (Not pObjeto.IdVal Is Nothing) Then
                cv = pObjeto.IdVal
                cv.Campo.SetValue(entidad, CType(pHLDatos.Item(pPrefijo & cv.NombreMap), String))
            End If

            'Rellenar campos de valor
            For Each cv In pObjeto.CamposValOriginal
                nombreCampo = cv.NombreMap

                camposUsuarioHT.Remove(nombreCampo) ' eliminar el campo en los campos de usuario si tine un campo definido para el

                If (cv.NombreMap.ToUpper <> "ID") Then
                    If (pHLDatos.ContainsKey(nombreCampo) AndAlso Not pHLDatos.Item(nombreCampo) Is System.DBNull.Value) Then
                        If (cv.NombreMap.ToLower = "fechamodificacion") Then
                            cv.Campo.SetValue(entidad, New Date(pHLDatos.Item(nombreCampo)))

                        ElseIf (cv.Campo.FieldType.IsEnum) Then
                            valorEnumeraion = pHLDatos.Item(nombreCampo)
                            cv.Campo.SetValue(entidad, [Enum].Parse(cv.Campo.FieldType, valorEnumeraion))

                        Else
                            If (Not pHLDatos.Item(nombreCampo) Is DBNull.Value) Then

                                Select Case cv.Campo.FieldType.Name.ToLower
                                    Case "double"
                                        cv.Campo.SetValue(entidad, CType(pHLDatos.Item(nombreCampo), Double))

                                    Case "datetime"
                                        If (IsNumeric(pHLDatos.Item(nombreCampo))) Then
                                            cv.Campo.SetValue(entidad, New DateTime(pHLDatos.Item(nombreCampo)))

                                        Else
                                            cv.Campo.SetValue(entidad, CType(pHLDatos.Item(nombreCampo), DateTime))
                                        End If
                                    Case "int16"
                                        cv.Campo.SetValue(entidad, CType(pHLDatos.Item(nombreCampo), Int16))


                                    Case "int32"
                                        cv.Campo.SetValue(entidad, CType(pHLDatos.Item(nombreCampo), Int32))

                                    Case "integer"
                                        cv.Campo.SetValue(entidad, CType(pHLDatos.Item(nombreCampo), Int32))

                                    Case "single"
                                        cv.Campo.SetValue(entidad, CType(pHLDatos.Item(nombreCampo), Single))

                                    Case Else
                                        cv.Campo.SetValue(entidad, pHLDatos.Item(nombreCampo))
                                End Select
                            End If
                        End If
                    End If
                End If
            Next

            'Rellenar campos de referencia
            grc = New GestorRelacionesCampoLN

            'Rellenar campos de referencia con persistencia contenida
            gestorMapIsnt = New InfoTypeInstLN

            For Each cr In pObjeto.CamposRefContenidos


                camposUsuarioHT.Remove(cr.Campo.Name) ' eliminar el campo en los campos de usuario si tine un campo definido para el


                'Si el campo es contenido serializado
                If (Not DatosMap Is Nothing) Then
                    datosMapCampo = DatosMap.GetCampoXNombre(cr.Campo.Name)
                End If

                If (Not datosMapCampo Is Nothing AndAlso datosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.PersistenciaContenidaSerializada)) Then
                    If (pHLDatos.ContainsKey(cr.NombreMap) AndAlso Not pHLDatos.Item(cr.NombreMap) Is System.DBNull.Value) Then
                        ms = New IO.MemoryStream(CType(pHLDatos.Item(cr.NombreMap), Byte()))
                        entidadContenida = bf.Deserialize(ms)
                        ' cr.Campo.SetValue(entidad, entidadContenida)
                        InstanciacionReflexionHelperLN.AsignarValoraEntidadDN(entidadContenida, cr.Campo, entidad)

                    End If

                    'Crear una instancia, rellenarla con los valores, vincular la propiedad del contendor con la entidad contenida
                    '(se debiera de registrar con los demas!!!)
                Else
                    maptypoCrContenido = gestorMapIsnt.Generar(cr.Campo.FieldType, cr, "")
                    entidadContenida = Me.ConstruirEntidad(pHLDatos, maptypoCrContenido, cr.NombreMap)

                    If (pObjeto.CamposRef.Contains(cr) = True) Then
                        '  cr.Campo.SetValue(entidad, entidadContenida)
                        InstanciacionReflexionHelperLN.AsignarValoraEntidadDN(entidadContenida, cr.Campo, entidad)

                    End If
                End If
            Next


            'Rellenar campos de referencia Externos
            For Each cr In pObjeto.CamposRefExteriores

                camposUsuarioHT.Remove(cr.Campo.Name) ' eliminar el campo en los campos de usuario si tine un campo definido para el

                If (Not cr.Campo.FieldType.GetInterface("IEnumerable", True) Is Nothing) Then
                    '(1-*) es una coleccion
                    relacionesN = grc.GenerarRelacionesCampoRef(pObjeto, cr)
                    col = Activator.CreateInstance(cr.Campo.FieldType)

                    For Each relacionN In relacionesN
                        If (pHLDatos.ContainsKey(relacionN.NombreBusquedaDatos) AndAlso Not pHLDatos(relacionN.NombreBusquedaDatos) Is System.DBNull.Value) Then
                            'TODO: eliminar los nulos de la col???
                            For Each elemento In pHLDatos.Item(relacionN.NombreBusquedaDatos)
                                If (Not elemento Is Nothing) Then
                                    col.Add(elemento)

                                Else
                                    'En teoria solo los campo marcados para post asignacion pueden ser nothing 
                                End If
                            Next
                        End If
                    Next

                    If (pObjeto.CamposRef.Contains(cr) = True) Then
                        'cr.Campo.SetValue(entidad, col)
                        InstanciacionReflexionHelperLN.AsignarValoraEntidadDN(col, cr.Campo, entidad)

                    End If


                Else '(1-1) Es una DN referida no colecion

                    relaciones = grc.GenerarRelacionesCampoRef(pObjeto, cr)

                    If (Not relaciones Is Nothing) Then
                        'Si es una interface se ha de solicitar por cada uno de los nombres de los campos que hay para las DN que acepta la interface

                        'If (cr.Campo.FieldType.IsInterface) Then
                        For Each relacion In relaciones
                            If (pHLDatos.ContainsKey(relacion.CampoTodo) AndAlso Not pHLDatos(relacion.CampoTodo) Is System.DBNull.Value) Then
                                ' cr.Campo.SetValue(entidad, pHLDatos.Item(relacion.CampoTodo))
                                InstanciacionReflexionHelperLN.AsignarValoraEntidadDN(pHLDatos.Item(relacion.CampoTodo), cr.Campo, entidad)


                            End If
                        Next


                    End If
                End If
            Next







            'Registrar los DN contenidos. Para todos los campos por referencia que sean de persistencia contenida o no y que no sean nothing



            '    Dim tscol As System.Type() = {GetType(IEnumerable)}
            Dim tsObjeto As System.Type() = {GetType(Object)}
            ' Dim tsIDatoPersistenteDN As System.Type() = {GetType(Framework.DatosNegocio.IDatoPersistenteDN)}


            tipo = entidad.GetType

            If (esEntidadBaseOCol = True) Then
                pObjeto.InstanciaPrincipal = entidad
                rh = New InstanciacionReflexionHelperLN

                For Each cr In pObjeto.CamposRef
                    '   If (Not cr.Valor Is Nothing AndAlso TypeOf cr.Valor Is IEntidadDN) Then
                    If (Not cr.Valor Is Nothing) Then
                        If (TypeOf cr.Valor Is Framework.DatosNegocio.IEntidadDN) Then
                            arg(0) = cr.Valor
                            metodo = InstanciacionReflexionHelperLN.RecuperarMetodo(pObjeto.Tipo, "RegistrarParte", Nothing, tsObjeto)
                            metodo.Invoke(entidad, arg)

                            'Regisar los hijos de la coleccion
                        ElseIf (TypeOf cr.Valor Is IEnumerable AndAlso Not TypeOf cr.Valor Is Array) Then
                            'arg(0) = cr.Valor
                            'metodo = InstanciacionReflexionHelperLN.RecuperarMetodo(pObjeto.Tipo, "RegistrarParte", Nothing, tscol)
                            'metodo.Invoke(entidad, arg)

                            ''Registrar los eventos de control de elementos de la coleccion
                            'eb = entidad

                            ''Si se trata de una coleccion del tipo dn que admite eventos para controlar la persistencia
                            'If (TypeOf cr.Valor Is Framework.DatosNegocio.IColEventos) Then
                            '    ColEventos = cr.Valor

                            '    RemoveHandler ColEventos.ElementoAñadido, AddressOf eb.ElementoAñadido
                            '    RemoveHandler ColEventos.ElementoAñadido, AddressOf eb.ElementoEliminado
                            '    AddHandler ColEventos.ElementoAñadido, AddressOf eb.ElementoAñadido
                            '    AddHandler ColEventos.ElementoEliminado, AddressOf eb.ElementoEliminado

                            'Else
                            '    'En el caso de que no lo sea que sepas que debiera de serlo

                            ' regisar los hijos de la coleccion
                            arg(0) = cr.Valor
                            ' metodo = rh.RecuperarMetodo(cr.CampoRefPAdre.Campo.DeclaringType, "RegistrarParte", Nothing, ts) ' TODO: Alex motor esto no seria mas correcto paa los campos ontenidos
                            metodo = InstanciacionReflexionHelperLN.RecuperarMetodo(pObjeto.Tipo, "RegistrarParte", Nothing, tsObjeto)
                            metodo.Invoke(entidad, arg)

                        Else
                            'en este caso el oejto no es una entidad dn ni una coleccion con lo cual llamammos a un metodo en la clase generico para 
                            ' que sea la clase quien se encarge de registrar y desregistrar correctamente el objeto para controlar su cambio
                            arg(0) = cr.Valor
                            Dim parametrostipos As System.Type() = {GetType(Object)}
                            metodo = InstanciacionReflexionHelperLN.RecuperarMetodo(pObjeto.Tipo, "RegistrarParteNoEntidaddDN", Nothing, parametrostipos)
                            If Not metodo Is Nothing Then
                                metodo.Invoke(entidad, arg)
                            End If




                        End If
                    End If
                Next
            End If





            ' crear las entradas en la lista de ++++campos de usuario+++++ para cada elemento que quede en el ht de usuario
            If TypeOf entidad Is IEntidadDN Then

                Dim idn As IEntidadDN
                Dim ide As IDictionaryEnumerator
                ide = camposUsuarioHT.GetEnumerator()
                idn = entidad

                If camposUsuarioHT.Values.Count > 0 Then

                    Dim listaCU As New ColCampoUsuario
                    'Dim cu As CampoUsuario

                    Do While ide.MoveNext
                        If ide.Key.ToString.Length > 2 AndAlso ide.Key.ToString.Substring(1, 2) = "cu_" Then
                            If ide.Value Is System.DBNull.Value Then
                                listaCU.Add(New CampoUsuario(ide.Key, Nothing))
                            Else
                                If ide.Value.GetType Is GetType(String) OrElse ide.Value.GetType.IsPrimitive Then
                                    listaCU.Add(New CampoUsuario(ide.Key, ide.Value))
                                End If
                            End If
                        End If
                    Loop
                    If listaCU.Count > 0 Then
                        idn.ColCampoUsuario = (listaCU)
                    End If

                End If

            End If





            'Poner el estado a sin modificaciones si se trata de un DN
            If (esEntidadBaseOCol = True) Then
                ep = entidad
                ep.EstadoDatos = DatosNegocio.EstadoDatosDN.SinModificar
            End If

            Return entidad
        End Function


        'Private Sub AsignarValoraEntidadDN(ByVal pValor As Object, ByVal pCanpoDestino As System.Reflection.FieldInfo, ByVal pInstanciaDestino As Object)
        '    If TypeOf pValor Is IDatoPersistenteDN AndAlso TypeOf pInstanciaDestino Is IDatoPersistenteDN Then
        '        Dim idp As IDatoPersistenteDN = pInstanciaDestino

        '        idp.DesRegistrarParte(pCanpoDestino.GetValue(pInstanciaDestino))
        '        pCanpoDestino.SetValue(pInstanciaDestino, pValor)
        '        idp.RegistrarParte(pValor)

        '    Else
        '        pCanpoDestino.SetValue(pInstanciaDestino, pValor)

        '    End If



        'End Sub



        Public Overloads Function ConstruirEntidad1(ByVal pALDatos As System.Collections.IList, ByVal pObjeto As InfoTypeInstClaseDN) As Object Implements IConstructorSQLAD.ConstruirEntidad
            Throw New NotImplementedException("Error: no implementado")
        End Function

        Public Function ConstSqlDelete(ByVal pObjeto As InfoTypeInstClaseDN, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date) As String Implements IConstructorSQLAD.ConstSqlDelete
            Dim sql As String
            Dim camposWhere As String = String.Empty
            Dim nombreCampoVal As String
            Dim entidadPrincipal As IEntidadDN
            Dim gdatosmap As GestorMapPersistenciaCamposLN
            Dim DatosMap As InfoDatosMapInstClaseDN

            If (pObjeto Is Nothing) Then
                Throw New ApplicationException("Error: el objeto no puede ser nulo")
            End If

            If (pParametros Is Nothing) Then
                pParametros = New List(Of IDataParameter)
            End If

            'Tomar la informacion de lo datos mapeado de instanciacion
            gdatosmap = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor
            DatosMap = gdatosmap.RecuperarMapPersistenciaCampos(pObjeto.Tipo)

            'Campos de filtro

            'ID
            nombreCampoVal = pObjeto.IdVal.NombreMap
            añadirCampoVal(pObjeto.IdVal, DatosMap, pParametros, pFechaModificacion)
            camposWhere += nombreCampoVal & "=@" & nombreCampoVal

            If (TypeOf pObjeto.InstanciaPrincipal Is Framework.DatosNegocio.IEntidadDN) Then
                entidadPrincipal = pObjeto.InstanciaPrincipal

                'Comprovacion de no modificacion
                pParametros.Add(ParametrosConstAD.ConstParametroString("@FechaVerificacion", entidadPrincipal.FechaModificacion))
                camposWhere += " AND fechaModificacion=@FechaVerificacion"
            End If

            sql = " Delete  " & pObjeto.TablaNombre & " WHERE " & camposWhere

            Return sql
        End Function

        Public Overloads Function ConstSqlInsert(ByVal pObjeto As InfoTypeInstClaseDN, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date, ByRef pSqlHistorica As String) As String Implements IConstructorSQLAD.ConstSqlInsert
            Dim sql As String
            Dim camposDef As String = String.Empty
            Dim camposParam As String = String.Empty
            Dim nombreCampoVal, nombreCampoRef As String
            Dim entidadReferida As IEntidadBaseDN
            Dim cv As InfoTypeInstCampoValDN
            Dim cr As InfoTypeInstCampoRefDN
            Dim gdatosmap As GestorMapPersistenciaCamposLN
            Dim DatosMap As InfoDatosMapInstClaseDN
            Dim DatosCampoMap As InfoDatosMapInstCampoDN = Nothing
            Dim ms As IO.MemoryStream
            Dim bf As Runtime.Serialization.Formatters.Binary.BinaryFormatter

            If (pObjeto Is Nothing) Then
                Throw New ApplicationException("Error: el objeto no puede ser nulo")
            End If

            If (pParametros Is Nothing) Then
                pParametros = New List(Of IDataParameter)
            End If

            ' tomar la informacion de lo datos mapeado de instanciacion
            gdatosmap = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor
            DatosMap = gdatosmap.RecuperarMapPersistenciaCampos(pObjeto.Tipo)

            bf = New Runtime.Serialization.Formatters.Binary.BinaryFormatter

            'No se atiende al valor de parametro ID. Campos por valor
            For Each cv In pObjeto.CamposVal
                nombreCampoVal = cv.NombreMap

                añadirCampoVal(cv, DatosMap, pParametros, pFechaModificacion)

                '2º crear la sql

                camposDef += nombreCampoVal & ","
                camposParam += "@" & nombreCampoVal & ","
            Next

            ' los campos de usuario

            If TypeOf pObjeto.InstanciaPrincipal Is IEntidadDN Then
                Dim ient As IEntidadDN
                ient = pObjeto.InstanciaPrincipal
                Dim icu As ICampoUsuario
                If Not ient.ColCampoUsuario Is Nothing Then
                    For Each icu In ient.ColCampoUsuario

                        nombreCampoVal = icu.Clave
                        pParametros.Add(MapeadoParametroSqls(nombreCampoVal, GetType(String), icu.Valor))
                        camposDef += nombreCampoVal & ","
                        camposParam += "@" & nombreCampoVal & ","

                    Next
                End If



            End If


            'Campos por referencia contenidos
            For Each cr In pObjeto.CamposRefContenidos
                If (Not DatosMap Is Nothing) Then
                    DatosCampoMap = DatosMap.GetCampoXNombre(cr.Campo.Name)
                End If

                If (Not DatosCampoMap Is Nothing AndAlso DatosCampoMap.ColCampoAtributo.Contains(CampoAtributoDN.PersistenciaContenidaSerializada)) Then
                    If (Not cr.Valor Is Nothing) Then
                        camposDef += cr.NombreMap & ","
                        camposParam += "@" & cr.NombreMap & ","

                        ms = New IO.MemoryStream
                        bf.Serialize(ms, cr.Valor)
                        pParametros.Add(ParametrosConstAD.ConstParametroArrayBytes("@" & cr.NombreMap, ms.GetBuffer))
                    End If
                End If
            Next

            'Campos por referencia exteriores
            For Each cr In pObjeto.CamposRefExteriores
                If (Not DatosMap Is Nothing) Then
                    DatosCampoMap = DatosMap.GetCampoXNombre(cr.Campo.Name)
                End If


                If (Not DatosCampoMap Is Nothing AndAlso DatosCampoMap.ColCampoAtributo.Contains(CampoAtributoDN.SoloGuardarYNoReferido)) Then
                Else

                    If (TypeOf cr.Valor Is IEnumerable) Then

                    Else
                        If (Not cr.Valor Is Nothing) Then
                            If (cr.Campo.FieldType.IsInterface) Then
                                nombreCampoRef = "id" & cr.NombreMap & cr.Valor.GetType.Name

                            Else
                                nombreCampoRef = "id" & cr.NombreMap
                            End If

                            camposDef += nombreCampoRef & ","
                            camposParam += "@" & nombreCampoRef & ","
                            entidadReferida = cr.Valor
                            If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(CType(entidadReferida, Object).GetType) Then

                                pParametros.Add(ParametrosConstAD.ConstParametroString("@" & nombreCampoRef, entidadReferida.GUID))
                            Else
                                pParametros.Add(ParametrosConstAD.ConstParametroID("@" & nombreCampoRef, entidadReferida.ID))

                            End If

                            If (entidadReferida.ID = String.Empty) Then
                                '  Throw New ApplicationException("Error: referencia circular guarde primero el todo en el sistema antes de relacionar con las partes en referencia circular ")
                            End If
                        End If
                    End If

                End If


            Next


            ' introducion de ID si se trata de una huella o una entidad base SI NO ESTA BASADO EN GUID
            '   If Not pObjeto.IdVal.NombreMap.ToLower = "guid" Then
            If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pObjeto.InstanciaPrincipal.GetType) OrElse TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsEntidadBaseNoEntidadDN(pObjeto.InstanciaPrincipal.GetType) Then
                If pObjeto.IdVal.NombreMap.ToLower = "guid" Then

                    añadirCampoVal(pObjeto.IdVal, DatosMap, pParametros, pFechaModificacion)

                    '2º crear la sql
                    nombreCampoVal = pObjeto.IdVal.NombreMap
                    camposDef += nombreCampoVal & ","
                    camposParam += "@" & nombreCampoVal & ","
                Else
                    cv = New InfoTypeInstCampoValDN(Nothing, InstanciacionReflexionHelperLN.RecuperarCampo(pObjeto.InstanciaPrincipal.GetType, "mid"), pObjeto.InstanciaPrincipal, Nothing)
                    nombreCampoVal = cv.NombreMap

                    añadirCampoVal(cv, DatosMap, pParametros, pFechaModificacion)

                    '2º crear la sql

                    camposDef += nombreCampoVal & ","
                    camposParam += "@" & nombreCampoVal & ","
                End If

            End If
            ' End If


            'Union de las partes de la sql de insercion
            camposDef = camposDef.Substring(0, camposDef.Length - 1) 'Quitamos la coma final
            camposParam = camposParam.Substring(0, camposParam.Length - 1) 'Quitamos la coma final


            If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pObjeto.Tipo) Then
                sql = " insert " & pObjeto.TablaNombre & " (" & camposDef & ") VALUES (" & camposParam & ")"

            Else
                sql = " insert " & pObjeto.TablaNombre & " (" & camposDef & ") VALUES (" & camposParam & ");Select max(id) from " & pObjeto.TablaNombre

            End If



            ' tratatmiento de tabla historica
            If Not DatosMap Is Nothing AndAlso Not String.IsNullOrEmpty(DatosMap.TablaHistoria) Then



                cv = New InfoTypeInstCampoValDN(Nothing, InstanciacionReflexionHelperLN.RecuperarCampo(pObjeto.InstanciaPrincipal.GetType, "mid"), pObjeto.InstanciaPrincipal, Nothing)
                nombreCampoVal = cv.NombreMap

                añadirCampoVal(cv, DatosMap, pParametros, pFechaModificacion)

                '2º crear la sql

                camposDef += "," & nombreCampoVal
                camposParam += ",@" & nombreCampoVal

                pSqlHistorica = "insert " & DatosMap.TablaHistoria & " (" & camposDef & ") VALUES (" & camposParam & ")"




            End If


            Return sql
        End Function


        'Public Function ConstSqlJOINSelectInversa(ByVal psqlOriginal As String, ByVal pObjeto As InfoTypeInstClaseDN, ByVal pInfoReferido As InfoTypeInstClaseDN, ByVal PropiedadDeInstanciaDN As TiposYReflexion.DN.PropiedadDeInstanciaDN, ByRef pParametros As List(Of IDataParameter), ByVal pID As String) As String


        '    Dim sql As String = String.Empty
        '    Dim sql1N As String = String.Empty
        '    Dim camposWhere As String = String.Empty
        '    Dim nombreCampoVal As String

        '    If (pObjeto Is Nothing) Then
        '        Throw New ApplicationException("Error: el objeto no puede ser nulo")
        '    End If

        '    If (pParametros Is Nothing) Then
        '        pParametros = New List(Of IDataParameter)
        '    End If

        '    'Recuperar todos los campos de la entidad principal



        '    ' aqui hay que usar la información relacional

        '    ' recuperar el campo

        '    nombreCampoVal = PropiedadDeInstanciaDN.RecuperarCampoRef(pObjeto).NombreMap


        '    Dim RelacionUnoNSQLs As RelacionUnoNSQLsDN
        '    RelacionUnoNSQLs = New RelacionUnoNSQLsDN(pObjeto.Tipo, pInfoReferido.Tipo, nombreCampoVal, pObjeto.TablaNombre, pInfoReferido.TablaNombre, PropiedadDeInstanciaDN.Propiedad)


        '    If PropiedadDeInstanciaDN.Propiedad.ReflectedType Is pObjeto.Tipo Then
        '        sql = RelacionUnoNSQLs.SelectInversa
        '        pParametros.Add(ParametrosConstAD.ConstParametroString("@" & RelacionUnoNSQLs.CampoParter.Replace(".", ""), pID))


        '    Else
        '        ' no estaria mal hacer las directas porque nos permitiria navegar sobre huellas
        '        ' aunqe por consistencia de datos no debiera dejar guardarse a algunas entidades si no estan cargadas en el contesto de otras
        '        ' vamos conmo en el motor de visualización se explicita
        '        sql = RelacionUnoNSQLs.SelectDirecta
        '        pParametros.Add(ParametrosConstAD.ConstParametroString("@" & RelacionUnoNSQLs.CampoTodoID.Replace(".", ""), pID))

        '    End If




        '    Return sql



        'End Function

        Public Function ConstSqlSelectInversa(ByVal pObjeto As InfoTypeInstClaseDN, ByVal pInfoReferido As InfoTypeInstClaseDN, ByVal PropiedadDeInstanciaDN As TiposYReflexion.DN.PropiedadDeInstanciaDN, ByRef pParametros As List(Of IDataParameter), ByVal pID As String, ByVal pguid As String) As String


            Dim sql As String = String.Empty
            Dim sql1N As String = String.Empty
            Dim camposWhere As String = String.Empty
            Dim nombreCampoVal As String

            If (pObjeto Is Nothing) Then
                Throw New ApplicationException("Error: el objeto no puede ser nulo")
            End If

            If (pParametros Is Nothing) Then
                pParametros = New List(Of IDataParameter)
            End If

            'Recuperar todos los campos de la entidad principal



            ' aqui hay que usar la información relacional

            ' recuperar el campo

            nombreCampoVal = PropiedadDeInstanciaDN.RecuperarCampoRef(pObjeto).NombreMap


            Dim RelacionUnoNSQLs As RelacionUnoNSQLsDN
            RelacionUnoNSQLs = New RelacionUnoNSQLsDN(pObjeto.Tipo, pInfoReferido.Tipo, nombreCampoVal, pObjeto.TablaNombre, pInfoReferido.TablaNombre, PropiedadDeInstanciaDN.Propiedad)


            If PropiedadDeInstanciaDN.Propiedad.ReflectedType Is pObjeto.Tipo Then
                sql = RelacionUnoNSQLs.SelectInversa
                If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(RelacionUnoNSQLs.TipoParte) Then
                    pParametros.Add(ParametrosConstAD.ConstParametroString("@" & RelacionUnoNSQLs.CampoParteGUID.Replace(".", ""), pguid))

                Else
                    pParametros.Add(ParametrosConstAD.ConstParametroString("@" & RelacionUnoNSQLs.CampoParter.Replace(".", ""), pID))
                End If




            Else
                ' no estaria mal hacer las directas porque nos permitiria navegar sobre huellas
                ' aunqe por consistencia de datos no debiera dejar guardarse a algunas entidades si no estan cargadas en el contesto de otras
                ' vamos conmo en el motor de visualización se explicita
                sql = RelacionUnoNSQLs.SelectDirecta
                pParametros.Add(ParametrosConstAD.ConstParametroString("@" & RelacionUnoNSQLs.CampoTodoID.Replace(".", ""), pID))
                'pParametros.Add(ParametrosConstAD.ConstParametroID("@" & RelacionUnoNSQLs.CampoTodoID.Replace(".", ""), pID))

            End If




            Return sql



        End Function



        Public Function ConstSqlSelectDirecta(ByVal pObjeto As InfoTypeInstClaseDN, ByVal pInfoReferido As InfoTypeInstClaseDN, ByVal PropiedadDeInstanciaDN As TiposYReflexion.DN.PropiedadDeInstanciaDN, ByRef pParametros As List(Of IDataParameter), ByVal pID As String) As String


            Dim sql As String = String.Empty
            Dim sql1N As String = String.Empty
            Dim camposWhere As String = String.Empty
            Dim nombreCampoVal As String

            If (pObjeto Is Nothing) Then
                Throw New ApplicationException("Error: el objeto no puede ser nulo")
            End If

            If (pParametros Is Nothing) Then
                pParametros = New List(Of IDataParameter)
            End If

            'Recuperar todos los campos de la entidad principal



            ' aqui hay que usar la información relacional

            ' recuperar el campo

            nombreCampoVal = PropiedadDeInstanciaDN.RecuperarCampoRef(pObjeto).NombreMap


            Dim RelacionUnoNSQLs As RelacionUnoNSQLsDN
            RelacionUnoNSQLs = New RelacionUnoNSQLsDN(pObjeto.Tipo, pInfoReferido.Tipo, nombreCampoVal, pObjeto.TablaNombre, pInfoReferido.TablaNombre, PropiedadDeInstanciaDN.Propiedad)


            ' no estaria mal hacer las directas porque nos permitiria navegar sobre huellas
            ' aunqe por consistencia de datos no debiera dejar guardarse a algunas entidades si no estan cargadas en el contesto de otras
            ' vamos conmo en el motor de visualización se explicita
            sql = RelacionUnoNSQLs.SelectDirecta
            pParametros.Add(ParametrosConstAD.ConstParametroString("@" & RelacionUnoNSQLs.CampoTodoID.Replace(".", ""), pID))




            Return sql



        End Function


        Public Overloads Function ConstSqlSelectID(ByVal pObjeto As InfoTypeInstClaseDN, ByRef pParametros As List(Of IDataParameter), ByVal pGUID As String) As String Implements IConstructorSQLAD.ConstSqlSelectID
            Dim sql As String = String.Empty
            Dim sql1N As String = String.Empty
            Dim camposWhere As String = String.Empty
            '  Dim nombreCampoVal As String

            If (pObjeto Is Nothing) Then
                Throw New ApplicationException("Error: el objeto no puede ser nulo")
            End If

            If (pParametros Is Nothing) Then
                pParametros = New List(Of IDataParameter)
            End If

            'Recuperar todos los campos de la entidad principal

            'ID
            'nombreCampoVal = pObjeto.IdVal.NombreMap
            pParametros.Add(ParametrosConstAD.ConstParametroString("@GUID", pGUID))

            camposWhere += " GUID=@GUID"
            sql += " Select ID FROM " & pObjeto.TablaNombre & " WHERE " & camposWhere

            Return sql
        End Function

        Public Overloads Function ConstSqlSelect(ByVal pObjeto As InfoTypeInstClaseDN, ByRef pParametros As List(Of IDataParameter), ByVal pID As String) As String Implements IConstructorSQLAD.ConstSqlSelect
            Dim sql As String = String.Empty
            Dim sql1N As String = String.Empty
            Dim camposWhere As String = String.Empty
            Dim nombreCampoVal As String

            If (pObjeto Is Nothing) Then
                Throw New ApplicationException("Error: el objeto no puede ser nulo")
            End If

            If (pParametros Is Nothing) Then
                pParametros = New List(Of IDataParameter)
            End If

            'Recuperar todos los campos de la entidad principal

            'ID
            nombreCampoVal = pObjeto.IdVal.NombreMap
            pParametros.Add(ParametrosConstAD.ConstParametroString("@" & nombreCampoVal, pID))

            camposWhere += nombreCampoVal & "=@" & nombreCampoVal
            sql += " Select * FROM " & pObjeto.TablaNombre & " WHERE " & camposWhere

            'Recuperar todos los campos de la entidades relacionadas 1-*
            GenerarMapeadoRelacion1N(pObjeto, pID, sql1N, pParametros)
            sql += sql1N

            Return sql
        End Function

        Private Sub GenerarMapeadoRelacion1N(ByVal pObjeto As InfoTypeInstClaseDN, ByVal pID As String, ByRef pSQL1n As String, ByRef pParametros As List(Of IDataParameter))
            Dim cr As InfoTypeInstCampoRefDN
            'Dim colValidable As IValidable
            'Dim validadorTipos As ValidadorTipos
            Dim nombreTabla As String = String.Empty
            Dim campoRecuperacion As String = String.Empty
            Dim campoFiltro As String
            Dim CampoComunFiltro As Object
            Dim relaciones As ListaRelacionUnoNSqlsDN
            Dim relacion As RelacionUnoNSQLsDN
            Dim grc As GestorRelacionesCampoLN

            Dim tipoFijado As System.Type
            Dim tipodeFijaciondeTipo As FijacionDeTipoDN


            Dim gdmi As GestorMapPersistenciaCamposLN
            Dim infoMapDatosInstClaseReferida As InfoDatosMapInstClaseDN
            Dim DatoMApeadoClaseHeredada As Object = Nothing
            gdmi = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor



            campoFiltro = "id" & pObjeto.TablaNombre
            CampoComunFiltro = ParametrosConstAD.ConstParametroID("@" & campoFiltro, pID)

            For Each cr In pObjeto.CamposRefExteriores
                'Se trata de una relacion 1-*
                'En teoria estas relaciones debieran estar implementadas mediante colecciones tipadas
                If (Not cr.Campo.FieldType.GetInterface("IEnumerable", True) Is Nothing) Then


                    'colValidable = cr.Valor
                    'If (colValidable Is Nothing) Then
                    '    colValidable = Activator.CreateInstance(cr.Campo.FieldType)
                    'End If

                    'validadorTipos = colValidable.Validador
                    'tipoFijado = validadorTipos.Tipo



                    tipoFijado = InstanciacionReflexionHelperLN.ObtenerTipoFijado(cr.Campo.FieldType, tipodeFijaciondeTipo)


                    grc = New GestorRelacionesCampoLN
                    relaciones = grc.GenerarRelacionesCampoRef(pObjeto, cr)

                    'El tipo fijado es una interface por lo que debemos recuperar los datos de mapeado que definen que DNs la implementan.
                    'Primero se atendera a lo que diga la clase para dicho campo. De no presentar informacion se atendera a las DN declaradas
                    'por la interface. De no presentar informacion se lanzara una excepcion


                    infoMapDatosInstClaseReferida = gdmi.RecuperarMapPersistenciaCampos(tipoFijado)
                    If Not infoMapDatosInstClaseReferida Is Nothing Then
                        DatoMApeadoClaseHeredada = infoMapDatosInstClaseReferida.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor)
                    End If

                    If (tipoFijado.IsInterface) OrElse Not DatoMApeadoClaseHeredada Is Nothing Then
                        If (Not pParametros.Contains(CampoComunFiltro)) Then
                            pParametros.Add(CampoComunFiltro)
                        End If

                        For Each relacion In relaciones
                            campoRecuperacion = relacion.CampoParteTR
                            nombreTabla = relacion.NombreTablaRel
                            pSQL1n += ";SELECT " & campoRecuperacion & " as " & relacion.NombreBusquedaDatos & " FROM " & nombreTabla & " WHERE " & relacion.CampoTodoTR & "=@" & campoFiltro
                        Next

                        'El tipo es una DN fijada
                    Else
                        relacion = relaciones(0)
                        NombresTablaRelacionUnoN(pObjeto, cr, tipoFijado, nombreTabla, campoFiltro, campoRecuperacion)

                        If (Not pParametros.Contains(CampoComunFiltro)) Then
                            pParametros.Add(CampoComunFiltro)
                        End If

                        'El nombre de la tabla debe de contemplar que un objeto tenga dos colecciones del mimo tipo, por ello incluimos
                        'el nombre de la propiedad en el nombre de la tabla
                        pSQL1n += ";SELECT " & relacion.CampoParteTR & " as " & relacion.NombreBusquedaDatos & " FROM " & nombreTabla & " WHERE " & relacion.CampoTodoTR & "=@" & campoFiltro
                    End If
                End If
            Next
        End Sub

        Public Sub NombresTablaRelacionUnoN(ByVal pContenedor As InfoTypeInstClaseDN, ByVal pContenido As InfoTypeInstCampoRefDN, ByVal pTipoDestino As System.Type, ByRef pNombreTabla As String, ByRef pCampoTodo As String, ByRef pCampoParte As String)
            pCampoTodo = "idtl" & pContenedor.Tipo.Name
            pCampoParte = "idtl" & pTipoDestino.Name
            pNombreTabla = "tr" & pContenedor.TablaNombre & pContenido.NombreMap.Substring(0) & "Xtl" & pTipoDestino.Name
        End Sub

        Public Overloads Function ConstSqlUpdate(ByVal pObjeto As InfoTypeInstClaseDN, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date, ByRef pSqlHistorica As String) As String Implements IConstructorSQLAD.ConstSqlUpdate
            Dim sql As String = String.Empty
            Dim camposSet As String = String.Empty
            Dim camposWhere As String = String.Empty
            Dim nombreCampoVal, nombreCampoRef As String
            Dim objetoreferido As Object
            Dim entidadReferida As Framework.DatosNegocio.IEntidadBaseDN
            Dim entidadPrincipal As Framework.DatosNegocio.IEntidadDN
            Dim grc As GestorRelacionesCampoLN
            Dim ColRelacionUnoUnoSqls As List(Of RelacionUnoUnoSQLsDN)
            Dim RelacionUnoUnoSqls As RelacionUnoUnoSQLsDN
            Dim cv As InfoTypeInstCampoValDN
            Dim cr As InfoTypeInstCampoRefDN
            Dim datosCampoMap As InfoDatosMapInstCampoDN = Nothing
            Dim gdatosmap As GestorMapPersistenciaCamposLN
            Dim datosMap As InfoDatosMapInstClaseDN
            Dim ms As IO.MemoryStream
            Dim bf As Runtime.Serialization.Formatters.Binary.BinaryFormatter
            Dim marcaTemporalEncontrada As Boolean

            Dim gdmi As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor
            Dim infoMapDatosInstClaseReferida As InfoDatosMapInstClaseDN
            Dim DatoMApeadoClaseHeredada As Object = Nothing
            gdmi = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor




            If (pObjeto Is Nothing) Then
                Throw New ApplicationException("Error: el objeto no puede ser nulo")
            End If

            If (pParametros Is Nothing) Then
                pParametros = New List(Of IDataParameter)
            End If

            'Tomar la informacion de lo datos mapeado de instanciacion
            grc = New GestorRelacionesCampoLN
            gdatosmap = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor
            datosMap = gdatosmap.RecuperarMapPersistenciaCampos(pObjeto.Tipo)
            bf = New Runtime.Serialization.Formatters.Binary.BinaryFormatter

            'Campos de filtro
            'ID
            nombreCampoVal = pObjeto.IdVal.NombreMap
            añadirCampoVal(pObjeto.IdVal, datosMap, pParametros, pFechaModificacion)
            camposWhere += nombreCampoVal & "=@" & nombreCampoVal

            If (TypeOf pObjeto.InstanciaPrincipal Is Framework.DatosNegocio.IEntidadDN AndAlso Not TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pObjeto.InstanciaPrincipal.GetType)) Then
                entidadPrincipal = pObjeto.InstanciaPrincipal

                'Comprobacion de no modificacion
                pParametros.Add(ParametrosConstAD.ConstParametroString("@FechaVerificacion", entidadPrincipal.FechaModificacion.Ticks))
                camposWhere += " AND fechaModificacion=@FechaVerificacion"
            End If

            'Campos por valor
            For Each cv In pObjeto.CamposVal
                nombreCampoVal = cv.NombreMap

                If (marcaTemporalEncontrada <> True AndAlso nombreCampoVal.ToLower = "fechamodificacion") Then
                    pParametros.Add(ParametrosConstAD.ConstParametroString("@fechaModificacion", pFechaModificacion.Ticks.ToString))
                    camposSet += "fechaModificacion=@fechaModificacion,"
                    marcaTemporalEncontrada = True

                Else
                    añadirCampoVal(cv, datosMap, pParametros, pFechaModificacion)
                    '2º crear campos la sql
                    camposSet += nombreCampoVal & "=@" & nombreCampoVal & ","
                End If
            Next




            ' los campos de usuario

            If TypeOf pObjeto.InstanciaPrincipal Is IEntidadDN Then
                Dim ient As IEntidadDN
                ient = pObjeto.InstanciaPrincipal
                Dim icu As ICampoUsuario
                If Not ient.ColCampoUsuario Is Nothing Then
                    For Each icu In ient.ColCampoUsuario
                        nombreCampoVal = icu.Clave
                        pParametros.Add(MapeadoParametroSqls(nombreCampoVal, GetType(String), icu.Valor))
                        camposSet += nombreCampoVal & "=@" & nombreCampoVal & ","
                    Next

                End If


            End If


            'Campos por referencia contenidos
            For Each cr In pObjeto.CamposRefContenidos
                If (Not datosMap Is Nothing) Then
                    datosCampoMap = datosMap.GetCampoXNombre(cr.Campo.Name)
                End If

                If (Not datosCampoMap Is Nothing AndAlso datosCampoMap.ColCampoAtributo.Contains(CampoAtributoDN.PersistenciaContenidaSerializada)) Then
                    If (Not cr.Valor Is Nothing) Then
                        camposSet += cr.NombreMap & "=@" & cr.NombreMap & ","
                        ms = New IO.MemoryStream
                        bf.Serialize(ms, cr.Valor)
                        pParametros.Add(ParametrosConstAD.ConstParametroArrayBytes("@" & cr.NombreMap, ms.GetBuffer))
                    End If
                End If
            Next

            'Campos por referencia exteriores. Campos por referencia en relacion 1-1
            For Each cr In pObjeto.CamposRefExteriores

                If (Not datosCampoMap Is Nothing AndAlso datosCampoMap.ColCampoAtributo.Contains(CampoAtributoDN.SoloGuardarYNoReferido)) Then

                Else
                    If (Not cr.Campo.FieldType.GetInterface("IEnumerable", True) Is Nothing) Then
                        'Las relaciones 1-* se tratan en otros metodos

                    Else
                        'A este nivel hemos de verificar si el campo lo implementa o no una interface para hallar el nombre correcto del campo
                        ' si la clase es heredada tambien puede tener varais opciones
                        infoMapDatosInstClaseReferida = gdmi.RecuperarMapPersistenciaCampos(cr.Campo.FieldType)
                        If Not infoMapDatosInstClaseReferida Is Nothing Then
                            DatoMApeadoClaseHeredada = infoMapDatosInstClaseReferida.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor)
                        End If

                        If (cr.Campo.FieldType.IsInterface) OrElse Not DatoMApeadoClaseHeredada Is Nothing Then
                            'El campo es una interface y hay que componer el nombre del campo
                            ColRelacionUnoUnoSqls = grc.GenerarRelacionesCampoRef(pObjeto, cr)

                            If (Not ColRelacionUnoUnoSqls Is Nothing) Then
                                entidadReferida = cr.Valor
                                objetoreferido = entidadReferida

                                For Each RelacionUnoUnoSqls In ColRelacionUnoUnoSqls
                                    nombreCampoRef = RelacionUnoUnoSqls.CampoTodo
                                    camposSet += nombreCampoRef & "=@" & nombreCampoRef & ","

                                    If Not objetoreferido Is Nothing AndAlso objetoreferido.GetType Is RelacionUnoUnoSqls.TipoParte Then
                                        pParametros.Add(ParametrosConstAD.ConstParametroID("@" & nombreCampoRef, entidadReferida.ID))

                                    Else
                                        pParametros.Add(ParametrosConstAD.ConstParametroID("@" & nombreCampoRef, Nothing))
                                    End If
                                Next
                            End If

                        Else
                            nombreCampoRef = "id" & cr.NombreMap

                            camposSet += nombreCampoRef & "=@" & nombreCampoRef & ","
                            entidadReferida = cr.Valor

                            If (entidadReferida Is Nothing) Then
                                pParametros.Add(ParametrosConstAD.ConstParametroID("@" & nombreCampoRef, Nothing))

                            Else
                                If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(CType(entidadReferida, Object).GetType) Then
                                    pParametros.Add(ParametrosConstAD.ConstParametroString("@" & nombreCampoRef, entidadReferida.GUID))

                                Else
                                    pParametros.Add(ParametrosConstAD.ConstParametroID("@" & nombreCampoRef, entidadReferida.ID))

                                End If
                            End If
                        End If
                    End If
                End If



            Next

            'Union de las partes de la sql de inserion
            camposSet = camposSet.Substring(0, camposSet.Length - 1) 'Quitamos la coma final
            sql = " update " & pObjeto.TablaNombre & " SET " & camposSet & " WHERE " & camposWhere
            If Not datosMap Is Nothing AndAlso Not String.IsNullOrEmpty(datosMap.TablaHistoria) Then
                pSqlHistorica = " update " & datosMap.TablaHistoria & " SET " & camposSet & " WHERE " & camposWhere
            End If
            Return sql
        End Function

        Private Sub añadirCampoVal(ByVal pCampoVal As InfoTypeInstCampoValDN, ByVal pDatosMap As InfoDatosMapInstClaseDN, ByRef pParametros As List(Of IDataParameter), ByVal pFechaVerific As Date)
            Dim tipoCampoVal As System.Type
            Dim nombreCampoVal As String
            Dim datosMapCampo As InfoDatosMapInstCampoDN = Nothing

            ' 1º crear los parametros 
            tipoCampoVal = pCampoVal.Campo.FieldType
            nombreCampoVal = pCampoVal.NombreMap

            'excepciones 
            If (Not pDatosMap Is Nothing) Then
                datosMapCampo = pDatosMap.GetCampoXNombre(pCampoVal.Campo.Name)
            End If

            If (Not datosMapCampo Is Nothing AndAlso datosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.PersistenciaContenidaSerializada)) Then
                tipoCampoVal = GetType(System.Byte())
            End If

            'Creacion de los campos de valor por algoritmo
            If (pCampoVal.NombreMap.ToLower = "fechamodificacion") Then
                pParametros.Add(ParametrosConstAD.ConstParametroString("@" & nombreCampoVal, pFechaVerific.Ticks))
                Exit Sub
            End If

            pParametros.Add(MapeadoParametroSqls(nombreCampoVal, tipoCampoVal, pCampoVal.Valor))
        End Sub

        Private Shared Function MapeadoParametroSqls(ByVal pNombreCampoVal As String, ByVal pTipo As System.Type, ByVal pValor As Object) As System.Data.IDataParameter
            If (pTipo Is GetType(System.DateTime)) Then
                Return ParametrosConstAD.ConstParametroFecha("@" & pNombreCampoVal, pValor)

            ElseIf (pTipo Is GetType(System.String)) Then
                Return ParametrosConstAD.ConstParametroString("@" & pNombreCampoVal, pValor)

            ElseIf (pTipo Is GetType(System.Int16) OrElse pTipo Is GetType(System.Int32) OrElse pTipo Is GetType(System.Int64)) Then
                Return ParametrosConstAD.ConstParametroInteger("@" & pNombreCampoVal, pValor)

            ElseIf (pTipo Is GetType(System.UInt32) OrElse pTipo Is GetType(System.UInt32) OrElse pTipo Is GetType(System.UInt64)) Then
                Return ParametrosConstAD.ConstParametroUInteger("@" & pNombreCampoVal, pValor)

            ElseIf (pTipo Is GetType(System.Double) OrElse pTipo Is GetType(System.Decimal) OrElse pTipo Is GetType(System.Single)) Then
                Return ParametrosConstAD.ConstParametroDouble("@" & pNombreCampoVal, pValor)

            ElseIf (pTipo Is GetType(System.Boolean)) Then
                Return ParametrosConstAD.ConstParametroBoolean("@" & pNombreCampoVal, pValor)

            ElseIf (pTipo Is GetType(System.Byte())) Then
                Return ParametrosConstAD.ConstParametroArrayBytes("@" & pNombreCampoVal, pValor)

            ElseIf (pTipo.IsEnum = True) Then
                Return ParametrosConstAD.ConstParametroInteger("@" & pNombreCampoVal, pValor)

            Else
                Throw New ApplicationException("Error: tipo desconocido")
            End If
        End Function

        'Este metodo debe crear las tablas relacionales y crear las relaciones necesarias
        Private Function ConstSqlCreateRelations(ByVal pObjeto As InfoTypeInstClaseDN, ByVal pCampoRef As InfoTypeInstCampoRefDN, ByVal pInfoCampo As InfoDatosMapInstCampoDN, ByVal pHistoricas As Boolean) As RelacionSQLsDN Implements IConstructorTablasSQLAD.ConstSqlCreateRelations
            ' Dim colValidable As Framework.DatosNegocio.IValidable
            '  Dim validadorTipos As Framework.DatosNegocio.ValidadorTipos
            Dim nombreTabla As String = String.Empty
            Dim nombreTablaparte, campoFiltro As String
            Dim campoRecuperacion As String = String.Empty
            Dim tablaRelacionSQLs As RelacionSQLsDN = Nothing
            Dim nombreDeTipo As String
            Dim Camino As String()
            Dim sufijoSubCampo As String
            Dim tipoFijadoPorValidador As System.Type
            Dim tipodefijacionDeTipo As FijacionDeTipoDN
            campoFiltro = "id" & pObjeto.Tipo.Name

            If (Not pCampoRef.Campo.FieldType.GetInterface("IEnumerable", True) Is Nothing) Then
                'Se trata de una relacion 1-*.
                'En teoria estas relaciones debieran estar implementadas mediante colecciones tipadas

                'colValidable = pCampoRef.Valor

                'If (colValidable Is Nothing) Then
                '    colValidable = Activator.CreateInstance(pCampoRef.Campo.FieldType)
                'End If

                'validadorTipos = colValidable.Validador
                'tipoFijadoPorValidador = validadorTipos.Tipo


                tipoFijadoPorValidador = InstanciacionReflexionHelperLN.ObtenerTipoFijado(pCampoRef.Valor, tipodefijacionDeTipo)


                'Es una leion fijada contra una interface
                If (tipoFijadoPorValidador.IsInterface) Then
                    If (pInfoCampo.ColCampoAtributo.Contains(CampoAtributoDN.InterfaceImplementadaPor)) Then

                    Else
                        Throw New ApplicationException("Error: falta mapeo para la interface")
                    End If

                    'Es una colecion fijada contra una DN
                Else
                    'El nombre de la tabla debe de contenplar que un objeto tenga dos colecciones del mismo tipo,
                    'por ello incluimos el nombre de la propiedad en el nombre de la tabla

                    tablaRelacionSQLs = New RelacionSQLsDN
                    '   NombresTablaRelacionUnoN(pObjeto, pCampoRef, validadorTipos.GetType, nombreTabla, campoFiltro, campoRecuperacion)
                    NombresTablaRelacionUnoN(pObjeto, pCampoRef, tipoFijadoPorValidador, nombreTabla, campoFiltro, campoRecuperacion)
                    tablaRelacionSQLs.CreacionTablaRelacionSQL = "CREATE TABLE " & nombreTabla & " ( ID bigint IDENTITY PRIMARY KEY," & campoRecuperacion & " bigint NOT NULL," & campoFiltro & " bigint NOT NULL)"
                    tablaRelacionSQLs.CreacionTrParteSQL = "ALTER TABLE " & nombreTabla & " ADD CONSTRAINT " & nombreTabla & campoRecuperacion & "  FOREIGN KEY(" & campoRecuperacion & ") REFERENCES tl" & tipoFijadoPorValidador.Name & " (ID)  "
                    tablaRelacionSQLs.CreacionTrTodoSQL = "ALTER TABLE " & nombreTabla & " ADD CONSTRAINT " & nombreTabla & campoFiltro & "  FOREIGN KEY(" & campoFiltro & ") REFERENCES " & pObjeto.TablaNombre & " (ID)  "
                    tablaRelacionSQLs.TipoRel = TipoRelacionDN.UnoN
                End If

                'Se trata de una relacion 1-1
            Else
                'Si el campo a procesar es una interface ha de te tener mapeado que defina ls clases que lo implementan 
                If (Not pCampoRef.Campo.FieldType.IsInterface) Then
                    tablaRelacionSQLs = New RelacionSQLsDN
                    campoFiltro = "id" & pCampoRef.NombreMap
                    nombreTablaparte = "tl" & pCampoRef.Campo.FieldType.Name
                    tablaRelacionSQLs.CreacionRelacionTodoParte = "ALTER TABLE " & pObjeto.TablaNombre & " ADD CONSTRAINT " & pObjeto.TablaNombre & campoFiltro & "  FOREIGN KEY(" & campoFiltro & ") REFERENCES " & nombreTablaparte & " (ID)  "
                    tablaRelacionSQLs.TipoRel = TipoRelacionDN.UnoUno

                Else
                    If (pInfoCampo.ColCampoAtributo.Contains(CampoAtributoDN.InterfaceImplementadaPor)) Then
                        For Each nombreDeTipo In pInfoCampo.Datos
                            Camino = nombreDeTipo.Split("."c)
                            sufijoSubCampo = Camino(Camino.GetUpperBound(0))
                            campoFiltro = GenerarNombreCampoRefUnoUno(pCampoRef, sufijoSubCampo)

                            tablaRelacionSQLs = New RelacionSQLsDN
                            nombreTablaparte = "tl" & sufijoSubCampo
                            tablaRelacionSQLs.CreacionRelacionTodoParte = "ALTER TABLE " & pObjeto.TablaNombre & " ADD CONSTRAINT " & pObjeto.TablaNombre & campoFiltro & "  FOREIGN KEY(" & campoFiltro & ") REFERENCES " & nombreTablaparte & " (ID)  "
                            tablaRelacionSQLs.TipoRel = TipoRelacionDN.UnoUno
                        Next

                    Else
                        Throw New ApplicationException("Error: no se pueden procesar los subcampos de un campo declarado como interface porque falta el mapeado de clases que lo implementan")
                    End If
                End If
            End If

            Return tablaRelacionSQLs
        End Function

        Private Function ConstSqlCreateTable(ByVal pObjeto As InfoTypeInstClaseDN, ByVal pHistoricas As Boolean) As String Implements IConstructorTablasSQLAD.ConstSqlCreateTable
            Dim sql As String
            Dim sqlIndices As String = ""
            Dim camposDef As String = String.Empty
            Dim nombreCampoVal, nombreTipo As String
            Dim cv As InfoTypeInstCampoValDN
            Dim cr As InfoTypeInstCampoRefDN

            Dim infoMapDatosInst, infoDatosMapInstCampoClaseHeredada As InfoDatosMapInstClaseDN
            Dim gdmi As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor
            Dim DatosMapClaseHeredable As Object = Nothing


            If (pObjeto Is Nothing) Then
                Throw New ApplicationException("Error: el objeto no puede ser nulo")
            End If

            'Obtener el mapeado de comportamiento de la entidad
            infoMapDatosInst = gdmi.RecuperarMapPersistenciaCampos(pObjeto.Tipo)



            'Tratamiento del campo id

            cv = pObjeto.IdVal
            If cv Is Nothing Then
                Throw New Framework.AccesoDatos.ApplicationExceptionAD("esposible que la clase  " & pObjeto.Tipo.ToString & " no sea una entidad dn")
            End If

            nombreCampoVal = cv.NombreMap
            nombreTipo = SeleccionTypoCampoIDSQLS(cv.Campo.ReflectedType)

            'Se decide el tipo de la coludna
            ' si el objeto es una huella su tabla no tine que ser autonumerica , tampo si es una tabla tipo es decir una entidad base
            If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pObjeto.Tipo) OrElse TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsEntidadBaseNoEntidadDN(pObjeto.Tipo) Then

                If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaTipada(pObjeto.Tipo) Then
                    camposDef += nombreCampoVal & " " & nombreTipo & "  PRIMARY KEY , "

                Else
                    camposDef += nombreCampoVal & " " & nombreTipo & " PRIMARY KEY , "

                End If


            Else
                camposDef += nombreCampoVal & " " & nombreTipo & " IDENTITY PRIMARY KEY , "

            End If

            'Campos por valor
            For Each cv In pObjeto.CamposVal
                Dim infoDatosMapInstCampo As InfoDatosMapInstCampoDN = Nothing
                nombreCampoVal = cv.NombreMap
                If (Not infoMapDatosInst Is Nothing) Then
                    infoDatosMapInstCampo = infoMapDatosInst.GetCampoXNombre(cv.Campo.Name)
                End If
                'Si el campo es un campo de persistencia contenida hay que concatenar el nombre de la clase que los agrupa.
                'Se decide el tipo de la coludna
                'Select Case cv.NombreMap

                '    Case "GUIDReferida" ' se trata de una huella luego debe ser clave unica

                '        nombreTipo = SeleccionTypoCampoSQLS(cv.Campo.FieldType, cv.NombreMap, infoDatosMapInstCampo)
                '        camposDef += nombreCampoVal & " " & nombreTipo & "  PRIMARY KEY ,"


                '    Case "GUID" ' debe ser unico y no permitir nulos
                '        nombreTipo = SeleccionTypoCampoSQLS(cv.Campo.FieldType, cv.NombreMap, infoDatosMapInstCampo)
                '        camposDef += nombreCampoVal & " " & nombreTipo & " ,"
                '        sqlIndices = "CREATE UNIQUE INDEX index" & nombreCampoVal & " ON " & pObjeto.TablaNombre.Replace("`"c, "-"c) & " (" & nombreCampoVal & ")"

                '    Case Else
                '        nombreTipo = SeleccionTypoCampoSQLS(cv.Campo.FieldType, cv.NombreMap, infoDatosMapInstCampo)
                '        camposDef += nombreCampoVal & " " & nombreTipo & ","

                'End Select

                nombreTipo = SeleccionTypoCampoSQLS(cv.Campo.FieldType, cv.NombreMap, infoDatosMapInstCampo)
                camposDef += nombreCampoVal & " " & nombreTipo & ","

            Next

            ' TODO: posiblemente esto debiera ser recursivo
            'Campos por referencia contenidos
            For Each cr In pObjeto.CamposRefContenidos
                Dim infoDatosMapInstCampo As InfoDatosMapInstCampoDN = Nothing
                If (Not infoMapDatosInst Is Nothing) Then
                    infoDatosMapInstCampo = infoMapDatosInst.GetCampoXNombre(cr.Campo.Name)
                End If

                If (Not infoDatosMapInstCampo Is Nothing AndAlso Not infoDatosMapInstCampo.ColCampoAtributo.Contains(CampoAtributoDN.SoloGuardarYNoReferido) AndAlso Not infoDatosMapInstCampo.ColCampoAtributo.Contains(CampoAtributoDN.NoProcesar) AndAlso infoDatosMapInstCampo.ColCampoAtributo.Contains(CampoAtributoDN.PersistenciaContenidaSerializada)) Then
                    nombreTipo = SeleccionTypoCampoSQLS(GetType(System.Byte()), cr.NombreMap, infoDatosMapInstCampo)
                    camposDef += cr.NombreMap & " " & nombreTipo & ","
                End If
            Next

            'Campos por referencia externos
            For Each cr In pObjeto.CamposRefExteriores
                'Si el tipo es una interface requerira datos de mapeo que indiquen que clases DN implementan esa interface en el sistema,
                'y se  se agregara a la tabla un campo por cada una de ellas y se establecera una relacion con cada una
                If (Not cr.Campo.FieldType.IsInterface) Then
                    infoDatosMapInstCampoClaseHeredada = gdmi.RecuperarMapPersistenciaCampos(cr.Campo.FieldType)
                    If Not infoDatosMapInstCampoClaseHeredada Is Nothing Then
                        DatosMapClaseHeredable = infoDatosMapInstCampoClaseHeredada.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor)
                    End If
                End If

                If (cr.Campo.FieldType.IsInterface) OrElse Not DatosMapClaseHeredable Is Nothing Then
                    Dim infoDatosMapInstCampo As InfoDatosMapInstCampoDN = Nothing
                    If Not infoMapDatosInst Is Nothing Then
                        infoDatosMapInstCampo = infoMapDatosInst.GetCampoXNombre(cr.Campo.Name)
                    End If

                    camposDef += Me.TratarInterfaceGeneracionCampos(cr, infoDatosMapInstCampo)

                Else
                    If Not cr.Campo.FieldType.GetInterface("IEnumerable", True) Is Nothing Then
                        'Campos por referencia en relacion 1-*
                        'Esta parte es trabajo de la creacion de las relaciones que es posterior

                    Else
                        camposDef += GenerarNombreCampoRefContiguoUnoUno(cr, String.Empty)
                    End If
                End If
            Next


            'si el nombre del objeto contine el caracter "`" quiere decir que es es una clase generica y el nombre debe el objeto debe complementarse con el valor
            'tipo fijado



            '''''''''''''''''''''''''''''''''''''''''''''''
            ' crear las restricciones de unicidad sobre campos de la  tabla principal
            ''''''''''''''''''''''''''''''''''''''''''''''''''''


            Dim campoval As Framework.TiposYReflexion.DN.InfoTypeInstCampoValDN
            Dim camporef As Framework.TiposYReflexion.DN.InfoTypeInstCampoRefDN
            Dim mapeadoCampo As Framework.TiposYReflexion.DN.InfoDatosMapInstCampoDN
            Dim sqlRestricciones As String = ""


            If Not infoMapDatosInst Is Nothing Then


                For Each campoval In pObjeto.CamposValOriginal
                    mapeadoCampo = infoMapDatosInst.GetCampoXNombre(campoval.Campo.Name)

                    If Not mapeadoCampo Is Nothing AndAlso mapeadoCampo.ColCampoAtributo.Contains(CampoAtributoDN.UnicoEnFuenteDatosoNulo) Then
                        sqlRestricciones += "/#/" & RestriccionCampoUnico(pObjeto.TablaNombre.Replace("`"c, "-"c), campoval.NombreMap)
                    End If

                Next

                For Each camporef In pObjeto.CamposRefContenidos

                    mapeadoCampo = infoMapDatosInst.GetCampoXNombre(camporef.Campo.Name)

                    If Not mapeadoCampo Is Nothing AndAlso mapeadoCampo.ColCampoAtributo.Contains(CampoAtributoDN.UnicoEnFuenteDatosoNulo) Then
                        ' todos los campos de valor provinientes de este campo ref deben de ser unicos o nulos

                        For Each campoval In pObjeto.CamposVal
                            If campoval.CampoRefPAdre Is camporef Then
                                If Not mapeadoCampo Is Nothing AndAlso mapeadoCampo.ColCampoAtributo.Contains(CampoAtributoDN.UnicoEnFuenteDatosoNulo) Then
                                    sqlRestricciones += "/#/" & RestriccionCampoUnico(pObjeto.TablaNombre.Replace("`"c, "-"c), campoval.NombreMap)
                                End If
                            End If

                        Next

                    End If




                Next

            End If



            'Union de las partes de la sql de inserion
            camposDef = camposDef.Substring(0, camposDef.Length - 1) 'Quitamos la coma final
            sql = " Create Table  " & pObjeto.TablaNombre.Replace("`"c, "-"c) & " (" & camposDef & ")"
            '  If sqlRestricciones <> "" Then
            If String.IsNullOrEmpty(sqlIndices) Then
                sql = sql & sqlRestricciones

            Else
                sql = sql & sqlRestricciones & ";" & sqlIndices

            End If



            ' procesado de tabla historica
            If pHistoricas Then
                If Not infoMapDatosInst Is Nothing AndAlso Not String.IsNullOrEmpty(infoMapDatosInst.TablaHistoria) Then
                    Dim sqlh As String = " Create Table  " & infoMapDatosInst.TablaHistoria & " (" & camposDef.Replace(" IDENTITY PRIMARY KEY", " PRIMARY KEY") & ")"
                    If String.IsNullOrEmpty(sqlIndices) Then
                        sqlh = sqlh & sqlRestricciones

                    Else
                        sqlh = sqlh & sqlRestricciones & ";" & sqlIndices

                    End If
                    'sql = sql & ";" & sqlh
                    Return sqlh
                End If
            End If




            ' End If
            Return sql
        End Function



        Private Function RestriccionCampoUnico(ByVal NombreTabla As String, ByVal NombreCampo As String) As String
            Dim sql As String
            sql = "CREATE  trigger #NombreTriger# on #NombreTabla# for insert, update as BEGIN  IF (select max(cnt) from (select count(i.#NombreCampo#) as cnt from #NombreTabla#, inserted i where #NombreTabla#.#NombreCampo#=i.#NombreCampo# group by i.#NombreCampo#) x) >1 raiserror('El #NombreCampo# ya existe en la tabla  #NombreTabla# ',16,1) END"

            sql = sql.Replace("#NombreTabla#", NombreTabla)
            sql = sql.Replace("#NombreCampo#", NombreCampo)
            sql = sql.Replace("#NombreTriger#", "ResCampoUnico" & NombreTabla & NombreCampo)

            Return sql
        End Function

        Private Function SeleccionTypoCampoIDSQLS(ByVal pTipo As System.Type) As String
            If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(pTipo) Then
                ' se trata de un guid
                Return "nvarchar (50)"
            Else
                Return "bigint" ' es un autoniumerico

            End If

        End Function

        Private Function SeleccionTypoCampoSQLS(ByVal pTipoCampoVal As System.Type, ByVal pNombreCampo As String, ByVal cmap As InfoDatosMapInstCampoDN) As String

            'Dim pNombreCampo As String
            'pNombreCampo = cv.NombreMap

            Dim pTamañoCampo As Double

            If Not cmap Is Nothing Then
                pTamañoCampo = cmap.TamañoCampo
            End If


            'Excepciones
            If (pNombreCampo.ToLower = "fechamodificacion") Then

                If pTamañoCampo = 0 Then
                    pTamañoCampo = 50
                End If



                Return "nvarchar (" & pTamañoCampo & ")"
            End If

            'Comportaminto estandar
            If (pTipoCampoVal Is GetType(System.DateTime)) Then
                Return "datetime"

            ElseIf (pTipoCampoVal Is GetType(System.String)) Then

                If pTamañoCampo = 0 Then
                    pTamañoCampo = 256
                End If


                Return "nvarchar (" & pTamañoCampo & ")"

            ElseIf (pTipoCampoVal Is GetType(System.Int16) OrElse pTipoCampoVal Is GetType(System.Int32) OrElse pTipoCampoVal Is GetType(System.Int64)) Then
                Return "bigint"

            ElseIf (pTipoCampoVal Is GetType(System.UInt16) OrElse pTipoCampoVal Is GetType(System.UInt32) OrElse pTipoCampoVal Is GetType(System.UInt64)) Then
                Return "bigint"

            ElseIf (pTipoCampoVal Is GetType(System.Double) OrElse pTipoCampoVal Is GetType(System.Decimal) OrElse pTipoCampoVal Is GetType(System.Single)) Then

                If pTamañoCampo = 0 Then
                    pTamañoCampo = 18
                End If

                Return "numeric(" & pTamañoCampo & ", 4)"

            ElseIf (pTipoCampoVal Is GetType(System.Boolean)) Then
                Return "bit"

            ElseIf (pTipoCampoVal Is GetType(System.Byte())) Then
                Return "image"

            ElseIf (pTipoCampoVal.IsEnum = True) Then
                Return "bigint"

            Else
                Throw New ApplicationException("Error: tipo de dato no controlado")
            End If
        End Function

        'Este metodo descompone un campo de  interface en un  ununto de campo que permite relaionarse con la tabla para las entidades que
        'el mapeado de datos diga que implementan la interface
        Private Function TratarInterfaceGeneracionCampos(ByVal pCampoRef As InfoTypeInstCampoRefDN, ByVal pInfoDatosMapInstCampo As InfoDatosMapInstCampoDN) As String
            Dim camposAñadidos As String = String.Empty
            Dim instancia As Object = Nothing
            Dim nombreDeTipo, sufijoSubCampo As String
            Dim Camino As String()
            Dim datosClaseInterface As InfoDatosMapInstClaseDN
            Dim alDAtosInterface As ArrayList
            Dim dato As Object
            Dim vc As VinculoClaseDN = Nothing
            Dim gdmi As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor
            Dim vinculoClase As VinculoClaseDN

            'If (Not pCampoRef.Campo.FieldType.IsInterface) Then

            '    ' alDAtosInterface
            '    Throw New ApplicationException("Error: no se trata de un parametro de tipo interface")
            'End If

            'Si la clase no dispone de informacion de mapeado para el campo que es una interface se intenta averiguar
            'si la propia interface expone datos de mapeo para indicar que clases la implementan 
            If (pInfoDatosMapInstCampo Is Nothing) Then
                'Obtener el mapeado de comportamiento de la entidad
                datosClaseInterface = gdmi.RecuperarMapPersistenciaCampos(pCampoRef.Campo.FieldType)
                alDAtosInterface = datosClaseInterface.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor)

                If alDAtosInterface Is Nothing Then
                    alDAtosInterface = datosClaseInterface.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor)
                End If

                If (Not alDAtosInterface Is Nothing) Then
                    For Each dato In alDAtosInterface
                        If (TypeOf dato Is VinculoClaseDN) Then
                            vc = dato
                        End If

                        Camino = vc.NombreClase.Split("."c)
                        sufijoSubCampo = Camino(Camino.GetUpperBound(0))
                        camposAñadidos += Me.GenerarNombreCampoRefContiguoUnoUno(pCampoRef, sufijoSubCampo)
                    Next

                    Return camposAñadidos

                Else
                    Throw New ApplicationException("Error: no hay informacion suiciente para resolver esta interface")
                End If

            Else
                If (pInfoDatosMapInstCampo.ColCampoAtributo.Contains(CampoAtributoDN.NoProcesar)) Then
                    Return Nothing
                End If

                'En este caso el campo es una interface y se reemplazara por las relaciones con las clases de los posiles objetos que pudiera contener
                If (Not pInfoDatosMapInstCampo Is Nothing AndAlso pInfoDatosMapInstCampo.ColCampoAtributo.Contains(CampoAtributoDN.InterfaceImplementadaPor)) Then
                    'Si la interface presenta una intancia se procesa solo el tipo de la instancia
                    If (Not instancia Is Nothing) Then
                        Throw New ApplicationException("Error: por implementar")

                    Else
                        If (pInfoDatosMapInstCampo.Datos.Count = 0) Then
                            If (Not pInfoDatosMapInstCampo.MapSubEntidad Is Nothing) Then
                                alDAtosInterface = pInfoDatosMapInstCampo.MapSubEntidad.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor)

                                For Each vinculoClase In alDAtosInterface
                                    Camino = vinculoClase.TipoClase.Name.Split("."c)
                                    sufijoSubCampo = Camino(Camino.GetUpperBound(0))
                                    camposAñadidos += Me.GenerarNombreCampoRefContiguoUnoUno(pCampoRef, sufijoSubCampo)
                                Next
                            End If

                        Else
                            For Each nombreDeTipo In pInfoDatosMapInstCampo.Datos
                                Camino = nombreDeTipo.Split("."c)
                                sufijoSubCampo = Camino(Camino.GetUpperBound(0))
                                camposAñadidos += Me.GenerarNombreCampoRefContiguoUnoUno(pCampoRef, sufijoSubCampo)
                            Next
                        End If


                        Return camposAñadidos
                    End If
                Else
                    Throw New ApplicationException("Error: no se puede generar ningun tabla para este interface. Falta informacion")
                End If
            End If
        End Function

        Private Function GenerarNombreCampoRefUnoUno(ByVal pCampoRef As InfoTypeInstCampoRefDN, ByVal CampoInterface As String) As String
            Dim nombreCampoRef As String
            If CampoInterface = String.Empty Then
                nombreCampoRef = "id" & pCampoRef.NombreMap

            Else
                nombreCampoRef = "id" & pCampoRef.NombreMap & CampoInterface
            End If

            Return nombreCampoRef
        End Function

        Private Function GenerarNombreCampoRefContiguoUnoUno(ByVal pCampoRef As InfoTypeInstCampoRefDN, ByVal pCampoInterface As String) As String
            Return GenerarNombreCampoRefUnoUno(pCampoRef, pCampoInterface) & " " & SeleccionTypoCampoIDSQLS(pCampoRef.Campo.FieldType) & ","
        End Function

        Public Overloads Function ConstSqlRelacionUnoN(ByVal pContenedor As Framework.DatosNegocio.IEntidadBaseDN, ByVal pContenido As Framework.DatosNegocio.IEntidadBaseDN, ByVal pNombreTabla As String, ByVal pCampoTodo As String, ByVal pCampoDestino As String, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date, ByVal pNumeroInstancia As Int64) As String Implements IConstructorSQLAD.ConstSqlRelacionUnoN
            Dim sql As String = String.Empty

            If (pParametros Is Nothing) Then
                pParametros = New List(Of IDataParameter)
            End If

            sql += " insert " & pNombreTabla & "(" & pCampoTodo & "," & pCampoDestino & ") VALUES (@" & pCampoTodo & ",@" & pCampoDestino & pNumeroInstancia & ")"

            Dim tipoFijado As System.Type

            Dim o As Object = pContenido


            If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsColeccion(o.GetType) Or Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaTipada(o.GetType) Then

                tipoFijado = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(CType(pContenido, Object).GetType, Nothing)
            Else

                tipoFijado = o.GetType

            End If




            If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(tipoFijado) Then
                pParametros.Add(ParametrosConstAD.ConstParametroString("@" & pCampoDestino & pNumeroInstancia, pContenido.GUID))

            Else
                pParametros.Add(ParametrosConstAD.ConstParametroID("@" & pCampoDestino & pNumeroInstancia, pContenido.ID))

            End If

            Return sql
        End Function

        Private Function ConstruirDatosVarios(ByVal pDR As System.Data.IDataReader, ByVal pTablaPrevia As Hashtable) As Object
            Dim lista As ArrayList 'Representa una coleccion de entidades
            Dim tabla As Hashtable 'Representa una entidad
            Dim i As Integer

            'Leemos el DataReader y cargamos los datos en un ArrayList
            lista = New ArrayList
            lista.Add(pTablaPrevia)

            tabla = New Hashtable
            For i = 0 To pDR.FieldCount - 1
                tabla.Add(pDR.GetName(i), pDR.GetValue(i))
            Next

            lista.Add(tabla)
            tabla = New Hashtable

            Do While pDR.Read()
                For i = 0 To pDR.FieldCount - 1
                    tabla.Add(pDR.GetName(i), pDR.GetValue(i))
                Next

                lista.Add(tabla)
                tabla = New Hashtable
            Loop

            'Seleccionamos el retorno adecuado
            Select Case lista.Count
                Case Is = 0
                    Return Nothing

                Case Is = 1
                    Return lista.Item(0)

                Case Is > 1
                    Return lista
            End Select

            Return Nothing
        End Function

        Public Function ConstruirDatos(ByVal pDR As System.Data.IDataReader) As Object Implements IConstructorSQLAD.ConstruirDatos
            Dim tabla As New Hashtable
            Dim lista As New ArrayList
            Dim i As Integer
            Dim posicion As Integer
            Dim alIDs As ArrayList
            Dim nombre As String

            'La primera sql ha de devolver los datos del objeto principal y solo debiera contener un registro
            If (pDR.Read()) Then
                'Rellenamos los valores para los campos "directos" de la entidad
                For i = 0 To pDR.FieldCount - 1
                    tabla.Add(pDR.GetName(i), pDR.GetValue(i))
                Next
            End If

            'Si hay mas de una fila
            If (pDR.Read()) Then
                Return ConstruirDatosVarios(pDR, tabla)

            Else
                ' El resto de los campos indirectos se devuelven en forma de AL de IDs en el orden en el que fueron encadenados en la cadena sql
                Do While (pDR.NextResult)
                    posicion += 1
                    alIDs = New ArrayList
                    nombre = String.Empty

                    Do While pDR.Read()
                        alIDs.Add(pDR.GetValue(0))
                        nombre = pDR.GetName(0)
                    Loop

                    If (Not nombre Is Nothing AndAlso Not nombre = String.Empty) Then
                        tabla.Add(nombre, alIDs)
                    End If
                Loop
            End If

            If (tabla.Count = 0) Then
                Return Nothing

            Else
                Return tabla
            End If
        End Function

        Public Overloads Function ConstSqlRelacionUnoN(ByVal pObjeto As InfoTypeInstClaseDN, ByVal pCampoRef As InfoTypeInstCampoRefDN, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date, ByRef pSqlHistorica As String) As List(Of SqlParametros) Implements IConstructorSQLAD.ConstSqlRelacionUnoN
            'Dim colValidable As Framework.DatosNegocio.IValidable
            '      Dim colValidable As IList
            'Dim validadorTipos As Framework.DatosNegocio.ValidadorTipos
            Dim tipoFijado As System.Type
            Dim grc As GestorRelacionesCampoLN
            Dim ColRelacionUnoNSqls As ListaRelacionUnoNSqlsDN
            'Dim RelacionUnoNSqls As RelacionUnoNSQLsDN
            'Dim entidad As Framework.DatosNegocio.IEntidadBaseDN
            'Dim objeto As Object
            'Dim colecion As IEnumerable
            'Dim numeroEntidad As Int16

            'Se presupone que el campo es una coleccion validable
            If (pCampoRef.Campo.FieldType.GetInterface("IEnumerable", True)) Is Nothing Then
                Throw New ApplicationException("Error: el campo no es una coleccion")
            End If

            If (pParametros Is Nothing) Then
                pParametros = New List(Of IDataParameter)
            End If

            grc = New GestorRelacionesCampoLN
            'colValidable = pCampoRef.Valor

            'If (colValidable Is Nothing) Then
            '    colValidable = Activator.CreateInstance(pCampoRef.Campo.FieldType)
            'End If


            Dim tipoTipofijado As FijacionDeTipoDN
            tipoFijado = InstanciacionReflexionHelperLN.ObtenerTipoFijado(pCampoRef.Campo.FieldType, tipoTipofijado)

            ColRelacionUnoNSqls = grc.GenerarRelacionesCampoRef(pObjeto, pCampoRef)



            Dim gdmi As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor
            Dim mapCampo As Framework.TiposYReflexion.DN.InfoDatosMapInstClaseDN = gdmi.RecuperarMapPersistenciaCampos(tipoFijado)
            Dim mapClase As Framework.TiposYReflexion.DN.InfoDatosMapInstClaseDN = gdmi.RecuperarMapPersistenciaCampos(pObjeto.Tipo)

            If Not mapCampo Is Nothing AndAlso Not String.IsNullOrEmpty(mapCampo.TablaHistoria) OrElse Not mapClase Is Nothing AndAlso Not String.IsNullOrEmpty(mapClase.TablaHistoria) Then
                ColRelacionUnoNSqls.AddRange(ColRelacionUnoNSqls.CrearClonHistorico(mapClase, mapCampo))
            End If


            Return ConstSqlRelacionUnoNp(pObjeto, pFechaModificacion, tipoFijado, pParametros, pCampoRef, ColRelacionUnoNSqls)


            'Si el campo es una interface
            'If (tipoFijado.IsInterface) Then
            '    RelacionUnoNSqls = ColRelacionUnoNSqls(0)
            '    pParametros.Add(ParametrosConstAD.ConstParametroID("@" & RelacionUnoNSqls.CampoTodoTR, pObjeto.InstanciaPrincipal.ID))

            '    'Borrar las relaciones para todas las posibles tablas relacionales
            '    For Each RelacionUnoNSqls In ColRelacionUnoNSqls
            '        sql += "; Delete " & RelacionUnoNSqls.NombreTablaRel & " WHERE " & RelacionUnoNSqls.CampoTodoTR & "=@" & RelacionUnoNSqls.CampoTodoTR
            '    Next

            '    For Each entidad In colecion ' ojito con esto *****
            '        numeroEntidad += 1
            '        objeto = entidad

            '        RelacionUnoNSqls = ColRelacionUnoNSqls.GetRelacionDeTipoAParte(objeto.GetType)
            '        If RelacionUnoNSqls Is Nothing Then
            '            Throw New Exception("falta información para resolver la interface")
            '        End If

            '        sql += ";" & Me.ConstSqlRelacionUnoN(pObjeto.InstanciaPrincipal, entidad, RelacionUnoNSqls.NombreTablaRel, RelacionUnoNSqls.CampoTodoTR, RelacionUnoNSqls.CampoParteTR, pParametros, pFechaModificacion, numeroEntidad)
            '    Next

            'Else
            '    'El campo fijado no es una interface
            '    RelacionUnoNSqls = ColRelacionUnoNSqls(0)
            '    pParametros.Add(ParametrosConstAD.ConstParametroID("@" & RelacionUnoNSqls.CampoTodoTR, pObjeto.InstanciaPrincipal.ID))
            '    sql = "; Delete " & RelacionUnoNSqls.NombreTablaRel & " WHERE " & RelacionUnoNSqls.CampoTodoTR & "=@" & RelacionUnoNSqls.CampoTodoTR

            '    'Para cada entidad contenida en la relacion inserto las vinculaciones en la tabla relacional
            '    For Each entidad In colecion
            '        If entidad IsNot Nothing Then
            '            numeroEntidad += 1
            '            sql += ";" & Me.ConstSqlRelacionUnoN(pObjeto.InstanciaPrincipal, entidad, RelacionUnoNSqls.NombreTablaRel, RelacionUnoNSqls.CampoTodoTR, RelacionUnoNSqls.CampoParteTR, pParametros, pFechaModificacion, numeroEntidad)
            '        End If
            '    Next
            'End If

            'Return sql.Substring(1)
        End Function




        Private Function ConstSqlRelacionUnoNp(ByVal pObjeto As InfoTypeInstClaseDN, ByVal pFechaModificacion As Date, ByVal tipoFijado As System.Type, ByRef pParametros As List(Of IDataParameter), ByVal pCampoRef As InfoTypeInstCampoRefDN, ByVal ColRelacionUnoNSqls As ListaRelacionUnoNSqlsDN) As List(Of SqlParametros)



            Dim RelacionUnoNSqls As RelacionUnoNSQLsDN
            Dim entidad As Framework.DatosNegocio.IEntidadBaseDN
            Dim objeto As Object
            ' Dim colecion As IEnumerable = pCampoRef.Valor
            Dim numeroEntidad As Int16
            Dim colValidable As IList

            Dim colSqlParametros As New List(Of SqlParametros)
            Dim SqlParametros As New SqlParametros
            SqlParametros.Parametros = pParametros ' el primer grupo de parametros es el que me pasan
            colSqlParametros.Add(SqlParametros)


            colValidable = pCampoRef.Valor

            If (colValidable Is Nothing) Then
                colValidable = Activator.CreateInstance(pCampoRef.Campo.FieldType)
            End If



            If (tipoFijado.IsInterface) Then


                ' esta paarte del codigo no esta probada para tablas HISTORICAS
                RelacionUnoNSqls = ColRelacionUnoNSqls(0)
                SqlParametros.Parametros.Add(ParametrosConstAD.ConstParametroID("@" & RelacionUnoNSqls.CampoTodoTR, pObjeto.InstanciaPrincipal.ID))

                'Borrar las relaciones para todas las posibles tablas relacionales
                For Each RelacionUnoNSqls In ColRelacionUnoNSqls
                    SqlParametros.sql += "; Delete " & RelacionUnoNSqls.NombreTablaRel & " WHERE " & RelacionUnoNSqls.CampoTodoTR & "=@" & RelacionUnoNSqls.CampoTodoTR
                Next

                For Each entidad In colValidable ' ojito con esto *****
                    numeroEntidad += 1
                    objeto = entidad

                    RelacionUnoNSqls = ColRelacionUnoNSqls.GetRelacionDeTipoAParte(objeto.GetType)
                    If RelacionUnoNSqls Is Nothing Then
                        Throw New Exception("falta información para resolver la interface")
                    End If


                    ' solo se pueden mandar 2100 parametros
                    Dim multiplo As Double = numeroEntidad / 2000
                    If Int(multiplo) = multiplo Then
                        SqlParametros = New SqlParametros
                        SqlParametros.Parametros.Add(ParametrosConstAD.ConstParametroID("@" & RelacionUnoNSqls.CampoTodoTR, pObjeto.InstanciaPrincipal.ID))
                        colSqlParametros.Add(SqlParametros)
                    End If

                    SqlParametros.sql += ";" & Me.ConstSqlRelacionUnoN(pObjeto.InstanciaPrincipal, entidad, RelacionUnoNSqls.NombreTablaRel, RelacionUnoNSqls.CampoTodoTR, RelacionUnoNSqls.CampoParteTR, SqlParametros.Parametros, pFechaModificacion, numeroEntidad)
                Next

            Else

                'El campo fijado no es una interface
                For Each RelacionUnoNSqls In ColRelacionUnoNSqls
                    pParametros.Add(ParametrosConstAD.ConstParametroID("@" & RelacionUnoNSqls.CampoTodoTR, pObjeto.InstanciaPrincipal.ID))
                    SqlParametros.sql += "; Delete " & RelacionUnoNSqls.NombreTablaRel & " WHERE " & RelacionUnoNSqls.CampoTodoTR & "=@" & RelacionUnoNSqls.CampoTodoTR

                    'Para cada entidad contenida en la relacion inserto las vinculaciones en la tabla relacional
                    For Each entidad In colValidable
                        If entidad IsNot Nothing Then
                            numeroEntidad += 1
                            ' solo se pueden encadenar hastra 2100 cadenas
                            Dim multiplo As Double = numeroEntidad / 2000
                            If Int(multiplo) = multiplo Then
                                SqlParametros = New SqlParametros
                                SqlParametros.Parametros.Add(ParametrosConstAD.ConstParametroID("@" & RelacionUnoNSqls.CampoTodoTR, pObjeto.InstanciaPrincipal.ID))
                                colSqlParametros.Add(SqlParametros)
                            End If

                            SqlParametros.sql += Me.ConstSqlRelacionUnoN(pObjeto.InstanciaPrincipal, entidad, RelacionUnoNSqls.NombreTablaRel, RelacionUnoNSqls.CampoTodoTR, RelacionUnoNSqls.CampoParteTR, SqlParametros.Parametros, pFechaModificacion, numeroEntidad)
                        End If
                    Next
                Next

            End If


            Return colSqlParametros

        End Function


#End Region


        Public Function ConstruirSQLBusqueda1(ByVal pTypo As System.Type, ByVal pNombreVistaFiltro As String, ByVal pFiltro As System.Collections.Generic.List(Of DN.CondicionRelacionalDN), ByRef pParametros As System.Collections.Generic.List(Of System.Data.IDataParameter)) As String Implements IConstructorBusquedaAD.ConstruirSQLBusqueda

            If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(pTypo) Then

                Return "select GUID from tl" & pTypo.Name
            Else
                Return "select id from tl" & pTypo.Name
            End If


        End Function



    End Class




    Public Class SqlParametros
        Public sql As String
        Public Parametros As New List(Of IDataParameter)
    End Class


End Namespace

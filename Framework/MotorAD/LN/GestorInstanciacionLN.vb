#Region "Importaciones"

Imports System.Collections.Generic
Imports System.Reflection

Imports Framework.AccesoDatos
Imports Framework.DatosNegocio
Imports Framework.LogicaNegocios
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos.MotorAD.AD
Imports Framework.AccesoDatos.MotorAD.DN
Imports Framework.TiposYReflexion.DN
Imports Framework.TiposYReflexion.LN

#End Region

Namespace LN
    Public Delegate Sub AutorizarOperacionEnEntidad(ByVal entidad As Object, ByVal operacion As Framework.LogicaNegocios.OperacionGuardarLN, ByRef autorizada As Boolean)

    Public Class GestorInstanciacionLN
        Inherits BaseTransaccionLN
        Implements IBaseMotorLN

#Region "Atributos"
        Private Shared mColSQLTablasCreadas As New ArrayList

        Private Shared mColSQLDiferidas As New ArrayList
        Private Shared mTablasGeneradasParaTipos As New ArrayList
        Private Shared mGestorMapPersistenciaCampos As GestorMapPersistenciaCamposLN
        Private mColIntanciasGuardadas As New ArrayList
        Private mColIntanciasRecuperadas As Hashtable   'su clave es el nombre completo de la clase + el id de la intancia
        Private mColCampoPostReuperacion As List(Of CampoPostRecuperacionDN)
        Private mColIntanciasPostGuarda As New ArrayList
        Private mDelegadoAutorizacion As AutorizarOperacionEnEntidad


#End Region

#Region "Constructores"
        Public Sub New(ByVal ptl As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
            MyBase.New(ptl, pRec)
            mColIntanciasRecuperadas = New Hashtable
        End Sub
#End Region

#Region "Propiedades"
        Public Shared Property TablasGeneradasParaTipos() As ArrayList
            Get
                Return mTablasGeneradasParaTipos
            End Get
            Set(ByVal value As ArrayList)
                mTablasGeneradasParaTipos = value
            End Set
        End Property
        Public Property ColIntanciasGuardadas() As ArrayList
            Get
                Return mColIntanciasGuardadas
            End Get
            Set(ByVal value As ArrayList)
                mColIntanciasGuardadas = value

            End Set
        End Property
        Public Property ColIntanciasRecuperadas() As Hashtable
            Get
                Return mColIntanciasRecuperadas
            End Get
            Set(ByVal value As Hashtable)
                mColIntanciasRecuperadas = value
            End Set
        End Property

        Public Sub AñadirAColIntanciasRecuperadas(ByVal ientidad As IEntidadBaseDN)
            mColIntanciasRecuperadas.Add(ientidad.ClaveEntidad, ientidad)
        End Sub
        Public Sub AñadirAColIntanciasRecuperadas(ByVal lista As IList)

            Dim ientidad As IEntidadBaseDN
            For Each ientidad In lista
                AñadirAColIntanciasRecuperadas(ientidad)
            Next

        End Sub


        Public Shared Property GestorMapPersistenciaCampos() As GestorMapPersistenciaCamposLN
            Get
                Return mGestorMapPersistenciaCampos
            End Get
            Set(ByVal value As GestorMapPersistenciaCamposLN)
                mGestorMapPersistenciaCampos = value
            End Set
        End Property

        Public Property DelegadoAutorizacion() As AutorizarOperacionEnEntidad
            Get
                Return mDelegadoAutorizacion
            End Get
            Set(ByVal value As AutorizarOperacionEnEntidad)
                mDelegadoAutorizacion = value
            End Set
        End Property
#End Region

#Region "Metodos"

        Public Shared Sub VaciarCacheTablasGeneradasParaTipos()
            mTablasGeneradasParaTipos = New ArrayList
        End Sub

        Private Sub TratarInterfacesGenerarTablas(ByVal pInfoTypeInstCampoRef As InfoTypeInstCampoRefDN, ByVal pInfoDatosMapCampo As InfoDatosMapInstCampoDN, ByVal GenerarRelaciones As Boolean, ByVal pHistoricas As Boolean, ByVal pRuta As String)
            Dim dato As Object
            Dim vc As VinculoClaseDN = Nothing
            Dim datoMapInterface As InfoDatosMapInstClaseDN
            Dim GestordatoMapInterface As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor

            Dim instancia As Object = Nothing
            Dim nombreDeTipo As String
            Dim ensamblado As Assembly
            Dim t As System.Type
            Dim PartesCamino As String()

            Dim datosClaseInterface As InfoDatosMapInstClaseDN
            Dim alDAtosInterface As ArrayList = Nothing

            Dim VinculoClase As VinculoClaseDN

            'If (Not pInfoTypeInstCampoRef.Campo.FieldType.IsInterface) Then
            '    Throw New ApplicationException("Error: no se trata de un parametro de tipo interface")
            'End If

            'Si la clase no dispone de informacion de mapeado para el campo que es una interface 
            'se intenta averiguar si la propia interface expone datos de mapeo para indicar que clases la implementan 
            If (pInfoDatosMapCampo Is Nothing) Then
                'Obtener le mapeado de De comportamiento de la entidad
                Dim gdmi As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor

                datosClaseInterface = gdmi.RecuperarMapPersistenciaCampos(pInfoTypeInstCampoRef.Campo.FieldType)
                If (datosClaseInterface IsNot Nothing) Then
                    alDAtosInterface = datosClaseInterface.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor)
                    If alDAtosInterface Is Nothing Then
                        alDAtosInterface = datosClaseInterface.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor)

                    End If
                End If

                If (alDAtosInterface IsNot Nothing) Then
                    For Each dato In alDAtosInterface
                        If (TypeOf dato Is VinculoClaseDN) Then
                            vc = dato
                        End If

                        'Datos de mapeado por defecto de la clase que implementa la interface
                        datoMapInterface = GestordatoMapInterface.RecuperarMapPersistenciaCampos(vc.TipoClase)

                        Me.GenerarTablas(vc.TipoClase, datoMapInterface, GenerarRelaciones, pHistoricas, pRuta)
                    Next

                Else
                    Throw New ApplicationException("Error: no hay informacion suficiente para resolver esta interface -->" & pInfoTypeInstCampoRef.Campo.FieldType.FullName & " para el campo --> " & pInfoTypeInstCampoRef.Campo.Name & " del tipo -->" & pInfoTypeInstCampoRef.Campo.ReflectedType.Name)
                End If

                'Si la clase dispone de informacion de mapeado para el campo que es una interface ella define que clases DN acepta
            Else
                If (pInfoDatosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.NoProcesar)) Then
                    Exit Sub
                End If

                'En este caso el campo es una interface y se reemplazara por las relaciones con las clases de los posiles objetos que pudiera contener
                If (pInfoDatosMapCampo.ColCampoAtributo.Contains(CampoAtributoDN.InterfaceImplementadaPor)) Then
                    'Si la interface presenta una intancia se procesa solo el tipo de la instancia
                    If (instancia IsNot Nothing) Then
                        Throw New NotImplementedException("Error: por implementar")

                    Else
                        If (pInfoDatosMapCampo.Datos.Count = 0) Then
                            If (pInfoDatosMapCampo.MapSubEntidad IsNot Nothing) Then
                                alDAtosInterface = pInfoDatosMapCampo.MapSubEntidad.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor)

                                For Each VinculoClase In alDAtosInterface
                                    Me.GenerarTablas(VinculoClase.TipoClase, Nothing, GenerarRelaciones, pHistoricas, pRuta)
                                Next
                            End If

                        Else
                            For Each nombreDeTipo In pInfoDatosMapCampo.Datos
                                PartesCamino = nombreDeTipo.Split("."c)
                                'TODO: (Alex) Por implementaar esto debe de tener el nombre del ensamblado y de la clase claramente diferenciado
                                ensamblado = Assembly.Load(PartesCamino(0))
                                t = ensamblado.GetType(nombreDeTipo)
                                Me.GenerarTablas(t, Nothing, GenerarRelaciones, pHistoricas, pRuta)
                            Next
                        End If
                    End If

                Else
                    Throw New ApplicationException("Error: no se puede generar ninguna tabla para este interface. Falta informacion")
                End If
            End If
        End Sub

        'Recuperar informacion de mapeado para la interface (requerido)
        Private Sub GenerarTablasParaInterface(ByVal pTipo As System.Type, ByVal pDatosClaseInterface As InfoDatosMapInstClaseDN, ByVal GenerarRelaciones As Boolean, ByVal pHistoricas As Boolean, ByVal pRuta As String)
            Try

                Dim vc As VinculoClaseDN
                Dim dato As Object
                Dim gdmi As GestorMapPersistenciaCamposLN
                Dim alDAtosInterface As ArrayList = Nothing

                Debug.Indent()
                'Debug.WriteLine("GT >" & pTipo.Name)
                'If (Not pTipo.IsInterface) Then
                '    Throw New ApplicationException("Error: el tipo pasado no es una interface")
                'End If

                If (pDatosClaseInterface Is Nothing) Then
                    gdmi = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor
                    pDatosClaseInterface = gdmi.RecuperarMapPersistenciaCampos(pTipo)
                End If

                If (pDatosClaseInterface IsNot Nothing) Then
                    alDAtosInterface = pDatosClaseInterface.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor)
                End If
                If (alDAtosInterface Is Nothing) Then
                    If pDatosClaseInterface Is Nothing Then
                        Throw New Framework.AccesoDatos.ApplicationExceptionAD("No existe informacion de mapeado para la interface " & pTipo.FullName & " " & pRuta)
                    Else
                        alDAtosInterface = pDatosClaseInterface.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor)

                    End If
                End If
                If (alDAtosInterface IsNot Nothing) Then
                    For Each dato In alDAtosInterface
                        If (TypeOf dato Is VinculoClaseDN) Then
                            vc = dato
                            Me.GenerarTablas(vc.TipoClase, Nothing, GenerarRelaciones, pHistoricas, pRuta)
                        End If
                    Next

                Else

                    If Not pDatosClaseInterface Is Nothing Then
                        If pDatosClaseInterface.NombreCompleto Is Nothing Then
                            Throw New ApplicationException("Error: no hay informacion suiciente para resolver esta interface: " & pTipo.FullName.ToString & " RUTA: " & pRuta)

                        Else
                            Throw New ApplicationException("Error:No hay informacion suficiente para resolver esta interface: " & pDatosClaseInterface.NombreCompleto & ", de la clase:" & pDatosClaseInterface.NombreCompleto & " RUTA: " & pRuta)

                        End If
                    Else
                        Throw New ApplicationException("Error: no hay informacion suiciente para resolver esta interface: " & pTipo.FullName.ToString & " RUTA: " & pRuta)

                    End If



                End If
            Finally
                Debug.Unindent()
            End Try
        End Sub

        Public Sub GenerarTablasPara(ByVal pTipo As System.Type, ByVal pInfoMapDatosInst As InfoDatosMapInstClaseDN, ByVal GenerarRelaciones As Boolean, ByVal pHistoricas As Boolean)
            mColSQLDiferidas = New ArrayList
            Dim consulta As String
            Dim registrosAfectados As Int64
            Dim ej As Ejecutor

            Me.GenerarTablas(pTipo, pInfoMapDatosInst, GenerarRelaciones, pHistoricas, "")

            ej = New Ejecutor(Nothing, Me.mRec)

            Try
                Debug.Indent()
                'Debug.WriteLine("GT >" & pTipo.Name)
                For Each consulta In mColSQLDiferidas
                    registrosAfectados = ej.EjecutarNoConsulta(consulta)
                Next

            Catch exsql As SqlClient.SqlException
                If (Not exsql.Number = 2714) Then
                    Throw
                End If

            Catch ex As Exception
            Finally
                Debug.Unindent()
            End Try
        End Sub


        Public Sub GenerarTablas2(ByVal pTipo As System.Type, ByVal pInfoMapDatosInst As InfoDatosMapInstClaseDN)

            Me.GenerarTablas(pTipo, pInfoMapDatosInst, False, False, "")
            GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()
            Me.GenerarTablas(pTipo, pInfoMapDatosInst, True, False, "")


      '
            ' If Not pInfoMapDatosInst Is Nothing AndAlso Not String.IsNullOrEmpty(pInfoMapDatosInst.TablaHistoria) Then
            GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()
            Me.GenerarTablas(pTipo, pInfoMapDatosInst, False, True, "")
            GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()
            Me.GenerarTablas(pTipo, pInfoMapDatosInst, True, True, "")


            '  End If





            '  consultas SQL diferidas
            Try
                Dim ej As Ejecutor
                Dim registrosAfectados As Integer
                ej = New Ejecutor(Nothing, Me.mRec)
                For Each consulta As String In mColSQLDiferidas
                    registrosAfectados = ej.EjecutarNoConsulta(consulta)
                Next

            Catch exsql As SqlClient.SqlException
                If Not (exsql.Number = 2714 OrElse exsql.Number = 1767) Then
                    Throw
                End If

            Catch ex As Exception
                Throw
            End Try

            '-------------------------------------------------------------

            ' Me.GenerarRelacionesTablas(pTipo, pInfoMapDatosInst)
        End Sub

        Private Function CalcularRura(ByVal pRuta As String, ByVal campoRef As InfoTypeInstCampoRefDN) As String
            Return pRuta & campoRef.Campo.ReflectedType.FullName & "c:" & campoRef.Campo.Name & " as " & campoRef.Campo.FieldType.FullName
        End Function


        Private Function ExisteTabla(ByVal mapInst As InfoTypeInstClaseDN) As Boolean
            ' verificar si la tabla ya exite en la base de datos
            Return ExisteTabla(mapInst.TablaNombre)
        End Function
        Private Function ExisteTabla(ByVal nombretabla As String) As Boolean
            ' verificar si la tabla ya exite en la base de datos



            Dim miej As New Ejecutor(Nothing, Me.mRec)
            ' Dim valor As Integer = miej.EjecutarEscalar("SELECT count(id) FROM sysobjects where xtype ='U' and name='" & nombretabla & "'")
            Dim valor As Integer = miej.EjecutarEscalar("SELECT count(id) FROM sysobjects where  name='" & nombretabla & "'")

            If valor > 0 Then
                Return True
            End If


            Return False

        End Function

        Public Sub GenerarTablas(ByVal pTipo As System.Type, ByVal pInfoMapDatosInst As InfoDatosMapInstClaseDN, ByVal GenerarRelaciones As Boolean, ByVal pHistoricas As Boolean, ByVal pRuta As String)

            Dim coordinador As CTDLN
            Dim transProc As ITransaccionLogicaLN = Nothing
            Dim registrosAfectados As Int16
            Dim sqlCrearTB As String
            Dim tipo As System.Type
            'Dim colValidable As IValidable
            'Dim validadorTipos As ValidadorTipos
            Dim consSqlTablas As IConstructorTablasSQLAD
            Dim datosmapCampoInterface As InfoDatosMapInstCampoDN = Nothing
            Dim campoRef As InfoTypeInstCampoRefDN
            Dim giLN As GestorInstanciacionLN
            Dim tipoCampoReferido As System.Type
            Dim ej As Ejecutor

            coordinador = New CTDLN

            Try


                Debug.Indent()
                'Debug.WriteLine("GT >" & pTipo.Name)
                'No generar ls tablas si ya han sido generadas para ese tipo
                If (pTipo.IsInterface = False AndAlso mTablasGeneradasParaTipos.Contains(pTipo.FullName)) Then
                    Exit Sub
                Else
                    If (pTipo.IsInterface = False AndAlso pTipo.GetInterface("IEnumerable", True) Is Nothing) Then
                        mTablasGeneradasParaTipos.Add(pTipo.FullName)
                    End If
                End If

                If (pTipo.IsInterface) Then
                    Me.GenerarTablasParaInterface(pTipo, pInfoMapDatosInst, GenerarRelaciones, pHistoricas, pRuta)
                    Exit Sub
                End If





                Dim tipoDeFijado As FijacionDeTipoDN
                If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo) Then
                    tipo = pTipo
                Else
                    tipo = InstanciacionReflexionHelperLN.ObtenerTipoFijado(pTipo, tipoDeFijado)
                End If
                If (tipoDeFijado = FijacionDeTipoDN.ColeccionGenerica Or tipoDeFijado = FijacionDeTipoDN.ColeccionValidable) AndAlso (tipo.IsInterface) Then
                    Me.GenerarTablas(tipo, pInfoMapDatosInst, GenerarRelaciones, pHistoricas, pRuta)
                    Exit Sub
                End If




                'Obtener le mapeado de Composicion de la entidad

                Dim mapInst As InfoTypeInstClaseDN
                Dim datosmaCampoRefiereClaseHeredadaEnSistema As InfoDatosMapInstClaseDN

                mapInst = GestorCacheInfoTypeInstLN.RecuperarMapInstanciacion(tipo)

                'If ExisteTabla(mapInst) Then
                '    Exit Sub
                'End If




                'Obtener le mapeado de comportamiento de la entidad (solo si no es aportado por el campo de la entidad contendora)
                Dim gdmi As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor

                If (pInfoMapDatosInst Is Nothing) Then
                    pInfoMapDatosInst = gdmi.RecuperarMapPersistenciaCampos(tipo)
                End If

                Dim datosCalseHeredada As Object = Nothing
                If (pInfoMapDatosInst IsNot Nothing) Then
                    datosCalseHeredada = pInfoMapDatosInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor)
                End If
                If Not datosCalseHeredada Is Nothing Then
                    ' se trata de una clase heredada por otras luego hay que tratarla como si fuera una interface

                    Me.GenerarTablasParaInterface(pTipo, pInfoMapDatosInst, GenerarRelaciones, pHistoricas, pRuta)

                End If



                'Crear Las Tablas para las entidades Referidas de modo externo
                giLN = New GestorInstanciacionLN(transProc, Me.mRec)

                For Each campoRef In mapInst.CamposRefExteriores
                    'Debug.WriteLine(campoRef.Campo.Name)
                    tipoCampoReferido = campoRef.Campo.FieldType

                    If (pInfoMapDatosInst IsNot Nothing) Then
                        datosmapCampoInterface = pInfoMapDatosInst.GetCampoXNombre(campoRef.Campo.Name)
                    Else
                        datosmaCampoRefiereClaseHeredadaEnSistema = gdmi.RecuperarMapPersistenciaCampos(tipoCampoReferido)
                        If Not datosmaCampoRefiereClaseHeredadaEnSistema Is Nothing Then
                            datosCalseHeredada = datosmaCampoRefiereClaseHeredadaEnSistema.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor)
                        End If

                    End If




                    If (tipoCampoReferido.IsInterface OrElse datosCalseHeredada IsNot Nothing) Then
                        If (datosmapCampoInterface IsNot Nothing) Then
                            If datosmapCampoInterface.ColCampoAtributo.Contains(CampoAtributoDN.NoProcesar) Then

                            ElseIf datosmapCampoInterface.ColCampoAtributo.Contains(CampoAtributoDN.PersistenciaContenidaSerializada) Then
                                Me.TratarInterfacesGenerarTablas(campoRef, datosmapCampoInterface, GenerarRelaciones, pHistoricas, pRuta)

                            ElseIf datosmapCampoInterface.ColCampoAtributo.Contains(CampoAtributoDN.InterfaceImplementadaPor) Then
                                Me.TratarInterfacesGenerarTablas(campoRef, datosmapCampoInterface, GenerarRelaciones, pHistoricas, pRuta)
                            End If

                        Else
                            Me.TratarInterfacesGenerarTablas(campoRef, datosmapCampoInterface, GenerarRelaciones, pHistoricas, pRuta)
                        End If

                    Else
                        'El campo referido no es una interface
                        If (tipoCampoReferido Is pTipo) Then

                        Else
                            If (datosmapCampoInterface Is Nothing) Then
                                'El campo no posee informacion particular para los datos de mapeado luego el sistema  los recuperara 
                                giLN.GenerarTablas(tipoCampoReferido, Nothing, GenerarRelaciones, pHistoricas, CalcularRura(pRuta, campoRef))

                            Else
                                'El campo posee informacion particular para los datos de mapeado luego el sistema no los recuperara
                                'sino que usara los pasados
                                giLN.GenerarTablas(tipoCampoReferido, datosmapCampoInterface.MapSubEntidad, GenerarRelaciones, pHistoricas, CalcularRura(pRuta, campoRef))
                            End If
                        End If
                    End If
                Next


                '''''''''''''''''''''''''''''''''''''''''''
                ' Crear la tabla para la entidad principal
                '''''''''''''''''''''''''''''''''''''''''''


                ej = New Ejecutor(Nothing, Me.mRec)

                If Not GenerarRelaciones Then
                    ' Crear la tabla para la entidad principal
                    consSqlTablas = New ConstructorSQLSQLsAD

                    sqlCrearTB = consSqlTablas.ConstSqlCreateTable(mapInst, pHistoricas)

                    Dim partesSql As String()
                    Dim sqlej As String
                    Dim separadores(0) As String
                    separadores(0) = "/#/"

                    partesSql = sqlCrearTB.Split(separadores, StringSplitOptions.RemoveEmptyEntries)
                    'Ejecutamos la sql para insertar los datos

                    Try

                        For Each sqlej In partesSql

                            If ExisteTabla(MotorAD.AD.AccesorMotorAD.ObtenerNombreTablaSqlCreateTable(sqlej)) Then
                                Exit Sub
                            End If

                            registrosAfectados = ej.EjecutarNoConsulta(sqlej)
                            mColSQLTablasCreadas.Add(sqlej)

                        Next

                    Catch exsql As SqlClient.SqlException
                        If (Not exsql.Number = 2714) Then
                            Throw
                        End If
                    End Try



                    ''''''''''''''''''''''''''''''''''''''''''''''''
                    '' crear las restricciones de unicidad sobre campos de la  tabla principal
                    '''''''''''''''''''''''''''''''''''''''''''''''''''''
                    'Dim campoval As Framework.TiposYReflexion.DN.InfoTypeInstCampoValDN
                    'Dim mapeadoCampo As Framework.TiposYReflexion.DN.InfoDatosMapInstCampoDN




                    'For Each campoval In mapInst.CamposVal
                    '    mapeadoCampo = pInfoMapDatosInst.GetCampoXNombre(campoval.Campo.Name)
                    '    If mapeadoCampo.ColCampoAtributo.Contains(CampoAtributoDN.UnicoEnFuenteDatosoNulo) Then
                    '        consSqlTablas = New ConstructorSQLSQLsAD
                    '        'sqlCrearTB = consSqlTablas.

                    '        'Ejecutamos la sql para insertar los datos
                    '        ej = New Ejecutor(Nothing, Me.mRec)

                    '        Try
                    '            Debug.WriteLine(sqlCrearTB)
                    '            registrosAfectados = ej.EjecutarNoConsulta(sqlCrearTB)

                    '        Catch exsql As SqlClient.SqlException
                    '            If (Not exsql.Number = 2714) Then
                    '                Throw exsql
                    '            End If
                    '        End Try

                    '    End If

                    'Next




                Else


                    'Crear las relaciones  
                    Dim grc As GestorRelacionesCampoLN
                    Dim cr As InfoTypeInstCampoRefDN
                    Dim Relaciones As New ArrayList
                    Dim relacion As Object
                    Dim RelacionUnoUno As IRelacionUnoUnoDN
                    Dim RelacionUnoN As IRelacionUnoNDN


                    '*******************************************************************************

                    'Generar las relaciones
                    grc = New GestorRelacionesCampoLN
                    For Each cr In mapInst.CamposRefExteriores
                        Relaciones.Add(grc.GenerarRelacionesCampoRef(mapInst, cr))
                    Next

                    'Generar las relaciones en el caso de que se trate de una huella
                    If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo) Then
                        Dim tipofijado As System.Type
                        ' si es una huella of te ver el tipo fijado
                        If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaTipada(pTipo) Then
                            tipofijado = TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(pTipo, Nothing)
                            Relaciones.Add(grc.GenerarRelacionesHuella(mapInst, pTipo))

                        End If

                    End If





                    'Ejecutar las relaciones






                    For Each relacion In Relaciones
                        If (TypeOf relacion Is ListaRelacionUnoNSqlsDN) Then
                            For Each RelacionUnoN In relacion


                                ' crear el clon de relaciones si hay tabla historica
                                'Dim RelacionUnoNHistorica As IRelacionUnoNDN

                                'If Not pInfoMapDatosInst Is Nothing AndAlso Not String.IsNullOrEmpty(pInfoMapDatosInst.TablaHistoria) Then
                                '    RelacionUnoNHistorica = RelacionUnoN.CrearClonHistorico(pInfoMapDatosInst.TablaHistoria)
                                'End If
                                If pHistoricas AndAlso Not pInfoMapDatosInst Is Nothing AndAlso Not String.IsNullOrEmpty(pInfoMapDatosInst.TablaHistoria) Then
                                    Dim tipofijado As System.Type
                                    tipofijado = TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(RelacionUnoN.TipoParte, Nothing)
                                    RelacionUnoN = RelacionUnoN.CrearClonHistorico(pInfoMapDatosInst, gdmi.RecuperarMapPersistenciaCampos(tipofijado))

                                End If


                                Try
                                    If Not Me.ExisteTabla(Framework.AccesoDatos.MotorAD.AD.AccesorMotorAD.ObtenerNombreTablaSqlCreateTable(RelacionUnoN.SqlTablaRel)) Then
                                        ej.EjecutarNoConsulta(RelacionUnoN.SqlTablaRel)
                                    End If

                                    'If Not RelacionUnoNHistorica Is Nothing Then
                                    '    ej.EjecutarNoConsulta(RelacionUnoNHistorica.SqlTablaRel)
                                    'End If


                                Catch exsql As SqlClient.SqlException
                                    If (Not exsql.Number = 2714) Then
                                        Throw
                                    End If
                                End Try

                                Try

                                    If Not Me.ExisteTabla(Framework.AccesoDatos.MotorAD.AD.AccesorMotorAD.ObtenerNombreTablaSqlCONSTRAINT(RelacionUnoN.SqlRelacionTodo)) Then
                                        ej.EjecutarNoConsulta(RelacionUnoN.SqlRelacionTodo)
                                    End If


                                    'If Not RelacionUnoNHistorica Is Nothing Then
                                    '    ej.EjecutarNoConsulta(RelacionUnoNHistorica.SqlRelacionTodo)
                                    'End If






                                Catch exsql As SqlClient.SqlException
                                    If (Not exsql.Number = 2714) Then
                                        Throw
                                    End If
                                End Try

                                Try

                                    If Not Me.ExisteTabla(Framework.AccesoDatos.MotorAD.AD.AccesorMotorAD.ObtenerNombreTablaSqlCONSTRAINT(RelacionUnoN.SqlRelacionParte)) Then
                                        ej.EjecutarNoConsulta(RelacionUnoN.SqlRelacionParte)
                                    End If


                                    'If Not RelacionUnoNHistorica Is Nothing Then
                                    '    ej.EjecutarNoConsulta(RelacionUnoNHistorica.SqlRelacionParte)
                                    'End If

                                Catch exsql As SqlClient.SqlException
                                    If (Not exsql.Number = 2714) Then
                                        Throw
                                    End If
                                End Try
                            Next
                        End If

                        If (TypeOf relacion Is List(Of RelacionUnoUnoSQLsDN)) Then
                            For Each RelacionUnoUno In relacion



                                'Dim RelacionUnoUnoHistorica As IRelacionUnoUnoDN
                                'If Not pInfoMapDatosInst Is Nothing AndAlso Not String.IsNullOrEmpty(pInfoMapDatosInst.TablaHistoria) Then
                                '    RelacionUnoUnoHistorica = RelacionUnoUno.CrearClonHistorico(pInfoMapDatosInst.TablaHistoria)
                                'End If

                                If pHistoricas AndAlso Not pInfoMapDatosInst Is Nothing AndAlso Not String.IsNullOrEmpty(pInfoMapDatosInst.TablaHistoria) Then
                                    RelacionUnoUno = RelacionUnoUno.CrearClonHistorico(pInfoMapDatosInst, gdmi.RecuperarMapPersistenciaCampos(RelacionUnoUno.TipoParte))

                                End If


                                Try

                                    If Not Me.ExisteTabla(RelacionUnoUno.TablaSqlRelacion) Then
                                        ej.EjecutarNoConsulta(RelacionUnoUno.SqlRelacion)
                                    End If

                                    'If Not RelacionUnoUnoHistorica Is Nothing Then
                                    '    ej.EjecutarNoConsulta(RelacionUnoUnoHistorica.SqlRelacion)
                                    'End If




                                Catch exsql As SqlClient.SqlException
                                    Select Case exsql.Number
                                        Case 2714
                                            'Nada
                                        Case 1767
                                            'Referencia a una tabla aun no cosntruida diferir la creacion de la sql
                                            mColSQLDiferidas.Add(RelacionUnoUno.SqlRelacion)

                                        Case Else
                                            Throw
                                    End Select
                                End Try
                            Next
                        End If
                    Next
                End If






                ''''''''''''''''''''''''''''''''''''''''''''''''
                '' Ejecutar los triggers de consistencia
                '''''''''''''''''''''''''''''''''''''''''''''''''''''
                If mapInst IsNot Nothing AndAlso pInfoMapDatosInst IsNot Nothing AndAlso pInfoMapDatosInst.ColTriger.Count > 0 Then
                    For Each mitiger As Triger In pInfoMapDatosInst.ColTriger
                        ej = New Ejecutor(Nothing, Me.mRec)
                        Try
                            If Not Me.ExisteTabla(Framework.AccesoDatos.MotorAD.AD.AccesorMotorAD.ObtenerNombreTablaSqlCONSTRAINT(mitiger.Sentencia)) Then
                                ej.EjecutarNoConsulta(mitiger.Sentencia)
                            End If


                        Catch exSql As SqlClient.SqlException


                            Select Case exSql.Number
                                Case 1913
                                    Throw
                                Case 2714
                                    ' nada porque ya exite y se trata de una referencia circular

                                Case Else
                                    Throw
                            End Select

                        End Try

                    Next
                End If




            Catch ex As Exception
                Debug.WriteLine(pRuta)

                Throw
            Finally
                Debug.Unindent()
            End Try
        End Sub




        'Public Sub GenerarRelacionesTablas(ByVal pTipo As System.Type, ByVal pInfoMapDatosInst As InfoDatosMapInstClaseDN)



        '    Dim coordinador As CTDLN
        '    Dim transProc As ITransaccionLogicaLN = Nothing
        '    Dim registrosAfectados As Int16
        '    Dim sqlCrearTB As String
        '    Dim tipo As System.Type
        '    'Dim colValidable As IValidable
        '    'Dim validadorTipos As ValidadorTipos
        '    Dim consSqlTablas As IConstructorTablasSQLAD
        '    Dim datosmapCampoInterface As InfoDatosMapInstCampoDN = Nothing
        '    Dim campoRef As InfoTypeInstCampoRefDN
        '    Dim giLN As GestorInstanciacionLN
        '    Dim tipoCampoReferido As System.Type
        '    Dim ej As Ejecutor

        '    coordinador = New CTDLN

        '    Try
        '        Debug.Indent()
        '        Debug.WriteLine("GT >" & pTipo.Name)
        '        'No generar ls tablas si ya han sido generadas para ese tipo
        '        If (pTipo.IsInterface = False AndAlso mTablasGeneradasParaTipos.Contains(pTipo.FullName)) Then
        '            Exit Sub

        '        Else
        '            If (pTipo.IsInterface = False AndAlso pTipo.GetInterface("IEnumerable", True) Is Nothing) Then
        '                mTablasGeneradasParaTipos.Add(pTipo.FullName)
        '            End If
        '        End If

        '        If (pTipo.IsInterface) Then
        '            Me.GenerarTablasParaInterface(pTipo, pInfoMapDatosInst, GenerarRelaciones)

        '            Exit Sub
        '        End If

        '        'Si el tipo es una coleccion validable y su validador es un validador de tipos que lo que se cree sea el tipo de su tipo fijado
        '        'If ptipo.GetInterface("IEnumerable", True) Is Nothing Then
        '        '    tipo = pTipo

        '        'Else
        '        '    colValidable = Activator.CreateInstance(ptipo)
        '        '    validadorTipos = colValidable.Validador
        '        '    tipo = validadorTipos.Tipo

        '        '    If (tipo.IsInterface) Then
        '        '        Me.GenerarTablas(tipo, pInfoMapDatosInst)

        '        '        Exit Sub
        '        '    End If
        '        'End If


        '        Dim tipoDeFijado As FijacionDeTipoDN
        '        If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo) Then
        '            tipo = pTipo
        '        Else
        '            tipo = InstanciacionReflexionHelperLN.ObtenerTipoFijado(pTipo, tipoDeFijado)
        '        End If
        '        If (tipoDeFijado = FijacionDeTipoDN.ColeccionGenerica Or tipoDeFijado = FijacionDeTipoDN.ColeccionValidable) AndAlso (tipo.IsInterface) Then
        '           Me.GenerarTablas(tipo, pInfoMapDatosInst)
        '            Exit Sub
        '        End If




        '        'Obtener le mapeado de Composicion de la entidad

        '        Dim mapInst As InfoTypeInstClaseDN
        '        Dim datosmaCampoRefiereClaseHeredadaEnSistema As InfoDatosMapInstClaseDN

        '        mapInst = GestorCacheInfoTypeInstLN.RecuperarMapInstanciacion(tipo)

        '        'Obtener le mapeado de comportamiento de la entidad (solo si no es aportado por el campo de la entidad contendora)
        '        Dim gdmi As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor

        '        If (pInfoMapDatosInst Is Nothing) Then
        '            pInfoMapDatosInst = gdmi.RecuperarMapPersistenciaCampos(tipo)
        '        End If

        '        Dim datosCalseHeredada As Object = Nothing
        '        If (pInfoMapDatosInst IsNot Nothing) Then
        '            datosCalseHeredada = pInfoMapDatosInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor)
        '        End If

        '        If Not datosCalseHeredada Is Nothing Then
        '            ' se trata de una clase heredada por otras luego hay que tratarla como si fuera una interface
        '           ' Me.GenerarTablasParaInterface(pTipo, pInfoMapDatosInst)
        '        End If



        '        'Crear Las Tablas para las entidades Referidas de modo externo
        '        giLN = New GestorInstanciacionLN(transProc, Me.mRec)

        '        For Each campoRef In mapInst.CamposRefExteriores
        '            Debug.WriteLine(campoRef.Campo.Name)
        '            tipoCampoReferido = campoRef.Campo.FieldType

        '            If (pInfoMapDatosInst IsNot Nothing) Then
        '                datosmapCampoInterface = pInfoMapDatosInst.GetCampoXNombre(campoRef.Campo.Name)
        '            Else
        '                datosmaCampoRefiereClaseHeredadaEnSistema = gdmi.RecuperarMapPersistenciaCampos(tipoCampoReferido)
        '                If Not datosmaCampoRefiereClaseHeredadaEnSistema Is Nothing Then
        '                    datosCalseHeredada = datosmaCampoRefiereClaseHeredadaEnSistema.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor)
        '                End If

        '            End If




        '            If (tipoCampoReferido.IsInterface OrElse datosCalseHeredada IsNot Nothing) Then
        '                If (datosmapCampoInterface IsNot Nothing) Then
        '                    If datosmapCampoInterface.ColCampoAtributo.Contains(CampoAtributoDN.NoProcesar) Then

        '                    ElseIf datosmapCampoInterface.ColCampoAtributo.Contains(CampoAtributoDN.PersistenciaContenidaSerializada) Then
        '                        Me.TratarInterfacesGenerarTablas(campoRef, datosmapCampoInterface)

        '                    ElseIf datosmapCampoInterface.ColCampoAtributo.Contains(CampoAtributoDN.InterfaceImplementadaPor) Then
        '                        Me.TratarInterfacesGenerarTablas(campoRef, datosmapCampoInterface)
        '                    End If

        '                Else
        '                    Me.TratarInterfacesGenerarTablas(campoRef, datosmapCampoInterface)
        '                End If

        '            Else
        '                'El campo referido no es una interface
        '                If (tipoCampoReferido Is pTipo) Then

        '                Else
        '                    If (datosmapCampoInterface Is Nothing) Then
        '                        'El campo no posee informacion particular para los datos de mapeado luego el sistema  los recuperara 
        '                        giLN.GenerarTablas(tipoCampoReferido, Nothing)

        '                    Else
        '                        'El campo posee informacion particular para los datos de mapeado luego el sistema no los recuperara
        '                        'sino que usara los pasados
        '                        giLN.GenerarTablas(tipoCampoReferido, datosmapCampoInterface.MapSubEntidad)
        '                    End If
        '                End If
        '            End If
        '        Next

        '        ' Crear la tabla para la entidad principal
        '        'consSqlTablas = New ConstructorSQLSQLsAD
        '        'sqlCrearTB = consSqlTablas.ConstSqlCreateTable(mapInst)

        '        'Ejecutamos la sql para insertar los datos
        '        ' ej = New Ejecutor(Nothing, Me.mRec)

        '        'Try
        '        '    Debug.WriteLine(sqlCrearTB)
        '        '    registrosAfectados = ej.EjecutarNoConsulta(sqlCrearTB)

        '        'Catch exsql As SqlClient.SqlException
        '        '    If (Not exsql.Number = 2714) Then
        '        '        Throw exsql
        '        '    End If
        '        'End Try





        '        'Crear las relaciones  
        '        Dim grc As GestorRelacionesCampoLN
        '        Dim cr As InfoTypeInstCampoRefDN
        '        Dim Relaciones As New ArrayList
        '        Dim relacion As Object
        '        Dim RelacionUnoUno As IRelacionUnoUnoDN
        '        Dim RelacionUnoN As IRelacionUnoNDN


        '        'Generar las relaciones
        '        grc = New GestorRelacionesCampoLN
        '        For Each cr In mapInst.CamposRefExteriores
        '            Relaciones.Add(grc.GenerarRelacionesCampoRef(mapInst, cr))
        '        Next

        '        'Generar las relaciones en el caso de que se trate de una huella
        '        If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo) Then
        '            Dim tipofijado As System.Type
        '            '  Dim ohuellaEntidad As Framework.DatosNegocio.HuellaEntidadDN
        '            tipofijado = TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(pTipo, Nothing)
        '            Relaciones.Add(grc.GenerarRelacionesHuella(mapInst, pTipo))

        '        End If





        '        'Ejecutar las relaciones
        '        For Each relacion In Relaciones
        '            If (TypeOf relacion Is ListaRelacionUnoNSqlsDN) Then
        '                For Each RelacionUnoN In relacion
        '                    Try
        '                        ej.EjecutarNoConsulta(RelacionUnoN.SqlTablaRel)

        '                    Catch exsql As SqlClient.SqlException
        '                        If (Not exsql.Number = 2714) Then
        '                            Throw exsql
        '                        End If
        '                    End Try

        '                    Try
        '                        ej.EjecutarNoConsulta(RelacionUnoN.SqlRelacionTodo)

        '                    Catch exsql As SqlClient.SqlException
        '                        If (Not exsql.Number = 2714) Then
        '                            Throw exsql
        '                        End If
        '                    End Try

        '                    Try
        '                        ej.EjecutarNoConsulta(RelacionUnoN.SqlRelacionParte)

        '                    Catch exsql As SqlClient.SqlException
        '                        If (Not exsql.Number = 2714) Then
        '                            Throw exsql
        '                        End If
        '                    End Try
        '                Next
        '            End If

        '            If (TypeOf relacion Is List(Of RelacionUnoUnoSQLsDN)) Then
        '                For Each RelacionUnoUno In relacion
        '                    Try
        '                        ej.EjecutarNoConsulta(RelacionUnoUno.SqlRelacion)

        '                    Catch exsql As SqlClient.SqlException
        '                        Select Case exsql.Number
        '                            Case 2714
        '                                'Nada
        '                            Case 1767
        '                                'Referencia a una tabla aun no cosntruida diferir la creacion de la sql
        '                                mColSQLDiferidas.Add(RelacionUnoUno.SqlRelacion)

        '                            Case Else
        '                                Throw exsql
        '                        End Select
        '                    End Try
        '                Next
        '            End If
        '        Next

        '    Catch ex As Exception
        '        Throw ex
        '    Finally
        '        Debug.Unindent()
        '    End Try
        'End Sub





        Public Function Baja(ByVal pObjeto As Object) As Object Implements IBaseMotorLN.Baja
            'Throw New NotImplementedException("Error: no implementado")

            ' guarda objetos que permiten que tenga el atributo baja activado

            Return GuardarEntidad(pObjeto)

        End Function

        Public Function Guardar(ByVal o As Object) As LogicaNegocios.OperacionGuardarLN Implements IBaseMotorLN.Guardar

            If TypeOf o Is EntidadDN Then
                Dim eb As EntidadDN
                eb = o
                If eb.Baja = True Then
                    Throw New ArgumentException("No esta permitido modificar objetos en baja")
                End If

            End If

            Return GuardarEntidad(o)
        End Function

        Private Function GuardarEntidad(ByVal o As Object) As LogicaNegocios.OperacionGuardarLN
            Dim coordinador As CTDLN
            Dim transProc As ITransaccionLogicaLN = Nothing
            Dim objeto As Object
            Dim idp As Framework.DatosNegocio.IDatoPersistenteDN

            Dim gi As GestorInstanciacionLN


            If o Is Nothing Then
                Throw New ArgumentException("Se paso nothing como valor a gurdar.")
            End If

            coordinador = New CTDLN

            Try
                coordinador.IniciarTransaccion(mTL, transProc)


                '1 
                mColIntanciasPostGuarda = New ArrayList
                If mColIntanciasGuardadas Is Nothing Then
                    mColIntanciasGuardadas = New ArrayList
                End If

                '2
                'Guardarp(o)

                If TypeOf o Is Framework.DatosNegocio.IEntidadDN Then
                    Guardarp(o)
                ElseIf TypeOf o Is IList Then
                    If TypeOf o Is Framework.DatosNegocio.IColEventos Then
                        Dim micol As Framework.DatosNegocio.IColEventos = o
                        If micol.ModificadosElemtosCol Then
                            For Each elemento As Object In o
                                Guardarp(elemento)
                            Next
                        End If
                    Else
                        For Each elemento As Object In o
                            Guardarp(elemento)
                        Next
                    End If

                End If



                '3

                For Each objeto In mColIntanciasPostGuarda

                    If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(objeto.GetType) Then
                        Dim h As Framework.DatosNegocio.HEDN = objeto
                        If h.Estado = EstadoDatosDN.SinModificar AndAlso Not String.IsNullOrEmpty(h.ID) Then
                        Else
                            gi = New GestorInstanciacionLN(transProc, Me.mRec)
                            idp = objeto
                            idp.EstadoDatos = DatosNegocio.EstadoDatosDN.Modificado
                            gi.Guardar(objeto)
                        End If
                    Else

                        gi = New GestorInstanciacionLN(transProc, Me.mRec)
                        idp = objeto
                        idp.EstadoDatos = DatosNegocio.EstadoDatosDN.Modificado
                        gi.Guardar(objeto)
                    End If



                Next


                ' 4
                mColIntanciasPostGuarda = Nothing
                mColIntanciasGuardadas = Nothing
                transProc.Confirmar()

            Catch ex As Exception

                If Not mTL Is Nothing Then
                    transProc.Cancelar()
                End If

                Throw
            Finally
                mColIntanciasPostGuarda = Nothing
                mColIntanciasGuardadas = Nothing

            End Try



        End Function


        Private Function ActualizarHuellaPorClonActual(ByVal phuella As Framework.DatosNegocio.HuellaEntidadCacheableDN) As Boolean
            Dim objeto As Object
            Dim entidad As IEntidadDN
            Dim tipohuella As System.Type
            Dim idhuella As String
            idhuella = phuella.ID

            If idhuella Is Nothing OrElse idhuella = "" Then
                Return False
            End If

            tipohuella = phuella.GetType
            ' busco en la col de objetos guardados si existe un clon actual para el tipo e id de huella dado
            For Each objeto In mColIntanciasGuardadas
                If objeto.GetType Is tipohuella Then
                    entidad = objeto
                    If idhuella = entidad.ID Then
                        ' debo actualizar la huella chache con su clo actual
                        phuella.ActualizarHuella(entidad)
                        Return True
                    End If
                End If
            Next

            Return False
        End Function




        Public Function Eliminar(ByVal pObjeto As Object) As LogicaNegocios.OperacionGuardarLN



            Dim operacionRealizada As New OperacionRealizada
            Dim coordinador As CTDLN
            Dim transProc As ITransaccionLogicaLN = Nothing
            Dim registrosAfectados As Int64
            Dim mensaje As String = String.Empty

            'Entidad princpal
            Dim accesor As IBaseAD

            'Entidades de logica para los campos por referencia del objeto
            Dim operacion As Framework.LogicaNegocios.OperacionGuardarLN

            Dim mapInst As InfoTypeInstClaseDN
            Dim enumerador As IEnumerable
            Dim idp As Framework.DatosNegocio.IDatoPersistenteDN

            Dim cref As InfoTypeInstCampoRefDN
            'Dim objetoContenido As Framework.DatosNegocio.IEntidadBaseDN

            Dim oa As Object
            Dim contrM As ConstructorSQLSQLsAD
            Dim contrucor As ConstructorAdapterAD
            '    Dim eper As IDatoPersistenteDN

            '    Dim parametros As List(Of IDataParameter)
            '  Dim regAfectados As Int64
            '     Dim ej As Ejecutor
            'Dim sqltr As String
            Dim oIEntidadDN As IEntidadDN


            coordinador = New CTDLN
            Debug.Indent()
            'Debug.WriteLine("1Eliminando:" & pObjeto.ToString)

            Try
                coordinador.IniciarTransaccion(mTL, transProc)

                'No procesar si la instancia ya fue procesada
                If mColIntanciasGuardadas.Contains(pObjeto) Then
                    If TypeOf pObjeto Is Framework.DatosNegocio.IEntidadDN Then
                        oIEntidadDN = pObjeto
                        If oIEntidadDN.ID Is Nothing OrElse oIEntidadDN.ID = "" Then
                            Debug.WriteLine("referencia circualr para el tipo " & pObjeto.GetType.Name & " de id " & oIEntidadDN.ID)
                            ' post guardas
                            operacionRealizada.Operacion = LogicaNegocios.OperacionGuardarLN.EnProceso
                            Return operacionRealizada.Operacion
                        End If

                    End If

                    Return LogicaNegocios.OperacionGuardarLN.YaProcesado
                Else


                    Throw New NotImplementedException
                    '' si se trata de una huella cache puede que un clon suyo ya exista en la col de entidades guardadas y se debe adsorver de los datos actualizados
                    'If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaCacheable(pObjeto.GetType) Then
                    '    If ActualizarHuellaPorClonActual(pObjeto) Then
                    '        ' la huella fue actualizada luego no requiere ser guardad denuevo
                    '        oIEntidadDN = pObjeto
                    '        Debug.WriteLine("huella actualizada tipo " & pObjeto.GetType.Name & " de id " & oIEntidadDN.ID)
                    '        Return OperacionGuardarLN.Modificar
                    '    Else
                    '        ' la huella no se actualizo y debe de continuar con el codigo habitual
                    '    End If
                    'Else
                    '    mColIntanciasGuardadas.Add(pObjeto)
                    'End If


                End If




                'No procesar si no es una entidadDN
                If TypeOf pObjeto Is Framework.DatosNegocio.IEntidadDN Then
                    oIEntidadDN = pObjeto
                    'Debug.WriteLine("2Eliminando:" & pObjeto.ToString & " id:" & oIEntidadDN.ID)
                Else
                    Return LogicaNegocios.OperacionGuardarLN.Ninguna
                End If

                operacion = OperacionGuardarLN.Eliminar

                ' verificar permiso para eliminar la entidad si se asigno un delegado

                If Not mDelegadoAutorizacion Is Nothing Then
                    Dim autorizacion As Boolean
                    Me.mDelegadoAutorizacion.Invoke(pObjeto, operacion, autorizacion)
                    If Not autorizacion Then
                        Throw New ApplicationException("el delegado denogo la operacion")
                    End If

                End If


                'Obtener el mapeado de composicion de la entidad
                mapInst = GestorCacheInfoTypeInstLN.RecuperarMapInstanciacion(pObjeto.GetType)

                'Vincular el mapeado con la instacia en concreto
                mapInst.InstanciaPrincipal = pObjeto

                'Verificar que el estado de integridad es consistente
                If (TypeOf pObjeto Is Framework.DatosNegocio.IDatoPersistenteDN) Then
                    idp = pObjeto

                    If (Not idp.EstadoIntegridad(mensaje) = EstadoIntegridadDN.Consistente) Then
                        Throw New ApplicationException(mensaje)
                    End If
                End If

                'Guardar las partes
                'For Each cref In mapInst.CamposRefExteriores
                '    If (cref.Instancia IsNot Nothing) Then
                '        Dim operacionrealizadEnLaParte As OperacionGuardarLN

                '        If TypeOf cref.Instancia Is IEnumerable Then
                '            enumerador = cref.Instancia

                '            For Each objetoContenido In enumerador
                '                If objetoContenido IsNot Nothing Then
                '                    operacionrealizadEnLaParte = Me.Guardarp(objetoContenido)
                '                    If operacionrealizadEnLaParte = OperacionGuardarLN.EnProceso OrElse operacionrealizadEnLaParte = OperacionGuardarLN.AñadidoColPostGuarda Then
                '                        mColIntanciasPostGuarda.Add(pObjeto)
                '                        operacionRealizada.Operacion = OperacionGuardarLN.AñadidoColPostGuarda
                '                    End If
                '                End If

                '            Next

                '        Else
                '            operacionrealizadEnLaParte = Me.Guardarp(cref.Instancia)
                '            If operacionrealizadEnLaParte = LogicaNegocios.OperacionGuardarLN.EnProceso OrElse operacionrealizadEnLaParte = OperacionGuardarLN.AñadidoColPostGuarda Then
                '                mColIntanciasPostGuarda.Add(pObjeto)
                '                operacionRealizada.Operacion = OperacionGuardarLN.AñadidoColPostGuarda
                '            End If
                '        End If
                '    End If
                'Next



                'Eliminar la entidad principal

                contrM = New ConstructorSQLSQLsAD
                contrucor = New ConstructorAdapterAD(contrM, mapInst)
                oa = New BaseTransaccionV2AD(transProc, Me.mRec, contrucor)
                accesor = oa

                If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pObjeto.GetType) Then
                    '' comportamiento especial si se trata de una huella:
                    '' 1º llamar a asignar el id de la entidad referida
                    '' 2º procedemos a updatar la entidad
                    '' 3º si los registros a fectados por el update son 0 procedemos a su inserción
                    'Dim operacionrealizadaEnHuella As OperacionGuardarLN

                    'operacionrealizadaEnHuella = GuardarEntidadPrincipalHuella(transProc, pObjeto, mapInst, operacion)
                    'operacionRealizada.Operacion = operacionrealizadaEnHuella


                    Throw New NotImplementedException

                Else






                    'eliminar
                    registrosAfectados = accesor.Eliminar(pObjeto)
                    operacionRealizada.Operacion = OperacionGuardarLN.Eliminar

                    If (registrosAfectados <> 1) Then
                        If TypeOf pObjeto Is EntidadDN Then
                            Dim edn As EntidadDN
                            edn = pObjeto
                            Throw New NingunaFilaAfectadaException("Error: no se eliminó ningun registro --> id=" & edn.ID & " , fm=" & edn.FechaModificacion.Ticks.ToString & ", Estado=" & edn.Estado.ToString & ", tipo=" & pObjeto.ToString)
                        Else
                            Throw New NingunaFilaAfectadaException("Error: no se eliminó ningun registro --> " & pObjeto)
                        End If
                    End If


                    'Si la entidad tenia entidades de persitencia contenida registrar el estado como sin cambios
                    'For Each cref In mapInst.CamposRefContenidos
                    '    If (TypeOf cref.Instancia Is IDatoPersistenteDN) Then
                    '        eper = cref.Instancia
                    '        eper.EstadoDatos = EstadoDatosDN.SinModificar
                    '    End If      'tipo.GetMethods(BindingFlags.Default)	'tipo.GetMethods' is not declared or the module containing it is not loaded in the debugging session.	

                    'Next


                    ' ******** Tal vez habria que borrar las huellas que pudieran haberse establecido contra este tipo

                    '' Actualizar las huellas Cacheables en 
                    'ActualizarHuellas(mapInst, pObjeto)

                End If


                ''Guardar las entradas en las tablas realcionales on las entidades 1-*
                'If Not mColIntanciasPostGuarda.Contains(pObjeto) Then
                '    ej = New Ejecutor(transProc, Me.mRec)

                '    For Each cref In mapInst.CamposRefExteriores
                '        If (cref.Campo.FieldType.GetInterface("IEnumerable", True) IsNot Nothing) Then
                '            parametros = New List(Of IDataParameter)
                '            sqltr = contrM.ConstSqlRelacionUnoN(mapInst, cref, parametros, transProc.FechaCreacion)

                '            'Ejecutamos la sql para insertar los datos
                '            regAfectados = ej.EjecutarNoConsulta(sqltr, parametros)
                '        End If
                '    Next

                'End If







                transProc.Confirmar()

            Catch ex As Exception
                If (mTL IsNot Nothing) Then
                    transProc.Cancelar()
                End If

                Throw
            Finally
                Eliminar = operacionRealizada.Operacion ' lo queocurrio
                'Debug.WriteLine(pObjeto.ToString & "-->" & operacionRealizada.Operacion.ToString)
                Debug.Unindent()
            End Try
        End Function
        Public Function InsertarEntidadBase(ByVal pCol As IList) As LogicaNegocios.OperacionGuardarLN



            Dim tlproc As ITransaccionLogicaLN = Nothing

            Try
                tlproc = Me.ObtenerTransaccionDeProceso

                Dim elemento As Object
                For Each elemento In pCol
                    Dim gi As New GestorInstanciacionLN(tlproc, Me.mRec)
                    gi.InsertarEntidadBase(elemento)
                Next


                tlproc.Confirmar()

            Catch e As Exception
                If (Not (tlproc Is Nothing)) Then
                    tlproc.Cancelar()
                End If
                Throw e
            End Try





        End Function


        Public Function InsertarEntidadBase(ByVal pObjeto As Object) As LogicaNegocios.OperacionGuardarLN



            Dim operacionRealizada As New OperacionRealizada
            Dim coordinador As CTDLN
            Dim transProc As ITransaccionLogicaLN = Nothing
            Dim mensaje As String = String.Empty

            'Entidad princpal
            Dim accesor As IBaseAD

            'Entidades de logica para los campos por referencia del objeto
            Dim operacion As Framework.LogicaNegocios.OperacionGuardarLN

            Dim mapInst As InfoTypeInstClaseDN


            Dim oa As Object
            Dim contrM As ConstructorSQLSQLsAD
            Dim contrucor As ConstructorAdapterAD

            Dim oIEntidadBaseDN As IEntidadBaseDN


            coordinador = New CTDLN
            Debug.Indent()
            'Debug.WriteLine("1InsertarEntidadBase:" & pObjeto.ToString)

            Try
                coordinador.IniciarTransaccion(mTL, transProc)

                'No procesar si la instancia ya fue procesada
                If mColIntanciasGuardadas.Contains(pObjeto) Then
                    If TypeOf pObjeto Is Framework.DatosNegocio.IEntidadDN Then
                        oIEntidadBaseDN = pObjeto
                        If oIEntidadBaseDN.ID Is Nothing OrElse oIEntidadBaseDN.ID = "" Then
                            Debug.WriteLine("referencia circualr para el tipo " & pObjeto.GetType.Name & " de id " & oIEntidadBaseDN.ID)
                            ' post guardas
                            operacionRealizada.Operacion = LogicaNegocios.OperacionGuardarLN.EnProceso
                            Return operacionRealizada.Operacion
                        End If

                    End If

                    Return LogicaNegocios.OperacionGuardarLN.YaProcesado
                Else


                    mColIntanciasGuardadas.Add(pObjeto)



                End If




                'No procesar si no es una entidad base
                If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsEntidadBaseNoEntidadDN(pObjeto.GetType) Then
                    oIEntidadBaseDN = pObjeto
                    'Debug.WriteLine("2InsertarEntidadBase:" & pObjeto.ToString & " id:" & oIEntidadBaseDN.ID)
                Else
                    Return LogicaNegocios.OperacionGuardarLN.Ninguna
                End If

                'No trabajar si no es para insertar o modificar
                operacion = OperacionGuardarLN.Insertar

                ' verificar permiso para guardar la entidad si se asigno un delegado

                If Not mDelegadoAutorizacion Is Nothing Then
                    Dim autorizacion As Boolean
                    Me.mDelegadoAutorizacion.Invoke(pObjeto, operacion, autorizacion)
                    If Not autorizacion Then
                        Throw New ApplicationException("el delegado denogo la operacion")
                    End If

                End If


                'Obtener el mapeado de composicion de la entidad
                mapInst = GestorCacheInfoTypeInstLN.RecuperarMapInstanciacion(pObjeto.GetType)

                'Vincular el mapeado con la instacia en concreto
                mapInst.InstanciaPrincipal = pObjeto





                'Guardar la entidad principal

                contrM = New ConstructorSQLSQLsAD
                contrucor = New ConstructorAdapterAD(contrM, mapInst)
                oa = New BaseTransaccionV2AD(transProc, Me.mRec, contrucor)
                accesor = oa


                'Insertar
                accesor.Insertar(pObjeto)
                operacionRealizada.Operacion = OperacionGuardarLN.Insertar


                transProc.Confirmar()

            Catch ex As Exception
                If (mTL IsNot Nothing) Then
                    transProc.Cancelar()
                End If

                Throw
            Finally
                InsertarEntidadBase = operacionRealizada.Operacion ' lo queocurrio
                Debug.WriteLine(pObjeto.ToString & "-->" & operacionRealizada.Operacion.ToString)
                Debug.Unindent()
            End Try
        End Function



        Private Function Guardarp(ByVal pObjeto As Object) As LogicaNegocios.OperacionGuardarLN
            If pObjeto Is Nothing Then
                Exit Function
            End If
            'Dim ednee As EntidadDN
            'ednee = pObjeto
            'If ednee.FechaModificacion.Ticks = "632609229430312500" Then
            '    Beep()
            'End If

            Dim operacionRealizada As New OperacionRealizada
            Dim coordinador As CTDLN
            Dim transProc As ITransaccionLogicaLN = Nothing
            Dim registrosAfectados As Int16
            Dim mensaje As String = String.Empty

            'Entidad princpal
            Dim accesor As IBaseAD

            'Entidades de logica para los campos por referencia del objeto
            Dim operacion As Framework.LogicaNegocios.OperacionGuardarLN

            Dim mapInst As InfoTypeInstClaseDN
            Dim enumerador As IEnumerable
            Dim idp As Framework.DatosNegocio.IDatoPersistenteDN

            Dim cref As InfoTypeInstCampoRefDN
            Dim objetoContenido As Framework.DatosNegocio.IEntidadBaseDN

            Dim oa As Object
            Dim contrM As ConstructorSQLSQLsAD
            Dim contrucor As ConstructorAdapterAD
            Dim eper As IDatoPersistenteDN

            Dim parametros As List(Of IDataParameter)
            Dim regAfectados As Integer
            Dim ej As Ejecutor
            Dim sqltr As String
            Dim oIEntidadDN As IEntidadBaseDN


            coordinador = New CTDLN
            'Debug.Indent()
            'Debug.WriteLine("")

            'Debug.WriteLine("1Guardando:(" & pObjeto.GetType.FullName & ") " & pObjeto.ToString)

            Try
                coordinador.IniciarTransaccion(mTL, transProc)

                'No procesar si la instancia ya fue procesada
                If mColIntanciasGuardadas.Contains(pObjeto) Then
                    If TypeOf pObjeto Is Framework.DatosNegocio.IEntidadBaseDN Then
                        oIEntidadDN = pObjeto
                        If String.IsNullOrEmpty(oIEntidadDN.ID) AndAlso pObjeto.GetType IsNot GetType(Framework.DatosNegocio.HEDN) Then
                            Debug.WriteLine("referencia circualr para el tipo " & pObjeto.GetType.Name & " de id " & oIEntidadDN.ID)
                            ' post guardas
                            operacionRealizada.Operacion = LogicaNegocios.OperacionGuardarLN.EnProceso
                            Return operacionRealizada.Operacion
                        End If

                    End If

                    Return LogicaNegocios.OperacionGuardarLN.YaProcesado
                Else

                    ' si se trata de una huella cache puede que un clon suyo ya exista en la col de entidades guardadas y se debe adsorver de los datos actualizados
                    If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaCacheable(pObjeto.GetType) Then
                        If ActualizarHuellaPorClonActual(pObjeto) Then
                            ' la huella fue actualizada luego no requiere ser guardad denuevo
                            mColIntanciasGuardadas.Add(pObjeto)
                            oIEntidadDN = pObjeto
                            'Debug.WriteLine("huella actualizada tipo " & pObjeto.GetType.Name & " de id " & oIEntidadDN.ID)
                            Return OperacionGuardarLN.Modificar
                        Else
                            ' la huella no se actualizo y debe de continuar con el codigo habitual
                            'mColIntanciasGuardadas.Add(pObjeto)
                            'Beep()
                        End If
                    Else
                        mColIntanciasGuardadas.Add(pObjeto)
                    End If


                End If




                'No procesar si no es una entidadDN
                If TypeOf pObjeto Is Framework.DatosNegocio.IEntidadBaseDN Then
                    oIEntidadDN = pObjeto
                    'Debug.WriteLine("2Guardando:" & pObjeto.ToString & " id:" & oIEntidadDN.ID & " GUID: " & oIEntidadDN.GUID & " fm:" & CType(oIEntidadDN, Framework.DatosNegocio.IDatoPersistenteDN).FechaModificacion)
                Else
                    Return LogicaNegocios.OperacionGuardarLN.Ninguna
                End If

                'No trabajar si no es para insertar o modificar
                operacion = OperacionARealizar(pObjeto)
                If (operacion = LogicaNegocios.OperacionGuardarLN.Ninguna) Then
                    Return operacion
                End If
                ' verificar permiso para guardar la entidad si se asigno un delegado

                If Not mDelegadoAutorizacion Is Nothing Then
                    Dim autorizacion As Boolean
                    Me.mDelegadoAutorizacion.Invoke(pObjeto, operacion, autorizacion)
                    If Not autorizacion Then
                        Throw New ApplicationException("el delegado denogo la operacion")
                    End If

                End If


                'Obtener el mapeado de composicion de la entidad
                mapInst = GestorCacheInfoTypeInstLN.RecuperarMapInstanciacion(pObjeto.GetType)

                'Vincular el mapeado con la instacia en concreto
                mapInst.InstanciaPrincipal = pObjeto

                'Verificar que el estado de integridad es consistente
                If (TypeOf pObjeto Is Framework.DatosNegocio.IDatoPersistenteDN) Then
                    idp = pObjeto

                    If (Not idp.EstadoIntegridad(mensaje) = EstadoIntegridadDN.Consistente) Then
                        Throw New ApplicationException(mensaje)
                    End If
                End If

                'Guardar las partes
                For Each cref In mapInst.CamposRefExteriores
                    If (cref.Instancia IsNot Nothing) Then
                        Dim operacionrealizadEnLaParte As OperacionGuardarLN

                        If TypeOf cref.Instancia Is IEnumerable Then
                            enumerador = cref.Instancia

                            For Each objetoContenido In enumerador
                                If objetoContenido IsNot Nothing Then
                                    operacionrealizadEnLaParte = Me.Guardarp(objetoContenido)
                                    If operacionrealizadEnLaParte = OperacionGuardarLN.EnProceso OrElse operacionrealizadEnLaParte = OperacionGuardarLN.AñadidoColPostGuarda Then
                                        mColIntanciasPostGuarda.Add(pObjeto)
                                        operacionRealizada.Operacion = OperacionGuardarLN.AñadidoColPostGuarda
                                    End If
                                End If

                            Next

                        Else
                            operacionrealizadEnLaParte = Me.Guardarp(cref.Instancia)
                            If operacionrealizadEnLaParte = LogicaNegocios.OperacionGuardarLN.EnProceso OrElse operacionrealizadEnLaParte = OperacionGuardarLN.AñadidoColPostGuarda Then
                                mColIntanciasPostGuarda.Add(pObjeto)
                                operacionRealizada.Operacion = OperacionGuardarLN.AñadidoColPostGuarda
                            End If
                        End If
                    End If
                Next


                '''''''''''''''''''''''''''''''''
                'Guardar la entidad principal
                '''''''''''''''''''''''''''''''''

                contrM = New ConstructorSQLSQLsAD
                contrucor = New ConstructorAdapterAD(contrM, mapInst)
                oa = New BaseTransaccionV2AD(transProc, Me.mRec, contrucor)
                accesor = oa

                If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pObjeto.GetType) Then
                    ' comportamiento especial si se trata de una huella:
                    ' 1º llamar a asignar el id de la entidad referida
                    ' 2º procedemos a updatar la entidad
                    ' 3º si los registros a fectados por el update son 0 procedemos a su inserción
                    Dim operacionrealizadaEnHuella As OperacionGuardarLN

                    operacionrealizadaEnHuella = GuardarEntidadPrincipalHuella(transProc, pObjeto, mapInst, operacion)
                    operacionRealizada.Operacion = operacionrealizadaEnHuella

                Else






                    'Insertar
                    If (operacion = OperacionGuardarLN.Insertar) Then
                        accesor.Insertar(pObjeto)
                        operacionRealizada.Operacion = OperacionGuardarLN.Insertar

                        'Modificar
                    Else
                        registrosAfectados = accesor.Modificar(pObjeto)
                        operacionRealizada.Operacion = OperacionGuardarLN.Modificar

                        If (registrosAfectados <> 1 AndAlso registrosAfectados <> 2) Then
                            Dim st As New StackTrace()
                            If TypeOf pObjeto Is EntidadDN Then
                                Dim edn As EntidadDN
                                edn = pObjeto

                                'Debug.WriteLine("Error: no se modifico ningun registro: --> id=" & edn.ID & " GUID:" & edn.GUID & " , fm=" & edn.FechaModificacion.Ticks.ToString & ", tipo=" & pObjeto.GetType.FullName & ", Traza" & st.ToString())

                                Throw New NingunaFilaAfectadaException("Error: no se modifico ningun registro --> id=" & edn.ID & " GUID:" & edn.GUID & " , fm=" & edn.FechaModificacion.Ticks.ToString & ", Estado=" & edn.Estado.ToString & ", tipo=" & pObjeto.GetType.FullName & ", Traza" & st.ToString())
                            Else
                                'Debug.WriteLine("Error: no se modifico ningun registro: --> " & pObjeto.ToString & " ; tipo: " & pObjeto.GetType.ToString & "--->" & operacionRealizada.Operacion.ToString & ", Traza" & st.ToString())

                                Throw New NingunaFilaAfectadaException("Error: no se modifico ningun registro --> " & pObjeto & " ; tipo: " & pObjeto.GetType.ToString)
                            End If
                        End If
                    End If

                    'Si la entidad tenia entidades de persitencia contenida registrar el estado como sin cambios
                    For Each cref In mapInst.CamposRefContenidos
                        If (TypeOf cref.Instancia Is IDatoPersistenteDN) Then
                            eper = cref.Instancia
                            eper.EstadoDatos = EstadoDatosDN.SinModificar
                        End If      'tipo.GetMethods(BindingFlags.Default)	'tipo.GetMethods' is not declared or the module containing it is not loaded in the debugging session.	

                    Next



                    ' Actualizar las huellas Cacheables
                    ActualizarHuellas(mapInst, pObjeto)

                End If


                'Guardar las entradas en las tablas realcionales on las entidades 1-*
                If Not mColIntanciasPostGuarda.Contains(pObjeto) Then
                    ej = New Ejecutor(transProc, Me.mRec)

                    For Each cref In mapInst.CamposRefExteriores
                        If (cref.Campo.FieldType.GetInterface("IEnumerable", True) IsNot Nothing) Then
                            parametros = New List(Of IDataParameter)
                            'Ejecutamos la sql para insertar los datos en bloques
                            For Each sqlparam As SqlParametros In contrM.ConstSqlRelacionUnoN(mapInst, cref, parametros, transProc.FechaCreacion, "")
                                regAfectados += ej.EjecutarNoConsulta(sqlparam.sql, sqlparam.Parametros)
                            Next



                        End If
                    Next

                End If







                transProc.Confirmar()

            Catch ex As Exception
                If (mTL IsNot Nothing) Then
                    transProc.Cancelar()
                End If
                If TypeOf pObjeto Is EntidadDN Then
                    Dim edn As EntidadDN
                    edn = pObjeto
                    'Debug.WriteLine("ERROR: --> id=" & edn.ID & " GUID:" & edn.GUID & " , fm=" & edn.FechaModificacion.Ticks.ToString & ", tipo=" & pObjeto.GetType.FullName & " er:" & ex.ToString)
                    Throw New NingunaFilaAfectadaException("ERROR: --> id=" & edn.ID & " GUID:" & edn.GUID & " , fm=" & edn.FechaModificacion.Ticks.ToString & ", tipo=" & pObjeto.GetType.FullName & " er:" & ex.ToString)
                Else
                    'Debug.WriteLine("ERROR: --> " & pObjeto.ToString & " ; tipo: " & pObjeto.GetType.ToString & "--->" & operacionRealizada.Operacion.ToString & " er:" & ex.ToString)
                    Throw New NingunaFilaAfectadaException("ERROR: --> " & pObjeto.ToString & " ; tipo: " & pObjeto.GetType.ToString & "--->" & operacionRealizada.Operacion.ToString & " er:" & ex.ToString)
                End If
                Throw
            Finally
                Guardarp = operacionRealizada.Operacion ' lo queocurrio
                'Debug.WriteLine("(" & pObjeto.GetType.FullName & ")" & pObjeto.ToString & "-->" & operacionRealizada.Operacion.ToString)

                If TypeOf pObjeto Is EntidadDN Then
                    Dim edn As EntidadDN
                    edn = pObjeto

                    'Debug.WriteLine("Operacion Realizada --> id=" & edn.ID & " GUID:" & edn.GUID & " , fm=" & edn.FechaModificacion.Ticks.ToString & ", tipo=" & pObjeto.GetType.FullName & "--->" & operacionRealizada.Operacion.ToString)
                Else
                    'Debug.WriteLine("Operacion Realizada  --> " & pObjeto.ToString & " ; tipo: " & pObjeto.GetType.ToString & "--->" & operacionRealizada.Operacion.ToString)
                End If

                Debug.Unindent()
            End Try
        End Function

        Private Function GuardarEntidadPrincipalHuella(ByVal transProc As ITransaccionLogicaLN, ByVal pEntidad As Framework.DatosNegocio.HEDN, ByVal mapInst As InfoTypeInstClaseDN, ByVal operacion As Framework.LogicaNegocios.OperacionGuardarLN) As OperacionGuardarLN

            'If Me.ColIntanciasGuardadas.Contains(pEntidad.EntidadReferida) Then
            '    Return OperacionGuardarLN.EnProceso
            'End If

            Dim h As Framework.DatosNegocio.HEDN
            Dim registrosAfectados As Int64
            Dim accesor As IBaseAD
            Dim contrM As ConstructorSQLSQLsAD
            Dim contrucor As ConstructorAdapterAD

            Dim operacionrealizada As OperacionGuardarLN
            h = pEntidad

            'If pEntidad.Estado = EstadoDatosDN.SinModificar AndAlso Not String.IsNullOrEmpty(h.IdEntidadReferida) AndAlso h.IdEntidadReferida <> 0 Then
            '    Return OperacionGuardarLN.Ninguna
            'End If


            contrM = New ConstructorSQLSQLsAD
            contrucor = New ConstructorAdapterAD(contrM, mapInst)
            accesor = New BaseTransaccionV2AD(transProc, Me.mRec, contrucor)


            ' asegurarse que la entidad referida ya esta guardada
            If h.EntidadReferida IsNot Nothing Then
                operacionrealizada = Me.Guardarp(h.EntidadReferida)


                If operacionrealizada = OperacionGuardarLN.EnProceso OrElse operacionrealizada = OperacionGuardarLN.AñadidoColPostGuarda Then
                    mColIntanciasPostGuarda.Add(h)
                    If Not TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(pEntidad) Then
                        Return OperacionGuardarLN.AñadidoColPostGuarda
                    End If
                End If


            Else
                '    mColIntanciasPostGuarda.Add(h) ' TODO:  alex modificado
            End If


            If h.IdEntidadReferida Is Nothing OrElse h.IdEntidadReferida = "" Then
                Throw New ApplicationException("IdEntidadReferida no puede der nulo o vacio")
            End If

            If Not h.EntidadReferida Is Nothing Then
                h.Refrescar()
            End If


            registrosAfectados = accesor.Modificar(pEntidad)
            If registrosAfectados = 0 Then
                accesor.Insertar(pEntidad)
                Return OperacionGuardarLN.Insertar
            Else
                Return OperacionGuardarLN.Modificar
            End If


        End Function

        Private Sub PostAsignacion()
            Dim cpa As CampoPostRecuperacionDN
            Dim ref As Object
            Dim icl As Framework.DatosNegocio.IColEventos
            Dim lista As IList

            For Each cpa In mColCampoPostReuperacion
                ref = mColIntanciasRecuperadas.Item(cpa.Clave)

                If (ref Is Nothing) Then
                    Throw New ApplicationException("Error: ninguna entrada en ColCampoPostRecuperacion debiera ser nothing en el proceso de PostAsignacion")

                Else
                    'Si el campo eferido es una coleccion , habra que añadir la instancia
                    If (cpa.InfoCampo.Campo.FieldType.GetInterface("IList") IsNot Nothing) Then
                        lista = cpa.InfoCampo.Campo.GetValue(cpa.InfoCampo.InstanciaReferidora)

                        If (TypeOf lista Is Framework.DatosNegocio.IColEventos) Then
                            icl = lista
                            icl.EventosActivos = False
                            lista = cpa.InfoCampo.Campo.GetValue(cpa.InfoCampo.InstanciaReferidora)
                            lista.Add(ref)
                            icl.EventosActivos = True

                        Else
                            lista.Add(ref)
                        End If

                    Else
                        cpa.InfoCampo.Campo.SetValue(cpa.InfoCampo.InstanciaReferidora, ref)
                    End If
                End If
            Next
        End Sub


        Public Overloads Function RecuperarColHuellasRelInversa(ByVal PropiedadDeInstanciaDN As TiposYReflexion.DN.PropiedadDeInstanciaDN) As IList


            Dim pTipoReferido As Type = PropiedadDeInstanciaDN.Propiedad.PropertyType
            Return RecuperarColHuellasRelInversa(PropiedadDeInstanciaDN, pTipoReferido)
        End Function





        ''' <summary>
        ''' dada una propiedad de untipo que dispone de un atitributo con relación a un campo , y el ide de la entidad prpietaria de la propiedad
        ''' permite recuperar la coleccion de entidades relacioandas con esta entidad en ese tipo
        ''' </summary>
        ''' <param name="pID"> </param>
        ''' <param name="pPropiedad"> </param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overloads Function RecuperarColHuellasRelInversa(ByVal PropiedadDeInstanciaDN As TiposYReflexion.DN.PropiedadDeInstanciaDN, ByVal pTipoReferido As Type) As IList


            ' crear un ad especifico para sql que 
            ' identifique la relacion o tabla relacional que esta en juego
            ' contrulla una sql que realice el join de las dos o tres tablas
            ' y recupere un dataset con informacion para motar la coleccion de huellas no tipadas (tipo + id + guid) de todos los tipos que pudieran estar implciados
            ' leugo esas huellas se pueden usar pare recuperar dichos objetos si fuera necesaario


            Dim tlp As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Me.ObtenerTransaccionDeProceso

            Try


                Dim infoReferidora, infoReferido As InfoTypeInstClaseDN
                Dim ginfoi As New InfoTypeInstLN
                Dim camporef As Framework.TiposYReflexion.DN.InfoTypeInstCampoRefDN

                infoReferidora = ginfoi.Generar(PropiedadDeInstanciaDN.Propiedad.ReflectedType, Nothing, "")

                Dim constructor As New Framework.AccesoDatos.MotorAD.AD.ConstructorSQLSQLsAD
                Dim adaptador As AccesorMotorAD
                adaptador = New AccesorMotorAD(tlp, Me.mRec, constructor)

                For Each camporef In infoReferidora.CamposRef

                    ' esto es para encontrar el campo o campos una vez aplicada las directrices del mapeado por ejem plo en una interface
                    If camporef.Campo.Name = PropiedadDeInstanciaDN.NombreCampoRel Then

                        ' ojo que pasa si es una interface (podrian devolver vaios campos o modificarse el nombre)
                        ' cuando encuntre el campo hacer una sql inversa que te daria una col de id para el mimo tipo con lo que podriamos contruir hurllas y meterlas en una colleccion
                        infoReferido = ginfoi.Generar(pTipoReferido, Nothing, "")
                        RecuperarColHuellasRelInversa = adaptador.ConstColHuellasRelInversa(infoReferidora, infoReferido, PropiedadDeInstanciaDN, Nothing, PropiedadDeInstanciaDN.IdInstancia, PropiedadDeInstanciaDN.GUIDInstancia)

                    End If
                Next

                tlp.Confirmar()
            Catch ex As Exception
                tlp.Cancelar()
                Throw
            End Try


            ' Return Recuperar(pID, pTipo, Nothing)
        End Function

        Public Overloads Function RecuperarColHuellasRelDirecta(ByVal PropiedadDeInstanciaDN As TiposYReflexion.DN.PropiedadDeInstanciaDN, ByVal pTipoReferido As Type) As IList


            ' crear un ad especifico para sql que 
            ' identifique la relacion o tabla relacional que esta en juego
            ' contrulla una sql que realice el join de las dos o tres tablas
            ' y recupere un dataset con informacion para motar la coleccion de huellas no tipadas (tipo + id + guid) de todos los tipos que pudieran estar implciados
            ' leugo esas huellas se pueden usar pare recuperar dichos objetos si fuera necesaario


            Dim tlp As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Me.ObtenerTransaccionDeProceso

            Try


                Dim infoReferidora, infoReferido As InfoTypeInstClaseDN
                Dim ginfoi As New InfoTypeInstLN
                Dim camporef As Framework.TiposYReflexion.DN.InfoTypeInstCampoRefDN

                infoReferidora = ginfoi.Generar(PropiedadDeInstanciaDN.Propiedad.ReflectedType, Nothing, "")

                Dim constructor As New Framework.AccesoDatos.MotorAD.AD.ConstructorSQLSQLsAD
                Dim adaptador As AccesorMotorAD
                adaptador = New AccesorMotorAD(tlp, Me.mRec, constructor)

                For Each camporef In infoReferidora.CamposRef

                    ' esto es para encontrar el campo o campos una vez aplicada las directrices del mapeado por ejem plo en una interface
                    If camporef.Campo.Name = PropiedadDeInstanciaDN.NombreCampoRel Then

                        ' ojo que pasa si es una interface (podrian devolver vaios campos o modificarse el nombre)
                        ' cuando encuntre el campo hacer una sql inversa que te daria una col de id para el mimo tipo con lo que podriamos contruir hurllas y meterlas en una colleccion
                        infoReferido = ginfoi.Generar(pTipoReferido, Nothing, "")
                        RecuperarColHuellasRelDirecta = adaptador.ConstColHuellasRelDirecta(infoReferidora, infoReferido, PropiedadDeInstanciaDN, Nothing, PropiedadDeInstanciaDN.IdInstancia)

                    End If
                Next

                tlp.Confirmar()
            Catch ex As Exception
                tlp.Cancelar()
                Throw
            End Try


            ' Return Recuperar(pID, pTipo, Nothing)
        End Function




        Public Overloads Function Recuperar(ByVal pID As String, ByVal pTipo As System.Type) As Object
            Return Recuperar(pID, pTipo, Nothing)
        End Function

        Public Overloads Function Recuperar(ByVal pHuella As Framework.DatosNegocio.IHuellaEntidadDN) As Object


            Dim id As String

            If String.IsNullOrEmpty(pHuella.IdEntidadReferida) OrElse pHuella.IdEntidadReferida = "0" Then
                If String.IsNullOrEmpty(pHuella.GUIDReferida) Then
                    Throw New Framework.AccesoDatos.ApplicationExceptionAD("la huella no permite la recuperacion de la entidad")
                Else
                    id = pHuella.GUIDReferida
                End If
            Else
                id = pHuella.IdEntidadReferida
            End If



            pHuella.EntidadReferida = Recuperar(id, pHuella.TipoEntidadReferida, Nothing)

            Return pHuella
        End Function






        ' FUNCIONALIDAD:
        ' Este método debe ser el llamado desde el exterior para guardar la primera instancia 
        ' PASOS:
        ' 1º Regenerar  las coleciones
        ' 2º Llamar al metodo interno de recuperaccion
        ' 3º Asignar los campos post referidos
        Public Overloads Function Recuperar(ByVal pID As String, ByVal pTipo As System.Type, ByVal pCampoContenedor As InfoTypeInstCampoRefDN) As Object Implements IBaseMotorLN.Recuperar
            Dim coordinador As CTDLN
            Dim transProc As ITransaccionLogicaLN = Nothing

            coordinador = New CTDLN

            Try
                coordinador.IniciarTransaccion(mTL, transProc)

                '1
                If mColIntanciasRecuperadas Is Nothing Then
                    mColIntanciasRecuperadas = New Hashtable
                End If

                mColCampoPostReuperacion = New List(Of CampoPostRecuperacionDN)



                ' conversion del iguid en id si procede

                '2
                Recuperar = RecuperarInst(pID, pTipo, pCampoContenedor)

                '3
                PostAsignacion()

                ' mColIntanciasRecuperadas = Nothing
                mColCampoPostReuperacion = Nothing

                transProc.Confirmar()

            Catch ex As Exception
                If (mTL IsNot Nothing) Then
                    transProc.Cancelar()
                End If

                Throw

            Finally
                'mColIntanciasRecuperadas = Nothing
                mColCampoPostReuperacion = Nothing
            End Try
        End Function

        Public Overloads Function Recuperar(Of T)(ByVal pID As String) As T
            Return CType(Recuperar(pID, GetType(T), Nothing), T)
        End Function



        Public Function RecuperarID(ByVal pTL As ITransaccionLogicaLN, ByVal pGUID As String, ByVal info As InfoTypeInstClaseDN) As String
            Dim idnumerico As Integer
            If pGUID.Length < 30 AndAlso Integer.TryParse(pGUID, idnumerico) Then
                Return pGUID
            End If

            Dim accesor As AccesorMotorAD
            Dim constructor As ConstructorSQLSQLsAD


            constructor = New ConstructorSQLSQLsAD
            accesor = New AccesorMotorAD(pTL, Me.mRec, constructor)
            Return accesor.RecuperarID(pGUID, info)

        End Function



        'Impedir recuperacion duplicada de un objeto
        'Su mision es mirar en la coleccion de objetos recuperados y ver si el ide para esa clase ya se ha recuperado para el mismo objeto
        'raiz de arbol de ser a si se da la entidad ya recuperada
        'No procesar si la instancia ya fue procesada:
        '- La  instancia estara ya procesada si exite la clave y no es nothing
        '- La clave esta formnada por el tipo y el id
        '- En la olecion podemos enontrar:
        '- Claves con valor nothing lo que indicaria que la instancia esta en proceso de recuperacion (esto se puede dar en relaciones con referencias circulares)
        '- Claves con referencia a instancia, en cuyo caso la instancia ya ha sido recuperada de manera total o parcial (parcial: le quedan campos para post recuperacion referencias circulares)
        Private Function RecuperarInst(ByVal id As String, ByVal pTipo As System.Type, ByVal pCampoContenedor As InfoTypeInstCampoRefDN) As Object
            Dim clave As String
            Dim ginfoi As New InfoTypeInstLN
            Dim info As InfoTypeInstClaseDN
            Dim csql As ConstructorSQLSQLsAD

            'Variables iguales para todos los LN
            Dim datos As Hashtable
            Dim coordinador As CTDLN
            Dim transProc As ITransaccionLogicaLN = Nothing
            Dim accesor As AccesorMotorAD
            Dim constructor As ConstructorSQLSQLsAD

            Dim GestorRelacionesCampo As New GestorRelacionesCampoLN
            Dim RelacionUnoUnoSqls As RelacionUnoUnoSQLsDN
            Dim ColRelacionUnoUnoSqls As List(Of RelacionUnoUnoSQLsDN)
            Dim RelacionUnoNSqls As RelacionUnoNSQLsDN
            Dim ColRelacionUnoNSqls As ListaRelacionUnoNSqlsDN

            Dim nombrecampo As String
            Dim cr As InfoTypeInstCampoRefDN
            Dim relaciones As Object







            clave = EntidadBaseDN.ClaveEntidad(pTipo, id)

            If (mColIntanciasRecuperadas.ContainsKey(clave)) Then
                If (Not mColIntanciasRecuperadas.Item(clave) Is Nothing) Then
                    'Ya esta recuperada, se devuelve la instancia
                    Return mColIntanciasRecuperadas.Item(clave)

                Else
                    'Si es nothing se agrega este campo a post recuperacion
                    Me.mColCampoPostReuperacion.Add(New CampoPostRecuperacionDN(clave, pCampoContenedor))
                    Return Nothing
                End If
            End If

            'Registrar el inicio de procesamiento de la entidad en la coleccion de entidades recuperadas.
            'Esta funcionalidad es necesaria para evitar las referencias circulares
            mColIntanciasRecuperadas.Add(clave, Nothing)






            'Paso 1º
            'Inicio de la transaccion
            coordinador = New CTDLN

            Try
                coordinador.IniciarTransaccion(mTL, transProc)



                csql = New ConstructorSQLSQLsAD
                info = ginfoi.Generar(pTipo, Nothing, id)

                If info.IdVal.NombreMap.ToUpper = "GUID" AndAlso Not Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipadaOaInterface(pTipo) Then
                    id = RecuperarID(transProc, id, info)
                    info = ginfoi.Generar(pTipo, Nothing, id)
                End If



                'Paso 2º 
                'Creamos el accesor y el constructor adecuados y recuperamos los datos de la fuente de datos
                constructor = New ConstructorSQLSQLsAD
                accesor = New AccesorMotorAD(transProc, Me.mRec, constructor)

                datos = accesor.RecuperarDatos(id, info)

                If (datos Is Nothing OrElse datos.Count = 0) Then
                    Return Nothing
                End If

                ' Paso 3º 
                'Contruccion de las entidades hijas para los casos de la HashTable que contengan IDs
                For Each cr In info.CamposRefExteriores
                    relaciones = GestorRelacionesCampo.GenerarRelacionesCampoRef(info, cr)

                    If (Not relaciones Is Nothing) Then
                        If (TypeOf relaciones Is List(Of RelacionUnoUnoSQLsDN)) Then
                            'Relacion 1-1
                            ColRelacionUnoUnoSqls = relaciones

                            For Each RelacionUnoUnoSqls In ColRelacionUnoUnoSqls
                                nombrecampo = RelacionUnoUnoSqls.CampoTodo

                                If (datos.Count > 0 AndAlso datos.ContainsKey(nombrecampo) AndAlso Not datos.Item(nombrecampo) Is Nothing AndAlso Not datos.Item(nombrecampo) Is System.DBNull.Value) Then
                                    datos.Item(nombrecampo) = Me.RecuperarInst(CType(datos.Item(nombrecampo), String), RelacionUnoUnoSqls.TipoParte, cr)
                                End If
                            Next

                        Else
                            'Relacion(1 - N)
                            ColRelacionUnoNSqls = relaciones

                            For Each RelacionUnoNSqls In ColRelacionUnoNSqls
                                nombrecampo = RelacionUnoNSqls.NombreBusquedaDatos

                                If (datos.ContainsKey(nombrecampo)) Then
                                    ' gi = New MotorLN.GestorInstanciacionLN(transProc, Me.mRec)
                                    datos.Item(nombrecampo) = Me.RecuperarInst(CType(datos.Item(nombrecampo), ArrayList), RelacionUnoNSqls.TipoParte, cr)
                                End If
                            Next
                        End If
                    End If
                Next

                'Paso 4º 
                'Aqui cosntruimos el objeto DN propio usando el constructor 
                RecuperarInst = constructor.ConstruirEntidad(datos, info, "")

                'Paso 4,1º 
                'Registrar la instancia como ya recuperada
                mColIntanciasRecuperadas.Item(clave) = RecuperarInst

                'Paso 5º 
                'Confirmacion o cancelacion de la transaccion
                transProc.Confirmar()

            Catch ex As Exception
                If (Not mTL Is Nothing) Then
                    transProc.Cancelar()
                End If

                Throw
            End Try
        End Function

        Private Function RecuperarInst(ByVal pColID As System.Collections.IList, ByVal pTipo As System.Type, ByVal pCampoContenedor As InfoTypeInstCampoRefDN) As System.Collections.IList
            Dim i As Int64

            Dim fijacion As TiposYReflexion.DN.FijacionDeTipoDN

            If (pColID Is Nothing) Then
                Throw New ApplicationException("Error: la coleccion de IDs no puede ser nula")
            End If

            For i = 0 To pColID.Count - 1
                If pColID(i) Is DBNull.Value Then

                    pColID(i) = Nothing
                Else
                    If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo) Then
                        pColID(i) = Me.RecuperarInst(CType(pColID(i), String), pTipo, pCampoContenedor)

                    Else

                        pColID(i) = Me.RecuperarInst(CType(pColID(i), String), TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(pTipo, fijacion), pCampoContenedor)

                    End If
                End If





            Next

            Return pColID
        End Function
        ''' <summary>
        ''' Recupera la lista enlazando todos los objetos 
        ''' </summary>
        ''' <param name="pColID"></param>
        ''' <param name="pTipo"></param>
        ''' <param name="pCampoContenedor"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overloads Function Recuperar(ByVal pColID As System.Collections.IList, ByVal pTipo As System.Type, ByVal pCampoContenedor As InfoTypeInstCampoRefDN) As System.Collections.IList Implements IBaseMotorLN.Recuperar
            Dim i As Int64

            If (pColID Is Nothing) Then
                Throw New ApplicationException("Error: la coleccion de IDs no puede ser nula")
            End If

            For i = 0 To pColID.Count - 1
                pColID(i) = Me.Recuperar(pColID(i), pTipo, pCampoContenedor)
            Next

            Return pColID
        End Function

        Private Sub ActualizarHuellas(ByVal mapInst As InfoTypeInstClaseDN, ByVal pEntidadDN As IEntidadDN)
            If Not mapInst.VinculosClasesCache Is Nothing AndAlso mapInst.VinculosClasesCache.Count Then

                Dim huella As HEDN
                Dim VinculoClasesCache As TiposYReflexion.DN.VinculoClaseDN

                For Each VinculoClasesCache In mapInst.VinculosClasesCache

                    ' aqui se pasa el objeto VinculoClasesCache a un metodo del motor que recupera todos los ids de 
                    ' hobjetos de huella cachebla que estan vinculados con esta intancia
                    ' son recuperados uno a uno
                    ' se les asigna el objeto mediante su metod AsignarEntidadReferida
                    ' se guarda
                    huella = Me.Recuperar(pEntidadDN.ID, VinculoClasesCache.TipoClase, Nothing)


                    If Not huella Is Nothing Then
                        huella.AsignarEntidadReferida(pEntidadDN)
                        huella.AsignarIDEntidadReferida()
                        huella.EstadoModificacion = EstadoDatosDN.Modificado
                        Me.Guardarp(huella)

                    End If

                Next

            End If
        End Sub

        Public Function RecuperarLista(ByVal tipo As System.Type) As IList
            Dim aMAD As AccesorMotorAD = Nothing
            Dim coordinador As CTDLN
            Dim transProc As ITransaccionLogicaLN = Nothing

            Try
                coordinador = New CTDLN
                coordinador.IniciarTransaccion(mTL, transProc)

                aMAD = New AccesorMotorAD(transProc, mRec, New ConstructorSQLSQLsAD())

                Dim alIDs As ArrayList = aMAD.BuscarGenericoIDS(tipo, Nothing)

                RecuperarLista = Recuperar(alIDs, tipo, Nothing)

                transProc.Confirmar()

            Catch ex As Exception
                If (mTL IsNot Nothing) Then
                    transProc.Cancelar()
                End If

                Throw
            End Try

        End Function

        Public Function RecuperarListaPorNombre(ByVal tipo As System.Type, ByVal pNombre As String) As IList
            Dim aMAD As AccesorMotorAD = Nothing
            Dim coordinador As CTDLN
            Dim transProc As ITransaccionLogicaLN = Nothing

            Try
                coordinador = New CTDLN
                coordinador.IniciarTransaccion(mTL, transProc)

                aMAD = New AccesorMotorAD(transProc, mRec, New ConstructorSQLSQLsAD())


                Dim cr As New CondicionRelacionalDN
                Dim lsitacr As New List(Of CondicionRelacionalDN)
                lsitacr.Add(cr)

                Dim cond1 As New CondicionCampoDN()
                cond1.Campo = "Nombre"
                cr.Factor1 = cond1
                cr.Operador = "="
                Dim cond2 As New CondicionCampoDN()
                cond2.Valor = pNombre
                cr.Factor2 = cond2

                Dim alIDs As ArrayList = aMAD.BuscarGenericoIDS(tipo, lsitacr)

                RecuperarListaPorNombre = Recuperar(alIDs, tipo, Nothing)

                transProc.Confirmar()

            Catch ex As Exception
                If (mTL IsNot Nothing) Then
                    transProc.Cancelar()
                End If

                Throw
            End Try

        End Function
#End Region

    End Class
End Namespace


Public Class OperacionRealizada
    Private mOperacion As OperacionGuardarLN = OperacionGuardarLN.Ninguna
    Public Property Operacion() As OperacionGuardarLN
        Get
            Return Me.mOperacion
        End Get
        Set(ByVal value As OperacionGuardarLN)


            If Not mOperacion = OperacionGuardarLN.AñadidoColPostGuarda Then
                mOperacion = value
            End If

        End Set
    End Property
End Class
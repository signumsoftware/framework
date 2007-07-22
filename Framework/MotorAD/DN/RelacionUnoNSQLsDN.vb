Imports System.Runtime.Serialization.Formatters.Binary
Imports System.IO
Imports Framework.TiposYReflexion.DN

Namespace DN
    <Serializable()> Public Class RelacionUnoNSQLsDN
        Implements IRelacionUnoNDN




#Region "Atributos"
        Protected mEsCol As Boolean
        Protected mEsInterface As Boolean
        Protected mTipoTodo As Type
        Protected mTipoParte As Type
        Protected mNombreCampoTodo As String
        Protected mTablaTodo As String
        Protected mTablaParte As String
        Protected mCampoTodo As String = "ID"
        Protected mCampoParte As String = "ID"
#End Region

#Region "Constructores"
        Public Sub New()

        End Sub
        Public Sub New(ByVal pTipoTodo As Type, ByVal pTipoParte As Type, ByVal pNombreCampoTodo As String, ByVal pTablaTodo As String, ByVal pTablaParte As String, ByVal pPropiedad As Reflection.PropertyInfo)
            mEsCol = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsColeccion(pPropiedad.PropertyType)
            mEsInterface = pPropiedad.PropertyType.IsInterface
            mNombreCampoTodo = pNombreCampoTodo
            mTablaTodo = pTablaTodo
            mTablaParte = pTablaParte
            mTipoTodo = pTipoTodo
            mTipoParte = pTipoParte
        End Sub
        Public Sub New(ByVal pTipoTodo As Type, ByVal pTipoParte As Type, ByVal pNombreCampoTodo As String, ByVal pTablaTodo As String, ByVal pTablaParte As String)
            mEsCol = True
            mEsInterface = False
            mNombreCampoTodo = pNombreCampoTodo
            mTablaTodo = pTablaTodo
            mTablaParte = pTablaParte
            mTipoTodo = pTipoTodo
            mTipoParte = pTipoParte
        End Sub
#End Region

#Region "Propiedades"

        Public ReadOnly Property SelectInversa() As String
            Get

                If Me.mEsInterface Then

                    Return " Select " & Me.CampoTodoID & "," & CampoTodoGUID & " FROM " & mTablaTodo & " INNER JOIN " & mTablaParte & " ON " & Me.CampoTodor & Me.mTipoParte.Name & "=" & Me.CampoParter & " WHERE " & Me.CampoParteID & " = " & "@" & Me.CampoParteID.Replace(".", "")

                Else
                    If mEsCol Then
                        If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(Me.mTipoTodo) Then
                            Return " Select " & Me.CampoTodoID & "," & CampoTodoGUID & " FROM " & mTablaTodo & " INNER JOIN " & NombreTablaRel & " ON " & Me.CampoTodoID & "=" & CampoTodoTR & "  INNER JOIN " & Me.mTablaParte & " ON " & Me.CampoParteTR & " = " & CampoParteID & " WHERE " & Me.CampoParteID & " = " & "@" & Me.CampoParter.Replace(".", "")

                        Else
                            Return " Select " & Me.CampoTodoID & "," & CampoTodoGUID & " FROM " & mTablaTodo & " INNER JOIN " & NombreTablaRel & " ON " & Me.CampoTodoID & "=" & CampoTodoTR & "  INNER JOIN " & Me.mTablaParte & " ON " & Me.CampoParteTR & " = " & CampoParteGUID & " WHERE " & Me.CampoParteGUID & " = " & "@" & Me.CampoParteGUID.Replace(".", "")

                        End If

                    Else
                        If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(Me.mTipoTodo) Then
                            Return " Select " & Me.CampoTodoID & "," & CampoTodoGUID & " FROM " & mTablaTodo & " INNER JOIN " & mTablaParte & " ON " & Me.CampoTodoGUID & "=" & Me.CampoParteGUID & " WHERE " & Me.CampoParteID & " = " & "@" & Me.CampoParteID.Replace(".", "")

                        Else
                            Return " Select " & Me.CampoTodoID & "," & CampoTodoGUID & " FROM " & mTablaTodo & " INNER JOIN " & mTablaParte & " ON " & Me.CampoTodor & "=" & Me.CampoParter & " WHERE " & Me.CampoParteID & " = " & "@" & Me.CampoParteID.Replace(".", "")

                        End If

                    End If

                End If



            End Get
        End Property

        'Public ReadOnly Property SelectJOINInversa(ByVal pSqlOriginal As String) As String
        '    Get


        '        Dim partessq() As String = pSqlOriginal.Split("where")
        '        If partessq.Length > 2 Then
        '            Throw New Framework.AccesoDatos.ApplicationExceptionAD("division de la sql incorrecta")
        '        End If


        '        If Me.mEsInterface Then

        '            Return partessq(0) & " INNER JOIN " & mTablaParte & " ON " & Me.CampoTodor & Me.mTipoParte.Name & "=" & Me.CampoParter & " WHERE (" & Me.CampoParteID & " = " & "@" & Me.CampoParteID.Replace(".", "") & ") and " & partessq(1)

        '        Else
        '            If mEsCol Then

        '                Return " Select " & Me.CampoTodoID & "," & CampoTodoGUID & " FROM " & mTablaTodo & " INNER JOIN " & NombreTablaRel & " ON " & Me.CampoTodoID & "=" & CampoTodoTR & "  INNER JOIN " & Me.mTablaParte & " ON " & Me.CampoParteTR & " = " & CampoParteID & " WHERE " & Me.CampoParteID & " = " & "@" & Me.CampoParter.Replace(".", "")

        '            Else
        '                Return " Select " & Me.CampoTodoID & "," & CampoTodoGUID & " FROM " & mTablaTodo & " INNER JOIN " & mTablaParte & " ON " & Me.CampoTodor & "=" & Me.CampoParter & " WHERE " & Me.CampoParteID & " = " & "@" & Me.CampoParteID.Replace(".", "")

        '            End If

        '        End If



        '    End Get
        'End Property

        Public ReadOnly Property SelectDirecta() As String
            Get

                If Me.mEsInterface Then

                    Return " Select " & Me.CampoParteID & "," & Me.CampoParteGUID & " FROM " & mTablaTodo & " INNER JOIN " & mTablaParte & " ON " & Me.CampoTodor & Me.mTipoParte.Name & "=" & Me.CampoParter & " WHERE " & Me.CampoTodoID & " = " & "@" & Me.CampoTodoID.Replace(".", "")

                Else
                    If mEsCol Then

                        Return " Select " & Me.CampoParteID & "," & Me.CampoParteGUID & " FROM " & mTablaTodo & " INNER JOIN " & NombreTablaRel & " ON " & Me.CampoTodoID & "=" & CampoTodoTR & "  INNER JOIN " & Me.mTablaParte & " ON " & Me.CampoParteTR & " = " & CampoParteID & " WHERE " & Me.CampoTodoID & " = " & "@" & Me.CampoTodor.Replace(".", "")

                    Else
                        If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(Me.mTipoTodo) Then
                            Return " Select " & Me.CampoParteID & "," & Me.CampoParteGUID & " FROM " & mTablaTodo & " INNER JOIN " & mTablaParte & " ON " & Me.CampoTodoGUID & "=" & Me.CampoParteGUID & " WHERE " & Me.CampoTodoID & " = " & "@" & Me.CampoTodoID.Replace(".", "")
                        Else
                            Return " Select " & Me.CampoParteID & "," & Me.CampoParteGUID & " FROM " & mTablaTodo & " INNER JOIN " & mTablaParte & " ON " & Me.CampoTodor & "=" & Me.CampoParter & " WHERE " & Me.CampoTodoID & " = " & "@" & Me.CampoTodoID.Replace(".", "")
                        End If

                    End If

                End If



            End Get
        End Property

        Public ReadOnly Property NombreBusquedaDatos() As String
            Get
                Return Me.mNombreCampoTodo & mTipoParte.Name.ToString.Replace("`", "")
            End Get
        End Property

        Public Property TipoParte() As System.Type Implements IRelacionUnoNDN.TipoParte
            Get
                Return Me.mTipoParte
            End Get
            Set(ByVal Value As System.Type)
                Me.mTipoParte = Value
            End Set
        End Property

        Public Property TipoTodo() As System.Type Implements IRelacionUnoNDN.TipoTodo
            Get
                Return Me.mTipoTodo
            End Get
            Set(ByVal Value As System.Type)
                Me.mTipoTodo = Value
            End Set
        End Property

        'Public Property CampoParte() As String Implements IRelacionUnoNDN.CampoParte
        '    Get
        '        Return mCampoParte
        '    End Get
        '    Set(ByVal Value As String)
        '        mCampoParte = Value
        '    End Set
        'End Property



        Public Property CampoParte() As String Implements IRelacionUnoNDN.CampoParte
            Get
                'If String.IsNullOrEmpty(mCampoParte) Then

                Dim tipoFijado As System.Type = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(Me.mTipoParte, Nothing)



                If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(tipoFijado) Then


                    ' mCampoParte = "GUIDReferida"
                    mCampoParte = "GUID"
                    ' mCampoParte = "ID"
                Else
                    mCampoParte = "ID"
                End If
                '   End If
                Return mCampoParte
            End Get
            Set(ByVal Value As String)
                mCampoParte = Value.Substring(1)
            End Set
        End Property

        Public Property CampoTodo() As String Implements IRelacionUnoNDN.CampoTodo
            Get
                Return mCampoTodo
            End Get
            Set(ByVal Value As String)
                mCampoTodo = Value
            End Set
        End Property

        Public ReadOnly Property SqlRelacionParte() As String Implements IRelacionUnoNDN.SqlRelacionParte
            Get
                Return "ALTER TABLE " & NombreTablaRel & " ADD CONSTRAINT " & NombreTablaRel & Me.CampoParteTR & "  FOREIGN KEY(" & CampoParteTR & ") REFERENCES " & Me.mTablaParte & " (" & Me.CampoParte & ")  "
            End Get
        End Property

        Public ReadOnly Property SqlRelacionTodo() As String Implements IRelacionUnoNDN.SqlRelacionTodo
            Get
                Return "ALTER TABLE " & NombreTablaRel & " ADD CONSTRAINT " & NombreTablaRel & CampoTodoTR & "  FOREIGN KEY(" & CampoTodoTR & ") REFERENCES " & mTablaTodo & " (" & CampoTodo & ")  "
            End Get
        End Property

        Public ReadOnly Property SqlTablaRel() As String Implements IRelacionUnoNDN.SqlTablaRel
            Get


                Return "CREATE TABLE " & NombreTablaRel & " ( ID bigint IDENTITY PRIMARY KEY," & Me.CampoTodoTR & " " & TipoCampo(Me.mTipoTodo) & " NOT NULL," & Me.CampoParteTR & " " & TipoCampo(Me.mTipoParte) & " NOT NULL )"
            End Get
        End Property


        Private Function TipoCampo(ByVal ptipo As System.Type) As String

            Dim tipoFijado As System.Type = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(ptipo, Nothing)


            If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(tipoFijado) Then


                Return "nvarchar(50)"

            Else

                Return "bigint"


            End If

            'If pNombreCampo.ToLower = "id" Then
            'End If


            'If pNombreCampo.ToLower = "guid" Then
            'End If
            Throw New Framework.AccesoDatos.ApplicationExceptionAD("no se puede resilver el tipo del campo de tabla")
        End Function


        Public Property TablaParte() As String Implements IRelacionUnoNDN.TablaParte
            Get
                Return mTablaParte
            End Get
            Set(ByVal Value As String)
                mTablaParte = Value
            End Set
        End Property

        Public Property TablaTodo() As String Implements IRelacionUnoNDN.TablaTodo
            Get
                Return mTablaTodo
            End Get
            Set(ByVal Value As String)
                mTablaTodo = Value
            End Set
        End Property

        Public ReadOnly Property NombreTablaRel() As String Implements IRelacionUnoNDN.NombreTablaRel
            Get
                Return "tr" & Me.mTablaTodo & Me.mNombreCampoTodo & "X" & Me.mTablaParte
            End Get
        End Property

        Public Property NombrePropidadTodo() As String Implements IRelacionUnoNDN.NombrePropidadTodo
            Get
                Return mNombreCampoTodo
            End Get
            Set(ByVal Value As String)
                mNombreCampoTodo = Value.Substring(1)
            End Set
        End Property

        Public ReadOnly Property CampoParteTR() As String Implements IRelacionUnoNDN.CampoParteTR
            Get
                Return "idp" & Me.mTablaParte
            End Get
        End Property

        Public ReadOnly Property CampoTodoTR() As String Implements IRelacionUnoNDN.CampoTodoTR
            Get
                Return "idt" & Me.mTablaTodo
            End Get
        End Property

        Public ReadOnly Property CampoParter() As String
            Get
                Return Me.mTablaParte & "." & mCampoParte
            End Get
        End Property

        Public ReadOnly Property CampoTodor() As String
            Get
                If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(Me.mTipoTodo) Then
                    Return Me.mTablaTodo & "." & mCampoTodo & mNombreCampoTodo.Replace("Huella", "")

                Else
                    Return Me.mTablaTodo & "." & mCampoTodo & mNombreCampoTodo
                End If

            End Get
        End Property
        Public ReadOnly Property CampoTodoID() As String
            Get
                Return Me.mTablaTodo & "." & mCampoTodo
            End Get
        End Property
        Public ReadOnly Property CampoTodoGUID() As String
            Get
                Return Me.mTablaTodo & ".GUID"
            End Get
        End Property
        Public ReadOnly Property CampoParteID() As String
            Get
                Return Me.mTablaParte & "." & Me.mCampoParte
            End Get

        End Property

        Public ReadOnly Property CampoParteGUID() As String
            Get
                Return Me.mTablaParte & ".GUID"
            End Get

        End Property

#End Region

        Public Function Clone() As Object Implements System.ICloneable.Clone
            Dim formateador As BinaryFormatter
            Dim memoria As MemoryStream

            formateador = New BinaryFormatter
            memoria = New MemoryStream

            'Nos serializamos y volvemos a poner el puntero de lectura/escritura al principio
            formateador.Serialize(memoria, Me)
            memoria.Seek(0, IO.SeekOrigin.Begin)

            'Nos desserializamos para conseguir la copia
            Return formateador.Deserialize(memoria)
        End Function

        Public ReadOnly Property TablaOrigenYDestinoIguales() As Boolean Implements IRelacionUnoNDN.TablaOrigenYDestinoIguales
            Get
                Return Me.mTablaParte = Me.mTablaTodo
            End Get
        End Property

        Function CrearClonHistorico(ByVal pTodoDatosMapInstClase As InfoDatosMapInstClaseDN, ByVal pParteDatosMapInstClase As InfoDatosMapInstClaseDN) As Object Implements IRelacionUnoNDN.CrearClonHistorico

            Dim RelacionUnoUnoHistorica As RelacionUnoNSQLsDN
            RelacionUnoUnoHistorica = Me.Clone

            If RelacionUnoUnoHistorica.TablaOrigenYDestinoIguales Then
                RelacionUnoUnoHistorica.TablaParte = pTodoDatosMapInstClase.TablaHistoria
            ElseIf Not pParteDatosMapInstClase Is Nothing AndAlso Not String.IsNullOrEmpty(pParteDatosMapInstClase.TablaHistoria) Then
                RelacionUnoUnoHistorica.TablaParte = pParteDatosMapInstClase.TablaHistoria
            End If
            If Not pTodoDatosMapInstClase Is Nothing Then
                RelacionUnoUnoHistorica.TablaTodo = pTodoDatosMapInstClase.TablaHistoria

            End If

            Return RelacionUnoUnoHistorica
        End Function
    End Class
End Namespace

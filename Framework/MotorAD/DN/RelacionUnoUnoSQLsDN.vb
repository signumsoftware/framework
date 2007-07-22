Imports System.Runtime.Serialization.Formatters.Binary
Imports System.IO
Namespace DN
    <Serializable()> Public Class RelacionUnoUnoSQLsDN
        Implements IRelacionUnoUnoDN







#Region "Atributos"
        Protected mTipoTodo As Type
        Protected mTipoParte As Type
        Protected mTablaTodo As String
        Protected mTablaParte As String
        Protected mCampoTodo As String
        Protected mCampoParte As String
#End Region

#Region "Constructores"
        Public Sub New()

        End Sub
        Public Sub New(ByVal pTipoTodo As Type, ByVal pTipoParte As Type, ByVal pCampoTodo As String, ByVal pTablaTodo As String, ByVal pTablaParte As String)
            mCampoTodo = pCampoTodo
            mTablaTodo = pTablaTodo
            mTablaParte = pTablaParte
            mTipoTodo = pTipoTodo
            mTipoParte = pTipoParte
        End Sub
#End Region

#Region "Propiedades"
        Public Property TipoParte() As Type Implements IRelacionUnoUnoDN.TipoParte
            Get
                Return Me.mTipoParte
            End Get
            Set(ByVal Value As Type)
                Me.mTipoParte = Value
            End Set
        End Property

        Public Property TipoTodo() As Type Implements IRelacionUnoUnoDN.TipoTodo
            Get
                Return Me.mTipoTodo
            End Get
            Set(ByVal Value As Type)
                Me.mTipoTodo = Value
            End Set
        End Property

        Public Property CampoParte() As String Implements IRelacionUnoUnoDN.CampoParte
            Get
                If String.IsNullOrEmpty(mCampoParte) Then
                    If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(Me.mTipoParte) Then


                        'mCampoParte = "GUIDReferida"
                        mCampoParte = "GUID"
                        ' mCampoParte = "ID"
                    Else
                        mCampoParte = "ID"
                    End If
                End If
                Return mCampoParte
            End Get
            Set(ByVal Value As String)
                mCampoParte = Value.Substring(1)
            End Set
        End Property

        Public Property CampoTodo() As String Implements IRelacionUnoUnoDN.CampoTodo
            Get
                'If String.IsNullOrEmpty(mCampoTodo) Then
                '    If TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(Me.mTipoTodo) Then
                '        mCampoTodo = "GUID"
                '    Else
                '        mCampoTodo = "ID"
                '    End If
                'End If

                Return mCampoTodo

            End Get
            Set(ByVal Value As String)

                mCampoTodo = Value
            End Set
        End Property

        Public ReadOnly Property SqlRelacion() As String Implements IRelacionUnoUnoDN.SqlRelacion
            Get
                SqlRelacion = "ALTER TABLE " & mTablaTodo & " ADD CONSTRAINT " & mTablaTodo & mCampoTodo & "  FOREIGN KEY(" & mCampoTodo & ") REFERENCES " & mTablaParte & " (" & CampoParte & ")  "
            End Get
        End Property

        Public Property TablaParte() As String Implements IRelacionUnoUnoDN.TablaParte
            Get
                Return mTablaParte
            End Get
            Set(ByVal Value As String)
                mTablaParte = Value
            End Set
        End Property

        Public Property TablaTodo() As String Implements IRelacionUnoUnoDN.TablaTodo
            Get
                Return mTablaTodo
            End Get
            Set(ByVal Value As String)
                mTablaTodo = Value
            End Set
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

        Public ReadOnly Property TablaOrigenYDestinoIguales() As Boolean Implements IRelacionUnoUnoDN.TablaOrigenYDestinoIguales
            Get
                Return Me.mTablaParte = Me.mTablaTodo
            End Get
        End Property



        Public Function CrearClonHistorico(ByVal pTodoDatosMapInstClase As TiposYReflexion.DN.InfoDatosMapInstClaseDN, ByVal pParteDatosMapInstClase As TiposYReflexion.DN.InfoDatosMapInstClaseDN) As Object Implements IRelacionUnoUnoDN.CrearClonHistorico
            Dim RelacionUnoUnoHistorica As RelacionUnoUnoSQLsDN
            RelacionUnoUnoHistorica = Me.Clone

            If RelacionUnoUnoHistorica.TablaOrigenYDestinoIguales Then
                RelacionUnoUnoHistorica.TablaParte = pTodoDatosMapInstClase.TablaHistoria
            ElseIf Not pParteDatosMapInstClase Is Nothing AndAlso Not String.IsNullOrEmpty(pParteDatosMapInstClase.TablaHistoria) Then
                RelacionUnoUnoHistorica.TablaParte = pParteDatosMapInstClase.TablaHistoria
            End If
            RelacionUnoUnoHistorica.TablaTodo = pTodoDatosMapInstClase.TablaHistoria

            Return RelacionUnoUnoHistorica
        End Function

        Public ReadOnly Property TablaSqlRelacion() As String Implements IRelacionUnoUnoDN.TablaSqlRelacion
            Get
                Return Me.mTablaTodo & Me.mCampoTodo
            End Get

        End Property
    End Class
End Namespace

Namespace DN
    Public Class RelacionSQLsDN

#Region "Atributos"
        Protected mTipoRel As TipoRelacionDN
        Protected mCreacionTablaRelacionSQL As String
        Protected mCreacionTrTodoSQL As String
        Protected mCreacionTrParteSQL As String
        Protected mCreacionRelacionTodoParte As String
#End Region

#Region "Propiedades"
        Public Property TipoRel() As TipoRelacionDN
            Get
                Return mTipoRel
            End Get
            Set(ByVal value As TipoRelacionDN)
                mTipoRel = value
            End Set
        End Property

        Public Property CreacionTablaRelacionSQL() As String
            Get
                Return mCreacionTablaRelacionSQL
            End Get
            Set(ByVal value As String)
                mCreacionTablaRelacionSQL = value
            End Set
        End Property

        Public Property CreacionTrTodoSQL() As String
            Get
                Return mCreacionTrTodoSQL
            End Get
            Set(ByVal value As String)
                mCreacionTrTodoSQL = value
            End Set
        End Property

        Public Property CreacionTrParteSQL() As String
            Get
                Return mCreacionTrParteSQL
            End Get
            Set(ByVal value As String)
                mCreacionTrParteSQL = value
            End Set
        End Property

        Public Property CreacionRelacionTodoParte() As String
            Get
                Return mCreacionRelacionTodoParte
            End Get
            Set(ByVal value As String)
                mCreacionRelacionTodoParte = value
            End Set
        End Property
#End Region

    End Class
End Namespace

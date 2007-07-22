Imports Framework.TiposYReflexion.DN
Namespace DN
    Public Interface IRelacionUnoNDN
        Inherits ICloneable

#Region "Propiedades"
        Property TipoTodo() As System.Type
        Property TipoParte() As System.Type

        Property TablaTodo() As String
        Property TablaParte() As String
        Property CampoTodo() As String
        Property CampoParte() As String
        Property NombrePropidadTodo() As String
        ReadOnly Property CampoTodoTR() As String
        ReadOnly Property CampoParteTR() As String
        ReadOnly Property NombreTablaRel() As String
        ReadOnly Property SqlRelacionTodo() As String
        ReadOnly Property SqlRelacionParte() As String
        ReadOnly Property SqlTablaRel() As String
        ReadOnly Property TablaOrigenYDestinoIguales() As Boolean
        Function CrearClonHistorico(ByVal pTodoDatosMapInstClase As InfoDatosMapInstClaseDN, ByVal pParteDatosMapInstClase As InfoDatosMapInstClaseDN) As Object
#End Region

    End Interface
End Namespace

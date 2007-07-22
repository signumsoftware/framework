Imports Framework.TiposYReflexion.DN
Namespace DN
    Public Interface IRelacionUnoUnoDN
        Inherits ICloneable

#Region "Propiedades"
        Property TipoTodo() As System.Type
        Property TipoParte() As System.Type

        Property TablaTodo() As String
        Property TablaParte() As String
        Property CampoTodo() As String
        Property CampoParte() As String
        ReadOnly Property SqlRelacion() As String
        ReadOnly Property TablaOrigenYDestinoIguales() As Boolean
        Function CrearClonHistorico(ByVal pTodoDatosMapInstClase As InfoDatosMapInstClaseDN, ByVal pParteDatosMapInstClase As InfoDatosMapInstClaseDN) As Object
        ReadOnly Property TablaSqlRelacion() As String

#End Region

    End Interface
End Namespace

#Region "Importaciones"

Imports Framework.TiposYReflexion.DN

#End Region

Namespace AD
    Public Interface IBaseAccesorMotorAD

#Region "Metodos"
        Function Insertar(ByVal pEntidad As Object) As Object
        Function Modificar(ByVal pEntidad As Object) As Integer
        Function Eliminar(ByVal pId As Integer) As Integer
        'Contiene los datos de la columnas mas los ID de las entidades relacionadas que pueden ser ArrayList para las relaciones 1-*
        Function RecuperarDatos(ByVal pId As String, ByVal pInfo As InfoTypeInstClaseDN) As ICollection
        Function RecuperarDatosID(ByVal pGUID As String, ByVal pInfo As InfoTypeInstClaseDN) As String
        'Contiene los datos de la columnas mas los ID de las entidades relacionadas que pueden ser ArrayList para las relaciones 1-*
        Function RecuperarDatosVarios(ByVal pColIDs As ArrayList) As ArrayList
        Function GuardarRelacion(ByVal pEntidad As Object, ByVal pMetodoGuardarRelacionTR As GuardarRelacionTR) As Int64
#End Region

    End Interface
End Namespace

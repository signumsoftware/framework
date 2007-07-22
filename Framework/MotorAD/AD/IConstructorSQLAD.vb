#Region "Importaciones"

Imports System.Collections.Generic

Imports Framework.TiposYReflexion.DN

#End Region

Namespace AD
    Public Interface IConstructorSQLAD
        Inherits IConstructorBusquedaAD

#Region "Metodos"
        'Devuelve una HashTable de los datos de los campos del objeto y de sus IDs para sus objetos relacionados
        Function ConstruirDatos(ByVal pDR As IDataReader) As Object
        Function ConstruirEntidad(ByVal pHLDatos As Hashtable, ByVal pObjeto As InfoTypeInstClaseDN, ByVal pPrefijo As String) As Object
        Function ConstruirEntidad(ByVal pALDatos As IList, ByVal pObjeto As InfoTypeInstClaseDN) As Object
        Function ConstSqlSelectID(ByVal pObjeto As InfoTypeInstClaseDN, ByRef pParametros As List(Of IDataParameter), ByVal pID As String) As String

        Function ConstSqlSelect(ByVal pObjeto As InfoTypeInstClaseDN, ByRef pParametros As List(Of IDataParameter), ByVal pID As String) As String
        Function ConstSqlInsert(ByVal pObjeto As InfoTypeInstClaseDN, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As DateTime, ByRef pSqlHistorica As String) As String
        Function ConstSqlUpdate(ByVal pObjeto As InfoTypeInstClaseDN, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As DateTime, ByRef pSqlHistorica As String) As String
        Function ConstSqlDelete(ByVal pObjeto As InfoTypeInstClaseDN, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As DateTime) As String
        ' Function ConstSqlRelacionUnoN(ByVal o As InfoTypeInstancClaseDN, ByVal cr As InfoTypeInstancCampoRefDN, ByRef parametros As ColIDataParameter, ByVal fechaModificacion As DateTime) As String
        Function ConstSqlRelacionUnoN(ByVal pContenedor As Framework.DatosNegocio.IEntidadBaseDN, ByVal pContenido As Framework.DatosNegocio.IEntidadBaseDN, ByVal pNombreTabla As String, ByVal pCampoTodo As String, ByVal pCampoDestino As String, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date, ByVal pNumeroInstancia As Int64) As String
        Function ConstSqlRelacionUnoN(ByVal pObjeto As InfoTypeInstClaseDN, ByVal pCampoRef As InfoTypeInstCampoRefDN, ByRef parametros As List(Of IDataParameter), ByVal pFechaModificacion As DateTime, ByRef pSqlHistorica As String) As List(Of SqlParametros)
#End Region

    End Interface
End Namespace

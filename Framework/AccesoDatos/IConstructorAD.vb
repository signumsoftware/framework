#Region "Importaciones"

Imports System.Collections.Generic

#End Region

''' <summary>
''' Esta interface define las operaciones minimas que tiene que proprocionar un constructor de entidades de
''' datos de negocio.
''' </summary>
Public Interface IConstructorAD

#Region "Metodos"
    ''' <summary>
    ''' Esta funcion transforma un IDataReader en un array o una hash de datos que representa a una entidad de forma
    ''' entendible para los constructores especificos de entidades.
    ''' </summary>
    ''' <remarks>
    ''' Para cada objeto se devuelve tambien una coleccion de ids de los objetos relacionados.
    ''' </remarks>
    ''' <param name="pDR" type="IDataReader">
    ''' IDataReader del que vamos a sacar los datos.
    ''' </param>
    ''' <returns>
    ''' Nothing si el IDataReader estaba vacio, una Hashtable si solo habia una entidad, o un ArrayList si habia varias
    ''' entidades.
    ''' </returns>
    Function ConstruirDatos(ByVal pDR As IDataReader) As Object

    ''' <summary>Este metodo construye una entidad de datos de negocio a partir de una hash de datos.</summary>
    ''' <param name="pHLDatos" type="Hashtable">
    ''' Hashtable con los datos de la entidad.
    ''' </param>
    ''' <returns>
    ''' La entidad de datos de negocio.
    ''' </returns>
    Function ConstruirEntidad(ByVal pHLDatos As Hashtable) As Object

    ''' <summary>Este metodo construye una coleccion de entidades de datos de negocio a partir de un ArrayList de datos.</summary>
    ''' <param name="pALDatos" type="IList">
    ''' ArrayList con los datos de las entidades.
    ''' </param>
    ''' <returns>
    ''' La coleccion de entidades.
    ''' </returns>
    Function ConstruirEntidad(ByVal pALDatos As IList) As Object

    ''' <summary>Este metodo construye una sql para seleccionar una entidad.</summary>
    ''' <param name="pID" type="String">
    ''' ID de la entidad que queremos seleccionar.
    ''' </param>
    ''' <returns>
    ''' La sql de seleccion.
    ''' </returns>
    Function ConstSqlSelect(ByVal pID As String) As String



    Function ConstSqlSelectID(ByVal pID As String) As String

    ''' <summary>Este metodo construye una sql para insertar una entidad.</summary>
    ''' <param name="pObjeto" type="Object">
    ''' Entidad a insertar.
    ''' </param>
    ''' <param name="pParametros" type="List(Of IDataParameter)">
    ''' Coleccion de parametros donde vamos a poner los campos de la entidad a insertar.
    ''' </param>
    ''' <param name="pFechaModificacion" type="DateTime">
    ''' Fecha de modificacion de la entidad (para control de accesos concurrentes).
    ''' </param>
    ''' <returns>
    ''' La sql de insercion.
    ''' </returns>
    Function ConstSqlInsert(ByVal pObjeto As Object, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As DateTime, ByRef pSqlHistorica As String) As String

    ''' <summary>Este metodo construye una sql para actualizar una entidad.</summary>
    ''' <param name="pObjeto" type="Object">
    ''' Entidad a actualizar.
    ''' </param>
    ''' <param name="pParametros" type="List(Of IDataParameter)">
    ''' Coleccion de parametros donde vamos a poner los campos de la entidad a actualizar.
    ''' </param>
    ''' <param name="pFechaModificacion" type="DateTime">
    ''' Fecha de modificacion de la entidad (para control de accesos concurrentes).
    ''' </param>
    ''' <returns>
    ''' La sql de actualizacion.
    ''' </returns>
    Function ConstSqlUpdate(ByVal pObjeto As Object, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As DateTime, ByRef pSqlHistorica As String) As String

    ''' <summary>Este metodo construye una sql para dar de baja una entidad.</summary>
    ''' <param name="pObjeto" type="Object">
    ''' Entidad a dar de baja.
    ''' </param>
    ''' <param name="pParametros" type="List(Of IDataParameter)">
    ''' Coleccion de parametros donde vamos a poner los campos de la entidad a dar de baja.
    ''' </param>
    ''' <param name="pFechaModificacion" type="DateTime">
    ''' Fecha de modificacion de la entidad (para control de accesos concurrentes).
    ''' </param>
    ''' <returns>
    ''' La sql de baja.
    ''' </returns>
    Function ConstSqlBaja(ByVal pObjeto As Object, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As DateTime) As String
#End Region

End Interface

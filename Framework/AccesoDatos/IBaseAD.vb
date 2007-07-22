#Region "Importaciones"

Imports System.Collections.Generic

#End Region

''' <summary>
''' Interfaz que proporciona la funcionalidad basica a un accesor de datos para trabajar con entidades de datos.
''' </summary>
Public Interface IBaseAD

#Region "Metodos"
    ''' <summary>Metodo que recupera los datos de un objeto a partir de su ID.</summary>
    ''' <param name="pID" type="String">
    ''' ID del objeto que queremos recuperar.
    ''' </param>
    ''' <returns>
    ''' Una coleccion que contiene los datos de la columnas mas los id de las entidades relacionadas que pueden ser arraylist para las relaciones 1-*.
    ''' </returns>
    Function RecuperarDatos(ByVal pID As String) As ICollection

    ''' <summary>Metodo que recupera los datos de un grupo de objetos a partir de una coleccion de IDs.</summary>
    ''' <param name="pColIDs" type="List(Of String)">
    ''' ID del objeto que queremos recuperar.
    ''' </param>
    ''' <returns>
    ''' Una coleccion que contiene los datos de la columnas mas los id de las entidades relacionadas que pueden ser arraylist para las relaciones 1-*.
    ''' </returns>
    Function RecuperarDatosVarios(ByVal pColIDs As List(Of String)) As ArrayList

    ''' <summary>Metodo que inserta una entidad en la base de datos.</summary>
    ''' <param name="pEntidad" type="Object">
    ''' Objeto que vamos a guardar.
    ''' </param>
    ''' <returns>
    ''' El objeto modificado (despues de guardarse tiene asignado un ID).
    ''' </returns>
    Function Insertar(ByVal pEntidad As Object) As Object

    ''' <summary>Metodo que modifica una entidad en la base de datos.</summary>
    ''' <param name="pEntidad" type="Object">
    ''' Objeto que vamos a modificar.
    ''' </param>
    ''' <returns>
    ''' El numero de filas modificadas en la BD.
    ''' </returns>
    Function Modificar(ByVal pEntidad As Object) As Integer

    ''' <summary>Metodo que elimina una entidad en la base de datos.</summary>
    ''' <param name="pID" type="String">
    ''' ID del objeto que vamos a eliminar.
    ''' </param>
    ''' <returns>
    ''' El numero de filas eliminadas en la BD.
    ''' </returns>
    Function Eliminar(ByVal pId As Integer) As Integer

    'TODO: ESTO QUE ES???
    Function GuardarRelacion(ByVal pEntidad As Object, ByVal pMetodoGuardarRelacionTR As GuardarRelacionTR) As Int64
#End Region

End Interface

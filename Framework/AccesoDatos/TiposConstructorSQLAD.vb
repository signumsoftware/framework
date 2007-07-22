#Region "Importaciones"

Imports System.Collections.Generic

#End Region

Public Delegate Function ConstruirDelg(ByVal pID As String, ByVal pNombre As String) As IEntidadBaseDN

''' <summary>Esta clase se encarga de recuperar y construir objetos de tipo.</summary>
''' <remarks>Los objetos que representan tipos son pares Id-Nombre.</remarks>
Public Class TiposConstructorSQLAD
    Inherits BaseConstructorSQLAD

#Region "Atributos"
    'Nombre de la tabla sobre la que vamos a construir las consultas de sql
    Protected mNombreTabla As String

    'Delegado que se encarga de construir las entidades de datos
    Protected mConstruirDelg As ConstruirDelg
#End Region

#Region "Constructores"
    ''' <summary>
    ''' Constructor parametrizado por defecto.
    ''' </summary>
    ''' <param name="pNombreTabla" type="String">
    ''' Nombre de la tabla sobre la que vamos a construir las consultas de sql
    ''' </param>
    ''' <param name="pMetodoConstructor" type="ConstruirDelg">
    ''' Delegado que se encarga de construir la entidad de datos
    ''' </param>
    Public Sub New(ByVal pNombreTabla As String, ByVal pMetodoConstructor As ConstruirDelg)
        If (pNombreTabla Is Nothing OrElse pNombreTabla = String.Empty) Then
            Throw New ApplicationException("el nombre de tabla no puede ser nulo")

        Else
            mNombreTabla = pNombreTabla
        End If

        If (pMetodoConstructor Is Nothing) Then
            Throw New ApplicationException("metodoContruccion no puede ser nulo")
        End If

        mConstruirDelg = pMetodoConstructor
    End Sub
#End Region

#Region "Propiedades"
    ''' <summary>Obtiene o asigna el nombre de la tabla.</summary>
    Public Property NombreTabla() As String
        Get
            Return (Me.mNombreTabla)
        End Get
        Set(ByVal Value As String)
            mNombreTabla = Value
        End Set
    End Property
#End Region

#Region "Metodos"
    ''' <summary>
    ''' Metodo que construye una entidad a partir de una Hashtable con los datos.
    ''' </summary>
    ''' <param name="pHTDatos" type="Hashtable">
    ''' Hashtable donde tenemos los datos de la entidad que queremos construir.
    ''' </param>
    ''' <returns>
    ''' La entidad de datos construida.
    ''' </returns>
    Public Overloads Overrides Function ConstruirEntidad(ByVal pHTDatos As Hashtable) As Object
        Return mConstruirDelg.Invoke(pHTDatos("Id"), pHTDatos("Nombre"))
    End Function

    ''' <summary>
    ''' Metodo que construye una coleccion de entidades a partir de una lista con los datos.
    ''' </summary>
    ''' <param name="pALDatos" type="IList">
    ''' Lista donde tenemos los datos de las entidades que queremos construir.
    ''' </param>
    ''' <returns>
    ''' 'TODO: QUE DEVUELVE ESTO??? HE PUESTO QUE DEVUELVA LA HASH Y HE AÑADIDO EL NEW!!!
    ''' </returns>
    Public Overloads Overrides Function ConstruirEntidad(ByVal pALDatos As IList) As Object
        Dim i As Int64
        Dim phtDatos As Hashtable

        phtDatos = New Hashtable
        For i = 0 To pALDatos.Count - 1
            phtDatos(i) = pALDatos.Item(i)
            pALDatos.Item(i) = mConstruirDelg.Invoke(phtDatos("Id"), phtDatos("Nombre"))
        Next

        Return phtDatos
    End Function

    ''' <summary>
    ''' Metodo que construye la sentencia sql para recuperar un tipo.
    ''' </summary>
    ''' <param name="pID" type="String">
    ''' ID del tipo que queremos recuperar.
    ''' </param>
    ''' <returns>La sentencia sql que recupera el tipo deseado.</returns>
    Public Overrides Function ConstSqlSelect(ByVal pID As String) As String
        Return "select Id, Nombre from " & mNombreTabla & " where id = " & pID
    End Function

    ''' <summary>
    ''' Metodo que construye la sentencia sql para insertar un tipo.
    ''' </summary>
    ''' <remarks>No esta implementado.</remarks>
    ''' <param name="pObjeto" type="Object">
    ''' Objeto que queremos insertar.
    ''' </param>
    ''' <param name="pParametros" type="ColIDataParameter">
    ''' Coleccion de parametros para la consulta.
    ''' </param>
    ''' <param name="pFechaModificacion" type="Date">
    ''' Fecha de modificacion de la entidad.
    ''' </param>
    ''' <returns>La sentencia sql que inserta el tipo deseado.</returns>
    Public Overrides Function ConstSqlInsert(ByVal pObjeto As Object, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date, ByRef psqlHistorica As String) As String
        Throw New NotImplementedException
    End Function

    ''' <summary>
    ''' Metodo que construye la sentencia sql para actualizar un tipo.
    ''' </summary>
    ''' <remarks>No esta implementado.</remarks>
    ''' <param name="pObjeto" type="Object">
    ''' Objeto que queremos actualizar.
    ''' </param>
    ''' <param name="pParametros" type="List(Of IDataParameter">
    ''' Coleccion de parametros para la consulta.
    ''' </param>
    ''' <param name="pFechaModificacion" type="Date">
    ''' Fecha de modificacion de la entidad.
    ''' </param>
    ''' <returns>La sentencia sql que actualiza el tipo deseado.</returns>
    Public Overrides Function ConstSqlUpdate(ByVal pObjeto As Object, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date, ByRef pSqlHistorica As String) As String
        Throw New NotImplementedException
    End Function

    ''' <summary>
    ''' Metodo que construye la sentencia sql para dar de baja un tipo.
    ''' </summary>
    ''' <remarks>No esta implementado.</remarks>
    ''' <param name="pObjeto" type="Object">
    ''' Objeto que queremos dar de baja.
    ''' </param>
    ''' <param name="pParametros" type="List(Of IDataParameter">
    ''' Coleccion de parametros para la consulta.
    ''' </param>
    ''' <param name="pFechaModificacion" type="Date">
    ''' Fecha de modificacion de la entidad.
    ''' </param>
    ''' <returns>La sentencia sql que da de baja el tipo deseado.</returns>
    Public Overrides Function ConstSqlBaja(ByVal pObjeto As Object, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date) As String
        Throw New NotImplementedException
    End Function
#End Region

End Class

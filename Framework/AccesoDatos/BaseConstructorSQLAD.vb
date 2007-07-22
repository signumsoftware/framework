#Region "Importaciones"

Imports System.Collections.Generic

#End Region

''' <summary>
''' Esta clase proporciona la funcionalidad minima necesaria para los constructores de sql y entidades.
''' </summary>
''' <remarks>
''' Esta clase proporciona la funcionalidad de transformar objetos IDataReader en arrays y tablas hash de datos
''' que luego cada constructor especifico podrá convertir en su entidad de datos correspondiente. Ademas, proporciona 
''' metodos que devuelven las consutas sql necesarias para trabajar con estas entidades.
''' </remarks>
Public MustInherit Class BaseConstructorSQLAD
    Implements IConstructorAD


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
    Public Overridable Function ConstruirDatos(ByVal pDR As IDataReader) As Object Implements IConstructorAD.ConstruirDatos
        Dim tabla As New Hashtable
        Dim lista As New ArrayList
        Dim posicion As Integer
        Dim alIDs As ArrayList
        Dim nombre As String
        Dim i As Integer

        'La primera sql ha de devolver los datos del objeto principal y solo debiera contener un registro
        If (pDR.Read()) Then
            'Rellenamos los valores para los campos "directos" de la entidad
            For i = 0 To pDR.FieldCount - 1
                tabla.Add(pDR.GetName(i), pDR.GetValue(i))
            Next
        End If

        'Si hay mas de una fila
        If (pDR.Read()) Then
            Return ConstruirDatosVarios(pDR, tabla)

        Else
            'El resto de los campos indirectos se devuelven en forma de arraylist de ids en el orden en el
            'que fueron encadenados en la cadena sql
            Do While (pDR.NextResult)
                posicion += 1
                alIDs = New ArrayList
                nombre = String.Empty

                Do While pDR.Read()
                    alIDs.Add(pDR.GetValue(0))
                    nombre = pDR.GetName(0)
                Loop

                If (Not nombre Is Nothing OrElse Not nombre = String.Empty) Then
                    tabla.Add(nombre, alIDs)
                End If
            Loop
        End If

        If (tabla.Count = 0) Then
            Return Nothing

        Else
            Return tabla
        End If
    End Function

    ''' <summary>
    ''' Esta funcion transforma las entidades de un IDataReader en Hashtables dentro de un ArrayList.
    ''' </summary>
    ''' <param name="pDR" type="IDataReader">
    ''' IDataReader del que vamos a sacar los datos.
    ''' </param>
    ''' <param name="pTablaPrevia" type="Hashtable">
    ''' Primer elemento de la lista.
    ''' </param>
    ''' <returns>
    ''' Una Hashtable si solo habia una entidad o un ArrayList si habia varias entidades.
    ''' </returns>
    Private Function ConstruirDatosVarios(ByVal pDR As IDataReader, ByVal pTablaPrevia As Hashtable) As Object
        Dim lista As ArrayList 'Representa una coleccion de entidades
        Dim tabla As Hashtable 'Representa una entidad
        Dim i As Integer

        'Leemos el datareader y cargamos los datos en un arraylist
        lista = New ArrayList
        lista.Add(pTablaPrevia)

        tabla = New Hashtable
        For i = 0 To pDR.FieldCount - 1
            tabla.Add(pDR.GetName(i), pDR.GetValue(i))
        Next

        lista.Add(tabla)
        tabla = New Hashtable

        Do While pDR.Read()
            For i = 0 To pDR.FieldCount - 1
                tabla.Add(pDR.GetName(i), pDR.GetValue(i))
            Next

            lista.Add(tabla)
            tabla = New Hashtable
        Loop

        'Seleccionamos el retorno adecuado
        Select Case lista.Count
            Case Is = 0
                Return Nothing

            Case Is = 1
                Return lista.Item(0)

            Case Is > 1
                Return lista
        End Select

        Return Nothing
    End Function

    ''' <summary>Este metodo construye una entidad de datos de negocio a partir de una hash de datos.</summary>
    ''' <param name="pHLDatos" type="Hashtable">
    ''' Hashtable con los datos de la entidad.
    ''' </param>
    ''' <returns>
    ''' La entidad de datos de negocio.
    ''' </returns>
    Public MustOverride Overloads Function ConstruirEntidad(ByVal pHLDatos As Hashtable) As Object Implements IConstructorAD.ConstruirEntidad

    ''' <summary>Este metodo construye una coleccion de entidades de datos de negocio a partir de un ArrayList de datos.</summary>
    ''' <param name="pALDatos" type="IList">
    ''' ArrayList con los datos de las entidades.
    ''' </param>
    ''' <returns>
    ''' La coleccion de entidades.
    ''' </returns>
    Public MustOverride Overloads Function ConstruirEntidad(ByVal pALDatos As IList) As Object Implements IConstructorAD.ConstruirEntidad

    ''' <summary>Este metodo construye una sql para seleccionar una entidad.</summary>
    ''' <param name="pID" type="String">
    ''' ID de la entidad que queremos seleccionar.
    ''' </param>
    ''' <returns>
    ''' La sql de seleccion.
    ''' </returns>
    Public MustOverride Function ConstSqlSelect(ByVal pID As String) As String Implements IConstructorAD.ConstSqlSelect

    ''' <summary>Este metodo construye una sql para insertar una entidad.</summary>
    ''' <param name="pObjeto" type="Object">
    ''' Entidad a insertar.
    ''' </param>
    ''' <param name="pParametros" type="ColIDataParameter">
    ''' Coleccion de parametros donde vamos a poner los campos de la entidad a insertar.
    ''' </param>
    ''' <param name="pFechaModificacion" type="DateTime">
    ''' Fecha de modificacion de la entidad (para control de accesos concurrentes).
    ''' </param>
    ''' <returns>
    ''' La sql de insercion.
    ''' </returns>
    Public MustOverride Function ConstSqlInsert(ByVal pObjeto As Object, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date, ByRef pSqlHistorica As String) As String Implements IConstructorAD.ConstSqlInsert

    ''' <summary>Este metodo construye una sql para actualizar una entidad.</summary>
    ''' <param name="pObjeto" type="Object">
    ''' Entidad a actualizar.
    ''' </param>
    ''' <param name="pParametros" type="ColIDataParameter">
    ''' Coleccion de parametros donde vamos a poner los campos de la entidad a actualizar.
    ''' </param>
    ''' <param name="pFechaModificacion" type="DateTime">
    ''' Fecha de modificacion de la entidad (para control de accesos concurrentes).
    ''' </param>
    ''' <returns>
    ''' La sql de actualizacion.
    ''' </returns>
    Public MustOverride Function ConstSqlUpdate(ByVal pObjeto As Object, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date, ByRef pSqlHistorica As String) As String Implements IConstructorAD.ConstSqlUpdate

    ''' <summary>Este metodo construye una sql para dar de baja una entidad.</summary>
    ''' <param name="pObjeto" type="Object">
    ''' Entidad a dar de baja.
    ''' </param>
    ''' <param name="pParametros" type="ColIDataParameter">
    ''' Coleccion de parametros donde vamos a poner los campos de la entidad a dar de baja.
    ''' </param>
    ''' <param name="pFechaModificacion" type="DateTime">
    ''' Fecha de modificacion de la entidad (para control de accesos concurrentes).
    ''' </param>
    ''' <returns>
    ''' La sql de baja.
    ''' </returns>
    Public MustOverride Function ConstSqlBaja(ByVal pObjeto As Object, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date) As String Implements IConstructorAD.ConstSqlBaja
#End Region

    Public Function ConstSqlSelectID(ByVal pID As String) As String Implements IConstructorAD.ConstSqlSelectID

    End Function
End Class

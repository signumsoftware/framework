#Region "Importaciones"

Imports System.Collections.Generic

#End Region

'TODO: ESTO QUE ES???
Public Delegate Function GuardarRelacionTR(ByVal pOjbeto As Object, ByRef pParametros As List(Of IDataParameter), ByVal pFechaModificacion As Date) As String

''' <summary>
''' Esta clase guarda la informacion de una transaccion y su recurso asociado y un constructor de objetos a
''' partir de sentencias sql.
''' </summary>
Public Class BaseTransaccionV2AD
    Inherits BaseTransaccionAD
    Implements IBaseAD

#Region "Atributos"
    'Constructor de objetos que se usa a la hora de trabajar con las entidades
    Protected mConstructor As IConstructorAD
#End Region

#Region "Constructores"
    ''' <summary>Constructor por defecto con parametros.</summary>
    ''' <param name="pTL" type="ITransaccionLogica">
    ''' ITransaccionLogica que vamos a guardar.
    ''' </param>
    ''' <param name="pRec" type="IRecurso">
    ''' IRecurso sobre el que se desarrolla la transaccion logica.
    ''' </param>
    ''' <param name="pConstructor" type="IConstructorAD">
    ''' IConstructorAD que se usa para trabajar con los objetos.
    ''' </param>
    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN, ByVal pConstructor As IConstructorAD)
        MyBase.New(pTL, pRec)

        If (pConstructor Is Nothing) Then
            Throw New ApplicationException("Error: el constructor no puede ser nulo.")
        End If

        mConstructor = pConstructor
    End Sub
#End Region

#Region "Metodos"
    ''' <summary>Metodo que recupera los datos de un objeto a partir de su ID.</summary>
    ''' <param name="pID" type="String">
    ''' ID del objeto que queremos recuperar.
    ''' </param>
    ''' <returns>
    ''' Una coleccion que contiene los datos de la columnas mas los id de las entidades relacionadas que pueden ser arraylist para las relaciones 1-*.
    ''' </returns>
    Public Overridable Function RecuperarDatos(ByVal pId As String) As ICollection Implements IBaseAD.RecuperarDatos
        Dim sql As String
        Dim ej As Ejecutor
        Dim datos As ICollection

        'Construimos la sql
        sql = mConstructor.ConstSqlSelect(pId)

        'Ejecutamos la sql para recuperar los datos
        ej = New Ejecutor(Me.mTL, Me.mRec)
        datos = ej.EjecutarRecuperarDatos(sql, mConstructor)

        Return datos
    End Function

    ''' <summary>Metodo que recupera los datos de un grupo de objetos a partir de una coleccion de IDs.</summary>
    ''' <remarks>No esta implementado.</remarks>
    ''' <param name="pColIDs" type="List(Of String)">
    ''' ID del objeto que queremos recuperar.
    ''' </param>
    ''' <returns>
    ''' Una coleccion que contiene los datos de la columnas mas los id de las entidades relacionadas que pueden ser arraylist para las relaciones 1-*.
    ''' </returns>
    Public Overridable Function RecuperarDatosVarios(ByVal pColIDs As List(Of String)) As ArrayList Implements IBaseAD.RecuperarDatosVarios
        Throw New NotImplementedException
    End Function

    ''' <summary>Metodo que inserta una entidad en la base de datos.</summary>
    ''' <param name="pEntidad" type="Object">
    ''' Objeto que vamos a guardar.
    ''' </param>
    ''' <returns>
    ''' El objeto modificado (despues de guardarse tiene asignado un ID).
    ''' </returns>
    Public Overridable Function Insertar(ByVal pEntidad As Object) As Object Implements IBaseAD.Insertar
        Dim sql As String
        Dim ej As Ejecutor
        Dim parametros As List(Of IDataParameter)
        Dim datoPer As IDatoPersistenteDN
        Dim resultado As String

        If (pEntidad Is Nothing) Then
            Throw New ApplicationException("Error: la instancia a insertar no puede ser nula.")
        End If

        'Construimos la sql
        parametros = New List(Of IDataParameter)
        Dim sqlh As String
        sql = mConstructor.ConstSqlInsert(pEntidad, parametros, FechaModificacionLN.ObtenerFechaModificacion(Me.mTL), sqlh)


        'Ejecutamos la sql para insertar los datos
        ej = New Ejecutor(Me.mTL, Me.mRec)
        resultado = ej.EjecutarEscalar(sql, parametros)


        If Not String.IsNullOrEmpty(sqlh) Then

            parametros.Item(parametros.Count - 1).Value = resultado ' poner el ide que en las tablas historicas no es autornimerico
            ej = New Ejecutor(Me.mTL, Me.mRec)
            ej.EjecutarEscalar(sqlh, parametros)

        End If



        'Comprobamos el resultado de la query

        If (resultado Is Nothing OrElse resultado = String.Empty) Then

            If Not TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pEntidad.GetType) Then
                Throw New ApplicationException("Error: hubo un error al insertar el elemento en la base de datos.")
            End If

        Else

            If Not Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pEntidad.GetType) Then
                pEntidad.ID = resultado
            End If

        End If




        'Modificamos el estado de la entidad
        If TypeOf pEntidad Is IDatoPersistenteDN Then
            datoPer = pEntidad
            datoPer.FechaModificacion = FechaModificacionLN.ObtenerFechaModificacion(mTL)
            datoPer.EstadoDatos = EstadoDatosDN.SinModificar
        End If




        Return pEntidad
    End Function

    ''' <summary>Metodo que modifica una entidad en la base de datos.</summary>
    ''' <param name="pEntidad" type="Object">
    ''' Objeto que vamos a modificar.
    ''' </param>
    ''' <returns>
    ''' El numero de filas modificadas en la BD.
    ''' </returns>
    Public Overridable Function Modificar(ByVal pEntidad As Object) As Integer Implements IBaseAD.Modificar
        Dim sql, sqlh As String
        Dim ej As Ejecutor
        Dim parametros As List(Of IDataParameter)
        Dim datoPer As IDatoPersistenteDN
        Dim registrosAfectados As Integer

        If (pEntidad Is Nothing) Then
            Throw New ApplicationException("Error: la instancia a insertar no puede ser nula.")
        End If

        'Construimos la sql
        parametros = New List(Of IDataParameter)

        sql = mConstructor.ConstSqlUpdate(pEntidad, parametros, FechaModificacionLN.ObtenerFechaModificacion(Me.mTL), sqlh)

        'Ejecutamos la sql para modificar los datos
        ej = New Ejecutor(Me.mTL, Me.mRec)
        registrosAfectados = ej.EjecutarNoConsulta(sql, parametros)

        'Comprobamos el resultado de la query
        If (registrosAfectados <> 1) Then
            'Throw New ApplicationException("Error: hubo un error al modificar el elemento en la base de datos.")
            Return registrosAfectados
        End If


        If Not String.IsNullOrEmpty(sqlh) Then
            ej = New Ejecutor(Me.mTL, Me.mRec)
            registrosAfectados = ej.EjecutarNoConsulta(sqlh, parametros)

            If (registrosAfectados <> 1) Then
                'Throw New ApplicationException("Error: hubo un error al modificar el elemento en la base de datos.")
                Return registrosAfectados
            End If
        End If





        'Modificamos el estado de la entidad
        datoPer = pEntidad
        datoPer.FechaModificacion = FechaModificacionLN.ObtenerFechaModificacion(mtl)
        datoPer.EstadoDatos = EstadoDatosDN.SinModificar

        Return registrosAfectados
    End Function

    ''' <summary>Metodo que elimina una entidad en la base de datos.</summary>
    ''' <param name="pID" type="String">
    ''' ID del objeto que vamos a eliminar.
    ''' </param>
    ''' <returns>
    ''' El numero de filas eliminadas en la BD.
    ''' </returns>
    Public Overridable Function Eliminar(ByVal pId As Integer) As Integer Implements IBaseAD.Eliminar
        Dim sql As String
        Dim ej As Ejecutor
        Dim parametros As List(Of IDataParameter)
        Dim resultado As Integer

        'Construimos la sql
        parametros = New List(Of IDataParameter)

        sql = mConstructor.ConstSqlBaja(pId, parametros, FechaModificacionLN.ObtenerFechaModificacion(Me.mTL))

        'Ejecutamos la sql para dar de baja los datos
        ej = New Ejecutor(Me.mTL, Me.mRec)
        resultado = ej.EjecutarNoConsulta(sql, parametros)

        Return resultado
    End Function

    'TODO: ESTO QUE ES???
    Public Overridable Function GuardarRelacion(ByVal pEntidad As Object, ByVal pMetodoGuardarRelacionTR As GuardarRelacionTR) As Int64 Implements IBaseAD.GuardarRelacion
        Dim sql As String
        Dim ej As Ejecutor
        Dim parametros As List(Of IDataParameter)
        Dim registrosAfectados As Integer

        If (pEntidad Is Nothing) Then
            Throw New ApplicationException("Error: la instancia a insertar no puede ser nula.")
        End If

        'Construimos la sql
        parametros = New List(Of IDataParameter)

        sql = pMetodoGuardarRelacionTR.Invoke(pEntidad, parametros, FechaModificacionLN.ObtenerFechaModificacion(Me.mTL))

        'Ejecutamos la sql para modificar los datos
        ej = New Ejecutor(Me.mTL, Me.mRec)
        registrosAfectados = ej.EjecutarNoConsulta(sql, parametros)

        Return registrosAfectados
    End Function
#End Region

End Class

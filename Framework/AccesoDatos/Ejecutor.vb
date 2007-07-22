#Region "Importaciones"

Imports System.Collections.Generic

#End Region

''' <summary>Esta clase permite ejecutar consultas de sql sobre un recurso dentro del marco de una transaccion.</summary>
''' <remarks>Las consultas pueden ir parametrizadas o no.</remarks>
Public Class Ejecutor

#Region "Atributos"
    'Transaccion logica en la que van embebidas las consultas
    Protected mTl As ITransaccionLogicaLN

    'Recurso sobre el que vamos a ejecutar las consultas
    Protected mRecurso As IRecursoLN
#End Region

#Region "Constructores"
    ''' <summary>Constructor por defecto que acepta los parametros iniciales.</summary>
    ''' <param name="pTl" type="ITransaccionLogica">
    ''' Transaccion logica dentro de la que vamos a ejecutar las consultas.
    ''' </param>
    ''' <param name="pRecurso" type="IRecurso">
    ''' Recurso sobre el que vamos a ejecutar las consultas.
    ''' </param>
    Public Sub New(ByVal pTl As ITransaccionLogicaLN, ByVal pRecurso As IRecursoLN)
        mRecurso = pRecurso
        mTl = pTl
    End Sub
#End Region

#Region "Metodos"
    ''' <summary>Metodo que encapsula una llamada del tipo ExecuteScalar.</summary>
    ''' <param name="pSql" type="String">
    ''' Consulta que vamos a ejecutar.
    ''' </param>
    Public Function EjecutarEscalar(ByVal pSql As String) As Object
        Return EjecutarEscalar(pSql, Nothing)
    End Function

    ''' <summary>Metodo que encapsula una llamada del tipo ExecuteScalar.</summary>
    ''' <param name="pSql" type="String">
    ''' Consulta que vamos a ejecutar
    ''' </param>
    ''' <param name="pParametros" type="ColIDataParameter">
    ''' Coleccion de parametros de la consulta.
    ''' </param>
    Public Function EjecutarEscalar(ByVal pSql As String, ByVal pParametros As List(Of IDataParameter)) As Object
        Dim cn As IDbConnection
        Dim tr As IDbTransaction = Nothing
        Dim cm As IDbCommand

        cn = GTDRGenericoBDAD.CrearInstancia(mRecurso).GetConexion(mTl)

        Try
            cn.Open()
            tr = cn.BeginTransaction
            cm = cn.CreateCommand

            'Si no estamos en una transaccion nos embebemos en ella
            If (cm.Transaction Is Nothing) Then
                cm.Transaction = tr
            End If

            cm.CommandText = pSql

            'Si hay parametros los añadimos
            If (Not pParametros Is Nothing) Then
                For Each param As IDataParameter In pParametros
                    cm.Parameters.Add(param)
                Next
            End If

            EjecutarEscalar = cm.ExecuteScalar()
            tr.Commit()

            cm.Parameters.Clear()

        Catch ex As Exception
            If (tr IsNot Nothing) Then
                tr.Rollback()
            End If

            Throw

        Finally
            If (Not cn Is Nothing) Then
                cn.Close()
            End If
        End Try
    End Function

    ''' <summary>Metodo que encapsula una llamada del tipo ExecuteNonQuery.</summary>
    ''' <param name="pSql" type="String">
    ''' Consulta que vamos a ejecutar.
    ''' </param>
    Public Function EjecutarNoConsulta(ByVal pSql As String) As Integer
        Return EjecutarNoConsulta(pSql, Nothing)
    End Function

    ''' <summary>Metodo que encapsula una llamada del tipo ExecuteNonQuery.</summary>
    ''' <param name="pSql" type="String">
    ''' Consulta que vamos a ejecutar
    ''' </param>
    ''' <param name="pParametros" type="ColIDataParameter">
    ''' Coleccion de parametros de la consulta.
    ''' </param>
    Public Function EjecutarNoConsulta(ByVal pSql As String, ByVal pParametros As List(Of IDataParameter)) As Integer

        Dim cn As IDbConnection
        Dim tr As IDbTransaction = Nothing
        Dim cm As IDbCommand

        cn = GTDRGenericoBDAD.CrearInstancia(mRecurso).GetConexion(mTl)

        Try
            cn.Open()
            tr = cn.BeginTransaction
            cm = cn.CreateCommand

            'Si no estamos en una transaccion nos embebemos en ella
            If (cm.Transaction Is Nothing) Then
                cm.Transaction = tr
            End If

            cm.CommandText = pSql

            'Si hay parametros los añadimos
            If (Not pParametros Is Nothing) Then
                For Each param As IDataParameter In pParametros
                    cm.Parameters.Add(param)
                Next
            End If

            EjecutarNoConsulta = cm.ExecuteNonQuery()
            tr.Commit()
            cm.Parameters.Clear()
            cm.Dispose()

        Catch ex As SqlClient.SqlException
            If Not ex.Number = 2714 Then
                Debug.Write(Not ex.Number)
                Debug.WriteLine("-->" & ex.Message)
            End If
            If (tr IsNot Nothing) Then
                Try
                    tr.Rollback()

                Catch RolBackex As SqlClient.SqlException
                    If RolBackex.Number = 3903 AndAlso ex.Number = 2714 Then
                        Throw ex
                    Else
                        Debug.Write(ex.Message)
                        Throw RolBackex
                    End If
                End Try
            End If

            Debug.WriteLine(pSql)
            Debug.WriteLine(ex.Message)

            Throw ex


        Finally
            If (Not cn Is Nothing) Then
                cn.Close()
            End If
        End Try
    End Function

    '''' <summary>
    '''' Metodo que encapsula una llamada del tipo ExecuteReader y construye un objeto con los datos devueltos.
    '''' </summary>
    '''' <param name="pSql" type="String">
    '''' Consulta que vamos a ejecutar.
    '''' </param>
    '''' <param name="pConstructor" type="IConstructor">
    '''' Constructor que vamos a usar para construir el objeto a partir del DataReader que leemos.
    '''' </param>
    'Public Function EjecutarDataReader(ByVal pSql As String, ByVal pConstructor As IConstructor) As Object
    '    Return EjecutarDataReader(pSql, Nothing, pConstructor)
    'End Function

    '''' <summary>
    '''' Metodo que encapsula una llamada del tipo ExecuteReader y construye un objeto con los datos devueltos.
    '''' </summary>
    '''' <param name="pSql" type="String">
    '''' Consulta que vamos a ejecutar.
    '''' </param>
    '''' <param name="pParametros" type="ColIDataParameter">
    '''' Coleccion de parametros de la consulta.
    '''' </param>
    '''' <param name="pConstructor" type="IConstructor">
    '''' Constructor que vamos a usar para construir el objeto a partir del DataReader que leemos.
    '''' </param>
    'Public Function EjecutarDataReader(ByVal pSql As String, ByVal pParametros As ColIDataParameter, ByVal pConstructor As IConstructor) As Object
    '    Dim cn As IDbConnection
    '    Dim tr As IDbTransaction
    '    Dim cm As IDbCommand
    '    Dim dr As IDataReader

    '    cn = GTDRGenericoBD.CrearInstancia(mRecurso).GetConexion(mTl)

    '    Try
    '        cn.Open()
    '        tr = cn.BeginTransaction
    '        cm = cn.CreateCommand

    '        'Si no estamos en una transaccion nos embebemos en ella
    '        If (cm.Transaction Is Nothing) Then
    '            cm.Transaction = tr
    '        End If

    '        cm.CommandText = pSql

    '        'Si hay parametros los añadimos
    '        If (Not pParametros Is Nothing) Then
    '            For Each param As IDataParameter In pParametros
    '                cm.Parameters.Add(param)
    '            Next
    '        End If

    '        dr = cm.ExecuteReader
    '        EjecutarDataReader = pConstructor.Construir(dr)

    '        tr.Commit()

    '    Catch ex As Exception
    '        If (Not tr Is Nothing) Then
    '            tr.Rollback()
    '        End If

    '        Throw ex

    '    Finally
    '        If (Not dr Is Nothing) Then
    '            dr.Close()
    '        End If

    '        If (Not cn Is Nothing) Then
    '            cn.Close()
    '        End If
    '    End Try
    'End Function

    ''' <summary>
    ''' Metodo que encapsula una llamada del tipo ExecuteReader y construye un objeto con los datos devueltos.
    ''' </summary>
    ''' <param name="pSql" type="String">
    ''' Consulta que vamos a ejecutar.
    ''' </param>
    ''' <param name="pConstructor" type="IConstructor">
    ''' Constructor que vamos a usar para construir el objeto a partir del DataReader que leemos.
    ''' </param>
    Public Function EjecutarRecuperarDatos(ByVal pSql As String, ByVal pConstructor As IConstructorAD) As Object
        Return EjecutarRecuperarDatos(pSql, Nothing, pConstructor)
    End Function

    ''' <summary>
    ''' Metodo que encapsula una llamada del tipo ExecuteReader y construye un objeto con los datos devueltos.
    ''' </summary>
    ''' <param name="pSql" type="String">
    ''' Consulta que vamos a ejecutar.
    ''' </param>
    ''' <param name="pParametros" type="ColIDataParameter">
    ''' Coleccion de parametros de la consulta.
    ''' </param>
    ''' <param name="pConstructor" type="IConstructor">
    ''' Constructor que vamos a usar para construir el objeto a partir del DataReader que leemos.
    ''' </param>
    Public Function EjecutarRecuperarDatos(ByVal pSql As String, ByVal pParametros As List(Of IDataParameter), ByVal pConstructor As IConstructorAD) As IEnumerable
        Dim cn As IDbConnection
        Dim tr As IDbTransaction = Nothing
        Dim cm As IDbCommand
        Dim dr As IDataReader = Nothing

        cn = GTDRGenericoBDAD.CrearInstancia(mRecurso).GetConexion(mTl)

        Try
            cn.Open()
            tr = cn.BeginTransaction
            cm = cn.CreateCommand

            'Si no estamos en una transaccion nos embebemos en ella
            If (cm.Transaction Is Nothing) Then
                cm.Transaction = tr
            End If

            cm.CommandText = pSql

            'Si hay parametros los añadimos
            If (Not pParametros Is Nothing) Then
                For Each param As IDataParameter In pParametros
                    cm.Parameters.Add(param)
                Next
            End If

            dr = cm.ExecuteReader
            EjecutarRecuperarDatos = pConstructor.ConstruirDatos(dr)

            tr.Commit()

        Catch ex As Exception
            If (tr IsNot Nothing) Then
                tr.Rollback()
            End If

            Throw

        Finally
            If (dr IsNot Nothing) Then
                dr.Close()
            End If

            If (Not cn Is Nothing) Then
                cn.Close()
            End If
        End Try
    End Function

    ''' <summary>
    ''' Metodo que devuelve un dataset con su informacion de esquema a partir de una consulta sql.
    ''' </summary>
    ''' <param name="pSql" type="String">
    ''' Consulta que vamos a ejecutar.
    ''' </param>
    ''' <returns>El dataset con la informacion de la consulta.</returns>
    Public Function EjecutarDataSet(ByVal pSql As String) As DataSet
        Return EjecutarDataSet(pSql, Nothing, True)
    End Function

    ''' <summary>
    ''' Metodo que devuelve un dataset con su informacion de esquema a partir de una consulta sql.
    ''' </summary>
    ''' <param name="pSql" type="String">
    ''' Consulta que vamos a ejecutar.
    ''' </param>
    ''' <param name="pParametros" type="ColIDataParameter">
    ''' Coleccion de parametros de la consulta.
    ''' </param>
    ''' <returns>El dataset con la informacion de la consulta.</returns>
    Public Function EjecutarDataSet(ByVal pSql As String, ByVal pParametros As List(Of IDataParameter)) As DataSet
        Return EjecutarDataSet(pSql, pParametros, True)
    End Function

    Public Function EjecutarDataSet(ByVal pSql As String, ByVal pParametros As List(Of IDataParameter), ByVal pUsarEsquema As Boolean) As DataSet
        Dim cn As IDbConnection
        Dim da As IDataAdapter
        Dim ds As DataSet
        Dim fact As IFactoriaAD
        Dim cm As IDbCommand
        Dim tr As IDbTransaction = Nothing

        cn = GTDRGenericoBDAD.CrearInstancia(mRecurso).GetConexion(mTl)

        Try
            fact = New FactoriaAD
            cn.Open()
            tr = cn.BeginTransaction
            cm = cn.CreateCommand

            'Si no estamos en una transaccion nos embebemos en ella
            If (cm.Transaction Is Nothing) Then
                cm.Transaction = tr
            End If

            cm.CommandText = pSql

            'Si hay parametros los añadimos
            If (Not pParametros Is Nothing) Then
                For Each param As IDataParameter In pParametros
                    cm.Parameters.Add(param)
                Next
            End If

            da = fact.GetAdaptador(Me.mRecurso, cm)

            ds = New DataSet

            If (pUsarEsquema = True) Then
                da.FillSchema(ds, SchemaType.Source)
            End If

            da.Fill(ds)

            tr.Commit()
        Catch ex As Exception
            If (tr IsNot Nothing) Then
                tr.Rollback()
            End If

            Throw ex
        Finally
            If (cn IsNot Nothing) Then
                cn.Close()
            End If
        End Try

        Return ds
    End Function


    'TODO: Estos dos métodos son muy especificos de Gilmar, no sería mejor para Gilmar heredar de esta clase y poner estos dos metodos en esa clase???
    Public Function EjecutarDataSet(ByVal nombreTabla As String, ByVal pSql As String, ByVal pParametros As List(Of IDataParameter), ByRef pDatos As DataSet, ByVal esquema As SchemaType, ByVal msa As MissingSchemaAction) As DataSet
        Dim cn As IDbConnection
        Dim da As IDataAdapter
        Dim fact As IFactoriaAD
        Dim cm As IDbCommand
        Dim tr As IDbTransaction = Nothing

        cn = GTDRGenericoBDAD.CrearInstancia(mRecurso).GetConexion(mTl)

        Try
            If pDatos Is Nothing Then
                pDatos = New DataSet
            End If

            fact = New FactoriaAD
            cn.Open()
            tr = cn.BeginTransaction
            cm = cn.CreateCommand

            If cm.Transaction Is Nothing Then
                cm.Transaction = tr
            End If

            cm.CommandText = pSql

            If (Not pParametros Is Nothing) Then
                For Each param As IDataParameter In pParametros
                    cm.Parameters.Add(param)
                Next
            End If

            da = fact.GetAdaptador(Me.mRecurso, cm)
            If msa = MissingSchemaAction.Add OrElse msa = MissingSchemaAction.AddWithKey OrElse msa = MissingSchemaAction.Error OrElse msa = MissingSchemaAction.Ignore Then
                da.MissingSchemaAction = msa
            End If


            If esquema = SchemaType.Mapped OrElse esquema = SchemaType.Source Then
                da.FillSchema(pDatos, esquema)
            End If

            da.TableMappings.Add("Table", nombreTabla)

            da.Fill(pDatos)

            tr.Commit()
        Catch ex As Exception
            If (tr IsNot Nothing) Then
                tr.Rollback()
            End If

            Throw ex
        Finally
            If Not cn Is Nothing Then
                cn.Close()
            End If
        End Try

        Return pDatos
    End Function

    Public Function EjecutarDataSet(ByVal pSql As String, ByVal pParametros As List(Of IDataParameter), ByRef pDatos As DataSet) As DataSet
        Return EjecutarDataSet(pSql, pParametros, pDatos, True)
    End Function

    Public Function EjecutarDataSet(ByVal pSql As String, ByVal pParametros As List(Of IDataParameter), ByRef pDatos As DataSet, ByVal pUsarEsquema As Boolean) As DataSet
        Dim cn As IDbConnection
        Dim da As IDataAdapter
        Dim fact As IFactoriaAD
        Dim cm As IDbCommand
        Dim tr As IDbTransaction = Nothing

        cn = GTDRGenericoBDAD.CrearInstancia(mRecurso).GetConexion(mTl)

        Try
            If pDatos Is Nothing Then
                pDatos = New DataSet
            End If

            fact = New FactoriaAD
            cn.Open()
            tr = cn.BeginTransaction
            cm = cn.CreateCommand

            If cm.Transaction Is Nothing Then
                cm.Transaction = tr
            End If

            cm.CommandText = pSql

            If (Not pParametros Is Nothing) Then
                For Each param As IDataParameter In pParametros
                    cm.Parameters.Add(param)
                Next
            End If

            da = fact.GetAdaptador(Me.mRecurso, cm)

            If (pUsarEsquema = True) Then
                da.FillSchema(pDatos, SchemaType.Source)
            End If

            da.Fill(pDatos)

            tr.Commit()

        Catch ex As Exception
            If (tr IsNot Nothing) Then
                tr.Rollback()
            End If

            Throw ex
        Finally
            If Not cn Is Nothing Then
                cn.Close()
            End If
        End Try

        Return pDatos
    End Function



    Public Function RecuperarEstructura(ByVal pSql As String) As DataTable
        Dim cn As IDbConnection
        Dim fact As IFactoriaAD
        Dim cm As IDbCommand
        Dim tr As IDbTransaction = Nothing
        Dim dr As IDataReader = Nothing

        cn = GTDRGenericoBDAD.CrearInstancia(mRecurso).GetConexion(mTl)

        Try


            fact = New FactoriaAD
            cn.Open()
            tr = cn.BeginTransaction
            cm = cn.CreateCommand

            If cm.Transaction Is Nothing Then
                cm.Transaction = tr
            End If

            cm.CommandText = pSql
            dr = cm.ExecuteReader
            RecuperarEstructura = dr.GetSchemaTable()

            tr.Commit()

        Catch ex As Exception
            If (tr IsNot Nothing) Then
                tr.Rollback()
            End If

            Throw ex
        Finally
            If Not dr Is Nothing Then
                dr.Close()
            End If
            If Not cn Is Nothing Then
                cn.Close()
            End If
        End Try

    End Function

#End Region

End Class




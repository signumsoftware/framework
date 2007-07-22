''' <summary>
''' Esta clase proporciona metodos que construyen parametros de sql a partir de valores.
''' </summary>
''' <remarks>
''' Se utiliza para las queries parametrizadas.
''' </remarks>
Public Class ParametrosConstAD

#Region "Metodos"
    ''' <summary>Este metodo construye un IDbDataParameter que almacena una fecha como un string.</summary>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="DateTime">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena la fecha introducida por valor.</returns>
    Public Shared Function ConstParametroFechaInformes(ByVal pNombreParametro As String, ByVal pValor As DateTime) As IDbDataParameter
        Dim miFechaFormateada As String

        ConstParametroFechaInformes = New SqlClient.SqlParameter
        ConstParametroFechaInformes.DbType = DbType.String
        ConstParametroFechaInformes.ParameterName = pNombreParametro

        If (pValor = Date.MinValue) Then
            ConstParametroFechaInformes.Value = System.DBNull.Value

        Else
            miFechaFormateada = Format(pValor, "MM/d/yyyy H:mm:ss")
            ConstParametroFechaInformes.Value = miFechaFormateada
        End If
    End Function

    ''' <summary>Este metodo construye un IDbDataParameter que almacena una fecha.</summary>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="DateTime">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena la fecha introducida por valor.</returns>
    Public Shared Function ConstParametroFecha(ByVal pNombreParametro As String, ByVal pValor As DateTime) As IDbDataParameter
        ConstParametroFecha = New SqlClient.SqlParameter
        ConstParametroFecha.DbType = DbType.DateTime
        ConstParametroFecha.ParameterName = pNombreParametro

        If (pValor = Date.MinValue) Then
            ConstParametroFecha.Value = System.DBNull.Value

        Else
            ConstParametroFecha.Value = pValor
        End If
    End Function

    ''' <summary>Este metodo modifica un IDbDataParameter para que almacene una fecha.</summary>
    ''' <param name="pParametro" type="IDbDataParameter">
    ''' Parametro que vamos a modificar.
    ''' </param>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="DateTime">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena la fecha introducida por valor.</returns>
    Public Shared Function ConstParametroFecha(ByVal pParametro As IDbDataParameter, ByVal pNombreParametro As String, ByVal pValor As DateTime) As IDbDataParameter
        pParametro.DbType = DbType.DateTime
        pParametro.ParameterName = pNombreParametro

        If (pValor = Date.MinValue) Then
            pParametro.Value = SqlTypes.SqlDateTime.Null

        Else
            pParametro.Value = pValor
        End If

        Return pParametro
    End Function

    ''' <summary>Este metodo construye un IDbDataParameter que almacena un string.</summary>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="String">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena el String introducido por valor.</returns>
    Public Shared Function ConstParametroString(ByVal pNombreParametro As String, ByVal pValor As String) As IDbDataParameter
        ConstParametroString = New SqlClient.SqlParameter
        ConstParametroString.DbType = DbType.String
        ConstParametroString.ParameterName = pNombreParametro

        If (pValor Is Nothing) Then
            ConstParametroString.Value = SqlTypes.SqlString.Null

        Else
            If (pValor = String.Empty) Then
                ConstParametroString.Value = SqlTypes.SqlString.Null

            Else
                ConstParametroString.Value = pValor
            End If
        End If
    End Function

    ''' <summary>Este metodo modifica un IDbDataParameter para que almacene un string.</summary>
    ''' <param name="pParametro" type="IDbDataParameter">
    ''' Parametro que vamos a modificar.
    ''' </param>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="String">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena el String introducido por valor.</returns>
    Public Shared Function ConstParametroString(ByVal pParametro As IDbDataParameter, ByVal pNombreParametro As String, ByVal pValor As String) As IDbDataParameter
        pParametro.DbType = DbType.String
        pParametro.ParameterName = pNombreParametro

        If (pValor Is Nothing) Then
            pParametro.Value = SqlTypes.SqlString.Null

        Else
            pParametro.Value = pValor
        End If

        Return pParametro
    End Function

    ''' <summary>Este metodo construye un IDbDataParameter que almacena un ID.</summary>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="String">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena el ID introducido por valor.</returns>
    Public Shared Function ConstParametroID(ByVal pNombreParametro As String, ByVal pValor As String) As IDbDataParameter
        ConstParametroID = New SqlClient.SqlParameter
        ConstParametroID.DbType = DbType.Int64
        ConstParametroID.ParameterName = pNombreParametro

        If (pValor Is Nothing OrElse pValor = String.Empty) Then
            ConstParametroID.Value = System.DBNull.Value

        Else
            ConstParametroID.Value = pValor
        End If
    End Function

    ''' <summary>Este metodo modifica un IDbDataParameter para que almacene un ID.</summary>
    ''' <param name="pParametro" type="IDbDataParameter">
    ''' Parametro que vamos a modificar.
    ''' </param>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="String">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena el ID introducido por valor.</returns>
    Public Shared Function ConstParametroID(ByVal pParametro As IDbDataParameter, ByVal pNombreParametro As String, ByVal pValor As String) As IDbDataParameter
        pParametro.DbType = DbType.Int64
        pParametro.ParameterName = pNombreParametro

        If (pValor Is Nothing OrElse pValor = String.Empty) Then
            pParametro.Value = System.DBNull.Value

        Else
            pParametro.Value = pValor
        End If

        Return pParametro
    End Function

    ''' <summary>Este metodo construye un IDbDataParameter que almacena un booleano.</summary>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="Boolean">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena el booleano introducido por valor.</returns>
    Public Shared Function ConstParametroBoolean(ByVal pNombreParametro As String, ByVal pValor As Boolean) As IDbDataParameter
        ConstParametroBoolean = New SqlClient.SqlParameter
        ConstParametroBoolean.DbType = DbType.Boolean
        ConstParametroBoolean.ParameterName = pNombreParametro
        ConstParametroBoolean.Value = pValor
    End Function

    ''' <summary>Este metodo modifica un IDbDataParameter para que almacene un booleano.</summary>
    ''' <param name="pParametro" type="IDbDataParameter">
    ''' Parametro que vamos a modificar.
    ''' </param>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="Boolean">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena el booleano introducido por valor.</returns>
    Public Shared Function ConstParametroBoolean(ByVal pParametro As IDbDataParameter, ByVal pNombreParametro As String, ByVal pValor As Boolean) As IDbDataParameter
        pParametro.DbType = DbType.Boolean
        pParametro.ParameterName = pNombreParametro
        pParametro.Value = pValor

        Return pParametro
    End Function

    ''' <summary>Este metodo construye un IDbDataParameter que almacena un double.</summary>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="Double">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena el double introducido por valor.</returns>
    Public Shared Function ConstParametroDouble(ByVal pNombreParametro As String, ByVal pValor As Double) As IDbDataParameter
        ConstParametroDouble = New SqlClient.SqlParameter
        ConstParametroDouble.DbType = DbType.Double
        ConstParametroDouble.ParameterName = pNombreParametro
        ConstParametroDouble.Value = pValor
    End Function

    ''' <summary>Este metodo modifica un IDbDataParameter para que almacene un double.</summary>
    ''' <param name="pParametro" type="IDbDataParameter">
    ''' Parametro que vamos a modificar.
    ''' </param>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="Double">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena el double introducido por valor.</returns>
    Public Shared Function ConstParametroDouble(ByVal pParametro As IDbDataParameter, ByVal pNombreParametro As String, ByVal pValor As Double) As IDbDataParameter
        pParametro.DbType = DbType.Double
        pParametro.ParameterName = pNombreParametro
        pParametro.Value = pValor

        Return pParametro
    End Function

    ''' <summary>Este metodo construye un IDbDataParameter que almacena un entero.</summary>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="Integer">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena el entero introducido por valor.</returns>
    Public Shared Function ConstParametroInteger(ByVal pNombreParametro As String, ByVal pValor As Int64) As IDbDataParameter
        ConstParametroInteger = New SqlClient.SqlParameter
        ConstParametroInteger.DbType = DbType.Double
        ConstParametroInteger.ParameterName = pNombreParametro
        ConstParametroInteger.Value = pValor
    End Function

    ''' <summary>Este metodo construye un IDbDataParameter que almacena un entero sin signo.</summary>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="Integer">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena el entero introducido por valor.</returns>
    Public Shared Function ConstParametroUInteger(ByVal pNombreParametro As String, ByVal pValor As UInt64) As IDbDataParameter
        ConstParametroUInteger = New SqlClient.SqlParameter
        ConstParametroUInteger.DbType = DbType.Double
        ConstParametroUInteger.ParameterName = pNombreParametro
        ConstParametroUInteger.Value = pValor
    End Function

    ''' <summary>Este metodo modifica un IDbDataParameter para que almacene un entero.</summary>
    ''' <param name="pParametro" type="IDbDataParameter">
    ''' Parametro que vamos a modificar.
    ''' </param>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="Integer">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena el entero introducido por valor.</returns>
    Public Shared Function ConstParametroInteger(ByVal pParametro As IDbDataParameter, ByVal pNombreParametro As String, ByVal pValor As Integer) As IDbDataParameter
        pParametro.DbType = DbType.Double
        pParametro.ParameterName = pNombreParametro
        pParametro.Value = pValor

        Return pParametro
    End Function

    ''' <summary>Este metodo construye un IDbDataParameter que almacena un array de bytes.</summary>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="Byte()">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena el array de bytes introducido por valor.</returns>
    Public Shared Function ConstParametroArrayBytes(ByVal pNombreParametro As String, ByVal pValor As Byte()) As IDbDataParameter
        Dim parametro As IDbDataParameter

        parametro = New SqlClient.SqlParameter

        parametro.DbType = DbType.Binary
        parametro.ParameterName = pNombreParametro
        parametro.Value = pValor

        Return parametro
    End Function

    ''' <summary>Este metodo modifica un IDbDataParameter para que almacene un array de bytes.</summary>
    ''' <param name="pParametro" type="IDbDataParameter">
    ''' Parametro que vamos a modificar.
    ''' </param>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre que vamos a asignar al parametro.
    ''' </param>
    ''' <param name="pValor" type="Integer">
    ''' Valor que vamos a guardar en el parametro.
    ''' </param>
    ''' <returns>El parametro que almacena el array de bytes introducido por valor.</returns>
    Public Shared Function ConstParametroArrayBytes(ByVal pParametro As IDbDataParameter, ByVal pNombreParametro As String, ByVal pValor As Byte()) As IDbDataParameter
        pParametro.DbType = DbType.Binary
        pParametro.ParameterName = pNombreParametro
        pParametro.Value = pValor

        Return pParametro
    End Function
#End Region

End Class

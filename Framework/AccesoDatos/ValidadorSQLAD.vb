''' <summary>Esta clase se encarga de validar y formatear de forma correcta sentencias SQL.</summary>
Public Class ValidadorSQL

#Region "Metodos"
    ''' <summary>Este metodo valida una fecha y construye un parametro con ella.</summary>
    ''' <param name="pNombreParametro" type="String">
    ''' Nombre del parametro que vamos a crear.
    ''' </param>
    ''' <param name="pValor" type="DateTime">
    ''' Fecha que vamos a validar.
    ''' </param>
    ''' <returns>El parametro que contiene la fecha formateada de forma correcta.</returns>
    Public Shared Function ValidarSQLFecha(ByVal pNombreParametro As String, ByVal pValor As DateTime) As IDbDataParameter
        ValidarSQLFecha = New SqlClient.SqlParameter
        ValidarSQLFecha.DbType = DbType.DateTime
        ValidarSQLFecha.ParameterName = pNombreParametro

        If (pValor = Date.MinValue) Then
            ValidarSQLFecha.Value = SqlTypes.SqlDateTime.Null

        Else
            ValidarSQLFecha.Value = pValor
        End If
    End Function

    ''' <summary>Este metodo valida si un objeto es un ID correcto.</summary>
    ''' <param name="pCampo" type="Object">
    ''' Campo que vamos a validar.
    ''' </param>
    ''' <returns>Un String con el ID si el objeto era correcto o -1 en caso contrario.</returns>
    Shared Function CampoOpcionalID(ByVal pCampo As Object) As String
        If (IsNumeric(pCampo) AndAlso pCampo >= 1) Then
            Return pCampo
        End If

        If (pCampo Is Nothing OrElse pCampo Is System.DBNull.Value OrElse pCampo = String.Empty) Then
            Return -1

        Else
            Return pCampo
        End If
    End Function

    ''' <summary>Este metodo valida si un objeto es un String.</summary>
    ''' <param name="pCampo" type="Object">
    ''' Campo que vamos a validar.
    ''' </param>
    ''' <returns>Un String vacio si el campo era nulo, nada en caso contrario.</returns>
    Shared Function CampoOpcionalString(ByVal pCampo As Object) As String
        If (pCampo Is Nothing) Then
            Return String.Empty
        End If

        Return pCampo.ToString
    End Function

    ''' <summary>Este metodo valida si un objeto es una fecha.</summary>
    ''' <param name="pCampo" type="Object">
    ''' Campo que vamos a validar.
    ''' </param>
    ''' <returns>El String "Null" si el campo era nulo, nada en caso contrario.</returns>
    Shared Function CampoOpcionalFecha(ByVal pCampo As Object) As String
        If (pCampo Is Nothing) Then
            Return "Null"
        End If

        Return pCampo.ToString
    End Function

    ''' <summary>Este metodo formatea un String.</summary>
    ''' <param name="pCampo" type="String">
    ''' String donde vamos a reemplazar las comillas simples por comillas dobles.
    ''' </param>
    ''' <returns>Devuelve el String formateado.</returns>
    Shared Function ValidarSQL(ByVal pCampo As String) As String
        Return Replace(pCampo, "'", "''")
    End Function

    ''' <summary>Este metodo formatea un Double.</summary>
    ''' <param name="pCampo" type="Double">
    ''' Double donde vamos a eliminar el caracter ';' y a reemplazar el caracter ',' por '.'.
    ''' </param>
    ''' <returns>Un String con el Double original formateado.</returns>
    Shared Function ValidarSQLN(ByVal pCampo As Double) As String
        'nos pasan un número y lo devolvemos formateado
        Dim cmp As String
        cmp = Replace(pCampo.ToString, ";", "")

        Return Replace(cmp, ",", ".")
    End Function
#End Region

End Class

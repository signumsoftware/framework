'TODO: ESTO SE USA???
Public Class ParametrosRecuAD

#Region "Metodos"
    Public Shared Function RecuParamFecha(ByVal pFecha As Object) As Date
        If (IsDBNull(pFecha)) Then
            Return Date.MinValue

        Else
            Return pFecha
        End If
    End Function

    Public Shared Function RecuParamCadena(ByVal pCadena As Object) As String
        If (IsDBNull(pCadena)) Then
            Return String.Empty

        Else
            Return pCadena
        End If
    End Function

    Public Shared Function RecuParamNumero(ByVal pNumero As Object) As Int64
        If (IsDBNull(pNumero)) Then
            Return 0

        Else
            Return pNumero
        End If
    End Function
#End Region

End Class

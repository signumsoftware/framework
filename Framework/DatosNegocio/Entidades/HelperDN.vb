Public Class HelperDN
    ''' <summary>
    ''' recive un tipo que es una operacion
    ''' </summary>
    ''' <param name="ttipo"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function EnumToArraylist(ByVal ttipo As System.Type) As ArrayList
        Dim arrl As ArrayList
        Try
            arrl = New ArrayList
            arrl.AddRange([Enum].GetValues(ttipo))
            Return arrl
        Catch ex As Exception
            Throw
        End Try

    End Function

End Class

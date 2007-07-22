Imports System.Text.RegularExpressions


Public Class ExceptionHelper

    Public Shared Function ConversorExcepcionSoap(ByVal excepcion As Web.Services.Protocols.SoapException) As String
        'nos pasan un error de tipo SoapException y devolvemos el texto inteligible
        Dim st As String
        Dim st2 As String
        Try

            If excepcion.Message.ToLower.Contains("--->") Then
                st = excepcion.Message
                st = Microsoft.VisualBasic.Right(st, st.Length - (st.ToLower.IndexOf("--->") + 4))
                If st.ToLower.Contains("exception:") Then
                    st = Microsoft.VisualBasic.Right(st, st.Length - (st.ToLower.IndexOf("exception:") + 10))
                End If
                If st.Contains(Chr(10)) Then
                    st = Microsoft.VisualBasic.Left(st, st.IndexOf(Chr(10)))
                End If
                Return EscapearCaracteresWeb(st)
            End If

            Dim elemento As String = String.Empty
            If excepcion.Message.IndexOf(" at ") <> -1 Then
                elemento = " at"
            ElseIf excepcion.Message.IndexOf(" en ") <> -1 Then
                elemento = " en"
            End If

            If InStr(excepcion.Message.ToLower, "soapexception:") <> 0 Then
                st = Mid(excepcion.Message, InStr(excepcion.Message.ToLower, "soapexception:"))
                st2 = Mid(st, InStr(st, elemento))
                st = Left(st, Len(st) - Len(st2))
                Return EscapearCaracteresWeb(st)
            Else
                If InStr(excepcion.Message.ToLower, "error") <> 0 AndAlso excepcion.Message.ToLower.IndexOf(" at") <> -1 Then
                    st = Mid(excepcion.Message, InStr(excepcion.Message.ToLower, "error"))
                    st2 = Mid(st, InStr(st, elemento))
                    st = Left(st, Len(st) - Len(st2))
                    Return EscapearCaracteresWeb(st)
                End If
            End If


            Return EscapearCaracteresWeb(excepcion.Message)

        Catch ex As Exception
            Return EscapearCaracteresWeb(excepcion.Message) & Chr(13) & ex.Message
        End Try

    End Function

    Public Shared Function EscapearCaracteresWeb(ByVal pstring As String) As String
        Dim micadena As String
        Dim micodeascii As Int16
        Dim micadenanueva As String = ""


        micadena = pstring

        Do While micadena.IndexOf("&#") <> -1
            'ponemos todo lo anterior en la cadena de salida
            micadenanueva += micadena.Substring(0, micadena.IndexOf("&#"))

            'quitamos de la cadena que usamos todo lo anterior
            micadena = micadena.Substring(micadena.IndexOf("&#"), micadena.Length - micadena.IndexOf("&#"))

            'vemos hasta dónde hay que escapear
            Dim miposfinal As Int16 = micadena.IndexOf(";") + 1
            Dim micaracteraescapear As String = micadena.Substring(0, miposfinal)

            'sustituimos el texto por el chr(ascii) correspondiente
            'y lo añadimos al texto final
            micodeascii = CInt(Replace(Replace(micaracteraescapear, "&#", ""), ";", ""))
            micadenanueva += Chr(micodeascii) 'lo convertimos en texto usando el código ascii

            'eliminamos el texto que ya hemos procesado de la cadena con la
            'que trabajamos
            micadena = micadena.Substring((micadena.IndexOf(";") + 1), (micadena.Length - micaracteraescapear.Length))
        Loop

        'si queda algo después del procesado, lo añadimos
        micadenanueva += micadena

        Return micadenanueva

    End Function

End Class



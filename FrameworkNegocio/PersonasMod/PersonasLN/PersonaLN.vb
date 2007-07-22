Imports Framework.LogicaNegocios.Transacciones
Imports FN.Personas.AD
Imports FN.Personas.DN

Public Class PersonaLN

    Public Function RecuperarPersonaFiscalxNIF(ByVal codigoNif As String) As PersonaFiscalDN
        Dim personaAD As PersonaAD

        Using tr As New Transaccion()

            personaAD = New PersonaAD()
            RecuperarPersonaFiscalxNIF = personaAD.RecuperarPersonaFiscalxNIF(codigoNif)

            tr.Confirmar()

        End Using
    End Function

End Class

Imports FN.Personas.DN

Public Class RelacionPropiedadFiscalDN
    Inherits RelacionPropiedadEmpresaDN


    Public Shared Function ValEntidadPoseedora(ByRef mensaje As String, ByVal value As FN.Localizaciones.DN.IEntidadFiscalDN) As Boolean
        If Not value.Correcta Then
            mensaje = "la entidad fiscal id " & value.ID & "-" & value.Nombre & ", no tine un numero de identificacion correcto " & value.IdentificacionFiscal.Codigo
        End If

        Return value.Correcta
    End Function
End Class

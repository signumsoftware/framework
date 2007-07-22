Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica
Imports Framework.Usuarios.DN


Public Class LocalizacionesFS
    Inherits BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal tl As ITransaccionLogicaLN, ByVal rec As IRecursoLN)
        MyBase.New(tl, rec)
    End Sub

#End Region

    Public Function RecuperarLocalidadPorCodigoPostal(ByVal pCodigoPostal As String, ByVal pIdSession As String, ByVal pActor As PrincipalDN) As FN.Localizaciones.DN.ColLocalidadDN
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)
                Dim mln As New FN.Localizaciones.LN.LocalizacionesLN
                RecuperarLocalidadPorCodigoPostal = mln.RecuperarLocalidadporCodigoPostal(pCodigoPostal)
                mfh.SalidaMetodo(pIdSession, pActor, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try
        End Using

    End Function



End Class

Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica
Imports Framework.Usuarios.DN.PrincipalDN

Public Class PolizasFS
    Inherits BaseFachadaFL

    Public Sub New(ByVal tl As ITransaccionLogicaLN, ByVal rec As IRecursoLN)
        MyBase.New(tl, rec)
    End Sub

    Public Function RecuperarCrearTomador(ByVal pIdSession As String, ByVal pActor As Framework.Usuarios.DN.PrincipalDN, ByVal cifNif As String) As FN.Seguros.Polizas.DN.TomadorDN
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)
                Dim mln As New FN.Seguros.Polizas.LN.PolizaLN
                RecuperarCrearTomador = mln.RecuperarCrearTomador(cifNif)
                mfh.SalidaMetodo(pIdSession, pActor, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try
        End Using

    End Function
End Class

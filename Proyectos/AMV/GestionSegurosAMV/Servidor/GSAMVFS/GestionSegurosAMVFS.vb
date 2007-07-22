Imports Framework.DatosNegocio
Imports Framework.DatosNegocio.Localizaciones.Temporales
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica
Imports Framework.Usuarios.DN

Public Class GestionSegurosAMVFS
    Inherits BaseFachadaFL

    Public Sub New(ByVal tl As ITransaccionLogicaLN, ByVal rec As IRecursoLN)
        MyBase.New(tl, rec)
    End Sub

    Public Function GenerarPresupuestoxCuestionarioRes(ByVal cuestionarioR As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal pIdSession As String, ByVal pActor As PrincipalDN) As FN.Seguros.Polizas.DN.PresupuestoDN
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)
                Dim mln As New GSAMV.LN.AdaptadorCuestionarioLN()
                GenerarPresupuestoxCuestionarioRes = mln.GenerarPresupuestoxCuestionarioRes(cuestionarioR)
                mfh.SalidaMetodo(pIdSession, pActor, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try

        End Using

    End Function

    Public Function GenerarTarifaxCuestionarioRes(ByVal cuestionarioR As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal tiempoTarificado As AnyosMesesDias, ByVal pIdSession As String, ByVal pActor As PrincipalDN) As FN.Seguros.Polizas.DN.TarifaDN
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)
                Dim mln As New GSAMV.LN.AdaptadorCuestionarioLN()
                GenerarTarifaxCuestionarioRes = mln.GenerarTarifaxCuestionarioRes(cuestionarioR, tiempoTarificado)
                mfh.SalidaMetodo(pIdSession, pActor, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try

        End Using

    End Function

End Class

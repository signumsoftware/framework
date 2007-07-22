Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica
Imports Framework.Usuarios.DN
Imports Framework.Cuestionario.CuestionarioDN

Imports FN.Seguros.Polizas.DN
Imports FN.RiesgosVehiculos.DN

Public Class RiesgosVehículosFS
    Inherits BaseFachadaFL

    Public Sub New(ByVal tl As ITransaccionLogicaLN, ByVal rec As IRecursoLN)
        MyBase.New(tl, rec)
    End Sub




    Public Function RecuperarRiesgoMotor(ByVal pMatricula As String, ByVal pNumeroBastidor As String, ByVal pIdSession As String, ByVal pActor As PrincipalDN) As FN.RiesgosVehiculos.DN.RiesgoMotorDN
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)

                Dim mln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()
                RecuperarRiesgoMotor = mln.RecuperarRiesgoMotorActivo(pMatricula, pNumeroBastidor)
                mfh.SalidaMetodo(pIdSession, pActor, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try
        End Using
    End Function

    Public Function RecuperarModelosPorMarca(ByVal pMarca As FN.RiesgosVehiculos.DN.MarcaDN, ByVal pIdSession As String, ByVal pActor As PrincipalDN) As List(Of FN.RiesgosVehiculos.DN.ModeloDN)
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)
                Dim mln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()
                RecuperarModelosPorMarca = mln.RecuperarModelosPorMarca(pMarca)
                mfh.SalidaMetodo(pIdSession, pActor, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try
        End Using

    End Function

    Public Function ExisteModeloDatos(ByVal nombreModelo As String, ByVal nombreMarca As String, ByVal estadoMatriculacion As Boolean, ByVal fecha As Date, ByVal pIdSession As String, ByVal pActor As PrincipalDN) As Boolean
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)
                Dim mln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()

                ExisteModeloDatos = (mln.RecuperarModeloDatos(nombreModelo, nombreMarca, estadoMatriculacion, fecha) IsNot Nothing)

                mfh.SalidaMetodo(pIdSession, pActor, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try
        End Using

    End Function

    Public Sub CargarGrafoTarificacion(ByVal pIdSession As String, ByVal pActor As PrincipalDN)
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)
                Dim mln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()
                mln.CargarGrafoTarificacion()
                mfh.SalidaMetodo(pIdSession, Nothing, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Sub
    Public Sub DesCargarGrafoTarificacion(ByVal pIdSession As String, ByVal pActor As PrincipalDN)
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)
                Dim mln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()
                mln.DesCargarGrafoTarificacion()
                mfh.SalidaMetodo(pIdSession, Nothing, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Sub
    Public Function VerificarDatosPresupuesto(ByVal presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN, ByVal pIdSession As String, ByVal pActor As PrincipalDN) As FN.Seguros.Polizas.DN.PresupuestoDN
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)
                Dim mln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN
                VerificarDatosPresupuesto = mln.VerificarDatosPresupuesto(presupuesto)
                mfh.SalidaMetodo(pIdSession, Nothing, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function

    Public Function TarificarPresupuesto(ByVal presupuesto As PresupuestoDN, ByVal pIdSession As String, ByVal pActor As PrincipalDN) As FN.Seguros.Polizas.DN.PresupuestoDN
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)
                Dim mln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()
                TarificarPresupuesto = mln.TarificarPresupuesto(presupuesto)
                mfh.SalidaMetodo(pIdSession, Nothing, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function

    Public Function TarificarTarifa(ByVal pTarifa As TarifaDN, ByVal pIdSession As String, ByVal pActor As PrincipalDN) As TarifaDN
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)
                Dim mln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()
                TarificarTarifa = mln.TarificarTarifa(pTarifa, Nothing, Nothing, True)
                mfh.SalidaMetodo(pIdSession, Nothing, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function


    Public Sub ModificarPoliza(ByVal periodoR As PeriodoRenovacionPolizaDN, ByVal tarifa As TarifaDN, ByVal cuestionarioR As CuestionarioResueltoDN, ByVal fechaInicioPC As Date, ByVal pIdSession As String, ByVal pActor As PrincipalDN)
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)
                Dim mln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizasOperLN
                mln.ModificarPoliza(periodoR, tarifa, cuestionarioR, fechaInicioPC)
                mfh.SalidaMetodo(pIdSession, Nothing, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Sub

    Public Function RecuperarModeloDatos(ByVal nombreModelo As String, ByVal nombreMarca As String, ByVal matriculada As Boolean, ByVal fecha As Date, ByVal pIdSession As String, ByVal pActor As PrincipalDN) As ModeloDatosDN
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)
                Dim mln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()
                RecuperarModeloDatos = mln.RecuperarModeloDatos(nombreModelo, nombreMarca, matriculada, fecha)
                mfh.SalidaMetodo(pIdSession, Nothing, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function

    Public Function RecuperarProductosModelo(ByVal modelo As ModeloDN, ByVal matriculada As Boolean, ByVal fecha As Date, ByVal pIdSession As String, ByVal pActor As PrincipalDN) As ColProductoDN
        Using New CajonHiloLN(mRec)
            Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

            Try
                mfh.EntradaMetodo(pIdSession, pActor, mRec)
                Dim mln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()
                RecuperarProductosModelo = mln.RecuperarProductosModelo(modelo, matriculada, fecha)
                mfh.SalidaMetodo(pIdSession, Nothing, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(pIdSession, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function

End Class

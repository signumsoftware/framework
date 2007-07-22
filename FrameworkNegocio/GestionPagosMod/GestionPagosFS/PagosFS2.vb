Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica
Imports Framework.Usuarios.DN
'Imports fn.Ficheros.FicherosDN


Public Class PagosFS2
    Inherits Framework.FachadaLogica.BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region

#Region "métodos"


    Public Function CargarAgrupacionID(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pAgrupApunteImpD As FN.GestionPagos.DN.AgrupApunteImpDDN) As FN.GestionPagos.DN.AgrupApunteImpDDN

        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim ml As New FN.GestionPagos.LN.AgrupApunteImpDLN
                CargarAgrupacionID = ml.CargarAgrupacionID(pAgrupApunteImpD)

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using


    End Function

    Public Function CrearAgrupacionID(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal param As FN.GestionPagos.DN.ParEntFiscalGenericaParamDN) As FN.GestionPagos.DN.ParEntFiscalGenericaParamDN

        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim ml As New FN.GestionPagos.LN.AgrupApunteImpDLN
                ' CrearAgrupacionID = ml.CrearAgrupacionID(param)

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using


    End Function



    Public Function BuscarImportesDebidosLibres(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal param As FN.GestionPagos.DN.ParEntFiscalGenericaParamDN) As DataSet





        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim ml As New FN.GestionPagos.LN.AgrupApunteImpDLN
                BuscarImportesDebidosLibres = ml.BuscarImportesDebidosLibres(param)

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using


    End Function

    Public Function CompensarPago(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pPagoCompensador As DN.PagoDN, ByRef colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN) As DN.PagoDN

        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                CompensarPago = ml.CompensarPago(pPagoCompensador, colLiqPago)

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function

    Public Function AnularPago(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pPago As DN.PagoDN) As DN.PagoDN

        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                ml.AnularPago(pPago)


                AnularPago = pPago

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function

    Public Function LiquidarPago(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pPago As DN.PagoDN) As DN.PagoDN

        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                ml.LiquidarPago(pPago)


                LiquidarPago = pPago

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function
    Public Function EfectuarPago(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pPago As DN.PagoDN) As DN.PagoDN

        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                ml.EfectuarPago(pPago)


                EfectuarPago = pPago

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function

    Public Function EfectuarYLiquidar(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pPago As DN.PagoDN) As DN.ColLiquidacionPagoDN

        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                EfectuarYLiquidar = ml.EfectuarYLiquidar(pPago)

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function

    Public Function AnularPagosNoEmitidosYCrearPagoAgrupador(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pPagoAgrupador As DN.PagoDN) As DN.PagoDN

        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                AnularPagosNoEmitidosYCrearPagoAgrupador = ml.AnularPagosNoEmitidosYCrearPagoAgrupador(pPagoAgrupador)

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function



    Public Function CrearPagoAgrupadorProvisional(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pPago As DN.PagoDN) As DN.PagoDN

        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                CrearPagoAgrupadorProvisional = ml.CrearPagoAgrupadorProvisional(pPago)

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function

#End Region

End Class

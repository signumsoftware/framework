Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica
Imports Framework.Usuarios.DN
'Imports fn.Ficheros.FicherosDN


Public Class PagosFS
    Inherits Framework.FachadaLogica.BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region

#Region "métodos"
    Public Function CargarPagos(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pDts As FN.GestionPagos.DN.dtsGestionPagos, ByVal tipoOrigen As FN.GestionPagos.DN.TipoEntidadOrigenDN, ByVal operacion As Framework.Procesos.ProcesosDN.OperacionDN) As Data.DataSet
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim miln As New FN.GestionPagos.LN.PagosLN()


                CargarPagos = miln.CargarPagos(pDts, pActor, tipoOrigen, operacion)

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function

    Public Function AltaModificacionProveedores(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pDts As FN.GestionPagos.DN.dtsGestionPagos) As Data.DataSet
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim miln As New FN.GestionPagos.LN.PagosLN()


                AltaModificacionProveedores = miln.AltaModificacionProveedores(pDts)

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function

    Public Function GuardarConfiguracionImpresion(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pConfigImp As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN) As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim miln As New FN.GestionPagos.LN.PagosLN()


                GuardarConfiguracionImpresion = miln.GuardarConfiguracionImpresionTalon(pConfigImp)

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using
    End Function

    Public Function GuardarTalonDoc(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pTdoc As FN.GestionPagos.DN.TalonDocumentoDN) As FN.GestionPagos.DN.TalonDocumentoDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim miln As New FN.GestionPagos.LN.PagosLN()

                GuardarTalonDoc = miln.GuardarTalonDoc(pTdoc)

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using

    End Function

    Public Function GuardarPlantillaCarta(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pPlantillaCarta As FN.GestionPagos.DN.PlantillaCartaDN) As FN.GestionPagos.DN.PlantillaCartaDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim miln As New FN.GestionPagos.LN.PagosLN()

                GuardarPlantillaCarta = miln.GuardarPlantillaCarta(pPlantillaCarta)

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using

    End Function

    Public Function GuardarTalonDN(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pTalonDN As FN.GestionPagos.DN.TalonDN) As FN.GestionPagos.DN.TalonDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim miln As New FN.GestionPagos.LN.PagosLN()

                GuardarTalonDN = miln.GuardarTalonDN(pTalonDN)

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try

        End Using

    End Function

    Public Function RecuperarTalonDN(ByVal pActor As PrincipalDN, ByVal idSesion As String, ByVal pIDTalon As String) As GestionPagos.DN.TalonDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim miln As New FN.GestionPagos.LN.PagosLN()
                RecuperarTalonDN = miln.RecuperarTalonDN(pIDTalon)

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, pActor, mRec)

            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, pActor, ex, "", mRec)
                Throw
            End Try
        End Using
    End Function

    Public Function RecuperarFicherosTransferenciasActivos(ByVal pActor As PrincipalDN, ByVal idSesion As String) As FN.GestionPagos.DN.ColFicheroTransferenciaDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)

            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, pActor, mRec)

                '2 º comprobación permisos
                pActor.Autorizado()

                '3º creación de la ln y ejecucion del método
                Dim miln As New FN.GestionPagos.LN.PagosLN()
                RecuperarFicherosTransferenciasActivos = miln.RecuperarFicherosTransferenciasActivos()

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

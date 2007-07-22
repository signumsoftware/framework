Imports FN.GestionPagos.DN


Public Class PagosLNC
    Private Mensaje As String = String.Empty


    'CargarPagos

    Public Function CargarPagos(ByVal pDts As GestionPagos.DN.dtsGestionPagos, ByVal tipoOrigen As FN.GestionPagos.DN.TipoEntidadOrigenDN, ByVal operacion As Framework.Procesos.ProcesosDN.OperacionDN) As Data.DataSet

        Dim dtsRespuesta As DataSet

        Dim miAs As New GestionPagos.AS.PagosAS()
        dtsRespuesta = miAs.CargarPagos(pDts, tipoOrigen, operacion)

        Return dtsRespuesta
    End Function

    Public Function AltaModificacionProveedores(ByVal pDts As GestionPagos.DN.dtsGestionPagos) As Data.DataSet

        Dim dtsRespuesta As DataSet

        Dim miAs As New GestionPagos.AS.PagosAS()
        dtsRespuesta = miAs.AltaModificacionProveedores(pDts)

        Return dtsRespuesta
    End Function


    Public Function CrearTransferencia(ByVal pControl As Object, ByVal pPago As PagoDN, ByVal pComando As Object) As PagoDN
        If Not Me.EstadoCorrecto(pPago, Mensaje) Then
            Throw New ApplicationException(Mensaje)
        End If

        Dim tr As New TransferenciaDN()
        tr.Pago = pPago

        pPago.Transferencia = tr

        Return pPago
    End Function

    Public Function CrearTalon(ByVal pControl As Object, ByVal pPago As PagoDN, ByVal pComando As Object) As PagoDN
        If Not Me.EstadoCorrecto(pPago, Mensaje) Then
            Throw New ApplicationException(Mensaje)
        End If

        Dim ta As New TalonDN()
        ta.Pago = pPago

        pPago.Talon = ta

        Return pPago
    End Function

    Private Function EstadoCorrecto(ByVal pPago As PagoDN, ByRef pMensaje As String) As Boolean
        If pPago Is Nothing Then
            pMensaje = "El Pago está vacío"
            Return False
        End If

        If Not pPago.Transferencia Is Nothing Then
            pMensaje = "El Pago ya contiene una transferencia"
            Return False
        End If

        If Not pPago.Talon Is Nothing Then
            pMensaje = "El Pago ya contiene un Talón"
            Return False
        End If

        Return True
    End Function

    Public Function GenerarFicheroTransferencia(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal sender As Object) As Framework.DatosNegocio.IEntidadBaseDN
        Dim ft As FicheroTransferenciaDN

        '1º Recupero el fichero de transferencia
        ft = pTransicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion

        '2º Verificaciones previas para generar el fichero
        If ft.Estado = Framework.DatosNegocio.EstadoDatosDN.Modificado Then
            Throw New ApplicationException("El fichero no puede ser modificado para realizar esta operación")
        End If

        If ft.FicheroGenerado AndAlso Not pTransicionRealizada.OperacionRealizadaOrigen.Operacion.Nombre = "Generar FT" Then
            Throw New ApplicationException("El fichero ya ha sido generado anteriormente")
        End If

        '3º Se genera la transferencia
        GenerarArchivoFT(ft)

        '4º Modifico el estado del fichero a generado
        If Not ft.FicheroGenerado Then
            ft.FicheroGenerado = True
        End If

        Dim proceso As New Framework.Procesos.ProcesosAS.OperacionesAS()

        Return proceso.EjecutarOperacion(objeto, pTransicionRealizada, Nothing)

    End Function

    Private Sub GenerarArchivoFT(ByVal ft As FicheroTransferenciaDN)

        '1º separar los pagos por cuenta origen del pago
        If ft.ColPagos Is Nothing Then
            Throw New ApplicationException("No existen pagos para generar el fichero")
        End If

        Dim listaColPagosxFT As List(Of ColPagoDN)
        listaColPagosxFT = ft.ColPagos.RecuperarListaColPagosxCuentaOrigen()

        '2º Para cada col de pagos, generar un ft
        For Each colP As ColPagoDN In listaColPagosxFT
            '3º Cadena común para cabecera
            Dim pathFT As String = System.IO.Directory.GetCurrentDirectory() & "\FicherosTransferencias\"

            If Not System.IO.Directory.Exists(pathFT) Then
                System.IO.Directory.CreateDirectory(pathFT)
            End If

            Dim fichero As IO.FileStream = Nothing
            Dim str As IO.StreamWriter = Nothing

            Try
                fichero = New IO.FileStream(pathFT & "FT_" & ft.Nombre & "_" & ft.FechaEnvio.Year & ft.FechaEnvio.Month & ft.FechaEnvio.Day & ".txt", IO.FileMode.CreateNew)
                str = New IO.StreamWriter(fichero)

                Dim respuesta As New System.Text.StringBuilder()
                Dim cadenaComun As String = ""

                '4º Registros de cabecera
                '1
                cadenaComun = "0356" & colP.Item(0).CuentaOrigenPago.Titulares.Item(0).IentidadFiscal.IdentificacionFiscal.Codigo.ToUpper()
                respuesta.Append(cadenaComun)
                Do While respuesta.Length < 26
                    respuesta.Append(" ")
                Loop
                respuesta.Append("001")
                respuesta.Append(ft.FechaEnvio.Day & ft.FechaEnvio.Month & ft.FechaEnvio.Year)
                respuesta.Append(ft.FechaEmision.Day & ft.FechaEmision.Month & ft.FechaEmision.Year)
                respuesta.Append(colP.Item(0).CuentaOrigenPago.CCC.CodigoEntidadBancaria & colP.Item(0).CuentaOrigenPago.CCC.CodigoOficina & colP.Item(0).CuentaOrigenPago.CCC.CodigoCuenta)
                respuesta.Append("0   ")
                respuesta.Append(colP.Item(0).CuentaOrigenPago.CCC.CodigoDigitosControl)
                respuesta.Append("       " & vbCrLf)

                '2
                respuesta.Append(cadenaComun)
                Do While respuesta.Length < 26
                    respuesta.Append(" ")
                Loop
                respuesta.Append("002")
                respuesta.Append(colP.Item(0).CuentaOrigenPago.Titulares.Item(0).IentidadFiscal.DenominacionFiscal.ToUpper() & vbCrLf)

                '3
                respuesta.Append(cadenaComun)
                Do While respuesta.Length < 26
                    respuesta.Append(" ")
                Loop
                respuesta.Append("003")
                respuesta.Append(colP.Item(0).CuentaOrigenPago.Titulares.Item(0).IentidadFiscal.DomicilioFiscal.ToString.ToUpper() & vbCrLf)

                '4
                respuesta.Append(cadenaComun)
                Do While respuesta.Length < 26
                    respuesta.Append(" ")
                Loop
                respuesta.Append("004")
                respuesta.Append(colP.Item(0).CuentaOrigenPago.Titulares.Item(0).IentidadFiscal.DomicilioFiscal.Localidad.Provincia.Nombre.ToUpper() & vbCrLf)

                For Each pago As PagoDN In ft.ColPagos
                    '5º Cadena común beneficiario
                    cadenaComun = "0656" & pago.CuentaOrigenPago.Titulares.Item(0).IentidadFiscal.IdentificacionFiscal.Codigo & pago.Destinatario.IentidadFiscal.IdentificacionFiscal.Codigo

                    '6º Registros beneficiario
                    '1
                    respuesta.Append(cadenaComun)
                    Do While respuesta.Length < 28
                        respuesta.Append(" ")
                    Loop
                    respuesta.Append("010")
                    respuesta.Append(Math.Round(pago.Importe * 100) & vbCrLf)
                    '2

                Next

                '7º Registro de totales
                respuesta.Append("0856")

                respuesta.Append(vbCrLf)

                str.Write(respuesta.ToString())

            Catch ex As Exception

            Finally
                If str IsNot Nothing Then
                    str.Close()
                    str.Dispose()
                End If

                If fichero IsNot Nothing Then
                    fichero.Close()
                    fichero.Dispose()
                End If
            End Try

        Next

    End Sub

    Private Function RellenoBlancos(ByVal numBlancos As Integer) As String
        Dim cadRespuesta As String = ""

        Do While cadRespuesta.Length < numBlancos
            cadRespuesta += " "
        Loop

        Return cadRespuesta

    End Function

    Private Function CompletarCeros(ByVal longitudCadena As Integer, ByVal cadena As String) As String
        Dim cadRespuesta As String = cadena

        Do While cadRespuesta.Length < longitudCadena
            cadRespuesta = "0" & cadRespuesta
        Loop

        Return cadRespuesta

    End Function

End Class

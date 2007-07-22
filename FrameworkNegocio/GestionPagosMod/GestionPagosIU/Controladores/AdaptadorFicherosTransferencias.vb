Imports MotorBusquedaBasicasDN
Public Class AdaptadorFicherosTransferencias

#Region "Constructor"
    Public Sub New()

    End Sub
#End Region

#Region "Métodos"

    'TODO: Esta función duplica el código del Adaptador de impresión -> Unificar
    ' proceso para los múltiples
    Public Function AdjuntarPagoUnicoFT(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal instanciaSolicitante As Object) As Framework.DatosNegocio.IEntidadBaseDN
        Dim principal As Framework.Usuarios.DN.PrincipalDN = Nothing
        Dim f As MotorIU.FormulariosP.IFormularioP
        Dim cp As MotorIU.ControlesP.IControlP

        'Se comprueba que no se haya modificado el objeto antes de ser enviado a imprimir
        Dim objEnt As Framework.DatosNegocio.IEntidadBaseDN = objeto
        If objEnt.Estado <> Framework.DatosNegocio.EstadoDatosDN.SinModificar Then
            Throw New ApplicationException("No se puede modificar el objeto para realizar esta operación")
        End If

        If TypeOf instanciaSolicitante Is MotorIU.FormulariosP.IFormularioP Then
            f = instanciaSolicitante
            principal = f.cMarco.Principal
        End If

        If TypeOf instanciaSolicitante Is MotorIU.ControlesP.IControlP Then
            cp = instanciaSolicitante
            principal = cp.Marco.Principal
            f = cp.FormularioPadre
        End If

        Dim pago As GestionPagos.DN.PagoDN = Nothing

        If principal IsNot Nothing Then
            Dim op As Framework.Procesos.ProcesosDN.OperacionDN = principal.ColOperaciones.RecuperarxNombreVerbo("Adjuntar Fichero Transferencia")

            If op IsNot Nothing Then
                pago = pTransicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion

                ' la operacion de impresion
                Dim colop As New Framework.Procesos.ProcesosDN.ColOperacionDN
                colop.Add(op)

                ' la resticcion para la busqueda de ste pago
                Dim colValores As New List(Of ValorCampo)
                Dim vc1 As New ValorCampo()
                Dim vc2 As New ValorCampo()

                vc1.Operador = OperadoresAritmeticos.igual
                vc1.NombreCampo = "id"
                vc1.Valor = pago.ID
                colValores.Add(vc1)

                vc2.Operador = OperadoresAritmeticos.igual
                vc2.NombreCampo = "idPrincipal"
                vc2.Valor = principal.ID
                colValores.Add(vc2)

                MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(f, GetType(FN.GestionPagos.DN.PagoDN), Nothing, MotorIU.Motor.TipoNavegacion.CerrarLanzador, "AdjuntarPagoFT", True, colop, colValores, Nothing, Nothing)

            Else
                MessageBox.Show("El principal actual no tiene autorizada la operación")
            End If

        Else
            Throw New ApplicationException("El actor no puede ser nulo")
        End If

        Return pago

    End Function

#End Region

End Class

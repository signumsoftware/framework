Imports MotorBusquedaBasicasDN
Imports Framework.IU.IUComun

Public Class AdaptadorImpresion
    Public Sub New()

    End Sub

    Public Function ImprimirUnico(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal instanciaSolicitante As Object) As Framework.DatosNegocio.IEntidadBaseDN
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

        If principal IsNot Nothing Then
            Dim op As Framework.Procesos.ProcesosDN.OperacionDN = principal.ColOperaciones.RecuperarxNombreVerbo("Impresión")

            If op IsNot Nothing Then


                Dim pago As GestionPagos.DN.PagoDN = pTransicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion


                ' la operacion de impresion
                Dim colop As New Framework.Procesos.ProcesosDN.ColOperacionDN
                colop.Add(op)

                ' la resticcion para la busqueda de ste pago
                Dim colValores As New List(Of ValorCampo)
                Dim vc As New ValorCampo

                vc.Operador = OperadoresAritmeticos.igual
                vc.NombreCampo = "id"
                vc.Valor = pago.ID
                colValores.Add(vc)

                MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(f, GetType(FN.GestionPagos.DN.PagoDN), Nothing, TipoNavegacion.CerrarLanzador, "PreImpresionTalones", True, colop, colValores, Nothing, Nothing)

            Else

                MessageBox.Show("El principal actual no tine autorzado la operación")

            End If


        End If

        'Return pTransicionRealizada.

    End Function

End Class

Imports Framework.LogicaNegocios.Transacciones.BaseTransaccionLN
Imports FN.Localizaciones.DN

Public Class LiquidadorConcretoPruebaLN
    Inherits FN.GestionPagos.LN.LiquidadorConcretoBaseLN




    Public Overrides Function LiquidarPago(ByVal pPago As FN.GestionPagos.DN.PagoDN) As FN.GestionPagos.DN.ColLiquidacionPagoDN


        Using tr As New Framework.LogicaNegocios.Transacciones.Transaccion

            MyBase.LiquidarPago(pPago)

            ' el pago debe tener un iporte debido
            If pPago.ApunteImpDOrigen Is Nothing OrElse pPago.ApunteImpDProducto Is Nothing Then
                Throw New Exception
            End If


            ' problema identificar el origen del importe debido del pago
            ' lo obtenemos de la huella al horigen que guarda el


            System.Diagnostics.Debug.WriteLine(pPago.ApunteImpDOrigen.HuellaIOrigenImpDebDN.ToStringEntidadReferida)


            ' recuperar empresas

            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim coldeudoras As New ColIEntidadFiscalDN
            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            coldeudoras.AddRangeObject(bgln.RecuperarLista(GetType(FN.Empresas.DN.EmpresaFiscalDN)))

            coldeudoras.EliminarEntidadDNxGUID(pPago.ApunteImpDOrigen.Acreedora.GUID)
            ' ponemos el caso de que liquide a 30% 20 %  a dos entidaades distieas

            Dim colef As ColIEntidadFiscalDN
            Dim colliq As New FN.GestionPagos.DN.ColLiquidacionPagoDN
            colef = coldeudoras


            Dim liq As FN.GestionPagos.DN.LiquidacionPagoDN
            liq = New FN.GestionPagos.DN.LiquidacionPagoDN
            liq.pago = pPago
            liq.IImporteDebidoDN = New FN.GestionPagos.DN.ApunteImpDDN(liq)
            liq.IImporteDebidoDN.Deudora = pPago.ApunteImpDOrigen.Acreedora
            liq.IImporteDebidoDN.Acreedora = colef.Item(0).EntidadFiscalGenerica
            liq.IImporteDebidoDN.Importe = pPago.Importe * 0.2
            liq.IImporteDebidoDN.FEfecto = pPago.ApunteImpDProducto.FEfecto
            liq.IImporteDebidoDN.FCreación = Now
            colliq.Add(liq)

            liq = New FN.GestionPagos.DN.LiquidacionPagoDN
            liq.IImporteDebidoDN = New FN.GestionPagos.DN.ApunteImpDDN(liq)
            liq.pago = pPago
            liq.IImporteDebidoDN.Deudora = pPago.ApunteImpDOrigen.Acreedora
            liq.IImporteDebidoDN.Acreedora = colef.Item(1).EntidadFiscalGenerica
            liq.IImporteDebidoDN.Importe = pPago.Importe * 0.3
            liq.IImporteDebidoDN.FEfecto = pPago.ApunteImpDProducto.FEfecto
            liq.IImporteDebidoDN.FCreación = Now
            colliq.Add(liq)


            ' guardar el pago liquidado
            'ppago.Esta
            Me.GuardarGenerico(pPago)

            ' llamar al gi para que guarde la col de liquidaciones

            Me.GuardarGenerico(colliq)
            'Me.GuardarGenerico(colliq.Item(0))
            'Me.GuardarGenerico(colliq.Item(1))
            tr.Confirmar()
            Return colliq
        End Using


    End Function



    'Public Overrides Function GuardarNuevoapunteImporteDebido(ByVal origen As FN.GestionPagos.DN.IOrigenIImporteDebidoDN) As FN.GestionPagos.DN.ColIImporteDebidoDN

    'End Function
End Class

'Public Class OrigenIdevPruebaDN
'    Inherits Framework.DatosNegocio.EntidadTemporalDN
'    Implements FN.GestionPagos.DN.IOrigenIImporteDebidoDN

'    Protected mIImporteDebidoDN As FN.GestionPagos.DN.IImporteDebidoDN

'    Public Property ColIEntidadDN() As Framework.DatosNegocio.ColIEntidadDN Implements FN.GestionPagos.DN.IOrigenIImporteDebidoDN.ColIEntidadDN
'        Get
'            Return Nothing
'        End Get
'        Set(ByVal value As Framework.DatosNegocio.ColIEntidadDN)

'        End Set
'    End Property

'    Public Property IImporteDebidoDN() As FN.GestionPagos.DN.IImporteDebidoDN Implements FN.GestionPagos.DN.IOrigenIImporteDebidoDN.IImporteDebidoDN
'        Get
'            Return mIImporteDebidoDN
'        End Get
'        Set(ByVal value As FN.GestionPagos.DN.IImporteDebidoDN)
'            Me.CambiarValorRef(Of FN.GestionPagos.DN.IOrigenIImporteDebidoDN)(value, mIImporteDebidoDN)
'        End Set
'    End Property
'End Class
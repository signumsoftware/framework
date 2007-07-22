Imports Framework.DatosNegocio
''' <summary>
''' Nota 
''' 
''' en la coleccion de entidades origen solo debe tener un elemento o cero 
''' y debe estar sincronizado con el pago que da origen a la liquidación
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public Class LiquidacionPagoDN
    Inherits OrigenIdevBaseDN

    Implements FN.GestionPagos.DN.IOrigenIImporteDebidoDN

    'Protected mFAnulacion As Date

    Protected mpago As PagoDN
    'Protected mIImporteDebidoDN As IImporteDebidoDN
    ' Protected mColIEntidadDN As Framework.DatosNegocio.ColIEntidadDN

    Protected mLiquidacionCompensada As LiquidacionPagoDN

    Protected mColHeCausas As ColHEDN


    Public Sub New()
        CambiarValorRef(Of ColHEDN)(New ColHEDN, mColHeCausas)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub



    <RelacionPropCampoAtribute("mColHeCausas")> _
    Public Property ColHeCausas() As ColHEDN

        Get
            Return mColHeCausas
        End Get

        Set(ByVal value As ColHEDN)
            CambiarValorRef(Of ColHEDN)(value, mColHeCausas)

        End Set
    End Property






    <RelacionPropCampoAtribute("mLiquidacionCompensada")> _
    Public Property LiquidacionCompensada() As LiquidacionPagoDN

        Get
            Return mLiquidacionCompensada
        End Get

        Set(ByVal value As LiquidacionPagoDN)
            CambiarValorRef(Of LiquidacionPagoDN)(value, mLiquidacionCompensada)

        End Set
    End Property









    Public Property pago() As PagoDN
        Get
            Return Me.mpago
        End Get
        Set(ByVal value As PagoDN)
            Me.CambiarValorRef(Of PagoDN)(value, Me.mpago)
            If mColHEDN Is Nothing Then
                Dim miColIEntidadDN As New Framework.DatosNegocio.ColHEDN
                miColIEntidadDN.AddHuellaPara(value)
                Me.ColHEDN = miColIEntidadDN
            Else
                If Not value Is Nothing Then
                    mColHEDN.Clear()
                    mColHEDN.AddHuellaPara(value)
                End If
            End If



        End Set
    End Property
    'Public Property ColIEntidadDN() As Framework.DatosNegocio.ColIEntidadDN Implements IOrigenIImporteDebidoDN.ColIEntidadDN
    '    Get
    '        Return mColIEntidadDN
    '    End Get
    '    Set(ByVal value As Framework.DatosNegocio.ColIEntidadDN)
    '        Me.CambiarValorRef(Of Framework.DatosNegocio.ColIEntidadDN)(value, mColIEntidadDN)

    '    End Set
    'End Property

    'Public Property IImporteDebidoDN() As IImporteDebidoDN Implements IOrigenIImporteDebidoDN.IImporteDebidoDN
    '    Get
    '        Return mIImporteDebidoDN
    '    End Get
    '    Set(ByVal value As IImporteDebidoDN)
    '        Me.CambiarValorRef(Of IImporteDebidoDN)(value, mIImporteDebidoDN)

    '    End Set

    'End Property

    'Public Property ColIEntidadDN() As System.Collections.Generic.IList(Of Framework.DatosNegocio.IEntidadDN) Implements IOrigenIImporteDebidoDN.ColIEntidadDN
    '    Get
    '        Return mColIEntidadDN
    '    End Get
    '    Set(ByVal value As System.Collections.Generic.IList(Of Framework.DatosNegocio.IEntidadDN))

    '        Me.CambiarValorRef(Of Framework.DatosNegocio.ColIEntidadDN)(value, mColIEntidadDN)
    '        SincronizarColeccionAPago(value)


    '    End Set
    'End Property



    Private Sub SincronizarColeccionAPago(ByVal value As Framework.DatosNegocio.ColIEntidadDN)
        If value Is Nothing Then
            Me.pago = Nothing

        Else
            Select Case value.Count
                Case 0
                    Me.pago = Nothing

                Case 1
                    If Not Me.pago Is value.Item(0) Then
                        Me.pago = value.Item(0)
                    End If

                Case Is > 1
                    Throw New Framework.DatosNegocio.ApplicationExceptionDN("la coleccion solo puede contener un pago")

            End Select
        End If



    End Sub

    Public Overrides Sub ElementoAñadido(ByVal pSender As Object, ByVal pElemento As Object)
        MyBase.ElementoAñadido(pSender, pElemento)
        If pSender Is Me.mColHEDN AndAlso Not Me.mColHEDN.Contiene(New HEDN(Me.mpago), CoincidenciaBusquedaEntidadDN.Todos) Then
            SincronizarColeccionAPago(pSender)
        End If

    End Sub

    Public Overrides Sub ElementoEliminado(ByVal pSender As Object, ByVal pElemento As Object)
        MyBase.ElementoEliminado(pSender, pElemento)

        SincronizarColeccionAPago(pSender)
    End Sub

    'Public Property FAnulacion() As Date Implements IOrigenIImporteDebidoDN.FAnulacion
    '    Get
    '        Return mFAnulacion
    '    End Get
    '    Set(ByVal value As Date)
    '        Me.CambiarValorVal(Of Date)(value, mFAnulacion)
    '    End Set
    'End Property


    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN


        If Me.mpago Is Nothing Then
            pMensaje = "una liquidación debe estar vinculada a un pago "
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        'If Me.mpago Is Nothing AndAlso Me.mLiquidacionCompensada Is Nothing Then
        '    pMensaje = "una liquidación debe estar vinculada a un pago o a una liquidación a la que compensa"
        '    Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        'End If



        ' compensa una liquidacion
        If mLiquidacionCompensada IsNot Nothing Then
            If Me.mLiquidacionCompensada.FAnulacion <> Date.MinValue Then
                pMensaje = "una liquidación no puede estar asociada a una liquidacion  anulada"
                Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
            End If
            If Me.mLiquidacionCompensada.IImporteDebidoDN.Importe <> Me.IImporteDebidoDN.Importe OrElse Me.mLiquidacionCompensada.IImporteDebidoDN.Deudora.GUID <> Me.IImporteDebidoDN.Acreedora.GUID AndAlso Me.mLiquidacionCompensada.IImporteDebidoDN.Acreedora.GUID <> Me.IImporteDebidoDN.Deudora.GUID Then
                pMensaje = "una liquidación no puede estar asociada a una liquidacion  anulada"
                Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
            End If
        End If



        ' liquida un pago
        If Me.mpago.FechaAnulacion <> Date.MinValue Then
            pMensaje = "una liquidación no puede estar asociada a un pago anulado"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If





        Return MyBase.EstadoIntegridad(pMensaje)
    End Function


    Public Function CrearLiqPagoCompensatoria(ByVal pagoCompensatorio As PagoDN) As LiquidacionPagoDN

        If Not pagoCompensatorio.PagoCompensado.GUID = Me.pago.GUID Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("El pago compesatorio y el pago de la liquidacion no representan la misma entidad de negocio")
        End If

        Dim porcentage As Single = pagoCompensatorio.Importe / Me.mpago.Importe


        Dim liqcomp As New LiquidacionPagoDN()

        liqcomp.pago = pagoCompensatorio
        liqcomp.IImporteDebidoDN = Me.IImporteDebidoDN.CrearImpDebCompesatorio(liqcomp)
        liqcomp.LiquidacionCompensada = Me
        Return liqcomp


    End Function
    Public Sub Anular()


        If Me.mIImporteDebidoDN.FAnulacion <> Date.MinValue Then
            Throw New ApplicationException("El importe debido ya esta anulado y no es anulable id:" & Me.mIImporteDebidoDN.ID)
        End If

        Me.FAnulacion = Now
        Me.mIImporteDebidoDN.HuellaIOrigenImpDebDN.EntidadReferida = Me
        Me.mIImporteDebidoDN.FAnulacion = Me.FAnulacion

    End Sub

End Class

<Serializable()> _
Public Class ColLiquidacionPagoDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of LiquidacionPagoDN)

End Class
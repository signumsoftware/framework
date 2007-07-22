Imports Framework.DatosNegocio
Imports FN.Seguros.Polizas.DN

Imports Framework.Cuestionario.CuestionarioDN

<Serializable()> _
Public Class DatosTarifaVehiculosDN
    Inherits EntidadDN
    Implements IDatosTarifaDN

#Region "Atributos"
    Protected mColTipoDocumentoRequerido As Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN
    Protected mColCajonDocumento As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN

    Protected mColConductores As ColConductorDN
    Protected mColOperacionPrimaBaseRVCache As ColOperacionPrimaBaseRVCacheDN
    Protected mColOperacionModuladorRVCache As ColOperacionModuladorRVCacheDN
    Protected mColOperacionImpuestoRVCache As ColOperacionImpuestoRVCacheDN
    Protected mColOperacionSumaRVCache As ColOperacionSumaRVCacheDN
    Protected mColOperacionComisionRVCache As ColOperacionComisionRVCacheDN
    Protected mColOperacionFracRVCache As ColOperacionFracRVCacheDN
    Protected mColOperacionBonifRVCache As ColOperacionBonificacionRVCacheDN

    Protected mHEEntidadColaboradora As FN.Empresas.DN.HEEntidadColaboradoraDN
    Protected mHeCuestionarioResuelto As HeCuestionarioResueltoDN
    Protected mValorBonificacion As Double
    Protected mTarifa As TarifaDN

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        Me.CambiarValorCol(Of Framework.Ficheros.FicherosDN.ColCajonDocumentoDN)(New Framework.Ficheros.FicherosDN.ColCajonDocumentoDN(), mColCajonDocumento)
        Me.CambiarValorCol(Of ColOperacionPrimaBaseRVCacheDN)(New ColOperacionPrimaBaseRVCacheDN(), mColOperacionPrimaBaseRVCache)
        Me.CambiarValorCol(Of ColOperacionModuladorRVCacheDN)(New ColOperacionModuladorRVCacheDN(), mColOperacionModuladorRVCache)
        Me.CambiarValorCol(Of ColOperacionImpuestoRVCacheDN)(New ColOperacionImpuestoRVCacheDN(), mColOperacionImpuestoRVCache)
        Me.CambiarValorCol(Of ColOperacionFracRVCacheDN)(New ColOperacionFracRVCacheDN(), mColOperacionFracRVCache)
        Me.CambiarValorCol(Of ColOperacionComisionRVCacheDN)(New ColOperacionComisionRVCacheDN(), mColOperacionComisionRVCache)
        Me.CambiarValorCol(Of ColOperacionBonificacionRVCacheDN)(New ColOperacionBonificacionRVCacheDN(), mColOperacionBonifRVCache)
        Me.CambiarValorCol(Of ColOperacionSumaRVCacheDN)(New ColOperacionSumaRVCacheDN(), mColOperacionSumaRVCache)
        Me.CambiarValorCol(Of ColConductorDN)(New ColConductorDN(), mColConductores)
        AsignarmeATarifa()
    End Sub

#End Region

#Region "Propiedades"







    <RelacionPropCampoAtribute("mColTipoDocumentoRequerido")> _
    Public Property ColTipoDocumentoRequerido() As Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN
        Get
            Return mColTipoDocumentoRequerido
        End Get
        Set(ByVal value As Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN)
            CambiarValorRef(Of Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN)(value, mColTipoDocumentoRequerido)
        End Set
    End Property







    <RelacionPropCampoAtribute("mColCajonDocumento")> _
    Public Property ColCajonDocumento() As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN
        Get
            Return mColCajonDocumento
        End Get
        Set(ByVal value As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN)
            CambiarValorRef(Of Framework.Ficheros.FicherosDN.ColCajonDocumentoDN)(value, mColCajonDocumento)
        End Set
    End Property






    Public ReadOnly Property TotalImporteFraccioanble() As Double
        Get
            Return ImportePrimaModuladaFraccionable + ImporteImpuestosFraccionable + ImporteComisionesFraccionable
        End Get
    End Property

    Public ReadOnly Property TotalImporteNoFraccioanble() As Double
        Get
            Return ImportePrimaModuladaNoFraccioable + ImporteImpuestosNoFraccionable + ImporteComisionesNoFraccionable
        End Get
    End Property

    Public ReadOnly Property ImportePrimaModuladaNoFraccioable() As Double
        Get
            Return 0
        End Get
    End Property

    Public ReadOnly Property ImportePrimaModuladaFraccionable() As Double
        Get
            Return mColOperacionModuladorRVCache.RecuperarColUltimasOperaciones.CalcularImporteTotal
        End Get
    End Property

    Public ReadOnly Property ImporteImpuestosFraccionable() As Double
        Get
            Return mColOperacionImpuestoRVCache.CalcularImporteTotal(Fraccionable.SI)
        End Get
    End Property

    Public ReadOnly Property ImporteImpuestosNoFraccionable() As Double
        Get
            Return mColOperacionImpuestoRVCache.CalcularImporteTotal(Fraccionable.No)
        End Get
    End Property

    Public ReadOnly Property ImporteComisionesFraccionable() As Double
        Get
            Return mColOperacionComisionRVCache.CalcularImporteTotal(Fraccionable.SI)
        End Get
    End Property

    Public ReadOnly Property ImporteComisionesNoFraccionable() As Double
        Get
            Return mColOperacionComisionRVCache.CalcularImporteTotal(Fraccionable.No)
        End Get
    End Property

    <RelacionPropCampoAtribute("mHeCuestionarioResuelto")> _
    Public Property HeCuestionarioResuelto() As HeCuestionarioResueltoDN
        Get
            Return mHeCuestionarioResuelto
        End Get
        Set(ByVal value As HeCuestionarioResueltoDN)
            CambiarValorRef(Of HeCuestionarioResueltoDN)(value, mHeCuestionarioResuelto)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColOperacionPrimaBaseRVCache")> _
    Public Property ColOperacionPrimaBaseRVCache() As ColOperacionPrimaBaseRVCacheDN
        Get
            Return mColOperacionPrimaBaseRVCache
        End Get
        Set(ByVal value As ColOperacionPrimaBaseRVCacheDN)
            CambiarValorRef(Of ColOperacionPrimaBaseRVCacheDN)(value, mColOperacionPrimaBaseRVCache)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColOperacionModuladorRVCache")> _
    Public Property ColOperacionModuladorRVCache() As ColOperacionModuladorRVCacheDN
        Get
            Return mColOperacionModuladorRVCache
        End Get
        Set(ByVal value As ColOperacionModuladorRVCacheDN)
            CambiarValorRef(Of ColOperacionModuladorRVCacheDN)(value, mColOperacionModuladorRVCache)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColOperacionFracRVCache")> _
        Public Property ColOperacionFracRVCache() As ColOperacionFracRVCacheDN
        Get
            Return mColOperacionFracRVCache
        End Get
        Set(ByVal value As ColOperacionFracRVCacheDN)
            CambiarValorRef(Of ColOperacionFracRVCacheDN)(value, mColOperacionFracRVCache)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColOperacionImpuestoRVCache")> _
    Public Property ColOperacionImpuestoRVCache() As ColOperacionImpuestoRVCacheDN
        Get
            Return mColOperacionImpuestoRVCache
        End Get

        Set(ByVal value As ColOperacionImpuestoRVCacheDN)
            CambiarValorRef(Of ColOperacionImpuestoRVCacheDN)(value, mColOperacionImpuestoRVCache)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColOperacionSumaRVCache")> _
    Public Property ColOperacionSumaRVCache() As ColOperacionSumaRVCacheDN
        Get
            Return mColOperacionSumaRVCache
        End Get
        Set(ByVal value As ColOperacionSumaRVCacheDN)
            CambiarValorRef(Of ColOperacionSumaRVCacheDN)(value, mColOperacionSumaRVCache)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColOperacionBonifRVCache")> _
    Public Property ColOperacionBonifRVCache() As ColOperacionBonificacionRVCacheDN
        Get
            Return mColOperacionBonifRVCache
        End Get
        Set(ByVal value As ColOperacionBonificacionRVCacheDN)
            CambiarValorCol(Of ColOperacionBonificacionRVCacheDN)(value, mColOperacionBonifRVCache)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColOperacionComisionRVCache")> _
    Public Property ColOperacionComisionRVCache() As ColOperacionComisionRVCacheDN
        Get
            Return mColOperacionComisionRVCache
        End Get
        Set(ByVal value As ColOperacionComisionRVCacheDN)
            CambiarValorCol(Of ColOperacionComisionRVCacheDN)(value, mColOperacionComisionRVCache)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColConductores")> _
    Public Property ColConductores() As ColConductorDN
        Get
            Return mColConductores
        End Get
        Set(ByVal value As ColConductorDN)
            CambiarValorCol(Of ColConductorDN)(value, mColConductores)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColOperacionSumaRVCache")> _
    Public Property Tarifa() As Seguros.Polizas.DN.TarifaDN Implements Seguros.Polizas.DN.IDatosTarifaDN.Tarifa
        Get
            Return mTarifa
        End Get
        Set(ByVal value As Seguros.Polizas.DN.TarifaDN)
            CambiarValorRef(Of TarifaDN)(value, mTarifa)
            AsignarmeATarifa()
        End Set
    End Property

    <RelacionPropCampoAtribute("mHEEntidadColaboradoraDN")> _
    Public Property HEEntidadColaboradoraDN() As Empresas.DN.HEEntidadColaboradoraDN Implements Seguros.Polizas.DN.IDatosTarifaDN.HEEmpresaColaboradora
        Get
            Return mHEEntidadColaboradora
        End Get
        Set(ByVal value As FN.Empresas.DN.HEEntidadColaboradoraDN)
            CambiarValorRef(Of Empresas.DN.HEEntidadColaboradoraDN)(value, mHEEntidadColaboradora)
        End Set
    End Property

    Public Property ValorBonificacion() As Double Implements Seguros.Polizas.DN.IDatosTarifaDN.ValorBonificacion
        Get
            Return mValorBonificacion
        End Get
        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorBonificacion)
        End Set
    End Property

#End Region

#Region "Métodos"
    Public Sub ActualizarProdutosAplicables(ByVal tarifa As FN.Seguros.Polizas.DN.TarifaDN, ByVal colFicherosRequeridos As Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN, ByVal colCdVinculadosaProductos As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN)


        'Precondiciones()
        '1º  verificar que los cajones documentos lo son para prodcutos 


        'Cuerpo()
        '2º verificamos que todos los documentos requeridos necesarios tiene su correspondoente cajon de doc verificado para cada  producto



        If colFicherosRequeridos Is Nothing OrElse colFicherosRequeridos.Count = 0 Then
            For Each lpropucto As FN.Seguros.Polizas.DN.LineaProductoDN In tarifa.ColLineaProducto
                lpropucto.Alcanzable = True
            Next




        Else


            For Each lpropucto As FN.Seguros.Polizas.DN.LineaProductoDN In tarifa.ColLineaProducto

                Dim alcanzable = True
                Dim micolFicherosRequeridos As Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN = colFicherosRequeridos.RecuperarPorColEntidadReferida(lpropucto.Producto.ColCoberturas)

                For Each tipoFicheroRequerido As Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN In micolFicherosRequeridos
                    If tipoFicheroRequerido IsNot Nothing AndAlso tipoFicheroRequerido.Necesario Then
                        ' este producto requeire tener un cajon documento
                        ' debe existir un cd para el y debe estar vinculado y verificado
                        Dim micolCdVinculadosaProductos As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN = colCdVinculadosaProductos.PodarCol(tipoFicheroRequerido.TipoDoc)

                        If micolCdVinculadosaProductos.Count = 0 Then
                            ' Throw New ApplicationExceptionDN("Debiera existri un cajon documentos para el tipo de documento requerido")
                        End If

                        For Each cd As Framework.Ficheros.FicherosDN.CajonDocumentoDN In micolCdVinculadosaProductos
                            If cd.Documento Is Nothing OrElse cd.FechaVerificacon = Date.MinValue Then
                                alcanzable = False
                                Exit For
                            End If
                        Next

                        If Not alcanzable Then
                            Exit For
                        End If


                    End If
                Next

                lpropucto.Alcanzable = alcanzable

            Next





        End If






    End Sub
    Public Function RecuperarCausas() As Framework.DatosNegocio.ColHEDN


        Dim col As New Framework.DatosNegocio.ColHEDN
        col.AddRangeObject(mColOperacionImpuestoRVCache.RecuperarCausas)
        col.AddRangeObject(mColOperacionComisionRVCache.RecuperarCausas)
        col.AddRangeObject(mColOperacionPrimaBaseRVCache.RecuperarCausas)

        Return col

    End Function

    Public Function RecuperarEntidadCache(ByVal pGUIDEntidadReferida As String) As IOperacionCausaRVCacheDN

        Dim objeto As Object

        objeto = mColOperacionPrimaBaseRVCache.RecuperarXGUID(pGUIDEntidadReferida)
        If objeto IsNot Nothing Then
            Return objeto
        End If

        objeto = mColOperacionModuladorRVCache.RecuperarXGUID(pGUIDEntidadReferida)
        If objeto IsNot Nothing Then
            Return objeto
        End If

        objeto = mColOperacionImpuestoRVCache.RecuperarXGUID(pGUIDEntidadReferida)
        If objeto IsNot Nothing Then
            Return objeto
        End If

        objeto = mColOperacionSumaRVCache.RecuperarXGUID(pGUIDEntidadReferida)
        If objeto IsNot Nothing Then
            Return objeto
        End If

        objeto = mColOperacionFracRVCache.RecuperarXGUID(pGUIDEntidadReferida)
        If objeto IsNot Nothing Then
            Return objeto
        End If

        objeto = mColOperacionComisionRVCache.RecuperarXGUID(pGUIDEntidadReferida)
        If objeto IsNot Nothing Then
            Return objeto
        End If

        objeto = mColOperacionBonifRVCache.RecuperarXGUID(pGUIDEntidadReferida)
        If objeto IsNot Nothing Then
            Return objeto
        End If

        Return Nothing

    End Function

    Public Function RecuperarEntidadCachexGuidCausa(ByVal pGUIDCausaEntidadReferida As String) As ColIOperacionCausaRVCacheDN

        Dim col As New ColIOperacionCausaRVCacheDN

        'objeto = mColOperacionPrimaBaseRVCache.RecuperarxGUIDCausa(pGUIDCausaEntidadReferida)
        'If objeto IsNot Nothing Then
        '    Return objeto
        'End If


        ' REALMNTE HAY QUE RECUPERAR EL VALOR de orden MAXIMO en vez de el de prima base *******

        col.AddRangeObject(mColOperacionModuladorRVCache.RecuperarColUltimasOperaciones().RecuperarxGUIDCausa(pGUIDCausaEntidadReferida))
        If col.Count > 0 Then
            Return col
        End If

        col.AddRangeObject(mColOperacionImpuestoRVCache.RecuperarxGUIDCausa(pGUIDCausaEntidadReferida))
        If col.Count > 0 Then
            Return col
        End If


        'objeto = mColOperacionSumaRVCache.RecuperarxGUIDCausa(pGUIDEntidadReferida)
        'If objeto IsNot Nothing Then
        '    Return objeto
        'End If

        col.AddRangeObject(mColOperacionComisionRVCache.RecuperarxGUIDCausa(pGUIDCausaEntidadReferida))

        If col.Count > 0 Then
            Return col
        End If

        Return Nothing

    End Function

    Public Sub ImporteFinaciablesImpuestos(ByRef impImpuestosFraccionable As Double, ByRef impImpuestosNOFraccionable As Double)
        impImpuestosFraccionable = 0
        impImpuestosNOFraccionable = 0

        If mColOperacionImpuestoRVCache IsNot Nothing Then
            For Each op As OperacionImpuestoRVCacheDN In mColOperacionImpuestoRVCache
                If op.Fraccionable Then
                    impImpuestosFraccionable += op.ValorImpuesto
                Else
                    impImpuestosNOFraccionable += op.ValorImpuesto
                End If
            Next
        End If

    End Sub

    Public Sub ImportesFinanciablesFraccionamiento(ByRef importeFracFraccionable As Double, ByRef importeFracNoFraccionable As Double)
        importeFracFraccionable = 0
        importeFracNoFraccionable = 0

        If mColOperacionFracRVCache IsNot Nothing Then
            For Each op As OperacionFracRVCacheDN In mColOperacionFracRVCache
                If op.Fraccionable Then
                    importeFracFraccionable += op.ValorresultadoOpr - op.ValorResultadoISVprecedente
                Else
                    importeFracNoFraccionable += op.ValorresultadoOpr - op.ValorResultadoISVprecedente
                End If
            Next
        End If

    End Sub

    Public Sub ImportesFinanciablesComisiones(ByRef importeComisionesFraccionable As Double, ByRef importeComisionesNoFraccionable As Double)
        importeComisionesFraccionable = 0
        importeComisionesNoFraccionable = 0

        If mColOperacionComisionRVCache IsNot Nothing Then
            For Each op As OperacionComisionRVCacheDN In mColOperacionComisionRVCache
                If op.Fraccionable Then
                    importeComisionesFraccionable += op.ValorresultadoOpr - op.ValorResultadoISVprecedente
                Else
                    importeComisionesNoFraccionable += op.ValorresultadoOpr - op.ValorResultadoISVprecedente
                End If
            Next
        End If

    End Sub

    Public Sub AsignarResultadosTarifa(ByVal colOpImp As ColOperacionImpuestoRVCacheDN, ByVal colOpMod As ColOperacionModuladorRVCacheDN, _
                                        ByVal colOpPB As ColOperacionPrimaBaseRVCacheDN, ByVal colOpSuma As ColOperacionSumaRVCacheDN, _
                                        ByVal colOpFrac As ColOperacionFracRVCacheDN, ByVal colOpComisiones As ColOperacionComisionRVCacheDN, _
                                        ByVal colOpBonif As ColOperacionBonificacionRVCacheDN)

        mColOperacionImpuestoRVCache = colOpImp
        mColOperacionModuladorRVCache = colOpMod
        mColOperacionPrimaBaseRVCache = colOpPB
        mColOperacionSumaRVCache = colOpSuma
        mColOperacionFracRVCache = colOpFrac
        mColOperacionComisionRVCache = colOpComisiones
        mColOperacionBonifRVCache = colOpBonif

        AsignarImportesLineasProducto()

    End Sub

    Public Sub EliminarEntidadesOReferidasOpCache()
        mColOperacionPrimaBaseRVCache.LimpiarEntidadesReferidas()
        mColOperacionModuladorRVCache.LimpiarEntidadesReferidas()
        mColOperacionImpuestoRVCache.LimpiarEntidadesReferidas()
        mColOperacionFracRVCache.LimpiarEntidadesReferidas()
        mColOperacionComisionRVCache.LimpiarEntidadesReferidas()
        mColOperacionBonifRVCache.LimpiarEntidadesReferidas()
        mColOperacionSumaRVCache.LimpiarEntidadesReferidas()
    End Sub

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mTarifa Is Nothing Then
            pMensaje = ("La tarifa no puede ser nula")
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mHeCuestionarioResuelto Is Nothing Then
            pMensaje = "Debe existir una huella de cuestionario resuelto"
            Return EstadoIntegridadDN.Inconsistente
        End If

        AsignarmeATarifa()

        AsignarImportesLineasProducto()

        ActualizarProdutosAplicables()




        Return MyBase.EstadoIntegridad(pMensaje)
    End Function




    Public Sub ActualizarProdutosAplicables()
        ActualizarProdutosAplicables(Me.mTarifa, Me.mColTipoDocumentoRequerido, Me.mColCajonDocumento)

    End Sub

    Public Function ClonarDatosTarifa() As Seguros.Polizas.DN.IDatosTarifaDN Implements Seguros.Polizas.DN.IDatosTarifaDN.ClonarDatosTarifa
        Dim datosTarifaClon As DatosTarifaVehiculosDN

        datosTarifaClon = Me.CloneSuperficialSinIdentidad()
        datosTarifaClon.EliminarEntidadesOReferidasOpCache()
        datosTarifaClon.mTarifa = Nothing
        datosTarifaClon.mHeCuestionarioResuelto = Nothing

        Return datosTarifaClon
    End Function

    Private Sub AsignarmeATarifa()
        If mTarifa IsNot Nothing AndAlso mTarifa.DatosTarifa.GUID <> mGUID Then
            mTarifa.DatosTarifa = Me
        End If
    End Sub

    Private Sub AsignarImportesLineasProducto()
        If mTarifa IsNot Nothing AndAlso mTarifa.ColLineaProducto IsNot Nothing Then
            For Each lp As LineaProductoDN In mTarifa.ColLineaProducto
                lp.ImporteLP = 0
                For Each cob As CoberturaDN In lp.Producto.ColCoberturas
                    lp.ImporteLP = lp.ImporteLP + RecuperarImporteCobertura(cob)
                Next
            Next
        End If
    End Sub

    Private Function RecuperarImporteCobertura(ByVal cob As CoberturaDN) As Double
        Dim valor As Double = 0
        If mColOperacionSumaRVCache IsNot Nothing Then
            valor = mColOperacionSumaRVCache.RecuperarValorOperacionxNombreOperacion("Suma Impuestos/" & cob.Nombre)
        End If

        Return valor
    End Function

    Public Sub CalcularImportePagos(ByVal numeroPagos As Integer, ByRef primerPago As Double, ByRef restoPagos As Double)
        Dim impNoFrac, impAux1, impAux2 As Double

        Me.ImportesFinanciablesComisiones(impAux1, impAux2)
        impNoFrac += impAux2
        Me.ImportesFinanciablesFraccionamiento(impAux1, impAux2)
        impNoFrac += impAux2
        Me.ImporteFinaciablesImpuestos(impAux1, impAux2)
        impNoFrac += impAux2

        restoPagos = (Me.Tarifa.Importe - impNoFrac) / numeroPagos

        primerPago = restoPagos + impNoFrac

    End Sub

#End Region

End Class




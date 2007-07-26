Imports Framework.IU.IUComun

Public Class GestionPagosCtrl

    Public Function CargarAgrupacionID(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN



        ' comando

        Dim control As Framework.IU.IUComun.IctrlBasicoDN = sender
        Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

        Dim AgrupApunteImpDDN As GestionPagos.DN.AgrupApunteImpDDN = control.DN



        Dim miias As New FN.GestionPagos.AS.GestionPagosAS
        AgrupApunteImpDDN = miias.CargarAgrupacionID(AgrupApunteImpDDN)


        control.DN = AgrupApunteImpDDN
        ' control.DNaIUgd()
        Return AgrupApunteImpDDN
    End Function

    Public Function CrearAgrupacionID(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN



        ' comando

        Dim control As Framework.IU.IUComun.IctrlBasicoDN = sender
        Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

        Dim ParEntFiscalGenericaParam As GestionPagos.DN.ParEntFiscalGenericaParamDN = control.DN



        Dim miias As New FN.GestionPagos.AS.GestionPagosAS
        ParEntFiscalGenericaParam = miias.CrearAgrupacionID(ParEntFiscalGenericaParam)


        control.DN = ParEntFiscalGenericaParam
        control.DNaIUgd()

    End Function


    Public Function EfectuarYLiquidarPago(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN



        ' comando

        Dim control As Framework.IU.IUComun.IctrlBasicoDN = sender
        Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

        Dim pagoOriginal As FN.GestionPagos.DN.PagoDN = control.DN

        '' solicitar al usuario los datos para efctuar el pago
        'MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarFormulario(fp, pagoOriginal, TipoNavegacion.Modal)



        'Dim fechaEfecto As DateTime = Now ' debiara de indicarlo el usuario ya que es un efecto manual
        'pagoOriginal.FechaEfecto = fechaEfecto


        ' invocamos la funcionalidad en el servidor


        Dim miias As New FN.GestionPagos.AS.GestionPagosAS
        Dim colliq As GestionPagos.DN.ColLiquidacionPagoDN = miias.EfectuarYLiquidarPago(pagoOriginal)

        If colliq.Count() > 0 Then

            Return colliq.Item(0).pago
        Else
            ' estu puede dar error
            Return pagoOriginal

        End If



    End Function
    Public Function LiquidarPago(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN



        ' comando

        Dim control As Framework.IU.IUComun.IctrlBasicoDN = sender
        Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

        Dim pagoOriginal As FN.GestionPagos.DN.PagoDN = control.DN

        '' solicitar al usuario los datos para efctuar el pago
        'MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarFormulario(fp, pagoOriginal, TipoNavegacion.Modal)



        'Dim fechaEfecto As DateTime = Now ' debiara de indicarlo el usuario ya que es un efecto manual
        'pagoOriginal.FechaEfecto = fechaEfecto


        ' invocamos la funcionalidad en el servidor
        Dim miias As New FN.GestionPagos.AS.GestionPagosAS
        Return miias.LiquidarPago(pagoOriginal)





    End Function
    Public Function EfectuarPago(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN



        ' comando

        Dim control As Framework.IU.IUComun.IctrlBasicoDN = sender
        Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

        Dim pagoOriginal As FN.GestionPagos.DN.PagoDN = control.DN

        '' solicitar al usuario los datos para efctuar el pago
        'MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarFormulario(fp, pagoOriginal, TipoNavegacion.Modal)



        'Dim fechaEfecto As DateTime = Now ' debiara de indicarlo el usuario ya que es un efecto manual
        'pagoOriginal.FechaEfecto = fechaEfecto


        ' invocamos la funcionalidad en el servidor
        Dim miias As New FN.GestionPagos.AS.GestionPagosAS
        Return miias.EfectuarPago(pagoOriginal)





    End Function
    Public Function CompensarPagoEfectuado(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN
        ' comando

        Dim control As Framework.IU.IUComun.IctrlBasicoDN = sender

        ' obtenemos el formulario
        Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

        Dim pagoOriginal As FN.GestionPagos.DN.PagoDN = control.DN

        ' crear el pago compensado
        Dim param As FN.GestionPagos.DN.PagoDN = pagoOriginal.CrearPagoCompensador
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarFormulario(fp, param, TipoNavegacion.Modal)

        ' invocamos la funcionalidad en el servidor
        Dim miias As New FN.GestionPagos.AS.GestionPagosAS
        Return miias.CompensarPago(param)


    End Function


    Public Function AnularPago(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN
        ' comando

        Dim control As Framework.IU.IUComun.IctrlBasicoDN = sender

        ' obtenemos el formulario
        Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

        Dim pagoOriginal As FN.GestionPagos.DN.PagoDN = control.DN



        'Dim mensaje As String
        'If Not pagoOriginal.AnularPago(mensaje) Then
        '    Throw New ApplicationException(mensaje)
        'End If


        ' invocamos la funcionalidad en el servidor
        Dim miias As New FN.GestionPagos.AS.GestionPagosAS
        Return miias.AnularPago(pagoOriginal)


    End Function



    Public Function AnularPagosNoEmitidosYCrearPagoAgrupador(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN
        ' comando

        Dim control As Framework.IU.IUComun.IctrlBasicoDN = sender

        ' obtenemos el formulario
        Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

        Dim dn As Object = control.DN
        Dim unPagoDelImporteDebido As FN.GestionPagos.DN.PagoDN

        If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsColeccion(dn.GetType) Then
            Dim PrimerElemento As Object = dn(0)

            If TypeOf PrimerElemento Is Framework.DatosNegocio.HEDN Then
                Dim mias As New Framework.AS.DatosBasicosAS
                Dim hedn As Framework.DatosNegocio.HEDN = mias.RecuperarGenerico(PrimerElemento)
                unPagoDelImporteDebido = hedn.EntidadReferida

            Else
                unPagoDelImporteDebido = PrimerElemento

            End If


        End If



        Dim miias As New FN.GestionPagos.AS.GestionPagosAS
        Dim pagoAgrupador As FN.GestionPagos.DN.PagoDN = miias.CrearPagoAgrupadorProvisional(unPagoDelImporteDebido)

        ' mostramos el apgo para que sea complimentado por el usuario
        Dim paquete As Hashtable = MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarFormulario(fp, pagoAgrupador, TipoNavegacion.Modal)


        ' verificamos si el usuario a haceptado para continuar con la anulacion
        If paquete.ContainsKey("Resultado") AndAlso paquete("Resultado") = 0 Then

            ' splicitamos ala anulacion
            Return miias.AnularPagosNoEmitidosYCrearPagoAgrupador(pagoAgrupador)
        Else
            Return Nothing

        End If

    End Function

End Class

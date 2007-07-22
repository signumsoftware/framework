Imports Framework.Procesos


Public Class NotificacionesPagoCtrl

    Public Function AdjuntarNotaaPago(ByVal objeto As Object, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN, ByVal instanciaSolicitante As Object) As Framework.DatosNegocio.IEntidadBaseDN
        'Se comprueba que no se haya realizado ninguna modificación sobre el objeto
        Dim objEnt As Framework.DatosNegocio.IEntidadBaseDN = objeto
        If objEnt.Estado <> Framework.DatosNegocio.EstadoDatosDN.SinModificar Then
            Throw New ApplicationException("No se puede modificar la entidad en esta operación")
        End If

        'instanciaSolicitante debe ser un control o un formulario p
        Dim nombreTitulo As String = "Motivo de " & pTransicionRealizada.Transicion.OperacionDestino.Nombre
        Dim comentario As String = InputBox("Indique el motivo para realizar esta operación", nombreTitulo)

        If Not String.IsNullOrEmpty(comentario) Then

            Dim NotificacionPago As GestionPagos.DN.NotificacionPagoDN
            NotificacionPago = New GestionPagos.DN.NotificacionPagoDN(pTransicionRealizada.OperacionRealizadaOrigen.SujetoOperacion, pTransicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion, DN.OrigenNotificacion.Manual, nombreTitulo, comentario)


            ' MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarFormulario(instanciaSolicitante, NotificacionPago, MotorIU.Motor.TipoNavegacion.Modal)
            NotificacionPago.Origen = DN.OrigenNotificacion.Manual

            Dim proceso As New ProcesosAS.OperacionesAS
            Return proceso.EjecutarOperacion(objeto, pTransicionRealizada, Nothing)
        Else


            Return objeto
            'Dim pago As GestionPagos.DN.PagoDN = pTransicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion
            'pTransicionRealizada.OperacionRealizadaOrigen = Nothing

            'Return pago

        End If



    End Function

    Public Function AdjuntarNotaaPagoModificarObjeto(ByVal objeto As Object, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN, ByVal instanciaSolicitante As Object) As Framework.DatosNegocio.IEntidadBaseDN

        'instanciaSolicitante debe ser un control o un formulario p

        Dim nombreTitulo As String = "Motivo de " & pTransicionRealizada.Transicion.OperacionDestino.Nombre
        Dim comentario As String = InputBox("Indique el motivo para realizar esta operación", nombreTitulo)


        If Not String.IsNullOrEmpty(comentario) Then

            Dim NotificacionPago As GestionPagos.DN.NotificacionPagoDN
            NotificacionPago = New GestionPagos.DN.NotificacionPagoDN(pTransicionRealizada.OperacionRealizadaOrigen.SujetoOperacion, pTransicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion, DN.OrigenNotificacion.Manual, nombreTitulo, comentario)


            ' MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarFormulario(instanciaSolicitante, NotificacionPago, MotorIU.Motor.TipoNavegacion.Modal)
            NotificacionPago.Origen = DN.OrigenNotificacion.Manual

            Dim proceso As New ProcesosAS.OperacionesAS
            Return proceso.EjecutarOperacion(objeto, pTransicionRealizada, Nothing)
        Else


            Return Nothing
            Dim pago As GestionPagos.DN.PagoDN = pTransicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion
            pTransicionRealizada.OperacionRealizadaOrigen = Nothing

            Return pago

        End If



    End Function

    'Dim f As MotorIU.FormulariosP.IFormularioP
    'Dim cp As MotorIU.ControlesP.IControlP

    'If TypeOf instanciaSolicitante Is MotorIU.FormulariosP.IFormularioP Then
    '    f = instanciaSolicitante
    '    MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarFormulario(f, GetType(GestionPagos.DN.NotificacionPagoDN), MotorIU.Motor.TipoNavegacion.Modal)

    'End If

    'If TypeOf instanciaSolicitante Is MotorIU.ControlesP.IControlP Then
    '    cp = instanciaSolicitante
    '    MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarFormulario(cp, GetType(GestionPagos.DN.NotificacionPagoDN), MotorIU.Motor.TipoNavegacion.Modal)

    'End If





End Class

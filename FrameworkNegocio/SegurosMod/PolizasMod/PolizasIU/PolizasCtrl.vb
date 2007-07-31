Imports Framework.IU.IUComun

Public Class PolizasCtrl
    'Public Function CrearReclamacion(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As FN.Seguros.Polizas.DN.PresupuestoDN

    '    ' comando

    '    Dim control As IctrlBasicoDN = sender
    '    Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

    '    Dim siniestro As FN.Seguros.Polizas.DN.SiniestroDN = control.DN

    '    Dim reclamacion As New FN.Seguros.Polizas.DN.ReclamacionDN
    '    reclamacion.Siniestro = siniestro

    '    ' obteneos el principal del marco
    '    ' Dim prin As Framework.Usuarios.DN.PrincipalDN = fp.cMarco.Principal

    '    presupuesto = lnc.VerificarDatosPresupuesto(presupuesto)



    '    'Dim paquete As New Hashtable
    '    'paquete.Add("DN", nota)

    '    '' navegar al formaulario que permite editar la nota
    '    'fp.cMarco.Navegar("FG", fp, Nothing, TipoNavegacion.Normal, paquete)


    '    Return presupuesto

    'End Function


    Public Function CrearAlertaGenerico(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN

        ' comando

        Dim control As IctrlBasicoDN = sender

        Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

        Dim col As New List(Of Framework.DatosNegocio.IEntidadDN)
        col.Add(control.DN)


        Dim lnc As New Framework.OperProg.OperProgLNC.OperProgLNC

        ' obteneos el principal del marco
        Dim prin As Framework.Usuarios.DN.PrincipalDN = fp.cMarco.Principal
        Dim Alerta As Framework.OperProg.OperProgDN.AlertaDN = lnc.CrearAlertaPara(prin.UsuarioDN, col)





        Dim paquete As New Hashtable
        paquete.Add("DN", Alerta)

        ' navegar al formaulario que permite editar la nota
        fp.cMarco.Navegar("FG", fp, Nothing, TipoNavegacion.Normal, paquete)


        Return control.DN

    End Function



    Public Function CrearNotaGenerico(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN

        ' comando

        Dim control As IctrlBasicoDN = sender

        Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

        Dim col As New List(Of Framework.DatosNegocio.IEntidadDN)
        col.Add(control.DN)


        Dim lnc As New Framework.Notas.NotasLNC.NotasLNC

        ' obteneos el principal del marco
        Dim prin As Framework.Usuarios.DN.PrincipalDN = fp.cMarco.Principal
        Dim nota As Framework.Notas.NotasDN.NotaDN = lnc.CrearNotaPara(prin.UsuarioDN, col)


        Debug.WriteLine(prin.UsuarioDN.Estado)


        Dim paquete As New Hashtable
        paquete.Add("DN", nota)

        ' navegar al formaulario que permite editar la nota
        fp.cMarco.Navegar("FG", fp, Nothing, TipoNavegacion.Normal, paquete)


        Return control.DN

    End Function

    Public Function CrearNotaPara(ByVal sender As Object, ByVal datos As Object, ByVal vm As Framework.TiposYReflexion.DN.VinculoMetodoDN) As Framework.DatosNegocio.IEntidadDN

        ' comando

        Dim control As IctrlBasicoDN = sender

        Dim fp As MotorIU.FormulariosP.IFormularioP = CType(control, System.Windows.Forms.ContainerControl).ParentForm

        Dim col As New List(Of Framework.DatosNegocio.IEntidadDN)
        col.Add(control.DN)


        Dim lnc As New Framework.Notas.NotasLNC.NotasLNC

        ' obteneos el principal del marco
        Dim prin As Framework.Usuarios.DN.PrincipalDN = fp.cMarco.Principal
        Dim nota As Framework.Notas.NotasDN.NotaDN = lnc.CrearNotaPara(prin.UsuarioDN, col)




        ' registrar la entidad de relacion con la nota
        'Dim npp As New FN.Seguros.Polizas.DN.NotaPolizaDN
        'npp.AsignarPeridoRenovacion(control.DN)
        'npp.Nota = nota
        ' nota.ColIHEntidad.Add(New Framework.DatosNegocio.HEDN(npp, Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir))
        ' TODO: revisar la linea anteriro hace que pete el id de la huella


        Dim paquete As New Hashtable
        paquete.Add("DN", nota)

        ' navegar al formaulario que permite editar la nota
        fp.cMarco.Navegar("FG", fp, Nothing, TipoNavegacion.Normal, paquete)


        Return control.DN

    End Function




    Public Function BajaPoliza(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object) As Framework.DatosNegocio.IEntidadBaseDN

        ' operación



        '' obtenemos el formulario
        Dim fp As MotorIU.FormulariosP.IFormularioP = pParametros


        Dim miBajaPolizaPr As New FN.Seguros.Polizas.DN.BajaPolizaPr
        miBajaPolizaPr.pr = objeto

        Dim paquete As New Hashtable
        paquete.Add("DN", miBajaPolizaPr)

        ' navegar al formaulario que permite establecer la fecha de baja
        fp.cMarco.Navegar("FG", fp, CType(fp, System.Windows.Forms.Form).ParentForm, TipoNavegacion.Modal, paquete)


        ' solicita la ejecución de la operacion
        Dim miProcesoLNC As New Framework.Procesos.ProcesosLNC.ProcesoLNC
        ' Return miProcesoLNC.EjecutarOperacionEnServidor(fp.cMarco.Principal, pTransicionRealizada, miBajaPolizaPr.pr, miBajaPolizaPr.FechaBajaPropuesta)

        Dim he As Framework.DatosNegocio.HEDN = New Framework.DatosNegocio.HEDN(miBajaPolizaPr.pr)
        he.EliminarEntidadReferida()
        'pTransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion = he
        pTransicionRealizada.OperacionRealizadaOrigen.AsignarOIenGrafo(he)
        'pTransicionRealizada.OperacionRealizadaOrigen.OperacionPadre.ObjetoIndirectoOperacion = he
        Return miProcesoLNC.EjecutarOperacionEnServidor(fp.cMarco.Principal, pTransicionRealizada, Nothing, miBajaPolizaPr.FechaBajaPropuesta)



    End Function




    Public Function AltaReclamacionDesdeSiniestro(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object) As Framework.DatosNegocio.IEntidadBaseDN

        ' operación



        '' obtenemos el formulario
        Dim fp As MotorIU.FormulariosP.IFormularioP = pParametros


        Dim miSiniestro As New FN.Seguros.Polizas.DN.SiniestroDN
        Dim miReclamacion As New FN.Seguros.Polizas.DN.ReclamacionDN
        miReclamacion.AltaDesdeSiniestro(miSiniestro)

        Dim paquete As New Hashtable
        paquete.Add("DN", miReclamacion)

        ' navegar al formaulario que permite establecer la fecha de baja
        fp.cMarco.Navegar("FG", fp, CType(fp, System.Windows.Forms.Form).ParentForm, TipoNavegacion.Modal, paquete)


        ' solicita la ejecución de la operacion
        Dim miProcesoLNC As New Framework.Procesos.ProcesosLNC.ProcesoLNC
        Return miProcesoLNC.EjecutarOperacionEnServidor(fp.cMarco.Principal, pTransicionRealizada, miReclamacion, Nothing)


    End Function




End Class

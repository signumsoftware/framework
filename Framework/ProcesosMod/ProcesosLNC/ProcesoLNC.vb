Public Class ProcesoLNC

    Public Function EjecutarOperacionEnServidor(ByVal pActor As Framework.Usuarios.DN.PrincipalDN, ByVal tranR As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pDn As Framework.DatosNegocio.IEntidadDN, ByVal pParametros As Object) As Object
        Dim miOperacionesAS As New Framework.Procesos.ProcesosAS.OperacionesAS
        Return miOperacionesAS.EjecutarOperacionModificarObjeto(pDn, tranR, pParametros)
    End Function



    Public Function EjecutarOperacionLNC(ByVal pActor As Framework.Usuarios.DN.PrincipalDN, ByVal tranR As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pDn As Framework.DatosNegocio.IEntidadDN, ByVal pParametros As Object) As Object



        ' podamos A las operaciones que tine autorizadas el principla
        Dim operacion As Framework.Procesos.ProcesosDN.OperacionDN = pActor.ColOperaciones.RecuperarXGUID(tranR.Transicion.OperacionDestino.GUID)


        If operacion Is Nothing Then
            Throw New ApplicationException("Debería haberse recuperado una operacion")
        End If


        Dim miIRecuperadorEjecutoresDeCliente As New Framework.Procesos.ProcesosAS.RecuperadorEjecutoresDeClienteAS
        ' ejecutar el metodo del controlador


        EjecutarOperacionLNC = miIRecuperadorEjecutoresDeCliente.EjecutarMethodInfo(CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente"), String), pParametros, tranR, pDn) ' para que se usa la instancia solicitante


    End Function

    Public Function RecuperarOperacionesAutorizadasSobreLNC(ByVal pDN As Framework.DatosNegocio.IEntidadDN) As Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN

        ' las operaciones introducidas por el gestor de flujos
        If Not pDN Is Nothing Then
            Dim coltran As Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN

            Dim miOperacionesAS As New Framework.Procesos.ProcesosAS.OperacionesAS
            Dim he As Framework.DatosNegocio.HEDN

            he = New Framework.DatosNegocio.HEDN(pDN)
            coltran = miOperacionesAS.RecuperarOperacionesAutorizadasSobre(he)

            Dim tranEliminadas As New Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN


            For Each transicion As Framework.Procesos.ProcesosDN.TransicionRealizadaDN In coltran
                If Not transicion.Transicion.Automatica AndAlso Not transicion.EsFinalizacion Then
                    tranEliminadas.Add(transicion)
                End If
            Next

            Dim coltranrespuesta As New Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN
            coltranrespuesta.AddRange(coltran.EliminarEntidadDN(tranEliminadas, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos))
            Return coltranrespuesta


        End If

        Return Nothing
    End Function

    Public Function RecuperarTransicionesDeInicio(ByVal pTipo As System.Type) As Framework.Procesos.ProcesosDN.ColTransicionDN

        Dim miOperacionesAS As New Framework.Procesos.ProcesosAS.OperacionesAS
        Return miOperacionesAS.RecuperarTransicionesDeInicio(pTipo)


    End Function



End Class

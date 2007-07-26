Imports FN.Empresas.DN
Imports Framework.IU.IUComun

Public Class EmpresasControlador

    ''' <summary>
    ''' Función que navega la formulario de usuarios. Si ya existe un usuario asignado al empleado, se
    ''' recupera este PrincipalDN, sino se crea un nuevo PrincipalDN asociado al empleado
    ''' </summary>
    ''' <param name="control"></param>
    ''' <param name="empPuestoR"></param>
    ''' <param name="vincMetodo"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function NavegarUsuarioEmpleado(ByVal control As Object, ByVal empPuestoR As EmpleadoYPuestosRDN, ByVal vincMetodo As Object) As EmpleadoYPuestosRDN
        Dim usuariosAS As New Framework.Usuarios.IUWin.AS.UsuariosAS()
        Dim principal As Framework.Usuarios.DN.PrincipalDN = Nothing

        If empPuestoR Is Nothing Then
            Throw New ApplicationException("No se puede asignar usuario a un empleado nulo")
        End If

        Dim empAS As New FN.Empresas.AS.EmpresaAS()
        empPuestoR = empAS.GuardarEmpleadoYPuestosR(Nothing, empPuestoR, Nothing)

        If Not String.IsNullOrEmpty(empPuestoR.ID) Then
            principal = usuariosAS.RecuperarPrincipalxEntidadUser(GetType(HuellaCacheEmpleadoYPuestosRDN), empPuestoR.ID)
        End If

        If principal Is Nothing Then
            principal = New Framework.Usuarios.DN.PrincipalDN()
            principal.UsuarioDN = New Framework.Usuarios.DN.UsuarioDN()
            principal.UsuarioDN.HuellaEntidadUserDN = New HuellaCacheEmpleadoYPuestosRDN(empPuestoR, Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional.ninguna)
        End If

        'Navegar al formulario de usuarios pasando la DN del usuario
        Dim paquete As New Hashtable()
        paquete.Add("DN", principal)
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarFormulario(CType(control, MotorIU.ControlesP.IControlP), paquete, TipoNavegacion.CerrarLanzador)

        Return empPuestoR

    End Function

    ''' <summary>
    ''' se navega a los puestos existentes para la empresa del empleado. Con el puesto seleccionado, 
    ''' se genera un PuestoRealizado para el empleado pasado como parámetro
    ''' </summary>
    ''' <param name="empPuestoR"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function AgregarPuestoEmpleado(ByVal control As Object, ByVal empPuestoR As EmpleadoYPuestosRDN, ByVal vincMetodo As Object) As EmpleadoYPuestosRDN
        If empPuestoR Is Nothing OrElse empPuestoR.Empleado Is Nothing Then
            Throw New ApplicationException("No se puede asignar un puesto a un empleado nulo")
        End If

        Dim paquete As New Hashtable()

        Dim miPaqueteFormularioBusqueda As New MotorBusquedaDN.PaqueteFormularioBusqueda()
        miPaqueteFormularioBusqueda.MultiSelect = False
        miPaqueteFormularioBusqueda.Agregable = False

        If empPuestoR.Empleado.SedeEmpresa IsNot Nothing AndAlso empPuestoR.Empleado.SedeEmpresa.Empresa IsNot Nothing AndAlso empPuestoR.Empleado.SedeEmpresa.Empresa.EntidadFiscal IsNot Nothing AndAlso empPuestoR.Empleado.SedeEmpresa.Empresa.EntidadFiscal.IentidadFiscal IsNot Nothing AndAlso Not String.IsNullOrEmpty(empPuestoR.Empleado.SedeEmpresa.Empresa.EntidadFiscal.ValorCifNif) Then
            Dim condicionFiltro As New MotorBusquedaBasicasDN.ValorCampo()
            condicionFiltro.NombreCampo = "CifEmpresa"
            condicionFiltro.Operador = MotorBusquedaBasicasDN.OperadoresAritmeticos.igual
            condicionFiltro.Valor = empPuestoR.Empleado.SedeEmpresa.Empresa.EntidadFiscal.IentidadFiscal

            miPaqueteFormularioBusqueda.ListaValores.Add(condicionFiltro)

            paquete.Add("PaqueteFormularioBusqueda", miPaqueteFormularioBusqueda)
        End If

        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(CType(control, MotorIU.ControlesP.IControlP), GetType(PuestoDN), paquete, TipoNavegacion.Modal, Nothing)

        'TODO: Pasar a un método en EmpresasLNC
        If paquete IsNot Nothing AndAlso paquete.Contains("ID") AndAlso Not String.IsNullOrEmpty(paquete.Item("ID")) Then
            Dim objAS As New Framework.AS.DatosBasicosAS()
            Dim puesto As PuestoDN
            puesto = objAS.RecuperarGenerico(paquete.Item("ID"), GetType(PuestoDN))

            If puesto IsNot Nothing Then
                If empPuestoR.ColPuestoRealizado Is Nothing Then
                    empPuestoR.ColPuestoRealizado = New ColPuestoRealizadoDN()
                End If

                If Not empPuestoR.ColPuestoRealizado.ContienePuesto(puesto) Then
                    Dim pr As New PuestoRealizadoDN()
                    pr.Puesto = puesto
                    pr.Empleado = empPuestoR.Empleado
                    empPuestoR.ColPuestoRealizado.Add(pr)
                End If
            Else
                Throw New ApplicationException("No se puede asignar el puesto")
            End If
        End If

        Return empPuestoR

    End Function
End Class

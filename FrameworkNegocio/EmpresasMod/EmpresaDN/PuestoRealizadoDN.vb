
<Serializable()> _
Public Class PuestoRealizadoDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

#Region "Atributos"

    Protected mPuesto As PuestoDN
    Protected mEmpleado As EmpleadoDN
    'Protected mColPermisos As Framework.Usuarios.DN.ColPermisoDN

#End Region

#Region "Propiedades"

    Public Property Puesto() As PuestoDN
        Get
            Return Me.mPuesto
        End Get
        Set(ByVal value As PuestoDN)
            Me.CambiarValorRef(Of PuestoDN)(value, mPuesto)
        End Set
    End Property

    Public Property Empleado() As EmpleadoDN
        Get
            Return Me.mEmpleado
        End Get
        Set(ByVal value As EmpleadoDN)
            Me.CambiarValorRef(Of EmpleadoDN)(value, mEmpleado)
        End Set
    End Property

    'Public Property ColPermisos() As Framework.Usuarios.DN.ColPermisoDN
    '    Get
    '        Return mColPermisos
    '    End Get
    '    Set(ByVal value As Framework.Usuarios.DN.ColPermisoDN)
    '        CambiarValorCol(Of Framework.Usuarios.DN.ColPermisoDN)(value, mColPermisos)
    '    End Set
    'End Property

#End Region

#Region "Validaciones"

    Private Function ValidarEmpleado(ByRef mensaje As String, ByVal empleado As EmpleadoDN) As Boolean
        If empleado Is Nothing Then
            mensaje = "El empleado no puede ser nulo"
            Return False
        End If

        Return True
    End Function

    Private Function ValidarPuesto(ByRef mensaje As String, ByVal puesto As PuestoDN) As Boolean
        If puesto Is Nothing Then
            mensaje = "El puesto no puede ser nulo"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarEmpleado(pMensaje, mEmpleado) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarPuesto(pMensaje, mPuesto) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Function RecuperarColPermisos() As Framework.Usuarios.DN.ColPermisoDN
        Dim colRolesPuestos As Framework.Usuarios.DN.ColRolDN
        RecuperarColPermisos = New Framework.Usuarios.DN.ColPermisoDN()

        If mPuesto IsNot Nothing Then
            colRolesPuestos = Me.mPuesto.ColRoles

            If colRolesPuestos IsNot Nothing Then
                RecuperarColPermisos.AddRange(colRolesPuestos.RecuperarColPermisos())
            End If
        End If

    End Function

#End Region

End Class


<Serializable()> _
Public Class ColPuestoRealizadoDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of PuestoRealizadoDN)

#Region "Métodos"

    Public Function RecuperarColOperaciones() As Framework.Procesos.ProcesosDN.ColOperacionDN
        RecuperarColOperaciones = New Framework.Procesos.ProcesosDN.ColOperacionDN

        For Each pr As PuestoRealizadoDN In Me
            RecuperarColOperaciones.AddRange(pr.Puesto.ColRoles.RecuperarColOperaciones())
        Next

    End Function

    Public Function RecuperarColRoles() As Framework.Usuarios.DN.ColRolDN
        RecuperarColRoles = New Framework.Usuarios.DN.ColRolDN()

        For Each pr As PuestoRealizadoDN In Me
            RecuperarColRoles.AddRange(pr.Puesto.ColRoles)
        Next

    End Function

    Public Function RecuperarColPermisos() As Framework.Usuarios.DN.ColPermisoDN
        RecuperarColPermisos = New Framework.Usuarios.DN.ColPermisoDN()

        For Each puestoR As PuestoRealizadoDN In Me
            RecuperarColPermisos.AddRange(puestoR.RecuperarColPermisos)
        Next
    End Function

    Public Function ComprobarEmpleado(ByVal empleado As EmpleadoDN, ByRef mensaje As String) As Boolean
        If empleado Is Nothing Then
            Return False
        End If


        If empleado.EstadoIntegridad(mensaje) <> Framework.DatosNegocio.EstadoIntegridadDN.Consistente Then
            Return False
        End If

        For Each puestoRealizado As PuestoRealizadoDN In Me
            If puestoRealizado.Empleado Is Nothing OrElse Not puestoRealizado.Empleado.GUID = empleado.GUID Then
                Return False
            End If
        Next

        Return True
    End Function

    Public Function RecuperarColPRNoInterseccion(ByVal colPR As ColPuestoRealizadoDN) As ColPuestoRealizadoDN
        Dim respuesta As New ColPuestoRealizadoDN()

        If colPR Is Nothing Then
            respuesta = Me
        Else

            For Each puestoR As PuestoRealizadoDN In Me
                If Not colPR.Contiene(puestoR, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
                    respuesta.Add(puestoR)
                End If
            Next

        End If

        Return respuesta

    End Function

    Public Function ComprobarEmpresa(ByVal empresa As EmpresaDN) As Boolean
        For Each puestoR As PuestoRealizadoDN In Me
            If puestoR.Puesto.DepartamentoNTareaN.Departamento.Empresa.GUID <> empresa.GUID Then
                Return False
            End If
        Next

        Return True
    End Function

    Public Function ContienePuesto(ByVal puesto As PuestoDN) As Boolean
        If Not puesto Is Nothing Then
            For Each puestoR As PuestoRealizadoDN In Me
                If puestoR.Puesto.GUID = puesto.GUID Then
                    Return True
                End If
            Next
        End If

        Return False

    End Function

#End Region


End Class
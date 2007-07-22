Imports Framework.DatosNegocio
Imports Framework.Procesos.ProcesosDN

<Serializable()> Public Class EmpleadoYPuestosRDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements Framework.Usuarios.DN.IEjecutorOperacionRolDN


#Region "Atributos"

    Protected mEmpleado As EmpleadoDN
    Protected mColPuestoRealizado As ColPuestoRealizadoDN

    'Se cachea el GUID del empleado para evitar tener dos EmpleadoYPuestosRDN contra el mismo empleado
    Protected mGUIDEmpleado As String

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        mEmpleado = New EmpleadoDN()
        modificarEstado = EstadoDatosDN.Inconsistente
    End Sub

#End Region

#Region "Propiedades"

    Public Property ColPuestoRealizado() As ColPuestoRealizadoDN
        Get
            Return Me.mColPuestoRealizado
        End Get
        Set(ByVal value As ColPuestoRealizadoDN)
            Me.CambiarValorRef(Of ColPuestoRealizadoDN)(value, mColPuestoRealizado)
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

#End Region

#Region "Propiedades IEjecutorOperacionRolDN"

    Public ReadOnly Property ColOperacion() As ColOperacionDN Implements IEjecutorOperacionDN.ColOperaciones
        Get
            If mColPuestoRealizado IsNot Nothing Then
                Return mColPuestoRealizado.RecuperarColOperaciones()
            Else
                Return Nothing
            End If
        End Get
    End Property

    Public Property ColPermisos() As Framework.Usuarios.DN.ColPermisoDN Implements Framework.Usuarios.DN.IEjecutorOperacionRolDN.ColPermisos
        Get
            Dim colPermisosR As New Framework.Usuarios.DN.ColPermisoDN()
            Dim colPermisosE As New Framework.Usuarios.DN.ColPermisoDN()

            If mColPuestoRealizado IsNot Nothing Then
                colPermisosR = mColPuestoRealizado.RecuperarColPermisos()
            End If

            If mEmpleado IsNot Nothing AndAlso mEmpleado.ColPermisos IsNot Nothing Then
                colPermisosE.AddRange(mEmpleado.ColPermisos)
            End If

            Return colPermisosE.RecuperarColPermisosUnion(colPermisosR)
        End Get
        Set(ByVal value As Framework.Usuarios.DN.ColPermisoDN)
            Throw New NotImplementedException
        End Set
    End Property

    Public Property ColRoles() As Framework.Usuarios.DN.ColRolDN Implements Framework.Usuarios.DN.IEjecutorOperacionRolDN.ColRoles
        Get
            If mColPuestoRealizado IsNot Nothing Then
                Return mColPuestoRealizado.RecuperarColRoles()
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As Framework.Usuarios.DN.ColRolDN)
            Throw New NotImplementedException

        End Set
    End Property
#End Region

#Region "Validaciones"

    Private Function ValidarEmpleado(ByRef mensaje As String, ByVal empleado As EmpleadoDN) As Boolean
        If empleado Is Nothing Then
            mensaje = "El empleado no puede ser nula"
            Return False
        End If

        Return True
    End Function

    Private Function ValidarEmpleadoColPuestosR(ByRef mensaje As String, ByVal empleado As EmpleadoDN, ByVal colPuestosR As ColPuestoRealizadoDN) As Boolean
        If colPuestosR IsNot Nothing Then
            If Not colPuestosR.ComprobarEmpleado(empleado, mensaje) Then
                ' mensaje = "El empleado y la colección de puestos no concuerdan"
                Return False
            End If

            If Not colPuestosR.ComprobarEmpresa(mEmpleado.SedeEmpresa.Empresa) Then
                mensaje = "El empleado solo puede pertenecer a una empresa"
                Return False
            End If

        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarEmpleado(pMensaje, mEmpleado) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarEmpleadoColPuestosR(pMensaje, mEmpleado, mColPuestoRealizado) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        'Cacheamos el GUID del empleado
        mGUIDEmpleado = mEmpleado.GUID

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function

    Public Overrides Function ToString() As String
        If mEmpleado IsNot Nothing Then
            Return mEmpleado.ToString()
        End If

        Return String.Empty

    End Function

#End Region


End Class


<Serializable()> _
Public Class HuellaCacheEmpleadoYPuestosRDN
    Inherits HETCacheableDN(Of EmpleadoYPuestosRDN)
    Implements Framework.Usuarios.DN.IEjecutorOperacionRolDN

#Region "Atributos"

    Protected mColOperaciones As ColOperacionDN
    Protected mColPermisos As Framework.Usuarios.DN.ColPermisoDN
    Protected mColRoles As Framework.Usuarios.DN.ColRolDN
    Protected mNombreYApellidos As String
    Protected mNIFEmpleado As String

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pEntidad As EmpleadoYPuestosRDN, ByVal pRelacionIntegridad As Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional)
        MyBase.New(pEntidad, pRelacionIntegridad)
        AsignarDatosHuella(pEntidad)
    End Sub

    Public Sub New(ByVal pEntidad As EmpleadoYPuestosRDN, ByVal pRelacionIntegridad As Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional, ByVal toStringEntidad As String)
        MyBase.New(pEntidad, pRelacionIntegridad, toStringEntidad)
        AsignarDatosHuella(pEntidad)
    End Sub

#End Region

#Region "Propiedades"

    Public ReadOnly Property NombreYApellidos() As String
        Get
            Return mNombreYApellidos
        End Get
    End Property

    Public ReadOnly Property NIFEmpleado() As String
        Get
            Return mNIFEmpleado
        End Get
    End Property

#End Region

#Region "Propiedades IEjecutorOperacionRolDN"

    Public ReadOnly Property ColOperaciones() As ColOperacionDN Implements IEjecutorOperacionDN.ColOperaciones
        Get
            Return mColOperaciones
        End Get
    End Property

    Public Property ColPermisos() As Framework.Usuarios.DN.ColPermisoDN Implements Framework.Usuarios.DN.IEjecutorOperacionRolDN.ColPermisos
        Get
            Return mColPermisos
        End Get
        Set(ByVal value As Framework.Usuarios.DN.ColPermisoDN)
            Throw New NotImplementedException
        End Set
    End Property

    Public Property ColRoles() As Framework.Usuarios.DN.ColRolDN Implements Framework.Usuarios.DN.IEjecutorOperacionRolDN.ColRoles
        Get
            Return mColRoles
        End Get
        Set(ByVal value As Framework.Usuarios.DN.ColRolDN)
            Throw New NotImplementedException
        End Set
    End Property
#End Region

#Region "Métodos"

    Public Overrides Sub AsignarEntidadReferida(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN)
        Dim empleadoYPuesto As EmpleadoYPuestosRDN
        empleadoYPuesto = pEntidad

        AsignarDatosHuella(empleadoYPuesto)

        MyBase.AsignarEntidadReferida(empleadoYPuesto)
    End Sub

    Public Overrides Function ToString() As String
        Return mToSt
    End Function

    Private Sub AsignarDatosHuella(ByRef empyPuestoR As EmpleadoYPuestosRDN)

        If empyPuestoR IsNot Nothing AndAlso empyPuestoR.Empleado IsNot Nothing AndAlso empyPuestoR.Empleado.Persona IsNot Nothing AndAlso empyPuestoR.Empleado.Persona.NIF IsNot Nothing Then
            mColOperaciones = empyPuestoR.ColOperacion
            mNombreYApellidos = empyPuestoR.Empleado.Persona.NombreYApellidos
            mNIFEmpleado = empyPuestoR.Empleado.Persona.NIF.Codigo

            If empyPuestoR.ColPuestoRealizado IsNot Nothing Then
                mColRoles = empyPuestoR.ColPuestoRealizado.RecuperarColRoles()
                mColPermisos = empyPuestoR.ColPermisos
            End If

            mToSt = empyPuestoR.ToStringEntidad
        End If

    End Sub

#End Region


End Class
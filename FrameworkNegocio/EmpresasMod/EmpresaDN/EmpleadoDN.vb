Imports FN.Personas.DN

#Region "Importaciones"
Imports PersonasDN
Imports Framework.DatosNegocio
Imports Framework.DatosNegocio.Localizaciones.Temporales
#End Region

''' <summary>
''' 
''' 
''' </summary>
''' <remarks>
'''Esta clase se ocupará de validar que persona no sea nothing, y que ninguno
'''de los campos de PersonaDN sea Nothing. Por otra parte, como los empleados
'''pertenecen a una empresa, tendran una colección de roles que juegan en la
'''empresa por cada uno de los departamentos en los que se encuentran
'''
'''ATRIBUTOS
'''
'''   - mPersonaDN --> no puede ser nothing ,....
'''   - mColRolDepartamentoDN --> en principio no tiene ninguna restricción, y son los roles
'''     que una persona juega en un departamento
'''   - mSedeEmpresa --> De momento no tiene ninguna restrucción
''' </remarks>
''' 

<Serializable()> Public Class EmpleadoDN
    Inherits EntidadTemporalDN


#Region "Atributos"
    Protected mPersona As PersonaDN
    ' Protected mColRolDepartamentoDN As colRolDepartamentoDN
    Protected mSedeEmpresa As SedeEmpresaDN

    'Se cachen el  CIF de la empresa para validar que el mismo empleado no pueda estar repetido en una empresa
    'y el NIF de la persona para validar que una persona sea dos veces empleado de la misma empresa. Únicamente
    'se cachea la información en el momento de guardar en base de datos, en el estado de integridad
    Protected mCIFNIFEmpresa As String
    Protected mNIFPersona As String

    Protected mColPermisos As Framework.Usuarios.DN.ColPermisoDN

#End Region

#Region "Constructores"
    Public Sub New()
        MyBase.New()
    End Sub

    'Public Sub New(ByVal pPersonaDN As PersonaDN)
    '    Dim mensaje As String = ""

    '    If ValidarPersona(mensaje, pPersonaDN) Then
    '        Me.CambiarValorRef(Of PersonaDN)(pPersonaDN, mPersonaDN)
    '    Else
    '        Throw New Exception(mensaje)
    '    End If

    '    Me.modificarEstado = EstadoDatosDN.SinModificar
    'End Sub

    'Public Sub New(ByVal pPersonaDN As PersonaDN, ByVal pColRolDepartamentoDN As colRolDepartamentoDN, ByVal pPeriodo As LocalizacionesDN.Temporales.IntervaloFechasDN)
    '    Me.New(pPersonaDN, pColRolDepartamentoDN, Nothing, pPeriodo)
    '    Me.CambiarValorRef(Of LocalizacionesDN.Temporales.IntervaloFechasDN)(pPeriodo, mPeriodo)
    '    Me.modificarEstado = EstadoDatosDN.SinModificar
    'End Sub

    'Public Sub New(ByVal pPersonaDN As PersonaDN, ByVal pColRolDepartamentoDN As colRolDepartamentoDN, ByVal pSedeEmpresaDN As SedeEmpresaDN, ByVal pPeriodo As Framework.DatosNegocio.Localizaciones.Temporales.IntervaloFechasDN)
    '    Dim mensaje As String = ""

    '    If ValidarPersona(mensaje, pPersonaDN) Then
    '        Me.CambiarValorRef(Of PersonaDN)(pPersonaDN, mPersona)
    '    Else
    '        Throw New Exception(mensaje)
    '    End If

    '    'If ValidarColRolDepartamento(mensaje, pColRolDepartamentoDN) Then
    '    '    Me.CambiarValorRef(Of colRolDepartamentoDN)(pColRolDepartamentoDN, mColRolDepartamentoDN)
    '    'Else
    '    '    Throw New Exception(mensaje)
    '    'End If
    '    Me.CambiarValorRef(Of IntervaloFechasDN)(pPeriodo, mPeriodo)
    '    Me.CambiarValorRef(Of SedeEmpresaDN)(pSedeEmpresaDN, mSedeEmpresa)
    '    Me.modificarEstado = EstadoDatosDN.SinModificar
    'End Sub


#End Region

#Region "Propiedades"
    Public Property Persona() As PersonaDN
        Get
            Return mPersona
        End Get
        Set(ByVal value As PersonaDN)
            Me.CambiarValorRef(Of PersonaDN)(value, mPersona)
        End Set
    End Property

    Public Property SedeEmpresa() As SedeEmpresaDN
        Get
            Return mSedeEmpresa
        End Get
        Set(ByVal value As SedeEmpresaDN)
            Me.CambiarValorRef(Of SedeEmpresaDN)(value, mSedeEmpresa)
        End Set
    End Property

    Public ReadOnly Property EstadoModificacion() As Framework.DatosNegocio.EstadoDatosDN
        Get
            Return MyBase.Estado
        End Get
    End Property

    Public ReadOnly Property NombreYApellidos() As String
        Get
            Return Me.mPersona.NombreYApellidos
        End Get
    End Property

    Public Property ColPermisos() As Framework.Usuarios.DN.ColPermisoDN
        Get
            Return mColPermisos
        End Get
        Set(ByVal value As Framework.Usuarios.DN.ColPermisoDN)
            CambiarValorCol(Of Framework.Usuarios.DN.ColPermisoDN)(value, mColPermisos)
        End Set
    End Property

#End Region

#Region "Validaciones"

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="persona"></param>
    ''' <remarks>Todo empleado es una persona, por lo tanto, siempre debe estar presente</remarks>
    Private Function ValidarPersona(ByRef mensaje As String, ByVal persona As PersonaDN) As Boolean
        If persona Is Nothing OrElse persona.NIF Is Nothing OrElse String.IsNullOrEmpty(persona.NIF.Codigo) Then
            mensaje = "Un empleado debe contener a una persona con un NIF válido"
            Return False
        End If

        Return True
    End Function

    Private Function ValidarSedePrincipal(ByRef mensaje As String, ByVal sedeP As SedeEmpresaDN) As Boolean
        If sedeP Is Nothing Then
            mensaje = "El empleado debe tener una sede principal"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarPersona(pMensaje, mPersona) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarSedePrincipal(pMensaje, mSedeEmpresa) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        'Se cachea la información del NIF de la persona y el CIF de la empresa
        mNIFPersona = mPersona.NIF.Codigo
        If mSedeEmpresa.Empresa IsNot Nothing AndAlso mSedeEmpresa.Empresa.EntidadFiscal IsNot Nothing AndAlso mSedeEmpresa.Empresa.EntidadFiscal.IentidadFiscal.IdentificacionFiscal IsNot Nothing Then
            mCIFNIFEmpresa = mSedeEmpresa.Empresa.EntidadFiscal.IentidadFiscal.IdentificacionFiscal.Codigo
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function

    Public Function EmpleadoEnEmpresa(ByVal pEmpresa As EmpresaDN) As Boolean
        'Dim e As RolDepartamentoDN
        'For Each e In mColRolDepartamentoDN
        '    If e.DepartamentoDN.EmpresaDN Is pEmpresa Then
        '        Return True
        '    End If
        'Next
        Throw New NotImplementedException
        Return False
    End Function

    Public Overrides Function ToString() As String
        Dim cadena As String = ""

        If mPersona IsNot Nothing Then
            cadena = mPersona.ToString()
        End If

        If mSedeEmpresa IsNot Nothing AndAlso mSedeEmpresa.Empresa IsNot Nothing AndAlso mSedeEmpresa.Empresa.EntidadFiscal IsNot Nothing AndAlso mSedeEmpresa.Empresa.EntidadFiscal.IentidadFiscal.IdentificacionFiscal IsNot Nothing Then
            cadena = cadena & ", " & mSedeEmpresa.Empresa.ToString()
        End If

        Return cadena

    End Function

#End Region

#Region "Eventos"
    Public Overrides Sub ElementoaEliminar(ByVal pSender As Object, ByVal pElemento As Object, ByRef pPermitido As Boolean)
        'If pSender Is Me.colRolDepartamentoDN Then
        '    Dim mensaje As String = ""
        '    Dim ColRolDepartamento As colRolDepartamentoDN
        '    ColRolDepartamento = pSender
        '    If ColRolDepartamento.Count = 1 Then
        '        Throw New Exception(mensaje)
        '    End If
        'End If
        MyBase.ElementoaEliminar(pSender, pElemento, pPermitido)
    End Sub

#End Region

End Class

<Serializable()> Public Class ColEmpleadosDN
    Inherits ArrayListValidable(Of EmpleadoDN)

    ' métodos de colección

    Public Function EmpleadosEnSede(ByVal pSede As SedeEmpresaDN) As ColEmpleadosDN
        Dim emple As EmpleadoDN
        EmpleadosEnSede = New ColEmpleadosDN

        For Each emple In Me
            If emple.SedeEmpresa.Equals(pSede) Then
                EmpleadosEnSede.Add(emple)
            End If
        Next

    End Function


End Class
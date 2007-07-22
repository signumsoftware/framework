#Region "Importaciones"
Imports Framework.DatosNegocio
Imports FN.Localizaciones.DN
#End Region

''' <summary>
''' 
''' </summary>
''' <remarks>
'''Esta clase únicamente guarda los datos básicos de una persona, sin validar
'''ninguno de ellos, por ejemplo, no tiene Fecha de nacimiento, que sin embargo
'''depende de para que lo utilicemos lo necesitariamos, pero lo suyo sería
'''ponerlo en otra clase que contenga dicho dato.
'''  - mContacto --> Establece los contactos de la persona, que estan en LocalizacionesDN
'''  - mApellido --> Apellido de la persona     
'''  - mNIF --> Es el número unívoco para la persona
''' 
''' Ninguno de estos atributos son obligatorios</remarks>
''' 
<Serializable()> Public Class PersonaDN
    Inherits EntidadTemporalDN

#Region "Atributos"
    Protected mNIF As NifDN
    Protected mApellido As String
    Protected mApellido2 As String
    Protected mSexo As TipoSexo
    Protected mFechaNacimiento As DateTime
#End Region

#Region "Constructores"
    Public Sub New()
        MyBase.New()
        Me.CambiarValorRef(Of NifDN)(New NifDN(), mNIF)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

    Public Sub New(ByVal nombre As String, ByVal apellido1 As String, ByVal apellido2 As String, ByVal fechaNac As Date)
        CambiarValorVal(Of String)(nombre, mNombre)
        CambiarValorVal(Of String)(apellido1, mApellido)
        CambiarValorVal(Of String)(apellido2, mApellido2)
        CambiarValorVal(Of DateTime)(fechaNac, mFechaNacimiento)

        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

    Public Sub New(ByVal nombre As String, ByVal apellido1 As String, ByVal apellido2 As String, ByVal fechaNac As Date, ByVal nif As NifDN)
        CambiarValorVal(Of String)(nombre, mNombre)
        CambiarValorVal(Of String)(apellido1, mApellido)
        CambiarValorVal(Of String)(apellido2, mApellido2)
        CambiarValorVal(Of DateTime)(fechaNac, mFechaNacimiento)
        
        Me.CambiarValorRef(Of NifDN)(nif, mNIF)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"
    Public Property FechaNacimiento() As DateTime
        Get
            Return mFechaNacimiento
        End Get
        Set(ByVal value As DateTime)
            Me.CambiarValorVal(Of DateTime)(value, Me.mFechaNacimiento)
        End Set
    End Property

    Public Property Apellido2() As String
        Get
            Return (Me.mApellido2)
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mApellido2)
        End Set
    End Property

    Public Property Sexo() As TipoSexo
        Get
            Return mSexo
        End Get
        Set(ByVal value As TipoSexo)
            Me.CambiarValorRef(Of TipoSexo)(value, mSexo)
        End Set
    End Property

    Public Property NIF() As NifDN
        Get
            Return mNIF
        End Get
        Set(ByVal value As NifDN)
            Me.CambiarValorRef(Of NifDN)(value, mNIF)
        End Set
    End Property

    Public Property Apellido() As String
        Get
            Return mApellido
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mApellido)
        End Set
    End Property

    Public ReadOnly Property EstadoModificacion() As Framework.DatosNegocio.EstadoDatosDN
        Get
            Return MyBase.Estado
        End Get
    End Property

    'Public Property Contacto() As ContactoDN
    '    Get
    '        '    Return mContacto
    '    End Get
    '    Set(ByVal value As ContactoDN)
    '        '  Me.CambiarValorVal(Of ContactoDN)(value, mContacto)
    '    End Set
    'End Property

    Public ReadOnly Property NombreYApellidos() As String
        Get
            Return String.Concat(Me.mNombre, " ", Me.mApellido, " ", Me.mApellido2)
        End Get
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarNombre(ByRef mensaje As String, ByVal nombre As String) As Boolean
        If String.IsNullOrEmpty(nombre) Then
            mensaje = "El nombre no puede ser nulo"
            Return False
        End If

        Return True

    End Function

    Private Function ValidarApellido(ByRef mensaje As String, ByVal apellido As String) As Boolean
        If String.IsNullOrEmpty(apellido) Then
            mensaje = "El apellido no puede ser nulo"
            Return False
        End If

        Return True

    End Function

    'Private Function ValidarNIF(ByRef mensaje As String, ByVal nif As NifDN) As Boolean
    '    If nif Is Nothing Then
    '        mensaje = "El NIF no puede ser nulo"
    '        Return False
    '    Else
    '        If nif.EstadoIntegridad(mensaje) <> EstadoIntegridadDN.Consistente Then
    '            Return False
    '        End If
    '    End If

    '    Return True

    'End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarNombre(pMensaje, mNombre) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarApellido(pMensaje, mApellido) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function

    Public Overrides Function ToString() As String
        If mNIF IsNot Nothing AndAlso Not String.IsNullOrEmpty(mNIF.Codigo) Then
            Return NombreYApellidos & " NIF: " & mNIF.Codigo
        Else
            Return NombreYApellidos
        End If
    End Function

#End Region

End Class


<Serializable()> _
Public Class PersonaFiscalDN
    Inherits EntidadTemporalDN
    Implements IEntidadFiscalDN


#Region "Atriibutos"
    Protected mPersona As PersonaDN
    Protected mDireccionFiscal As DireccionNoUnicaDN
    Protected mEntidadFiscalGenerica As Localizaciones.DN.EntidadFiscalGenericaDN

#End Region



    Public Sub New()
        Me.CambiarValorRef(Of Localizaciones.DN.EntidadFiscalGenericaDN)(New Localizaciones.DN.EntidadFiscalGenericaDN, mEntidadFiscalGenerica)
        mEntidadFiscalGenerica.IentidadFiscal = Me
        Me.modificarEstado = EstadoDatosDN.Inconsistente
    End Sub

#Region "Propiedades"

    Public Property Persona() As PersonaDN
        Get
            Return mPersona
        End Get
        Set(ByVal value As PersonaDN)
            CambiarValorRef(Of PersonaDN)(value, mPersona)
        End Set
    End Property

#End Region

#Region "Propiedades IEntidadFiscalDN"

    Public ReadOnly Property Correcta() As Boolean Implements IEntidadFiscalDN.Correcta
        Get
            Dim mensaje As String = ""
            If Me.EstadoIntegridad(mensaje) = EstadoIntegridadDN.Consistente Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    Public ReadOnly Property DenominacionFiscal() As String Implements IEntidadFiscalDN.DenominacionFiscal
        Get
            If mPersona IsNot Nothing Then
                Return mPersona.NombreYApellidos

            End If
        End Get
    End Property

    Public Property DomicilioFiscal() As DireccionNoUnicaDN Implements IEntidadFiscalDN.DomicilioFiscal
        Get
            Return mDireccionFiscal
        End Get
        Set(ByVal value As DireccionNoUnicaDN)
            CambiarValorRef(Of DireccionNoUnicaDN)(value, mDireccionFiscal)
        End Set
    End Property

    Public Property IdentificacionFiscal() As IIdentificacionFiscal Implements IEntidadFiscalDN.IdentificacionFiscal
        Get
            Return mPersona.NIF
        End Get
        Set(ByVal value As IIdentificacionFiscal)
            mPersona.NIF = value
        End Set
    End Property

    Public Property NombreComercial() As String Implements IEntidadFiscalDN.NombreComercial
        Get
            Return Me.Nombre
        End Get
        Set(ByVal value As String)
            Me.Nombre = value
        End Set
    End Property


    Public Property EntidadFiscalGenerica() As Localizaciones.DN.EntidadFiscalGenericaDN Implements Localizaciones.DN.IEntidadFiscalDN.EntidadFiscalGenerica
        Get
            Return mEntidadFiscalGenerica
        End Get
        Set(ByVal value As Localizaciones.DN.EntidadFiscalGenericaDN)
            Me.CambiarValorRef(Of EntidadFiscalGenericaDN)(value, mEntidadFiscalGenerica)
            If Not mEntidadFiscalGenerica.IentidadFiscal Is Me Then
                mEntidadFiscalGenerica.IentidadFiscal = Me
            End If
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarPersona(ByRef mensaje As String, ByVal persona As PersonaDN) As Boolean
        If persona Is Nothing Then
            mensaje = "persona no válida"
            Return False
        End If

        Return True
    End Function

    Private Function ValidarIdentificacionFiscal(ByRef mensaje As String, ByVal nif As NifDN) As Boolean
        If nif Is Nothing Then
            mensaje = "NIF no válido"
            Return False
        End If

        Return True
    End Function

    Private Function ValidarDomicilioFiscal(ByRef mensaje As String, ByVal domicilioFiscal As DireccionNoUnicaDN) As Boolean
        If domicilioFiscal Is Nothing Then
            mensaje = "domicilio fiscal no válido"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If Not mEntidadFiscalGenerica.IentidadFiscal Is Me Then
            pMensaje = "la entidad fiscal debia ser me"
            Return EstadoIntegridadDN.Inconsistente
        End If


        If Not ValidarPersona(pMensaje, mPersona) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        'If Not ValidarDomicilioFiscal(pMensaje, mDireccionFiscal) Then
        '    Return EstadoIntegridadDN.Inconsistente
        'End If

        If Not ValidarIdentificacionFiscal(pMensaje, Persona.NIF) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToStringEntidad

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Overrides Function ToString() As String
        Dim cadena As String = ""

        If mPersona IsNot Nothing Then
            cadena = mPersona.ToStringEntidad
        End If

        If mDireccionFiscal IsNot Nothing Then
            cadena = cadena & ", " & mDireccionFiscal.ToCadena
        End If

        Return cadena
    End Function

#End Region


End Class


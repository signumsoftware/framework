#Region "Importaciones"
Imports Framework.DatosNegocio
Imports FN.Personas.DN
Imports FN.Localizaciones.DN
#End Region

<Serializable()> _
Public Class EmpresaDN
    Inherits EntidadTemporalDN

#Region "Atributos"
    Protected mTipoEmpresaDN As TipoEmpresaDN
    Protected mEntidadFiscal As EntidadFiscalGenericaDN
    Protected mCIFNIF As String
#End Region

#Region "Constructores"

    'Public Sub New()
    '    MyBase.New()
    'End Sub

    'Public Sub New(ByVal pNombre As String, ByVal pDomicilioFiscal As LocalizacionesDN.ContactoDN, ByVal pTipoEmpresa As TipoEmpresaDN, ByVal pCif As LocalizacionesDN.CifDN)
    '    Dim mensaje As String = ""
    '    If ValidarNombre(mensaje, pNombre) Then
    '        Me.CambiarValorVal(Of String)(pNombre, mNombre)
    '    Else
    '        Throw New Exception(mensaje)
    '    End If

    '    If ValidarCif(mensaje, pCif) Then
    '        Me.CambiarValorRef(Of LocalizacionesDN.CifDN)(pCif, mCif)
    '    Else
    '        Throw New ArgumentException(mensaje)
    '    End If

    '    '   Me.CambiarValorRef(Of LocalizacionesDN.ContactoDN)(pDomicilioFiscal, mDomicilioFiscalDN)
    '    Me.CambiarValorRef(Of TipoEmpresaDN)(pTipoEmpresa, mTipoEmpresaDN)
    '    Me.modificarEstado = EstadoDatosDN.SinModificar
    'End Sub

#End Region

#Region "Propiedades"


    Public Property EntidadFiscal() As EntidadFiscalGenericaDN
        Get
            Return mEntidadFiscal
        End Get
        Set(ByVal value As EntidadFiscalGenericaDN)
            CambiarValorRef(Of EntidadFiscalGenericaDN)(value, mEntidadFiscal)
        End Set
    End Property

    Public Property TipoEmpresaDN() As TipoEmpresaDN
        Get
            Return mTipoEmpresaDN
        End Get
        Set(ByVal value As TipoEmpresaDN)
            Me.CambiarValorRef(Of TipoEmpresaDN)(value, mTipoEmpresaDN)
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarEntidadFiscal(ByRef mensaje As String, ByVal entFiscal As EntidadFiscalGenericaDN) As Boolean
        If entFiscal Is Nothing Then
            mensaje = "La entidad fiscal de la empresa no puede ser nula"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarEntidadFiscal(pMensaje, mEntidadFiscal) Then
            Return EstadoIntegridadDN.Inconsistente
        End If



        If Me.mEntidadFiscal IsNot Nothing Then
            Me.mCIFNIF = mEntidadFiscal.IentidadFiscal.IdentificacionFiscal.Codigo
        End If



        EstadoIntegridad = (MyBase.EstadoIntegridad(pMensaje))

    End Function


    Public Overrides Function ToString() As String
        If mEntidadFiscal IsNot Nothing Then
            Return CType(Me.mEntidadFiscal, Object).ToString

        End If
        Return Nothing
    End Function

#End Region


End Class

<Serializable()> Public Class ColEmpresasDN
    Inherits ArrayListValidable(Of EmpresaDN)

    ' metodos de coleccion
    '
End Class

<Serializable()> _
Public Class EmpresaFiscalDN
    Inherits EntidadTemporalDN
    Implements IEntidadFiscalDN




#Region "Atributos"
    Protected mNombreComercial As String
    Protected mCif As CifDN
    Protected mRazonSocial As String
    Protected mDireccion As DireccionNoUnicaDN
    Protected mEntidadFiscalGenerica As Localizaciones.DN.EntidadFiscalGenericaDN

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()

        Me.CambiarValorRef(Of Localizaciones.DN.EntidadFiscalGenericaDN)(New Localizaciones.DN.EntidadFiscalGenericaDN, mEntidadFiscalGenerica)
        mEntidadFiscalGenerica.IentidadFiscal = Me
        Me.CambiarValorRef(Of CifDN)(New CifDN, mCif)
        Me.modificarEstado = EstadoDatosDN.Inconsistente
    End Sub

#End Region

#Region "Propiedades Interfaz"

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
    Public Property NombreComercial() As String Implements IEntidadFiscalDN.NombreComercial
        Get
            Return mNombreComercial
        End Get
        Set(ByVal value As String)

            Me.CambiarValorVal(Of String)(value, mNombreComercial)

        End Set
    End Property

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

    Private ReadOnly Property DenominacionFiscal() As String Implements IEntidadFiscalDN.DenominacionFiscal
        Get
            Return mRazonSocial
        End Get
    End Property

    Public Property IdentificacionFiscal() As IIdentificacionFiscal Implements IEntidadFiscalDN.IdentificacionFiscal
        Get
            Return mCif
        End Get
        Set(ByVal value As IIdentificacionFiscal)
            CambiarValorRef(Of CifDN)(value, mCif)
        End Set
    End Property

    Public Property DomicilioFiscal() As DireccionNoUnicaDN Implements IEntidadFiscalDN.DomicilioFiscal
        Get
            Return mDireccion
        End Get
        Set(ByVal value As DireccionNoUnicaDN)
            CambiarValorRef(Of DireccionNoUnicaDN)(value, mDireccion)
        End Set
    End Property

#End Region

#Region "Propiedades"

    Public ReadOnly Property CifTexto() As String
        Get
            Return Me.mCif.Codigo
        End Get
    End Property

    Public Property RazonSocial() As String
        Get
            Return mRazonSocial
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mRazonSocial)
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarRazonSocial(ByRef mensaje As String, ByVal razonSocial As String) As Boolean
        If String.IsNullOrEmpty(razonSocial) Then
            mensaje = "La empresa debe tener una denominación fiscal"
            Return False
        End If
        Return True
    End Function

    Private Function ValidarCif(ByRef mensaje As String, ByVal pCif As CifDN) As Boolean
        If pCif Is Nothing Then
            mensaje = "La empresa debe tener una identificación fiscal"
            Return False
        Else
            If pCif.EstadoIntegridad(mensaje) <> EstadoIntegridadDN.Consistente Then
                Return False
            End If
        End If

        Return True
    End Function

    Private Function ValidarDomicilioFiscal(ByRef mensaje As String, ByVal domicilio As DireccionNoUnicaDN) As Boolean
        If domicilio Is Nothing Then
            mensaje = "La empresa debe tener un domicilio social"
            Return False
        End If

        Return True

    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If Not mEntidadFiscalGenerica.IentidadFiscal Is Me Then
            pMensaje = "la entidad fiscal debia de ser me"
            Return EstadoIntegridadDN.Inconsistente
        End If


        If Not ValidarRazonSocial(pMensaje, Me.mRazonSocial) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarCif(pMensaje, mCif) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarDomicilioFiscal(pMensaje, mDireccion) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        Me.mToSt = Me.ToString
        Return MyBase.EstadoIntegridad(pMensaje)

    End Function


    Public Overrides Function ToString() As String


        Dim dir, idfiscal As String

        dir = String.Empty
        idfiscal = String.Empty

        If Not Me.IdentificacionFiscal Is Nothing Then
            idfiscal = Me.IdentificacionFiscal.Codigo
        End If

        If Not Me.mDireccion Is Nothing Then
            dir = Me.mDireccion.ToString
        End If

        Return Me.NombreComercial & " - " & idfiscal & " - " & dir
    End Function

#End Region

 
End Class
Imports Framework.DatosNegocio

<Serializable()> _
Public Class FuturoTomadorDN
    Inherits EntidadTemporalDN
    Implements ITomador

#Region "atributos"

    Protected mApellido1FuturoTomador As String
    Protected mApellido2FuturoTomador As String
    Protected mNIFCIFFuturoTomador As String
    Protected mTomador As TomadorDN
    Protected mDireccion As FN.Localizaciones.DN.DireccionNoUnicaDN
    Protected mValorBonificacion As Double

#End Region

#Region "Propiedades"

    Public Property Apellido1FuturoTomador() As String
        Get
            Return mApellido1FuturoTomador
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mApellido1FuturoTomador)
        End Set
    End Property

    Public Property Apellido2FuturoTomador() As String
        Get
            Return mApellido2FuturoTomador
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mApellido2FuturoTomador)
        End Set
    End Property

    Public Property NIFCIFFuturoTomador() As String Implements ITomador.ValorCifNif
        Get
            Return mNIFCIFFuturoTomador
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mNIFCIFFuturoTomador)
        End Set
    End Property

    <RelacionPropCampoAtribute("mTomador")> _
    Public Property Tomador() As TomadorDN
        Get
            Return (mTomador)
        End Get
        Set(ByVal value As TomadorDN)
            CambiarValorRef(Of TomadorDN)(value, mTomador)
        End Set
    End Property

    <RelacionPropCampoAtribute("mDireccion")> _
    Public Property Direccion() As FN.Localizaciones.DN.DireccionNoUnicaDN Implements ITomador.Direccion
        Get
            Return mDireccion
        End Get
        Set(ByVal value As FN.Localizaciones.DN.DireccionNoUnicaDN)
            CambiarValorRef(Of FN.Localizaciones.DN.DireccionNoUnicaDN)(value, mDireccion)
        End Set
    End Property

    Public ReadOnly Property Iguales() As Boolean
        Get
            Return ComprobarTomadorFuturoTomador()
        End Get
    End Property

    Public Property ValorBonificacion() As Double Implements ITomador.ValorBonificacion
        Get
            Return mValorBonificacion
        End Get
        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorBonificacion)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String
        Dim cadena As String = String.Empty

        If mTomador Is Nothing Then
            cadena = mNombre & " " & mApellido1FuturoTomador & " " & mApellido2FuturoTomador
            If Not String.IsNullOrEmpty(mNIFCIFFuturoTomador) Then
                cadena = Trim(cadena) & " - " & mNIFCIFFuturoTomador
            End If
        Else
            cadena = mTomador.ToString()
        End If

        Return cadena
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        Dim mensaje As String = String.Empty
        Dim validacion As Boolean = True

        If String.IsNullOrEmpty(mNombre) OrElse String.IsNullOrEmpty(mApellido1FuturoTomador) Then
            pMensaje = "El nombre y apellido del futuro tomador no pueden ser nulos"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not String.IsNullOrEmpty(mNIFCIFFuturoTomador) Then
            If FN.Localizaciones.DN.NifDN.ValidaNif(mNIFCIFFuturoTomador, mensaje) Then
                validacion = True
            End If

            If FN.Localizaciones.DN.CifDN.ValidaCif(mNIFCIFFuturoTomador, mensaje) Then
                validacion = True
            End If

            If Not validacion Then
                pMensaje = "El NIF o CIF del futuro tomador no es válido"
                Return EstadoIntegridadDN.Inconsistente
            End If
        End If

        If mDireccion Is Nothing Then
            pMensaje = "La dirección del futuro tomador no puede ser nul"
            Return EstadoIntegridadDN.Inconsistente
        End If
    
        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Private Function ComprobarTomadorFuturoTomador() As Boolean
        If mTomador Is Nothing OrElse mTomador.EntidadFiscalGenerica Is Nothing OrElse mTomador.EntidadFiscalGenerica.IentidadFiscal Is Nothing Then
            Return False
        End If
        Dim mensaje As String = String.Empty
        If String.IsNullOrEmpty(mNIFCIFFuturoTomador) Then
            Return False
        End If

        If Me.mValorBonificacion <> Me.Tomador.ValorBonificacion Then
            mensaje = "los ValorBonificacion no coinciden"
            Return False
        End If


        If FN.Localizaciones.DN.NifDN.ValidaNif(mNIFCIFFuturoTomador, mensaje) Then
            Dim personaFiscal As FN.Personas.DN.PersonaFiscalDN
            personaFiscal = CType(mTomador.EntidadFiscalGenerica.IentidadFiscal, FN.Personas.DN.PersonaFiscalDN)

            If personaFiscal.Persona Is Nothing OrElse personaFiscal.Persona.Periodo Is Nothing _
                    OrElse personaFiscal.IdentificacionFiscal Is Nothing OrElse personaFiscal.DomicilioFiscal Is Nothing Then
                Return False
            End If

            If mNombre = mTomador.Nombre AndAlso mApellido1FuturoTomador = personaFiscal.Persona.Apellido AndAlso mApellido2FuturoTomador = personaFiscal.Persona.Apellido2 _
                    AndAlso mNIFCIFFuturoTomador = personaFiscal.IdentificacionFiscal.Codigo AndAlso mPeriodo.FInicio = personaFiscal.Persona.FI _
                    AndAlso mDireccion.Iguales(personaFiscal.DomicilioFiscal) Then
                Return True
            End If

        ElseIf FN.Localizaciones.DN.CifDN.ValidaCif(mNIFCIFFuturoTomador, mensaje) Then
            Dim empresaFiscal As FN.Empresas.DN.EmpresaFiscalDN
            empresaFiscal = CType(mTomador.EntidadFiscalGenerica.IentidadFiscal, FN.Empresas.DN.EmpresaFiscalDN)

            If empresaFiscal.IdentificacionFiscal Is Nothing OrElse empresaFiscal.DomicilioFiscal Is Nothing Then
                Return False
            End If

            If mNombre = mTomador.Nombre AndAlso mNIFCIFFuturoTomador = empresaFiscal.IdentificacionFiscal.Codigo AndAlso _
                    mDireccion.Iguales(empresaFiscal.DomicilioFiscal) Then
                Return True
            End If

        End If

        Return False

    End Function

#End Region


End Class



Public Interface ITomador

    Property ValorCifNif() As String
    Property Direccion() As FN.Localizaciones.DN.DireccionNoUnicaDN
    Property ValorBonificacion() As Double

End Interface
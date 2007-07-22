#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Clase que implementa el tipo de dirección introducido de manera manual -> Se ha cambiado la clase para simplificar
''' el control que muestra los contactos
''' </summary>
''' <remarks>
''' 
''' </remarks>
''' 
<Serializable()> Public Class DireccionNoUnicaDN
    Inherits EntidadDN
    Implements IDatoContactoDN

#Region "Atributos"
    Protected mLocalidad As LocalidadDN
    Protected mTipoVia As TipoViaDN
    Protected mVia As String
    Protected mNumero As String
    Protected mCodPostal As String
#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    'Public Sub New(ByVal pNombre As String, ByVal pProvincia As String, ByVal pLocalidad As String, ByVal pTipoVia As String, ByVal pVia As String, ByVal pNumero As String, ByVal pCodPostal As String)
    '    Me.CambiarValorVal(Of String)(pNombre, mNombre)
    '    Me.CambiarValorVal(Of String)(pProvincia, mProvincia)
    '    Me.CambiarValorVal(Of String)(pLocalidad, mLocalidad)
    '    Me.CambiarValorVal(Of String)(pTipoVia, mTipoVia)
    '    Me.CambiarValorVal(Of String)(pVia, mVia)
    '    Me.CambiarValorVal(Of String)(pProvincia, mProvincia)
    '    Me.CambiarValorVal(Of String)(mCodPostal, pCodPostal)
    '    Me.ModificarEstado = EstadoDatosDN.SinModificar
    'End Sub

#End Region

#Region "Propiedades"
    Public ReadOnly Property ToCadena() As String
        Get
            ' Return Me.mTipoVia.Nombre & " " & Me.mVia & " " & Me.mCodPostal & " " & Me.mLocalidad.ToCadena
            Return Me.mVia & " " & Me.mCodPostal & " " & Me.mLocalidad.ToCadena
        End Get
    End Property
    Public Property tipo() As String Implements IDatoContactoDN.Tipo
        Get
            Return Me.GetType.ToString
        End Get
        Set(ByVal value As String)
            Throw New NotImplementedException()
        End Set
    End Property

    Public Property Valor() As String Implements IDatoContactoDN.Valor
        Get
            Dim mDireccion As String
            'De momento se devuelve la dirección sin ningún formato, con un espacio de separación
            mDireccion = mTipoVia.Nombre & " " & mVia & " " & mNumero & " " & mLocalidad.Nombre & " " & mCodPostal & " " & mLocalidad.Provincia.Nombre
            Return Trim(mDireccion)
        End Get
        Set(ByVal value As String)
            Throw New NotImplementedException()
        End Set
    End Property

    Public Property TipoVia() As TipoViaDN
        Get
            Return mTipoVia
        End Get
        Set(ByVal value As TipoViaDN)
            Me.CambiarValorRef(Of TipoViaDN)(value, mTipoVia)
        End Set
    End Property

    Public Property Via() As String
        Get
            Return mVia
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(value, mVia)
        End Set
    End Property

    Public Property Numero() As String
        Get
            Return mNumero
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(value, mNumero)
        End Set
    End Property

    Public Property CodPostal() As String
        Get
            Return mCodPostal
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(value, mCodPostal)
        End Set
    End Property

    Public Property Localidad() As LocalidadDN
        Get
            Return Me.mLocalidad
        End Get
        Set(ByVal value As LocalidadDN)
            Me.CambiarValorRef(Of LocalidadDN)(value, Me.mLocalidad)
        End Set
    End Property

    Public Property Comentario() As String Implements IDatoContactoDN.Comentario
        Get

        End Get
        Set(ByVal value As String)

        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarLocalidad(ByRef mensaje As String, ByVal localidad As LocalidadDN) As Boolean
        If localidad Is Nothing Then
            mensaje = "La localidad no puede ser nula"
            Return False
        End If

        Return True
    End Function

    Private Function ValidarCodigoPostal(ByRef mensaje As String, ByVal codPostal As String, ByVal localidad As LocalidadDN) As Boolean
        If localidad IsNot Nothing AndAlso localidad.ColCodigoPostal IsNot Nothing Then
            For Each cp As CodigoPostalDN In localidad.ColCodigoPostal
                If cp.Nombre = codPostal Then
                    Return True
                End If
            Next

        End If

        mensaje = "El código postal no es válido para la localidad seleccionada"
        Return False

    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarLocalidad(pMensaje, mLocalidad) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarCodigoPostal(pMensaje, mCodPostal, mLocalidad) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Overrides Function ToString() As String
        Dim cadena As String = ""

        If mTipoVia IsNot Nothing Then
            cadena = mTipoVia.Nombre & " "
        End If

        cadena = cadena & mVia & " " & mNumero
        cadena = Trim(cadena) & " " & mCodPostal

        If mLocalidad IsNot Nothing AndAlso mLocalidad.Provincia IsNot Nothing Then
            cadena = Trim(cadena) & " " & mLocalidad.Nombre & " " & mLocalidad.Provincia.Nombre
        End If

        cadena = Trim(cadena) & " " & mCodPostal

        Return Trim(cadena)

    End Function

    Public Function Iguales(ByVal direccion As DireccionNoUnicaDN) As Boolean
        If direccion Is Nothing Then
            Return False
        End If

        If mLocalidad Is Nothing OrElse direccion.mLocalidad Is Nothing OrElse mTipoVia Is Nothing OrElse direccion.mTipoVia Is Nothing Then
            Return False
        End If

        If mLocalidad.GUID = direccion.Localidad.GUID AndAlso mTipoVia.GUID = direccion.mTipoVia.GUID AndAlso mVia = direccion.Via _
                AndAlso mNumero = direccion.mNumero AndAlso mCodPostal = direccion.mCodPostal Then
            Return True
        End If

        Return False

    End Function

#End Region


End Class

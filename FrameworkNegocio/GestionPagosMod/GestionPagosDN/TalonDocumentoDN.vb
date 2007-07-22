
<Serializable()> Public Class TalonDocumentoDN
    Inherits Framework.DatosNegocio.EntidadDN


#Region "Atributos"

    Protected mFechaImpresion As Date
    Protected mNumeroSerie As String
    Protected mImporte As Single
    Protected mDestinatario As String
    Protected mFechaTalon As Date
    Protected mHuellaRTF As HuellaContenedorRTFDN
    Protected mTalon As TalonDN


    Protected mAnulado As Boolean = False

#End Region


#Region "Propiedades"
    Public Property Anulado() As Boolean
        Get
            Return Me.mAnulado
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(value, Me.mAnulado)
        End Set
    End Property

    ''' <summary>
    ''' Rellena todas las propiedades del talónDoc a partir  del talón.
    ''' OJO: hay que cargar la huellaRTF del talón antes de asignar la propiedad
    ''' para que tome el mismo texto que el talón
    ''' </summary>
    ''' <value></value>
    ''' <remarks></remarks>
    Public Property Talon() As TalonDN
        Get
            Return Me.mTalon
        End Get
        Set(ByVal value As TalonDN)
            Me.CambiarValorRef(value, Me.mTalon)
            If Not mTalon Is Nothing Then
                Me.Importe = mTalon.ImportePago
                Me.Destinatario = mTalon.Destinatario.DenominacionFiscal
                Me.FechaTalon = mTalon.Pago.FechaProgramadaEmision
                If (Not Me.mTalon.HuellaRTF Is Nothing) AndAlso (Not Me.mTalon.HuellaRTF.EntidadReferida Is Nothing) Then
                    Me.HuellaRTF = New HuellaContenedorRTFDN(New ContenedorRTFDN(CType(Me.mTalon.HuellaRTF.EntidadReferida, ContenedorRTFDN).RTF))
                End If
            Else
                Me.Importe = 0
                Me.Destinatario = String.Empty
                Me.FechaTalon = DateTime.MinValue
                Me.HuellaRTF = Nothing
            End If
        End Set
    End Property

    Public Property Importe() As Single
        Get
            Return mImporte
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(value, mImporte)
        End Set
    End Property

    Public Property Destinatario() As String
        Get
            Return Me.mDestinatario
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(value, mDestinatario)
        End Set
    End Property

    Public Property FechaTalon() As Date
        Get
            Return Me.mFechaTalon
        End Get
        Set(ByVal value As Date)
            Me.CambiarValorVal(value, mFechaTalon)
        End Set
    End Property

    Public Property FechaImpresion() As DateTime
        Get
            Return mFechaImpresion
        End Get
        Set(ByVal value As DateTime)
            CambiarValorVal(Of Date)(value, mFechaImpresion)
        End Set
    End Property

    Public Property NumeroSerie() As String
        Get
            Return mNumeroSerie
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mNumeroSerie)
        End Set
    End Property

    Public Property HuellaRTF() As HuellaContenedorRTFDN
        Get
            Return mHuellaRTF
        End Get
        Set(ByVal value As HuellaContenedorRTFDN)
            CambiarValorRef(Of HuellaContenedorRTFDN)(value, mHuellaRTF)
        End Set
    End Property




#End Region

#Region "Validaciones"


    Private Function ValidarNumeroSerie(ByRef mensaje As String, ByVal numSerie As String) As Boolean
        If String.IsNullOrEmpty(numSerie) Then
            mensaje = "El número de serie no puede ser nulo"
            Return False
        End If

        Return True
    End Function

    Private Function ValidarDestinatario(ByVal pDestinatario As String, ByRef pMensaje As String) As Boolean
        If String.IsNullOrEmpty(pDestinatario) Then
            pMensaje = "No se ha definido un destinatario para el talón impreso"
            Return False
        End If
        Return True
    End Function

    Private Function ValidarImporte(ByVal pImporte As Single, ByRef pMensaje As String) As Boolean
        If pImporte <= 0 Then
            pMensaje = "No se ha definido un importe para el talón impreso"
            Return False
        End If
        Return True
    End Function

    Private Function ValidarFecha(ByVal pFecha As Date, ByVal pMensaje As String) As Boolean
        If pFecha = Date.MinValue OrElse pFecha = Date.MaxValue Then
            pMensaje = "No se ha definido una fecha correcta para el talón"
            Return False
        End If
        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If Not ValidarNumeroSerie(pMensaje, mNumeroSerie) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarDestinatario(mDestinatario, pMensaje) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarImporte(Me.mImporte, pMensaje) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarFecha(Me.mFechaTalon, pMensaje) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class

<Serializable()> _
Public Class ColTalonDocumentoDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of TalonDocumentoDN)

#Region "Métodos"

    ''' <summary>
    ''' Itera la colección de talones y devuelve el nº de talones que están ok
    ''' </summary>
    Public Function NumeroTalonesSinAnular() As Integer
        Dim numero As Integer = 0
        For Each mit As TalonDocumentoDN In Me
            If Not mit.Anulado Then
                numero += 1
            End If
        Next
    End Function

#End Region

End Class

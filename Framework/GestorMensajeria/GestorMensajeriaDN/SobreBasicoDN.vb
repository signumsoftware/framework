Imports Framework.DatosNegocio

<Serializable()> _
Public Class SobreBasicoDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mDatosMensaje As DatosMensajeDN
    Protected mDestino As IDestinoDN

    Protected mFechaEncolado As DateTime
    Protected mFechaEnviado As DateTime
    Protected mFechaReintento As DateTime

    Protected mEnviado As Boolean
    Protected mDescartado As Boolean
    Protected mReintentos As Integer

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal datosMensaje As DatosMensajeDN, ByVal destino As IDestinoDN)
        Dim mensaje As String
        mensaje = ""

        If Not ValidarDatosMensaje(mensaje, datosMensaje) Then
            Throw New ApplicationExceptionDN(mensaje)
        End If

        If Not ValidarDestino(mensaje, destino) Then
            Throw New ApplicationExceptionDN(mensaje)
        End If

        CambiarValorRef(Of DatosMensajeDN)(datosMensaje, mDatosMensaje)
        CambiarValorRef(Of IDestinoDN)(destino, mDestino)
        CambiarValorVal(Of Boolean)(False, mEnviado)
        CambiarValorVal(Of Boolean)(False, mDescartado)
        CambiarValorVal(Of Integer)(0, mReintentos)

        modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"

    Public ReadOnly Property DatosMensaje() As DatosMensajeDN
        Get
            Return mDatosMensaje
        End Get
    End Property

    Public ReadOnly Property Destino() As IDestinoDN
        Get
            Return mDestino
        End Get
    End Property

    Public Property FechaEncolado() As DateTime
        Get
            Return mFechaEncolado
        End Get
        Set(ByVal value As DateTime)
            CambiarValorVal(Of DateTime)(value, mFechaEncolado)
        End Set
    End Property

    Public Property FechaEnviado() As DateTime
        Get
            Return mFechaEnviado
        End Get
        Set(ByVal value As DateTime)
            CambiarValorVal(Of DateTime)(value, mFechaEnviado)
        End Set
    End Property

    Public Property FechaReintento() As DateTime
        Get
            Return mFechaReintento
        End Get
        Set(ByVal value As DateTime)
            CambiarValorVal(Of DateTime)(value, mFechaReintento)
        End Set
    End Property

    Public Property Enviado() As Boolean
        Get
            Return mEnviado
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mEnviado)
        End Set
    End Property

    Public Property Descartado() As Boolean
        Get
            Return mDescartado
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mDescartado)
        End Set
    End Property

    Public Property Reintentos() As Integer
        Get
            Return mReintentos
        End Get
        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mReintentos)
        End Set
    End Property

#End Region

#Region "Métodos de validación"

    Private Function ValidarDatosMensaje(ByRef mensaje As String, ByVal datosMensaje As DatosMensajeDN) As Boolean
        If datosMensaje Is Nothing Then
            mensaje = "Los datos de mensaje no pueden ser nulos"
            Return False
        End If

        Return True
    End Function

    Public Function ValidarDestino(ByRef mensaje As String, ByVal destino As IDestinoDN) As Boolean
        If destino Is Nothing Then
            mensaje = "El destino no puede ser nulo"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN
        If Not ValidarDatosMensaje(pMensaje, mDatosMensaje) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarDestino(pMensaje, mDestino) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Sub Abrir()
        'mMensaje = MensajeDN.FromXml(mXmlMensaje);
        'mXmlMensaje = null;
    End Sub

    Public Sub Cerrar()
        'mXmlMensaje = MensajeDN.ToXml(mMensaje);
        'mMensaje = null;
    End Sub

#End Region

End Class

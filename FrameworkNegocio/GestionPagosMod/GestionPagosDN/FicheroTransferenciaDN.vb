Imports Framework.DatosNegocio

<Serializable()> _
Public Class FicheroTransferenciaDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mColPagos As ColPagoDN
    Protected mFechaEmision As Date
    Protected mFechaCreacion As Date
    Protected mFechaEnvio As Date
    Protected mFicheroGenerado As Boolean

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        CambiarValorVal(Of Date)(Now(), mFechaCreacion)
        modificarEstado = EstadoDatosDN.Inconsistente
    End Sub

#End Region

#Region "Propiedades"

    Public Property ColPagos() As ColPagoDN
        Get
            Return mColPagos
        End Get
        Set(ByVal value As ColPagoDN)
            Me.CambiarValorCol(Of ColPagoDN)(value, mColPagos)
        End Set
    End Property

    Public Property FechaEmision() As Date
        Get
            Return mFechaEmision
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaEmision)
        End Set
    End Property

    Public Property FechaCreacion() As Date
        Get
            Return mFechaCreacion
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaCreacion)
        End Set
    End Property

    Public Property FechaEnvio() As Date
        Get
            Return mFechaEnvio
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaEnvio)
        End Set
    End Property

    Public Property FicheroGenerado() As Boolean
        Get
            Return mFicheroGenerado
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mFicheroGenerado)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mFicheroGenerado AndAlso (mColPagos Is Nothing OrElse mColPagos.Count = 0) Then
            pMensaje = "El fichero de transferencias no puede ser generado si no tiene al menos un pago"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mFechaEmision.DayOfWeek = DayOfWeek.Saturday OrElse mFechaEmision.DayOfWeek = DayOfWeek.Sunday Then
            pMensaje = "Una transferencia debe estar programada para ser emitida en día laborable"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function

    Public Overrides Function ToString() As String
        Dim cadena As String = ""
        cadena = Nombre + ", Fecha envío: " & mFechaEnvio.ToShortDateString() & ", Fecha emisión: " & mFechaEmision.ToShortDateString()

        If mFicheroGenerado Then
            cadena = cadena & ", Generado: " & mFechaCreacion.ToShortDateString()
        End If

        Return cadena

    End Function
#End Region

End Class

<Serializable()> _
Public Class ColFicheroTransferenciaDN
    Inherits ArrayListValidable(Of FicheroTransferenciaDN)

End Class
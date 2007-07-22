Imports Framework.DatosNegocio

<Serializable()> _
Public Class ModeloDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mMarca As MarcaDN

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mMarca")> _
    Public Property Marca() As MarcaDN
        Get
            Return mMarca
        End Get
        Set(ByVal value As MarcaDN)
            CambiarValorRef(Of MarcaDN)(value, mMarca)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String
        Dim cadena As String

        cadena = Me.Nombre

        If Marca IsNot Nothing Then
            cadena = cadena & " - " & Marca.ToString()
        End If

        Return cadena
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mNombre = String.Empty Then
            pMensaje = "El nombre del modelo no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Marca Is Nothing Then
            pMensaje = "La marca del modelo no puede ser nula"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class




<Serializable()> _
Public Class ColModeloDN
    Inherits ArrayListValidable(Of ModeloDN)

    Public Function RecuperarModeloxNombreMarca(ByVal nombreModelo As String, ByVal marca As MarcaDN) As ModeloDN

        For Each modelo As ModeloDN In Me
            If modelo.Nombre = nombreModelo AndAlso modelo.Marca.GUID = marca.GUID Then
                Return modelo
            End If
        Next

        Return Nothing
    End Function

End Class





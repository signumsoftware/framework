''' <summary>
''' Esta clase nos sirve para tener versionados los elementos de negocio principales, de modo que a través de su 
''' versión sabemos si han cambiado o no
''' </summary>
''' <remarks></remarks>
Public Class EntidadConVersionDN
    Inherits EntidadDN

#Region "Atributos"
    Private mVersion As String = "0.0"
#End Region

#Region "Constructores"
    Public Sub New()
        MyBase.New()
    End Sub
    Public Sub New(ByVal pVersion As String)
        Dim mensaje As String = ""
        If ValidarVersion(mensaje, pVersion) Then
            Me.CambiarValorVal(mVersion, pVersion)
        Else
            Throw New Exception(mensaje)
        End If
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub
#End Region

#Region "Propiedades"

    Public Property Version() As String
        Get
            Return mVersion
        End Get
        Set(ByVal value As String)
            Dim mensaje As String = ""
            If ValidarVersion(mensaje, value) Then
                Me.CambiarValorVal(mVersion, value)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property
#End Region

#Region "Validaciones"
    Private Function ValidarVersion(ByRef mensaje As String, ByVal pVersion As String) As String
        If pVersion Is Nothing OrElse pVersion = String.Empty Then
            mensaje = "Los objetos de tipo EntidadConVersionDN deben tener una version"
            Return False
        End If
        Try
            Dim val As Integer = CType(pVersion, Integer)
        Catch ex As Exception
            mensaje = "Las versiones deben ser valores enteros"
        End Try
        Return True
    End Function
#End Region

    Protected Overrides Sub OnCambioEstadoDatos()
        MyBase.OnCambioEstadoDatos()
        Me.Version = (CType(Me.Version, Integer) + 1).ToString + ".0"
    End Sub
End Class

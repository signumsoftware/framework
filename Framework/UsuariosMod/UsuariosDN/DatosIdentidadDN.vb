#Region "Importaciones"
Imports Framework.DatosNegocio
Imports Framework.Utilidades
#End Region

<Serializable()> Public Class DatosIdentidadDN
    Inherits EntidadDN

#Region "Atributos"
    Protected mHashClave As Byte()
    'Protected mClave As String
    Protected mNick As String
#End Region

#Region "Constructores"
    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pNick As String, ByVal pClave As String)
        Dim mensaje As String = ""

        If ValNick(mensaje, pNick) Then
            Me.CambiarValorVal(Of String)(pNick, mNick)
        Else
            Throw New Exception(mensaje)
        End If

        AsignarClave(pClave)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub
#End Region


#Region "Propiedades"

    Public Property Nick() As String
        Get
            Return mNick
        End Get
        Set(ByVal value As String)
            Dim mensaje As String = ""
            If ValNick(mensaje, value) Then
                Me.CambiarValorVal(Of String)(value, mNick)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property

    Public ReadOnly Property HashClave() As Byte()
        Get
            Return Me.mHashClave
        End Get
    End Property

    Public WriteOnly Property Clave() As String
        Set(ByVal value As String)
            AsignarClave(value)
        End Set
    End Property

    Private Sub AsignarClave(ByVal pclave As String)
        Dim vh As New GeneradorHashes
        Dim auxClave As Byte() = vh.CalcularHash(pclave)
        Dim mensaje As String = ""
        If ValHashClave(mensaje, auxClave) Then
            Me.CambiarValorVal(Of Byte())(auxClave, mHashClave)
            'Me.CambiarValorVal(Of String)(pclave, mClave)

        Else
            Throw New Exception(mensaje)
        End If
    End Sub

#End Region

#Region "Metodos Validacion"

    Private Function ValNick(ByRef mensaje As String, ByVal pNick As String) As Boolean
        If pNick = String.Empty Then
            mensaje = "El identificador del usuario no puede ser nulo"
            Return False
        End If
        Return True
    End Function

    Private Function ValHashClave(ByRef mensaje As String, ByVal pHashClave As Byte()) As Boolean
        If pHashClave Is Nothing Then
            mensaje = "El password no puede ser nulo"
            Return False
        End If
        Return True
    End Function

#End Region

#Region "Métodos"

    'Public Function GetClave() As String
    '    Return Me.mClave
    'End Function

    Public Function ValidarClave(ByVal pClave As String) As Boolean
        Dim vh As New GeneradorHashes
        Dim pHashClave As Byte()

        pHashClave = vh.CalcularHash(pClave)
        Return ValidarClave(pHashClave)

    End Function

    Public Function ValidarClave(ByVal pHashClave As Byte()) As Boolean
        Dim vh As New GeneradorHashes
        Dim b As Integer

        For b = mHashClave.GetLowerBound(0) To mHashClave.GetUpperBound(0)
            If mHashClave(b) <> pHashClave(b) Then
                Return False
            End If
        Next
        Return True

    End Function

#End Region

End Class

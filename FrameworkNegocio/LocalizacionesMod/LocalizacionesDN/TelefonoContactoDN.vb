#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

<Serializable()> _
Public Class TelefonoContactoDN
    Inherits EntidadDN

#Region "Atributos"
    Protected mColTelefonosDN As ColTelefonosDN
#End Region

#Region "Constructores"
    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pColTelefonosDN As ColTelefonosDN)
        Dim mensaje As String = ""
        If ValColTelefonosDN(mensaje, pColTelefonosDN) Then
            Throw New Exception(mensaje)
        Else
            Me.CambiarValorRef(Of ColTelefonosDN)(pColTelefonosDN, mColTelefonosDN)
        End If
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub
#End Region

#Region "Propiedades"
    Public Property ColTelefonosDN() As ColTelefonosDN
        Get
            Return mColTelefonosDN
        End Get
        Set(ByVal value As ColTelefonosDN)
            Dim mensaje As String = ""
            If ValColTelefonosDN(mensaje, value) Then
                Me.CambiarValorRef(Of ColTelefonosDN)(value, mColTelefonosDN)
            Else
                Throw New Exception(mensaje)
            End If

        End Set
    End Property

#End Region

#Region "Validaciones"
    Private Function ValColTelefonosDN(ByRef mensaje As String, ByVal pColTelefonosDN As ColTelefonosDN) As Boolean
        If pColTelefonosDN Is Nothing Then
            mensaje = "La colección de teléfonos debe existir"
            Return False
        Else
            Return True
        End If
    End Function
#End Region

End Class

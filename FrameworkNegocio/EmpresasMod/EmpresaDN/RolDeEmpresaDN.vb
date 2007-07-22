#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region
<Serializable()> Public Class RolDeEmpresaDN
    Inherits EntidadDN

#Region "Atributos"
    'TODO: Se ha agregado este atributo -> completar 
    Protected mActividad As ActividadDN
#End Region

#Region "Constructores"
    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pNombreRol As String)
        Dim mensaje As String = ""
        If Me.ValidarNombre(mensaje, pNombreRol) Then
            Me.CambiarValorVal(Of String)(pNombreRol, mNombre)
        Else
            Throw New Exception(mensaje)
        End If
        Me.ModificarEstado = EstadoDatosDN.SinModificar
    End Sub
#End Region

#Region "Propiedades"
    Public Overrides Property Nombre() As String
        Get
            Return mNombre
        End Get
        Set(ByVal value As String)
            Dim mensaje As String = ""
            If Me.ValidarNombre(mensaje, value) Then
                Me.CambiarValorVal(Of String)(value, mNombre)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property

    Public ReadOnly Property EstadoModificacion() As Framework.DatosNegocio.EstadoDatosDN
        Get
            Return MyBase.Estado
        End Get
    End Property



#End Region

#Region "Validaciones"
    Private Function ValidarNombre(ByRef mensaje As String, ByVal pNombre As String) As String
        If pNombre Is Nothing OrElse pNombre = String.Empty Then
            mensaje = "Todo rol asignado tiene que tener un nombre"
            Return False
        End If
        Return True
    End Function
#End Region
End Class

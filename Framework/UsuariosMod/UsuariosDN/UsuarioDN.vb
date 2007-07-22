Imports Framework.DatosNegocio
Imports Framework.Procesos.ProcesosDN

<Serializable()> _
Public Class UsuarioDN
    Inherits EntidadDN
    Implements System.Security.Principal.IIdentity

#Region "Atributos"
    Protected mAutenticado As Boolean
    Protected mHuellaEntidadUserDN As IHuellaEntidadDN
    'Protected mEntidadUser As IEjecutorOperacionRolDN

#End Region

#Region "Constructores"
    Public Sub New()
        MyBase.New()
        mAutenticado = False
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

    Public Sub New(ByVal pNombre As String, ByVal pAutenticado As Boolean)
        Dim mensaje As String = String.Empty

        Me.CambiarValorVal(Of String)(pNombre, mNombre)
        Me.CambiarValorVal(Of Boolean)(pAutenticado, mAutenticado)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

    'Public Sub New(ByVal pNombre As String, ByVal pAutenticado As Boolean, ByVal pHuellaEntidadUserDN As IHuellaEntidadDN)
    '    Dim mensaje As String = String.Empty

    '    If (ValHuellaEntidadUserDN(mensaje, pHuellaEntidadUserDN) = False) Then
    '        Throw New ApplicationException(mensaje)
    '    End If

    '    Me.CambiarValorVal(Of String)(pNombre, mNombre)
    '    Me.CambiarValorVal(Of Boolean)(pAutenticado, mAutenticado)
    '    If mEntidadUser IsNot Nothing Then
    '        Me.CambiarValorRef(Of IHuellaEntidadDN)(pHuellaEntidadUserDN, mEntidadUser)

    '    End If
    '    Me.modificarEstado = EstadoDatosDN.SinModificar
    'End Sub
#End Region

#Region "Propiedades"

    Public ReadOnly Property AuthenticationType() As String Implements System.Security.Principal.IIdentity.AuthenticationType
        Get
            Return "Anonymous"
        End Get
    End Property

    Public ReadOnly Property IsAuthenticated() As Boolean Implements System.Security.Principal.IIdentity.IsAuthenticated
        Get
            Return mAutenticado
        End Get
    End Property

    Public ReadOnly Property Name() As String Implements System.Security.Principal.IIdentity.Name
        Get
            Return Me.Nombre
        End Get
    End Property

    Public Property Autenticado() As Boolean
        Get
            Return mAutenticado
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mAutenticado)
        End Set
    End Property

    'TODO: Hay que homogeneizar la parte de GDocEntrantes con la parte de operadores
    <RelacionPropCampoAtribute("mHuellaEntidadUserDN")> _
    Public Property HuellaEntidadUserDN() As IHuellaEntidadDN

        Get
            Return mHuellaEntidadUserDN
        End Get
        Set(ByVal value As IHuellaEntidadDN)
            CambiarValorRef(Of IHuellaEntidadDN)(value, mHuellaEntidadUserDN)
        End Set
    End Property

    'Public Property EntidadUser() As IEjecutorOperacionRolDN
    '    Get
    '        Return mEntidadUser
    '    End Get
    '    Set(ByVal value As IEjecutorOperacionRolDN)
    '        CambiarValorRef(Of IEjecutorOperacionRolDN)(value, mEntidadUser)
    '    End Set
    'End Property

#End Region

#Region "Validaciones"

    Private Function ValHuellaEntidadUserDN(ByRef pMensaje As String, ByVal pHuellaEntidadUserDN As IHuellaEntidadDN) As Boolean
        'Si permitimos que entidad user de usuario sea nothing
        'If (pHuellaEntidadUserDN Is Nothing) Then
        '    pMensaje = "Error: la huella a la entidad no puede ser nula"
        '    Return False
        'End If

        Return True
    End Function

#End Region

#Region "Métodos"
    'Public Function RecuperarColOperacionesEntidad() As ColOperacionDN
    '    If mEntidadUser Is Nothing Then
    '        Return Nothing
    '    End If

    '    Return mEntidadUser.ColOperaciones
    'End Function
#End Region

End Class

Imports Framework.DatosNegocio

<Serializable()> _
Public Class PermisoDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mDatoVal As String
    Protected mDatoRef As IEntidadBaseDN
    Protected mEsRef As Boolean
    Protected mTipoPermiso As TipoPermisoDN

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

#End Region

#Region "Propiedades"

    Public Property DatoVal() As String
        Get
            Return mDatoVal
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mDatoVal)
        End Set
    End Property

    Public Property DatoRef() As IEntidadBaseDN
        Get
            Return mDatoRef
        End Get
        Set(ByVal value As IEntidadBaseDN)
            CambiarValorRef(Of IEntidadBaseDN)(value, mDatoRef)
        End Set
    End Property

    Public Property EsRef() As Boolean
        Get
            Return mEsRef
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mEsRef)
        End Set
    End Property

    Public Property TipoPermiso() As TipoPermisoDN
        Get
            Return mTipoPermiso
        End Get
        Set(ByVal value As TipoPermisoDN)
            CambiarValorRef(Of TipoPermisoDN)(value, mTipoPermiso)
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarTipoPermiso(ByRef mensaje As String, ByVal tipoP As TipoPermisoDN) As Boolean
        If tipoP Is Nothing Then
            mensaje = "El tipo de permiso no puede ser nulo"
            Return False
        End If

        Return True
    End Function

    Private Function ValidarDatoRef(ByRef mensaje As String, ByVal datoR As IEntidadBaseDN) As Boolean
        If mEsRef AndAlso datoR Is Nothing Then
            mensaje = "El dato por referencia del permiso no puede ser nulo"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN
        If Not ValidarTipoPermiso(pMensaje, mTipoPermiso) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarDatoRef(pMensaje, mDatoRef) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Overrides Function ToString() As String
        Dim cadena As String = ""

        If mTipoPermiso IsNot Nothing Then
            cadena = mTipoPermiso.ToString()
        End If

        If Not String.IsNullOrEmpty(mNombre) Then
            cadena = cadena & " " & mNombre
        End If

        If mEsRef Then
            If mDatoRef IsNot Nothing Then
                cadena = cadena & " - " & mDatoRef.ToString()
            End If
        Else
            cadena = cadena & " - " & mDatoVal
        End If

        Return cadena
    End Function

#End Region

End Class


<Serializable()> _
Public Class TipoPermisoDN
    Inherits EntidadTipoDN

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal nombreTipoP As String)
        MyBase.New(nombreTipoP)
    End Sub

#End Region

End Class

<Serializable()> _
Public Class ColPermisoDN
    Inherits ArrayListValidable(Of PermisoDN)

#Region "Métodos"

    Public Function ContienePermisoxTipo(ByVal tipoP As TipoPermisoDN) As Boolean
        If tipoP IsNot Nothing Then
            For Each permiso As PermisoDN In Me
                If permiso.TipoPermiso.GUID = tipoP.GUID Then
                    Return True
                End If
            Next
        End If

        Return False

    End Function

    Public Function ContienePermisoxTipo(ByVal nombreTipo As String) As Boolean
        If Not String.IsNullOrEmpty(nombreTipo) Then
            For Each permiso As PermisoDN In Me
                If permiso.TipoPermiso.Nombre = nombreTipo Then
                    Return True
                End If
            Next
        End If

        Return False

    End Function

    Public Function RecuperarPermisoxTipo(ByVal tipoP As TipoPermisoDN) As ColPermisoDN
        RecuperarPermisoxTipo = New ColPermisoDN()

        For Each permiso As PermisoDN In Me
            If permiso.TipoPermiso.GUID = tipoP.GUID Then
                RecuperarPermisoxTipo.Add(permiso)
            End If
        Next
    End Function

    Public Function RecuperarPermisoxTipo(ByVal nombreTipo As String) As ColPermisoDN
        RecuperarPermisoxTipo = New ColPermisoDN()

        For Each permiso As PermisoDN In Me
            If permiso.TipoPermiso.Nombre = nombreTipo Then
                RecuperarPermisoxTipo.Add(permiso)
            End If
        Next

    End Function

    ''' <summary>
    ''' Este método devuelve la colección de permisos obtenida de la unión con la colección de permisos pasada
    ''' como parámetro, prevaleciendo los permisos cuyo tipo ya esté contenido en la colección original
    ''' </summary>
    ''' <param name="colPermisosAuxiliar"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarColPermisosUnion(ByVal colPermisosAuxiliar As ColPermisoDN) As ColPermisoDN
        RecuperarColPermisosUnion = Me

        If colPermisosAuxiliar IsNot Nothing Then
            For Each permisoR As Framework.Usuarios.DN.PermisoDN In colPermisosAuxiliar
                If Not Me.ContienePermisoxTipo(permisoR.TipoPermiso) Then
                    RecuperarColPermisosUnion.Add(permisoR)
                End If
            Next
        End If

    End Function

#End Region

End Class
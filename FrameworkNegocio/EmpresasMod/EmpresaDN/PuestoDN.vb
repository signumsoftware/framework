Imports Framework.Usuarios.DN

<Serializable()> _
Public Class PuestoDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

#Region "Atributos"

    Protected mDepartamentoNTareaN As DepartamentoNTareaNDN
    Protected mColRoles As ColRolDN

#End Region

#Region "Propiedades"

    Public Property DepartamentoNTareaN() As DepartamentoNTareaNDN
        Get
            Return mDepartamentoNTareaN
        End Get
        Set(ByVal value As DepartamentoNTareaNDN)
            Me.CambiarValorRef(Of DepartamentoNTareaNDN)(value, mDepartamentoNTareaN)
        End Set
    End Property

    Public Property ColRoles() As ColRolDN
        Get
            Return mColRoles
        End Get
        Set(ByVal value As ColRolDN)
            CambiarValorCol(Of ColRolDN)(value, mColRoles)
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarDepartamentoNTarea(ByRef mensaje As String, ByVal departamentoTarea As DepartamentoNTareaNDN) As Boolean
        If departamentoTarea Is Nothing Then
            mensaje = "El DepartamentoNTareaN no puede ser nulo"
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' Valida que la colección de roles esté contenida en la colección de roles
    ''' correspondientes al departamento
    ''' </summary>
    ''' <param name="mensaje"></param>
    ''' <param name="colRoles"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function ValidarColRoles(ByRef mensaje As String, ByVal colRoles As ColRolDN) As Boolean
        If colRoles IsNot Nothing AndAlso colRoles.Count > 1 Then
            If mDepartamentoNTareaN Is Nothing OrElse mDepartamentoNTareaN.ColRoles Is Nothing OrElse Not mDepartamentoNTareaN.ColRoles.ContieneColRoles(colRoles) Then
                mensaje = "La colección de roles del puesto tiene que estar contenida en la colección de roles de su departamento"
                Return False
            End If
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarDepartamentoNTarea(pMensaje, mDepartamentoNTareaN) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If Not ValidarColRoles(pMensaje, mColRoles) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Me.mToSt = Me.ToString

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Overrides Function ToString() As String
        Dim departamento As String = ""

        If mDepartamentoNTareaN IsNot Nothing AndAlso mDepartamentoNTareaN.Departamento IsNot Nothing AndAlso Not String.IsNullOrEmpty(mDepartamentoNTareaN.Departamento.Nombre) Then
            departamento = ", Dpto.- " & mDepartamentoNTareaN.Departamento.Nombre
        End If

        Return mNombre & departamento

    End Function

#End Region

End Class
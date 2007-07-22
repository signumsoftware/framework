Imports Framework.Usuarios.DN

<Serializable()> Public Class DepartamentoNTareaNDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

#Region "Atributos"

    Protected mDepartamento As DepartamentoDN
    Protected mColRoles As ColRolDN

#End Region

#Region "Constructores"
    'Public Sub New()
    '    Me.CambiarValorRef(Of ProcesosDN.ColOperacionBaseDN)(New ProcesosDN.ColOperacionBaseDN, mColNucleoTareaNegocio)
    '    'Me.CambiarValorRef(Of DepartamentoDN)(New DepartamentoDN, mDepartamento)

    '    Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Inconsistente
    'End Sub
#End Region

#Region "Propiedades"

    Public Property ColRoles() As ColRolDN
        Get
            Return mColRoles
        End Get
        Set(ByVal value As ColRolDN)
            CambiarValorCol(Of ColRolDN)(value, mColRoles)
        End Set
    End Property

    Public Property Departamento() As DepartamentoDN
        Get
            Return Me.mDepartamento
        End Get
        Set(ByVal value As DepartamentoDN)
            Me.CambiarValorRef(Of DepartamentoDN)(value, mDepartamento)
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarDepartamento(ByRef mensaje As String, ByVal departamento As DepartamentoDN) As Boolean
        If departamento Is Nothing Then
            mensaje = "El departamento no puede ser nulo"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarDepartamento(pMensaje, mDepartamento) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Function RecuperarColOperaciones() As Framework.Procesos.ProcesosDN.ColOperacionDN
        If mColRoles IsNot Nothing Then
            Return mColRoles.RecuperarColOperaciones()
        Else
            Return Nothing
        End If
    End Function

#End Region


End Class

<Serializable()> Public Class CentroCostesDepartamentoDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN


    Protected mDepartamento As FN.Empresas.DN.DepartamentoDN
    Protected mCentroCostes As CentroCostesDN

    Public Property CentroCostes() As CentroCostesDN
        Get
            Return Me.mCentroCostes
        End Get
        Set(ByVal value As CentroCostesDN)
            Me.CambiarValorRef(Of CentroCostesDN)(value, mCentroCostes)

        End Set
    End Property

    Public Property Departamento() As FN.Empresas.DN.DepartamentoDN
        Get
            Return Me.mDepartamento
        End Get
        Set(ByVal value As FN.Empresas.DN.DepartamentoDN)
            Me.CambiarValorRef(Of FN.Empresas.DN.DepartamentoDN)(value, mDepartamento)
        End Set
    End Property

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If mDepartamento Is Nothing Then
            pMensaje = "mDepartamento no puede ser nothing"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If mCentroCostes Is Nothing Then
            pMensaje = "mCentroCostes no puede ser nothing"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Me.mToSt = mCentroCostes.Nombre & " - " & mDepartamento.Nombre


        Return MyBase.EstadoIntegridad(pMensaje)
    End Function


End Class

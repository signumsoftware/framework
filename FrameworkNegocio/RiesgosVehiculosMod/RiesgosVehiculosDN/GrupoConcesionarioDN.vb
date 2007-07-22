Imports Framework.DatosNegocio

Imports FN.Empresas.DN
Imports FN.Personas.DN

<Serializable()> _
Public Class GrupoConcesionarioDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mColEmpresas As ColEmpresasDN
    Protected mColPersonasContacto As ColContactoPersonaDN

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        CambiarValorCol(Of ColEmpresasDN)(New ColEmpresasDN(), mColEmpresas)
        CambiarValorCol(Of ColContactoPersonaDN)(New ColContactoPersonaDN(), mColPersonasContacto)
    End Sub

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mColEmpresas")> _
    Public Property ColEmpresas() As ColEmpresasDN
        Get
            Return mColEmpresas
        End Get
        Set(ByVal value As ColEmpresasDN)
            CambiarValorCol(Of ColEmpresasDN)(value, mColEmpresas)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColPersonasContacto")> _
    Public Property ColPersonasContacto() As ColContactoPersonaDN
        Get
            Return mColPersonasContacto
        End Get
        Set(ByVal value As ColContactoPersonaDN)
            CambiarValorCol(Of ColContactoPersonaDN)(value, mColPersonasContacto)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mColEmpresas Is Nothing OrElse mColEmpresas.Count = 0 Then
            pMensaje = "El grupo deber tener al menos una empresa asignada"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If mColPersonasContacto Is Nothing OrElse mColPersonasContacto.Count = 0 Then
            pMensaje = "El grupo debe tener al menos una persona de contacto"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class

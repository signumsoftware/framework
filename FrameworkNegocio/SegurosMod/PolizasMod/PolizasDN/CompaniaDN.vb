Imports Framework.DatosNegocio
Imports FN.Empresas.DN

<Serializable()> _
Public Class CompaniaDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mEmpresa As EmpresaDN

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mEmpresa")> _
    Public Property Empresa() As EmpresaDN
        Get
            Return mEmpresa
        End Get
        Set(ByVal value As EmpresaDN)
            CambiarValorRef(Of EmpresaDN)(value, mEmpresa)
        End Set
    End Property

#End Region


End Class

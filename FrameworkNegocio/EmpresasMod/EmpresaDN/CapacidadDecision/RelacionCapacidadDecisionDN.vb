Imports FN.Personas.DN




''' <summary>
''' indica que una persona judirica tinen capacidad de decisión en una empresa, es decir forma parte de la junta directiva
''' impide la entidad referida no sea una empresa y que la referidora no sea una ientidad fiscal
''' </summary>
''' <remarks></remarks>
Public Class RelacionCapacidadDecisionEmpresaFiscalDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements Framework.DatosNegocio.IRelacionPorcentual


    Protected mNombreTipoRelacion As String
    Protected mPorcentajeRelacion As Double
    Protected mEmpresaDN As EmpresaDN
    Protected mDirectivo As FN.Localizaciones.DN.IEntidadFiscalDN



    Public Property EntidadReferida() As Framework.DatosNegocio.IEntidadDN Implements Framework.DatosNegocio.IRelacionPorcentual.EntidadReferida
        Get

        End Get
        Set(ByVal value As Framework.DatosNegocio.IEntidadDN)

        End Set
    End Property

    Public Property EntidadReferidora() As Framework.DatosNegocio.IEntidadDN Implements Framework.DatosNegocio.IRelacionPorcentual.EntidadReferidora
        Get

        End Get
        Set(ByVal value As Framework.DatosNegocio.IEntidadDN)

        End Set
    End Property

    Public ReadOnly Property NombreTipoRelacion() As String Implements Framework.DatosNegocio.IRelacionPorcentual.NombreTipoRelacion
        Get

        End Get
    End Property

    Public Property PorcentajeRelacion() As Double Implements Framework.DatosNegocio.IRelacionPorcentual.PorcentajeRelacion
        Get

        End Get
        Set(ByVal value As Double)

        End Set
    End Property
End Class
Public Class ColRelacionCapacidadDecisionEmpresaFiscalDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of RelacionCapacidadDecisionEmpresaFiscalDN)
End Class
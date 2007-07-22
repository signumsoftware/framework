Imports Framework.DatosNegocio
Imports FN.Personas.DN
Imports FN.Localizaciones.DN

Public Class RelacionPropiedadEmpresaDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements Framework.DatosNegocio.IRelacionPorcentual



    Protected mNombreTipoRelacion As String
    Protected mPorcentajeRelacion As Double
    Protected mEntidadReferida As EmpresaDN
    Protected mEntidadReferidora As IEntidadFiscalDN


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


<Serializable()> Public Class ColIRelacionPropiedadDN
    Inherits ArrayListValidable(Of RelacionPropiedadEmpresaDN)

    ' metodos de coleccion
    '
End Class

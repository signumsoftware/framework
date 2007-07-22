Imports Framework.TiposYReflexion.DN

<Serializable()> Public Class VcEjecutorDeVerboEnClienteDN
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mClientedeFachada As ClientedeFachadaDN
    Protected mVerbo As VerboDN
    Protected mVinculoMetodo As Framework.TiposYReflexion.DN.VinculoMetodoDN
    Public Property ClientedeFachada() As ClientedeFachadaDN
        Get
            Return mClientedeFachada
        End Get
        Set(ByVal value As ClientedeFachadaDN)
            Me.CambiarValorRef(Of ClientedeFachadaDN)(value, mClientedeFachada)
        End Set
    End Property


    Public Property Verbo() As VerboDN
        Get
            Return Me.mVerbo
        End Get
        Set(ByVal value As VerboDN)
            Me.CambiarValorRef(Of VerboDN)(value, mVerbo)

        End Set
    End Property

    Public Property VinculoMetodo() As VinculoMetodoDN
        Get
            Return mVinculoMetodo
        End Get
        Set(ByVal value As VinculoMetodoDN)
            Me.CambiarValorRef(Of VinculoMetodoDN)(value, mVinculoMetodo)

        End Set
    End Property


End Class


<Serializable()> Public Class ColVcEjecutorDeVerboEnClienteDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of VcEjecutorDeVerboEnClienteDN)


    Public Function RecuperarxNombreVerbo(ByVal pNombreVerbo As String) As VcEjecutorDeVerboEnClienteDN
        For Each vce As VcEjecutorDeVerboEnClienteDN In Me
            If vce.Verbo.Nombre.ToLower = pNombreVerbo.ToLower Then
                Return vce
            End If
        Next
        Return Nothing
    End Function

End Class





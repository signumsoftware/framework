
<Serializable()> Public Class EjecutoresDeClienteDN
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mClientedeFachada As ClientedeFachadaDN
    Protected mColVcEjecutorDeVerboEnCliente As ColVcEjecutorDeVerboEnClienteDN

    Public Sub New()

        Me.CambiarValorRef(Of ColVcEjecutorDeVerboEnClienteDN)(New ColVcEjecutorDeVerboEnClienteDN, mColVcEjecutorDeVerboEnCliente)
        Me.modificarEstado = DatosNegocio.EstadoDatosDN.Inconsistente
    End Sub
    Public Function RecuperarMethodInfoEjecutor(ByVal pVerbo As Framework.Procesos.ProcesosDN.VerboDN) As System.Reflection.MethodInfo

        Dim miVcEjecutorDeVerboEnCliente As VcEjecutorDeVerboEnClienteDN = mColVcEjecutorDeVerboEnCliente.RecuperarxNombreVerbo(pVerbo.Nombre)
        If miVcEjecutorDeVerboEnCliente Is Nothing Then
            Throw New ApplicationException("no se recuperó ningun ejecutor para el verbo :" & pVerbo.Nombre)
        End If
        Return miVcEjecutorDeVerboEnCliente.VinculoMetodo.RecuperarMethodInfo()

    End Function
    Public Function RecuperarTipoEjecutor(ByVal pVerbo As Framework.Procesos.ProcesosDN.VerboDN) As System.Type

        Dim miVcEjecutorDeVerboEnCliente As VcEjecutorDeVerboEnClienteDN = mColVcEjecutorDeVerboEnCliente.RecuperarxNombreVerbo(pVerbo.Nombre)
        Return miVcEjecutorDeVerboEnCliente.VinculoMetodo.VinculoClase.TipoClase

    End Function
    Public Property ClientedeFachada() As ClientedeFachadaDN
        Get
            Return mClientedeFachada
        End Get
        Set(ByVal value As ClientedeFachadaDN)
            Me.CambiarValorRef(Of ClientedeFachadaDN)(value, mClientedeFachada)
        End Set
    End Property

    Public Property ColVcEjecutorDeVerboEnCliente() As ColVcEjecutorDeVerboEnClienteDN
        Get
            Return Me.mColVcEjecutorDeVerboEnCliente
        End Get
        Set(ByVal value As ColVcEjecutorDeVerboEnClienteDN)
            Me.CambiarValorRef(Of ColVcEjecutorDeVerboEnClienteDN)(value, mColVcEjecutorDeVerboEnCliente)

        End Set
    End Property


End Class


<Serializable()> Public Class ColEjecutoresDeClienteDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of EjecutoresDeClienteDN)

    Public Function RecuperarXNombreCliente(ByVal pNombreCleinte As String) As EjecutoresDeClienteDN

        For Each ejc As EjecutoresDeClienteDN In Me
            If ejc IsNot Nothing Then
                If ejc.ClientedeFachada.Nombre.ToLower = pNombreCleinte.ToLower Then
                    Return ejc
                End If
            End If


        Next

        Return Nothing

    End Function

End Class


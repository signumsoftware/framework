Imports Framework.Procesos.ProcesosDN

<Serializable()> Public Class OprrTrazaDN
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mOprr As OperacionRealizadaDN

    Public Sub New()

    End Sub


    'Public Sub New(ByVal pOprr As OperacionRealizadaDN)
    '    Me.CambiarValorRef(Of OperacionRealizadaDN)(pOprr, mOprr)
    '    Me.modificarEstado = DatosNegocio.EstadoDatosDN.SinModificar
    'End Sub

    Public Property Oprr() As OperacionRealizadaDN
        Get
            Return mOprr
        End Get
        Set(ByVal value As Framework.Procesos.ProcesosDN.OperacionRealizadaDN)
            Me.CambiarValorRef(Of OperacionRealizadaDN)(value, mOprr)

        End Set
    End Property




End Class

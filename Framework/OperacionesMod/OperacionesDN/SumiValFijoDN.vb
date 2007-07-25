<Serializable()> _
Public Class SumiValFijoDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN


    Private mvalor As Double


    Public Sub New()

    End Sub

    Public Sub New(ByVal pvalor As Double)
        mvalor = pvalor

    End Sub

    Public Property valor() As Double
        Get
            Return Me.mvalor
        End Get
        Set(ByVal value As Double)
            Me.CambiarValorVal(Of Double)(value, Me.mvalor)
        End Set
    End Property


    Public Function GetValor() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.GetValor
        Return mvalor
    End Function

    Public Property IRecSumiValorLN() As Framework.Operaciones.OperacionesDN.IRecSumiValorLN Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.IRecSumiValorLN
        Get

        End Get
        Set(ByVal value As Framework.Operaciones.OperacionesDN.IRecSumiValorLN)

        End Set
    End Property

    Public ReadOnly Property ValorCacheado() As Object Implements ISuministradorValorDN.ValorCacheado
        Get

        End Get
    End Property

    Public Function RecuperarOrden() As Integer Implements ISuministradorValorDN.RecuperarOrden
        Throw New NotImplementedException("Recuperar orden no está implementado para esta clase")
    End Function

    Public Sub Limpiar() Implements ISuministradorValorDN.Limpiar

    End Sub
End Class
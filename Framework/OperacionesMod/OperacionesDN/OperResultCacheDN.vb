
<Serializable()> _
Public Class OperResultCacheDN
    Inherits Framework.DatosNegocio.HuellaEntidadTipadaDN(Of IOperacionSimpleDN) ' no es una huella cache porque no necesita que el motor refresque nada

    Implements ISuministradorValorDN

    Protected mValor As String


    Public Sub New(ByVal pIOPS As IOperacionSimpleDN)
        MyBase.New(pIOPS, DatosNegocio.HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir)
        Me.mValor = pIOPS.ValorCacheado
        Me.Nombre = CType(pIOPS, Framework.DatosNegocio.IEntidadBaseDN).Nombre
    End Sub

    Public Sub ActualizarValor(ByVal pvalor As String)
        Me.CambiarValorVal(Of String)(pvalor, Me.mValor)
    End Sub

    Public Function GetValor() As Object Implements ISuministradorValorDN.GetValor
        Return Me.mValor
    End Function

    Public Property IRecSumiValorLN() As IRecSumiValorLN Implements ISuministradorValorDN.IRecSumiValorLN
        Get

        End Get
        Set(ByVal value As IRecSumiValorLN)

        End Set
    End Property
    Public Overrides Function ToString() As String

        Return Me.ID & " " & Me.mValor & " " & Me.mNombre
    End Function

    Public ReadOnly Property ValorCacheado() As Object Implements ISuministradorValorDN.ValorCacheado
        Get
            Return mValor
        End Get

    End Property

    Public Function RecuperarOrden() As Integer Implements ISuministradorValorDN.RecuperarOrden
        Throw New NotImplementedException("Recuperar orden no está implementado para esta clase")
    End Function

    Public Sub Limpiar() Implements ISuministradorValorDN.Limpiar
        'mValor = Nothing
        'Nombre = Nothing
    End Sub
End Class

Public Class ColOperResultCacheDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of OperResultCacheDN)


    Public Function RecuperarxGuidEntReferida(ByVal pGuidRef As String) As OperResultCacheDN

        For Each oper As OperResultCacheDN In Me

            If oper.GUIDReferida = pGuidRef Then
                Return oper
            End If

        Next
        Return Nothing


    End Function

End Class
#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region
<Serializable()> Public Class DatoConfiguracionDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mColDatosConfigurables As ColDatoConfigurableDN

#End Region

#Region "Constructor"

    Public Sub New()
        MyBase.New()
        If Me.mColDatosConfigurables Is Nothing Then
            Me.mColDatosConfigurables = New ColDatoConfigurableDN
        End If
    End Sub

    Public Sub New(ByVal pColDatosConfigurables As ColDatoConfigurableDN)
        Dim mensaje As String = String.Empty
        If Me.ValidarColDatosConfigurables(mensaje, pColDatosConfigurables) Then
            Me.CambiarValorRef(Of ColDatoConfigurableDN)(pColDatosConfigurables, mColDatosConfigurables)
        Else
            Throw New ApplicationException(mensaje)
        End If
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"

    Public Property ColDatosConfigurables() As ColDatoConfigurableDN
        Get
            Return Me.mColDatosConfigurables
        End Get
        Set(ByVal value As ColDatoConfigurableDN)
            Dim mensaje As String
            If Me.ValidarColDatosConfigurables(mensaje, value) Then
                Me.CambiarValorRef(Of ColDatoConfigurableDN)(value, mColDatosConfigurables)
            Else
                Throw New ApplicationException(mensaje)
            End If
        End Set
    End Property

#End Region

#Region "Validacion"

    Private Function ValidarColDatosConfigurables(ByRef mensaje As String, ByVal pColDatosConfigurables As ColDatoConfigurableDN) As Boolean
        If Not pColDatosConfigurables Is Nothing Then
            Return True
        End If
        Return False
    End Function

#End Region

End Class

<Serializable()> Public Class ColDatoConfiguracionDN
    Inherits ArrayListValidable(Of DatoConfiguracionDN)

End Class
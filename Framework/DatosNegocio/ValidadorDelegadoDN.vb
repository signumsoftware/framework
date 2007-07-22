Public Delegate Function ValidacionDelegada(ByRef mensaje As String, ByVal sender As Object, ByVal pValor As Object) As Boolean


<Serializable()> Public Class ValidadorDelegadoDN
    Implements Framework.DatosNegocio.IValidador


    Dim mdelegadov As ValidacionDelegada
    Dim msender As Object

    Public Sub New(ByVal psender As Object, ByVal pDelegado As ValidacionDelegada)
        If pDelegado Is Nothing Then
            Throw New ApplicationException
        End If
        mdelegadov = pDelegado
    End Sub

    Public Property sender() As Object
        Get
            Return msender
        End Get
        Set(ByVal value As Object)
            msender = value
        End Set
    End Property

    Public Function Formula() As String Implements Framework.DatosNegocio.IValidador.Formula
        Return mdelegadov.ToString
    End Function

    Public Function Validacion(ByRef mensaje As String, ByVal pValor As Object) As Boolean Implements Framework.DatosNegocio.IValidador.Validacion
        Return mdelegadov(mensaje, msender, pValor)
    End Function
End Class

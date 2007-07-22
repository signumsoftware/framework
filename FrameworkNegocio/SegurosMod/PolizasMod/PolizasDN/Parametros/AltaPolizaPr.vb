<Serializable()> Public Class AltaPolizaPr
    Inherits Framework.DatosNegocio.EntidadDN



    Protected mMatricula As String
    Protected mCifNif As String


    Public Property CifNif() As String

        Get
            Return mCifNif
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mCifNif)

        End Set
    End Property



    Public Property Matricula() As String

        Get
            Return mMatricula
        End Get

        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mMatricula)

        End Set
    End Property








End Class

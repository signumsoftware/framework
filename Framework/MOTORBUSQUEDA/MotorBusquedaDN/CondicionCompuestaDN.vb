<Serializable()> _
Public Class CondicionCompuestaDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements ICondicionDN


    Protected mfactor1 As ICondicionDN
    Protected moperadorRelacional As OperadoresRelacionales
    Protected mfactor2 As ICondicionDN

    Public Property Factor1() As ICondicionDN Implements ICondicionDN.Factor1
        Get
            Return mfactor1
        End Get
        Set(ByVal value As ICondicionDN)
            Me.CambiarValorVal(Of String)(value, mfactor1)
            '¡    mfactor1 = value
        End Set
    End Property

    Public Property Factor2() As ICondicionDN Implements ICondicionDN.Factor2
        Get
            Return mfactor2
        End Get
        Set(ByVal value As ICondicionDN)
            '   mfactor2 = value
            Me.CambiarValorVal(Of String)(value, mfactor2)

        End Set
    End Property

    Public Property OperadorRelacional() As OperadoresRelacionales Implements ICondicionDN.OperadorRelacional
        Get
            Return moperadorRelacional
        End Get
        Set(ByVal value As OperadoresRelacionales)
            ' moperadorRelacional = value
            Me.CambiarValorVal(Of Integer)(value, moperadorRelacional)
        End Set
    End Property

    Public Function evaluacion() As Boolean Implements ICondicionDN.evaluacion
        Select Case Me.moperadorRelacional
            Case OperadoresRelacionales.O
                Return Me.Factor1.evaluacion OrElse Me.Factor2.evaluacion

            Case OperadoresRelacionales.Y
                Return Me.Factor1.evaluacion AndAlso Me.Factor2.evaluacion
        End Select
    End Function
End Class

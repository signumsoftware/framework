<AttributeUsage(AttributeTargets.Property)> Public Class RelacionPropCampoAtribute
    Inherits System.Attribute
    Protected mNombreCampo As String
    Public Sub New(ByVal pNombreCampo As String)
        mNombreCampo = pNombreCampo

    End Sub


    Public ReadOnly Property NombreCampo() As String
        Get
            Return mNombreCampo
        End Get
    End Property

End Class



#Region "Importaciones"
Imports Framework.Usuarios.DN
Imports Framework.Usuarios.IUWin.AS
#End Region

Public Class RolLNC

#Region "Atributos"
    Protected mSession As Hashtable
#End Region

#Region "Constructores"
    Public Sub New(ByVal pDatosMarco As Hashtable)
        mSession = pDatosMarco
    End Sub
#End Region

#Region "Métodos"

    Public Function RecuperarColRol() As ColRolDN
        Dim miAS As UsuariosAS
        miAS = New UsuariosAS()

        Return miAS.RecuperarColRol()
    End Function

#End Region

End Class

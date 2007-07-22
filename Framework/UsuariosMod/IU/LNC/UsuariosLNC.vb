#Region "Importaciones"

Imports Framework.Usuarios.DN
Imports Framework.Usuarios.IUWin.AS

#End Region

Public Class UsuariosLNC

#Region "Atributos"
    Protected mSession As Hashtable
#End Region

#Region "Constructores"

    Public Sub New()

    End Sub

    Public Sub New(ByVal pDatosMarco As Hashtable)
        mSession = pDatosMarco
    End Sub
#End Region

#Region "Métodos"

    Public Function RecuperarListadoUsuarios() As DataSet
        Dim miAS As UsuariosAS
        miAS = New UsuariosAS()

        Return miAS.RecuperarListadoUsuarios()

    End Function

    Public Function ObtenerPrincipal(ByVal id As String) As PrincipalDN
        Dim miAS As UsuariosAS
        miAS = New UsuariosAS()

        Return miAS.ObtenerPrincipal(id)
    End Function

    Public Function AltaPrincipal(ByVal principal As PrincipalDN, ByVal di As DatosIdentidadDN) As PrincipalDN
        Dim miAS As New UsuariosAS()
        Return miAS.AltaPrincipal(principal, di)
    End Function

    Public Function BajaPrincipal(ByVal principal As PrincipalDN) As PrincipalDN
        Dim miAS As New UsuariosAS()
        Return miAS.BajaPrincipal(principal)
    End Function

    ''' <summary>
    ''' Método LNC para guardar un principal, adapatado al motor de visualización genérica
    ''' </summary>
    ''' <param name="control"></param>
    ''' <param name="principal"></param>
    ''' <param name="vincMetodo"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GuardarPrincipal(ByVal control As Object, ByVal principal As PrincipalDN, ByVal vincMetodo As Object) As PrincipalDN
        Dim miAS As New UsuariosAS()

        If Not String.IsNullOrEmpty(principal.ID) AndAlso String.IsNullOrEmpty(principal.ClavePropuesta) Then
            Return miAS.GuardarPrincipal(principal)
        Else
            Dim di As New DatosIdentidadDN()
            di.Nick = principal.Nombre
            di.Clave = principal.ClavePropuesta

            Return miAS.GuardarPrincipal(principal, di)
        End If

    End Function

#End Region


End Class

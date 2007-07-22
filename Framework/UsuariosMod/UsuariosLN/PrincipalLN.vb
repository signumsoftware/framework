Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Usuarios.DN
Public Class PrincipalLN

    Public Sub New()

    End Sub

    Public Function AltaPrincipalClavePropuesta(ByVal pPrincipal As Usuarios.DN.PrincipalDN, ByVal pTransicionRealizadaDes As Procesos.ProcesosDN.TransicionRealizadaDN, ByVal solicitante As Object) As PrincipalDN




        Using tr As New Transaccion



            Dim miDatosIdentidad As New DatosIdentidadDN(pPrincipal.Nombre, pPrincipal.ClavePropuesta)
            pPrincipal.UsuarioDN = New UsuarioDN(pPrincipal.Nombre, False)

            Dim ln As New UsuariosLN(Transaccion.Actual, Recurso.Actual)
            AltaPrincipalClavePropuesta = ln.AltaPrincipal(pPrincipal, miDatosIdentidad)
            tr.Confirmar()

        End Using







    End Function




End Class

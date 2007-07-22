Imports Framework.Usuarios.DN

<Serializable()> Public Class TrazaMetodoSistemaDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Campos"

    Protected mPrincipal As PrincipalDN
    Protected mNombreMetodo As String
    Protected mMensajeExcepcion As String
    Protected mDatos As String
    Protected mIdentificadorSesion As String
    Protected mFecha As Date


#End Region

    Public Sub New()

    End Sub
    Public Sub New(ByVal pPrincipal As PrincipalDN, ByVal pNombreMetodo As String, ByVal pMensajeExcepcion As String, ByVal pDatos As String, ByVal pFecha As Date, ByVal pIdentificadorSesion As String)
        mPrincipal = pPrincipal
        mNombreMetodo = pNombreMetodo
        mMensajeExcepcion = pMensajeExcepcion
        mDatos = pDatos
        mFecha = pFecha
        mIdentificadorSesion = pIdentificadorSesion
    End Sub
End Class
<Serializable()> Public Class ContenedorImagenDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "atributos"

    Protected mImagen As System.Drawing.Image
    Protected mImagenX As Single
    Protected mImagenY As Single
    Protected mAplicarDesviacion As Boolean

#End Region

#Region "constructores"

    Public Sub New()

    End Sub

    Public Sub New(ByVal pNombre As String, ByVal pImagen As System.Drawing.Image, ByVal pImagenX As Single, ByVal pImagenY As Single, ByVal pAplicarDesviacion As Boolean)
        Me.CambiarValorVal(pNombre, mNombre)
        Me.CambiarValorRef(pImagen, mImagen)
        Me.CambiarValorVal(pImagenX, Me.mImagenX)
        Me.CambiarValorVal(pImagenY, Me.mImagenY)
        Me.CambiarValorVal(pAplicarDesviacion, Me.mAplicarDesviacion)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Modificado
    End Sub

#End Region

#Region "propiedades"
    Public Property Imagen() As System.Drawing.Image
        Get
            Return Me.mImagen
        End Get
        Set(ByVal value As System.Drawing.Image)
            Me.CambiarValorRef(value, Me.mImagen)
        End Set
    End Property

    Public Property ImagenX() As Single
        Get
            Return Me.mImagenX
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(value, Me.mImagenX)
        End Set
    End Property

    Public Property ImagenY() As Single
        Get
            Return Me.mImagenY
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(value, Me.mImagenY)
        End Set
    End Property

    Public Property AplicarDesviacion() As Boolean
        Get
            Return Me.mAplicarDesviacion
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(value, Me.mAplicarDesviacion)
        End Set
    End Property
#End Region

#Region "métodos"

#End Region


End Class

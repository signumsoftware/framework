Public Class PruebaControlBasico

    Implements Framework.IU.IUComun.IctrlBasicoDN

    Dim mdn As Framework.DatosNegocio.EntidadDN



    Private Sub PruebaControlBasico_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub

    Public Property DN() As Object Implements Framework.IU.IUComun.IctrlBasicoDN.DN
        Get
            Return mdn
        End Get
        Set(ByVal value As Object)
            mdn = value
        End Set
    End Property

    Public Sub DNaIUgd() Implements Framework.IU.IUComun.IctrlBasicoDN.DNaIUgd
        Me.TextBox1.Text = mdn.Nombre
    End Sub

    Public Sub IUaDNgd() Implements Framework.IU.IUComun.IctrlBasicoDN.IUaDNgd
        mdn.Nombre = Me.TextBox1.Text
    End Sub

    Public Sub Poblar() Implements Framework.IU.IUComun.IctrlBasicoDN.Poblar

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Me.Label1.Text = Me.TextBox1.Text
    End Sub
End Class

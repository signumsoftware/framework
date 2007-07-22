Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms




Public Class ImagenTextBoxXT
    Inherits UserControl

    Private mOver As Boolean = False
    Public mTXT As textboxXT
    Public MargenTop As Integer = 1
    Public MargenRight As Integer = 1

    Public Sub New(ByVal pTxt As textboxXT)
        Me.mTXT = pTxt

        'para el anchor
        AddHandler mTXT.LocationChanged, AddressOf Relocalizar
        'para el anchor y el resize
        AddHandler mTXT.SizeChanged, AddressOf Relocalizar
        'para el color de fondo del control
        AddHandler mTXT.BackColorChanged, AddressOf Refrescar
        AddHandler mTXT.EnabledChanged, AddressOf Refrescar
        'para la visibilidad
        AddHandler mTXT.VisibleChanged, AddressOf EstablecerVisibilidad


        'tamañopor defecto
        Me.Size = New Size(9, 9)

        'situamos el control en las coordenadas correctas
        Relocalizar(Nothing, Nothing)
        'ponemos el color correcto
        'EstablecerColorFondo(Nothing, Nothing)
    End Sub

    Private Sub Refrescar(ByVal sender As Object, ByVal e As System.EventArgs)
        Me.Refresh()
    End Sub

    Private Sub Relocalizar(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim x As Integer = Me.mTXT.Right - Me.Width - MargenRight
        Dim y As Integer = Me.mTXT.Top + MargenTop
        Me.Location = New Point(x, y)
    End Sub

    'Private Sub EstablecerColorFondo(ByVal sender As Object, ByVal e As System.EventArgs)
    '    Me.BackColor = Me.mTXT.BackColor
    'End Sub

    Private Sub EstablecerVisibilidad(ByVal sender As Object, ByVal e As System.EventArgs)
        Me.Visible = Me.mTXT.Visible
    End Sub


    Protected Overrides Sub OnPaint(ByVal e As System.Windows.Forms.PaintEventArgs)
        Dim gr As Graphics = e.Graphics

        Dim ColorFondoTriangulo As Color

        If Me.mOver Then
            ColorFondoTriangulo = Color.Yellow
        Else
            ColorFondoTriangulo = Color.DarkGreen
        End If


        Dim pa As New GraphicsPath()

        pa.StartFigure()
        'definimos los puntos del triángulo
        Dim p1 As New Point(0, 0)
        Dim p2 As New Point(Me.Width, 0)
        Dim p3 As New Point(Me.Width, Me.Height)

        'las líneas que forman el triángulo
        pa.AddLine(p1, p2)
        pa.AddLine(p2, p3)
        pa.AddLine(p3, p1)

        pa.CloseFigure()

        'pintamos el triángulo
        Dim pincel As PathGradientBrush = New PathGradientBrush(pa)
        pincel.CenterColor = ColorFondoTriangulo
        pincel.SurroundColors = New Color() {Color.AliceBlue, Color.Gray, Color.White}

        'Dim mir As New Region(pa)

        'gr.SetClip(pa)

        Dim brbk As SolidBrush
        Dim colorf As Color = Me.mTXT.BackColor

        If Me.mTXT.Enabled OrElse colorf <> SystemColors.Window Then
            brbk = New SolidBrush(colorf)
        Else
            brbk = SystemBrushes.Control.Clone
        End If


        gr.FillRectangle(brbk, New Rectangle(0, 0, Me.Width, Me.Height))

        gr.FillPath(pincel, pa)


        'limpiamos
        brbk.Dispose()
        pincel.Dispose()
        pa.Dispose()

        'por si alguien hereda y necesita el evento
        MyBase.OnPaint(e)

    End Sub


    Protected Overrides Sub OnMouseEnter(ByVal e As System.EventArgs)
        Me.mOver = True
        MyBase.OnMouseEnter(e)
        Me.Refresh()
    End Sub

    Protected Overrides Sub OnMouseLeave(ByVal e As System.EventArgs)
        Me.mOver = False
        MyBase.OnMouseHover(e)
        Me.Refresh()
    End Sub

    Protected Overrides Sub OnMouseClick(ByVal e As System.Windows.Forms.MouseEventArgs)
        Dim frmTextoDetalle As New frmTextoExtendido

        frmTextoDetalle.Texto = Me.mTXT.Text

        frmTextoDetalle.Habilitado = Not Me.mTXT.ReadonlyXT

        If frmTextoDetalle.ShowDialog = DialogResult.OK AndAlso frmTextoDetalle.Habilitado Then
            Me.mTXT.Text = frmTextoDetalle.Texto
        End If

        frmTextoDetalle.Dispose()

        MyBase.OnMouseClick(e)
    End Sub

End Class

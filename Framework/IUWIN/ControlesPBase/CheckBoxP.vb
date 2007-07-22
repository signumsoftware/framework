Public Class CheckBoxP
    Inherits Windows.Forms.CheckBox

    Private mIlumnarSeleccion As Boolean
    Private mColorBaseIluminacion As Color
    Private mColorIluminacion As Color
    Private mMouseOver As Boolean
    Private mNivelTransparenciaIluminacion As Integer

    Public Sub New()
        MyBase.New()

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Establecemos las propiedades iniciales en el orden correcto
        mIlumnarSeleccion = True
        mColorBaseIluminacion = Color.Orange
        mNivelTransparenciaIluminacion = 64
        mColorIluminacion = Color.FromArgb(mNivelTransparenciaIluminacion, mColorBaseIluminacion)

    End Sub

    Private Sub CrearColor()
        Me.mColorIluminacion = Color.FromArgb(64, mColorBaseIluminacion)
    End Sub

    ''' <summary>
    ''' Determina si cuando reciba el foco o en el evento mouseOver el checkbox 
    ''' se iluminará con algún color
    ''' </summary>
    <System.ComponentModel.DefaultValue(GetType(Boolean), "True")> _
    Public Property IluminarSeleccion() As Boolean
        Get
            Return Me.mIlumnarSeleccion
        End Get
        Set(ByVal value As Boolean)
            Me.mIlumnarSeleccion = value
            If Me.mIlumnarSeleccion = True Then
                Me.FlatAppearance.MouseOverBackColor = Me.mColorIluminacion
            End If
        End Set
    End Property

    ''' <summary>
    ''' El color base con el que se iluminará el chechkbox si la propiedad
    ''' IluminarSeleccion es true
    ''' </summary>
    <System.ComponentModel.DefaultValue(GetType(System.Drawing.Color), "Color.Orange")> _
    Public Property ColorBaseIluminacion() As Color
        Get
            Return Me.mColorBaseIluminacion
        End Get
        Set(ByVal value As Color)
            Me.mColorBaseIluminacion = value
            CrearColor()
        End Set
    End Property

    ''' <summary>
    ''' Determina el nivel de transparencia del color que iluminará el checkbox
    ''' si la propiedad IluminarSeleccion es true
    ''' </summary>
    <System.ComponentModel.DefaultValue(GetType(Integer), "64")> _
    Public Property NivelTransparenciaIluminacion() As Integer
        Get
            Return Me.mNivelTransparenciaIluminacion
        End Get
        Set(ByVal value As Integer)
            Me.mNivelTransparenciaIluminacion = value
            CrearColor()
        End Set
    End Property

    Protected Overrides Sub OnMouseEnter(ByVal eventargs As System.EventArgs)
        Me.mMouseOver = True
        Me.Refresh()
        MyBase.OnMouseEnter(eventargs)
    End Sub

    'Protected Overrides Sub OnMouseHover(ByVal e As System.EventArgs)
    '    Me.mMouseOver = True
    '    Me.Refresh()
    '    MyBase.OnMouseHover(e)
    'End Sub

    Protected Overrides Sub OnMouseLeave(ByVal eventargs As System.EventArgs)
        Me.mMouseOver = False
        Me.Refresh()
        MyBase.OnMouseLeave(eventargs)
    End Sub

    Protected Overrides Sub OnPaint(ByVal e As System.Windows.Forms.PaintEventArgs)
        MyBase.OnPaint(e)
        'Add your custom paint code here

        If Me.mIlumnarSeleccion Then
            If Me.Focused OrElse Me.mMouseOver Then
                Dim rect As New Rectangle(0, 0, Me.Width, Me.Height)
                Using brush As New SolidBrush(Me.mColorIluminacion)
                    e.Graphics.FillRectangle(brush, rect)
                End Using
            End If
        End If
    End Sub

End Class

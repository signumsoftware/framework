Public Class CboValidador
    Inherits ComboBox
    Implements IControlES

    Implements Framework.DatosNegocio.IValidable


#Region "métodos dados al heredar de combobox"
    Protected Overrides Sub RefreshItem(ByVal index As Integer)

    End Sub

    Protected Overrides Sub SetItemsCore(ByVal items As System.Collections.IList)

    End Sub
#End Region

#Region "campos"
    Private mValidador As Framework.DatosNegocio.IValidador 'si hay función de validación
    Private mSoloInteger As Boolean 'si debe admitir sólo integers
    Private mSoloDouble As Boolean 'si debe admitir sólo nºs decimales
    Private mFormateador As AuxIU.IFormateador 'si hay función de formateo
    Private mPropiedadesControl As PropiedadesControles.PropiedadesControlP
    Private mMensajeError As String
    Private mToolTipText As String
    Private mMensajeErrorValidacion As String 'el mensaje de error q se debe mostrar cuando no se valide
    Private mRequerido As Boolean 'si debe tener algo en el text
    Private mRequeridoItem As Boolean 'si debe tener un item seleccionado
    'para el flatstyle
    Private BorderBrush As Brush = New SolidBrush(SystemColors.Window) 'para el borde
    Private ArrowBrush As Brush = New SolidBrush(SystemColors.ControlText) 'para el desplegable
    Private DropButtonBrush As Brush = New SolidBrush(SystemColors.Control) 'para botón cuando está desplegado
    Private _ButtonColor As Color = SystemColors.Control 'para el botón

#End Region

#Region "constructor"
    Public Sub New()
        MyBase.New()
    End Sub
#End Region

#Region "inicialización (propiedades fijas)"
    Private Sub InitializeComponent()
        'BotonP
        Me.Name = "cboP"
    End Sub
#End Region

#Region "propiedades"
    Public Property ColorBotón() As Color
        Get
            Return _ButtonColor
        End Get
        Set(ByVal Value As Color)
            _ButtonColor = Value
            DropButtonBrush = New SolidBrush(Value) 'establecemos el dropdrownbrush
            Me.Invalidate() 'llamamos al método que hace que se redibuje
        End Set
    End Property

    Public Property RequeridoItem() As Boolean
        Get
            Return mRequeridoItem
        End Get
        Set(ByVal Value As Boolean)
            mRequeridoItem = Value
        End Set
    End Property

    Public Property Requerido() As Boolean
        Get
            Return mRequerido
        End Get
        Set(ByVal Value As Boolean)
            mRequerido = Value
        End Set
    End Property

    Public Property MensajeErrorValidacion() As String Implements IValidadorModificable.MensajeErrorValidacion
        Get
            Return mMensajeErrorValidacion
        End Get
        Set(ByVal Value As String)
            mMensajeErrorValidacion = Value
        End Set
    End Property

    Property MensajeError() As String Implements IControlPBase.MensajeError
        Get
            Return mMensajeError
        End Get
        Set(ByVal Value As String)
            mMensajeError = Value
        End Set
    End Property

    Public Property ToolTipText() As String Implements IControlPBase.ToolTipText
        Get
            Return mToolTipText
        End Get
        Set(ByVal Value As String)
            mToolTipText = Value
        End Set
    End Property
    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property PropiedadesControl() As PropiedadesControles.PropiedadesControlP Implements IControlPBase.PropiedadesControl
        Get
            Return mPropiedadesControl
        End Get
        Set(ByVal Value As PropiedadesControles.PropiedadesControlP)
            mPropiedadesControl = Value
            'si no está vacío, establecemos las propiedades
            If Not Value Is Nothing Then
                Select Case Value.TipoControl
                    Case PropiedadesControles.modControlesp.TipoControl.Entrada
                        'ponemos las propiedades para control entrada
                        'agregar y editar
                        'Me.BackColor = Value.ColorEdicion
                        'Me.Enabled = True
                    Case PropiedadesControles.modControlesp.TipoControl.Salida
                        'ponemos las propiedades para control salida
                        '(sólo consulta)
                        'Me.BackColor = Value.ColorConsulta
                        'Me.Enabled = False
                End Select
            End If
        End Set
    End Property
    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property Formateador() As AuxIU.IFormateador Implements IControlES.Formateador
        Get
            Return mFormateador
        End Get
        Set(ByVal Value As AuxIU.IFormateador)
            mFormateador = Value
        End Set
    End Property

    Public Property SoloInteger() As Boolean
        Get
            Return mSoloInteger
        End Get
        Set(ByVal Value As Boolean)
            mSoloInteger = Value
            'si es sólo integer, no puede ser solo double
            If Value = True Then
                mSoloDouble = False
            End If
        End Set
    End Property

    Public Property SoloDouble() As Boolean
        Get
            Return mSoloDouble
        End Get
        Set(ByVal Value As Boolean)
            mSoloDouble = Value
            'si es sólo double, no puede ser sólo integer
            If Value = True Then
                mSoloInteger = False
            End If
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public ReadOnly Property Validador() As Framework.DatosNegocio.IValidador Implements Framework.DatosNegocio.IValidable.Validador
        Get
            Return mValidador
        End Get
    End Property

    <System.ComponentModel.Browsable(False)> Public Property Validador1() As Framework.DatosNegocio.IValidador Implements IValidadorModificable.Validador
        Get
            Return mValidador
        End Get
        Set(ByVal Value As Framework.DatosNegocio.IValidador)
            If Not Value Is Nothing Then
                mValidador = Value
            Else
                ' Throw New ApplicationException("El validador para el combo box validable no puede ser nulo")
            End If
        End Set
    End Property
#End Region

#Region "métodos de control keypress para solo integer/double"
    Private Sub txtValidable_KeyPress(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles MyBase.KeyPress
        If SoloInteger Then
            'sólo permitimos que continúe el evento si es un carácter válido
            If Key_Solo_Numero(Convert.ToInt16(e.KeyChar)) = 0 Then
                'si es una tecla no permitida, ignoramos el evento
                e.Handled = True
            End If
        End If
        If SoloDouble Then
            'sólo permitimos que continúe el evento si es un carácter válido
            If Key_Numero(Convert.ToInt16(e.KeyChar), Me.Text) = 0 Then
                'si es una tecla no permitida, ignoramos el evento
                e.Handled = True
            End If
        End If
    End Sub

    Private Function Key_Numero(ByVal ascii As Integer, ByVal numero As String) As Integer
        'Controla la tecla presionada y sólo permite que se generen eventos de teclado
        'y de borrado, número,decimal

        'ascii es el código de carácter de la tecla presionada
        'numero es el numero en cstr(), para comprobar si ya tiene una coma o no

        Dim bandera As Boolean

        bandera = False

        'si es un punto, lo convierto en coma
        If ascii = 46 Then ascii = 44

        'numeros del 0 al 9
        If ascii >= 48 And ascii <= 57 Then bandera = True
        'teclado o decimales
        If ascii = 8 Or ascii = 10 Or ascii = 13 = True Or ascii = 127 Or ascii = 44 Then bandera = True

        'comprobamos si ya tiene una coma, y si la tecla presionada es otra coma
        'devolvemos 0
        If InStr(numero, ",") <> 0 Then
            If ascii = 44 Then
                bandera = False
            End If
        End If

        'devolvemos lo q corresponda
        If bandera = True Then
            Key_Numero = ascii
        Else
            Key_Numero = 0
        End If

    End Function

    Private Function Key_Solo_Numero(ByVal ascii As Integer) As Integer
        'Controla la tecla presionada y sólo permite que se generen eventos de teclado
        'y de borrado, número

        Dim bandera As Boolean

        bandera = False

        'numeros del 0 1l 9
        If ascii >= 48 And ascii <= 57 Then bandera = True
        'teclado o decimales
        If ascii = 8 Or ascii = 10 Or ascii = 13 = True Or ascii = 127 Then bandera = True
        'devolvemos lo q corresponda
        If bandera = True Then
            Key_Solo_Numero = ascii
        Else
            Key_Solo_Numero = 0
        End If

    End Function

#End Region

#Region "función validación"

    'el suceso que se desencadena cuando hay un error de validación
    'para que pueda ser interceptado por su contenedor
    Public Event ErrorValidacion(ByVal sender As Object, ByVal e As EventArgs) Implements IControlES.ErrorValidacion

    Public Sub cboValidador_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.TextChanged
        'esta función hace que validemos el texto en función de la fción de validación que 
        'estemos usando. Si el texto se valida, se pone forecolor normal, si no, se pone rojo
        Dim mensaje As String = String.Empty

        Try
            If Not Me.Validador Is Nothing Then
                If Validador.Validacion(mensaje, Me.Text) Or Me.Text = "" Then
                    'si ha validado, ponemos forecolor definido por propiedadescontrol
                    If Not Me.PropiedadesControl Is Nothing Then
                        Me.ForeColor = Me.PropiedadesControl.ForeColor
                    Else
                        'o bien black (por defecto si no hay propiedades definido)
                        Me.ForeColor = Color.Black
                    End If
                    'decimos q valida
                    RaiseEvent Validado(Me, New EventArgs)
                Else
                    'no valida
                    'decimos que no valida
                    Me.ErrorValidando(Nothing)
                End If
            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Public Sub ErrorValidando(ByVal mensaje As String) Implements IControlES.ErrorValidando
        'este sub es el que desencadena el error de validación.
        'lo declaramos público para que pueda ser provocado desde
        'fuera
        Try
            'ponemos el color de error validando
            If Not Me.PropiedadesControl Is Nothing Then
                'elcolorde error definido por propiedades
                Me.ForeColor = Me.PropiedadesControl.ForeColorError
            Else
                'o bien rojo, color de error predefinido
                Me.ForeColor = Color.Red
            End If
            'ponemos el mensaje de error
            If Not mensaje Is Nothing Then
                'si nos han dado uno, ponemos ese como mensaje de error
                Me.mMensajeError = mensaje
            Else
                If Not Me.MensajeErrorValidacion Is Nothing Then
                    'si no nos pasan mensaje, ponemos el mensaje del validador
                    Me.MensajeError = Me.mMensajeErrorValidacion
                Else
                    'mensaje por defecto en caso de q no haya nada
                    Me.MensajeError = "Error en el formato del texto"
                End If
            End If
            'lanzamos el suceso de error
            RaiseEvent ErrorValidacion(Me, New System.EventArgs)
        Catch ex As Exception
            Throw ex
        End Try

    End Sub

    Protected Overrides Sub OnValidating(ByVal e As System.ComponentModel.CancelEventArgs) Implements IControlES.OnValidating
        Dim mensaje As String = String.Empty

        Try

            If Not Validador Is Nothing Then
                'si tenemos validador
                If mValidador.Validacion(mensaje, Me.Text) Or Me.Text = "" Then
                    'se ha validado correctamente
                    'ponemos el color normal
                    Me.ForeColor = Me.PropiedadesControl.ForeColor
                    'ponemos el mensaje vacío
                    Me.PropiedadesControl.MensajeError = ""
                    'lanzamos un evento de validación correcta
                    RaiseEvent Validado(Me, New EventArgs)
                    'le decimos a la clase base que valide
                    MyBase.OnValidating(e)
                Else
                    'hay un error en la validación
                    'lanzamos el sub que realiza y formatea el error
                    ErrorValidando(Nothing)
                    'cancelamos el evento de cambio de foco
                    e.Cancel = True
                End If
            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Public Event Validado(ByVal sender As Object, ByVal e As System.EventArgs) Implements IControlES.Validado

#End Region

#Region "comparación con otros validadores"
    Public Function ValidacionIdentica(ByVal pValidador As Framework.DatosNegocio.IValidador) As Boolean Implements Framework.DatosNegocio.IValidable.ValidacionIdentica
        'si me pasan otro validador, decimos si es igual o no
        If Me.mValidador.Formula = pValidador.Formula Then
            Return True
        Else
            Return False
        End If
    End Function

#End Region

#Region "función de formateado y requerido"
    Private Sub txtValidable_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.LostFocus
        'si nos han pasado un formateador, formateamos el texto al salir del control
        If Not Formateador Is Nothing Then
            Me.Text = Formateador.Formatear(Me.Text)
        End If
        'enlaza con validación: si no se ha escrito nada
        'y está requerido, lanza un error de validación
        If Requerido Then
            If Me.Text = "" Then
                ErrorValidando("Debe escribir un valor")
            End If
        End If
        'enlaza con validación: si no se ha seleccionado nada
        'y está requerido, lanza un error de validación
        If RequeridoItem Then
            If Me.SelectedItem = Nothing Then
                ErrorValidando("Debe seleccionar un valor")
            End If
        End If
    End Sub
#End Region

#Region "métodos over"
    Public Sub Mouse_Over(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.MouseEnter
        Try
            If Not Me.PropiedadesControl Is Nothing Then
                Me.BackColor = Me.PropiedadesControl.ColorOver
            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Sub
    Public Sub Mouse_Out(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.MouseLeave
        Try
            If Not Me.PropiedadesControl Is Nothing Then
                Select Case Me.PropiedadesControl.TipoControl
                    'según el tipo de control
                    Case PropiedadesControles.modControlesp.TipoControl.Entrada
                        'color de edición normal
                        Me.BackColor = Me.PropiedadesControl.ColorEdicion
                    Case PropiedadesControles.modControlesp.TipoControl.Salida
                        'color de edición consulta
                        Me.BackColor = Me.PropiedadesControl.ColorConsulta
                End Select
            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Sub
#End Region

    '#Region "Funcionalidad de FlatStyle"

    '    Protected Overrides Sub WndProc(ByRef m As Message)
    '        MyBase.WndProc(m)

    '        Select Case m.Msg
    '            Case &HF
    '                'Paint the background. Only the borders
    '                'will show up because the edit
    '                'box will be overlayed
    '                Dim g As Graphics = Me.CreateGraphics
    '                Dim p As Pen = New Pen(Color.White, 2)
    '                g.FillRectangle(BorderBrush, Me.ClientRectangle)

    '                'Draw the background of the dropdown button
    '                Dim rect As Rectangle = New Rectangle(Me.Width - 15, 3, 12, Me.Height - 6)
    '                g.FillRectangle(DropButtonBrush, rect)

    '                'Create the path for the arrow
    '                Dim pth As Drawing2D.GraphicsPath = New Drawing2D.GraphicsPath
    '                Dim TopLeft As PointF = New PointF(Me.Width - 13, (Me.Height - 5) / 2)
    '                Dim TopRight As PointF = New PointF(Me.Width - 6, (Me.Height - 5) / 2)
    '                Dim Bottom As PointF = New PointF(Me.Width - 9, (Me.Height + 2) / 2)
    '                pth.AddLine(TopLeft, TopRight)
    '                pth.AddLine(TopRight, Bottom)

    '                g.SmoothingMode = Drawing2D.SmoothingMode.HighQuality

    '                'Determine the arrow's color.
    '                If Me.DroppedDown Then
    '                    ArrowBrush = New SolidBrush(SystemColors.HighlightText)
    '                Else
    '                    ArrowBrush = New SolidBrush(SystemColors.ControlText)
    '                End If

    '                'Draw the arrow
    '                g.FillPath(ArrowBrush, pth)

    '            Case Else
    '                Exit Select
    '        End Select
    '    End Sub

    '    'Sobreescribimos el mouse y el focus para pintar los bordes. Básicamente, ponemos el color
    '    'y llamamos a Invalidate(). En general, Invalidate causa que el propio control se redibuje

    '    Protected Overrides Sub OnMouseEnter(ByVal e As System.EventArgs)
    '        MyBase.OnMouseEnter(e)
    '        BorderBrush = New SolidBrush(SystemColors.Highlight)
    '        Me.Invalidate()
    '    End Sub

    '    Protected Overrides Sub OnMouseLeave(ByVal e As System.EventArgs)
    '        MyBase.OnMouseLeave(e)
    '        If Me.Focused Then Exit Sub
    '        If Not Me.PropiedadesControl Is Nothing Then
    '            BorderBrush = New SolidBrush(Me.PropiedadesControl.ForeColor)
    '        Else
    '            BorderBrush = New SolidBrush(SystemColors.Window)
    '        End If
    '        Me.Invalidate()
    '    End Sub

    '    Protected Overrides Sub OnLostFocus(ByVal e As System.EventArgs)
    '        MyBase.OnLostFocus(e)
    '        If Not Me.PropiedadesControl Is Nothing Then
    '            BorderBrush = New SolidBrush(Me.PropiedadesControl.ForeColor)
    '        Else
    '            BorderBrush = New SolidBrush(SystemColors.Window)
    '        End If
    '        Me.Invalidate()
    '    End Sub

    '    Protected Overrides Sub OnGotFocus(ByVal e As System.EventArgs)
    '        MyBase.OnGotFocus(e)
    '        BorderBrush = New SolidBrush(SystemColors.Highlight)
    '        Me.Invalidate()
    '    End Sub

    '    Protected Overrides Sub OnMouseHover(ByVal e As System.EventArgs)
    '        MyBase.OnMouseHover(e)
    '        BorderBrush = New SolidBrush(SystemColors.Highlight)
    '        Me.Invalidate()
    '    End Sub

    '#End Region



#Region "color habilitado/deshabilitado)"
    Protected Overrides Sub OnEnabledChanged(ByVal e As System.EventArgs)
        'llamamos a mi base
        MyBase.OnEnabledChanged(e)
        'ponemos el color en función de si está habilitado o no
        If Me.Enabled = True Then
            'está habilitado
            If Not Me.PropiedadesControl Is Nothing Then
                Me.BackColor = Me.PropiedadesControl.ColorFondo
            Else
                Me.BackColor = Color.White 'por defecto si no hay propiedadescontrol
            End If
        Else
            'está deshabilitado
            Me.BackColor = Color.LightGray
        End If
    End Sub
#End Region

    Public ReadOnly Property FormularioPadre() As System.Windows.Forms.Form Implements IControlPBase.FormularioPadre
        Get
            Return ControlHelper.ObtenerFormularioPadre(Me)
        End Get
    End Property
End Class

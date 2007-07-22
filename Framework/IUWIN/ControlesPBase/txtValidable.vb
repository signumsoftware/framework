Public Class txtValidable
    Inherits System.Windows.Forms.TextBox
    Implements Framework.DatosNegocio.IValidable
    'Implements IValidadorModificable -- ya lo hace la interfaz icontrolES
    Implements IControlES


#Region "campos"
    Private mValidador As Framework.DatosNegocio.IValidador 'si hay función de validación
    Private mSoloInteger As Boolean 'si debe admitir sólo integers
    Private mSoloDouble As Boolean 'si debe admitir sólo nºs decimales
    Private mFormateador As AuxIU.IFormateador 'si hay función de formateo
    Private mPropiedadesControl As PropiedadesControles.PropiedadesControlP
    Private mMensajeError As String
    Private mToolTipText As String
    Private mMensajeErrorValidacion As String 'el mensaje de error q se debe mostrar cuando no se valide
    Private mTrimText As Boolean
#End Region

#Region "constructor"

    Public Sub New()
        MyBase.New()
        Inicializar()
    End Sub

    Public Sub New(ByVal pvalidador As Framework.DatosNegocio.IValidador)
        Me.New()

        Validador1 = pvalidador
        Inicializar()
    End Sub

    Public Sub New(ByVal pValidador As Framework.DatosNegocio.IValidador, ByVal pFormateador As AuxIU.IFormateador, ByVal pPropiedadesControl As PropiedadesControles.PropiedadesControlP)
        'iniciamos la clase base
        Me.New()
        'establecemos el validador que nos pasan
        mValidador = pValidador
        'establecemos las propiedades del control
        PropiedadesControl = pPropiedadesControl
        Inicializar()
    End Sub

    Private Sub Inicializar()
        'Me.BorderStyle = Windows.Forms.BorderStyle.FixedSingle
    End Sub
#End Region

#Region "propiedades"

#Region "sobrescritas"
    Public Overrides Property Text() As String
        Get
            'Dim mtext As String = MyBase.Text
            'If Me.mTrimText Then
            '    mtext = mtext.Trim()
            'End If
            'If Me.SoloInteger OrElse Me.SoloDouble Then
            '    If String.IsNullOrEmpty(mtext) OrElse Not Integer.TryParse(mtext, Nothing) Then
            '        mtext = "0"
            '    End If
            'End If
            'Return mtext
            Return MyBase.Text
        End Get
        Set(ByVal value As String)
            If (Me.mSoloInteger OrElse Me.mSoloDouble) AndAlso String.IsNullOrEmpty(value) Then
                MyBase.Text = "0"
            Else
                MyBase.Text = value
            End If
        End Set
    End Property
#End Region

    <System.ComponentModel.DefaultValue(True)> _
    Public Property TrimText() As Boolean
        Get
            Return Me.mTrimText
        End Get
        Set(ByVal value As Boolean)
            Me.mTrimText = value
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

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
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

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property PropiedadesControl() As PropiedadesControles.PropiedadesControlP Implements IControlPBase.PropiedadesControl
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
                        'Me.ReadOnly = False
                    Case PropiedadesControles.modControlesp.TipoControl.Salida
                        'ponemos las propiedades para control salida
                        '(sólo consulta)
                        'Me.BackColor = Value.ColorConsulta
                        'Me.ReadOnly = True
                End Select
            End If
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property Formateador() As AuxIU.IFormateador Implements IControlES.Formateador
        Get
            Return mFormateador
        End Get
        Set(ByVal Value As AuxIU.IFormateador)
            mFormateador = Value
        End Set
    End Property

    <System.ComponentModel.DefaultValue(False)> _
    Public Property SoloInteger() As Boolean
        Get
            Return mSoloInteger
        End Get
        Set(ByVal Value As Boolean)
            mSoloInteger = Value
            'si es sólo integer, no puede ser solo double
            If Value = True Then
                Me.Text = "0"
                mSoloDouble = False
            End If
        End Set
    End Property

    <System.ComponentModel.DefaultValue(False)> _
    Public Property SoloDouble() As Boolean
        Get
            Return mSoloDouble
        End Get
        Set(ByVal Value As Boolean)
            mSoloDouble = Value
            'si es sólo double, no puede ser sólo integer
            If Value = True Then
                Me.Text = "0"
                mSoloInteger = False
            End If
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public ReadOnly Property Validador() As Framework.DatosNegocio.IValidador Implements Framework.DatosNegocio.IValidable.Validador
        Get
            Return mValidador
        End Get
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property Validador1() As Framework.DatosNegocio.IValidador Implements IValidadorModificable.Validador
        Get
            Return mValidador
        End Get
        Set(ByVal Value As Framework.DatosNegocio.IValidador)
            If Not Value Is Nothing Then
                mValidador = Value
            Else
                ' Throw New ApplicationException("El validador para el txtvalidable no puede ser nulo")
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

        'si es un punto, lo convierto en coma <---- no podemos hacerlo (no se puede modificar
        'el valor de ascii), así que lo que hacemos es decir que no se escriba el punto
        If ascii = 46 Then
            bandera = False
        End If

        'numeros del 0 al 9
        If ascii >= 48 And ascii <= 57 Then
            bandera = True
        End If

        'teclado o decimales
        If ascii = 8 Or ascii = 10 Or ascii = 13 = True Or ascii = 127 Or ascii = 44 Then
            bandera = True
        End If

        'comprobamos si ya tiene una coma, y si la tecla presionada es otra coma
        'devolvemos 0
        If InStr(numero, ",") <> 0 Then
            If ascii = 44 Then
                bandera = False
            End If
        End If

        'devolvemos lo q corresponda
        If bandera = True Then
            Return ascii
        Else
            Return 0
        End If

    End Function

    Private Function Key_Solo_Numero(ByVal ascii As Integer) As Integer
        'Controla la tecla presionada y sólo permite que se generen eventos de teclado
        'y de borrado, número

        Dim bandera As Boolean

        bandera = False

        'numeros del 0 1l 9
        If ascii >= 48 And ascii <= 57 Then
            bandera = True
        End If

        'teclado o decimales
        If ascii = 8 Or ascii = 10 Or ascii = 13 = True Or ascii = 127 Then
            bandera = True
        End If

        'devolvemos lo q corresponda
        If bandera = True Then
            Return ascii
        Else
            Return 0
        End If

    End Function

#End Region

#Region "función validación"

    'el suceso que se desencadena cuando hay un error de validación
    'para que pueda ser interceptado por su contenedor
    Public Event ErrorValidacion(ByVal sender As Object, ByVal e As EventArgs) Implements IControlES.ErrorValidacion

    Public Sub txtvTelefono_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.TextChanged
        'esta función hace que validemos el texto en función de la fción de validación que 
        'estemos usando. Si el texto se valida, se pone forecolor normal, si no, se pone rojo
        Dim mensaje As String
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
        Dim mensaje As String

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

#Region "función de formateado"
    Protected Overrides Sub OnGotFocus(ByVal e As System.EventArgs)
        MyBase.OnGotFocus(e)
        Me.SelectAll()
    End Sub

    Private Sub txtValidable_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.LostFocus
        'si nos han pasado un formateador, formateamos el texto al salir del control
        If Not Formateador Is Nothing Then
            Me.Text = Formateador.Formatear(Me.Text)
        End If
    End Sub
#End Region


    Public ReadOnly Property FormularioPadre() As System.Windows.Forms.Form Implements IControlPBase.FormularioPadre
        Get
            Return ControlHelper.ObtenerFormularioPadre(Me)
        End Get
    End Property
End Class
Public Class BotonP
    Inherits System.Windows.Forms.Button
    Implements IControlBotonP


#Region "campos"
    Private mPropiedadesControl As PropiedadesControles.PropiedadesControlP
    Private mToolTipText As String
    Private mMensajeError As String
    Private mOcultarEnSalida As Boolean
#End Region

#Region "constructor"
    Public Sub New()
        MyBase.New()
        Inicializar()
    End Sub

    Public Sub New(ByVal pPropiedadesControl As PropiedadesControles.PropiedadesControlP)
        MyBase.New()
        'inicializamos
        Inicializar()
    End Sub

    Private Sub Inicializar()
        Try
            'obsoleto: ahora lo define el propiedadescontrol
            'ponemos el estilo
            'Me.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Catch ex As Exception
            Throw ex
        End Try

    End Sub
#End Region

#Region "propiedades"
    ''' <summary>
    ''' Determina si cuando el formulario sea de tipo Salida debe ocultarse el botón
    ''' </summary>
    <System.ComponentModel.DefaultValue(False)> _
    Public Property OcultarEnSalida() As Boolean
        Get
            Return Me.mOcultarEnSalida
        End Get
        Set(ByVal value As Boolean)
            Me.mOcultarEnSalida = value
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property MensajeError() As String Implements IControlPBase.MensajeError
        Get
            Return mMensajeError
        End Get
        Set(ByVal Value As String)
            mMensajeError = Value
        End Set
    End Property

    <System.ComponentModel.ReadOnly(True)> Public Property ToolTipText() As String Implements IControlPBase.ToolTipText
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
            Dim propiedadclonada As PropiedadesControles.PropiedadesControlP

            mPropiedadesControl = Value
            'si no está vacío,definimos las propiedades del control
            If Not Value Is Nothing Then
                'ponemos la fuente
                Me.Font = Value.Font
                'color de fondo
                Me.BackColor = Value.ColorFondo
                'forecolor
                Me.ForeColor = Value.ForeColor
                'establecemos la imagen: si hay imagen de fondo, se establece como imagenfondo
                If Value.ImagenFondo Is Nothing AndAlso Not Me.Image Is Nothing Then
                    'hacemos un clone para que esta imagen se quede sólo en el obj
                    'propiedades al que este control, y sólo ese control, hace referencia
                    propiedadclonada = Value.Clone
                    propiedadclonada.ImagenFondo = Me.Image
                    Me.mPropiedadesControl = propiedadclonada
                Else
                    'hay imagen fondo,así q se establece como imagen
                    Me.Image = Value.ImagenFondo
                End If
                ''establecemos el estilo 3d
                'Me.FlatStyle = Value.FlatStyle
            End If
        End Set
    End Property

#End Region

#Region "métodos"

    Private Sub MouseOver(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.MouseEnter
        Try
            'método OnMouseHover: cuando el ratón se ponga encima
            If Not Me.PropiedadesControl Is Nothing Then
                'COLOR DE FONDO
                Me.BackColor = PropiedadesControl.ColorOver
                'IMAGEN OVER
                If Not PropiedadesControl.ImagenOver Is Nothing Then
                    Me.Image = PropiedadesControl.ImagenOver
                End If
                'FORECOLOR OVER
                If Not Me.PropiedadesControl.ForeColorOver.IsEmpty Then
                    Me.ForeColor = Me.PropiedadesControl.ForeColorOver
                End If
            End If
        Catch ex As Exception
            Throw ex
        End Try

    End Sub

    Private Sub MouseOut(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.MouseLeave
        'método OnMouseOut: cuando el ratón se quita
        Try
            If Not Me.PropiedadesControl Is Nothing Then
                'COLOR FONDO
                Me.BackColor = PropiedadesControl.ColorFondo
                'IMAGEN FONDO
                If Not PropiedadesControl.ImagenFondo Is Nothing Then
                    Me.Image = PropiedadesControl.ImagenFondo
                Else
                    'quitamos la imagen
                    Me.Image = Nothing
                End If
                'FORECOLOR
                Me.ForeColor = Me.PropiedadesControl.ForeColor
            End If
        Catch ex As Exception
            Throw ex
        End Try

    End Sub

#End Region

#Region "inicialización (propiedades fijas)"
    Private Sub InitializeComponent()
        'BotonP
        Me.Name = "BotonP"
    End Sub
#End Region

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

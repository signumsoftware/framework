Namespace FormulariosP
    Public Class FormularioBase
        Inherits Form
        Implements IFormularioP



#Region "campos"
        Private mPropiedadesControl As PropiedadesControles.PropiedadesControlP
        Private mPropiedadesBoton As PropiedadesControles.PropiedadesControlP
        Private mPropiedadesES As PropiedadesControles.PropiedadesControlP
        Private mMarco As Motor.INavegador 'la referencia al motor de navegación
        Private mControlador As IControladorForm 'el controlador de formulario
        Private mDatos As Hashtable 'el hashtable con los datos: propiedades y controlador
        Private mPaquete As Hashtable 'el paquete de datos que nos pasan en la navegación
        Private mListaExFormateadores As Hashtable 'la hash q guarda el estado original de los controles para los formateadores
#End Region

#Region "constructor"

        Public Sub New()
            MyBase.New()
            Try
                InitializeComponent()

            Catch ex As Exception
                Throw ex
            End Try

        End Sub
#End Region

#Region "propiedades"
        <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property Paquete() As Hashtable Implements IFormularioP.Paquete
            Get
                Return mPaquete
            End Get
            Set(ByVal Value As Hashtable)
                mPaquete = Value
            End Set
        End Property

        <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Overridable Property PropiedadesControl() As PropiedadesControles.PropiedadesControlP Implements IFormularioP.PropiedadesControl
            Get
                Return mPropiedadesControl
            End Get
            Set(ByVal Value As PropiedadesControles.PropiedadesControlP)
                Dim formateador As Formateadores.FormateadorES

                'establecemos el valor
                mPropiedadesControl = Value

                'ponemos las propiedades del nuestro
                If Not mPropiedadesControl Is Nothing Then
                    'color fondo
                    If Not mPropiedadesControl.ColorFondo.ToArgb = Color.Empty.ToArgb Then
                        Me.BackColor = mPropiedadesControl.ColorFondo
                    End If
                    'forecolor
                    If Not mPropiedadesControl.ForeColor.ToArgb = Color.Empty.ToArgb Then
                        Me.ForeColor = mPropiedadesControl.ForeColor
                    End If
                    'fuente
                    If Not mPropiedadesControl.Font Is Nothing Then
                        Me.Font = mPropiedadesControl.Font
                    End If
                    'imagen de fondo del formulario
                    If Not mPropiedadesControl.ImagenFondo Is Nothing Then
                        Me.BackgroundImage = mPropiedadesControl.ImagenFondo
                    End If
                End If

                'lanzamos en cascada las propiedades
                EstablecerPropiedadesEnCascada(Me)

                'llamamos al formateador de controles para mostrar u ocultar los controlesES
                formateador = New Formateadores.FormateadorES(Me)

                Select Case mPropiedadesControl.TipoControl
                    Case PropiedadesControles.modControlesp.TipoControl.Entrada
                        formateador.FormatearEntrada()
                    Case PropiedadesControles.modControlesp.TipoControl.Salida
                        formateador.FormatearSalida()
                End Select

            End Set
        End Property

        <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property PropiedadesBoton() As PropiedadesControles.PropiedadesControlP Implements IFormularioP.PropiedadesBoton
            Get
                Return mPropiedadesBoton
            End Get
            Set(ByVal Value As PropiedadesControles.PropiedadesControlP)

                mPropiedadesBoton = Value
                If Not mPropiedadesBoton Is Nothing Then
                    EstablecerPropiedadesEnCascada(Me)
                End If
            End Set
        End Property

        <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property PropiedadesES() As PropiedadesControles.PropiedadesControlP Implements IFormularioP.PropiedadesES
            Get
                Return mPropiedadesES
            End Get
            Set(ByVal Value As PropiedadesControles.PropiedadesControlP)

                mPropiedadesES = Value
                If Not mPropiedadesES Is Nothing Then
                    'burbujeamos hacia abajo la propiedad entre 
                    'todos los controles que lo admitan
                    EstablecerPropiedadesEnCascada(Me)
                End If

            End Set
        End Property

        <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property cMarco() As Motor.INavegador Implements IFormularioP.cMarco
            Get
                Return mMarco
            End Get
            Set(ByVal Value As Motor.INavegador)
                If Value Is Nothing Then 'requerido
                    Throw New ApplicationException("El marco de contexto para WindowsForms no está establecido")
                End If
                mMarco = Value
            End Set
        End Property

        <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property Controlador() As IControladorForm Implements IFormularioP.Controlador
            Get
                Return mControlador
            End Get
            Set(ByVal Value As IControladorForm)
                'permitimos que no haya controlador
                'If Value Is Nothing Then 'requerido
                '    Throw New ApplicationException("El Controlador para este formulario no está definido")
                'End If
                mControlador = Value
            End Set
        End Property

        <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property Datos() As System.Collections.Hashtable Implements IFormularioP.Datos
            Get
                Return mDatos
            End Get
            Set(ByVal Value As System.Collections.Hashtable)
                'requerido (dentro va el marco y el controlador
                If Value Is Nothing Then
                    Throw New ApplicationException("La colección de datos pasada al formulario está vacía")
                End If
                mDatos = Value
            End Set
        End Property

#End Region

#Region "código generado por windows forms"
        Protected WithEvents ToolTip As System.Windows.Forms.ToolTip
        Private components As System.ComponentModel.IContainer


        Private Sub InitializeComponent()
            Me.components = New System.ComponentModel.Container
            Me.ToolTip = New System.Windows.Forms.ToolTip(Me.components)
            '
            'FormularioBase
            '
            Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
            Me.BackColor = System.Drawing.SystemColors.Control
            Me.ClientSize = New System.Drawing.Size(328, 161)
            Me.Name = "FormularioBase"

        End Sub
#End Region

#Region "Inicializador"
        Public Sub InicializarEnCascada(ByVal pcontrol As Object) Implements IFormularioP.InicializarEnCascada
            Dim subcontrol As Control
            Dim subcontrolp As ControlesP.IControlP

            Try
                'si está vacío, salimos
                If Not pcontrol Is Nothing Then
                    For Each subcontrol In pcontrol.controls
                        'si es un controlP, establecer su inicializar
                        If TypeOf subcontrol Is ControlesP.IControlP Then
                            subcontrolp = subcontrol
                            subcontrolp.Marco = Me.cMarco
                            subcontrolp.Inicializar()
                            'establecemos mi controlador en el controlador del controlp
                            If Not subcontrolp.Controlador Is Nothing Then
                                subcontrolp.Controlador.ControladorForm = Me.Controlador
                                'le decimos al subcontrolP que nosotros somos su propietario
                                subcontrolp.Controlador.Propietario = Me
                            End If
                        End If
                        If Not TypeOf subcontrol Is ControlesP.IControlP Then 'sólo con los q no sean controlesP
                            'llamamos recursivamente para llegar a todos los controles
                            InicializarEnCascada(subcontrol)
                        End If
                    Next
                End If
            Catch ex As Exception
                Throw ex
            End Try
        End Sub

        Public Overridable Sub Inicializar() Implements IFormularioP.Inicializar
            Try
                If Datos.Count <> 0 Then
                    'definimos el marco
                    If Datos.ContainsKey("Marco") Then
                        Me.cMarco = Datos.Item("Marco")
                    End If
                    'establecemos las propiedades Botones
                    If Datos.ContainsKey("PropiedadesBoton") Then
                        Me.PropiedadesBoton = Datos.Item("PropiedadesBoton")
                    End If
                    'establecemos las propiedades ES
                    If Datos.ContainsKey("PropiedadesES") Then
                        Me.PropiedadesES = Datos.Item("PropiedadesES")
                    End If
                    'establecemos las propiedades del control (y cascada)
                    If Datos.ContainsKey("PropiedadesControl") Then
                        Me.PropiedadesControl = Datos.Item("PropiedadesControl")
                    End If
                End If

                'agregar la escucha de errores
                AgregarSubControles(Me)

                'establecer los tooltips de los controles q hubiera
                Me.EstablecerToolTips(Me)

                'le decimos al controlador quiénes somos (le pasamos una instancia de mí mismo)
                If Not Me.Controlador Is Nothing Then
                    Me.Controlador.FormularioContenedor = Me
                End If

                'hacer lo que corresponda en cada formulario
                'overriding este sub, pero llamándolo desde fuera
                '......

                InicializarEnCascada(Me)

            Catch ex As Exception
                Throw ex
            End Try

        End Sub

        Public Overridable Sub PostInicializar() Implements IFormularioP.PostInicializar
            'aquí no hacemos nada, espara relaizar operaciones 
            'cuando haya terminado de cargarlo el navegador
        End Sub
#End Region

#Region "Agregar escuchadores"
        Public Sub AgregarSubControles(ByVal sender As Object) Implements IFormularioP.AgregarSubControles
            'iteramos entre todos los subcontroles que sean ES o CtrlP y agregamos una
            'escucha a su error de validación
            Dim ctrl As Control
            Dim controlp As ControlesP.IControlP
            Dim controles As ControlesPBase.IControlES

            Try
                If sender.Controls.Count = 0 Then
                    Exit Sub
                End If

                For Each ctrl In sender.Controls
                    'si es un controlP agregamos un escuchador
                    If TypeOf ctrl Is ControlesP.IControlP Then
                        controlp = ctrl

                        '1 - Para los eventos de error de validación
                        'quitamos el posible escuchador que tuviera antes
                        'para evitar que se duplique por error
                        RemoveHandler controlp.ErrorValidacion, AddressOf ErrorValidacion
                        'agregamos el escuchador
                        AddHandler controlp.ErrorValidacion, AddressOf ErrorValidacion

                        '2 - Para los eventos de validación feliz
                        'quitamos el escuchador para evitar duplicidades
                        RemoveHandler controlp.Validado, AddressOf Validado
                        'agregamos el escuchador
                        AddHandler controlp.Validado, AddressOf Validado

                    End If
                    'si es un control ES le agregamos el escuchador
                    If TypeOf ctrl Is ControlesPBase.IControlES Then
                        controles = ctrl

                        '1 - Para los eventos de error de validación
                        'quitamos el posible escuchador que tuviera antes
                        'para evitar que se duplique por error
                        RemoveHandler controles.ErrorValidacion, AddressOf ErrorValidacion
                        'agregamos el escuchador
                        AddHandler controles.ErrorValidacion, AddressOf ErrorValidacion

                        '2 - Para los eventos de validación feliz
                        'quitamos el escuchador para evitar duplicidades
                        RemoveHandler controles.Validado, AddressOf Validado
                        'agregamos el escuchador
                        AddHandler controles.Validado, AddressOf Validado
                    End If
                    'recursivamente llamamos a este sub para controlar los subcontroles
                    'si no es un controlP (sólo queremos escuchar al más alto)
                    If Not (TypeOf ctrl Is ControlesP.IControlP) Then
                        AgregarSubControles(ctrl)
                    End If
                Next
            Catch ex As Exception
                Throw ex
            End Try


        End Sub

#End Region

#Region "Establecer tooltips"
        Protected Sub EstablecerToolTips(ByVal sender As Object) Implements IFormularioP.EstablecerToolTip
            'establece los tooltips de los objetosP
            '(a llamar desde el inicializar de las clases q hereden)
            Dim ctrl As Object
            Dim subcontrol As ControlesPBase.IControlPBase

            Try
                'si no es contenedor o formulario salimos
                If Not (TypeOf sender Is Control) And Not (TypeOf sender Is Form) Then
                    Exit Sub
                End If

                For Each ctrl In sender.Controls
                    'si es un control
                    If TypeOf ctrl Is ControlesPBase.IControlPBase Then
                        subcontrol = ctrl
                        'le ponemos el tooltiptext
                        ' TODO: Comentario Provisinal     Me.ToolTip.SetToolTip(subcontrol, subcontrol.ToolTipText)
                    End If
                    'llamamos recursivamente a la función de tooltips
                    If TypeOf ctrl Is Control Or TypeOf ctrl Is Form Then
                        If Not TypeOf ctrl Is ControlesP.IControlP Then 'para q no se meta en los subctontroles de los controlesP
                            EstablecerToolTips(ctrl)
                        End If
                    End If
                Next
            Catch ex As Exception
                Throw ex
            End Try

        End Sub

#End Region

#Region "Establecer propiedades en cascada"
        Private Sub EstablecerPropiedadesEnCascada(ByVal sender As Object)
            Dim subcontrol As Control
            Dim subcontrolp As ControlesP.IControlP
            Dim subcontroles As ControlesPBase.IControlES
            Dim subcontrolb As ControlesPBase.IControlBotonP


            Try
                If sender Is Nothing Then
                    Exit Sub
                End If

                If TypeOf sender Is Form Or TypeOf sender Is Control Then
                    If sender.controls.count = 0 Then
                        Exit Sub
                    End If
                Else
                    'no nos han pasado un formulario ni un control
                    Throw New ApplicationException("Las propiedades en cascada sólo se pueden establecer sobre formularios o controles")
                End If

                For Each subcontrol In sender.controls
                    'si es un controlP, le establecemos las propiedades
                    If TypeOf subcontrol Is ControlesP.IControlP Then
                        subcontrolp = subcontrol
                        subcontrolp.PropiedadesBoton = Me.PropiedadesBoton
                        subcontrolp.PropiedadesES = Me.PropiedadesES
                        subcontrolp.PropiedadesControl = Me.PropiedadesControl
                    End If

                    'si es un controlES,leestablecemos las propiedadesES
                    If TypeOf subcontrol Is ControlesPBase.IControlES Then
                        subcontroles = subcontrol
                        subcontroles.PropiedadesControl = Me.PropiedadesES
                    End If

                    'si es un controlBoton, l establecemos la propiedadB
                    If TypeOf subcontrol Is ControlesPBase.IControlBotonP Then
                        subcontrolb = subcontrol
                        subcontrolb.PropiedadesControl = Me.PropiedadesBoton
                    End If

                    'si no es un controlP, llamamos recursivamente a este método
                    If Not TypeOf subcontrol Is ControlesP.IControlP AndAlso subcontrol.Controls.Count <> 0 Then
                        EstablecerPropiedadesEnCascada(subcontrol)
                    End If
                Next

            Catch ex As Exception
                Throw ex
            End Try

        End Sub
#End Region

#Region "Genera la base de datos para llamar al formulario"
        Public Function GenerarDatosCarga() As Hashtable
            Dim coldatos As Hashtable

            Try
                coldatos = New Hashtable
                'metemos el marco
                coldatos.Add("Marco", cMarco)
                'metemos, por defecto, las propiedades de formato que tenemos nosotros
                coldatos.Add("PropiedadesControl", Me.PropiedadesControl)
                coldatos.Add("PropiedadesES", Me.PropiedadesES)
                coldatos.Add("PropiedadesBoton", Me.PropiedadesBoton)

                'devolvemos
                Return coldatos
            Catch ex As Exception
                Throw ex
            End Try

        End Function
#End Region

#Region "sucesos de validación"
        Public Overridable Sub ErrorValidacion(ByVal sender As Object, ByVal e As EventArgs) Implements IFormularioP.ErrorValidacion
            'implementar en cada formulario que herede
        End Sub

        Public Overridable Sub Validado(ByVal sender As Object, ByVal e As System.EventArgs) Implements IFormularioP.Validado
            'implementar en cada formulario que herede
        End Sub
#End Region

#Region "Resizear cuando nos activan si minimizado: DESACTIVADO"
        'Private Sub FormularioBase_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.GotFocus
        '    Try
        '        If Me.WindowState = FormWindowState.Minimized Then
        '            Me.WindowState = FormWindowState.Normal
        '        End If
        '    Catch ex As Exception
        '        Throw ex
        '    End Try
        'End Sub

        'Private Sub FormularioBase_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.Activated
        '    Try
        '        If Me.WindowState = FormWindowState.Minimized Then
        '            Me.WindowState = FormWindowState.Normal
        '        End If
        '    Catch ex As Exception
        '        Throw ex
        '    End Try
        'End Sub
#End Region

#Region "Tratamiento Errores"
        ''' <summary>
        ''' Trata el error tal y como se ha definido en el Navegador
        ''' </summary>
        ''' <param name="excepcion">La excepción que se quiere tratar</param>
        ''' <param name="sender">El formulario que ha producido el error</param>
        Public Overloads Sub MostrarError(ByVal excepcion As System.Exception, ByVal sender As Form)
            cMarco.MostrarError(excepcion, sender)
        End Sub

        ''' <summary>
        ''' Trata el error tal y como se haa definido en el Navegador
        ''' </summary>
        ''' <param name="excepcion">La excepción que se quiere tratar</param>
        ''' <param name="sender">El control que ha producido el error</param>
        Public Overloads Sub MostrarError(ByVal excepcion As System.Exception, ByVal sender As Control)
            cMarco.MostrarError(excepcion, sender)
        End Sub

        ''' <summary>
        ''' Trata el error tal y como se haa definido en el Navegador
        ''' </summary>
        ''' <param name="excepcion">La excepción que se quiere tratar</param>
        ''' <param name="titulo">El titulo del mensaje de error</param>
        Public Overloads Sub MostrarError(ByVal excepcion As System.Exception, ByVal titulo As String)
            cMarco.MostrarError(excepcion, titulo)
        End Sub

        Public Overloads Sub MostrarError(ByVal excepcion As System.Exception)
            cMarco.MostrarError(excepcion, "Error en la aplicación")
        End Sub
#End Region


    End Class
End Namespace

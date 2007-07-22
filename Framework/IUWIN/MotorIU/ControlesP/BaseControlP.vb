Namespace ControlesP
    Public Class BaseControlP
        Inherits System.Windows.Forms.UserControl
        Implements IControlP

#Region "campos"
        Private mPropiedadesControl As PropiedadesControles.PropiedadesControlP
        Private mPropiedadesES As PropiedadesControles.PropiedadesControlP
        Private mPropiedadesBoton As PropiedadesControles.PropiedadesControlP
        Private mMensajeError As String
        Private mToolTipText As String
        Private mErroresValidacion As Framework.DatosNegocio.ArrayListValidable
        Private mInicializando As Boolean
        Private mMarco As Motor.INavegador
        Private mControlador As IControladorCtrl
#End Region

#Region "constructor"
        Public Sub New()
            MyBase.New()
            Try
                InitializeComponent()

                'le decimos al arraylistvalidable q se fije contra el tipo
                'de icontrolespbase
                Me.ErroresValidadores = New Framework.DatosNegocio.ArrayListValidable(New Framework.DatosNegocio.ValidadorTipos(GetType(ControlesPBase.IControlPBase), True))
            Catch ex As Exception
                Throw ex
            End Try

        End Sub

        Protected WithEvents ToolTip As System.Windows.Forms.ToolTip
        Private components As System.ComponentModel.IContainer

        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Me.components = New System.ComponentModel.Container
            Me.ToolTip = New System.Windows.Forms.ToolTip(Me.components)
            '
            'BaseControlP
            '
            'color de fondo del sistema
            'Me.BackColor = System.Drawing.SystemColors.Control
            Me.Name = "BaseControlP"

        End Sub
#End Region

#Region "propiedades"

        <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property Titulo() As String
            'nos da o establece el text del titulo del lblbTitulo, si lo hay
            Get
                Return GetTitulo(Me)
            End Get
            Set(ByVal Value As String)
                SetTitulo(Me, Value)
            End Set
        End Property

        <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property Controlador() As IControladorCtrl Implements IControlP.Controlador
            Get
                Return mControlador
            End Get
            Set(ByVal Value As IControladorCtrl)
                mControlador = Value
            End Set
        End Property

        <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property Marco() As Motor.INavegador Implements IControlP.Marco
            Get
                If mMarco Is Nothing Then
                    If TypeOf Me.ParentForm Is MotorIU.FormulariosP.IFormularioP Then
                        Dim ifp As MotorIU.FormulariosP.IFormularioP = Me.ParentForm
                        Return ifp.cMarco
                    End If
                Else
                    Return mMarco
                End If

            End Get
            Set(ByVal Value As Motor.INavegador)
                mMarco = Value
            End Set
        End Property

        <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Property ErroresValidadores() As Framework.DatosNegocio.ArrayListValidable Implements IControlP.ErroresValidadores
            Get
                Return mErroresValidacion
            End Get
            Set(ByVal Value As Framework.DatosNegocio.ArrayListValidable)
                mErroresValidacion = Value
                'lanzar el evento que toque
                ComprobarValidaciones()
            End Set
        End Property

        <System.ComponentModel.Browsable(False)> Public Property MensajeError() As String Implements ControlesPBase.IControlPBase.MensajeError
            Get
                Return mMensajeError
            End Get
            Set(ByVal Value As String)
                mMensajeError = Value
            End Set
        End Property

        Public Property ToolTipText() As String Implements ControlesPBase.IControlPBase.ToolTipText
            Get
                Return mToolTipText
            End Get
            Set(ByVal Value As String)
                mToolTipText = Value
            End Set
        End Property

        <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Overridable Property PropiedadesBoton() As PropiedadesControles.PropiedadesControlP Implements IControlP.PropiedadesBoton
            Get
                Return mPropiedadesBoton
            End Get
            Set(ByVal Value As PropiedadesControles.PropiedadesControlP)
                Try
                    mPropiedadesBoton = Value
                    'le pasamos las propiedade a todos los elementos que contenga este control
                    If Not mInicializando Then
                        EstablecerPropiedadesBEnCascada(Me)
                    End If
                Catch ex As Exception
                    Throw ex
                End Try
            End Set
        End Property

        <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Overridable Property PropiedadesControl() As PropiedadesControles.PropiedadesControlP Implements IControlP.PropiedadesControl
            Get
                Return mPropiedadesControl
            End Get
            Set(ByVal Value As PropiedadesControles.PropiedadesControlP)
                Dim formateador As Formateadores.FormateadorES

                Try
                    mPropiedadesControl = Value
                    If Not mPropiedadesControl Is Nothing Then
                        'si no está vacío, establecemos las propiedades
                        'color de fondo
                        If Not mPropiedadesControl.ColorFondo.ToArgb = Color.Empty.ToArgb Then
                            Me.BackColor = mPropiedadesControl.ColorFondo
                        End If
                        'fore color
                        If Not mPropiedadesControl.ForeColor.ToArgb = Color.Empty.ToArgb Then
                            Me.ForeColor = mPropiedadesControl.ForeColor
                        End If
                        'font
                        If Not mPropiedadesControl.Font Is Nothing Then
                            Me.Font = mPropiedadesControl.Font
                        End If
                        'el título
                        EstablecerColorTitulo(Me)
                        'le pasamos las propiedade a todos los elementos que contenga este control
                        If Not mInicializando Then
                            EstablecerPropiedadesEnCascada(Me)
                        End If

                        'llamamos al formateador de controles para mostrar u ocultar los controlesES
                        formateador = New Formateadores.FormateadorES(Me)

                        Select Case mPropiedadesControl.TipoControl
                            Case PropiedadesControles.modControlesp.TipoControl.Entrada
                                formateador.FormatearEntrada()
                            Case PropiedadesControles.modControlesp.TipoControl.Salida
                                formateador.FormatearSalida()
                        End Select
                    End If
                Catch ex As Exception
                    Throw ex
                End Try
            End Set
        End Property

        <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> Public Overridable Property PropiedadesES() As PropiedadesControles.PropiedadesControlP Implements IControlP.PropiedadesES
            Get
                Return mPropiedadesES
            End Get
            Set(ByVal Value As PropiedadesControles.PropiedadesControlP)
                Try
                    mPropiedadesES = Value
                    'le pasamos las propiedade a todos los elementos que contenga este control
                    If Not mInicializando Then
                        EstablecerPropiedadesESEnCascada(Me)
                    End If
                Catch ex As Exception
                    Throw ex
                End Try
            End Set
        End Property

#End Region

#Region "establecer titulo"
        Private Function GetTitulo(ByVal pcontrol As Control) As String
            'recursivamente, ponemos el titulo en el label q se llame 'lblTitulo'
            Dim subcontrol As Control

            Try
                For Each subcontrol In pcontrol.Controls
                    'buscamos un label con el name 'lblTitulo' (1º capa de controles anidados)
                    If TypeOf subcontrol Is Label AndAlso subcontrol.Name = "lblTitulo" Then
                        Return subcontrol.Text
                    End If
                    'si no es un controlP, llamamos recursivamente al sub para que busque en los
                    'siguientes niveles anidados
                    If Not TypeOf subcontrol Is IControlP Then
                        Return GetTitulo(subcontrol)
                    End If
                Next
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Private Sub SetTitulo(ByVal pcontrol As Control, ByVal ptitulo As String)
            'recursivamente, ponemos el titulo en el label q se llame 'lblTitulo'
            Dim subcontrol As Control

            Try
                For Each subcontrol In pcontrol.Controls
                    'buscamos un label con el name 'lblTitulo' (1º capa de controles anidados)
                    If TypeOf subcontrol Is Label AndAlso subcontrol.Name = "lblTitulo" Then
                        subcontrol.Text = ptitulo
                    End If
                    'si no es un controlP, llamamos recursivamente al sub para que busque en los
                    'siguientes niveles anidados
                    If Not TypeOf subcontrol Is IControlP Then
                        SetTitulo(subcontrol, ptitulo)
                    End If
                Next
            Catch ex As Exception
                Throw ex
            End Try
        End Sub

#End Region

#Region "Establecer propiedades en cascada"
        Private Sub EstablecerColorTitulo(ByVal pcontrol As Object)
            'recursivamente, ponemos los labels q tengan en el name o en el tag ""
            Dim subcontrol As Control

            Try
                For Each subcontrol In pcontrol.Controls
                    If TypeOf subcontrol Is Label Then
                        'buscamos un label con el name 'lblTitulo' (1º capa de controles anidados)
                        If subcontrol.Name = "lblTitulo" OrElse TypeOf subcontrol.Tag Is String AndAlso subcontrol.Tag = "lblTitulo" Then
                            'si hay q ponerle el font
                            If Not mPropiedadesControl.TituloFont Is Nothing Then
                                subcontrol.Font = mPropiedadesControl.TituloFont
                            End If
                            'si hay q ponerle el forecolor
                            If Not mPropiedadesControl.TituloForeColor.ToArgb = Color.Empty.ToArgb Then
                                subcontrol.ForeColor = mPropiedadesControl.TituloForeColor
                            End If
                        End If
                        'buscamos un label con el name 'lblObligatorio' (1ª capa de controles)
                        If subcontrol.Name = "lblObligatorio" OrElse TypeOf subcontrol.Tag Is String AndAlso subcontrol.Tag = "lblObligatorio" Then
                            'si hay que ponerle el font
                            If Not mPropiedadesControl.ObligatorioFont Is Nothing Then
                                subcontrol.Font = mPropiedadesControl.ObligatorioFont
                            End If
                            'si hay que ponerle el forecolor
                            If Not mPropiedadesControl.ObligatorioForeColor.ToArgb = Color.Empty.ToArgb Then
                                subcontrol.ForeColor = mPropiedadesControl.ObligatorioForeColor
                            End If
                        End If
                    End If
                    'si no es un controlP, llamamos recursivamente al sub para que busque en los
                    'siguientes niveles anidados
                    If Not TypeOf subcontrol Is IControlP AndAlso subcontrol.Controls.Count <> 0 Then
                        EstablecerColorTitulo(subcontrol)
                    End If
                Next
            Catch ex As Exception
                Throw ex
            End Try
        End Sub

        Private Sub EstablecerPropiedadesEnCascada(ByVal pcontrol As Object)
            'le pasamos el control sobre el que se deben aplicar
            'las propiedades en cascada, y él las establece recursivamente
            Dim ctrl As Control
            Dim frm As Form
            Dim subcontrolP As IControlP
            Dim subcontrol As Object
            Dim migroup As GroupBox
            Dim mipanel As Panel

            Try

                If pcontrol Is Nothing Then
                    'si no hay nada, salimos
                    Exit Sub
                End If

                '<- esto debería ir en un select case, pero no nos permite hacerlo con variables type

                'comprobamos si es un formulario
                If TypeOf pcontrol Is Form Then
                    frm = pcontrol
                    'si no tiene controles hijos, salimos
                    If frm.Controls.Count = 0 Then
                        Exit Sub
                    End If
                Else
                    'comprobamos si tiene controles hijos
                    If TypeOf pcontrol Is Control Then
                        ctrl = pcontrol
                        'si no tiene controles hijos y no es un control personalizado, salimos
                        If ctrl.Controls.Count = 0 AndAlso Not (TypeOf ctrl Is ControlesPBase.IControlPBase) Then
                            Exit Sub
                        End If
                    Else
                        'si no es un formulario ni un control, dmaso un error, pq nos han pasado
                        'un cosa muy rara con la que no podemos operar
                        Throw New ApplicationException("Sólo se pueden aplicar propiedades de controles a Formularios y Controles")
                    End If
                End If

                'si es un panel o un groupbox, le ponemos como imagen de fondo la q corresponda si la hay
                If Not Me.PropiedadesControl.ImagenFondo Is Nothing Then
                    If TypeOf pcontrol Is System.Windows.Forms.Panel Then
                        mipanel = pcontrol
                        mipanel.BackColor = Color.Transparent
                        'mipanel.BackgroundImage = Me.PropiedadesControl.ImagenFondo
                    ElseIf TypeOf pcontrol Is System.Windows.Forms.GroupBox Then
                        migroup = pcontrol
                        migroup.BackColor = Color.Transparent
                        'migroup.BackgroundImage = Me.PropiedadesControl.ImagenFondo
                    End If
                End If


                '->

                'ahora (nos da igual q sea ctrl o form, pq hemos visto que
                'tiene controles hijos)
                'recorremos todos los controles que tenga, y vamos estableciendo en
                'cascada todas las propiedades

                For Each subcontrol In pcontrol.controls
                    'si es un controlP
                    If TypeOf subcontrol Is IControlP Then
                        subcontrolP = subcontrol
                        subcontrolP.PropiedadesControl = Me.PropiedadesControl
                    End If
                    'si es un controlBoton: nada
                    'si es un controlES: nada

                    'ahora, si es un control o un form, relanzamos el establecimiento
                    'de propiedades recursivamente hacia abajo
                    If TypeOf subcontrol Is Form Or TypeOf subcontrol Is Control Then
                        'If Not TypeOf subcontrol Is IControlP Then - 777?
                        EstablecerPropiedadesEnCascada(subcontrol)
                        'End If
                    End If
                Next

            Catch ex As Exception
                Throw ex
            End Try

        End Sub

        Private Sub EstablecerPropiedadesESEnCascada(ByVal pcontrol As Object)
            'le pasamos el control sobre el que se deben aplicar
            'las propiedades en cascada, y él las establece recursivamente
            Dim ctrl As Control
            Dim frm As Form
            Dim subcontrolES As ControlesPBase.IControlES
            Dim subcontrolP As IControlP
            Dim subcontrol As Object

            Try

                If pcontrol Is Nothing Then
                    'si no hay nada, salimos
                    Exit Sub
                End If

                '<- esto debería ir en un select case, pero no nos permite hacerlo con variables type

                'comprobamos si es un formulario
                If TypeOf pcontrol Is Form Then
                    frm = pcontrol
                    'si no tiene controles hijos, salimos
                    If frm.Controls.Count = 0 Then
                        Exit Sub
                    End If
                Else
                    'comprobamos si tiene controles hijos
                    If TypeOf pcontrol Is Control Then
                        ctrl = pcontrol
                        'si no tiene controles hijos y no es un control personalizado, salimos
                        If ctrl.Controls.Count = 0 AndAlso Not (TypeOf ctrl Is ControlesPBase.IControlPBase) Then
                            Exit Sub
                        End If
                    Else
                        'si no es un formulario ni un control, damos un error, pq nos han pasado
                        'un cosa muy rara con la que no podemos operar
                        Throw New ApplicationException("Sólo se pueden aplicar propiedades de controles a Formularios y Controles")
                    End If
                End If

                '->

                'ahora (nos da igual q sea ctrl o form, pq hemos visto que
                'tiene controles hijos)
                'recorremos todos los controles que tenga, y vamos estableciendo en
                'cascada todas las propiedades

                For Each subcontrol In pcontrol.controls
                    'si es un controlP
                    If TypeOf subcontrol Is IControlP Then
                        subcontrolP = subcontrol
                        subcontrolP.PropiedadesES = Me.PropiedadesES
                    End If

                    'si es un controlBoton:nada

                    'si es un controlES
                    If TypeOf subcontrol Is ControlesPBase.IControlES Then
                        subcontrolES = subcontrol
                        subcontrolES.PropiedadesControl = Me.PropiedadesES
                    End If

                    'ahora, si es un control o un form, relanzamos el establecimiento
                    'de propiedades recursivamente hacia abajo
                    If TypeOf subcontrol Is Form Or TypeOf subcontrol Is Control Then
                        If Not TypeOf subcontrol Is IControlP Then
                            EstablecerPropiedadesESEnCascada(subcontrol)
                        End If
                    End If
                Next

            Catch ex As Exception
                Throw ex
            End Try
        End Sub

        Private Sub EstablecerPropiedadesBEnCascada(ByVal pcontrol As Object)
            'le pasamos el control sobre el que se deben aplicar
            'las propiedades en cascada, y él las establece recursivamente
            Dim ctrl As Control
            Dim frm As Form
            Dim subcontrolB As ControlesPBase.IControlBotonP
            Dim subcontrolP As IControlP
            Dim subcontrol As Object

            Try

                If pcontrol Is Nothing Then
                    'si no hay nada, salimos
                    Exit Sub
                End If

                '<- esto debería ir en un select case, pero no nos permite hacerlo con variables type

                'comprobamos si es un formulario
                If TypeOf pcontrol Is Form Then
                    frm = pcontrol
                    'si no tiene controles hijos, salimos
                    If frm.Controls.Count = 0 Then
                        Exit Sub
                    End If
                Else
                    'comprobamos si tiene controles hijos
                    If TypeOf pcontrol Is Control Then
                        ctrl = pcontrol
                        'si no tiene controles hijos y no es un control personalizado, salimos
                        If ctrl.Controls.Count = 0 AndAlso Not (TypeOf ctrl Is ControlesPBase.IControlPBase) Then
                            Exit Sub
                        End If
                    Else
                        'si no es un formulario ni un control, dmaso un error, pq nos han pasado
                        'un cosa muy rara con la que no podemos operar
                        Throw New ApplicationException("Sólo se pueden aplicar propiedades de controles a Formularios y Controles")
                    End If
                End If

                '->

                'ahora (nos da igual q sea ctrl o form, pq hemos visto que
                'tiene controles hijos)
                'recorremos todos los controles que tenga, y vamos estableciendo en
                'cascada todas las propiedades

                For Each subcontrol In pcontrol.controls
                    'si es un controlP
                    If TypeOf subcontrol Is IControlP Then
                        subcontrolP = subcontrol
                        subcontrolP.PropiedadesBoton = Me.PropiedadesBoton
                    End If
                    'si es un controlBoton
                    If TypeOf subcontrol Is ControlesPBase.IControlBotonP Then
                        subcontrolB = subcontrol
                        subcontrolB.PropiedadesControl = Me.PropiedadesBoton
                    End If
                    'si es un controlES: nada

                    'ahora, si es un control o un form, relanzamos el establecimiento
                    'de propiedades recursivamente hacia abajo
                    If TypeOf subcontrol Is Form Or TypeOf subcontrol Is Control Then
                        If Not TypeOf subcontrol Is IControlP Then
                            EstablecerPropiedadesBEnCascada(subcontrol)
                        End If
                    End If
                Next

            Catch ex As Exception
                Throw ex
            End Try
        End Sub
#End Region

#Region "Establecer tooltips"
        Protected Sub EstablecerToolTips(ByVal sender As Object)
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
                        ' TODO: Comentario Provisinal Me.ToolTip.SetToolTip(subcontrol, subcontrol.ToolTipText)
                    End If
                    'llamamos recursivamente a la función de tooltips
                    If TypeOf ctrl Is Control Or TypeOf ctrl Is Form Then
                        If Not TypeOf ctrl Is IControlP Then
                            EstablecerToolTips(ctrl)
                        End If
                    End If
                Next
            Catch ex As Exception
                Throw ex
            End Try

        End Sub

#End Region

#Region "validaciones"

        Public Event ErrorValidacion(ByVal sender As Object, ByVal e As EventArgs) Implements IControlP.ErrorValidacion

        Public Event Validado(ByVal sender As Object, ByVal e As System.EventArgs) Implements IControlP.Validado

        Public Sub ErrorValidando(ByVal sender As Object, ByVal e As EventArgs) Implements IControlP.ErrorValidando
            'tomamos el error y lo lanzamos
            Dim subcontrol As ControlesPBase.IControlPBase
            Dim subcontrolES As ControlesPBase.IControlES
            Try
                subcontrol = sender
                'agregamos el error de mi colección de errores
                If Not ErroresValidadores.Contains(sender) Then
                    ErroresValidadores.Add(sender)
                End If
                'ponemos el tooltip que corresponda
                '- si es un controlES
                If TypeOf subcontrol Is ControlesPBase.IControlES Then
                    subcontrolES = subcontrol
                    ' TODO: Comentario Provisinal Me.ToolTip.SetToolTip(subcontrolES, subcontrolES.MensajeErrorValidacion) 'lo tomamos de su mensaje de error validación
                End If
                '- si es un controlP
                If TypeOf subcontrol Is IControlP Then
                    ' TODO: Comentario Provisinal Me.ToolTip.SetToolTip(subcontrol, subcontrol.MensajeError) 'lo tomamos de su mensaje error (q será el último error provocado)
                End If

                'llamamos al método que comprueba los errores y lanza el error
                ComprobarValidaciones()
            Catch ex As Exception
                Throw ex
            End Try
        End Sub

        Public Sub ComprobarValidaciones() Implements IControlP.ComprobarValidaciones
            Dim subcontrol As ControlesPBase.IControlPBase

            Try
                If Not ErroresValidadores Is Nothing Then
                    If ErroresValidadores.Count <> 0 Then 'hay algún error

                        'ponemos el mensaje de error
                        For Each subcontrol In ErroresValidadores
                            'ponemos el error de este subcontrol como nuestro último error
                            Me.MensajeError = subcontrol.MensajeError
                        Next

                        'lanzamos el evento de error validación
                        RaiseEvent ErrorValidacion(Me, New EventArgs)

                    Else 'no hay errores
                        'borramos el mensaje error
                        Me.MensajeError = ""

                        'lanzamos evento de que todo está bien
                        RaiseEvent Validado(Me, New EventArgs)
                    End If
                End If
            Catch ex As Exception
                Throw ex
            End Try

        End Sub

        Public Sub AgregarSubControles(ByVal sender As Object)
            Dim ctrl As Control
            Dim subcontrol As ControlesPBase.IControlES
            Dim subcontrolp As IControlP
            Try
                For Each ctrl In sender.Controls
                    'agregamos un escuchador de eventos error para los controles ES q haya
                    If TypeOf ctrl Is ControlesPBase.IControlES Then
                        subcontrol = ctrl

                        '1 con las validaciones erróneas
                        'quitamos el escuchador que hubiera antes para evitar duplicidades
                        RemoveHandler subcontrol.ErrorValidacion, AddressOf ErrorValidando
                        'agregamos el escuchador a ese evento del control
                        AddHandler subcontrol.ErrorValidacion, AddressOf ErrorValidando

                        '2 con las validaciones felices
                        'quitamos el escuchador para evitar duplicidades
                        RemoveHandler subcontrol.Validado, AddressOf Validacion
                        'agregamos el escuchador al evento del subcontrol
                        AddHandler subcontrol.Validado, AddressOf Validacion

                    End If
                    'agregamos un escuchador deeventos error para los macrocontroles que haya
                    If TypeOf ctrl Is IControlP Then
                        subcontrolp = ctrl

                        '1 con las validaciones erróneas
                        'quitamos el escuchador que hubiera antes para evitar duplicidades
                        RemoveHandler subcontrolp.ErrorValidacion, AddressOf ErrorValidando
                        'agregamos el escuchador a ese evento del control
                        AddHandler subcontrolp.ErrorValidacion, AddressOf ErrorValidando

                        '2 con las validaciones felices
                        'quitamos el escuchador para evitar duplicidades
                        RemoveHandler subcontrolp.Validado, AddressOf Validacion
                        'agregamos el escuchador al evento del controlP
                        AddHandler subcontrolp.Validado, AddressOf Validacion
                    End If
                    'llamamos recursivamente a este sub con cada objeto
                    AgregarSubControles(ctrl)
                Next
            Catch ex As Exception
                Throw ex
            End Try
        End Sub

        Public Sub Validacion(ByVal sender As Object, ByVal e As System.EventArgs) Implements IControlP.Validacion
            Dim subcontrol As ControlesPBase.IControlPBase
            Try
                If sender Is Nothing Then
                    Exit Sub
                End If

                subcontrol = sender

                'ponemos el tooltiptext de que se ha validado correctamente
                ' TODO: Comentario Provisinal     Me.ToolTip.SetToolTip(subcontrol, subcontrol.ToolTipText)

                'lo quitamos del array de errores si existe
                If Me.ErroresValidadores.Count <> 0 Then
                    If Me.ErroresValidadores.Contains(subcontrol) Then
                        Me.ErroresValidadores.Remove(subcontrol)
                    End If
                End If

                'mandamos a comprobador de errores, q lanza los eventos de error o de ok
                ComprobarValidaciones()
            Catch ex As Exception
                Throw ex
            End Try
        End Sub

#End Region

#Region "Inicializadores"
        Public Overridable Sub Inicializar() Implements IControlP.Inicializar
            Try
                'establecemos los tooltips de los controlesPBase
                EstablecerToolTips(Me)
                'ponemos los escuchadores para los hijos: rellamarlo en los q hereden
                AgregarSubControles(Me)
                'inicializar en cascada
                Me.InicializarEnCascada(Me)

            Catch ex As Exception
                Throw ex
            End Try
        End Sub

        Public Sub InicializarEnCascada(ByVal pcontrol As Object) Implements IControlP.InicializarEnCascada
            Dim subcontrol As Control
            Dim subcontrolp As IControlP

            Try
                'si está vacío, salimos

                If Not pcontrol Is Nothing Then
                    For Each subcontrol In pcontrol.controls
                        'si es un controlP, establecer su inicializar
                        If TypeOf subcontrol Is IControlP Then
                            subcontrolp = subcontrol
                            subcontrolp.Marco = Me.Marco
                            subcontrolp.Inicializar() 'con ésto, el subcontrolP ya lanza a su vez su inicializar en cascada
                        Else
                            'llamamos recursivamente para llegar a todos los controles
                            InicializarEnCascada(subcontrol)
                        End If
                    Next
                End If
            Catch ex As Exception
                Throw ex
            End Try
        End Sub
#End Region

#Region "controlador de errores y excepciones"
        'TODO: El método MostrarError debería ser Protected, pero debido a un error de Visual Studio
        'provisionalmente lo ponemos como público
        Public Overloads Sub MostrarError(ByVal excepcion As Exception, ByVal sender As Control)
            Me.Marco.MostrarError(excepcion, sender)
        End Sub

        Public Overloads Sub MostrarError(ByVal excepcion As Exception, ByVal titulo As String)
            Me.Marco.MostrarError(excepcion, titulo)
        End Sub

        Public Overloads Sub MostrarError(ByVal excepcion As Exception)
            Me.Marco.MostrarError(excepcion, "Error en la aplicación")
        End Sub
        '--------------------------------------------------------------------------------------------

#End Region

#Region "Establecer y Rellenar Datos"
        Protected Overridable Function IUaDN() As Boolean

        End Function

        Protected Overridable Sub DNaIU(ByVal pDN As Object)

        End Sub
#End Region

#Region "Generar Datos de Carga"
        Public Function GenerarDatosCarga() As Hashtable
            If Not (Me.ParentForm Is Nothing) AndAlso (TypeOf Me.ParentForm Is FormulariosP.IFormularioP) Then
                Return CType(Me.ParentForm, FormulariosP.FormularioBase).GenerarDatosCarga
            End If

            Return Nothing
        End Function
#End Region


        Public ReadOnly Property FormularioPadre() As System.Windows.Forms.Form Implements ControlesPBase.IControlPBase.FormularioPadre
            Get
                Return ControlesPBase.ControlHelper.ObtenerFormularioPadre(Me)
            End Get
        End Property
    End Class
End Namespace

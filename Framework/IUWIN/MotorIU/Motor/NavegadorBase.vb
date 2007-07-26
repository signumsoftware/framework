Imports Framework.IU.IUComun

Namespace Motor

    ''' <summary>
    ''' Clase base Navegador o Marco,de la que deben heredar los Motores de Navegación
    ''' </summary>
    ''' <remarks>Esta es la clase NAVEGADOR ó MARCO
    '''Es el principio y el fin de la aplicación, la que contiene todos los procesos y formularios
    '''Antes de ella sólo debe existir el Sub Main(), que debe construir o cargar la Tabla de Navegación
    '''y pasársela en el constructor</remarks>
    Public Class NavegadorBase
        Implements Motor.INavegador


#Region "campos"
        Private mTablaNavegacion As Hashtable
        Private ColForms As ArrayList
        Private mDatosMarco As Hashtable
        Private mPropiedadesBoton As PropiedadesControles.PropiedadesControlP 'propiedades Botones
        Private mPropiedadesCtrl As PropiedadesControles.PropiedadesControlP 'Propiedades formularios
        Private mPropiedadesES As PropiedadesControles.PropiedadesControlP 'propiedades ctrles edición/consulta
        Private mPropiedadesForm As PropiedadesControles.PropiedadesControlP 'propiedades formularios
#End Region

#Region "Constructor"


        Public Sub New(ByVal pTablaNavegacion As Hashtable)
            Dim PropiedadesBoton As New PropiedadesControles.PropiedadesControlP 'propiedades Botones
            Dim PropiedadesCtrl As New PropiedadesControles.PropiedadesControlP 'Propiedades formularios
            Dim PropiedadesES As New PropiedadesControles.PropiedadesControlP 'propiedades ctrles edición/consulta
            Dim PropiedadesForm As New PropiedadesControles.PropiedadesControlP 'propiedades formularios

            'determinamos el aspecto para los botones personalizados
            PropiedadesBoton.ColorFondo = Color.Empty
            PropiedadesBoton.ColorOver = Color.Empty
            PropiedadesBoton.ForeColor = Color.Black
            PropiedadesBoton.ForeColorOver = Color.Black

            'determinamos el aspecto para los controles
            PropiedadesCtrl.ColorFondo = Color.Empty
            PropiedadesCtrl.TituloForeColor = Color.Blue

            'determinamos el aspecto para los controles de entrada/salida
            PropiedadesES.ColorEdicion = Color.White
            PropiedadesES.ColorConsulta = Color.Empty
            PropiedadesES.ForeColor = Color.Black
            PropiedadesES.ForeColorError = Color.Red
            PropiedadesES.ColorFondo = Color.White
            PropiedadesES.ColorOver = Color.White

            'determinamos las propiedades del formulario
            PropiedadesForm = PropiedadesCtrl

            Crear(pTablaNavegacion, PropiedadesForm, PropiedadesES, PropiedadesBoton)
        End Sub


        Public Sub New(ByVal pTablaNavegacion As Hashtable, ByVal pPropiedadesForm As PropiedadesControles.PropiedadesControlP, _
        ByVal pPropiedadesES As PropiedadesControles.PropiedadesControlP, ByVal pPropiedadesBoton As PropiedadesControles.PropiedadesControlP)
            Crear(pTablaNavegacion, pPropiedadesForm, pPropiedadesES, pPropiedadesBoton)
        End Sub


        Private Sub Crear(ByVal pTablaNavegacion As Hashtable, ByVal pPropiedadesForm As PropiedadesControles.PropiedadesControlP, ByVal pPropiedadesES As PropiedadesControles.PropiedadesControlP, ByVal pPropiedadesBoton As PropiedadesControles.PropiedadesControlP)

            Try
                'instanciamos datosmarco
                DatosMarco = New Hashtable

                'asignamos la tabla de navegación
                TablaNavegacion = pTablaNavegacion

                'instancio la colección que lleva el motor de forms
                ColForms = New ArrayList

                'asignamos las propiedades
                PropiedadesForm = pPropiedadesForm
                PropiedadesES = pPropiedadesES
                PropiedadesBoton = pPropiedadesBoton

            Catch ex As Exception
                Throw ex
            End Try

        End Sub


        Protected Function GenerarDatosIniciales() As Hashtable
            Dim datos As Hashtable

            Try
                'cargo los datos que de le han de pasar al formulario
                datos = New Hashtable
                datos.Add("PropiedadesControl", PropiedadesForm) 'agregamos el formato form
                datos.Add("PropiedadesBoton", PropiedadesBoton) 'prop. botones
                datos.Add("PropiedadesES", PropiedadesES) 'prop. ES
                datos.Add("Marco", Me) 'le mandamos como marco la instancia de mi mismo

                Return datos
            Catch ex As Exception
                Throw ex
            End Try
        End Function

#End Region

#Region "Propiedades"
        'un hashtable que contiene los datos globales de la aplicación (usuario, configs...)
        Public Property DatosMarco() As Hashtable Implements Motor.INavegador.DatosMarco
            Get
                Return mDatosMarco
            End Get
            Set(ByVal Value As Hashtable)
                mDatosMarco = Value
            End Set
        End Property

        'una hashtable que contiene todas las rutas de navegación de formularios
        Public Property TablaNavegacion() As Hashtable Implements INavegador.TablaNavegacion
            Get
                Return mTablaNavegacion
            End Get
            Set(ByVal Value As Hashtable)
                mTablaNavegacion = Value
            End Set
        End Property

        'Las Propiedades de apariencia que se pasan a los Formularios
        Public Property PropiedadesBoton() As PropiedadesControles.PropiedadesControlP 'propiedades Botones
            Get
                Return mPropiedadesBoton
            End Get
            Set(ByVal Value As PropiedadesControles.PropiedadesControlP)
                mPropiedadesBoton = Value
            End Set
        End Property

        Public Property PropiedadesCtrl() As PropiedadesControles.PropiedadesControlP 'Propiedades formularios
            Get
                Return mPropiedadesCtrl
            End Get
            Set(ByVal Value As PropiedadesControles.PropiedadesControlP)
                mPropiedadesCtrl = Value
            End Set
        End Property

        Public Property PropiedadesES() As PropiedadesControles.PropiedadesControlP 'propiedades ctrles edición/consulta
            Get
                Return mPropiedadesES
            End Get
            Set(ByVal Value As PropiedadesControles.PropiedadesControlP)
                mPropiedadesES = Value
            End Set
        End Property

        Public Property PropiedadesForm() As PropiedadesControles.PropiedadesControlP 'propiedades formularios
            Get
                Return mPropiedadesForm
            End Get
            Set(ByVal Value As PropiedadesControles.PropiedadesControlP)
                mPropiedadesForm = Value
            End Set
        End Property

        Public ReadOnly Property Principal() As Object Implements INavegador.Principal
            Get
                If Not Me.mDatosMarco Is Nothing AndAlso Me.mDatosMarco.Contains("Principal") Then
                    Dim mip As Framework.DatosNegocio.EntidadDN = Me.mDatosMarco("Principal")
                    Return mip.Clone
                End If
                Return Nothing
            End Get
        End Property

#End Region


#Region "métodos"

#Region "Métodos de navegación inicial"

        ''' <summary>
        ''' Navega al primer formulario de la aplicación generando los datos necesarios
        ''' de PropiedadesControles de manera adecuada
        ''' </summary>
        ''' <param name="funcion">La función de navegación a la que se quiere navegar
        ''' al comenzar la aplicación</param>
        Public Overloads Sub NavegarInicial(ByVal funcion As String) Implements INavegador.NavegarInicial
            Me.Navegar(funcion, Me, Nothing, TipoNavegacion.Normal, Me.GenerarDatosIniciales, Nothing)
        End Sub

        ''' <summary>
        ''' Navega al primer formulario de la aplicación generando los datos necesarios
        ''' de PropiedadesControles de manera adecuada
        ''' </summary>
        ''' <param name="funcion">La función de navegación a la que se quiere navegar
        ''' al comenzar la aplicación</param>
        '''<param name="paquete">El paquete que se le quiere pasar al formulario inicial</param>
        Public Overloads Sub NavegarInicial(ByVal funcion As String, ByVal paquete As Hashtable)
            Me.Navegar(funcion, Me, Nothing, TipoNavegacion.Normal, Me.GenerarDatosIniciales, paquete)
        End Sub

#End Region



        ''' <summary>
        ''' Navega a un formulario con un MDI padre
        ''' </summary>
        ''' <param name="Funcion">El nombre de la función a la que se quiere navegar</param>
        ''' <param name="Sender">El formulario desde el que se navega</param>
        ''' <param name="TipoNavegacion">Tipo de navegación a realizar</param>
        ''' <param name="Paquete">Hashtable que contiene los datos para inicializar en el formulario de destino</param>
        ''' <param name="Padre">El nombre del formulario MDI que contandrá al formulario al que se navega</param>
        Public Overloads Sub Navegar(ByVal Funcion As String, ByVal Sender As Form, ByVal Padre As Form, ByVal TipoNavegacion As TipoNavegacion, ByRef Paquete As Hashtable) Implements INavegador.Navegar
            Navegar(Funcion, Sender, Padre, TipoNavegacion, CType(Sender, FormulariosP.FormularioBase).GenerarDatosCarga, Paquete)
        End Sub

        ''' <summary>
        ''' Navega a un formulario sin un MDI padre
        ''' </summary>
        ''' <param name="Funcion">El nombre de la función a la que se quiere navegar</param>
        ''' <param name="Sender">El formulario desde el que se navega</param>
        ''' <param name="TipoNavegacion">Tipo de navegación a realizar</param>
        ''' <param name="Paquete">Hashtable que contiene los datos para inicializar en el formulario de destino</param>
        Public Overloads Sub Navegar(ByVal Funcion As String, ByVal Sender As Form, ByVal TipoNavegacion As TipoNavegacion, ByRef Paquete As Hashtable) Implements INavegador.Navegar
            Navegar(Funcion, Sender, Nothing, TipoNavegacion, CType(Sender, FormulariosP.FormularioBase).GenerarDatosCarga, Paquete)
        End Sub

        ''' <summary>
        ''' Navegación Completa
        ''' </summary>
        ''' <param name="Funcion">El nombre de la función a la que se quiere navegar</param>
        ''' <param name="Sender">El formulario desde el que se navega</param>
        ''' <param name="Padre">El nombre del formulario MDI que contandrá al formulario al que se navega</param>
        ''' <param name="pTipoNavegacion">Tipo de navegación a realizar</param>
        ''' <param name="Datos">Los datos de carga que contienen los Formatos de aspecto y comportamiento</param>
        ''' <param name="paquete">Contiene los datos que se le quieren pasar al formulario</param>
        ''' <remarks></remarks>
        Public Overloads Sub Navegar(ByVal Funcion As String, ByVal Sender As Object, ByVal Padre As Form, ByVal pTipoNavegacion As TipoNavegacion, ByVal Datos As Hashtable, ByRef paquete As Hashtable) Implements Motor.INavegador.Navegar
            Navegacion(Funcion, Sender, Padre, pTipoNavegacion, Datos, paquete)
        End Sub

        ''' <summary>
        ''' Navegación completa que devuelve una referencia al formulario creado
        ''' </summary>
        ''' <param name="Funcion">El nombre de la función a la que se quiere navegar</param>
        ''' <param name="Sender">El formulario desde el que se navega</param>
        ''' <param name="Padre">El nombre del formulario MDI que contandrá al formulario al que se navega</param>
        ''' <param name="pTipoNavegacion">Tipo de navegación a realizar</param>
        ''' <param name="Datos">Los datos de carga que contienen los Formatos de aspecto y comportamiento</param>
        ''' <param name="paquete">Contiene los datos que se le quieren pasar al formulario</param>
        ''' <param name="NuevoForm">Devuelve byref el nuevo formulario que se ha creado</param>
        ''' <remarks></remarks>
        Public Overloads Sub Navegar(ByVal Funcion As String, ByVal Sender As Object, ByVal Padre As System.Windows.Forms.Form, ByVal pTipoNavegacion As TipoNavegacion, ByVal Datos As System.Collections.Hashtable, ByRef paquete As System.Collections.Hashtable, ByRef NuevoForm As FormulariosP.IFormularioP) Implements INavegador.Navegar
            NuevoForm = Navegacion(Funcion, Sender, Padre, pTipoNavegacion, Datos, paquete)
        End Sub

        ''' <summary>
        '''Realiza la función denavegación y devuelve 
        '''una referencia al formulario que se crea
        ''' </summary>
        ''' <param name="Funcion">El nombre de la función a la que se quiere navegar</param>
        ''' <param name="Sender">El formulario desde el que se navega</param>
        ''' <param name="Padre">El nombre del formulario MDI que contandrá al formulario al que se navega</param>
        ''' <param name="pTipoNavegacion">Tipo de navegación a realizar</param>
        ''' <param name="Datos">Los datos de carga que contienen los Formatos de aspecto y comportamiento</param>
        ''' <param name="paquete">Contiene los datos que se le quieren pasar al formulario</param>
        ''' <returns>El formulario que se ha creado en la navegación</returns>
        ''' <remarks></remarks>
        Private Function Navegacion(ByVal Funcion As String, ByVal Sender As Object, ByVal Padre As Form, ByVal pTipoNavegacion As TipoNavegacion, ByVal Datos As Hashtable, ByRef paquete As Hashtable) As System.Windows.Forms.Form
            'cogemos la función que nos pasan, buscamos en el navegador el formulario que 
            'se corresponde y lo cargamos con todas las propiedades que sean
            Dim midestino As Motor.Destino = Nothing
            Dim miControlador As FormulariosP.IControladorForm = Nothing
            Dim formulario As Form = Nothing
            Dim formulariop As FormulariosP.IFormularioP = Nothing
            Dim miSender As Form = Nothing

            Try
                'validación
                If String.IsNullOrEmpty(Funcion) Then
                    Throw (New ApplicationException("La función de navegación está vacía"))
                End If

                'buscamos la función en la tabla de navegación
                If Not TablaNavegacion.ContainsKey(Funcion) Then
                    Throw New ApplicationException("No se encontró la función de navegación")
                End If

                'si no se trata de un formulario es como si fuera nothing
                If Not Sender Is Nothing AndAlso TypeOf Sender Is Form Then
                    miSender = Sender
                ElseIf Not TypeOf Sender Is INavegador Then
                    'el navegador puede lanzar formularios porque inicia la aplicación
                    Throw New ApplicationException("El objeto lanzador está vacío o no es un Formulario")
                End If

                'establecemos el formulario
                midestino = TablaNavegacion.Item(Funcion)

                Dim miassemblyform, miassemblycontr As System.Reflection.Assembly

                'permitimos que no haya controlador
                If Not midestino.Controlador Is Nothing Then
                    miassemblycontr = midestino.Controlador.Assembly
                    miControlador = miassemblycontr.CreateInstance(midestino.Controlador.FullName)
                    If Not miControlador Is Nothing Then
                        miControlador.Marco = Me
                    Else
                        'si aquí nos habían establecido un controlador, es que es
                        'necesario para este formulario, así que lanzamos una 
                        'excepción
                        Throw New ApplicationException("No se ha encontrado el controlador " & midestino.Controlador.FullName & " en el ensamblado " & miassemblycontr.FullName)

                    End If
                End If

                'cargamos el formulario
                miassemblyform = midestino.Formulario.Assembly
                formulario = miassemblyform.CreateInstance(midestino.Formulario.FullName)

                If formulario Is Nothing Then
                    Throw New ApplicationException("No se ha creado correctamente la instancia del formulario " & midestino.Formulario.FullName & " en el ensamblado " & miassemblyform.FullName)
                End If

                'si es un formulario personalizado, lo inicializamos
                If TypeOf formulario Is FormulariosP.IFormularioP Then
                    formulariop = formulario
                    'le pasamos el controlador
                    formulariop.Controlador = miControlador
                    'le pasamos los datos
                    formulariop.Datos = Datos
                    'le pasamos el paquete
                    formulariop.Paquete = paquete
                    'ahora inicializamos: esto lanza la inicialización de los controles que haya dentro de él, y así en cascada
                    formulariop.Inicializar()
                End If

                'en función del tipo de navegación que nos pidan, vemos cómo lo
                'cargamos
                CargarFormularioPorTipoNavegacion(formulario, miSender, Padre, pTipoNavegacion)

                If Not formulariop Is Nothing Then
                    formulariop.PostInicializar()
                End If


                'ahora devolvemos la referencia al formulario 
                'que acabamos de crear
                Return formulario


            Catch ex As Exception
                Throw ex
            End Try
        End Function

        ''' <summary>
        ''' Realiza la carga del formulario en función
        ''' del tipo de navegación que se ha solicitado
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub CargarFormularioPorTipoNavegacion(ByVal pFormulario As Form, ByVal pSender As Form, ByVal pPadre As Form, ByVal pTipoNavegacion As TipoNavegacion)
            Select Case pTipoNavegacion
                Case TipoNavegacion.Normal
                    'cargar normal (multiinstancia)
                    CargarForm(pFormulario, False, pSender, pPadre, False)

                Case TipoNavegacion.Poseido
                    'cargar form poseido
                    'TODO: luis - MOTOR - cargar form poseido: No implementado
                    Throw New NotImplementedException("Formulario poseído no implementado")

                Case TipoNavegacion.Modal
                    'cargar modal
                    CargarForm(pFormulario, False, pSender, pPadre, True)

                Case TipoNavegacion.MonoInstancia
                    'cargar una sóla vez
                    CargarFormUnico(pFormulario, False, pSender, pPadre)

                Case TipoNavegacion.CerrarLanzador
                    'cargar y cerrar el sender
                    CargarForm(pFormulario, True, pSender, pPadre, False)

                Case TipoNavegacion.CerrarLanzador_y_MonoInstancia
                    'cerrar el sender y cargar una sóla vez
                    CargarFormUnico(pFormulario, True, pSender, pPadre)

                Case TipoNavegacion.MonoInstReemplazo
                    CargarFormUnicoReemplazo(pFormulario, False, pSender, pPadre)

                Case TipoNavegacion.CerrarLanzador_y_MonoInstReemplazo
                    CargarFormUnicoReemplazo(pFormulario, True, pSender, pPadre)

            End Select
        End Sub

        ''' <summary>
        ''' Devuelve un formulario que coincida con el nombre del formulario origen
        ''' de entre los que estén cargados en el motor
        ''' </summary>
        ''' <param name="pFormulario">El formulario que se está buscando</param>
        ''' <returns>El formulario, si es que se encuentra en la colección</returns>
        ''' <remarks></remarks>
        Private Function DameFormularioPorNombre(ByVal pFormulario As Form) As Form
            For Each miformulario As Form In ColForms
                'lo buscamos por el nombre del formulario
                If miformulario.Name = pFormulario.Name Then
                    'lo mostramos y salimos
                    Return miformulario
                End If
            Next

            Return Nothing
        End Function

        ''' <summary>
        ''' Carga un formulario
        ''' </summary>
        ''' <param name="formulario">el formularioa  cargar</param>
        ''' <param name="cerrarme">si hay que cerrar el formulario lanzador</param>
        ''' <param name="sender">el formulario lanzador</param>
        ''' <param name="parent">el formulario MdiParent</param>
        ''' <param name="modal">si se abre de forma modal</param>
        ''' <remarks></remarks>
        Private Sub CargarForm(ByVal formulario As Form, ByVal cerrarme As Boolean, ByVal sender As Form, ByVal parent As Form, ByVal modal As Boolean)
            Try
                If formulario Is Nothing Then Exit Sub

                'ponemos el padre -> si no es modal (una ventana de nivel inferior no puedemostrarse como modal)
                If Not parent Is Nothing Then
                    If modal = False Then
                        formulario.MdiParent = parent
                        'Else
                        '   formulario.Owner = parent
                    End If
                End If

                'enlazamos el formulario a nuestro método closed
                AddHandler formulario.Closed, AddressOf frm_Closed

                'agregamos el formulario a nuestra lista
                ColForms.Add(formulario)

                'lo mostramos
                If modal = False Then
                    formulario.Show()
                Else
                    formulario.StartPosition = FormStartPosition.CenterScreen
                    formulario.ShowDialog(sender)
                End If


                'si hay q cerrar al lanzador, lo hacemos
                If cerrarme = True Then
                    If Not sender Is Nothing Then
                        CerrarForm(sender)
                    End If
                End If



            Catch ex As Exception
                Throw ex
            Finally
                'si hay q cerrar al padre, lo hacemos
                If cerrarme = True Then
                    If Not sender Is Nothing Then
                        CerrarForm(sender)
                    End If
                End If
            End Try
        End Sub

        ''' <summary>
        ''' es como cargarform, sólo que si el formulario ya está cargado simplemente lo muestra
        ''' </summary>
        ''' <param name="formulario">el formularioa  cargar</param>
        ''' <param name="cerrarme">si hay que cerrar el formulario lanzador</param>
        ''' <param name="sender">el formulario lanzador</param>
        ''' <param name="parent">el formulario MdiParent</param>
        ''' <remarks></remarks>
        Private Sub CargarFormUnico(ByVal formulario As Form, ByVal cerrarme As Boolean, ByVal sender As Form, ByVal parent As Form)

            Dim miformulario As Form = Me.DameFormularioPorNombre(formulario)

            If Not miformulario Is Nothing Then
                miformulario.Show()
            Else
                CargarForm(formulario, cerrarme, sender, parent, False)
            End If

        End Sub

        ''' <summary>
        ''' Cargar el formulario. Si a estaba cargado, cierra la instancia
        ''' actual y abre una nueva
        ''' </summary>
        ''' <param name="formulario">el formularioa  cargar</param>
        ''' <param name="cerrarme">si hay que cerrar el formulario lanzador</param>
        ''' <param name="sender">el formulario lanzador</param>
        ''' <param name="parent">el formulario MdiParent</param>
        ''' <remarks></remarks>
        Private Sub CargarFormUnicoReemplazo(ByVal formulario As Form, ByVal cerrarme As Boolean, ByVal sender As Form, ByVal parent As Form)
            Dim miformulario As Form = Me.DameFormularioPorNombre(formulario)

            If Not miformulario Is Nothing Then
                Me.CerrarForm(miformulario)
            End If

            CargarForm(formulario, cerrarme, sender, parent, False)
        End Sub

        Public Sub CerrarForm(ByVal formulario As Form)
            Try
                If formulario Is Nothing Then Exit Sub

                If ColForms.Contains(formulario) Then
                    formulario.Close()
                End If
            Catch ex As Exception
                Throw ex
            End Try
        End Sub

        Private Sub frm_Closed(ByVal sender As Object, ByVal e As System.EventArgs)
            Dim f As Form
            Try
                f = sender
                RemoveHandler f.Closed, AddressOf frm_Closed
                ColForms.Remove(f)
                If ColForms.Count = 0 Then
                    Application.Exit()
                End If
            Catch ex As Exception
                Throw ex
            End Try
        End Sub

#Region "Tratamiento Errores"

        Public Overridable Overloads Sub MostrarError(ByVal excepcion As System.Exception, ByVal titulo As String) Implements INavegador.MostrarError
            If TypeOf excepcion Is System.Web.Services.Protocols.SoapException Then
                MessageBox.Show("Error:" & Chr(13) & Chr(10) & Chr(13) & Chr(10) & ExceptionHelper.ConversorExcepcionSoap(excepcion), titulo, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1)
            Else
                MessageBox.Show("Error:" & Chr(13) & Chr(10) & Chr(13) & Chr(10) & excepcion.Message, titulo, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1)
            End If
        End Sub

        Public Overridable Overloads Sub MostrarError(ByVal excepcion As System.Exception, ByVal sender As Windows.Forms.Control) Implements INavegador.MostrarError
            MostrarError(excepcion, sender.Text)
        End Sub

        Public Overridable Overloads Sub MostrarError(ByVal excepcion As System.Exception, ByVal sender As Windows.Forms.Form) Implements INavegador.MostrarError
            MostrarError(excepcion, sender.Text)
        End Sub

#End Region



#Region "Mostrar mensajes modales"
        Public Sub MostrarAdvertencia(ByVal mensaje As String, ByVal titulo As String) Implements INavegador.MostrarAdvertencia
            MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Sub

        Public Sub MostrarInformacion(ByVal mensaje As String, ByVal titulo As String) Implements INavegador.MostrarInformacion
            MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub
#End Region

#End Region

    End Class

#Region "Ejemplo de Sub Main()"
    'Public Module modMarco

    '    Public PropiedadesBoton As PropiedadesControles.PropiedadesControlP 'propiedades Botones
    '    Public PropiedadesCtrl As PropiedadesControles.PropiedadesControlP 'Propiedades formularios
    '    Public PropiedadesES As PropiedadesControles.PropiedadesControlP 'propiedades ctrles edición/consulta
    '    Public PropiedadesForm As PropiedadesControles.PropiedadesControlP 'propiedades formularios

    '    Public Navegador As Hashtable

    '    'Public Enum TipoNavegacion As Short
    '    '    Normal = 0 'lanza el form
    '    '    Modal = 1 'lo lanza y espera cierre
    '    '    Poseido = 2 'lo lanza como formulario poseído
    '    '    CerrarLanzador = 3 'lo lanza y cierra al padre
    '    '    MonoInstancia = 4 'si existe antes, sólo lo muestra
    '    '    CerrarLanzador_y_MonoInstancia = 5 '3+4
    '    'End Enum

    '    Public Sub Main()
    '        Dim cmarco As Marco
    '        Dim midestino As Destino

    '        'determinamos el aspecto para los botones personalizados
    '        PropiedadesBoton = New PropiedadesControles.PropiedadesControlP
    '        PropiedadesBoton.ColorFondo = ColorTranslator.FromWin32(13434879)
    '        PropiedadesBoton.ColorOver = Color.Brown
    '        PropiedadesBoton.ForeColor = Color.Black
    '        PropiedadesBoton.ForeColorOver = Color.White

    '        'determinamos el aspecto para los controles
    '        PropiedadesCtrl = New PropiedadesControles.PropiedadesControlP
    '        PropiedadesCtrl.ColorFondo = ColorTranslator.FromWin32(13434879)
    '        PropiedadesCtrl.TituloForeColor = Color.Brown

    '        'determinamos el aspecto para los controles de entrada/salida
    '        PropiedadesES = New PropiedadesControles.PropiedadesControlP
    '        PropiedadesES.ColorEdicion = Color.White
    '        PropiedadesES.ColorConsulta = ColorTranslator.FromWin32(13434879)
    '        PropiedadesES.ForeColor = Color.Black
    '        PropiedadesES.ForeColorError = Color.Red
    '        PropiedadesES.ColorFondo = Color.White
    '        PropiedadesES.ColorOver = Color.White

    '        'determinamos las propiedades del formulario
    '        PropiedadesForm = PropiedadesCtrl


    '        'TODO: esto debe ir en la inicialización??
    '        'cargamos los datos para el navegador
    '        Navegador = New Hashtable

    '        Navegador.Add("Login", New Destino("MarcoAplicacion.frmLogin", "Controladores.ctrlLogin"))
    '        Navegador.Add("Splash", New Destino("MarcoAplicacion.frmSplash", "Controladores.ctrlSplash"))
    '        Navegador.Add("Marco", New Destino("MarcoAplicacion.frmMarco", "Controladores.ctrlMarco"))
    '        Navegador.Add("ConsultarAvisos", New Destino("MarcoAplicacion.frmAvisosConsultar", "Controladores.ctrlAvisosConsultar"))
    '        Navegador.Add("Test", New Destino("MarcoAplicacion.frmTest", "Controladores.ctrlDirecciones"))
    '        Navegador.Add("Contacto", New Destino("MarcoAplicacion.frmTestContacto", "Controladores.ctrlPersonas"))
    '        Navegador.Add("Clientes", New Destino("MarcoAplicacion.FrmTestCliente", "Controladores.ctrlCliente"))
    '        Navegador.Add("Testeador", New Destino("MarcoAplicacion.frmTesteador", "Controladores.ctrlTesteador"))
    '        Navegador.Add("BusquedaZonas", New Destino("MarcoaAplicacion.frmBusquedaZonas", "Controladores.ctrlDirecciones"))

    '        'instanciamos la clase que va a llevar el motor de formularios
    '        cmarco = New Marco

    '        Application.Run()
    '    End Sub

    'End Module
#End Region

#Region "Ejemplo de clase Marco"
    '
    'Public Class Marco

    '#Region "campos"
    '    Private ColForms As ArrayList
    '    Private Navegador As Hashtable
    '#End Region

    '#Region "constructor"
    '    Public Sub New(ByVal pNavegador As Hashtable, ByVal PropiedadesForm As PropiedadesControles.PropiedadesControlP, ByVal PropiedadesBoton As PropiedadesControles.PropiedadesControlP, ByVal PropiedadesES As PropiedadesControles.PropiedadesControlP)
    '        Dim datos As Hashtable
    '        Try
    '            'establezco el hashtable de navegación
    '            Me.Navegador = pNavegador
    '            'cargamos el 1er formulario
    '            ColForms = New ArrayList 'instancio la colección que lleva el motor de forms
    '            'cargo el 1er formulario
    '            '--> 1.- cargo los datos que de le han de pasar al formulario
    '            datos = New Hashtable
    '            datos.Add("PropiedadesControl", PropiedadesForm) 'agregamos el formato form
    '            datos.Add("PropiedadesBoton", PropiedadesBoton) 'prop. botones
    '            datos.Add("PropiedadesES", PropiedadesES) 'prop. ES
    '            datos.Add("Marco", Me) 'le mandamos como marco la instancia de mi mismo
    '            '--> 2.- navego hacia la función login
    '            Navegar("Login", Me, Nothing, TipoNavegacion.Normal, datos, Nothing)

    '            'CargarForm(New frmLogin(Me), False, Nothing, Nothing)
    '        Catch ex As Exception
    '            MsgBox("Error gordo " & ex.Message)
    '            Application.Exit()
    '        End Try
    '    End Sub
    '#End Region


    '    Public Sub Navegar(ByVal Funcion As String, ByVal Sender As Object, ByVal Padre As Form, ByVal pTipoNavegacion As TipoNavegacion, ByVal Datos As Hashtable, ByRef paquete As ArrayList)
    '        'cogemos la función que nos pasan, buscamos en el navegador el formulario que 
    '        'se corresponde y lo cargamos con todas las propiedades que sean
    '        Dim ref As System.Reflection.Assembly
    '        Dim contr As System.Reflection.Assembly
    '        Dim midestino As Destino
    '        Dim miControlador As Object
    '        Dim formulario As Form
    '        Dim formulariop As IFormularioP
    '        Dim miSender As Form
    '        Dim miassembly As String

    '        '
    '        '   ^^
    '        '  |OO____o
    '        '  |  vvvv       ¡guau!
    '        '  |  ----   
    '        '  |  |        
    '        '

    '        Try
    '            'validación
    '            If Not Funcion Is Nothing AndAlso Funcion <> "" Then
    '                'hay función
    '                If Not Sender Is Nothing Then
    '                    If TypeOf Sender Is Form Then
    '                        'definimos el sender como formulario, si no, al cargar
    '                        'pasaremos como sender un nothing
    '                        miSender = Sender
    '                    End If
    '                Else
    '                    Throw New ApplicationException("El objeto lanzador está vacío")
    '                End If
    '            Else
    '                'error
    '                Throw (New ApplicationException("La función de navegación está vacía"))
    '            End If

    '            'buscamos la función en la tabla de navegación
    '            If Navegador.ContainsKey(Funcion) Then
    '                'establecemos el formulario
    '                midestino = Navegador.Item(Funcion)
    '                'obtengo el controlador
    '                ' contr = Reflection.Assembly.Load("D:\gedas\WindowsForms\WindowsForms\MarcoAplicación\bin\" & midestino.Controlador.Split(".")(0))
    '                ' --> es instr. sería si queremos ponerle un path determinado:
    '                ' --> contr = Reflection.Assembly.LoadFrom("D:\gedas\WindowsForms\WindowsForms\MarcoAplicación\bin\Framework.IU.dll")

    '                'defino la dll en la q está (cojo el "miperro" de "miperro.controlador")
    '                miassembly = Left(midestino.Controlador, InStr(midestino.Controlador, ".") - 1)
    '                'cargo el ensamblado que me ha especificado
    '                contr = Reflection.Assembly.Load(miassembly)
    '                'creo una instancia y se la asigno a micontrolador
    '                'es como hacer micontrolador=new...
    '                miControlador = contr.CreateInstance(midestino.Controlador)

    '                'Lo mismo, pero con el formulario
    '                'obtengo el ensamblado en el que se encuentra el formulario
    '                miassembly = Left(midestino.Formulario, InStr(midestino.Formulario, ".") - 1)
    '                'lo cargo
    '                ref = Reflection.Assembly.Load(miassembly)
    '                formulario = ref.CreateInstance(midestino.Formulario)


    '                'si es un formulario personalizado, lo inicializamos
    '                If TypeOf formulario Is IFormularioP Then
    '                    formulariop = formulario
    '                    'le pasamos el controlador
    '                    formulariop.Controlador = miControlador
    '                    'le pasamos los datos
    '                    formulariop.Datos = Datos
    '                    'le pasamos el paquete
    '                    formulariop.Paquete = paquete
    '                    'ahora inicializamos: esto lanza la inicialización de los controles que haya dentro de él, y así en cascada
    '                    formulariop.Inicializar()
    '                End If

    '                'en función del tipo de navegación que nos pidan, vemos cómo lo
    '                'cargamos
    '                Select Case pTipoNavegacion
    '                    Case TipoNavegacion.Normal
    '                        'cargar normal (multiinstancia)
    '                        CargarForm(formulario, False, miSender, Padre, False)
    '                    Case TipoNavegacion.Poseido
    '                        'cargar form poseido
    '                        'TODO: luis - No implementado
    '                    Case TipoNavegacion.Modal
    '                        'cargar modal
    '                        CargarForm(formulario, False, miSender, Padre, True)
    '                    Case TipoNavegacion.MonoInstancia
    '                        'cargar una sóla vez
    '                        CargarFormUnico(formulario, False, miSender, Padre)
    '                    Case TipoNavegacion.CerrarLanzador
    '                        'cargar y cerrar el sender
    '                        CargarForm(formulario, True, miSender, Padre, False)
    '                    Case TipoNavegacion.CerrarLanzador_y_MonoInstancia
    '                        'cerrar el sender y cargar una sóla vez
    '                        CargarFormUnico(formulario, True, miSender, Padre)
    '                End Select

    '            Else
    '                'no ha encontrado la función
    '                Throw New ApplicationException("No se encontró la función de navegación")
    '            End If

    '        Catch ex As Exception
    '            Throw ex
    '        End Try
    '    End Sub

    '    Public Sub CargarForm(ByVal formulario As Form, ByVal cerrarme As Boolean, ByVal sender As Form, ByVal parent As Form, ByVal modal As Boolean)
    '        Try
    '            If formulario Is Nothing Then Exit Sub

    '            'ponemos el padre -> si no es modal (una ventana de nivel inferior no puedemostrarse como modal)
    '            If Not parent Is Nothing AndAlso modal = False Then
    '                formulario.MdiParent = parent
    '            End If

    '            'enlazamos el formulario a nuestro método closed
    '            AddHandler formulario.Closed, AddressOf frm_Closed

    '            'agregamos el formulario a nuestra lista
    '            ColForms.Add(formulario)

    '            'lo mostramos
    '            If modal = False Then
    '                formulario.Show()
    '            Else
    '                formulario.ShowDialog(sender)
    '            End If


    '            'si hay q cerrar al padre, lo hacemos
    '            If cerrarme = True Then
    '                If Not sender Is Nothing Then
    '                    CerrarForm(sender)
    '                End If
    '            End If



    '        Catch ex As Exception
    '            MsgBox("Error  " & ex.Message)
    '        Finally
    '            'si hay q cerrar al padre, lo hacemos
    '            If cerrarme = True Then
    '                If Not sender Is Nothing Then
    '                    CerrarForm(sender)
    '                End If
    '            End If
    '        End Try
    '    End Sub

    '    Public Sub CargarFormUnico(ByVal formulario As Form, ByVal cerrarme As Boolean, ByVal sender As Form, ByVal parent As Form)
    '        'es como cargarform, sólo que si el formulario ya está cargado simplemente lo muestra
    '        Dim miformulario As Form
    '        Try
    '            For Each miformulario In ColForms
    '                'lo buscamos por el nombre del formulario
    '                If miformulario.Name = formulario.Name Then
    '                    'lo mostramos y salimos
    '                    miformulario.Show()
    '                    Exit Sub
    '                End If
    '            Next
    '            'si llega hasta aquí, no existe en la col de forms
    '            CargarForm(formulario, cerrarme, sender, parent, False)
    '        Catch ex As Exception
    '            MsgBox("Error gordo: " & ex.Message)
    '        End Try
    '    End Sub

    '    Public Sub CerrarForm(ByVal formulario As Form)
    '        Try
    '            If formulario Is Nothing Then Exit Sub

    '            If ColForms.Contains(formulario) Then
    '                formulario.Close()
    '            End If
    '        Catch ex As Exception
    '            MsgBox("Error gordo " & ex.Message)
    '        End Try
    '    End Sub

    '    Private Sub frm_Closed(ByVal sender As Object, ByVal e As System.EventArgs)
    '        Dim f As Form
    '        Try
    '            f = sender
    '            RemoveHandler f.Closed, AddressOf frm_Closed
    '            ColForms.Remove(f)
    '            If ColForms.Count = 0 Then
    '                Application.Exit()
    '            End If
    '        Catch ex As Exception
    '            Throw ex
    '        End Try
    '    End Sub
    'End Class
#End Region
End Namespace

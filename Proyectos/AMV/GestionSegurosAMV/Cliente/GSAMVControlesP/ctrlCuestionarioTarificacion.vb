Imports Framework.DatosNegocio.Localizaciones.Temporales
Imports Framework.Cuestionario.CuestionarioDN
Imports MotorBusquedaBasicasDN
Imports Framework.IU.IUComun

Public Class ctrlCuestionarioTarificacion
    Inherits MotorIU.ControlesP.BaseControlP

#Region "Atributos"

    Private mCuestionario As CuestionarioDN
    Private mCuestionarioResuelto As CuestionarioResueltoDN

    Private mControlador As GSAMVControladores.ctrlCuestionarioTarificacion

    Private mColLocalidadesTodas As FN.Localizaciones.DN.ColLocalidadDN
    Private mColLocalidadesFiltradasporCP As FN.Localizaciones.DN.ColLocalidadDN

    Private mTomador As FN.Seguros.Polizas.DN.TomadorDN
    Private mColTelefonos As ArrayList
    Private mColEmail As ArrayList
    Private mColFax As ArrayList
    Private mColDirecciones As ArrayList
    Private mColCaracteristicas As ColCaracteristicaDN
    Private mFechaEfecto As Date

#Region "Restricciones y validaciones en caliente"
    Private mAdmiteMulticonductor As Boolean
    Private mEdadAnosMinimo As Integer
    Private mCarnetsAdmitidos As List(Of FN.RiesgosVehiculos.DN.TipoCarnet)
    Private mAnosCarnetMinimo As Integer
    Private mAdmiteNoMatriculado As Boolean = True
#End Region

#End Region

#Region "Inicializador"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        'instanciamos nuestro controlador tipado
        Me.mControlador = New GSAMVControladores.ctrlCuestionarioTarificacion(Me.Marco, Me)
        Me.Controlador = Me.mControlador

        'inicializar numeroconductores
        For a As Integer = 0 To 4
            Me.cboNumeroConductoresAdic.Items.Add(a)
        Next
        Me.cboNumeroConductoresAdic.SelectedItem = Me.cboNumeroConductoresAdic.Items(0)

        'inicilaizamos el sexo
        Dim sexos As ArrayList = Me.mControlador.ObtenerSexos()
        Me.cboSexo.Items.AddRange(sexos.ToArray())
        Me.cboSexo.SelectedItem = Me.cboSexo.Items(0)

        'establecemos las localidades
        'Me.mColLocalidadesTodas = Me.mControlador.ObtenerLocalidades()
        'Me.cboLocalidadCondHabitual.Items.AddRange(mColLocalidadesTodas.ToArray())
        Me.cboLocalidadCondHabitual.Enabled = False

        For Each elto As Object In [Enum].GetValues(GetType(FN.RiesgosVehiculos.DN.TipoCarnet))
            Me.cboTipoCarne.Items.Add(elto)
        Next

        'obtenemos todas las marcas que haya
        Dim listamarcas As ArrayList = Me.mControlador.ObtenerMarcas()
        Me.cboMarca.Items.AddRange(listamarcas.ToArray())

        'ponemos el del modelo disabled
        Me.cboModelo.Enabled = False

        'ponemos las fechas de hoy en todos los dtp
        mFechaEfecto = Now()
        Me.dtpFecha1Matricula.Value = Now()
        Me.dtpFechaCarne.Value = Now()
        Me.dtpFechaFabricacion.Value = Now()
        Me.dtpFechaNacimiento.Value = Now()
        Me.dtpVencimientoSeguro.Value = Now()

        Me.ctrlMulticonductor1.FechaEfecto = mFechaEfecto

        'mostramos el primer panel
        MostrarPanel(Me.grpDatosIniciales)
        Me.cmdTerminarCuestionario.Enabled = False

        'deshabilitamos las opciones que deben estarlo

        'inhabilitamos el boón de buscar cliente
        Me.cmdBuscar.Enabled = False
        Me.lblIDCliente.Visible = False
        Me.lblIDClienteValor.Visible = False
        'inhabilitamos la fecha de 1 matriculación
        Me.dtpFecha1Matricula.Enabled = False
        'inhabilitamos los datos de 'ya está asegurado'
        Me.dtpVencimientoSeguro.Enabled = False

        ''si estamos en ES, deshabilitamos los grps para que se rellene en orden
        'If Not Me.PropiedadesES Is Nothing Then
        '    If Me.PropiedadesES.TipoControl = PropiedadesControles.TipoControl.Entrada Then
        '        Me.grpBonificaciones.Enabled = False
        '        Me.grpCarnetConducir.Enabled = False
        '        Me.grpConductoresAdicionales.Enabled = False
        '        Me.grpDatosVehiculo.Enabled = False
        '        Me.grpAntecedentes.Enabled = False
        '        


        '    End If
        'End If

        'ponemos el tamaño correcto
        ResizeconductoresAdicionales()

        'habilitamos/deshabilitamos carnet de conducir
        ComprobarEnabledCarnet()

        'ponemos los años sin siniestro en 0
        Me.cboAñosSinSiniestro.SelectedIndex = 0

    End Sub

#End Region

#Region "Eventos"
    ''' <summary>
    ''' Se produce cuando el usuario hace click en "Terminar Cuestionario"
    ''' </summary>
    ''' <remarks></remarks>
    Public Event CuestionarioFinalizado()
#End Region

#Region "Propiedades"

    Public Overrides Property PropiedadesES() As PropiedadesControles.PropiedadesControlP
        Get
            Return MyBase.PropiedadesES
        End Get
        Set(ByVal value As PropiedadesControles.PropiedadesControlP)
            MyBase.PropiedadesES = value
            If Not value Is Nothing Then
                Select Case value.TipoControl
                    Case PropiedadesControles.TipoControl.Entrada
                        'es de edición, así que se puede terminar
                        'Me.cmdBuscarCliente.Visible = True
                        Me.Height = 2480
                    Case PropiedadesControles.TipoControl.Salida
                        'no es editable, así que no se puede terminar
                        'Me.cmdBuscarCliente.Visible = False
                        Me.Height = (2480 - Me.cmdTerminarCuestionario.Height)
                End Select
            End If
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property Cuestionario() As CuestionarioDN
        Get
            Return Me.mCuestionario
        End Get
        Set(ByVal value As CuestionarioDN)
            Me.mCuestionario = value
            DNaIU(value)
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property CuestionarioResuelto() As CuestionarioResueltoDN
        Get
            If IUaDN() Then
                Return Me.mCuestionarioResuelto
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As CuestionarioResueltoDN)
            Me.mCuestionarioResuelto = value
            DNaIU(value)
        End Set
    End Property

#End Region

#Region "Métodos"

#Region "Establecer y rellenar Datos"

    Protected Overrides Function IUaDN() As Boolean
        If Not ValidarTodo(Me.MensajeError, Nothing) Then
            Return False
        End If

        RellenarCuestionarioDesdePreguntas()

        If Me.mCuestionarioResuelto.EstadoIntegridad(Me.MensajeError) = Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente Then
            Return False
        End If

        Me.MensajeError = String.Empty
        Return True
    End Function

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        If TypeOf (pDN) Is CuestionarioDN Then
            Dim miCuestionario As CuestionarioDN = pDN

            'establecemos el texto de las preguntas personalizadas
            RellenarTextoCuestionario(miCuestionario)

        ElseIf TypeOf (pDN) Is CuestionarioResueltoDN Then
            Dim miCuestionarioResuelto As CuestionarioResueltoDN = pDN

            'establecemos el texto de las preguntas personalizadas
            RellenarTextoCuestionario(miCuestionarioResuelto.CuestionarioDN)

            'habilitamos todos los grp
            Me.grpAntecedentes.Enabled = True
            Me.grpBonificaciones.Enabled = True
            Me.grpCarnetConducir.Enabled = True
            Me.grpConductoresAdicionales.Enabled = True
            Me.grpDatosCliente.Enabled = True
            Me.grpDatosIniciales.Enabled = True
            Me.grpDatosVehiculo.Enabled = True

            'establecemos las respuestas de las preguntas
            RellenarCuestionarioDesdeCuestionarioRelleno(miCuestionarioResuelto)

        End If

    End Sub

    Private Sub RellenarTextoCuestionario(ByVal pCuestionario As CuestionarioDN)
        'cogemos cada una de las preguntas y las asignamos a cada uno de los controles
        Dim pregunta As PreguntaDN = Nothing

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("CodigoConcesionario")
        Me.lblConcesionario.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("CodigoVendedor")
        Me.lblVendedor.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("EsCliente")
        Me.lblEsCliente.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("IDCliente")
        Me.lblIDCliente.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("FechaEfecto")
        Me.lblFechaTarificacion.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("TarificacionPrueba")
        Me.lblTarificacionPrueba.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("Nombre")
        Me.lblNombre.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("Apellido1")
        Me.lblApellidos.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("Apellido2")

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("Sexo")
        Me.lblSexo.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("FechaNacimiento")
        Me.lblFechaNacimiento.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("Telefono")
        Me.lblTelefono.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("Fax")
        Me.lblFax.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("Email")
        Me.lblEmail.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("DireccionEnvio")
        Me.lblDireccionEnvio.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("ZONA")

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("Circulacion-Localidad")

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("Marca")
        Me.lblMarca.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("Modelo")
        Me.lblModelo.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("CYLD")
        Me.lblcCilindrada.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("EstaMatriculado")
        Me.lblEstaMatriculado.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("FechaMatriculacion")
        Me.lblFecha1Matricula.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("FechaFabricacion")
        Me.lblFechaFabricacion.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("TieneCarnet")
        Me.lblTieneCarne.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("FechaCarnet")
        Me.lblFechaCarne.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("TipoCarnet")
        Me.lblTipoCarne.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("EsUnicoConductor")
        Me.lblEsConductorUnico.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("MCND")
        Me.lblConductoresAdicionales.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("ConductoresAdicionalesConCarnet")
        Me.lblConductoresAdicTienenCarne.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("SiniestroResponsable3años")
        Me.lblSiniestrosResponsabilidad.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("SiniestroSinResponsabilidad3años")
        Me.lblSiniestrosSinResponsabilidad.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("RetiradaCarnet3años")
        Me.lblInfraccionRetirada.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("ConduccionEbrio3años")
        Me.lblConduccionEbrio.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("VehículoTransporteRemunerado")
        Me.lblTransporte.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("CanceladoSeguro3años")
        Me.lblSeguroCancelado.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("PermisoCirculacionEspañol")
        Me.lblPermisoCirculacion.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("TitularPermisoCirculación")
        Me.lblTitularPermisoCirculacion.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("AseguradoActualmente")
        Me.lblAseguradoVehiculo.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("VencimientoSeguroActual")
        Me.lblVencimientoSeguro.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("AñosSinSiniestro")
        Me.lblAñosSinSiniestro.Text = pregunta.TextoPregunta

        pregunta = pCuestionario.ColPreguntaDN.RecuperarPrimeroXNombre("Justificantes")
        Me.lblJustificantes.Text = pregunta.TextoPregunta

    End Sub

    Private Sub RellenarCuestionarioDesdePreguntas()
        Dim cr As CuestionarioResueltoDN
        If Me.mCuestionarioResuelto Is Nothing Then
            cr = New CuestionarioResueltoDN()
            cr.CuestionarioDN = mCuestionario
        Else
            cr = Me.mCuestionarioResuelto
        End If
        If cr.ColRespuestaDN Is Nothing Then
            cr.ColRespuestaDN = New ColRespuestaDN()
        End If

        If Me.mColCaracteristicas Is Nothing Then
            Me.mColCaracteristicas = New ColCaracteristicaDN()
            For Each caract As CaracteristicaDN In Me.mControlador.ObtenerCaracteristicas()
                Me.mColCaracteristicas.Add(caract)
            Next
        End If

        'recuperamos la característica y la respuesta correpondiente a cada pregunta 
        'para formar las preguntas
        responder(cr, "CodigoConcesionario", New ValorTextoCaracteristicaDN(), Me.txtConcesionario.Text, mFechaEfecto)
        responder(cr, "CodigoVendedor", New ValorTextoCaracteristicaDN(), Me.txtVendedor.Text, mFechaEfecto)
        responder(cr, "FechaEfecto", New ValorCaracteristicaFechaDN(), Me.dtpFechaTarificacion.Value, mFechaEfecto)
        responder(cr, "TarificacionPrueba", New ValorBooleanoCaracterisitcaDN(), Me.chkTarificacionPrueba.Checked, mFechaEfecto)
        responder(cr, "EsCliente", New ValorBooleanoCaracterisitcaDN(), Me.chkEsCliente.Checked,mFechaEfecto)
        responder(cr, "IDCliente", New ValorTextoCaracteristicaDN(), Me.lblIDClienteValor.Text, mFechaEfecto)
        responder(cr, "Nombre", New ValorTextoCaracteristicaDN(), Me.txtNombre.Text, mFechaEfecto)
        responder(cr, "Apellido1", New ValorTextoCaracteristicaDN(), Me.txtApellido1.Text, mFechaEfecto)
        responder(cr, "Apellido2", New ValorTextoCaracteristicaDN(), Me.txtApellido2.Text, mFechaEfecto)
        responder(cr, "Sexo", New FN.RiesgosVehiculos.DN.ValorSexoCaracteristicaDN(), Me.cboSexo.SelectedItem, mFechaEfecto)
        responder(cr, "FechaNacimiento", New ValorCaracteristicaFechaDN(), Me.dtpFechaNacimiento.Value, mFechaEfecto)
        responder(cr, "EDAD", New ValorNumericoCaracteristicaDN(), AnyosMesesDias.CalcularDirAMD(mFechaEfecto, Me.dtpFechaNacimiento.Value).Anyos, mFechaEfecto)
        responder(cr, "Telefono", New ValorTextoCaracteristicaDN(), Me.txtTelefono.Text, mFechaEfecto)
        responder(cr, "Fax", New ValorTextoCaracteristicaDN(), Me.txtTelefono.Text, mFechaEfecto)
        responder(cr, "Email", New ValorTextoCaracteristicaDN(), Me.txtEmail.Text, mFechaEfecto)
        responder(cr, "DireccionEnvio", New FN.RiesgosVehiculos.DN.ValorDireccionNoUnicaCaracteristicaDN(), Me.CtrlDireccionEnvio.DireccionNoUnica, mFechaEfecto)
        responder(cr, "ZONA", New ValorNumericoCaracteristicaDN(), Me.txtCPCondHabitual.Text, mFechaEfecto)
        responder(cr, "EsUnicoConductor", New ValorBooleanoCaracterisitcaDN(), Me.chkEsUnicoConductor.Checked, mFechaEfecto)
        responder(cr, "Circulacion-Localidad", New FN.RiesgosVehiculos.DN.ValorLocalidadCaracteristicaDN, Me.cboLocalidadCondHabitual.SelectedItem, mFechaEfecto)
        responder(cr, "Marca", New FN.RiesgosVehiculos.DN.ValorMarcaCaracterisitcaDN(), Me.cboMarca.SelectedItem, mFechaEfecto)
        responder(cr, "Modelo", New FN.RiesgosVehiculos.DN.ValorModeloCaracteristicaDN(), Me.cboModelo.SelectedItem, mFechaEfecto)
        responder(cr, "CYLD", New ValorNumericoCaracteristicaDN(), CInt(Me.txtCilindrada.Text), mFechaEfecto)
        responder(cr, "EstaMatriculado", New ValorBooleanoCaracterisitcaDN(), Me.chkEstaMatriculado.Checked, mFechaEfecto)
        responder(cr, "FechaMatriculacion", New ValorCaracteristicaFechaDN(), Me.dtpFecha1Matricula.Value, mFechaEfecto)
        responder(cr, "ANTG", New ValorNumericoCaracteristicaDN(), AnyosMesesDias.CalcularDirAMD(mFechaEfecto, Me.dtpFecha1Matricula.Value).Anyos, mFechaEfecto)
        responder(cr, "FechaFabricacion", New ValorCaracteristicaFechaDN(), Me.dtpFechaFabricacion.Value, mFechaEfecto)

        responder(cr, "TieneCarnet", New ValorBooleanoCaracterisitcaDN(), Me.chkTieneCarne.Checked, mFechaEfecto)
        If Me.chkTieneCarne.Checked Then
            responder(cr, "FechaCarnet", New ValorCaracteristicaFechaDN, Me.dtpFechaCarne.Value, mFechaEfecto)
            responder(cr, "TipoCarnet", New ValorNumericoCaracteristicaDN, Me.cboTipoCarne.SelectedItem, mFechaEfecto)
            responder(cr, "CARN", New ValorNumericoCaracteristicaDN(), AnyosMesesDias.CalcularDirAMD(mFechaEfecto, Me.dtpFechaCarne.Value).Anyos, mFechaEfecto)
        Else
            responder(cr, "CARN", New ValorNumericoCaracteristicaDN(), 0, mFechaEfecto)
        End If

        Dim valorMCND As Integer = 0
        If Me.ctrlMulticonductor1.ColDatosConductorAdicional IsNot Nothing AndAlso Me.ctrlMulticonductor1.ColDatosConductorAdicional.Count > 0 Then
            Dim fechaMCND As Date = Me.ctrlMulticonductor1.ColDatosConductorAdicional.FechaNacimientoMenor()
            valorMCND = AnyosMesesDias.CalcularDirAMD(mFechaEfecto, fechaMCND).Anyos
            responder(cr, "MCND", New ValorNumericoCaracteristicaDN, valorMCND, mFechaEfecto)
            responder(cr, "ConductoresAdicionalesConCarnet", New ValorBooleanoCaracterisitcaDN(), Me.chkConductoresAdicTienenCarne.Checked, mFechaEfecto)
            responder(cr, "ColConductoresAdicionales", New FN.RiesgosVehiculos.DN.ValorMCNDCaracteristicaDN(), Me.ctrlMulticonductor1.ColDatosConductorAdicional, mFechaEfecto)
        Else
            EliminarRespuestaCustionario(cr, "MCND")
            EliminarRespuestaCustionario(cr, "ConductoresAdicionalesConCarnet")
            EliminarRespuestaCustionario(cr, "ColConductoresAdicionales")
        End If

        responder(cr, "SiniestroResponsable3años", New ValorNumericoCaracteristicaDN(), CInt(Me.txtSiniestrosResponsabilidad.Text), mFechaEfecto)
        responder(cr, "SiniestroSinResponsabilidad3años", New ValorNumericoCaracteristicaDN(), CInt(Me.txtSiniestrosSinCulpa.Text), mFechaEfecto)
        responder(cr, "RetiradaCarnet3años", New ValorBooleanoCaracterisitcaDN(), Me.chkInfraccionRetirada.Checked, mFechaEfecto)
        responder(cr, "ConduccionEbrio3años", New ValorBooleanoCaracterisitcaDN(), Me.chkConduccionEbrio.Checked, mFechaEfecto)
        responder(cr, "VehículoTransporteRemunerado", New ValorBooleanoCaracterisitcaDN(), Me.chkTransporte.Checked, mFechaEfecto)
        responder(cr, "CanceladoSeguro3años", New ValorBooleanoCaracterisitcaDN(), Me.chkSeguroCancelado.Checked, mFechaEfecto)
        responder(cr, "PermisoCirculacionEspañol", New ValorBooleanoCaracterisitcaDN(), Me.chkTitularPermisoCirculacion.Checked, mFechaEfecto)
        responder(cr, "TitularPermisoCirculación", New ValorBooleanoCaracterisitcaDN(), Me.chkTitularPermisoCirculacion.Checked, mFechaEfecto)
        responder(cr, "AseguradoActualmente", New ValorBooleanoCaracterisitcaDN(), Me.chkAseguradoVehiculo.Checked, mFechaEfecto)
        responder(cr, "VencimientoSeguroActual", New ValorCaracteristicaFechaDN(), Me.dtpVencimientoSeguro.Value, mFechaEfecto)
        responder(cr, "AñosSinSiniestro", New ValorNumericoCaracteristicaDN, AñosSinSiniestro(), mFechaEfecto)
        Dim miJustificante As FN.RiesgosVehiculos.DN.Justificantes
        If Me.optJustNinguno.Checked Then
            miJustificante = FN.RiesgosVehiculos.DN.Justificantes.ninguno
        ElseIf Me.optJustCertif.Checked Then
            miJustificante = FN.RiesgosVehiculos.DN.Justificantes.certificado
        ElseIf Me.optJustCertifRecibo.Checked Then
            miJustificante = FN.RiesgosVehiculos.DN.Justificantes.certificado_y_recibo
        End If
        responder(cr, "Justificantes", New ValorNumericoCaracteristicaDN, miJustificante, mFechaEfecto)

        'If Me.mCuestionarioResuelto Is Nothing Then
        Me.mCuestionarioResuelto = cr
        'End If

    End Sub

    Private Function AñosSinSiniestro() As Integer
        Return AñosSinSiniestro(CStr(cboAñosSinSiniestro.SelectedItem))
    End Function

    Private Function AñosSinSiniestro(ByVal cadena As String) As Integer
        Dim res As Integer = 0
        If Not Integer.TryParse(cadena, res) Then
            res = 4
        End If
        Return res
    End Function

    ''' <summary>
    ''' Cambia o establece el valor para una respuesta a una característica, creando la respuesta
    ''' si ésta no existe
    ''' </summary>
    ''' <param name="CuestionarioResuelto">El cuestionario resuelto al que se quiere asignar el valor o la respuesta</param>
    ''' <param name="nombre">el nombre de la característica/pregunta</param>
    ''' <param name="valor">Un nuevo valorcaracterística del tipo apropiado</param>
    ''' <param name="valorAsignar">El valor que se quiere asignar a la respuesta</param>
    ''' <remarks></remarks>
    Public Sub responder(ByVal CuestionarioResuelto As CuestionarioResueltoDN, ByVal nombre As String, ByVal valor As IValorCaracteristicaDN, ByVal valorAsignar As Object, ByVal fechaEfecto As Date)
        Dim pregunta As PreguntaDN = Nothing
        Dim respuesta As RespuestaDN = Nothing
        Dim caracteristica As CaracteristicaDN = mColCaracteristicas.RecuperarPrimeroXNombre(nombre)

        If CuestionarioResuelto.ColRespuestaDN IsNot Nothing Then
            respuesta = CuestionarioResuelto.ColRespuestaDN.RecuperarxCaracteristica(caracteristica)
        End If

        If respuesta Is Nothing Then
            respuesta = New RespuestaDN()
            pregunta = CuestionarioResuelto.CuestionarioDN.ColPreguntaDN.RecuperarPrimeroXNombre(nombre)
            respuesta.PreguntaDN = pregunta
        End If

        If respuesta.IValorCaracteristicaDN Is Nothing Then
            respuesta.IValorCaracteristicaDN = valor
        End If

        respuesta.IValorCaracteristicaDN.Valor = valorAsignar
        respuesta.IValorCaracteristicaDN.Caracteristica = caracteristica
        respuesta.IValorCaracteristicaDN.FechaEfectoValor = fechaEfecto

        If caracteristica.Padre IsNot Nothing Then
            respuesta.IValorCaracteristicaDN.ValorCaracPadre = CuestionarioResuelto.ColRespuestaDN.RecuperarxCaracteristica(caracteristica.Padre).IValorCaracteristicaDN
        End If

        If Not CuestionarioResuelto.ColRespuestaDN.Contains(respuesta) Then
            CuestionarioResuelto.ColRespuestaDN.Add(respuesta)
        Else
            CuestionarioResuelto.ColRespuestaDN.Remove(respuesta)
            CuestionarioResuelto.ColRespuestaDN.Add(respuesta)
        End If

    End Sub

    Private Sub EliminarRespuestaCustionario(ByVal CuestionarioResuelto As CuestionarioResueltoDN, ByVal nombre As String)
        Dim pregunta As PreguntaDN = Nothing
        Dim respuesta As RespuestaDN = Nothing
        Dim caracteristica As CaracteristicaDN = mColCaracteristicas.RecuperarPrimeroXNombre(nombre)

        If CuestionarioResuelto.ColRespuestaDN IsNot Nothing Then
            respuesta = CuestionarioResuelto.ColRespuestaDN.RecuperarxCaracteristica(caracteristica)
        End If

        If respuesta IsNot Nothing Then
            CuestionarioResuelto.ColRespuestaDN.EliminarEntidadDN(respuesta, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
        End If

    End Sub

    Private Sub RellenarCuestionarioDesdeCuestionarioRelleno(ByVal cr As CuestionarioResueltoDN)
        'obtenemos el valor de cada respuesta y lo asignamos al control correspondiente
        For Each respuesta As RespuestaDN In cr.ColRespuestaDN
            If respuesta.IValorCaracteristicaDN IsNot Nothing AndAlso respuesta.IValorCaracteristicaDN.Valor IsNot Nothing Then
                Select Case respuesta.PreguntaDN.CaracteristicaDN.Nombre
                    Case "CodigoConcesionario"
                        Me.txtConcesionario.Text = respuesta.IValorCaracteristicaDN.Valor.ToString()
                    Case "CodigoVendedor"
                        Me.txtVendedor.Text = respuesta.IValorCaracteristicaDN.Valor.ToString()
                    Case "EsCliente"
                        Me.chkEsCliente.Checked = CBool(respuesta.IValorCaracteristicaDN.Valor)
                    Case "IDCliente"
                        Me.lblIDClienteValor.Text = respuesta.IValorCaracteristicaDN.Valor
                    Case "FechaEfecto"
                        Me.dtpFechaTarificacion.Value = respuesta.IValorCaracteristicaDN.Valor
                    Case "TarificacionPrueba"
                        Me.chkTarificacionPrueba.Checked = respuesta.IValorCaracteristicaDN.Valor
                    Case "Nombre"
                        Me.txtNombre.Text = respuesta.IValorCaracteristicaDN.Valor
                    Case "Apellido1"
                        Me.txtApellido1.Text = respuesta.IValorCaracteristicaDN.Valor
                    Case "Apellido2"
                        Me.txtApellido2.Text = respuesta.IValorCaracteristicaDN.Valor
                    Case "Sexo"
                        Dim ts As FN.Personas.DN.TipoSexo = respuesta.IValorCaracteristicaDN.Valor
                        For Each tipoS As FN.Personas.DN.TipoSexo In Me.cboSexo.Items
                            If tipoS.ID = ts.ID Then
                                Me.cboSexo.SelectedItem = tipoS
                                Exit For
                            End If
                        Next
                    Case "FechaNacimiento"
                        Me.dtpFechaNacimiento.Value = respuesta.IValorCaracteristicaDN.Valor
                    Case "Telefono"
                        Me.txtTelefono.Text = respuesta.IValorCaracteristicaDN.Valor
                    Case "Fax"
                        Me.txtTelefono.Text = respuesta.IValorCaracteristicaDN.Valor
                    Case "Email"
                        Me.txtEmail.Text = respuesta.IValorCaracteristicaDN.Valor
                    Case "DireccionEnvio"
                        Me.CtrlDireccionEnvio.DireccionNoUnica = respuesta.IValorCaracteristicaDN.Valor
                    Case "ZONA"
                        Me.txtCPCondHabitual.Text = respuesta.IValorCaracteristicaDN.Valor.ToString
                        CargarLocalidadxCP()
                    Case "Circulacion-Localidad"
                        Dim mil As FN.Localizaciones.DN.LocalidadDN = respuesta.IValorCaracteristicaDN.Valor
                        For Each localidad As FN.Localizaciones.DN.LocalidadDN In Me.cboLocalidadCondHabitual.Items
                            If localidad.ID = mil.ID Then
                                Me.cboLocalidadCondHabitual.SelectedItem = localidad
                                Exit For
                            End If
                        Next
                    Case "Marca"
                        Dim marca As FN.RiesgosVehiculos.DN.MarcaDN = respuesta.IValorCaracteristicaDN.Valor
                        For Each m As FN.RiesgosVehiculos.DN.MarcaDN In Me.cboMarca.Items
                            If m.ID = marca.ID Then
                                Me.cboMarca.SelectedItem = m
                                Exit For
                            End If
                        Next
                    Case "Modelo"
                        Dim modelo As FN.RiesgosVehiculos.DN.ModeloDN = respuesta.IValorCaracteristicaDN.Valor
                        For Each m As FN.RiesgosVehiculos.DN.ModeloDN In Me.cboModelo.Items
                            If m.ID = modelo.ID Then
                                Me.cboModelo.SelectedItem = m
                                Exit For
                            End If
                        Next
                    Case "CYLD"
                        Me.txtCilindrada.Text = respuesta.IValorCaracteristicaDN.Valor
                    Case "EstaMatriculado"
                        Me.chkEstaMatriculado.Checked = respuesta.IValorCaracteristicaDN.Valor
                    Case "FechaMatriculacion"
                        Me.dtpFecha1Matricula.Value = respuesta.IValorCaracteristicaDN.Valor
                    Case "FechaFabricacion"
                        Me.dtpFechaFabricacion.Value = respuesta.IValorCaracteristicaDN.Valor
                    Case "TieneCarnet"
                        Me.chkTieneCarne.Checked = respuesta.IValorCaracteristicaDN.Valor
                    Case "FechaCarnet"
                        Me.dtpFechaCarne.Value = respuesta.IValorCaracteristicaDN.Valor
                    Case "TipoCarnet"
                        For Each tipoC As FN.RiesgosVehiculos.DN.TipoCarnet In Me.cboTipoCarne.Items
                            If tipoC = respuesta.IValorCaracteristicaDN.Valor Then
                                Me.cboTipoCarne.SelectedItem = tipoC
                                Exit For
                            End If
                        Next
                    Case "MCND"
                        Me.ctrlMulticonductor1.ColDatosConductorAdicional = respuesta.IValorCaracteristicaDN.Valor
                        Me.cboNumeroConductoresAdic.SelectedItem = Me.ctrlMulticonductor1.NumeroConductores
                    Case "EsUnicoConductor"
                        Me.chkEsUnicoConductor.Checked = respuesta.IValorCaracteristicaDN.Valor
                    Case "ConductoresAdicionalesConCarnet"
                        Me.chkConductoresAdicTienenCarne.Checked = respuesta.IValorCaracteristicaDN.Valor
                    Case "SiniestroResponsable3años"
                        Me.txtSiniestrosResponsabilidad.Text = respuesta.IValorCaracteristicaDN.Valor
                    Case "SiniestroSinResponsabilidad3años"
                        Me.txtSiniestrosSinCulpa.Text = respuesta.IValorCaracteristicaDN.Valor
                    Case "RetiradaCarnet3años"
                        Me.chkInfraccionRetirada.Checked = respuesta.IValorCaracteristicaDN.Valor
                    Case "VehículoTransporteRemunerado"
                        Me.chkTransporte.Checked = respuesta.IValorCaracteristicaDN.Valor
                    Case "CanceladoSeguro3años"
                        Me.chkSeguroCancelado.Checked = respuesta.IValorCaracteristicaDN.Valor
                    Case "ConduccionEbrio3años"
                        Me.chkConduccionEbrio.Checked = respuesta.IValorCaracteristicaDN.Valor
                    Case "PermisoCirculacionEspañol"
                        Me.chkPermisoCirculacion.Checked = respuesta.IValorCaracteristicaDN.Valor
                    Case "TitularPermisoCirculación"
                        Me.chkTitularPermisoCirculacion.Checked = respuesta.IValorCaracteristicaDN.Valor
                    Case "AseguradoActualmente"
                        Me.chkAseguradoVehiculo.Checked = respuesta.IValorCaracteristicaDN.Valor
                    Case "VencimientoSeguroActual"
                        Me.dtpVencimientoSeguro.Value = respuesta.IValorCaracteristicaDN.Valor
                    Case "AñosSinSiniestro"
                        For Each res As String In Me.cboAñosSinSiniestro.Items
                            If AñosSinSiniestro(res) = respuesta.IValorCaracteristicaDN.Valor Then
                                Me.cboAñosSinSiniestro.SelectedItem = res
                                Exit For
                            End If
                        Next
                    Case "Justificantes"
                        Select Case CType(respuesta.IValorCaracteristicaDN.Valor, FN.RiesgosVehiculos.DN.Justificantes)
                            Case FN.RiesgosVehiculos.DN.Justificantes.ninguno
                                Me.optJustNinguno.Checked = True
                            Case FN.RiesgosVehiculos.DN.Justificantes.certificado
                                Me.optJustCertif.Checked = True
                            Case FN.RiesgosVehiculos.DN.Justificantes.certificado_y_recibo
                                Me.optJustCertifRecibo.Checked = True
                        End Select
                End Select
            End If
        Next
    End Sub

#End Region


#Region "Acciones de los Controles"
    Private Sub cboNumeroConductoresAdic_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles cboNumeroConductoresAdic.SelectedIndexChanged
        Try
            EstablecerNumeroConductoresAdicionales(Me.cboNumeroConductoresAdic.SelectedItem)
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

    Private Sub EstablecerNumeroConductoresAdicionales(ByVal pNumero As Integer)
        Me.ctrlMulticonductor1.NumeroConductores = pNumero
    End Sub

    Private Sub chkEsCliente_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkEsCliente.CheckedChanged
        Try
            Me.cmdBuscar.Enabled = Me.chkEsCliente.Checked
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdBuscar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdBuscar.Click
        Try
            Throw New NotImplementedException("Funcionalidad no implementada")

            Dim tomador As FN.Seguros.Polizas.DN.TomadorDN = BuscarTomador()
            If Not tomador Is Nothing Then
                'TODO: 777 - si es impago lo ponemos visible

                If TypeOf tomador.EntidadFiscalGenerica.IentidadFiscal Is FN.Empresas.DN.EmpresaFiscalDN Then
                    Throw New NotImplementedException("Actualmente no se soporta la selección de empresas como cliente")

                ElseIf TypeOf tomador.EntidadFiscalGenerica.IentidadFiscal Is FN.Personas.DN.PersonaFiscalDN Then
                    Dim PersonaFiscal As FN.Personas.DN.PersonaFiscalDN = tomador.EntidadFiscalGenerica.IentidadFiscal
                    Dim Persona As FN.Personas.DN.PersonaDN = PersonaFiscal.Persona

                    Me.txtNombre.Text = Persona.Nombre
                    Me.txtApellido1.Text = Persona.Apellido
                    Me.txtApellido2.Text = Persona.Apellido2
                    For Each sexo As FN.Personas.DN.TipoSexo In Me.cboSexo.Items
                        If sexo.ID = Persona.Sexo.ID Then
                            cboSexo.SelectedItem = sexo
                            Exit For
                        End If
                    Next
                    Me.dtpFechaNacimiento.Value = Persona.FechaNacimiento
                    Me.lblEdadCalc.Text = CInt(Now.Subtract(Persona.FechaNacimiento).TotalDays / 365).ToString

                    'TODO: 777 - recuperar el contacto desde la persona
                    Dim contacto As FN.Personas.DN.ContactoPersonaDN = Nothing

                    Dim colTelefonos As New ArrayList()
                    Dim colEmail As New ArrayList()
                    Dim colFax As New ArrayList()
                    Dim colDirecciones As New ArrayList()

                    For Each elemento As FN.Localizaciones.DN.IDatoContactoDN In contacto.Contacto.ColDatosContacto
                        Select Case elemento.Tipo
                            Case GetType(FN.Localizaciones.DN.TelefonoDN).ToString
                                colTelefonos.Add(elemento)
                            Case GetType(FN.Localizaciones.DN.EmailDN).ToString
                                colEmail.Add(elemento)
                                'Case GetType(FN.Localizaciones.DN.FaxDN).ToString
                                '    colFax.Add(elemento)
                            Case GetType(FN.Localizaciones.DN.DireccionNoUnicaDN).ToString
                                colDirecciones.Add(elemento)
                            Case Else
                                'no hacemos nada
                        End Select
                    Next

                    If colTelefonos.Count <> 0 Then
                        Me.mColTelefonos = New ArrayList()
                        Me.mColTelefonos.AddRange(colTelefonos.ToArray())
                        Me.txtTelefono.Text = CType(colTelefonos(0), FN.Localizaciones.DN.TelefonoDN).Nombre
                        Me.cmdMasTelefonos.Visible = colTelefonos.Count > 1
                    End If

                    If colEmail.Count <> 0 Then
                        Me.mColEmail = New ArrayList()
                        Me.mColEmail.AddRange(colEmail.ToArray())
                        Me.txtEmail.Text = CType(colEmail(0), FN.Localizaciones.DN.EmailDN).Nombre
                        Me.cmdMasEmail.Visible = colEmail.Count > 1
                    End If

                    If colFax.Count <> 0 Then
                        Me.mColFax = New ArrayList()
                        Me.mColFax.AddRange(colFax.ToArray())
                        'Me.txtFax.Text = CType(colEmail(0), FN.Localizaciones.DN.FaxDN).Nombre
                        Me.cmdMasFax.Visible = colFax.Count > 1
                    End If

                    If colDirecciones.Count <> 0 Then
                        Me.CtrlDireccionEnvio.DireccionNoUnica = colDirecciones(0)
                        Me.mColDirecciones = New ArrayList()
                        Me.mColDirecciones.AddRange(colDirecciones.ToArray())
                        Me.cmdMasdirecciones.Visible = colDirecciones.Count > 1
                    End If

                    'TODO: 777 - hay que buscar los datos de las pólizas que estén activas para rellenar los
                    'datos del panel de Bonificaciones

                    'TODO: 777 - buscar la siniestralidad del Tomador para rellenar automáticamente los datos
                    'de los Antecedentes


                End If
            End If
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

    Private Function BuscarTomador() As FN.Seguros.Polizas.DN.TomadorDN

        Dim miPaquete As New Hashtable()
        Dim miParametroCargaEst As New ParametroCargaEstructuraDN()

        'miParametroCargaEst.NombreVistaSel = "vwPlantillaCartaSel"
        'miParametroCargaEst.NombreVistaVis = "vwPlantillaCartaSel"
        miParametroCargaEst.TipodeEntidad = GetType(FN.Seguros.Polizas.DN.TomadorDN)


        'miParametroCargaEst.DestinoNavegacion = "PlantillaCartaModelo"

        'Dim lista As New List(Of String)
        'lista.Add("Tipo_Operacion")
        'lista.Add("Entidad_Negocio")
        'lista.Add("Estado_Documento")
        'miParametroCargaEst.CamposaCargarDatos = lista

        miPaquete.Add("ParametroCargaEstructura", miParametroCargaEst)

        Dim mipaqueteconf As New MotorBusquedaDN.PaqueteFormularioBusqueda
        mipaqueteconf.Agregable = False
        mipaqueteconf.EnviarDatatableAlNavegar = False
        mipaqueteconf.MultiSelect = False
        mipaqueteconf.TipoNavegacion = TipoNavegacion.Modal
        mipaqueteconf.Titulo = "Buscar Cliente"
        mipaqueteconf.Navegable = False
        mipaqueteconf.ParametroCargaEstructura = miParametroCargaEst

        miPaquete.Add("PaqueteFormularioBusqueda", mipaqueteconf)

        Me.Marco.Navegar("Filtro", Me, Me.FormularioPadre, TipoNavegacion.Normal, Me.GenerarDatosCarga, miPaquete)

        Dim tomador As FN.Seguros.Polizas.DN.TomadorDN = Nothing
        If miPaquete.Contains("ID") Then
            tomador = Me.mControlador.ObtenerTomador(miPaquete("ID"))
        End If

        Return tomador
    End Function

    Private Sub cboMarca_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles cboMarca.SelectedIndexChanged
        Try
            Me.cboModelo.Items.Clear()
            Dim marca As FN.RiesgosVehiculos.DN.MarcaDN = Me.cboMarca.SelectedItem
            If marca Is Nothing Then
                Me.cboModelo.Items.Clear()
                Me.cboModelo.Enabled = False
            Else
                Dim modelos As List(Of FN.RiesgosVehiculos.DN.ModeloDN) = Me.mControlador.ObtenerModelosPorMarca(marca)
                Me.cboModelo.Items.AddRange(modelos.ToArray())
                Me.cboModelo.Enabled = True
            End If
        Catch ex As Exception
            MostrarError(ex, "Seleccionar Modelos")
        End Try
    End Sub

    Private Sub cmdTerminarCuestionario_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmdTerminarCuestionario.Click
        Try
            Dim pcontrol As Control = Nothing
            Dim mensaje As String = String.Empty
            If Not ValidarTodo(mensaje, pcontrol) Then
                ErrorValidandoDatos(mensaje, pcontrol)
                Exit Sub
            End If

            RaiseEvent CuestionarioFinalizado()
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

    Private Sub txtCPCondHabitual_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtCPCondHabitual.LostFocus
        Try
            CargarLocalidadxCP()
        Catch ex As Exception
            MostrarError(ex, "Determinar Localidad por CP")
        End Try
    End Sub

    Private Sub chkEstaMatriculado_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles chkEstaMatriculado.CheckedChanged
        Try
            Me.dtpFecha1Matricula.Enabled = Me.chkEstaMatriculado.Checked
            Me.dtpFechaFabricacion.Enabled = Not Me.chkEstaMatriculado.Checked
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub chkAseguradoVehiculo_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkAseguradoVehiculo.CheckedChanged
        Try
            'Me.dtpVencimientoSeguro.Enabled = Me.chkAseguradoVehiculo.Checked
            'Me.cboAñosSinSiniestro.Enabled = Not Me.chkAseguradoVehiculo.Checked
            'Me.optJustCertif.Enabled = Not Me.chkAseguradoVehiculo.Checked
            'Me.optJustCertifRecibo.Enabled = Not Me.chkAseguradoVehiculo.Checked
            'Me.optJustNinguno.Enabled = Not Me.chkAseguradoVehiculo.Checked
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub chkTieneCarne_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles chkTieneCarne.CheckedChanged
        Try
            ComprobarEnabledCarnet()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub dtpFechaNacimiento_ValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles dtpFechaNacimiento.ValueChanged
        Try
            If mFechaEfecto >= Me.dtpFechaNacimiento.Value() Then
                Me.lblEdadCalc.Text = Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias.CalcularDirAMD(mFechaEfecto, Me.dtpFechaNacimiento.Value()).Anyos.ToString()
            Else
                Me.lblEdadCalc.Text = 0
            End If
        Catch ex As Exception
            MostrarError(ex, "ERROR: Calcular edad")
        End Try
    End Sub

    Private Sub dtpFecha1Matricula_ValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles dtpFecha1Matricula.ValueChanged
        Try
            If mFechaEfecto >= Me.dtpFecha1Matricula.Value() Then
                Me.lblAntgCalculada.Text = Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias.CalcularDirAMD(mFechaEfecto, Me.dtpFecha1Matricula.Value()).Anyos.ToString()
            Else
                Me.lblAntgCalculada.Text = 0
            End If
        Catch ex As Exception
            MostrarError(ex, "ERROR: Calcular antigüedad de la moto")
        End Try
    End Sub

    Private Sub dtpFechaCarne_ValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles dtpFechaCarne.ValueChanged
        Try
            If mFechaEfecto >= Me.dtpFechaCarne.Value() Then
                Me.lblAñosCarnetCalculados.Text = Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias.CalcularDirAMD(mFechaEfecto, Me.dtpFechaCarne.Value()).Anyos.ToString()
            Else
                Me.lblAñosCarnetCalculados.Text = 0
            End If
        Catch ex As Exception
            MostrarError(ex, "ERROR: Calcular años de carnet")
        End Try
    End Sub

#End Region


#Region "Validación de datos"
    Private Function ValidarTodo(ByRef pMensaje As String, ByRef pControl As Control) As Boolean

        If Not Me.ValidarDatosIniciales() Then
            Return False
        End If
        If Not Me.ValidarDatosDelCliente() Then
            Return False
        End If
        If Not Me.ValidarDatosDelVehiculo() Then
            Return False
        End If
        If Not Me.ValidarCarnetDeConducir() Then
            Return False
        End If
        If Not Me.ValidarConductoresAdicionales() Then
            Return False
        End If
        If Not Me.ValidarAntecedentes() Then
            Return False
        End If
        If Not Me.ValidarBonificaciones() Then
            Return False
        End If

        Return True

    End Function


    Private Function ValidarDatosIniciales() As Boolean
        Dim pMensaje As String
        Dim pControl As Control
        If Me.chkEsCliente.Checked AndAlso Me.lblIDClienteValor.Text = "-" Then
            pMensaje = "Se ha indicado que es un cliente de AMV pero no se han cargado sus datos"
            pControl = Me.chkEsCliente
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If
        pMensaje = String.Empty
        Return True
    End Function

    Private Function ValidarDatosDelCliente() As Boolean
        Dim pMensaje As String
        Dim pControl As Control
        If String.IsNullOrEmpty(Me.txtNombre.Text) Then
            pMensaje = "Debe indicarse el nombre del cliente"
            pControl = Me.txtNombre
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If String.IsNullOrEmpty(Me.txtApellido1.Text) Then
            pMensaje = "Debe indicarse el apellido del cliente"
            pControl = Me.txtApellido1
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If String.IsNullOrEmpty(Me.txtApellido2.Text) Then
            pMensaje = "Debe indicarse el apellido del cliente"
            pControl = Me.txtApellido2
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        'If String.IsNullOrEmpty(Me.txtNIF.Text) OrElse Not FN.Localizaciones.DN.NifDN.ValidaNif(Me.txtNIF.Text, pMensaje) Then
        '    If pMensaje = String.Empty Then
        '        pMensaje = "Debe indicarse el NIF del cliente"
        '    End If
        '    pControl = Me.txtNIF
        '    Return False
        'End If

        If cboSexo.SelectedItem Is Nothing OrElse CType(cboSexo.SelectedItem, FN.Personas.DN.TipoSexo).Nombre = "sin determinar" Then
            pMensaje = "Debe inidicarse el sexo del cliente"
            pControl = Me.cboSexo
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If Me.lblEdadCalc.Text = "-" OrElse Integer.Parse(Me.lblEdadCalc.Text) < 18 Then
            pMensaje = "El cliente no tiene más de 18 años"
            pControl = Me.dtpFechaNacimiento
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If String.IsNullOrEmpty(Me.txtTelefono.Text) Then
            pMensaje = "Debe inidicarse el el teléfono del cliente"
            pControl = Me.txtTelefono
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If Me.CtrlDireccionEnvio.DireccionNoUnica Is Nothing Then
            pMensaje = "Debe inidicarse la dirección de envío del cliente:" & Me.CtrlDireccionEnvio.MensajeError
            pControl = Me.CtrlDireccionEnvio
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        pMensaje = String.Empty
        Return True
    End Function

    Private Function ValidarDatosDelVehiculo() As Boolean
        Dim pMensaje As String
        Dim pControl As Control

        If String.IsNullOrEmpty(Me.txtCPCondHabitual.Text) Then
            pMensaje = "Debe inidicarse el Código Postal del lugar de conducción habitual"
            pControl = Me.txtCPCondHabitual
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If Me.cboLocalidadCondHabitual.SelectedItem Is Nothing Then
            pMensaje = "No se ha definido una localidad de conducción habitual del vehículo" & Chr(13) & Chr(10) & "Si no hay ninguna localidad disponible es posible que el Código Postal introducido no sea correcto"
            pControl = Me.cboLocalidadCondHabitual
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If Me.cboMarca.SelectedItem Is Nothing Then
            pMensaje = "No se ha determiando la marca del vehículo"
            pControl = Me.cboMarca
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If Me.cboModelo.SelectedItem Is Nothing Then
            pMensaje = "No se ha definido el modelo del vehículo"
            pControl = Me.cboModelo
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If Me.txtCilindrada.Text = "0" Then
            pMensaje = "No se ha definido la cilindrada del vehículo"
            pControl = Me.txtCilindrada
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        'obtenemos los requisitos a partir de los datos del modelo
        Dim modelo As FN.RiesgosVehiculos.DN.ModeloDN = Me.cboModelo.SelectedItem
        Dim cilindrada As Integer = Integer.Parse(Me.txtCilindrada.Text)

        Try
            Me.mControlador.RecuperarRequisitosTarificar(modelo, cilindrada, Me.mFechaEfecto, Me.chkEstaMatriculado.Checked, Me.mEdadAnosMinimo, Me.mAnosCarnetMinimo, Me.mAdmiteNoMatriculado, Me.mCarnetsAdmitidos)
        Catch ex As Exception
            If ex.Message <> "El vehículo debe estar matriculado para poder ser asegurado" Then
                ErrorValidandoDatos(ex.Message, Me.cboModelo)
                Return False
            End If
        End Try

        'comprobamos si puede estar no matriculado
        If Not Me.mAdmiteNoMatriculado AndAlso Not Me.chkEstaMatriculado.Checked Then
            pMensaje = "No se puede asegurar el vehículo indicado sin matricular"
            pControl = Me.chkEstaMatriculado
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        'comprobamos si los años mínimos del conductor son válidos
        If Integer.Parse(Me.lblEdadCalc.Text) < Me.mEdadAnosMinimo Then
            pMensaje = "No se puede asegurar el vehículo indicado con una edad inferior a " & Me.mEdadAnosMinimo.ToString & " años"
            pControl = Me.cboModelo
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If


        pMensaje = String.Empty
        Return True
    End Function

    Private Function ValidarCarnetDeConducir() As Boolean
        Dim pMensaje As String
        Dim pControl As Control
        'Dim obligatoriocarnet As Boolean = Me.chkEstaMatriculado.Checked AndAlso (Integer.Parse(Me.txtCilindrada.Text) >= 125)
        Dim obligatoriocarnet As Boolean '= (-1 = Me.mAnosCarnetMinimo)
        If Me.mAnosCarnetMinimo = -1 Then
            obligatoriocarnet = False
        Else
            obligatoriocarnet = True
        End If


        If Me.chkTieneCarne.Checked AndAlso Me.cboTipoCarne.SelectedItem Is Nothing Then
            pMensaje = "Debe indicarse el tipo de carné de conducir que posee el conductor"
            pControl = Me.cboTipoCarne
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        Dim añosCarnet As Integer = 0

        If obligatoriocarnet Then
            'es obligatorio que tenga carnet
            If Not Me.chkTieneCarne.Checked Then
                pMensaje = "No se puede asegurar el vehículo si el cliente no tiene carné de conducir"
                pControl = Me.chkTieneCarne
                ErrorValidandoDatos(pMensaje, pControl)
                Return False
            End If

            'comprobamos que el carnet sea alguno de la lista de los válidos
            'para el riesgo/categoría
            Dim correcto As Boolean
            For Each carnet As FN.RiesgosVehiculos.DN.TipoCarnet In Me.mCarnetsAdmitidos
                If CType(Me.cboTipoCarne.SelectedItem, FN.RiesgosVehiculos.DN.TipoCarnet) = carnet Then
                    correcto = True
                    Exit For
                End If
            Next
            If Not correcto Then
                pMensaje = "El riesgo no puede ser asegurado con el carnet de conducir que se ha seleccionado"
                pControl = Me.cboTipoCarne
                ErrorValidandoDatos(pMensaje, pControl)
                Return False
            End If

            'comprobamos los años de carnet que tiene

            'comprobamos si hay una restricción por ccc + carnet de conducir
            Dim restr As Integer = Me.mControlador.AñosCarnetMinimoXCCCyCarnet(Me.cboTipoCarne.SelectedItem)
            If restr <> -1 AndAlso restr > Me.mAnosCarnetMinimo Then
                Me.mAnosCarnetMinimo = restr
            End If

            If Integer.TryParse(Me.lblAñosCarnetCalculados.Text, añosCarnet) Then
                If añosCarnet < Me.mAnosCarnetMinimo Then
                    pMensaje = "No se alcanzan los años mínimos de posesión del carnet de conducir (" & Me.mAnosCarnetMinimo.ToString() & ") para poder asegurar este riesgo"
                    pControl = Me.dtpFechaCarne
                    ErrorValidandoDatos(pMensaje, pControl)
                    Return False
                End If
            End If
        End If


        If Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias.CalcularDirAMD(Me.dtpFechaCarne.Value, Me.dtpFechaNacimiento.Value).Anyos < 18 Then
            If cboTipoCarne.SelectedItem <> FN.RiesgosVehiculos.DN.TipoCarnet.A1 Then
                pMensaje = "La fecha de carné no es consistente con la fecha de nacimiento del asegurado"
                pControl = Me.cboTipoCarne
                ErrorValidandoDatos(pMensaje, pControl)
                Return False
            End If
        End If

        'comprobamos si admite o no multiconductor
        If Not Me.mControlador.AdmiteMulticonductor(Me.cboModelo.SelectedItem, Integer.Parse(Me.txtCilindrada.Text), Me.mFechaEfecto, Integer.Parse(Me.lblAñosCarnetCalculados.Text), añosCarnet, Me.chkEstaMatriculado.Checked, Me.cboTipoCarne.SelectedItem) Then
            If Not Me.chkEsUnicoConductor.Checked Then
                pMensaje = "No puede asegurarse a más de un conductor para este riesgo con los datos que se han proporcionado"
                pControl = Nothing
                ErrorValidandoDatos(pMensaje, pControl)
                If MessageBox.Show("¿Desea que se rellene el cuestionario para un sólo conductor?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                    Me.chkEstaMatriculado.Checked = False
                Else
                    Return False
                End If
            End If
        End If

        pMensaje = String.Empty
        Return True
    End Function

    Private Function ValidarConductoresAdicionales() As Boolean
        Dim pMensaje As String
        Dim pControl As Control
        If CInt(Me.cboNumeroConductoresAdic.SelectedItem) <> 0 Then
            If Me.ctrlMulticonductor1.ColDatosConductorAdicional Is Nothing Then
                pMensaje = "Se deben asignar los datos de los conductores adicionales: " & Chr(13) & Chr(10) & Me.ctrlMulticonductor1.MensajeError
                pControl = Me.ctrlMulticonductor1
                ErrorValidandoDatos(pMensaje, pControl)
                Return False
            End If

            If Not Me.chkConductoresAdicTienenCarne.Checked Then
                pMensaje = "Los conductores matriculados deben tener el carné de conducir requerido para el vehículo que se quiere asegurar"
                pControl = Me.chkConductoresAdicTienenCarne
                ErrorValidandoDatos(pMensaje, pControl)
                Return False
            End If
        End If

        pMensaje = String.Empty
        Return True
    End Function

    Private Function ValidarAntecedentes() As Boolean
        Dim pMensaje As String
        Dim pControl As Control
        If Integer.Parse(Me.txtSiniestrosResponsabilidad.Text) <> 0 Then
            pMensaje = "No se puede asegurar a un conductor que haya tenido algún siniestro con responsabilidad en los últimos 3 años"
            pControl = Me.txtSiniestrosResponsabilidad
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If Integer.Parse(Me.txtSiniestrosSinCulpa.Text) > 2 Then
            pMensaje = "No se puede asegurar a un conductor que haya tenido más de dos siniestros sin responsabilidad en los últimos 3 años"
            pControl = Me.txtSiniestrosSinCulpa
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If Me.chkInfraccionRetirada.Checked Then
            pMensaje = "No se puede asegurar a un conductor al que se le ha retirado el carné por una infracción grave"
            pControl = Me.chkInfraccionRetirada
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If Me.chkTransporte.Checked Then
            pMensaje = "No se puede asegurar un vehículo que se utiliza para el transporte remunerado de personas o mercancías"
            pControl = Me.chkTransporte
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If Me.chkConduccionEbrio.Checked Then
            pMensaje = "No se puede asegurar a un conductor que ha cometido una infracción por conducir en estado ebrio en los últimos 3 años"
            pControl = Me.chkSeguroCancelado
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If Me.chkSeguroCancelado.Checked Then
            pMensaje = "No se puede asegurar a un conductor al que se le ha cancelado un seguro en los últimos 3 años"
            pControl = Me.chkSeguroCancelado
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If Not Me.chkPermisoCirculacion.Checked Then
            pMensaje = "No se puede asegurar a un conductor que no posea un permiso de circulación español"
            pControl = Me.chkPermisoCirculacion
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        If Not Me.chkTitularPermisoCirculacion.Checked Then
            pMensaje = "No se puede asegurar el vehículo si el titular del mismo no es el cliente o un pariente de primer grado"
            pControl = Me.chkTitularPermisoCirculacion
            ErrorValidandoDatos(pMensaje, pControl)
            Return False
        End If

        pMensaje = String.Empty
        Return True
    End Function

    Private Function ValidarBonificaciones() As Boolean
        Dim pMensaje As String
        Dim pControl As Control
        '777 - comprobar si aquí hay que verificar integridad de datos
        'ErrorValidandoDatos(pMensaje, pControl)
        pMensaje = String.Empty
        Return True
    End Function

#End Region

#Region "Métodos"

    Private Sub ComprobarEnabledCarnet()
        Me.dtpFechaCarne.Enabled = Me.chkTieneCarne.Checked
        Me.cboTipoCarne.Enabled = Me.chkTieneCarne.Checked
    End Sub

    Private Sub ErrorValidandoDatos(ByVal pMensaje As String, ByVal pControl As Control)
        pControl.Visible = True
        pControl.Focus()
        Me.Marco.MostrarAdvertencia(pMensaje, "Error de validación")
    End Sub

    Private Sub CargarLocalidadxCP()
        If Not String.IsNullOrEmpty(Me.txtCPCondHabitual.Text) Then
            'formatear a 5 dígitos el cp completando con 0 por la izq
            While Me.txtCPCondHabitual.Text.Length < 5
                Me.txtCPCondHabitual.Text = "0" & Me.txtCPCondHabitual.Text
            End While

            'obtener las localidades que le corresponden
            Me.mColLocalidadesFiltradasporCP = Me.mControlador.ObtenerLocalidadPorCodigoPostal(Me.txtCPCondHabitual.Text.Trim())

            cboLocalidadCondHabitual.Enabled = True

            Me.cboLocalidadCondHabitual.Items.Clear()
            Me.cboLocalidadCondHabitual.Items.AddRange(Me.mColLocalidadesFiltradasporCP.ToArray())
            'seleccionamos el 1er elemento por defecto
            If Me.cboLocalidadCondHabitual.Items.Count <> 0 Then
                Me.cboLocalidadCondHabitual.SelectedItem = Me.cboLocalidadCondHabitual.Items(0)
            End If
        Else
            Me.cboLocalidadCondHabitual.Items.Clear()
            Me.cboLocalidadCondHabitual.Items.AddRange(Me.mColLocalidadesTodas.ToArray())
        End If
    End Sub
#End Region

#End Region
    Private mPanelActual As Control

    Private Sub MostrarPanel(ByVal panel As Control)
        OcultarPaneles()
        panel.Visible = True
        Select Case panel.Name
            Case "grpAntecedentes"
                Me.grpAntecedentes.Visible = True
            Case "grpBonificaciones"
                Me.grpBonificaciones.Visible = True
            Case "grpCarnetConducir"
                Me.grpCarnetConducir.Visible = True
            Case "grpConductoresAdicionales"
                Me.grpConductoresAdicionales.Visible = True
            Case "grpDatosCliente"
                Me.grpDatosCliente.Visible = True
            Case "grpDatosIniciales"
                Me.grpDatosIniciales.Visible = True
            Case "grpDatosVehiculo"
                Me.grpDatosVehiculo.Visible = True
        End Select
        Me.mPanelActual = panel
    End Sub

    Private Sub OcultarPaneles()
        Me.grpAntecedentes.Visible = False
        Me.grpBonificaciones.Visible = False
        Me.grpCarnetConducir.Visible = False
        Me.grpConductoresAdicionales.Visible = False
        Me.grpDatosCliente.Visible = False
        Me.grpDatosIniciales.Visible = False
        Me.grpDatosVehiculo.Visible = False
    End Sub


    Private Sub ctrlMulticonductor1_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles ctrlMulticonductor1.Resize
        Try
            If Me.ctrlMulticonductor1.Bottom > 308 Then
                Me.grpConductoresAdicionales.Height = Me.ctrlMulticonductor1.Bottom + 112
            Else
                Me.grpConductoresAdicionales.Height = 371
            End If
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

    Private Sub grpConductoresAdicionales_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles grpConductoresAdicionales.Resize
        Try
            ResizeconductoresAdicionales()
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

    Private Sub ResizeconductoresAdicionales()
        Me.Height = Me.grpConductoresAdicionales.Bottom + 69
    End Sub

    Private Sub cmdSiguiente_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdSiguiente.Click
        Try
            Dim continuar As Boolean
            Select Case Me.mPanelActual.Name
                Case "grpAntecedentes"
                    If Me.ValidarAntecedentes() Then
                        continuar = True
                        MostrarPanel(Me.grpBonificaciones)
                    End If
                Case "grpBonificaciones"
                    If Me.ValidarBonificaciones() Then
                        continuar = True
                        Me.cmdTerminarCuestionario.Enabled = True
                        Me.cmdSiguiente.Enabled = False
                    End If
                Case "grpCarnetConducir"
                    If Me.ValidarCarnetDeConducir() Then
                        continuar = True
                        If Me.chkEsUnicoConductor.Checked Then
                            'es el único conductor, así que no hay que mostrar el de datos multiconductor
                            MostrarPanel(Me.grpAntecedentes)
                        Else
                            'no es el único conductor, mostramos el panel de multiconductor
                            MostrarPanel(Me.grpConductoresAdicionales)
                        End If
                    End If
                Case "grpConductoresAdicionales"
                    If Me.ValidarConductoresAdicionales() Then
                        continuar = True
                        MostrarPanel(Me.grpAntecedentes)
                    End If
                Case "grpDatosCliente"
                    If Me.ValidarDatosDelCliente() Then
                        continuar = True
                        MostrarPanel(Me.grpDatosVehiculo)
                    End If
                Case "grpDatosIniciales"
                    If Me.ValidarDatosIniciales() Then
                        continuar = True
                        MostrarPanel(Me.grpDatosCliente)
                        Me.cmdAnterior.Enabled = True
                    End If
                Case "grpDatosVehiculo"
                    If Me.ValidarDatosDelVehiculo() Then
                        continuar = True
                        MostrarPanel(Me.grpCarnetConducir)
                    End If
            End Select
            If continuar Then
                Me.SelectNextControl(Me.cmdSiguiente, True, True, True, True)
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdAnterior_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAnterior.Click
        Try
            Dim grSeleccionado As Control = Nothing
            Select Case Me.mPanelActual.Name
                Case "grpAntecedentes"
                    'If Me.ValidarAntecedentes() Then
                    If Me.chkEsUnicoConductor.Checked Then
                        'es el último conductor, no hay que mostra conductores adicionales
                        grSeleccionado = Me.grpCarnetConducir
                        'MostrarPanel(Me.grpCarnetConducir)
                    Else
                        'no es el único conductor, hay que mostrar el de conductores adicionales
                        grSeleccionado = Me.grpConductoresAdicionales
                        'MostrarPanel(Me.grpConductoresAdicionales)
                    End If

                    'End If
                Case "grpBonificaciones"
                    'If Me.ValidarBonificaciones() Then
                    Me.cmdTerminarCuestionario.Enabled = False
                    Me.cmdSiguiente.Enabled = True
                    grSeleccionado = Me.grpAntecedentes
                    'MostrarPanel(Me.grpAntecedentes)
                    'End If
                Case "grpCarnetConducir"
                    'If Me.ValidarCarnetDeConducir() Then
                    grSeleccionado = Me.grpDatosVehiculo
                    'MostrarPanel(Me.grpDatosVehiculo)
                    'End If
                Case "grpConductoresAdicionales"
                    'If Me.ValidarConductoresAdicionales() Then
                    grSeleccionado = Me.grpCarnetConducir
                    'MostrarPanel(Me.grpCarnetConducir)
                    'End If
                Case "grpDatosCliente"
                    'If Me.ValidarDatosDelCliente() Then
                    grSeleccionado = Me.grpDatosIniciales
                    'MostrarPanel(Me.grpDatosIniciales)
                    Me.cmdAnterior.Enabled = False
                    'End If
                Case "grpDatosIniciales"
                    'If Me.ValidarDatosIniciales() Then
                    '    MostrarPanel(Me.grpDatosIniciales)
                    '    Me.cmdAnterior.Enabled = False
                    'End If
                Case "grpDatosVehiculo"
                    'If Me.ValidarDatosDelVehiculo() Then
                    grSeleccionado = Me.grpDatosCliente
                    'MostrarPanel(Me.grpDatosCliente)
                    'End If
            End Select
            If Not grSeleccionado Is Nothing Then
                MostrarPanel(grSeleccionado)
                Me.SelectNextControl(grSeleccionado, True, True, True, True)
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub dtpFechaTarificacion_ValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles dtpFechaTarificacion.ValueChanged
        Try
            mFechaEfecto = dtpFechaTarificacion.Value
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub
End Class

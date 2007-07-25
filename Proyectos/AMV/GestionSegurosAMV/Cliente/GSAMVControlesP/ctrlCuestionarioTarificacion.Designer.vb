<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ctrlCuestionarioTarificacion
    Inherits MotorIU.ControlesP.BaseControlP

    'UserControl1 overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.cmdAnterior = New ControlesPBase.BotonP
        Me.cmdSiguiente = New ControlesPBase.BotonP
        Me.cmdTerminarCuestionario = New ControlesPBase.BotonP
        Me.grpConductoresAdicionales = New System.Windows.Forms.GroupBox
        Me.Label29 = New System.Windows.Forms.Label
        Me.chkConductoresAdicTienenCarne = New ControlesPBase.CheckBoxP
        Me.lblConductoresAdicTienenCarne = New System.Windows.Forms.Label
        Me.cboNumeroConductoresAdic = New System.Windows.Forms.ComboBox
        Me.lblNumeroConductoresAdic = New System.Windows.Forms.Label
        Me.lblConductoresAdicionales = New System.Windows.Forms.Label
        Me.ctrlMulticonductor1 = New GSAMVControlesP.ctrlMulticonductor
        Me.grpAntecedentes = New System.Windows.Forms.GroupBox
        Me.lblConduccionEbrio = New System.Windows.Forms.Label
        Me.txtSiniestrosSinCulpa = New ControlesPBase.txtValidable
        Me.txtSiniestrosResponsabilidad = New ControlesPBase.txtValidable
        Me.chkTitularPermisoCirculacion = New ControlesPBase.CheckBoxP
        Me.lblTitularPermisoCirculacion = New System.Windows.Forms.Label
        Me.chkPermisoCirculacion = New ControlesPBase.CheckBoxP
        Me.lblPermisoCirculacion = New System.Windows.Forms.Label
        Me.chkSeguroCancelado = New ControlesPBase.CheckBoxP
        Me.lblSeguroCancelado = New System.Windows.Forms.Label
        Me.chkTransporte = New ControlesPBase.CheckBoxP
        Me.lblTransporte = New System.Windows.Forms.Label
        Me.chkConduccionEbrio = New ControlesPBase.CheckBoxP
        Me.chkInfraccionRetirada = New ControlesPBase.CheckBoxP
        Me.lblInfraccionRetirada = New System.Windows.Forms.Label
        Me.lblSiniestrosSinResponsabilidad = New System.Windows.Forms.Label
        Me.lblSiniestrosResponsabilidad = New System.Windows.Forms.Label
        Me.grpBonificaciones = New System.Windows.Forms.GroupBox
        Me.cboAñosSinSiniestro = New System.Windows.Forms.ComboBox
        Me.optJustCertifRecibo = New System.Windows.Forms.RadioButton
        Me.optJustCertif = New System.Windows.Forms.RadioButton
        Me.optJustNinguno = New System.Windows.Forms.RadioButton
        Me.lblJustificantes = New System.Windows.Forms.Label
        Me.lblAñosSinSiniestro = New System.Windows.Forms.Label
        Me.dtpVencimientoSeguro = New System.Windows.Forms.DateTimePicker
        Me.lblVencimientoSeguro = New System.Windows.Forms.Label
        Me.chkAseguradoVehiculo = New ControlesPBase.CheckBoxP
        Me.lblAseguradoVehiculo = New System.Windows.Forms.Label
        Me.grpCarnetConducir = New System.Windows.Forms.GroupBox
        Me.lblAñosCarnetCalculados = New System.Windows.Forms.Label
        Me.lblAnyosCarnet = New System.Windows.Forms.Label
        Me.cboTipoCarne = New ControlesPBase.CboValidador
        Me.lblTipoCarne = New System.Windows.Forms.Label
        Me.dtpFechaCarne = New System.Windows.Forms.DateTimePicker
        Me.lblFechaCarne = New System.Windows.Forms.Label
        Me.chkTieneCarne = New ControlesPBase.CheckBoxP
        Me.lblTieneCarne = New System.Windows.Forms.Label
        Me.grpDatosVehiculo = New System.Windows.Forms.GroupBox
        Me.lblAntgCalculada = New System.Windows.Forms.Label
        Me.lblAntiguedad = New System.Windows.Forms.Label
        Me.txtCilindrada = New ControlesPBase.txtValidable
        Me.cboModelo = New ControlesPBase.CboValidador
        Me.cboMarca = New ControlesPBase.CboValidador
        Me.Panel1 = New System.Windows.Forms.Panel
        Me.cboLocalidadCondHabitual = New ControlesPBase.CboValidador
        Me.txtCPCondHabitual = New ControlesPBase.txtValidable
        Me.lblCPConduccionHabitual = New System.Windows.Forms.Label
        Me.lblLocalidadCondHabitual = New System.Windows.Forms.Label
        Me.lblModelo = New System.Windows.Forms.Label
        Me.dtpFecha1Matricula = New System.Windows.Forms.DateTimePicker
        Me.lblFecha1Matricula = New System.Windows.Forms.Label
        Me.dtpFechaFabricacion = New System.Windows.Forms.DateTimePicker
        Me.lblFechaFabricacion = New System.Windows.Forms.Label
        Me.chkEstaMatriculado = New ControlesPBase.CheckBoxP
        Me.lblEstaMatriculado = New System.Windows.Forms.Label
        Me.lblcCilindrada = New System.Windows.Forms.Label
        Me.lblMarca = New System.Windows.Forms.Label
        Me.lblCirculacionHabitual = New System.Windows.Forms.Label
        Me.grpDatosIniciales = New System.Windows.Forms.GroupBox
        Me.lblTarificacionPrueba = New System.Windows.Forms.Label
        Me.lblFechaTarificacion = New System.Windows.Forms.Label
        Me.dtpFechaTarificacion = New System.Windows.Forms.DateTimePicker
        Me.chkTarificacionPrueba = New ControlesPBase.CheckBoxP
        Me.lblIDClienteValor = New System.Windows.Forms.Label
        Me.lblIDCliente = New System.Windows.Forms.Label
        Me.cmdBuscar = New ControlesPBase.BotonP
        Me.txtVendedor = New ControlesPBase.txtValidable
        Me.txtConcesionario = New ControlesPBase.txtValidable
        Me.lblEsCliente = New System.Windows.Forms.Label
        Me.chkEsCliente = New ControlesPBase.CheckBoxP
        Me.lblConcesionario = New System.Windows.Forms.Label
        Me.lblVendedor = New System.Windows.Forms.Label
        Me.grpDatosCliente = New System.Windows.Forms.GroupBox
        Me.chkEsUnicoConductor = New ControlesPBase.CheckBoxP
        Me.lblEsConductorUnico = New System.Windows.Forms.Label
        Me.cmdMasdirecciones = New ControlesPBase.BotonP
        Me.cmdMasEmail = New ControlesPBase.BotonP
        Me.cmdMasFax = New ControlesPBase.BotonP
        Me.cmdMasTelefonos = New ControlesPBase.BotonP
        Me.lblEdadCalc = New System.Windows.Forms.Label
        Me.lblEdad = New System.Windows.Forms.Label
        Me.txtEmail = New ControlesPBase.txtValidable
        Me.txtFax = New ControlesPBase.txtValidable
        Me.txtTelefono = New ControlesPBase.txtValidable
        Me.cboSexo = New ControlesPBase.CboValidador
        Me.txtApellido2 = New ControlesPBase.txtValidable
        Me.txtApellido1 = New ControlesPBase.txtValidable
        Me.txtNombre = New ControlesPBase.txtValidable
        Me.CtrlDireccionEnvio = New GSAMVControlesP.ctrlDireccionNoUnica
        Me.lblDireccionEnvio = New System.Windows.Forms.Label
        Me.lblSexo = New System.Windows.Forms.Label
        Me.lblEmail = New System.Windows.Forms.Label
        Me.lblFax = New System.Windows.Forms.Label
        Me.lblTelefono = New System.Windows.Forms.Label
        Me.lblFechaNacimiento = New System.Windows.Forms.Label
        Me.dtpFechaNacimiento = New System.Windows.Forms.DateTimePicker
        Me.lblApellidos = New System.Windows.Forms.Label
        Me.lblNombre = New System.Windows.Forms.Label
        Me.grpConductoresAdicionales.SuspendLayout()
        Me.grpAntecedentes.SuspendLayout()
        Me.grpBonificaciones.SuspendLayout()
        Me.grpCarnetConducir.SuspendLayout()
        Me.grpDatosVehiculo.SuspendLayout()
        Me.Panel1.SuspendLayout()
        Me.grpDatosIniciales.SuspendLayout()
        Me.grpDatosCliente.SuspendLayout()
        Me.SuspendLayout()
        '
        'cmdAnterior
        '
        Me.cmdAnterior.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdAnterior.Image = Global.GSAMVControlesP.My.Resources.Resources._1leftarrow
        Me.cmdAnterior.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdAnterior.Location = New System.Drawing.Point(305, 402)
        Me.cmdAnterior.Name = "cmdAnterior"
        Me.cmdAnterior.Size = New System.Drawing.Size(90, 25)
        Me.cmdAnterior.TabIndex = 7
        Me.cmdAnterior.Text = "Anterior"
        Me.cmdAnterior.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.cmdAnterior.UseVisualStyleBackColor = True
        '
        'cmdSiguiente
        '
        Me.cmdSiguiente.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdSiguiente.Image = Global.GSAMVControlesP.My.Resources.Resources._1rightarrow
        Me.cmdSiguiente.ImageAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.cmdSiguiente.Location = New System.Drawing.Point(414, 402)
        Me.cmdSiguiente.Name = "cmdSiguiente"
        Me.cmdSiguiente.Size = New System.Drawing.Size(90, 25)
        Me.cmdSiguiente.TabIndex = 8
        Me.cmdSiguiente.Text = "Siguiente"
        Me.cmdSiguiente.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdSiguiente.UseVisualStyleBackColor = True
        '
        'cmdTerminarCuestionario
        '
        Me.cmdTerminarCuestionario.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmdTerminarCuestionario.Enabled = False
        Me.cmdTerminarCuestionario.Image = Global.GSAMVControlesP.My.Resources.Resources._2rightarrow
        Me.cmdTerminarCuestionario.ImageAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.cmdTerminarCuestionario.Location = New System.Drawing.Point(523, 402)
        Me.cmdTerminarCuestionario.Name = "cmdTerminarCuestionario"
        Me.cmdTerminarCuestionario.OcultarEnSalida = True
        Me.cmdTerminarCuestionario.Size = New System.Drawing.Size(148, 25)
        Me.cmdTerminarCuestionario.TabIndex = 9
        Me.cmdTerminarCuestionario.Text = "Terminar Cuestionario"
        Me.cmdTerminarCuestionario.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdTerminarCuestionario.UseVisualStyleBackColor = True
        '
        'grpConductoresAdicionales
        '
        Me.grpConductoresAdicionales.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grpConductoresAdicionales.Controls.Add(Me.Label29)
        Me.grpConductoresAdicionales.Controls.Add(Me.chkConductoresAdicTienenCarne)
        Me.grpConductoresAdicionales.Controls.Add(Me.lblConductoresAdicTienenCarne)
        Me.grpConductoresAdicionales.Controls.Add(Me.cboNumeroConductoresAdic)
        Me.grpConductoresAdicionales.Controls.Add(Me.lblNumeroConductoresAdic)
        Me.grpConductoresAdicionales.Controls.Add(Me.lblConductoresAdicionales)
        Me.grpConductoresAdicionales.Controls.Add(Me.ctrlMulticonductor1)
        Me.grpConductoresAdicionales.Location = New System.Drawing.Point(3, 12)
        Me.grpConductoresAdicionales.Name = "grpConductoresAdicionales"
        Me.grpConductoresAdicionales.Size = New System.Drawing.Size(673, 371)
        Me.grpConductoresAdicionales.TabIndex = 4
        Me.grpConductoresAdicionales.TabStop = False
        Me.grpConductoresAdicionales.Text = "Conductores Adicionales"
        '
        'Label29
        '
        Me.Label29.AutoSize = True
        Me.Label29.Location = New System.Drawing.Point(33, 192)
        Me.Label29.Name = "Label29"
        Me.Label29.Size = New System.Drawing.Size(0, 13)
        Me.Label29.TabIndex = 7
        '
        'chkConductoresAdicTienenCarne
        '
        Me.chkConductoresAdicTienenCarne.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.chkConductoresAdicTienenCarne.AutoSize = True
        Me.chkConductoresAdicTienenCarne.ColorBaseIluminacion = System.Drawing.Color.Orange
        Me.chkConductoresAdicTienenCarne.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(165, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.chkConductoresAdicTienenCarne.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.chkConductoresAdicTienenCarne.IluminarSeleccion = False
        Me.chkConductoresAdicTienenCarne.Location = New System.Drawing.Point(368, 327)
        Me.chkConductoresAdicTienenCarne.Name = "chkConductoresAdicTienenCarne"
        Me.chkConductoresAdicTienenCarne.Size = New System.Drawing.Size(12, 11)
        Me.chkConductoresAdicTienenCarne.TabIndex = 4
        Me.chkConductoresAdicTienenCarne.UseVisualStyleBackColor = True
        '
        'lblConductoresAdicTienenCarne
        '
        Me.lblConductoresAdicTienenCarne.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lblConductoresAdicTienenCarne.Location = New System.Drawing.Point(33, 308)
        Me.lblConductoresAdicTienenCarne.Name = "lblConductoresAdicTienenCarne"
        Me.lblConductoresAdicTienenCarne.Size = New System.Drawing.Size(318, 45)
        Me.lblConductoresAdicTienenCarne.TabIndex = 5
        Me.lblConductoresAdicTienenCarne.Text = "¿Los conductores adicionales son titulares del carné B desde hace más de 3 años o" & _
            " del carné A o A1?"
        Me.lblConductoresAdicTienenCarne.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'cboNumeroConductoresAdic
        '
        Me.cboNumeroConductoresAdic.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboNumeroConductoresAdic.FormattingEnabled = True
        Me.cboNumeroConductoresAdic.Location = New System.Drawing.Point(258, 65)
        Me.cboNumeroConductoresAdic.Name = "cboNumeroConductoresAdic"
        Me.cboNumeroConductoresAdic.Size = New System.Drawing.Size(54, 21)
        Me.cboNumeroConductoresAdic.TabIndex = 0
        '
        'lblNumeroConductoresAdic
        '
        Me.lblNumeroConductoresAdic.AutoSize = True
        Me.lblNumeroConductoresAdic.Location = New System.Drawing.Point(53, 68)
        Me.lblNumeroConductoresAdic.Name = "lblNumeroConductoresAdic"
        Me.lblNumeroConductoresAdic.Size = New System.Drawing.Size(179, 13)
        Me.lblNumeroConductoresAdic.TabIndex = 1
        Me.lblNumeroConductoresAdic.Text = "Número de Conductores Adicionales"
        '
        'lblConductoresAdicionales
        '
        Me.lblConductoresAdicionales.Location = New System.Drawing.Point(26, 27)
        Me.lblConductoresAdicionales.Name = "lblConductoresAdicionales"
        Me.lblConductoresAdicionales.Size = New System.Drawing.Size(597, 41)
        Me.lblConductoresAdicionales.TabIndex = 0
        Me.lblConductoresAdicionales.Text = "Puede designar de 1 a 4 conductor(es) ocasional(es). " & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Estos conductores deben se" & _
            "r miembros de su familia y disponer del carné de conducir necesario para el vehí" & _
            "culo a asegurar."
        '
        'ctrlMulticonductor1
        '
        Me.ctrlMulticonductor1.Location = New System.Drawing.Point(56, 102)
        Me.ctrlMulticonductor1.MensajeError = ""
        Me.ctrlMulticonductor1.Name = "ctrlMulticonductor1"
        Me.ctrlMulticonductor1.Size = New System.Drawing.Size(529, 615)
        Me.ctrlMulticonductor1.TabIndex = 3
        Me.ctrlMulticonductor1.ToolTipText = Nothing
        '
        'grpAntecedentes
        '
        Me.grpAntecedentes.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grpAntecedentes.Controls.Add(Me.lblConduccionEbrio)
        Me.grpAntecedentes.Controls.Add(Me.txtSiniestrosSinCulpa)
        Me.grpAntecedentes.Controls.Add(Me.txtSiniestrosResponsabilidad)
        Me.grpAntecedentes.Controls.Add(Me.chkTitularPermisoCirculacion)
        Me.grpAntecedentes.Controls.Add(Me.lblTitularPermisoCirculacion)
        Me.grpAntecedentes.Controls.Add(Me.chkPermisoCirculacion)
        Me.grpAntecedentes.Controls.Add(Me.lblPermisoCirculacion)
        Me.grpAntecedentes.Controls.Add(Me.chkSeguroCancelado)
        Me.grpAntecedentes.Controls.Add(Me.lblSeguroCancelado)
        Me.grpAntecedentes.Controls.Add(Me.chkTransporte)
        Me.grpAntecedentes.Controls.Add(Me.lblTransporte)
        Me.grpAntecedentes.Controls.Add(Me.chkConduccionEbrio)
        Me.grpAntecedentes.Controls.Add(Me.chkInfraccionRetirada)
        Me.grpAntecedentes.Controls.Add(Me.lblInfraccionRetirada)
        Me.grpAntecedentes.Controls.Add(Me.lblSiniestrosSinResponsabilidad)
        Me.grpAntecedentes.Controls.Add(Me.lblSiniestrosResponsabilidad)
        Me.grpAntecedentes.Location = New System.Drawing.Point(3, 12)
        Me.grpAntecedentes.Name = "grpAntecedentes"
        Me.grpAntecedentes.Size = New System.Drawing.Size(673, 371)
        Me.grpAntecedentes.TabIndex = 5
        Me.grpAntecedentes.TabStop = False
        Me.grpAntecedentes.Text = "Antecedentes"
        '
        'lblConduccionEbrio
        '
        Me.lblConduccionEbrio.Location = New System.Drawing.Point(26, 156)
        Me.lblConduccionEbrio.Name = "lblConduccionEbrio"
        Me.lblConduccionEbrio.Size = New System.Drawing.Size(309, 31)
        Me.lblConduccionEbrio.TabIndex = 15
        Me.lblConduccionEbrio.Text = "¿Ha cometido alguna infracción por conducir en estado ebrio en los últimos 3 años" & _
            "?"
        Me.lblConduccionEbrio.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'txtSiniestrosSinCulpa
        '
        Me.txtSiniestrosSinCulpa.Location = New System.Drawing.Point(352, 74)
        Me.txtSiniestrosSinCulpa.MensajeErrorValidacion = Nothing
        Me.txtSiniestrosSinCulpa.Name = "txtSiniestrosSinCulpa"
        Me.txtSiniestrosSinCulpa.Size = New System.Drawing.Size(53, 20)
        Me.txtSiniestrosSinCulpa.SoloInteger = True
        Me.txtSiniestrosSinCulpa.TabIndex = 1
        Me.txtSiniestrosSinCulpa.Text = "0"
        Me.txtSiniestrosSinCulpa.ToolTipText = Nothing
        Me.txtSiniestrosSinCulpa.TrimText = False
        '
        'txtSiniestrosResponsabilidad
        '
        Me.txtSiniestrosResponsabilidad.Location = New System.Drawing.Point(352, 27)
        Me.txtSiniestrosResponsabilidad.MensajeErrorValidacion = Nothing
        Me.txtSiniestrosResponsabilidad.Name = "txtSiniestrosResponsabilidad"
        Me.txtSiniestrosResponsabilidad.Size = New System.Drawing.Size(53, 20)
        Me.txtSiniestrosResponsabilidad.SoloInteger = True
        Me.txtSiniestrosResponsabilidad.TabIndex = 0
        Me.txtSiniestrosResponsabilidad.Text = "0"
        Me.txtSiniestrosResponsabilidad.ToolTipText = Nothing
        Me.txtSiniestrosResponsabilidad.TrimText = False
        '
        'chkTitularPermisoCirculacion
        '
        Me.chkTitularPermisoCirculacion.AutoSize = True
        Me.chkTitularPermisoCirculacion.ColorBaseIluminacion = System.Drawing.Color.Orange
        Me.chkTitularPermisoCirculacion.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(165, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.chkTitularPermisoCirculacion.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.chkTitularPermisoCirculacion.Location = New System.Drawing.Point(352, 341)
        Me.chkTitularPermisoCirculacion.Name = "chkTitularPermisoCirculacion"
        Me.chkTitularPermisoCirculacion.Size = New System.Drawing.Size(12, 11)
        Me.chkTitularPermisoCirculacion.TabIndex = 7
        Me.chkTitularPermisoCirculacion.UseVisualStyleBackColor = True
        '
        'lblTitularPermisoCirculacion
        '
        Me.lblTitularPermisoCirculacion.Location = New System.Drawing.Point(13, 327)
        Me.lblTitularPermisoCirculacion.Name = "lblTitularPermisoCirculacion"
        Me.lblTitularPermisoCirculacion.Size = New System.Drawing.Size(322, 41)
        Me.lblTitularPermisoCirculacion.TabIndex = 14
        Me.lblTitularPermisoCirculacion.Text = "¿Es usted, su cónyuge o sus padres titular del permiso de circulación del vehícul" & _
            "o?"
        Me.lblTitularPermisoCirculacion.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'chkPermisoCirculacion
        '
        Me.chkPermisoCirculacion.AutoSize = True
        Me.chkPermisoCirculacion.ColorBaseIluminacion = System.Drawing.Color.Orange
        Me.chkPermisoCirculacion.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(165, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.chkPermisoCirculacion.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.chkPermisoCirculacion.Location = New System.Drawing.Point(352, 296)
        Me.chkPermisoCirculacion.Name = "chkPermisoCirculacion"
        Me.chkPermisoCirculacion.Size = New System.Drawing.Size(12, 11)
        Me.chkPermisoCirculacion.TabIndex = 6
        Me.chkPermisoCirculacion.UseVisualStyleBackColor = True
        '
        'lblPermisoCirculacion
        '
        Me.lblPermisoCirculacion.AutoSize = True
        Me.lblPermisoCirculacion.Location = New System.Drawing.Point(99, 296)
        Me.lblPermisoCirculacion.Name = "lblPermisoCirculacion"
        Me.lblPermisoCirculacion.Size = New System.Drawing.Size(236, 13)
        Me.lblPermisoCirculacion.TabIndex = 12
        Me.lblPermisoCirculacion.Text = "¿Dispone de un permiso de circulación español?"
        Me.lblPermisoCirculacion.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'chkSeguroCancelado
        '
        Me.chkSeguroCancelado.AutoSize = True
        Me.chkSeguroCancelado.ColorBaseIluminacion = System.Drawing.Color.Orange
        Me.chkSeguroCancelado.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(165, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.chkSeguroCancelado.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.chkSeguroCancelado.Location = New System.Drawing.Point(352, 253)
        Me.chkSeguroCancelado.Name = "chkSeguroCancelado"
        Me.chkSeguroCancelado.Size = New System.Drawing.Size(12, 11)
        Me.chkSeguroCancelado.TabIndex = 5
        Me.chkSeguroCancelado.UseVisualStyleBackColor = True
        '
        'lblSeguroCancelado
        '
        Me.lblSeguroCancelado.Location = New System.Drawing.Point(13, 239)
        Me.lblSeguroCancelado.Name = "lblSeguroCancelado"
        Me.lblSeguroCancelado.Size = New System.Drawing.Size(322, 41)
        Me.lblSeguroCancelado.TabIndex = 10
        Me.lblSeguroCancelado.Text = "¿Le ha cancelado su seguro alguna compañía de seguros en los últimos 3 años?"
        Me.lblSeguroCancelado.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'chkTransporte
        '
        Me.chkTransporte.AutoSize = True
        Me.chkTransporte.ColorBaseIluminacion = System.Drawing.Color.Orange
        Me.chkTransporte.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(165, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.chkTransporte.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.chkTransporte.Location = New System.Drawing.Point(352, 204)
        Me.chkTransporte.Name = "chkTransporte"
        Me.chkTransporte.Size = New System.Drawing.Size(12, 11)
        Me.chkTransporte.TabIndex = 4
        Me.chkTransporte.UseVisualStyleBackColor = True
        '
        'lblTransporte
        '
        Me.lblTransporte.Location = New System.Drawing.Point(13, 190)
        Me.lblTransporte.Name = "lblTransporte"
        Me.lblTransporte.Size = New System.Drawing.Size(322, 41)
        Me.lblTransporte.TabIndex = 8
        Me.lblTransporte.Text = "¿Utiliza su vehículo para el transporte remunerado de personas y/o de mercancías?" & _
            ""
        Me.lblTransporte.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'chkConduccionEbrio
        '
        Me.chkConduccionEbrio.AutoSize = True
        Me.chkConduccionEbrio.ColorBaseIluminacion = System.Drawing.Color.Orange
        Me.chkConduccionEbrio.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(165, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.chkConduccionEbrio.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.chkConduccionEbrio.Location = New System.Drawing.Point(352, 163)
        Me.chkConduccionEbrio.Name = "chkConduccionEbrio"
        Me.chkConduccionEbrio.Size = New System.Drawing.Size(12, 11)
        Me.chkConduccionEbrio.TabIndex = 3
        Me.chkConduccionEbrio.UseVisualStyleBackColor = True
        '
        'chkInfraccionRetirada
        '
        Me.chkInfraccionRetirada.AutoSize = True
        Me.chkInfraccionRetirada.ColorBaseIluminacion = System.Drawing.Color.Orange
        Me.chkInfraccionRetirada.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(165, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.chkInfraccionRetirada.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.chkInfraccionRetirada.Location = New System.Drawing.Point(352, 125)
        Me.chkInfraccionRetirada.Name = "chkInfraccionRetirada"
        Me.chkInfraccionRetirada.Size = New System.Drawing.Size(12, 11)
        Me.chkInfraccionRetirada.TabIndex = 2
        Me.chkInfraccionRetirada.UseVisualStyleBackColor = True
        '
        'lblInfraccionRetirada
        '
        Me.lblInfraccionRetirada.Location = New System.Drawing.Point(13, 111)
        Me.lblInfraccionRetirada.Name = "lblInfraccionRetirada"
        Me.lblInfraccionRetirada.Size = New System.Drawing.Size(322, 41)
        Me.lblInfraccionRetirada.TabIndex = 3
        Me.lblInfraccionRetirada.Text = "¿Ha cometido usted alguna infracción que conllevó la retirada del carné de conduc" & _
            "ir en los últimos 3 años?"
        Me.lblInfraccionRetirada.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'lblSiniestrosSinResponsabilidad
        '
        Me.lblSiniestrosSinResponsabilidad.Location = New System.Drawing.Point(13, 63)
        Me.lblSiniestrosSinResponsabilidad.Name = "lblSiniestrosSinResponsabilidad"
        Me.lblSiniestrosSinResponsabilidad.Size = New System.Drawing.Size(322, 41)
        Me.lblSiniestrosSinResponsabilidad.TabIndex = 0
        Me.lblSiniestrosSinResponsabilidad.Text = "¿Cuántos siniestros sin responsabilidad ha tenido usted en el transcurso de los ú" & _
            "ltimos 3 años (coche y moto)?"
        Me.lblSiniestrosSinResponsabilidad.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'lblSiniestrosResponsabilidad
        '
        Me.lblSiniestrosResponsabilidad.Location = New System.Drawing.Point(13, 16)
        Me.lblSiniestrosResponsabilidad.Name = "lblSiniestrosResponsabilidad"
        Me.lblSiniestrosResponsabilidad.Size = New System.Drawing.Size(322, 41)
        Me.lblSiniestrosResponsabilidad.TabIndex = 0
        Me.lblSiniestrosResponsabilidad.Text = "¿Cuántos siniestros con responsabilidad parcial o total ha tenido usted en el tra" & _
            "nscurso de los últimos 3 años (coche y moto)?"
        Me.lblSiniestrosResponsabilidad.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'grpBonificaciones
        '
        Me.grpBonificaciones.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grpBonificaciones.Controls.Add(Me.cboAñosSinSiniestro)
        Me.grpBonificaciones.Controls.Add(Me.optJustCertifRecibo)
        Me.grpBonificaciones.Controls.Add(Me.optJustCertif)
        Me.grpBonificaciones.Controls.Add(Me.optJustNinguno)
        Me.grpBonificaciones.Controls.Add(Me.lblJustificantes)
        Me.grpBonificaciones.Controls.Add(Me.lblAñosSinSiniestro)
        Me.grpBonificaciones.Controls.Add(Me.dtpVencimientoSeguro)
        Me.grpBonificaciones.Controls.Add(Me.lblVencimientoSeguro)
        Me.grpBonificaciones.Controls.Add(Me.chkAseguradoVehiculo)
        Me.grpBonificaciones.Controls.Add(Me.lblAseguradoVehiculo)
        Me.grpBonificaciones.Location = New System.Drawing.Point(3, 12)
        Me.grpBonificaciones.Name = "grpBonificaciones"
        Me.grpBonificaciones.Size = New System.Drawing.Size(673, 371)
        Me.grpBonificaciones.TabIndex = 6
        Me.grpBonificaciones.TabStop = False
        Me.grpBonificaciones.Text = "Bonificaciones"
        '
        'cboAñosSinSiniestro
        '
        Me.cboAñosSinSiniestro.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboAñosSinSiniestro.FormattingEnabled = True
        Me.cboAñosSinSiniestro.Items.AddRange(New Object() {"0", "1", "2", "3", "4 ó más"})
        Me.cboAñosSinSiniestro.Location = New System.Drawing.Point(352, 101)
        Me.cboAñosSinSiniestro.Name = "cboAñosSinSiniestro"
        Me.cboAñosSinSiniestro.Size = New System.Drawing.Size(90, 21)
        Me.cboAñosSinSiniestro.TabIndex = 22
        '
        'optJustCertifRecibo
        '
        Me.optJustCertifRecibo.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.optJustCertifRecibo.Location = New System.Drawing.Point(352, 195)
        Me.optJustCertifRecibo.Name = "optJustCertifRecibo"
        Me.optJustCertifRecibo.Size = New System.Drawing.Size(287, 31)
        Me.optJustCertifRecibo.TabIndex = 5
        Me.optJustCertifRecibo.Text = "Certificado de No Siniestralidad de la compañía anterior y Recibo del año en curs" & _
            "o"
        Me.optJustCertifRecibo.UseVisualStyleBackColor = True
        '
        'optJustCertif
        '
        Me.optJustCertif.AutoSize = True
        Me.optJustCertif.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.optJustCertif.Location = New System.Drawing.Point(352, 172)
        Me.optJustCertif.Name = "optJustCertif"
        Me.optJustCertif.Size = New System.Drawing.Size(286, 17)
        Me.optJustCertif.TabIndex = 4
        Me.optJustCertif.Text = "Certificado de No Siniestralidad de la compañía anterior"
        Me.optJustCertif.UseVisualStyleBackColor = True
        '
        'optJustNinguno
        '
        Me.optJustNinguno.AutoSize = True
        Me.optJustNinguno.Checked = True
        Me.optJustNinguno.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.optJustNinguno.Location = New System.Drawing.Point(352, 149)
        Me.optJustNinguno.Name = "optJustNinguno"
        Me.optJustNinguno.Size = New System.Drawing.Size(64, 17)
        Me.optJustNinguno.TabIndex = 3
        Me.optJustNinguno.TabStop = True
        Me.optJustNinguno.Text = "Ninguno"
        Me.optJustNinguno.UseVisualStyleBackColor = True
        '
        'lblJustificantes
        '
        Me.lblJustificantes.AutoSize = True
        Me.lblJustificantes.Location = New System.Drawing.Point(100, 149)
        Me.lblJustificantes.Name = "lblJustificantes"
        Me.lblJustificantes.Size = New System.Drawing.Size(235, 13)
        Me.lblJustificantes.TabIndex = 21
        Me.lblJustificantes.Text = "¿Cuáles son los justificantes que puede aportar?"
        Me.lblJustificantes.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'lblAñosSinSiniestro
        '
        Me.lblAñosSinSiniestro.AutoSize = True
        Me.lblAñosSinSiniestro.Location = New System.Drawing.Point(35, 104)
        Me.lblAñosSinSiniestro.Name = "lblAñosSinSiniestro"
        Me.lblAñosSinSiniestro.Size = New System.Drawing.Size(300, 13)
        Me.lblAñosSinSiniestro.TabIndex = 19
        Me.lblAñosSinSiniestro.Text = "¿Cuántos años lleva sin un siniestro (como responsable o no)?"
        Me.lblAñosSinSiniestro.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'dtpVencimientoSeguro
        '
        Me.dtpVencimientoSeguro.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpVencimientoSeguro.Location = New System.Drawing.Point(352, 58)
        Me.dtpVencimientoSeguro.Name = "dtpVencimientoSeguro"
        Me.dtpVencimientoSeguro.Size = New System.Drawing.Size(90, 20)
        Me.dtpVencimientoSeguro.TabIndex = 1
        '
        'lblVencimientoSeguro
        '
        Me.lblVencimientoSeguro.AutoSize = True
        Me.lblVencimientoSeguro.Location = New System.Drawing.Point(270, 62)
        Me.lblVencimientoSeguro.Name = "lblVencimientoSeguro"
        Me.lblVencimientoSeguro.Size = New System.Drawing.Size(65, 13)
        Me.lblVencimientoSeguro.TabIndex = 17
        Me.lblVencimientoSeguro.Text = "Vencimiento"
        '
        'chkAseguradoVehiculo
        '
        Me.chkAseguradoVehiculo.AutoSize = True
        Me.chkAseguradoVehiculo.ColorBaseIluminacion = System.Drawing.Color.Orange
        Me.chkAseguradoVehiculo.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(165, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.chkAseguradoVehiculo.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.chkAseguradoVehiculo.Location = New System.Drawing.Point(352, 32)
        Me.chkAseguradoVehiculo.Name = "chkAseguradoVehiculo"
        Me.chkAseguradoVehiculo.Size = New System.Drawing.Size(12, 11)
        Me.chkAseguradoVehiculo.TabIndex = 0
        Me.chkAseguradoVehiculo.UseVisualStyleBackColor = True
        '
        'lblAseguradoVehiculo
        '
        Me.lblAseguradoVehiculo.AutoSize = True
        Me.lblAseguradoVehiculo.Location = New System.Drawing.Point(68, 32)
        Me.lblAseguradoVehiculo.Name = "lblAseguradoVehiculo"
        Me.lblAseguradoVehiculo.Size = New System.Drawing.Size(267, 13)
        Me.lblAseguradoVehiculo.TabIndex = 0
        Me.lblAseguradoVehiculo.Text = "Actualmente está asegurado con una moto o un coche"
        '
        'grpCarnetConducir
        '
        Me.grpCarnetConducir.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grpCarnetConducir.Controls.Add(Me.lblAñosCarnetCalculados)
        Me.grpCarnetConducir.Controls.Add(Me.lblAnyosCarnet)
        Me.grpCarnetConducir.Controls.Add(Me.cboTipoCarne)
        Me.grpCarnetConducir.Controls.Add(Me.lblTipoCarne)
        Me.grpCarnetConducir.Controls.Add(Me.dtpFechaCarne)
        Me.grpCarnetConducir.Controls.Add(Me.lblFechaCarne)
        Me.grpCarnetConducir.Controls.Add(Me.chkTieneCarne)
        Me.grpCarnetConducir.Controls.Add(Me.lblTieneCarne)
        Me.grpCarnetConducir.Location = New System.Drawing.Point(3, 12)
        Me.grpCarnetConducir.Name = "grpCarnetConducir"
        Me.grpCarnetConducir.Size = New System.Drawing.Size(673, 371)
        Me.grpCarnetConducir.TabIndex = 3
        Me.grpCarnetConducir.TabStop = False
        Me.grpCarnetConducir.Text = "Carné de Conducir"
        '
        'lblAñosCarnetCalculados
        '
        Me.lblAñosCarnetCalculados.AutoSize = True
        Me.lblAñosCarnetCalculados.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblAñosCarnetCalculados.Location = New System.Drawing.Point(445, 59)
        Me.lblAñosCarnetCalculados.Name = "lblAñosCarnetCalculados"
        Me.lblAñosCarnetCalculados.Size = New System.Drawing.Size(12, 15)
        Me.lblAñosCarnetCalculados.TabIndex = 26
        Me.lblAñosCarnetCalculados.Text = "-"
        '
        'lblAnyosCarnet
        '
        Me.lblAnyosCarnet.AutoSize = True
        Me.lblAnyosCarnet.Location = New System.Drawing.Point(399, 59)
        Me.lblAnyosCarnet.Name = "lblAnyosCarnet"
        Me.lblAnyosCarnet.Size = New System.Drawing.Size(31, 13)
        Me.lblAnyosCarnet.TabIndex = 19
        Me.lblAnyosCarnet.Text = "Años"
        '
        'cboTipoCarne
        '
        Me.cboTipoCarne.ColorBotón = System.Drawing.SystemColors.Control
        Me.cboTipoCarne.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboTipoCarne.FormattingEnabled = True
        Me.cboTipoCarne.Location = New System.Drawing.Point(258, 87)
        Me.cboTipoCarne.MensajeError = Nothing
        Me.cboTipoCarne.MensajeErrorValidacion = Nothing
        Me.cboTipoCarne.Name = "cboTipoCarne"
        Me.cboTipoCarne.Requerido = False
        Me.cboTipoCarne.RequeridoItem = False
        Me.cboTipoCarne.Size = New System.Drawing.Size(100, 21)
        Me.cboTipoCarne.SoloDouble = False
        Me.cboTipoCarne.SoloInteger = False
        Me.cboTipoCarne.TabIndex = 2
        Me.cboTipoCarne.ToolTipText = Nothing
        Me.cboTipoCarne.Validador1 = Nothing
        '
        'lblTipoCarne
        '
        Me.lblTipoCarne.AutoSize = True
        Me.lblTipoCarne.Location = New System.Drawing.Point(159, 90)
        Me.lblTipoCarne.Name = "lblTipoCarne"
        Me.lblTipoCarne.Size = New System.Drawing.Size(73, 13)
        Me.lblTipoCarne.TabIndex = 4
        Me.lblTipoCarne.Text = "Tipo de carné"
        '
        'dtpFechaCarne
        '
        Me.dtpFechaCarne.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpFechaCarne.Location = New System.Drawing.Point(258, 55)
        Me.dtpFechaCarne.Name = "dtpFechaCarne"
        Me.dtpFechaCarne.Size = New System.Drawing.Size(100, 20)
        Me.dtpFechaCarne.TabIndex = 1
        '
        'lblFechaCarne
        '
        Me.lblFechaCarne.AutoSize = True
        Me.lblFechaCarne.Location = New System.Drawing.Point(148, 59)
        Me.lblFechaCarne.Name = "lblFechaCarne"
        Me.lblFechaCarne.Size = New System.Drawing.Size(84, 13)
        Me.lblFechaCarne.TabIndex = 2
        Me.lblFechaCarne.Text = "Fecha del carné"
        '
        'chkTieneCarne
        '
        Me.chkTieneCarne.AutoSize = True
        Me.chkTieneCarne.ColorBaseIluminacion = System.Drawing.Color.Orange
        Me.chkTieneCarne.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(165, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.chkTieneCarne.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.chkTieneCarne.Location = New System.Drawing.Point(258, 27)
        Me.chkTieneCarne.Name = "chkTieneCarne"
        Me.chkTieneCarne.Size = New System.Drawing.Size(12, 11)
        Me.chkTieneCarne.TabIndex = 0
        Me.chkTieneCarne.UseVisualStyleBackColor = True
        '
        'lblTieneCarne
        '
        Me.lblTieneCarne.AutoSize = True
        Me.lblTieneCarne.Location = New System.Drawing.Point(97, 28)
        Me.lblTieneCarne.Name = "lblTieneCarne"
        Me.lblTieneCarne.Size = New System.Drawing.Size(135, 13)
        Me.lblTieneCarne.TabIndex = 0
        Me.lblTieneCarne.Text = "¿Tiene carné de conducir?"
        '
        'grpDatosVehiculo
        '
        Me.grpDatosVehiculo.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grpDatosVehiculo.Controls.Add(Me.lblAntgCalculada)
        Me.grpDatosVehiculo.Controls.Add(Me.lblAntiguedad)
        Me.grpDatosVehiculo.Controls.Add(Me.txtCilindrada)
        Me.grpDatosVehiculo.Controls.Add(Me.cboModelo)
        Me.grpDatosVehiculo.Controls.Add(Me.cboMarca)
        Me.grpDatosVehiculo.Controls.Add(Me.Panel1)
        Me.grpDatosVehiculo.Controls.Add(Me.lblModelo)
        Me.grpDatosVehiculo.Controls.Add(Me.dtpFecha1Matricula)
        Me.grpDatosVehiculo.Controls.Add(Me.lblFecha1Matricula)
        Me.grpDatosVehiculo.Controls.Add(Me.dtpFechaFabricacion)
        Me.grpDatosVehiculo.Controls.Add(Me.lblFechaFabricacion)
        Me.grpDatosVehiculo.Controls.Add(Me.chkEstaMatriculado)
        Me.grpDatosVehiculo.Controls.Add(Me.lblEstaMatriculado)
        Me.grpDatosVehiculo.Controls.Add(Me.lblcCilindrada)
        Me.grpDatosVehiculo.Controls.Add(Me.lblMarca)
        Me.grpDatosVehiculo.Controls.Add(Me.lblCirculacionHabitual)
        Me.grpDatosVehiculo.Location = New System.Drawing.Point(3, 12)
        Me.grpDatosVehiculo.Name = "grpDatosVehiculo"
        Me.grpDatosVehiculo.Size = New System.Drawing.Size(673, 371)
        Me.grpDatosVehiculo.TabIndex = 2
        Me.grpDatosVehiculo.TabStop = False
        Me.grpDatosVehiculo.Text = "Datos del Vehículo"
        '
        'lblAntgCalculada
        '
        Me.lblAntgCalculada.AutoSize = True
        Me.lblAntgCalculada.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblAntgCalculada.Location = New System.Drawing.Point(445, 273)
        Me.lblAntgCalculada.Name = "lblAntgCalculada"
        Me.lblAntgCalculada.Size = New System.Drawing.Size(12, 15)
        Me.lblAntgCalculada.TabIndex = 25
        Me.lblAntgCalculada.Text = "-"
        '
        'lblAntiguedad
        '
        Me.lblAntiguedad.AutoSize = True
        Me.lblAntiguedad.Location = New System.Drawing.Point(399, 275)
        Me.lblAntiguedad.Name = "lblAntiguedad"
        Me.lblAntiguedad.Size = New System.Drawing.Size(31, 13)
        Me.lblAntiguedad.TabIndex = 18
        Me.lblAntiguedad.Text = "Años"
        '
        'txtCilindrada
        '
        Me.txtCilindrada.Location = New System.Drawing.Point(258, 169)
        Me.txtCilindrada.MensajeErrorValidacion = Nothing
        Me.txtCilindrada.Name = "txtCilindrada"
        Me.txtCilindrada.Size = New System.Drawing.Size(100, 20)
        Me.txtCilindrada.SoloInteger = True
        Me.txtCilindrada.TabIndex = 3
        Me.txtCilindrada.Text = "0"
        Me.txtCilindrada.ToolTipText = Nothing
        Me.txtCilindrada.TrimText = False
        '
        'cboModelo
        '
        Me.cboModelo.ColorBotón = System.Drawing.SystemColors.Control
        Me.cboModelo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboModelo.Enabled = False
        Me.cboModelo.FormattingEnabled = True
        Me.cboModelo.Location = New System.Drawing.Point(258, 134)
        Me.cboModelo.MensajeError = Nothing
        Me.cboModelo.MensajeErrorValidacion = Nothing
        Me.cboModelo.Name = "cboModelo"
        Me.cboModelo.Requerido = False
        Me.cboModelo.RequeridoItem = False
        Me.cboModelo.Size = New System.Drawing.Size(182, 21)
        Me.cboModelo.SoloDouble = False
        Me.cboModelo.SoloInteger = False
        Me.cboModelo.TabIndex = 2
        Me.cboModelo.ToolTipText = Nothing
        Me.cboModelo.Validador1 = Nothing
        '
        'cboMarca
        '
        Me.cboMarca.ColorBotón = System.Drawing.SystemColors.Control
        Me.cboMarca.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboMarca.FormattingEnabled = True
        Me.cboMarca.Location = New System.Drawing.Point(258, 100)
        Me.cboMarca.MensajeError = Nothing
        Me.cboMarca.MensajeErrorValidacion = Nothing
        Me.cboMarca.Name = "cboMarca"
        Me.cboMarca.Requerido = False
        Me.cboMarca.RequeridoItem = False
        Me.cboMarca.Size = New System.Drawing.Size(182, 21)
        Me.cboMarca.SoloDouble = False
        Me.cboMarca.SoloInteger = False
        Me.cboMarca.TabIndex = 1
        Me.cboMarca.ToolTipText = Nothing
        Me.cboMarca.Validador1 = Nothing
        '
        'Panel1
        '
        Me.Panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel1.Controls.Add(Me.cboLocalidadCondHabitual)
        Me.Panel1.Controls.Add(Me.txtCPCondHabitual)
        Me.Panel1.Controls.Add(Me.lblCPConduccionHabitual)
        Me.Panel1.Controls.Add(Me.lblLocalidadCondHabitual)
        Me.Panel1.Location = New System.Drawing.Point(248, 26)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(391, 43)
        Me.Panel1.TabIndex = 0
        '
        'cboLocalidadCondHabitual
        '
        Me.cboLocalidadCondHabitual.ColorBotón = System.Drawing.SystemColors.Control
        Me.cboLocalidadCondHabitual.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboLocalidadCondHabitual.FormattingEnabled = True
        Me.cboLocalidadCondHabitual.Location = New System.Drawing.Point(209, 11)
        Me.cboLocalidadCondHabitual.MensajeError = Nothing
        Me.cboLocalidadCondHabitual.MensajeErrorValidacion = Nothing
        Me.cboLocalidadCondHabitual.Name = "cboLocalidadCondHabitual"
        Me.cboLocalidadCondHabitual.Requerido = False
        Me.cboLocalidadCondHabitual.RequeridoItem = False
        Me.cboLocalidadCondHabitual.Size = New System.Drawing.Size(175, 21)
        Me.cboLocalidadCondHabitual.SoloDouble = False
        Me.cboLocalidadCondHabitual.SoloInteger = False
        Me.cboLocalidadCondHabitual.TabIndex = 1
        Me.cboLocalidadCondHabitual.ToolTipText = Nothing
        Me.cboLocalidadCondHabitual.Validador1 = Nothing
        '
        'txtCPCondHabitual
        '
        Me.txtCPCondHabitual.Location = New System.Drawing.Point(79, 11)
        Me.txtCPCondHabitual.MensajeErrorValidacion = Nothing
        Me.txtCPCondHabitual.Name = "txtCPCondHabitual"
        Me.txtCPCondHabitual.Size = New System.Drawing.Size(64, 20)
        Me.txtCPCondHabitual.TabIndex = 0
        Me.txtCPCondHabitual.ToolTipText = Nothing
        Me.txtCPCondHabitual.TrimText = False
        '
        'lblCPConduccionHabitual
        '
        Me.lblCPConduccionHabitual.AutoSize = True
        Me.lblCPConduccionHabitual.Location = New System.Drawing.Point(1, 14)
        Me.lblCPConduccionHabitual.Name = "lblCPConduccionHabitual"
        Me.lblCPConduccionHabitual.Size = New System.Drawing.Size(72, 13)
        Me.lblCPConduccionHabitual.TabIndex = 4
        Me.lblCPConduccionHabitual.Text = "Código Postal"
        '
        'lblLocalidadCondHabitual
        '
        Me.lblLocalidadCondHabitual.AutoSize = True
        Me.lblLocalidadCondHabitual.Location = New System.Drawing.Point(150, 14)
        Me.lblLocalidadCondHabitual.Name = "lblLocalidadCondHabitual"
        Me.lblLocalidadCondHabitual.Size = New System.Drawing.Size(53, 13)
        Me.lblLocalidadCondHabitual.TabIndex = 5
        Me.lblLocalidadCondHabitual.Text = "Localidad"
        '
        'lblModelo
        '
        Me.lblModelo.AutoSize = True
        Me.lblModelo.Location = New System.Drawing.Point(190, 137)
        Me.lblModelo.Name = "lblModelo"
        Me.lblModelo.Size = New System.Drawing.Size(42, 13)
        Me.lblModelo.TabIndex = 9
        Me.lblModelo.Text = "Modelo"
        '
        'dtpFecha1Matricula
        '
        Me.dtpFecha1Matricula.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpFecha1Matricula.Location = New System.Drawing.Point(258, 271)
        Me.dtpFecha1Matricula.Name = "dtpFecha1Matricula"
        Me.dtpFecha1Matricula.Size = New System.Drawing.Size(100, 20)
        Me.dtpFecha1Matricula.TabIndex = 6
        '
        'lblFecha1Matricula
        '
        Me.lblFecha1Matricula.AutoSize = True
        Me.lblFecha1Matricula.Location = New System.Drawing.Point(101, 275)
        Me.lblFecha1Matricula.Name = "lblFecha1Matricula"
        Me.lblFecha1Matricula.Size = New System.Drawing.Size(131, 13)
        Me.lblFecha1Matricula.TabIndex = 17
        Me.lblFecha1Matricula.Text = "Fecha de 1ª Matriculación"
        '
        'dtpFechaFabricacion
        '
        Me.dtpFechaFabricacion.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpFechaFabricacion.Location = New System.Drawing.Point(258, 240)
        Me.dtpFechaFabricacion.Name = "dtpFechaFabricacion"
        Me.dtpFechaFabricacion.Size = New System.Drawing.Size(100, 20)
        Me.dtpFechaFabricacion.TabIndex = 5
        '
        'lblFechaFabricacion
        '
        Me.lblFechaFabricacion.AutoSize = True
        Me.lblFechaFabricacion.Location = New System.Drawing.Point(122, 244)
        Me.lblFechaFabricacion.Name = "lblFechaFabricacion"
        Me.lblFechaFabricacion.Size = New System.Drawing.Size(110, 13)
        Me.lblFechaFabricacion.TabIndex = 15
        Me.lblFechaFabricacion.Text = "Fecha de Fabricación"
        '
        'chkEstaMatriculado
        '
        Me.chkEstaMatriculado.AutoSize = True
        Me.chkEstaMatriculado.ColorBaseIluminacion = System.Drawing.Color.Orange
        Me.chkEstaMatriculado.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(165, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.chkEstaMatriculado.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.chkEstaMatriculado.Location = New System.Drawing.Point(258, 209)
        Me.chkEstaMatriculado.Name = "chkEstaMatriculado"
        Me.chkEstaMatriculado.Size = New System.Drawing.Size(12, 11)
        Me.chkEstaMatriculado.TabIndex = 4
        Me.chkEstaMatriculado.UseVisualStyleBackColor = True
        '
        'lblEstaMatriculado
        '
        Me.lblEstaMatriculado.AutoSize = True
        Me.lblEstaMatriculado.Location = New System.Drawing.Point(79, 210)
        Me.lblEstaMatriculado.Name = "lblEstaMatriculado"
        Me.lblEstaMatriculado.Size = New System.Drawing.Size(153, 13)
        Me.lblEstaMatriculado.TabIndex = 13
        Me.lblEstaMatriculado.Text = "¿Está matriculado el vehículo?"
        '
        'lblcCilindrada
        '
        Me.lblcCilindrada.AutoSize = True
        Me.lblcCilindrada.Location = New System.Drawing.Point(150, 172)
        Me.lblcCilindrada.Name = "lblcCilindrada"
        Me.lblcCilindrada.Size = New System.Drawing.Size(82, 13)
        Me.lblcCilindrada.TabIndex = 11
        Me.lblcCilindrada.Text = "Cilindrada (cm3)"
        '
        'lblMarca
        '
        Me.lblMarca.AutoSize = True
        Me.lblMarca.Location = New System.Drawing.Point(195, 103)
        Me.lblMarca.Name = "lblMarca"
        Me.lblMarca.Size = New System.Drawing.Size(37, 13)
        Me.lblMarca.TabIndex = 7
        Me.lblMarca.Text = "Marca"
        '
        'lblCirculacionHabitual
        '
        Me.lblCirculacionHabitual.AutoSize = True
        Me.lblCirculacionHabitual.Location = New System.Drawing.Point(26, 41)
        Me.lblCirculacionHabitual.Name = "lblCirculacionHabitual"
        Me.lblCirculacionHabitual.Size = New System.Drawing.Size(206, 13)
        Me.lblCirculacionHabitual.TabIndex = 2
        Me.lblCirculacionHabitual.Text = "Datos de circulación habitual del vehículo"
        '
        'grpDatosIniciales
        '
        Me.grpDatosIniciales.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grpDatosIniciales.Controls.Add(Me.lblTarificacionPrueba)
        Me.grpDatosIniciales.Controls.Add(Me.lblFechaTarificacion)
        Me.grpDatosIniciales.Controls.Add(Me.dtpFechaTarificacion)
        Me.grpDatosIniciales.Controls.Add(Me.chkTarificacionPrueba)
        Me.grpDatosIniciales.Controls.Add(Me.lblIDClienteValor)
        Me.grpDatosIniciales.Controls.Add(Me.lblIDCliente)
        Me.grpDatosIniciales.Controls.Add(Me.cmdBuscar)
        Me.grpDatosIniciales.Controls.Add(Me.txtVendedor)
        Me.grpDatosIniciales.Controls.Add(Me.txtConcesionario)
        Me.grpDatosIniciales.Controls.Add(Me.lblEsCliente)
        Me.grpDatosIniciales.Controls.Add(Me.chkEsCliente)
        Me.grpDatosIniciales.Controls.Add(Me.lblConcesionario)
        Me.grpDatosIniciales.Controls.Add(Me.lblVendedor)
        Me.grpDatosIniciales.Location = New System.Drawing.Point(3, 12)
        Me.grpDatosIniciales.Name = "grpDatosIniciales"
        Me.grpDatosIniciales.Size = New System.Drawing.Size(673, 371)
        Me.grpDatosIniciales.TabIndex = 0
        Me.grpDatosIniciales.TabStop = False
        Me.grpDatosIniciales.Text = "Datos Iniciales"
        '
        'lblTarificacionPrueba
        '
        Me.lblTarificacionPrueba.AutoSize = True
        Me.lblTarificacionPrueba.Location = New System.Drawing.Point(132, 192)
        Me.lblTarificacionPrueba.Name = "lblTarificacionPrueba"
        Me.lblTarificacionPrueba.Size = New System.Drawing.Size(98, 13)
        Me.lblTarificacionPrueba.TabIndex = 19
        Me.lblTarificacionPrueba.Text = "Tarificación prueba"
        '
        'lblFechaTarificacion
        '
        Me.lblFechaTarificacion.AutoSize = True
        Me.lblFechaTarificacion.Location = New System.Drawing.Point(139, 149)
        Me.lblFechaTarificacion.Name = "lblFechaTarificacion"
        Me.lblFechaTarificacion.Size = New System.Drawing.Size(91, 13)
        Me.lblFechaTarificacion.TabIndex = 18
        Me.lblFechaTarificacion.Text = "Fecha tarificación"
        '
        'dtpFechaTarificacion
        '
        Me.dtpFechaTarificacion.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpFechaTarificacion.Location = New System.Drawing.Point(258, 149)
        Me.dtpFechaTarificacion.Name = "dtpFechaTarificacion"
        Me.dtpFechaTarificacion.Size = New System.Drawing.Size(100, 20)
        Me.dtpFechaTarificacion.TabIndex = 16
        '
        'chkTarificacionPrueba
        '
        Me.chkTarificacionPrueba.AutoSize = True
        Me.chkTarificacionPrueba.ColorBaseIluminacion = System.Drawing.Color.Orange
        Me.chkTarificacionPrueba.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(165, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.chkTarificacionPrueba.Location = New System.Drawing.Point(258, 191)
        Me.chkTarificacionPrueba.Name = "chkTarificacionPrueba"
        Me.chkTarificacionPrueba.Size = New System.Drawing.Size(15, 14)
        Me.chkTarificacionPrueba.TabIndex = 17
        Me.chkTarificacionPrueba.UseVisualStyleBackColor = True
        '
        'lblIDClienteValor
        '
        Me.lblIDClienteValor.AutoSize = True
        Me.lblIDClienteValor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblIDClienteValor.Location = New System.Drawing.Point(558, 92)
        Me.lblIDClienteValor.Name = "lblIDClienteValor"
        Me.lblIDClienteValor.Size = New System.Drawing.Size(12, 15)
        Me.lblIDClienteValor.TabIndex = 15
        Me.lblIDClienteValor.Text = "-"
        '
        'lblIDCliente
        '
        Me.lblIDCliente.AutoSize = True
        Me.lblIDCliente.Location = New System.Drawing.Point(496, 92)
        Me.lblIDCliente.Name = "lblIDCliente"
        Me.lblIDCliente.Size = New System.Drawing.Size(56, 13)
        Me.lblIDCliente.TabIndex = 14
        Me.lblIDCliente.Text = "ID Cliente:"
        '
        'cmdBuscar
        '
        Me.cmdBuscar.Enabled = False
        Me.cmdBuscar.Image = Global.GSAMVControlesP.My.Resources.Resources.find
        Me.cmdBuscar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.cmdBuscar.Location = New System.Drawing.Point(363, 87)
        Me.cmdBuscar.Name = "cmdBuscar"
        Me.cmdBuscar.OcultarEnSalida = True
        Me.cmdBuscar.Size = New System.Drawing.Size(118, 33)
        Me.cmdBuscar.TabIndex = 3
        Me.cmdBuscar.Text = "Buscar Cliente"
        Me.cmdBuscar.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.cmdBuscar.UseVisualStyleBackColor = True
        '
        'txtVendedor
        '
        Me.txtVendedor.Location = New System.Drawing.Point(258, 59)
        Me.txtVendedor.MensajeErrorValidacion = Nothing
        Me.txtVendedor.Name = "txtVendedor"
        Me.txtVendedor.Size = New System.Drawing.Size(100, 20)
        Me.txtVendedor.TabIndex = 1
        Me.txtVendedor.ToolTipText = Nothing
        Me.txtVendedor.TrimText = False
        '
        'txtConcesionario
        '
        Me.txtConcesionario.Location = New System.Drawing.Point(258, 28)
        Me.txtConcesionario.MensajeErrorValidacion = Nothing
        Me.txtConcesionario.Name = "txtConcesionario"
        Me.txtConcesionario.Size = New System.Drawing.Size(100, 20)
        Me.txtConcesionario.TabIndex = 0
        Me.txtConcesionario.ToolTipText = Nothing
        Me.txtConcesionario.TrimText = False
        '
        'lblEsCliente
        '
        Me.lblEsCliente.AutoSize = True
        Me.lblEsCliente.Location = New System.Drawing.Point(67, 92)
        Me.lblEsCliente.Name = "lblEsCliente"
        Me.lblEsCliente.Size = New System.Drawing.Size(163, 13)
        Me.lblEsCliente.TabIndex = 7
        Me.lblEsCliente.Text = "¿Está usted asegurado en AMV?"
        '
        'chkEsCliente
        '
        Me.chkEsCliente.AutoSize = True
        Me.chkEsCliente.CheckAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.chkEsCliente.ColorBaseIluminacion = System.Drawing.Color.Orange
        Me.chkEsCliente.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(165, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.chkEsCliente.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.chkEsCliente.Location = New System.Drawing.Point(258, 92)
        Me.chkEsCliente.Name = "chkEsCliente"
        Me.chkEsCliente.Size = New System.Drawing.Size(12, 11)
        Me.chkEsCliente.TabIndex = 2
        Me.chkEsCliente.UseVisualStyleBackColor = True
        '
        'lblConcesionario
        '
        Me.lblConcesionario.AutoSize = True
        Me.lblConcesionario.ForeColor = System.Drawing.Color.Green
        Me.lblConcesionario.Location = New System.Drawing.Point(122, 28)
        Me.lblConcesionario.Name = "lblConcesionario"
        Me.lblConcesionario.Size = New System.Drawing.Size(110, 13)
        Me.lblConcesionario.TabIndex = 0
        Me.lblConcesionario.Text = "Código Concesionario"
        '
        'lblVendedor
        '
        Me.lblVendedor.AutoSize = True
        Me.lblVendedor.ForeColor = System.Drawing.Color.Green
        Me.lblVendedor.Location = New System.Drawing.Point(143, 59)
        Me.lblVendedor.Name = "lblVendedor"
        Me.lblVendedor.Size = New System.Drawing.Size(89, 13)
        Me.lblVendedor.TabIndex = 2
        Me.lblVendedor.Text = "Código Vendedor"
        '
        'grpDatosCliente
        '
        Me.grpDatosCliente.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grpDatosCliente.Controls.Add(Me.chkEsUnicoConductor)
        Me.grpDatosCliente.Controls.Add(Me.lblEsConductorUnico)
        Me.grpDatosCliente.Controls.Add(Me.cmdMasdirecciones)
        Me.grpDatosCliente.Controls.Add(Me.cmdMasEmail)
        Me.grpDatosCliente.Controls.Add(Me.cmdMasFax)
        Me.grpDatosCliente.Controls.Add(Me.cmdMasTelefonos)
        Me.grpDatosCliente.Controls.Add(Me.lblEdadCalc)
        Me.grpDatosCliente.Controls.Add(Me.lblEdad)
        Me.grpDatosCliente.Controls.Add(Me.txtEmail)
        Me.grpDatosCliente.Controls.Add(Me.txtFax)
        Me.grpDatosCliente.Controls.Add(Me.txtTelefono)
        Me.grpDatosCliente.Controls.Add(Me.cboSexo)
        Me.grpDatosCliente.Controls.Add(Me.txtApellido2)
        Me.grpDatosCliente.Controls.Add(Me.txtApellido1)
        Me.grpDatosCliente.Controls.Add(Me.txtNombre)
        Me.grpDatosCliente.Controls.Add(Me.CtrlDireccionEnvio)
        Me.grpDatosCliente.Controls.Add(Me.lblDireccionEnvio)
        Me.grpDatosCliente.Controls.Add(Me.lblSexo)
        Me.grpDatosCliente.Controls.Add(Me.lblEmail)
        Me.grpDatosCliente.Controls.Add(Me.lblFax)
        Me.grpDatosCliente.Controls.Add(Me.lblTelefono)
        Me.grpDatosCliente.Controls.Add(Me.lblFechaNacimiento)
        Me.grpDatosCliente.Controls.Add(Me.dtpFechaNacimiento)
        Me.grpDatosCliente.Controls.Add(Me.lblApellidos)
        Me.grpDatosCliente.Controls.Add(Me.lblNombre)
        Me.grpDatosCliente.Location = New System.Drawing.Point(3, 12)
        Me.grpDatosCliente.Name = "grpDatosCliente"
        Me.grpDatosCliente.Size = New System.Drawing.Size(673, 371)
        Me.grpDatosCliente.TabIndex = 1
        Me.grpDatosCliente.TabStop = False
        Me.grpDatosCliente.Text = "Datos del Cliente"
        '
        'chkEsUnicoConductor
        '
        Me.chkEsUnicoConductor.AutoSize = True
        Me.chkEsUnicoConductor.ColorBaseIluminacion = System.Drawing.Color.Orange
        Me.chkEsUnicoConductor.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(165, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.chkEsUnicoConductor.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.chkEsUnicoConductor.Location = New System.Drawing.Point(258, 308)
        Me.chkEsUnicoConductor.Name = "chkEsUnicoConductor"
        Me.chkEsUnicoConductor.Size = New System.Drawing.Size(12, 11)
        Me.chkEsUnicoConductor.TabIndex = 27
        Me.chkEsUnicoConductor.UseVisualStyleBackColor = True
        '
        'lblEsConductorUnico
        '
        Me.lblEsConductorUnico.AutoSize = True
        Me.lblEsConductorUnico.Location = New System.Drawing.Point(17, 309)
        Me.lblEsConductorUnico.Name = "lblEsConductorUnico"
        Me.lblEsConductorUnico.Size = New System.Drawing.Size(213, 13)
        Me.lblEsConductorUnico.TabIndex = 26
        Me.lblEsConductorUnico.Text = "¿Es usted el único conductor del vehículo?"
        '
        'cmdMasdirecciones
        '
        Me.cmdMasdirecciones.Location = New System.Drawing.Point(640, 250)
        Me.cmdMasdirecciones.Name = "cmdMasdirecciones"
        Me.cmdMasdirecciones.Size = New System.Drawing.Size(24, 23)
        Me.cmdMasdirecciones.TabIndex = 13
        Me.cmdMasdirecciones.Text = "..."
        Me.cmdMasdirecciones.UseVisualStyleBackColor = True
        Me.cmdMasdirecciones.Visible = False
        '
        'cmdMasEmail
        '
        Me.cmdMasEmail.Location = New System.Drawing.Point(446, 204)
        Me.cmdMasEmail.Name = "cmdMasEmail"
        Me.cmdMasEmail.Size = New System.Drawing.Size(24, 23)
        Me.cmdMasEmail.TabIndex = 11
        Me.cmdMasEmail.Text = "..."
        Me.cmdMasEmail.UseVisualStyleBackColor = True
        Me.cmdMasEmail.Visible = False
        '
        'cmdMasFax
        '
        Me.cmdMasFax.Location = New System.Drawing.Point(570, 161)
        Me.cmdMasFax.Name = "cmdMasFax"
        Me.cmdMasFax.Size = New System.Drawing.Size(24, 23)
        Me.cmdMasFax.TabIndex = 9
        Me.cmdMasFax.Text = "..."
        Me.cmdMasFax.UseVisualStyleBackColor = True
        Me.cmdMasFax.Visible = False
        '
        'cmdMasTelefonos
        '
        Me.cmdMasTelefonos.Location = New System.Drawing.Point(368, 164)
        Me.cmdMasTelefonos.Name = "cmdMasTelefonos"
        Me.cmdMasTelefonos.Size = New System.Drawing.Size(24, 23)
        Me.cmdMasTelefonos.TabIndex = 7
        Me.cmdMasTelefonos.Text = "..."
        Me.cmdMasTelefonos.UseVisualStyleBackColor = True
        Me.cmdMasTelefonos.Visible = False
        '
        'lblEdadCalc
        '
        Me.lblEdadCalc.AutoSize = True
        Me.lblEdadCalc.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.lblEdadCalc.Location = New System.Drawing.Point(455, 132)
        Me.lblEdadCalc.Name = "lblEdadCalc"
        Me.lblEdadCalc.Size = New System.Drawing.Size(12, 15)
        Me.lblEdadCalc.TabIndex = 24
        Me.lblEdadCalc.Text = "-"
        '
        'lblEdad
        '
        Me.lblEdad.AutoSize = True
        Me.lblEdad.Location = New System.Drawing.Point(408, 132)
        Me.lblEdad.Name = "lblEdad"
        Me.lblEdad.Size = New System.Drawing.Size(32, 13)
        Me.lblEdad.TabIndex = 23
        Me.lblEdad.Text = "Edad"
        '
        'txtEmail
        '
        Me.txtEmail.Location = New System.Drawing.Point(258, 206)
        Me.txtEmail.MensajeErrorValidacion = Nothing
        Me.txtEmail.Name = "txtEmail"
        Me.txtEmail.Size = New System.Drawing.Size(182, 20)
        Me.txtEmail.TabIndex = 10
        Me.txtEmail.ToolTipText = Nothing
        Me.txtEmail.TrimText = False
        '
        'txtFax
        '
        Me.txtFax.Location = New System.Drawing.Point(460, 163)
        Me.txtFax.MensajeErrorValidacion = Nothing
        Me.txtFax.Name = "txtFax"
        Me.txtFax.Size = New System.Drawing.Size(100, 20)
        Me.txtFax.SoloInteger = True
        Me.txtFax.TabIndex = 8
        Me.txtFax.Text = "0"
        Me.txtFax.ToolTipText = Nothing
        Me.txtFax.TrimText = False
        '
        'txtTelefono
        '
        Me.txtTelefono.Location = New System.Drawing.Point(258, 166)
        Me.txtTelefono.MensajeErrorValidacion = Nothing
        Me.txtTelefono.Name = "txtTelefono"
        Me.txtTelefono.Size = New System.Drawing.Size(100, 20)
        Me.txtTelefono.SoloInteger = True
        Me.txtTelefono.TabIndex = 6
        Me.txtTelefono.Text = "0"
        Me.txtTelefono.ToolTipText = Nothing
        Me.txtTelefono.TrimText = False
        '
        'cboSexo
        '
        Me.cboSexo.ColorBotón = System.Drawing.SystemColors.Control
        Me.cboSexo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboSexo.FormattingEnabled = True
        Me.cboSexo.Location = New System.Drawing.Point(258, 93)
        Me.cboSexo.MensajeError = Nothing
        Me.cboSexo.MensajeErrorValidacion = Nothing
        Me.cboSexo.Name = "cboSexo"
        Me.cboSexo.Requerido = False
        Me.cboSexo.RequeridoItem = False
        Me.cboSexo.Size = New System.Drawing.Size(100, 21)
        Me.cboSexo.SoloDouble = False
        Me.cboSexo.SoloInteger = False
        Me.cboSexo.TabIndex = 4
        Me.cboSexo.ToolTipText = Nothing
        Me.cboSexo.Validador1 = Nothing
        '
        'txtApellido2
        '
        Me.txtApellido2.Location = New System.Drawing.Point(455, 55)
        Me.txtApellido2.MensajeErrorValidacion = Nothing
        Me.txtApellido2.Name = "txtApellido2"
        Me.txtApellido2.Size = New System.Drawing.Size(182, 20)
        Me.txtApellido2.TabIndex = 2
        Me.txtApellido2.ToolTipText = Nothing
        Me.txtApellido2.TrimText = False
        '
        'txtApellido1
        '
        Me.txtApellido1.Location = New System.Drawing.Point(258, 55)
        Me.txtApellido1.MensajeErrorValidacion = Nothing
        Me.txtApellido1.Name = "txtApellido1"
        Me.txtApellido1.Size = New System.Drawing.Size(182, 20)
        Me.txtApellido1.TabIndex = 1
        Me.txtApellido1.ToolTipText = Nothing
        Me.txtApellido1.TrimText = False
        '
        'txtNombre
        '
        Me.txtNombre.Location = New System.Drawing.Point(258, 19)
        Me.txtNombre.MensajeErrorValidacion = Nothing
        Me.txtNombre.Name = "txtNombre"
        Me.txtNombre.Size = New System.Drawing.Size(182, 20)
        Me.txtNombre.TabIndex = 0
        Me.txtNombre.ToolTipText = Nothing
        Me.txtNombre.TrimText = False
        '
        'CtrlDireccionEnvio
        '
        Me.CtrlDireccionEnvio.Location = New System.Drawing.Point(255, 243)
        Me.CtrlDireccionEnvio.MensajeError = "La localidad no puede ser nula"
        Me.CtrlDireccionEnvio.Name = "CtrlDireccionEnvio"
        Me.CtrlDireccionEnvio.Size = New System.Drawing.Size(379, 60)
        Me.CtrlDireccionEnvio.TabIndex = 12
        Me.CtrlDireccionEnvio.ToolTipText = Nothing
        '
        'lblDireccionEnvio
        '
        Me.lblDireccionEnvio.AutoSize = True
        Me.lblDireccionEnvio.Location = New System.Drawing.Point(132, 255)
        Me.lblDireccionEnvio.Name = "lblDireccionEnvio"
        Me.lblDireccionEnvio.Size = New System.Drawing.Size(98, 13)
        Me.lblDireccionEnvio.TabIndex = 0
        Me.lblDireccionEnvio.Text = "Dirección de envío"
        '
        'lblSexo
        '
        Me.lblSexo.AutoSize = True
        Me.lblSexo.Location = New System.Drawing.Point(199, 96)
        Me.lblSexo.Name = "lblSexo"
        Me.lblSexo.Size = New System.Drawing.Size(31, 13)
        Me.lblSexo.TabIndex = 13
        Me.lblSexo.Text = "Sexo"
        '
        'lblEmail
        '
        Me.lblEmail.AutoSize = True
        Me.lblEmail.Location = New System.Drawing.Point(195, 209)
        Me.lblEmail.Name = "lblEmail"
        Me.lblEmail.Size = New System.Drawing.Size(35, 13)
        Me.lblEmail.TabIndex = 11
        Me.lblEmail.Text = "E-mail"
        '
        'lblFax
        '
        Me.lblFax.AutoSize = True
        Me.lblFax.Location = New System.Drawing.Point(408, 166)
        Me.lblFax.Name = "lblFax"
        Me.lblFax.Size = New System.Drawing.Size(24, 13)
        Me.lblFax.TabIndex = 9
        Me.lblFax.Text = "Fax"
        '
        'lblTelefono
        '
        Me.lblTelefono.AutoSize = True
        Me.lblTelefono.Location = New System.Drawing.Point(181, 169)
        Me.lblTelefono.Name = "lblTelefono"
        Me.lblTelefono.Size = New System.Drawing.Size(49, 13)
        Me.lblTelefono.TabIndex = 7
        Me.lblTelefono.Text = "Teléfono"
        '
        'lblFechaNacimiento
        '
        Me.lblFechaNacimiento.AutoSize = True
        Me.lblFechaNacimiento.Location = New System.Drawing.Point(122, 132)
        Me.lblFechaNacimiento.Name = "lblFechaNacimiento"
        Me.lblFechaNacimiento.Size = New System.Drawing.Size(108, 13)
        Me.lblFechaNacimiento.TabIndex = 6
        Me.lblFechaNacimiento.Text = "Fecha de Nacimiento"
        '
        'dtpFechaNacimiento
        '
        Me.dtpFechaNacimiento.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpFechaNacimiento.Location = New System.Drawing.Point(258, 128)
        Me.dtpFechaNacimiento.Name = "dtpFechaNacimiento"
        Me.dtpFechaNacimiento.Size = New System.Drawing.Size(100, 20)
        Me.dtpFechaNacimiento.TabIndex = 5
        '
        'lblApellidos
        '
        Me.lblApellidos.AutoSize = True
        Me.lblApellidos.Location = New System.Drawing.Point(181, 58)
        Me.lblApellidos.Name = "lblApellidos"
        Me.lblApellidos.Size = New System.Drawing.Size(49, 13)
        Me.lblApellidos.TabIndex = 2
        Me.lblApellidos.Text = "Apellidos"
        '
        'lblNombre
        '
        Me.lblNombre.AutoSize = True
        Me.lblNombre.Location = New System.Drawing.Point(186, 22)
        Me.lblNombre.Name = "lblNombre"
        Me.lblNombre.Size = New System.Drawing.Size(44, 13)
        Me.lblNombre.TabIndex = 0
        Me.lblNombre.Text = "Nombre"
        '
        'ctrlCuestionarioTarificacion
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.Control
        Me.Controls.Add(Me.cmdAnterior)
        Me.Controls.Add(Me.cmdSiguiente)
        Me.Controls.Add(Me.cmdTerminarCuestionario)
        Me.Controls.Add(Me.grpDatosVehiculo)
        Me.Controls.Add(Me.grpDatosIniciales)
        Me.Controls.Add(Me.grpDatosCliente)
        Me.Controls.Add(Me.grpConductoresAdicionales)
        Me.Controls.Add(Me.grpAntecedentes)
        Me.Controls.Add(Me.grpBonificaciones)
        Me.Controls.Add(Me.grpCarnetConducir)
        Me.Name = "ctrlCuestionarioTarificacion"
        Me.Size = New System.Drawing.Size(679, 447)
        Me.grpConductoresAdicionales.ResumeLayout(False)
        Me.grpConductoresAdicionales.PerformLayout()
        Me.grpAntecedentes.ResumeLayout(False)
        Me.grpAntecedentes.PerformLayout()
        Me.grpBonificaciones.ResumeLayout(False)
        Me.grpBonificaciones.PerformLayout()
        Me.grpCarnetConducir.ResumeLayout(False)
        Me.grpCarnetConducir.PerformLayout()
        Me.grpDatosVehiculo.ResumeLayout(False)
        Me.grpDatosVehiculo.PerformLayout()
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        Me.grpDatosIniciales.ResumeLayout(False)
        Me.grpDatosIniciales.PerformLayout()
        Me.grpDatosCliente.ResumeLayout(False)
        Me.grpDatosCliente.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents lblConcesionario As System.Windows.Forms.Label
    Friend WithEvents lblVendedor As System.Windows.Forms.Label
    Friend WithEvents grpDatosIniciales As System.Windows.Forms.GroupBox
    Friend WithEvents chkEsCliente As ControlesPBase.CheckBoxP
    Friend WithEvents grpDatosCliente As System.Windows.Forms.GroupBox
    Friend WithEvents lblEmail As System.Windows.Forms.Label
    Friend WithEvents lblFax As System.Windows.Forms.Label
    Friend WithEvents lblTelefono As System.Windows.Forms.Label
    Friend WithEvents lblFechaNacimiento As System.Windows.Forms.Label
    Friend WithEvents dtpFechaNacimiento As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblApellidos As System.Windows.Forms.Label
    Friend WithEvents lblNombre As System.Windows.Forms.Label
    Friend WithEvents lblEsCliente As System.Windows.Forms.Label
    Friend WithEvents lblSexo As System.Windows.Forms.Label
    Friend WithEvents lblDireccionEnvio As System.Windows.Forms.Label
    Friend WithEvents lblLocalidadCondHabitual As System.Windows.Forms.Label
    Friend WithEvents lblCPConduccionHabitual As System.Windows.Forms.Label
    Friend WithEvents lblCirculacionHabitual As System.Windows.Forms.Label
    Friend WithEvents grpDatosVehiculo As System.Windows.Forms.GroupBox
    Friend WithEvents lblcCilindrada As System.Windows.Forms.Label
    Friend WithEvents lblModelo As System.Windows.Forms.Label
    Friend WithEvents lblMarca As System.Windows.Forms.Label
    Friend WithEvents dtpFechaFabricacion As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblFechaFabricacion As System.Windows.Forms.Label
    Friend WithEvents chkEstaMatriculado As ControlesPBase.CheckBoxP
    Friend WithEvents lblEstaMatriculado As System.Windows.Forms.Label
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents dtpFecha1Matricula As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblFecha1Matricula As System.Windows.Forms.Label
    Friend WithEvents grpCarnetConducir As System.Windows.Forms.GroupBox
    Friend WithEvents lblTipoCarne As System.Windows.Forms.Label
    Friend WithEvents dtpFechaCarne As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblFechaCarne As System.Windows.Forms.Label
    Friend WithEvents chkTieneCarne As ControlesPBase.CheckBoxP
    Friend WithEvents lblTieneCarne As System.Windows.Forms.Label
    Friend WithEvents grpConductoresAdicionales As System.Windows.Forms.GroupBox
    Friend WithEvents lblConductoresAdicionales As System.Windows.Forms.Label
    Friend WithEvents cboNumeroConductoresAdic As System.Windows.Forms.ComboBox
    Friend WithEvents lblNumeroConductoresAdic As System.Windows.Forms.Label
    Friend WithEvents Label29 As System.Windows.Forms.Label
    Friend WithEvents chkConductoresAdicTienenCarne As ControlesPBase.CheckBoxP
    Friend WithEvents lblConductoresAdicTienenCarne As System.Windows.Forms.Label
    Friend WithEvents grpAntecedentes As System.Windows.Forms.GroupBox
    Friend WithEvents lblInfraccionRetirada As System.Windows.Forms.Label
    Friend WithEvents lblSiniestrosSinResponsabilidad As System.Windows.Forms.Label
    Friend WithEvents lblSiniestrosResponsabilidad As System.Windows.Forms.Label
    Friend WithEvents chkPermisoCirculacion As ControlesPBase.CheckBoxP
    Friend WithEvents lblPermisoCirculacion As System.Windows.Forms.Label
    Friend WithEvents chkSeguroCancelado As ControlesPBase.CheckBoxP
    Friend WithEvents lblSeguroCancelado As System.Windows.Forms.Label
    Friend WithEvents chkTransporte As ControlesPBase.CheckBoxP
    Friend WithEvents lblTransporte As System.Windows.Forms.Label
    Friend WithEvents chkInfraccionRetirada As ControlesPBase.CheckBoxP
    Friend WithEvents chkTitularPermisoCirculacion As ControlesPBase.CheckBoxP
    Friend WithEvents lblTitularPermisoCirculacion As System.Windows.Forms.Label
    Friend WithEvents grpBonificaciones As System.Windows.Forms.GroupBox
    Friend WithEvents lblVencimientoSeguro As System.Windows.Forms.Label
    Friend WithEvents chkAseguradoVehiculo As ControlesPBase.CheckBoxP
    Friend WithEvents lblAseguradoVehiculo As System.Windows.Forms.Label
    Friend WithEvents optJustCertifRecibo As System.Windows.Forms.RadioButton
    Friend WithEvents optJustCertif As System.Windows.Forms.RadioButton
    Friend WithEvents optJustNinguno As System.Windows.Forms.RadioButton
    Friend WithEvents lblJustificantes As System.Windows.Forms.Label
    Friend WithEvents lblAñosSinSiniestro As System.Windows.Forms.Label
    Friend WithEvents dtpVencimientoSeguro As System.Windows.Forms.DateTimePicker
    Friend WithEvents CtrlDireccionEnvio As GSAMVControlesP.ctrlDireccionNoUnica
    Friend WithEvents txtVendedor As ControlesPBase.txtValidable
    Friend WithEvents txtConcesionario As ControlesPBase.txtValidable
    Friend WithEvents txtEmail As ControlesPBase.txtValidable
    Friend WithEvents txtFax As ControlesPBase.txtValidable
    Friend WithEvents txtTelefono As ControlesPBase.txtValidable
    Friend WithEvents cboSexo As ControlesPBase.CboValidador
    Friend WithEvents txtApellido2 As ControlesPBase.txtValidable
    Friend WithEvents txtApellido1 As ControlesPBase.txtValidable
    Friend WithEvents txtNombre As ControlesPBase.txtValidable
    Friend WithEvents txtCilindrada As ControlesPBase.txtValidable
    Friend WithEvents cboModelo As ControlesPBase.CboValidador
    Friend WithEvents cboMarca As ControlesPBase.CboValidador
    Friend WithEvents cboLocalidadCondHabitual As ControlesPBase.CboValidador
    Friend WithEvents txtCPCondHabitual As ControlesPBase.txtValidable
    Friend WithEvents cboTipoCarne As ControlesPBase.CboValidador
    Friend WithEvents txtSiniestrosSinCulpa As ControlesPBase.txtValidable
    Friend WithEvents txtSiniestrosResponsabilidad As ControlesPBase.txtValidable
    Friend WithEvents cmdTerminarCuestionario As ControlesPBase.BotonP
    Friend WithEvents cmdBuscar As ControlesPBase.BotonP
    Friend WithEvents lblEdad As System.Windows.Forms.Label
    Friend WithEvents lblEdadCalc As System.Windows.Forms.Label
    Friend WithEvents ctrlMulticonductor1 As GSAMVControlesP.ctrlMulticonductor
    Friend WithEvents cmdMasTelefonos As ControlesPBase.BotonP
    Friend WithEvents cmdMasdirecciones As ControlesPBase.BotonP
    Friend WithEvents cmdMasEmail As ControlesPBase.BotonP
    Friend WithEvents cmdMasFax As ControlesPBase.BotonP
    Friend WithEvents lblIDCliente As System.Windows.Forms.Label
    Friend WithEvents lblIDClienteValor As System.Windows.Forms.Label
    Friend WithEvents lblAntgCalculada As System.Windows.Forms.Label
    Friend WithEvents lblAntiguedad As System.Windows.Forms.Label
    Friend WithEvents lblAnyosCarnet As System.Windows.Forms.Label
    Friend WithEvents lblAñosCarnetCalculados As System.Windows.Forms.Label
    Friend WithEvents cmdSiguiente As ControlesPBase.BotonP
    Friend WithEvents cmdAnterior As ControlesPBase.BotonP
    Friend WithEvents chkEsUnicoConductor As ControlesPBase.CheckBoxP
    Friend WithEvents lblEsConductorUnico As System.Windows.Forms.Label
    Friend WithEvents lblConduccionEbrio As System.Windows.Forms.Label
    Friend WithEvents chkConduccionEbrio As ControlesPBase.CheckBoxP
    Friend WithEvents cboAñosSinSiniestro As System.Windows.Forms.ComboBox
    Friend WithEvents dtpFechaTarificacion As System.Windows.Forms.DateTimePicker
    Friend WithEvents chkTarificacionPrueba As ControlesPBase.CheckBoxP
    Friend WithEvents lblTarificacionPrueba As System.Windows.Forms.Label
    Friend WithEvents lblFechaTarificacion As System.Windows.Forms.Label

End Class

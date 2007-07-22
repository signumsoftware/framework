<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmFormularioGenerico
    Inherits MotorIU.FormulariosP.FormularioBase
    Implements Framework.IU.IUComun.IctrlBasicoDN

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing AndAlso components IsNot Nothing Then
            components.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmFormularioGenerico))
        Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
        Me.CtrlGD1 = New MV2Controles.ctrlGD
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip
        Me.AceptarVolver = New System.Windows.Forms.ToolStripButton
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer
        Me.ctrlArbolNavD21 = New MNavegacionDatosIUWin.ctrlArbolNavD2
        Me.TableLayoutPanel1.SuspendLayout()
        Me.ToolStrip1.SuspendLayout()
        Me.SplitContainer1.Panel1.SuspendLayout()
        Me.SplitContainer1.Panel2.SuspendLayout()
        Me.SplitContainer1.SuspendLayout()
        Me.SuspendLayout()
        '
        'TableLayoutPanel1
        '
        Me.TableLayoutPanel1.BackColor = System.Drawing.SystemColors.Control
        Me.TableLayoutPanel1.ColumnCount = 1
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel1.Controls.Add(Me.CtrlGD1, 0, 0)
        Me.TableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TableLayoutPanel1.Location = New System.Drawing.Point(0, 0)
        Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
        Me.TableLayoutPanel1.Padding = New System.Windows.Forms.Padding(3)
        Me.TableLayoutPanel1.RowCount = 1
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 277.0!))
        Me.TableLayoutPanel1.Size = New System.Drawing.Size(605, 283)
        Me.TableLayoutPanel1.TabIndex = 0
        '
        'CtrlGD1
        '
        Me.CtrlGD1.BackColor = System.Drawing.SystemColors.Control
        Me.CtrlGD1.ControlDinamicoSeleccioando = Nothing
        Me.CtrlGD1.DatosControl = Nothing
        Me.CtrlGD1.Location = New System.Drawing.Point(6, 6)
        Me.CtrlGD1.MensajeError = ""
        Me.CtrlGD1.Name = "CtrlGD1"
        Me.CtrlGD1.NombreMapeadoNombreDiseñoyTipo = Nothing
        Me.CtrlGD1.Size = New System.Drawing.Size(138, 47)
        Me.CtrlGD1.TabIndex = 0
        Me.CtrlGD1.ToolTipText = Nothing
        '
        'ToolStrip1
        '
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.AceptarVolver})
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 0)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Size = New System.Drawing.Size(718, 25)
        Me.ToolStrip1.TabIndex = 1
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'AceptarVolver
        '
        Me.AceptarVolver.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.AceptarVolver.Image = CType(resources.GetObject("AceptarVolver.Image"), System.Drawing.Image)
        Me.AceptarVolver.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.AceptarVolver.Name = "AceptarVolver"
        Me.AceptarVolver.Size = New System.Drawing.Size(23, 22)
        Me.AceptarVolver.Text = "Aceptar"
        '
        'SplitContainer1
        '
        Me.SplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.SplitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1
        Me.SplitContainer1.Location = New System.Drawing.Point(0, 25)
        Me.SplitContainer1.Name = "SplitContainer1"
        '
        'SplitContainer1.Panel1
        '
        Me.SplitContainer1.Panel1.Controls.Add(Me.ctrlArbolNavD21)
        Me.SplitContainer1.Panel1.RightToLeft = System.Windows.Forms.RightToLeft.No
        '
        'SplitContainer1.Panel2
        '
        Me.SplitContainer1.Panel2.Controls.Add(Me.TableLayoutPanel1)
        Me.SplitContainer1.Panel2.RightToLeft = System.Windows.Forms.RightToLeft.No
        Me.SplitContainer1.RightToLeft = System.Windows.Forms.RightToLeft.No
        Me.SplitContainer1.Size = New System.Drawing.Size(718, 283)
        Me.SplitContainer1.SplitterDistance = 109
        Me.SplitContainer1.TabIndex = 2
        '
        'ctrlArbolNavD21
        '
        Me.ctrlArbolNavD21.Dock = System.Windows.Forms.DockStyle.Fill
        Me.ctrlArbolNavD21.Location = New System.Drawing.Point(0, 0)
        Me.ctrlArbolNavD21.Name = "ctrlArbolNavD21"
        Me.ctrlArbolNavD21.Size = New System.Drawing.Size(109, 283)
        Me.ctrlArbolNavD21.TabIndex = 0
        '
        'frmFormularioGenerico
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(718, 308)
        Me.Controls.Add(Me.SplitContainer1)
        Me.Controls.Add(Me.ToolStrip1)
        Me.Name = "frmFormularioGenerico"
        Me.Text = "frmFormularioGenerico"
        Me.TableLayoutPanel1.ResumeLayout(False)
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        Me.SplitContainer1.Panel1.ResumeLayout(False)
        Me.SplitContainer1.Panel2.ResumeLayout(False)
        Me.SplitContainer1.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents CtrlGD1 As MV2Controles.ctrlGD
    Friend WithEvents ToolStrip1 As System.Windows.Forms.ToolStrip
    Friend WithEvents AceptarVolver As System.Windows.Forms.ToolStripButton
    Friend WithEvents SplitContainer1 As System.Windows.Forms.SplitContainer
    Friend WithEvents ctrlArbolNavD21 As MNavegacionDatosIUWin.ctrlArbolNavD2

    Public Property DN() As Object Implements Framework.IU.IUComun.IctrlBasicoDN.DN
        Get
            Return Me.CtrlGD1.DN
        End Get
        Set(ByVal value As Object)
            Me.CtrlGD1.DN = value
        End Set
    End Property

    Public Sub DNaIUgd() Implements Framework.IU.IUComun.IctrlBasicoDN.DNaIUgd

    End Sub

    Public Sub IUaDNgd() Implements Framework.IU.IUComun.IctrlBasicoDN.IUaDNgd

    End Sub

    Public Sub Poblar() Implements Framework.IU.IUComun.IctrlBasicoDN.Poblar

    End Sub
End Class

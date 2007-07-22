<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Public Class ctrlListadoTipos
    Inherits MotorIU.ControlesP.BaseControlP

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ctrlListadoTipos))
        Me.dgpTipos = New ControlesPBase.Datagrid.DataGridP
        CType(Me.dgpTipos, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'dgpTipos
        '
        Me.dgpTipos.AlternatingBackColor = System.Drawing.Color.LightCyan
        Me.dgpTipos.BackColorResaltado = System.Drawing.Color.Yellow
        Me.dgpTipos.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.dgpTipos.CaptionBackColor = System.Drawing.SystemColors.Highlight
        Me.dgpTipos.CaptionForeColor = System.Drawing.SystemColors.HighlightText
        Me.dgpTipos.CaptionText = "Tipos"
        Me.dgpTipos.ColumnaID = 0
        Me.dgpTipos.DataMember = ""
        Me.dgpTipos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.dgpTipos.ForeColorResaltado = System.Drawing.Color.Black
        Me.dgpTipos.HeaderForeColor = System.Drawing.SystemColors.ControlText
        Me.dgpTipos.IDsResaltados = CType(resources.GetObject("dgpTipos.IDsResaltados"), System.Collections.ArrayList)
        Me.dgpTipos.Location = New System.Drawing.Point(0, 0)
        Me.dgpTipos.Name = "dgpTipos"
        Me.dgpTipos.ParentRowsVisible = False
        Me.dgpTipos.ReadOnly = True
        Me.dgpTipos.Resaltar = True
        Me.dgpTipos.RowHeadersVisible = False
        Me.dgpTipos.Size = New System.Drawing.Size(201, 279)
        Me.dgpTipos.TabIndex = 2
        '
        'ctrlListadoTipos
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.dgpTipos)
        Me.Name = "ctrlListadoTipos"
        Me.Size = New System.Drawing.Size(201, 279)
        CType(Me.dgpTipos, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents dgpTipos As ControlesPBase.Datagrid.DataGridP

End Class

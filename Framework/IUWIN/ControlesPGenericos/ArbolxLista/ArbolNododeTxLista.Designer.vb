<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ArbolNododeTxLista
    Inherits MotorIU.ControlesP.BaseControlP

    'UserControl overrides dispose to clean up the component list.
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
        Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
        Me.TableLayoutPanel2 = New System.Windows.Forms.TableLayoutPanel
        Me.cmdQuitarTodos = New ControlesPBase.BotonP
        Me.cmdQuitar1 = New ControlesPBase.BotonP
        Me.cmdPasar1 = New ControlesPBase.BotonP
        Me.cmdPasarTodos = New ControlesPBase.BotonP
        Me.lbLista = New System.Windows.Forms.ListBox
        Me.TableLayoutPanel3 = New System.Windows.Forms.TableLayoutPanel
        Me.cmdColapsarArbol = New ControlesPBase.BotonP
        Me.cmdDesplegarArbol = New ControlesPBase.BotonP
        Me.ArbolNododeT1 = New ControlesPGenericos.ArbolNododeT
        Me.TableLayoutPanel1.SuspendLayout()
        Me.TableLayoutPanel2.SuspendLayout()
        Me.TableLayoutPanel3.SuspendLayout()
        Me.SuspendLayout()
        '
        'TableLayoutPanel1
        '
        Me.TableLayoutPanel1.ColumnCount = 3
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50.0!))
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.Controls.Add(Me.TableLayoutPanel2, 1, 1)
        Me.TableLayoutPanel1.Controls.Add(Me.lbLista, 2, 1)
        Me.TableLayoutPanel1.Controls.Add(Me.TableLayoutPanel3, 0, 0)
        Me.TableLayoutPanel1.Controls.Add(Me.ArbolNododeT1, 0, 1)
        Me.TableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TableLayoutPanel1.Location = New System.Drawing.Point(0, 0)
        Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
        Me.TableLayoutPanel1.RowCount = 2
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30.0!))
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel1.Size = New System.Drawing.Size(572, 404)
        Me.TableLayoutPanel1.TabIndex = 0
        '
        'TableLayoutPanel2
        '
        Me.TableLayoutPanel2.ColumnCount = 1
        Me.TableLayoutPanel2.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel2.Controls.Add(Me.cmdQuitarTodos, 0, 3)
        Me.TableLayoutPanel2.Controls.Add(Me.cmdQuitar1, 0, 4)
        Me.TableLayoutPanel2.Controls.Add(Me.cmdPasar1, 0, 1)
        Me.TableLayoutPanel2.Controls.Add(Me.cmdPasarTodos, 0, 2)
        Me.TableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Right
        Me.TableLayoutPanel2.Location = New System.Drawing.Point(264, 33)
        Me.TableLayoutPanel2.Name = "TableLayoutPanel2"
        Me.TableLayoutPanel2.RowCount = 6
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel2.Size = New System.Drawing.Size(44, 368)
        Me.TableLayoutPanel2.TabIndex = 0
        '
        'cmdQuitarTodos
        '
        Me.cmdQuitarTodos.Anchor = System.Windows.Forms.AnchorStyles.Top
        Me.cmdQuitarTodos.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.cmdQuitarTodos.Image = Global.ControlesPGenericos.My.Resources.Resources.navigate_left2
        Me.cmdQuitarTodos.Location = New System.Drawing.Point(9, 187)
        Me.cmdQuitarTodos.Name = "cmdQuitarTodos"
        Me.cmdQuitarTodos.Size = New System.Drawing.Size(25, 25)
        Me.cmdQuitarTodos.TabIndex = 6
        '
        'cmdQuitar1
        '
        Me.cmdQuitar1.Anchor = System.Windows.Forms.AnchorStyles.Top
        Me.cmdQuitar1.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.cmdQuitar1.Image = Global.ControlesPGenericos.My.Resources.Resources.navigate_left
        Me.cmdQuitar1.Location = New System.Drawing.Point(9, 219)
        Me.cmdQuitar1.Name = "cmdQuitar1"
        Me.cmdQuitar1.Size = New System.Drawing.Size(25, 25)
        Me.cmdQuitar1.TabIndex = 7
        '
        'cmdPasar1
        '
        Me.cmdPasar1.Anchor = System.Windows.Forms.AnchorStyles.Top
        Me.cmdPasar1.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.cmdPasar1.Image = Global.ControlesPGenericos.My.Resources.Resources.navigate_right
        Me.cmdPasar1.Location = New System.Drawing.Point(9, 123)
        Me.cmdPasar1.Name = "cmdPasar1"
        Me.cmdPasar1.Size = New System.Drawing.Size(25, 25)
        Me.cmdPasar1.TabIndex = 4
        '
        'cmdPasarTodos
        '
        Me.cmdPasarTodos.Anchor = System.Windows.Forms.AnchorStyles.Top
        Me.cmdPasarTodos.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.cmdPasarTodos.Image = Global.ControlesPGenericos.My.Resources.Resources.navigate_right2
        Me.cmdPasarTodos.Location = New System.Drawing.Point(9, 155)
        Me.cmdPasarTodos.Name = "cmdPasarTodos"
        Me.cmdPasarTodos.Size = New System.Drawing.Size(25, 25)
        Me.cmdPasarTodos.TabIndex = 5
        '
        'lbLista
        '
        Me.lbLista.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lbLista.FormattingEnabled = True
        Me.lbLista.Location = New System.Drawing.Point(314, 33)
        Me.lbLista.Name = "lbLista"
        Me.lbLista.Size = New System.Drawing.Size(255, 368)
        Me.lbLista.TabIndex = 8
        '
        'TableLayoutPanel3
        '
        Me.TableLayoutPanel3.ColumnCount = 4
        Me.TableLayoutPanel3.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10.0!))
        Me.TableLayoutPanel3.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
        Me.TableLayoutPanel3.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
        Me.TableLayoutPanel3.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel3.Controls.Add(Me.cmdColapsarArbol, 2, 0)
        Me.TableLayoutPanel3.Controls.Add(Me.cmdDesplegarArbol, 1, 0)
        Me.TableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TableLayoutPanel3.Location = New System.Drawing.Point(0, 0)
        Me.TableLayoutPanel3.Margin = New System.Windows.Forms.Padding(0)
        Me.TableLayoutPanel3.Name = "TableLayoutPanel3"
        Me.TableLayoutPanel3.RowCount = 1
        Me.TableLayoutPanel3.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel3.Size = New System.Drawing.Size(261, 30)
        Me.TableLayoutPanel3.TabIndex = 17
        '
        'cmdColapsarArbol
        '
        Me.cmdColapsarArbol.Anchor = System.Windows.Forms.AnchorStyles.Top
        Me.cmdColapsarArbol.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.cmdColapsarArbol.Image = Global.ControlesPGenericos.My.Resources.Resources.folder_closed
        Me.cmdColapsarArbol.Location = New System.Drawing.Point(50, 3)
        Me.cmdColapsarArbol.Name = "cmdColapsarArbol"
        Me.cmdColapsarArbol.Size = New System.Drawing.Size(25, 24)
        Me.cmdColapsarArbol.TabIndex = 2
        '
        'cmdDesplegarArbol
        '
        Me.cmdDesplegarArbol.Anchor = System.Windows.Forms.AnchorStyles.Top
        Me.cmdDesplegarArbol.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.cmdDesplegarArbol.Image = Global.ControlesPGenericos.My.Resources.Resources.folder
        Me.cmdDesplegarArbol.Location = New System.Drawing.Point(15, 3)
        Me.cmdDesplegarArbol.Name = "cmdDesplegarArbol"
        Me.cmdDesplegarArbol.Size = New System.Drawing.Size(25, 24)
        Me.cmdDesplegarArbol.TabIndex = 1
        '
        'ArbolNododeT1
        '
        Me.ArbolNododeT1.BackColor = System.Drawing.SystemColors.Control
        Me.ArbolNododeT1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.ArbolNododeT1.Location = New System.Drawing.Point(3, 33)
        Me.ArbolNododeT1.MensajeError = ""
        Me.ArbolNododeT1.Name = "ArbolNododeT1"
        Me.ArbolNododeT1.Size = New System.Drawing.Size(255, 368)
        Me.ArbolNododeT1.TabIndex = 3
        Me.ArbolNododeT1.ToolTipText = Nothing
        '
        'ArbolNododeTxLista
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.TableLayoutPanel1)
        Me.Name = "ArbolNododeTxLista"
        Me.Size = New System.Drawing.Size(572, 404)
        Me.TableLayoutPanel1.ResumeLayout(False)
        Me.TableLayoutPanel2.ResumeLayout(False)
        Me.TableLayoutPanel3.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents TableLayoutPanel2 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents cmdPasarTodos As ControlesPBase.BotonP
    Friend WithEvents lbLista As System.Windows.Forms.ListBox
    Friend WithEvents cmdPasar1 As ControlesPBase.BotonP
    Friend WithEvents cmdQuitarTodos As ControlesPBase.BotonP
    Friend WithEvents cmdQuitar1 As ControlesPBase.BotonP
    Friend WithEvents cmdDesplegarArbol As ControlesPBase.BotonP
    Friend WithEvents cmdColapsarArbol As ControlesPBase.BotonP
    Friend WithEvents TableLayoutPanel3 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents ArbolNododeT1 As ControlesPGenericos.ArbolNododeT

End Class

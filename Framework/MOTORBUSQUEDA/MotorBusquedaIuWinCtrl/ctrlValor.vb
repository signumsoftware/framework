Public Class ctrlValor
    Private mCampo As MotorBusquedaDN.CampoDN

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        'ponemos el valor de fecha actual por defecto
        Me.dtpValor.Value = Now

        'dejamos visible el combo por defecto
        Me.cboValor.Visible = True
        Me.dtpValor.Visible = False
    End Sub

    Public WriteOnly Property Campo() As MotorBusquedaDN.CampoDN
        Set(ByVal value As MotorBusquedaDN.CampoDN)
            Me.mCampo = value
            DNaIU(value)
        End Set
    End Property

    Public ReadOnly Property Valor() As Object
        Get
            If Not Me.mCampo Is Nothing Then
                Select Case Me.mCampo.tipoCampo
                    Case MotorBusquedaDN.tipocampo.boleano
                        Select Case Me.cboValor.Text
                            Case Is = "Sí"
                                Return True
                            Case Is = "No"
                                Return False
                            Case Else
                                Return Nothing
                        End Select
                    Case MotorBusquedaDN.tipocampo.fecha
                        Return Me.dtpValor.Value.ToShortDateString
                    Case Else
                        If mCampo.TieneListaValores Then
                            Return Me.cboValor.Text
                        Else
                            Return Me.cboValor.Text
                        End If
                End Select
            Else
                Return Nothing
            End If
        End Get

    End Property

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        If Not pDN Is Nothing Then
            Dim micampo As MotorBusquedaDN.CampoDN = pDN

            Select Case micampo.tipoCampo
                Case MotorBusquedaDN.tipocampo.fecha
                    Me.dtpValor.Visible = True
                    Me.cboValor.Visible = False

                Case MotorBusquedaDN.tipocampo.boleano
                    ComboVisible()
                    LimpiarCombo()
                    Me.cboValor.DropDownStyle = Windows.Forms.ComboBoxStyle.DropDownList
                    Me.cboValor.Items.Add("Sí")
                    Me.cboValor.Items.Add("No")

                Case Else
                    If micampo.TieneListaValores Then
                        ComboVisible()
                        Me.cboValor.DataSource = micampo.Valores.Tables(0)
                        Me.cboValor.DropDownStyle = Windows.Forms.ComboBoxStyle.DropDownList
                        Me.cboValor.DisplayMember = micampo.NombreCampo

                    Else
                        ComboVisible()
                        LimpiarCombo()

                    End If
            End Select
            Me.cboValor.Refresh()
            Me.dtpValor.Refresh()
        End If

    End Sub

    Private Sub ComboVisible()
        Me.dtpValor.Visible = False
        Me.cboValor.Visible = True
    End Sub

    Private Sub LimpiarCombo()
        cboValor.DataSource = Nothing
        cboValor.Items.Clear()
        cboValor.DropDownStyle = Windows.Forms.ComboBoxStyle.Simple
        cboValor.SelectedText = ""

    End Sub

End Class

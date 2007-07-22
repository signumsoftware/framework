Public Class ctrlCanalEntrada


    Protected mCanalEntradaDocs As AmvDocumentosDN.CanalEntradaDocsDN


    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        FolderBrowserDialog1.ShowDialog()
        Me.txtRuta.Text = Me.FolderBrowserDialog1.SelectedPath
    End Sub


    Public Property CanalEntradaDocs() As AmvDocumentosDN.CanalEntradaDocsDN
        Get

            If Not mCanalEntradaDocs Is Nothing Then
                mCanalEntradaDocs.Nombre = Me.txtNombre.Text
                mCanalEntradaDocs.Ruta = Me.txtRuta.Text
                If Me.CheckBox1.Checked Then
                    NuevoCanal.TipoEntNegocioReferidora = Nothing
                Else
                    NuevoCanal.TipoEntNegocioReferidora = Nothing
                End If
            End If

            Return mCanalEntradaDocs

        End Get
        Set(ByVal value As AmvDocumentosDN.CanalEntradaDocsDN)
            If Not value Is Nothing Then

                Me.txtNombre.Text = value.Nombre
                Me.txtRuta.Text = value.Ruta
                If value.HuellaNodoTipoEntNegoio IsNot Nothing OrElse value.TipoEntNegocioReferidora IsNot Nothing Then
                    Me.CheckBox1.Checked = True
                    If value.HuellaNodoTipoEntNegoio IsNot Nothing Then
                        Dim miobjeto As Framework.DatosNegocio.Arboles.INodoDN
                        Dim arbol As Framework.DatosNegocio.Arboles.INodoDN
                        arbol = Me.ArbolNododeT1.NodoPrincipal
                        miobjeto = arbol.RecuperarNodoXGUID(value.HuellaNodoTipoEntNegoio.GUIDReferida)

                        Me.ArbolNododeT1.ElementoSeleccionado = miobjeto

                    Else
                        Me.ArbolNododeT1.ElementoSeleccionado = value.TipoEntNegocioReferidora
                    End If
                Else
                    Me.CheckBox1.Checked = False
                End If

                ' selecionar el tipo de canal si se tiene

                Me.ComboBox1.SelectedItem = value.TipoCanal


            Else

                Me.txtNombre.Text = ""
                Me.txtRuta.Text = ""
                Me.CheckBox1.Checked = False
                Me.ComboBox1.SelectedItem = Nothing

            End If

            mCanalEntradaDocs = value
        End Set
    End Property




    Public Function NuevoCanal() As AmvDocumentosDN.CanalEntradaDocsDN
        Dim miTipoCanal As AmvDocumentosDN.TipoCanalDN

        NuevoCanal = New AmvDocumentosDN.CanalEntradaDocsDN(Me.txtNombre.Text, Me.txtRuta.Text, False, Me.ComboBox1.SelectedItem)

        If Me.CheckBox1.Checked AndAlso Me.ArbolNododeT1.ElementoSeleccionado IsNot Nothing Then


            If TypeOf Me.ArbolNododeT1.ElementoSeleccionado Is AmvDocumentosDN.TipoEntNegoioDN Then
                Dim tipoEN As AmvDocumentosDN.TipoEntNegoioDN
                tipoEN = Me.ArbolNododeT1.ElementoSeleccionado
                NuevoCanal.TipoEntNegocioReferidora = tipoEN
            Else
                Dim nodo As AmvDocumentosDN.NodoTipoEntNegoioDN
                nodo = Me.ArbolNododeT1.ElementoSeleccionado
                NuevoCanal.HuellaNodoTipoEntNegoio = New AmvDocumentosDN.HuellaNodoTipoEntNegoioDN(nodo)
            End If


        Else
            NuevoCanal.TipoEntNegocioReferidora = Nothing
            NuevoCanal.HuellaNodoTipoEntNegoio = Nothing
        End If


        '   NuevoCanal.TipoCanal = Me.ComboBox1.SelectedItem


    End Function

    Public ReadOnly Property TipoEntNegoioSelecioando() As AmvDocumentosDN.TipoEntNegoioDN

        Get
            Return Me.ArbolNododeT1.ElementoSeleccionado
        End Get

    End Property


    Public Sub Asignar(ByVal nodo As AmvDocumentosDN.NodoTipoEntNegoioDN)

        Me.ArbolNododeT1.NodoPrincipal = nodo

        Me.ArbolNododeT1.ExpandirArbol()

    End Sub

    Public Sub AsignarCanales(ByVal pColCanales As AmvDocumentosDN.ColTipoCanalDN)
        Me.ComboBox1.DisplayMember = "Nombre"
        Me.ComboBox1.Items.Clear()
        For Each micanal As AmvDocumentosDN.TipoCanalDN In pColCanales
            Me.ComboBox1.Items.Add(micanal)
        Next
    End Sub

    'Obsoleto: ahora se pude seleccionar cualquier cosa
    'Private Sub ArbolNododeT1_BeforeSelect(ByRef ElementoSeleccionado As Object, ByVal e As System.Windows.Forms.TreeViewCancelEventArgs) Handles ArbolNododeT1.BeforeSelect
    '    If Not ElementoSeleccionado.GetType Is GetType(AmvDocumentosDN.TipoEntNegoioDN) Then
    '        e.Cancel = True
    '    End If
    'End Sub

    Private Sub CheckBox1_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles CheckBox1.CheckedChanged
        Me.ArbolNododeT1.Enabled = Me.CheckBox1.Checked
    End Sub

    Private Sub ComboBox1_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles ComboBox1.KeyUp
        If e.KeyCode = Keys.Back OrElse e.KeyCode = Keys.Delete Then
            Me.ComboBox1.SelectedItem = Nothing
        End If
    End Sub
End Class

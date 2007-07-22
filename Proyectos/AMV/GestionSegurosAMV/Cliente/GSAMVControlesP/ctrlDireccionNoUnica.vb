Public Class ctrlDireccionNoUnica
    Inherits MotorIU.ControlesP.BaseControlP

    Private mDireccionNoUnica As FN.Localizaciones.DN.DireccionNoUnicaDN
    Private mColLocalidadesTodas As FN.Localizaciones.DN.ColLocalidadDN
    Private mColLocalidadesFiltradasporCP As FN.Localizaciones.DN.ColLocalidadDN

    Private mControlador As GSAMVControladores.ctrlDireccionNoUnica
    Private mLocalidades As FN.Localizaciones.DN.ColLocalidadDN


    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.Controlador = New GSAMVControladores.ctrlDireccionNoUnica(Me.Marco, Me)
        Me.mControlador = Me.Controlador

        'mColLocalidadesTodas = mControlador.ObtenerLocalidades()

        ''cargamos todas las loclaidades por defecto
        'Me.cboLocalidad.Items.AddRange(mColLocalidadesTodas.ToArray)

        Me.cboLocalidad.Enabled = False

        'cargamos todos los tipos de vía
        Me.cboTipoVia.Items.AddRange(mControlador.ObtenerTiposVia().ToArray)

    End Sub

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property DireccionNoUnica() As FN.Localizaciones.DN.DireccionNoUnicaDN
        Get
            If IUaDN() Then
                Return mDireccionNoUnica
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As FN.Localizaciones.DN.DireccionNoUnicaDN)
            mDireccionNoUnica = value
            DNaIU(value)
        End Set
    End Property

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Try
            Dim direccion As FN.Localizaciones.DN.DireccionNoUnicaDN = pDN
            If direccion Is Nothing Then
                Me.cboTipoVia.SelectedItem = Nothing
                Me.tXTVia.Text = String.Empty
                Me.txtCodPostal.Text = String.Empty
                Me.cboLocalidad.SelectedItem = Nothing
            Else
                For Each tipoV As FN.Localizaciones.DN.TipoViaDN In Me.cboTipoVia.Items
                    If tipoV.ID = direccion.TipoVia.ID Then
                        Me.cboTipoVia.SelectedItem = tipoV
                        Exit For
                    End If
                Next
                Me.tXTVia.Text = direccion.Via
                Me.txtCodPostal.Text = direccion.CodPostal
                For Each localidad As FN.Localizaciones.DN.LocalidadDN In Me.cboLocalidad.Items
                    If localidad.ID = direccion.Localidad.ID Then
                        Me.cboLocalidad.SelectedItem = localidad
                        Exit For
                    End If
                Next
            End If

        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

    Protected Overrides Function IUaDN() As Boolean
        Try
            If mDireccionNoUnica Is Nothing Then
                mDireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()
            End If

            mDireccionNoUnica.TipoVia = cboTipoVia.SelectedItem
            mDireccionNoUnica.Via = tXTVia.Text
            mDireccionNoUnica.Numero = tXTVia.Text
            mDireccionNoUnica.CodPostal = txtCodPostal.Text
            mDireccionNoUnica.Localidad = cboLocalidad.SelectedItem

            If mDireccionNoUnica.EstadoIntegridad(me.MensajeError) = Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente Then
                Return False
            End If

            Return True
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Function

    'Private Sub TextboxXT1_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tXTVia.TextChanged
    '    'si el contenido es mayor que el textbox, hacemos que sea extendido
    '    Dim g As Graphics = Me.CreateGraphics()
    '    Me.tXTVia.Extendido = (g.MeasureString(tXTVia.Text, tXTVia.Font).Width > tXTVia.Width)
    '    g.Dispose()
    'End Sub

    Private Sub txtCodPostal_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtCodPostal.LostFocus
        Try
            If Not String.IsNullOrEmpty(Me.txtCodPostal.Text.Trim()) Then
                'formatear a 5 dígitos el cp completando con 0 por la izq
                While Me.txtCodPostal.Text.Length < 5
                    Me.txtCodPostal.Text = "0" & Me.txtCodPostal.Text
                End While

                Me.cboLocalidad.Enabled = True

                'obtener las localidades que le corresponden
                Me.mColLocalidadesFiltradasporCP = Me.mControlador.ObtenerLocalidadPorCodigoPostal(Me.txtCodPostal.Text.Trim())
                Me.cboLocalidad.Items.Clear()
                Me.cboLocalidad.Items.AddRange(Me.mColLocalidadesFiltradasporCP.ToArray())
                'seleccionamos el 1er elemento por defecto
                If Me.cboLocalidad.Items.Count <> 0 Then
                    Me.cboLocalidad.SelectedItem = Me.cboLocalidad.Items(0)
                End If
            Else
                Me.cboLocalidad.Items.Clear()
                Me.cboLocalidad.Items.AddRange(Me.mColLocalidadesTodas.ToArray())
            End If
        Catch ex As Exception
            MostrarError(ex, "Determinar Localidad por CP")
        End Try
    End Sub
End Class

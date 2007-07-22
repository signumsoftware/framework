Namespace Formateadores
    Public Class FormateadorES

        '(*) Si hacemos el recorrido sobre la propiedad Controls del sender directamente, como hacemos removes
        'los elementos se sitúan en una posición más de la que tienen, impidiendo que el último elemento
        'de la colección pase correctamente

        '(**) Hacemos los Remove y los Add sobre el control del que dependan directamente en vez de sobre el mSender
        'porque ralmente están en la colección del control propietario (p. ej, en el Panel en vez de en el Form)

#Region "campos"
        Private mSender As Object
#End Region

#Region "constructores"

        Public Sub New(ByVal sender As Object)
            mSender = sender
        End Sub

#End Region

#Region "métodos"
        Public Sub FormatearEntrada()
            'formateamos todos los controlesES como de entrada: rescatamos los controles del
            'tag del control label dinámico y destruimos éste

            Try
                If mSender Is Nothing Then
                    Exit Sub
                End If

                'llamamos a la función recursiva de formateado
                FormatearEntradaRecursivo(mSender)

            Catch ex As Exception
                Throw ex
            End Try
        End Sub

        Private Sub FormatearEntradaRecursivo(ByVal ControlPropietario As Object)
            Dim micontrol As Control
            Dim micontrol2 As Control
            Dim colcontroles As ArrayList

            Try
                If ControlPropietario Is Nothing Then
                    Exit Sub
                End If

                'cargamos los controles en nuestro propio array (*)
                colcontroles = New ArrayList
                For Each micontrol In ControlPropietario.controls
                    colcontroles.Add(micontrol)
                Next

                For Each micontrol In colcontroles
                    If TypeOf micontrol Is Label Then
                        If Not micontrol.Tag Is Nothing AndAlso Not micontrol.Tag Is Nothing Then
                            If TypeOf micontrol.Tag Is ControlesPBase.IControlES Then
                                'rescatamos el control del tag y lo mostramos
                                micontrol2 = micontrol.Tag
                                micontrol2.Visible = True
                                'destruimos las referencias al control label creado dinàmicamente
                                ControlPropietario.Controls.Remove(micontrol) '(**)
                            End If
                        End If
                    ElseIf Not (TypeOf micontrol Is MotorIU.ControlesP.IControlP) AndAlso NoEsControlGD(micontrol) Then
                        If TypeOf micontrol Is Button Then
                            FormatearBotonesEntrada(micontrol)
                        End If
                        'si es un check o un opt lo habilitamos
                        If micontrol.Tag Is Nothing OrElse TypeOf micontrol Is CheckBox OrElse TypeOf micontrol Is RadioButton Then
                            If Not TypeOf micontrol.Tag Is String OrElse micontrol.Tag <> "ex_formateador" Then
                                micontrol.Enabled = True
                            End If
                        End If
                        'si es un datepicker, lo desactivamos
                        If TypeOf micontrol Is Windows.Forms.DateTimePicker Then
                            micontrol.Enabled = True
                        End If


                        If micontrol.Controls.Count <> 0 Then
                            'llamamos recursivamente a este mismo sub para recorrer
                            'todas las capas de controles
                            FormatearEntradaRecursivo(micontrol)
                        End If
                    End If


                Next
            Catch ex As Exception
                Throw ex
            End Try
        End Sub


        Private Sub FormatearBotonesEntrada(ByVal pBoton As Button)
            'si es cmdAceptar/cmdGuardar actuamos en función del Modal
            If pBoton.Name = "cmdAceptar" Then
                pBoton.Visible = ObtenerFormPadre(pBoton).Modal
            ElseIf pBoton.Name = "cmdGuardar" Then
                pBoton.Visible = Not ObtenerFormPadre(pBoton).Modal

            Else
                'si no, lo restauramos en función del botonP.ocultarensalida
                If TypeOf pBoton Is ControlesPBase.BotonP Then
                    If CType(pBoton, ControlesPBase.BotonP).OcultarEnSalida Then
                        pBoton.Visible = True
                    End If
                ElseIf pBoton.Name = "cmdCancelar" Then
                    'si es cmdCancelar, siempre es visible
                    pBoton.Visible = True
                End If
            End If
        End Sub

        Private Function ObtenerFormPadre(ByVal pControl As Control) As Form
            If TypeOf pControl Is Form Then
                Return pControl
            Else
                Return ObtenerFormPadre(pControl.Parent)
            End If
        End Function

        Private Function NoEsControlGD(ByVal pControl As Control) As Boolean
            If Not pControl.Tag Is Nothing AndAlso (TypeOf (pControl.Tag) Is String) AndAlso (pControl.Tag = "ControlGD") Then
                Return False
            Else
                Return True
            End If
        End Function

        Public Sub FormatearSalida()
            'creamos dinámicamente un control label y lo situamos encima de cada
            'controlES, con su size,location y text, y oculto el original

            Try
                If mSender Is Nothing Then
                    Exit Sub
                End If

                'llamamos a nuestra función recursiva de formateo
                FormatearSalidaRecursivo(mSender)

            Catch ex As Exception
                Throw ex
            End Try
        End Sub

        Private Sub FormatearBotonesSalida(ByVal pBoton As Button)
            'si es botonP formateamos en función de botonP.ocultarensalida
            Select Case pBoton.Name
                Case Is = "cmdAceptar", "cmdGuardar", "cmdCancelar"
                    pBoton.Visible = False
                Case Else
                    If TypeOf pBoton Is ControlesPBase.BotonP AndAlso CType(pBoton, ControlesPBase.BotonP).OcultarEnSalida Then
                        pBoton.Visible = False
                    End If
            End Select
        End Sub

        Private Sub FormatearSalidaRecursivo(ByVal ControlPropietario As Object)
            Dim micolcontroles As ArrayList
            Dim micontrol As Control
            Dim micontrol2 As Label
            Dim mitext As ControlesPBase.txtValidable
            Dim micontrolp As ControlesPBase.IControlPBase
            Try
                If ControlPropietario Is Nothing Then
                    Exit Sub
                End If

                'metemos los controles en nuestro propio arraylist (*)
                micolcontroles = New ArrayList
                For Each micontrol In ControlPropietario.controls
                    micolcontroles.Add(micontrol)
                Next

                For Each micontrol In micolcontroles
                    If TypeOf micontrol Is ControlesPBase.IControlES Then
                        'formateamos con los controles de la primera capa
                        'creamos un nuevo label con las misma posición contenido y tamaño
                        micontrol2 = New Label

                        'establecemos el texto con un enlace al controlP, por si luego operamos
                        'sobre el control ocultado (desde código)
                        'micontrol2.Text = micontrol.Text <-- old: sólo establecía el texto en el estado actual
                        micontrol2.DataBindings.Add(New Binding("Text", micontrol, "Text"))

                        micontrol2.Location = micontrol.Location
                        micontrol2.Size = micontrol.Size
                        micontrol2.Name = "lbld" & micontrol.Name
                        micontrol2.Tag = micontrol
                        micontrol2.Anchor = micontrol.Anchor
                        'si tenemos el controlP con propiedades control
                        'establecemos los colores a partir de éste
                        micontrolp = micontrol
                        If Not micontrolp.PropiedadesControl Is Nothing Then
                            micontrol2.BackColor = micontrolp.PropiedadesControl.ColorConsulta
                            micontrol2.ForeColor = micontrolp.PropiedadesControl.ForeColor
                            micontrol2.Font = micontrol.Font
                        End If

                        'traemos el control al frente
                        micontrol2.BringToFront()

                        '''''si es invisible, lo ocultamos
                        ''''If Not micontrol.Visible Then
                        ''''    micontrol2.Visible = False
                        ''''End If
                        'si es un combobox o un text sin multiline, ponemos el texto alineado al centro
                        'para que no quede mal
                        If TypeOf micontrol Is ControlesPBase.CboValidador Then
                            micontrol2.TextAlign = ContentAlignment.MiddleLeft
                        Else
                            If TypeOf micontrol Is ControlesPBase.txtValidable Then
                                mitext = micontrol
                                If mitext.Multiline = False Then
                                    Select Case mitext.TextAlign
                                        Case Is = HorizontalAlignment.Center
                                            micontrol2.TextAlign = ContentAlignment.MiddleCenter
                                        Case Is = HorizontalAlignment.Left
                                            micontrol2.TextAlign = ContentAlignment.MiddleLeft
                                        Case Is = HorizontalAlignment.Right
                                            micontrol2.TextAlign = ContentAlignment.MiddleRight
                                    End Select
                                End If
                            End If
                        End If

                        'lo agregamos a la colección controls del sender
                        ControlPropietario.controls.add(micontrol2) '(**)
                        'ocultamos el controlES
                        micontrol.Visible = False
                    ElseIf Not TypeOf micontrol Is MotorIU.ControlesP.IControlP AndAlso NoEsControlGD(micontrol) Then
                        'si es un botonP y tiene ocultar en salida y no ex_formateadores lo
                        'hacemos invisible
                        If TypeOf micontrol Is Button Then
                            Me.FormatearBotonesSalida(micontrol)
                        ElseIf TypeOf micontrol Is CheckBox OrElse TypeOf micontrol Is RadioButton Then
                            'si es un check o un opt lo deshabilitamos 
                            If micontrol.Tag Is Nothing OrElse (TypeOf micontrol.Tag Is String AndAlso micontrol.Tag <> "ex_formateador") Then
                                micontrol.Enabled = False
                            End If
                            'si es un datepicker, lo desactivamos
                        ElseIf TypeOf micontrol Is Windows.Forms.DateTimePicker Then
                            micontrol.Enabled = False
                        End If
                        If TypeOf micontrol Is Windows.Forms.DateTimePicker Then

                        End If
                    End If

                    If micontrol.Controls.Count <> 0 Then
                        'llamamos recursivamente a este mismo sub para recorrer
                        'todas las capas de controles
                        FormatearSalidaRecursivo(micontrol)
                    End If

                Next

            Catch ex As Exception
                Throw ex
            End Try
        End Sub

#End Region

    End Class
End Namespace

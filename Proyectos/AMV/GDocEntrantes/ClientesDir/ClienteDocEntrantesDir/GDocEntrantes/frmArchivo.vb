Public Class frmArchivo

#Region "atributos"
    Private mControlador As Controladores.frmArchivoctrl
    Private mRelacionEnFichero As AmvDocumentosDN.RelacionENFicheroDN = Nothing
#End Region

#Region "inicializar"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        'establecemos el controlador
        Me.mControlador = Me.Controlador

        'cargamos el id que venga en el paquete
        If Not Me.Paquete Is Nothing Then
            If Me.Paquete.Contains("ID") Then
                Dim miid As String = Me.Paquete("ID")

                mRelacionEnFichero = Me.mControlador.RecuperarRelacionEnFichero(miid)

                If mRelacionEnFichero IsNot Nothing Then
                    Me.txtExtension.Text = mRelacionEnFichero.HuellaFichero.Extension
                    Me.txtNombreArchivo.Text = mRelacionEnFichero.HuellaFichero.NombreyExtension
                    Me.txtNombreOriginal.Text = mRelacionEnFichero.HuellaFichero.NombreOriginalFichero
                    Me.txtRuta.Text = mRelacionEnFichero.HuellaFichero.RutaAbsoluta
                    Me.txtTipoEntidadNegocio.Text = mRelacionEnFichero.TipoEntNegoio.Nombre
                    'Me.txtIDEntidadNegocio.Text = mRelacionEnFichero.EntidadNegocio.IdEntNeg
                End If

            End If


        Else
            Throw New ApplicationException("El Paquete de Navegación está vacío")
        End If

        'mostramos u ocultamos el botón de abrir archivo en función de si puede o no postclasificar
        Dim miprincipal As Framework.Usuarios.DN.PrincipalDN = Me.cMarco.DatosMarco("Principal")


        Me.cmdRecuperarOperacion.Visible = miprincipal.IsInRole("Operador Cierre")


    End Sub
#End Region

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Try
            Me.Close()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub cmdAbrirArchivo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAbrirArchivo.Click
        Try
            If Not String.IsNullOrEmpty(Me.txtRuta.Text) Then
                System.Diagnostics.Process.Start(Me.txtRuta.Text)
            End If
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub cmdRecuperarOperacion_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdRecuperarOperacion.Click
        Try
            Dim mensaje As String = String.Empty
            Dim mioperacion As AmvDocumentosDN.OperacionEnRelacionENFicheroDN = Nothing

            'If mRelacionEnFichero IsNot Nothing AndAlso mRelacionEnFichero.EntidadNegocio IsNot Nothing Then
            '    mioperacion = Me.mControlador.RecuperarOperacionActivaPorIdEntidad(mRelacionEnFichero.EntidadNegocio.ID, mensaje)
            'Else
            '    mensaje = "No se ha seleccionado ningún documento, o la entidad relacionada es nula"
            'End If


            If Not mioperacion Is Nothing Then

                Dim frmgdoc As frmDocsEntrantes = Nothing
                Dim frmbusqueda As MotorBusquedaIuWin.frmFiltro = Nothing

                For Each miform As Form In Application.OpenForms
                    If miform.Name = "frmDocsEntrantes" Then
                        frmgdoc = miform
                    ElseIf miform.Name = "frmFiltro" Then
                        frmbusqueda = miform
                    End If
                Next

                frmgdoc.CargarOperacionPostClasificar(mioperacion)

                Me.Close()
                frmbusqueda.Close()

                frmgdoc.Show()


            Else
                MessageBox.Show(mensaje, "Recuperar", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End If

        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub
End Class
Public Class frmAdministracionArbol

    Private mControlador As frmAdministracionArbolctrl
    Private mArbol As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.mControlador = Me.Controlador

        'recuperamos el arbol
        mArbol = Me.mControlador.RecuperarArbolEntidades()

        'lo metemos en el árbol
        Me.ArbolNododeT1.NodoPrincipal = mArbol.NodoTipoEntNegoio
    End Sub


    Private Sub ArbolNododeT1_OnElementoSeleccionado(ByRef pElemento As Object) Handles ArbolNododeT1.OnElementoSeleccionado
        Try
            Me.cmdEliminar.Enabled = (Not pElemento Is Nothing)
        Catch ex As Exception
            MostrarError(ex, "Error")
        End Try
    End Sub

    Private Sub cmdAgregarCarpeta_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAgregarCarpeta.Click
        Try
            Dim nombrecarpeta As String = InputBox("Nombre", "Agregar Carpeta", "")

            If nombrecarpeta = String.Empty Then
                Advertencia("Debe determinar un nombre", "advertencia")
                Exit Sub
            End If

            Dim miCarpeta As New AmvDocumentosDN.NodoTipoEntNegoioDN()
            miCarpeta.Nombre = nombrecarpeta

            Dim micarpetapadre As AmvDocumentosDN.NodoTipoEntNegoioDN = CarpetaPadre()

            micarpetapadre.AñadirHijo(miCarpeta)

            Me.ArbolNododeT1.NodoPrincipal = Me.mArbol.NodoTipoEntNegoio
            Me.ArbolNododeT1.ElementoSeleccionado = miCarpeta
            Me.ArbolNododeT1.Refresh()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Function CarpetaPadre() As AmvDocumentosDN.NodoTipoEntNegoioDN
        If TypeOf Me.ArbolNododeT1.ElementoSeleccionado Is AmvDocumentosDN.NodoTipoEntNegoioDN Then
            Return Me.ArbolNododeT1.ElementoSeleccionado
        Else
            Return Me.mArbol.NodoTipoEntNegoio.NodoContenedorPorHijos(Me.ArbolNododeT1.ElementoSeleccionado, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
        End If
    End Function

    Public Sub Advertencia(ByVal mensaje As String, ByVal titulo As String)
        MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
    End Sub

    Private Sub cmdAgregarElemento_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAgregarElemento.Click
        Try
            Dim nombre As String = InputBox("Escriba el Nombre", "Agregar Elemento", "")

            If nombre = String.Empty Then
                Advertencia("Debe determinar un nombre", "advertencia")
                Exit Sub
            End If

            Dim mielemento As New AmvDocumentosDN.TipoEntNegoioDN()
            mielemento.Nombre = nombre

            Dim micarpetapadre As AmvDocumentosDN.NodoTipoEntNegoioDN = CarpetaPadre()

            micarpetapadre.ColHojas.Add(mielemento)

            Me.ArbolNododeT1.NodoPrincipal = Me.mArbol.NodoTipoEntNegoio
            Me.ArbolNododeT1.ElementoSeleccionado = mielemento
            Me.ArbolNododeT1.Refresh()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdEliminar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdEliminar.Click
        Try
            If Not Me.ArbolNododeT1.ElementoSeleccionado Is Nothing Then
                If TypeOf Me.ArbolNododeT1.ElementoSeleccionado Is AmvDocumentosDN.NodoTipoEntNegoioDN Then
                    CType(Me.ArbolNododeT1.ElementoSeleccionado, AmvDocumentosDN.NodoTipoEntNegoioDN).Padre.Eliminar(Me.ArbolNododeT1.ElementoSeleccionado, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
                Else
                    Dim padre As AmvDocumentosDN.NodoTipoEntNegoioDN = CarpetaPadre()
                    padre.ColHojas.Remove(CType(Me.ArbolNododeT1.ElementoSeleccionado, AmvDocumentosDN.TipoEntNegoioDN))
                End If
            End If

            Me.ArbolNododeT1.NodoPrincipal = Me.mArbol.NodoTipoEntNegoio
            Me.ArbolNododeT1.Refresh()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancelar.Click
        Try
            Me.Close()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmd_Aceptar.Click
        Try
            'guardar
            Me.mControlador.GuardarArbol(Me.mArbol)

            Me.Close()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

End Class
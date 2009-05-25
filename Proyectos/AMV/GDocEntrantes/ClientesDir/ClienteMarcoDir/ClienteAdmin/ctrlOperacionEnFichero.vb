Imports AuxIU

Imports AmvDocumentosDN

Public Class ctrlOperacionEnFichero

#Region "Atributos"

    Private mOperacionEnRelacionFichero As OperacionEnRelacionENFicheroDN

#End Region

#Region "Inicializador"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

    End Sub

#End Region

#Region "Propiedades"

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public WriteOnly Property OperacionEnRelacionFichero() As OperacionEnRelacionENFicheroDN
        Set(ByVal value As OperacionEnRelacionENFicheroDN)
            Me.mOperacionEnRelacionFichero = value
            Me.DNaIU(value)
        End Set
    End Property

#End Region

#Region "Establecer y rellenar datos"

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Dim miOperacion As OperacionEnRelacionENFicheroDN

        miOperacion = pDN

        mOperacionEnRelacionFichero = miOperacion

        EstablecerEstadoBotonesFichero()

        If miOperacion IsNot Nothing Then

            If miOperacion.RelacionENFichero IsNot Nothing AndAlso miOperacion.RelacionENFichero.TipoEntNegoio IsNot Nothing Then
                txtEntidadNegocio.Text = miOperacion.RelacionENFichero.TipoEntNegoio.Nombre
            End If

            txtComentarioOperacion.Text = miOperacion.ComentarioOperacion

            'If miOperacion.RelacionENFichero IsNot Nothing AndAlso miOperacion.RelacionENFichero.EntidadNegocio IsNot Nothing Then
            '    txtComentarioEntidad.Text = miOperacion.RelacionENFichero.EntidadNegocio.Comentario
            'End If

            If miOperacion.RelacionENFichero IsNot Nothing AndAlso miOperacion.RelacionENFichero.EstadosRelacionENFichero IsNot Nothing Then
                txtEstado.Text = miOperacion.RelacionENFichero.EstadosRelacionENFichero.Valor
            End If


            txtFF.Text = miOperacion.FF.ToShortDateString() & miOperacion.FF.ToShortTimeString()
            txtFI.Text = miOperacion.FI.ToShortDateString() & miOperacion.FI.ToShortTimeString()

            If miOperacion.RelacionENFichero IsNot Nothing AndAlso miOperacion.RelacionENFichero.HuellaFichero IsNot Nothing Then
                txtNombreFicheroOriginal.Text = miOperacion.RelacionENFichero.HuellaFichero.NombreOriginalFichero
            End If

            If miOperacion.Operador IsNot Nothing Then
                txtNombreOperador.Text = miOperacion.Operador.Nombre
            End If

            If miOperacion.TipoOperacionREnF IsNot Nothing Then
                txtTipoOperacion.Text = miOperacion.TipoOperacionREnF.Nombre
            End If

        End If

    End Sub

#End Region

#Region "Manejadores de eventos"

    Private Sub cmdAbrir_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAbrir.Click
        Try
            AbrirFichero()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdCopiarID_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCopiarID.Click
        Try
            CopiarID()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdCopiarRuta_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCopiarRuta.Click
        Try
            CopiarRuta()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub cmdAbrirFichero_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAbrirFichero.Click
        Try
            AbrirFichero()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

#Region "reconocedor eventos teclado"

    Public Sub RecogerTeclaAbreviada(ByVal e As KeyEventArgs)
        Select Case e.KeyCode
            Case Keys.F2
                'Copiar ID
                CopiarID()
            Case Keys.F3
                'Copiar Ruta
                CopiarRuta()
            Case Keys.F4
                'Abrir archivo
                AbrirFichero()
        End Select
    End Sub

#End Region

#End Region

#Region "Métodos"

    Private Sub AbrirFichero()
        Using New CursorScope(Cursors.WaitCursor)
            If mOperacionEnRelacionFichero IsNot Nothing AndAlso mOperacionEnRelacionFichero.RelacionENFichero IsNot Nothing AndAlso mOperacionEnRelacionFichero.RelacionENFichero.HuellaFichero IsNot Nothing Then
                Dim fichero As String
                fichero = mOperacionEnRelacionFichero.RelacionENFichero.HuellaFichero.RutaAbsoluta
                If System.IO.File.Exists(fichero) Then
                    Dim pr As Process = System.Diagnostics.Process.Start(fichero)
                Else
                    MessageBox.Show("No se ha encontrado el fichero", "Fichero", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                End If
            Else
                MessageBox.Show("La operación no es válida", "Fichero", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End Using
    End Sub

    Private Sub CopiarID()
        Throw New NotImplementedException
        'If mOperacionEnRelacionFichero IsNot Nothing AndAlso mOperacionEnRelacionFichero.RelacionENFichero IsNot Nothing AndAlso mOperacionEnRelacionFichero.RelacionENFichero.EntidadNegocio IsNot Nothing Then
        '    Clipboard.Clear()
        '    Clipboard.SetText(mOperacionEnRelacionFichero.RelacionENFichero.EntidadNegocio.IdEntNeg)
        'Else
        '    MessageBox.Show("La operación no es válida", "Fichero", MessageBoxButtons.OK, MessageBoxIcon.Error)
        'End If
    End Sub

    Private Sub CopiarRuta()
        If mOperacionEnRelacionFichero IsNot Nothing AndAlso mOperacionEnRelacionFichero.RelacionENFichero IsNot Nothing AndAlso mOperacionEnRelacionFichero.RelacionENFichero.HuellaFichero IsNot Nothing Then
            Clipboard.Clear()
            Clipboard.SetText(mOperacionEnRelacionFichero.RelacionENFichero.HuellaFichero.RutaAbsoluta)
        Else
            MessageBox.Show("La operación no es válida", "Fichero", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End If
    End Sub

    Private Sub EstablecerEstadoBotonesFichero()
        If mOperacionEnRelacionFichero Is Nothing Then
            cmdAbrir.Enabled = False
            cmdAbrirFichero.Enabled = False
            cmdCopiarRuta.Enabled = False
            cmdCopiarID.Enabled = False
            Exit Sub
        End If

        If mOperacionEnRelacionFichero.RelacionENFichero Is Nothing OrElse mOperacionEnRelacionFichero.RelacionENFichero.HuellaFichero Is Nothing Then
            cmdAbrir.Enabled = False
            cmdAbrirFichero.Enabled = False
            cmdCopiarRuta.Enabled = False
        Else
            cmdAbrir.Enabled = True
            cmdAbrirFichero.Enabled = True
            cmdCopiarRuta.Enabled = True
        End If
        Throw New NotImplementedException
        'If mOperacionEnRelacionFichero.RelacionENFichero Is Nothing OrElse mOperacionEnRelacionFichero.RelacionENFichero.EntidadNegocio Is Nothing Then
        '    cmdCopiarID.Enabled = False
        'Else
        '    cmdCopiarID.Enabled = True
        'End If
    End Sub

#End Region


End Class

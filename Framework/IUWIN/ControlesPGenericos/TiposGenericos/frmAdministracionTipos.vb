Imports system.Windows.Forms

Public Class frmAdministracionTipos

#Region "Atributos"
    Private mControlador As ctrlAdministracionTiposForm
    Private mTipo As System.Type
    Private mNombre As String
    Private mListaTipos As IList
#End Region

#Region "Inicializar"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
        mControlador = Me.Controlador

        'Para inicializar el formulario tengo que recibir el tipo que administro y el nombre del formulario
        If Me.Paquete IsNot Nothing Then
            If Me.Paquete.Contains("Tipo") AndAlso Me.Paquete("Tipo") IsNot Nothing Then
                Me.mTipo = CType(Me.Paquete("Tipo"), Type)
                Me.CtrlTipos1.Tipo = Me.mTipo
                Me.Paquete.Remove("Tipo")
            Else
                Me.CtrlTipos1.Tipo = GetType(Framework.DatosNegocio.TipoConOrdenDN)
            End If

            'Recupero la lista de objetos para el tipo
            mListaTipos = mControlador.RecuperarListaTipos(mTipo)
            Me.CtrlTipos1.ColTipos = mListaTipos

            'If Me.Paquete.Contains("ColTipos") AndAlso Not Me.Paquete("ColTipos") Is Nothing Then
            '    Me.CtrlTipos1.ColTipos = Me.Paquete("ColTipos")
            '    Me.Paquete.Remove("ColTipos")
            'Else
            '    Me.CtrlTipos1.ColTipos = Nothing
            'End If

            If Me.Paquete.Contains("NombreForm") AndAlso Not Me.Paquete("NombreForm") Is Nothing Then
                mNombre = CType(Me.Paquete("NombreForm"), String)
                Me.Text = mNombre
                Me.Paquete.Remove("NombreForm")
            End If
        End If

    End Sub

#End Region

#Region "Controladores de eventos"

    Private Sub btnAceptar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAceptar.Click
        Try
            Me.Guardar()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub btnCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancelar.Click
        Try
            Me.Close()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

#End Region

#Region "Metodos"

    Private Sub Guardar()
        Try
            If Me.CtrlTipos1.ColTipos IsNot Nothing Then
                Me.mControlador.GuardarListaTipos(Me.CtrlTipos1.ColTipos)
                MessageBox.Show("Los tipos han sido guardados correctamente", mNombre, MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
            Me.Close()
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

#End Region

End Class
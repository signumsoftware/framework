Imports Framework.Mensajeria.GestorMails.DN
Imports Framework.Mensajeria.GestorMensajeriaDN

Imports AmvDocumentosDN

Public Class ctrlOperador

#Region "Atributos"

    Private mControlador As ctrlOperadorControlador

    Private mOperador As OperadorDN
    Private mColTipoEntidadNegocio As New ColTipoEntNegoioDN()
    Private mColDestinos As New ColIDestinos()

    Private mModoConsulta As Boolean

#End Region

#Region "Inicializador"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        Me.mControlador = New ctrlOperadorControlador(Me.Marco, Me)

        'Se cargan los tipos de entidades en el árbol
        Me.ArbolNododeTxLista1.NodoPrincipalArbol = Me.mControlador.RecuperarArbolEntidades.NodoTipoEntNegoio()

    End Sub

#End Region

#Region "Propiedades"

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property Operador() As OperadorDN
        Get
            If IUaDN() Then
                Return Me.mOperador
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As OperadorDN)
            Me.mOperador = value
            Me.DNaIU(value)
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public WriteOnly Property ModoConsulta() As Boolean
        Set(ByVal value As Boolean)
            mModoConsulta = value
            EstablecerModoEdicion()
        End Set
    End Property
#End Region

#Region "Establecer y rellenar datos"

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Dim miOperador As OperadorDN

        miOperador = pDN

        If miOperador Is Nothing Then
            Me.mOperador = Nothing

            Me.txtNombre.Text = ""
            Me.txtFechaAlta.Text = Now.ToLongDateString()
            Me.txtFechaBaja.Text = ""
            Me.cbBaja.Checked = False

            Me.txtMail.Text = ""
            Me.lsbMails.Items.Clear()

        Else
            Me.txtNombre.Text = miOperador.Nombre
            Me.txtFechaAlta.Text = miOperador.FI.ToLongDateString
            Me.cbBaja.Checked = miOperador.Baja

            If miOperador.Baja Then
                Me.txtFechaBaja.Text = miOperador.FF.ToLongDateString
            Else
                Me.txtFechaBaja.Text = ""
            End If

            Me.mColTipoEntidadNegocio.AddRange(miOperador.ColTipoEntNegoio)

            'Se carga el control Arbol a Lista con la colección de nodos
            If mColTipoEntidadNegocio IsNot Nothing AndAlso mColTipoEntidadNegocio.Count > 0 Then
                Dim listaEN As New ArrayList()
                listaEN.AddRange(mColTipoEntidadNegocio.ToArray())
                ArbolNododeTxLista1.ElementosLista = listaEN
            Else
                ArbolNododeTxLista1.ElementosLista = New ArrayList()
            End If

            'Mails del operador
            mColDestinos = New ColIDestinos()

            If miOperador.ColDestinos IsNot Nothing AndAlso miOperador.ColDestinos.Count > 0 Then
                mColDestinos.AddRange(miOperador.ColDestinos)

                For Each miDestino As IDestinoDN In mColDestinos
                    lsbMails.Items.Add(miDestino.Direccion)
                Next
            End If

        End If

    End Sub

    Protected Overrides Function IUaDN() As Boolean
        If Me.ErroresValidadores.Count > 0 Then
            Return False
        End If

        mColTipoEntidadNegocio.Clear()

        If ArbolNododeTxLista1.ElementosLista IsNot Nothing Then
            For Each tipoEN As TipoEntNegoioDN In ArbolNododeTxLista1.ElementosLista
                mColTipoEntidadNegocio.Add(tipoEN)
            Next
        End If

        mColDestinos.Clear()

        mColDestinos = RecuperarColDestinosMail()
        

        If mOperador Is Nothing Then
            mOperador = New OperadorDN(txtNombre.Text, mColTipoEntidadNegocio, mColDestinos)
        Else
            mOperador.Nombre = txtNombre.Text
            mOperador.ColTipoEntNegoio = mColTipoEntidadNegocio
            mOperador.ColDestinos = mColDestinos
        End If

        Return True

    End Function

#End Region

#Region "Manejadores de eventos"

    Private Sub btnAgregarMail_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAgregarMail.Click
        Try
            AgregarMail()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub btnEliminarMail_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEliminarMail.Click
        Try
            EliminarMail()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

#End Region

#Region "Métodos"

    Private Sub EstablecerModoEdicion()
        txtNombre.ReadOnly = mModoConsulta
        txtMail.ReadOnly = mModoConsulta
        btnAgregarMail.Enabled = Not mModoConsulta
        btnEliminarMail.Enabled = Not mModoConsulta
        ArbolNododeTxLista1.SoloLectura = mModoConsulta
    End Sub

    Private Sub AgregarMail()
        Dim valorEmail As String
        valorEmail = txtMail.Text

        If Not String.IsNullOrEmpty(valorEmail) AndAlso Not lsbMails.Items.Contains(valorEmail) Then
            txtMail.Text = ""
            lsbMails.Items.Add(valorEmail)
        End If
    End Sub

    Private Sub EliminarMail()
        If lsbMails.SelectedItem IsNot Nothing Then
            lsbMails.Items.Remove(lsbMails.SelectedItem)
        End If
    End Sub

    Private Function RecuperarColDestinosMail() As ColIDestinos
        Dim miColD As New ColIDestinos()

        If lsbMails.Items.Count > 0 Then
            For Each mail As String In lsbMails.Items
                Dim eMail As New EmailDN(mail)
                Dim eMailUnico As EmailDN

                eMailUnico = mControlador.RecuperarEntidadUnica(Of EmailDN)(eMail.HashValores)

                If eMailUnico Is Nothing Then
                    eMailUnico = eMail
                End If

                Dim canalMail As New CanalMailDN("Email")
                Dim canalMailUnico As CanalMailDN

                canalMailUnico = mControlador.RecuperarEntidadUnica(Of CanalMailDN)(canalMail.HashValores)

                If canalMailUnico Is Nothing Then
                    canalMailUnico = canalMail
                End If

                Dim destinoMail As New DestinoMailDN(eMailUnico, canalMailUnico)
                Dim destinoMailUnico As DestinoMailDN

                destinoMailUnico = mControlador.RecuperarEntidadUnica(Of DestinoMailDN)(destinoMail.HashValores)

                If destinoMailUnico Is Nothing Then
                    destinoMailUnico = destinoMail
                End If

                miColD.Add(destinoMailUnico)

            Next

            Return miColD
        Else
            Return miColD
        End If
    End Function

#End Region


End Class
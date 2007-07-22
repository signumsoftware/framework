
Imports Framework.Ficheros.FicherosDN
Public Class ctrlRutaAlmacenamiento

#Region "Atributos"

    Private mRutaAlmacenamiento As RutaAlmacenamientoFicherosDN
    Private mSoloLecura As Boolean
    Private mRutaAlmacenamientoEstado As RutaAlmacenamientoFicherosEstado

#End Region

#Region "Inicializar"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
    End Sub

#End Region

#Region "Propiedades"

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property RutaAlmacenamiento() As RutaAlmacenamientoFicherosDN
        Get
            If IUaDN() Then
                Return Me.mRutaAlmacenamiento
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As RutaAlmacenamientoFicherosDN)
            Me.mRutaAlmacenamiento = value
            Me.DNaIU(value)
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property SoloLectura() As Boolean
        Get
            Return mSoloLecura
        End Get
        Set(ByVal value As Boolean)
            mSoloLecura = value
            ActualizarEstadoControl()
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public WriteOnly Property RutaAlmacenamientoEstado() As RutaAlmacenamientoFicherosEstado
        Set(ByVal value As RutaAlmacenamientoFicherosEstado)

            mRutaAlmacenamientoEstado = value

            If mRutaAlmacenamientoEstado = RutaAlmacenamientoFicherosEstado.Cerrada Then
                SoloLectura = True
            Else
                SoloLectura = False
            End If
            txtEstado.Text = mRutaAlmacenamientoEstado.ToString()
        End Set
    End Property

#End Region

#Region "Establecer y rellenar datos"

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Dim miRutaAlm As RutaAlmacenamientoFicherosDN

        miRutaAlm = pDN

        mSoloLecura = False
        ActualizarEstadoControl()

        If miRutaAlm Is Nothing Then
            Me.mRutaAlmacenamiento = Nothing

            txtNombre.Text = ""
            txtRuta.Text = ""
            txtEstado.Text = ""

        Else
            txtNombre.Text = miRutaAlm.Nombre
            txtRuta.Text = miRutaAlm.RutaCarpeta
            mRutaAlmacenamientoEstado = miRutaAlm.EstadoRAF

            txtEstado.Text = mRutaAlmacenamientoEstado.ToString()

            If mRutaAlmacenamientoEstado = RutaAlmacenamientoFicherosEstado.Cerrada Then
                SoloLectura = True
            End If

        End If
    End Sub

    Protected Overrides Function IUaDN() As Boolean
        If Me.ErroresValidadores.Count > 0 Then
            Return False
        End If

        If mRutaAlmacenamiento Is Nothing Then
            mRutaAlmacenamiento = New RutaAlmacenamientoFicherosDN(txtNombre.Text, txtRuta.Text)
        Else
            mRutaAlmacenamiento.Nombre = txtNombre.Text
            mRutaAlmacenamiento.RutaCarpeta = txtRuta.Text
            mRutaAlmacenamiento.EstadoRAF = mRutaAlmacenamientoEstado
        End If

        Return True
    End Function

#End Region

#Region "Delegados Eventos"

    Private Sub cmdBuscar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBuscar.Click
        Try
            BuscarRuta()
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

#End Region

#Region "Métodos"

    Private Sub BuscarRuta()
        If FolderBrowserDialog1.ShowDialog() = DialogResult.OK Then
            Me.txtRuta.Text = Me.FolderBrowserDialog1.SelectedPath
        End If
    End Sub

    Private Sub ActualizarEstadoControl()
        txtNombre.ReadOnly = mSoloLecura
        txtRuta.ReadOnly = mSoloLecura
        btnBuscar.Enabled = Not mSoloLecura
    End Sub

#End Region

End Class

Imports MotorBusquedaDN
Imports System.Windows.Forms
Imports MotorBusquedaBasicasDN
Public Class ctrlContructorFiltro

    Public Event Buscar(ByVal sender As Object, ByVal e As EventArgs)


    Private mFiltro As MotorBusquedaDN.FiltroDN
    Private mEstructura As MotorBusquedaDN.EstructuraVistaDN
    Private mParametroCargaEstructura As ParametroCargaEstructuraDN

#Region "hecho"

    Public Property Filtro() As FiltroDN
        Get
            Return mFiltro
        End Get
        Set(ByVal value As FiltroDN)
            mFiltro = value
            Refrescar()

        End Set
    End Property

    Public Sub CargarEstructura(ByVal pParametroCargaEstructura As ParametroCargaEstructuraDN)

        mParametroCargaEstructura = pParametroCargaEstructura

        Dim mias As MotorBusquedaAS.GestorBusquedaAS
        mias = New MotorBusquedaAS.GestorBusquedaAS
        mEstructura = mias.RecuperarEstructuraVista(pParametroCargaEstructura)

        Me.cboCampos.DisplayMember = "NombreCampo"
        Me.cboCampos.DataSource = mEstructura.ListaCampos

        Me.cboOperadores.DataSource = [Enum].GetValues(GetType(OperadoresAritmeticos))

    End Sub

    Private Sub CargarListadoValores(ByVal pCampoSeleccioando As MotorBusquedaDN.CampoDN)

        If pCampoSeleccioando Is Nothing OrElse Not pCampoSeleccioando.TieneListaValores Then
            Me.cboValoresI.DataSource = Nothing
            Me.cboValoresF.DataSource = Nothing
            cboValoresF.Items.Clear()
            cboValoresI.Items.Clear()

            cboValoresI.DropDownStyle = ComboBoxStyle.Simple
            cboValoresF.DropDownStyle = ComboBoxStyle.Simple
            cboValoresI.SelectedText = ""
            cboValoresF.SelectedText = ""

        Else
            cboValoresI.DropDownStyle = ComboBoxStyle.DropDownList
            cboValoresF.DropDownStyle = ComboBoxStyle.DropDownList

            cboValoresI.DisplayMember = pCampoSeleccioando.NombreCampo
            Me.cboValoresI.DataSource = pCampoSeleccioando.mValores.Tables(0)

            cboValoresF.DisplayMember = pCampoSeleccioando.NombreCampo
            Me.cboValoresF.DataSource = pCampoSeleccioando.mValores.Tables(0)

        End If




    End Sub

    Private Sub cboCampos_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboCampos.SelectedIndexChanged


        CargarListadoValores(cboCampos.SelectedItem)


    End Sub


#End Region

    Public Sub Refrescar()
        CondicionDNBindingSource.DataSource = Nothing
        If mFiltro IsNot Nothing AndAlso Not CondicionDNBindingSource.DataSource Is mFiltro.condiciones Then
            'Dim col As New MotorBusquedaDN.ColCondicionDN
            CondicionDNBindingSource.DataSource = mFiltro.condiciones
        End If

        Me.DataGridView1.Refresh()
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAgregar.Click

        If mFiltro Is Nothing Then
            mFiltro = New MotorBusquedaDN.FiltroDN
            mFiltro.ConsultaSQL = mParametroCargaEstructura.ConsultaSQL
            mFiltro.NombreVistaSel = mParametroCargaEstructura.NombreVistaSel
            Me.mFiltro.NombreVistaVis = mParametroCargaEstructura.NombreVistaVis
        End If


        Dim vi, vf As String


        vi = Me.cboValoresI.Text
        vf = Me.cboValoresF.Text

        Dim condicion As MotorBusquedaDN.CondicionDN
        condicion = New MotorBusquedaDN.CondicionDN(Me.cboCampos.SelectedValue, Me.cboOperadores.SelectedValue, vi, vf)
        mFiltro.condiciones.Add(condicion)

        Refrescar()

    End Sub




    Public Function RecuperarDatos() As DataSet

        If mFiltro Is Nothing Then
            Return Nothing
        Else

            Dim mias As MotorBusquedaAS.GestorBusquedaAS
            mias = New MotorBusquedaAS.GestorBusquedaAS
            RecuperarDatos = mias.RecuperarDatos(Me.mFiltro)

        End If





    End Function



    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        'CondicionDNBindingSource.DataSource = Nothing
        'CondicionDNBindingSource.DataSource = Me.mFiltro
        'Me.DataGridView1.DataSource = CondicionDNBindingSource
        ' DataGridView1.Refresh()
    End Sub


    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        If Me.mFiltro IsNot Nothing Then
            Me.CondicionDNBindingSource.DataSource = Nothing
            mFiltro.condiciones.Clear()
            Me.Refrescar()
        End If
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        RaiseEvent Buscar(Me, e)
    End Sub
End Class

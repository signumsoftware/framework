Public Class ctrlConductorAdicional
    Inherits MotorIU.ControlesP.BaseControlP

    Private mDatosconductorAdicional As FN.RiesgosVehiculos.DN.DatosMCND
    Private mFechaEfecto As Date

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        mFechaEfecto = Now()

        'cargamos los parentescos
        Dim arr As Array = [Enum].GetValues(GetType(FN.RiesgosVehiculos.DN.ParentescoConductorAdicional))
        For a As Integer = 0 To arr.GetLength(0) - 1
            Me.cboParentesco.Items.Add(arr(a))
        Next

    End Sub

    Public Property ConductorAdicional() As FN.RiesgosVehiculos.DN.DatosMCND
        Get
            If IUaDN() Then
                Return Me.mDatosconductorAdicional
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As FN.RiesgosVehiculos.DN.DatosMCND)
            Me.mDatosconductorAdicional = value
            DNaIU(value)
        End Set
    End Property

    Public WriteOnly Property FechaEfecto() As Date
        Set(ByVal value As Date)
            mFechaEfecto = value
            DNaIU(Me.mDatosconductorAdicional)
        End Set
    End Property

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Dim dc As FN.RiesgosVehiculos.DN.DatosMCND = pDN
        If Not dc Is Nothing Then
            Me.txtNombre.Text = dc.Nombre
            Me.txtApellido1.Text = dc.Apellido1
            Me.txtApellido2.Text = dc.Apellido2
            Me.dtpFechaNacimiento.Value = dc.FechaNacimiento
            Me.txtNIF.Text = dc.NIF.Codigo
            Me.lblEdadCalc.Text = dc.Edad
            Me.cboParentesco.SelectedItem = dc.Parentesco
        Else
            Me.txtNombre.Text = String.Empty
            Me.txtApellido1.Text = String.Empty
            Me.txtApellido2.Text = String.Empty
            Me.dtpFechaNacimiento.Value = DateTime.Now
            Me.lblEdadCalc.Text = "0"
            Me.txtNIF.Text = String.Empty
            Me.cboParentesco.SelectedItem = Nothing
        End If

    End Sub

    Protected Overrides Function IUaDN() As Boolean
        If Me.mDatosconductorAdicional Is Nothing Then
            Me.mDatosconductorAdicional = New FN.RiesgosVehiculos.DN.DatosMCND()
        End If

        mDatosconductorAdicional.Nombre = Me.txtNombre.Text
        mDatosconductorAdicional.Apellido1 = Me.txtApellido1.Text
        mDatosconductorAdicional.Apellido2 = Me.txtApellido2.Text
        mDatosconductorAdicional.FechaNacimiento = Me.dtpFechaNacimiento.Value
        mDatosconductorAdicional.Parentesco = Me.cboParentesco.SelectedItem
        If Not String.IsNullOrEmpty(Me.txtNIF.Text) Then
            mDatosconductorAdicional.NIF = New FN.Localizaciones.DN.NifDN(Me.txtNIF.Text)
        End If

        Return (Me.mDatosconductorAdicional.EstadoIntegridad(Me.MensajeError) = Framework.DatosNegocio.EstadoIntegridadDN.Consistente)
    End Function

    Private Sub dtpFechaNacimiento_ValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles dtpFechaNacimiento.ValueChanged
        Try
            If mFechaEfecto >= Me.dtpFechaNacimiento.Value() Then
                Me.lblEdadCalc.Text = Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias.CalcularDirAMD(Now(), Me.dtpFechaNacimiento.Value()).Anyos.ToString()
            Else
                Me.lblEdadCalc.Text = 0
            End If
        Catch ex As Exception
            MostrarError(ex, "calcular edad")
        End Try
    End Sub
End Class

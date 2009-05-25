Public Class ctrlMulticonductor
    Inherits MotorIU.ControlesP.BaseControlP

    Private mNumeroConductores As Integer
    Private mColDatosConductorAdicional As FN.RiesgosVehiculos.DN.ColDatosMCND


    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
    End Sub

    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property ColDatosConductorAdicional() As FN.RiesgosVehiculos.DN.ColDatosMCND
        Get
            If Me.IUaDN() Then
                Return Me.mColDatosConductorAdicional
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As FN.RiesgosVehiculos.DN.ColDatosMCND)
            Me.mColDatosConductorAdicional = value
            Me.DNaIU(value)
        End Set
    End Property

    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property NumeroConductores() As Integer
        Get
            Return Me.mNumeroConductores
        End Get
        Set(ByVal value As Integer)
            Me.mNumeroConductores = value
            Select Case value
                Case 0
                    Me.ctrlConductorAdicional1.Enabled = False
                    Me.ctrlConductorAdicional1.Visible = True
                    Me.CtrlConductorAdicional2.Enabled = False
                    Me.CtrlConductorAdicional2.Visible = False
                    Me.CtrlConductorAdicional3.Enabled = False
                    Me.CtrlConductorAdicional3.Visible = False
                    Me.CtrlConductorAdicional4.Enabled = False
                    Me.CtrlConductorAdicional4.Visible = False
                    Me.Height = Me.ctrlConductorAdicional1.Bottom + 3
                Case 1
                    Me.ctrlConductorAdicional1.Enabled = True
                    Me.CtrlConductorAdicional2.Enabled = False
                    Me.CtrlConductorAdicional3.Enabled = False
                    Me.CtrlConductorAdicional4.Enabled = False
                    Me.ctrlConductorAdicional1.Visible = True
                    Me.CtrlConductorAdicional2.Visible = False
                    Me.CtrlConductorAdicional3.Visible = False
                    Me.CtrlConductorAdicional4.Visible = False
                    Me.Height = Me.ctrlConductorAdicional1.Bottom + 3
                Case 2
                    Me.ctrlConductorAdicional1.Enabled = True
                    Me.CtrlConductorAdicional2.Enabled = True
                    Me.CtrlConductorAdicional3.Enabled = False
                    Me.CtrlConductorAdicional4.Enabled = False
                    Me.ctrlConductorAdicional1.Visible = True
                    Me.CtrlConductorAdicional2.Visible = True
                    Me.CtrlConductorAdicional3.Visible = False
                    Me.CtrlConductorAdicional4.Visible = False
                    Me.Height = Me.CtrlConductorAdicional2.Bottom + 3
                Case 3
                    Me.ctrlConductorAdicional1.Enabled = True
                    Me.CtrlConductorAdicional2.Enabled = True
                    Me.CtrlConductorAdicional3.Enabled = True
                    Me.CtrlConductorAdicional4.Enabled = False
                    Me.ctrlConductorAdicional1.Visible = True
                    Me.CtrlConductorAdicional2.Visible = True
                    Me.CtrlConductorAdicional3.Visible = True
                    Me.CtrlConductorAdicional4.Visible = False
                    Me.Height = Me.CtrlConductorAdicional3.Bottom + 3

                Case 4
                    Me.ctrlConductorAdicional1.Enabled = True
                    Me.CtrlConductorAdicional2.Enabled = True
                    Me.CtrlConductorAdicional3.Enabled = True
                    Me.CtrlConductorAdicional4.Enabled = True
                    Me.ctrlConductorAdicional1.Visible = True
                    Me.CtrlConductorAdicional2.Visible = True
                    Me.CtrlConductorAdicional3.Visible = True
                    Me.CtrlConductorAdicional4.Visible = True
                    Me.Height = Me.CtrlConductorAdicional4.Bottom + 3
                Case Else
                    Throw New NotImplementedException("Actualmente sólo se pueden insertar 4 conductores adicionales")
            End Select
        End Set
    End Property

    Public WriteOnly Property FechaEfecto() As Date
        Set(ByVal value As Date)
            Me.ctrlConductorAdicional1.FechaEfecto = value
        End Set
    End Property

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Dim col As FN.RiesgosVehiculos.DN.ColDatosMCND = pDN
        Me.NumeroConductores = col.Count
        For a As Integer = 1 To col.Count - 1
            Dim mictrl As ctrlConductorAdicional
            Select Case a
                Case 1
                    mictrl = Me.ctrlConductorAdicional1
                    mictrl.ConductorAdicional = col.Item(a)
                Case 2
                    mictrl = Me.CtrlConductorAdicional2
                    mictrl.ConductorAdicional = col.Item(a)
                Case 3
                    mictrl = Me.CtrlConductorAdicional3
                    mictrl.ConductorAdicional = col.Item(a)
                Case 4
                    mictrl = Me.CtrlConductorAdicional4
                    mictrl.ConductorAdicional = col.Item(a)
                Case Else
                    Throw New NotImplementedException("Actualmente sólo se pueden insertar 4 conductores adicionales")
            End Select
        Next
    End Sub

    Protected Overrides Function IUaDN() As Boolean
        If Me.mColDatosConductorAdicional Is Nothing Then
            Me.mColDatosConductorAdicional = New FN.RiesgosVehiculos.DN.ColDatosMCND
        End If

        For a As Integer = 1 To Me.mNumeroConductores
            Dim midatos As FN.RiesgosVehiculos.DN.DatosMCND
            Dim ctrl As ctrlConductorAdicional = Nothing
            Select Case a
                Case 1
                    ctrl = Me.ctrlConductorAdicional1
                Case 2
                    ctrl = Me.CtrlConductorAdicional2
                Case 3
                    ctrl = Me.CtrlConductorAdicional3
                Case 4
                    ctrl = Me.CtrlConductorAdicional4
            End Select

            midatos = ctrl.ConductorAdicional
            If midatos Is Nothing Then
                Me.MensajeError = ctrl.MensajeError
                Return False
            Else
                If Not Me.mColDatosConductorAdicional.Contains(midatos) Then
                    Me.mColDatosConductorAdicional.Add(midatos)
                End If
            End If
        Next
        Return True
    End Function

End Class

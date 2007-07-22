Imports Framework.Usuarios.DN


Public Class Form1
    Implements MV2DN.IRecuperadorInstanciaMap

    Public Sub New()
        AddHandler AppDomain.CurrentDomain.AssemblyResolve, AddressOf ControladorCargaEnsamblados

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.



    End Sub

    Public Function ControladorCargaEnsamblados(ByVal Sender As Object, ByVal args As ResolveEventArgs) As System.Reflection.Assembly
        System.Windows.Forms.MessageBox.Show(Sender.ToString)

    End Function

    Public Function RecuperarInstanciaMap(ByVal pNombreMapInstancia As String) As MV2DN.InstanciaMapDN Implements MV2DN.IRecuperadorInstanciaMap.RecuperarInstanciaMap

    End Function

    Public Function RecuperarInstanciaMap(ByVal pTipo As System.Type) As MV2DN.InstanciaMapDN Implements MV2DN.IRecuperadorInstanciaMap.RecuperarInstanciaMap

        If pTipo.Name = "PrincipalDN" Then



            Dim instMap As MV2DN.InstanciaMapDN
            Dim agMap As MV2DN.AgrupacionMapDN
            Dim propMap As MV2DN.PropMapDN

            instMap = New MV2DN.InstanciaMapDN
            instMap.Nombre = "Principal vis1"

            Dim tag As New MV2DN.TipoAgrupacionMapDN
            tag.ID = "1"
            tag.Nombre = "Solapa"

            agMap = New MV2DN.AgrupacionMapDN(instMap, tag)
            agMap.NombreVis = "Solapa1"

            propMap = New MV2DN.PropMapDN
            propMap.NombreProp = "ID"
            propMap.NombreVis = "codigo:"
            agMap.ColPropMap.Add(propMap)

            propMap = New MV2DN.PropMapDN
            propMap.NombreProp = "Nombre"
            propMap.NombreVis = "NombPrincipal:"
            agMap.ColPropMap.Add(propMap)

            propMap = New MV2DN.PropMapDN
            propMap.NombreProp = "UsuarioDN.Nombre"
            propMap.NombreVis = "NombUsuario:"
            agMap.ColPropMap.Add(propMap)

            propMap = New MV2DN.PropMapDN
            propMap.NombreProp = "ColRolDN"
            propMap.NombreVis = "Roles"
            agMap.ColPropMap.Add(propMap)

            propMap = New MV2DN.PropMapDN
            propMap.NombreProp = "Nombreeeeee"
            propMap.NombreVis = "NoEsta:"
            agMap.ColPropMap.Add(propMap)

            ' System.Diagnostics.Debug.WriteLine(instMap.ToXML)
            Return instMap
        End If




        If pTipo.Name = "RolDN" Then



            Dim instMap As MV2DN.InstanciaMapDN
            Dim agMap As MV2DN.AgrupacionMapDN
            Dim propMap As MV2DN.PropMapDN

            instMap = New MV2DN.InstanciaMapDN
            instMap.Nombre = "Principal vis1"

            Dim tag As New MV2DN.TipoAgrupacionMapDN
            tag.ID = "1"
            tag.Nombre = "Solapa"

            agMap = New MV2DN.AgrupacionMapDN(instMap, tag)
            agMap.NombreVis = "Solapa1"

            propMap = New MV2DN.PropMapDN
            propMap.NombreProp = "ID"
            propMap.NombreVis = "codigo:"
            agMap.ColPropMap.Add(propMap)

            propMap = New MV2DN.PropMapDN
            propMap.NombreProp = "Nombre"
            propMap.NombreVis = "Nombre:"
            agMap.ColPropMap.Add(propMap)


            'System.Diagnostics.Debug.WriteLine(instMap.ToXML)
            Return instMap
        End If

    End Function

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click


        Me.CtrlGD1.TipoEntidad = GetType(PrincipalDN)
        Me.CtrlGD1.Poblar()
    End Sub




    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        'Me.CtrlGD1.Poblar()
    End Sub

    Private Sub FlowLayoutPanel1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs)

    End Sub

    Private Sub CtrlGD1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CtrlGD1.Load

    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        Dim usuario As UsuarioDN
        usuario = New UsuarioDN("jose parada", False)

        ' Dim rol As UsuariosDN.RolDN

        Dim colroles As ColRolDN
        colroles = New ColRolDN
        colroles.Add(New RolDN("todo poderoso presentador", New ColCasosUsoDN))
        colroles.Add(New RolDN("metro sexual", New ColCasosUsoDN))
        colroles.Add(New RolDN("divino", New ColCasosUsoDN))


        Dim prin As PrincipalDN
        prin = New PrincipalDN("principal1", usuario, colroles)

        Me.CtrlGD1.DN = prin
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        Me.CtrlGD1.IUaDNgd()
        Dim pr As PrincipalDN
        pr = Me.CtrlGD1.DN

        ' Me.TextBox1.Text = pr.Nombre & "  " & pr.UsuarioDN.Nombre & "  " & pr.ColRolDN(0).Nombre




    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        Me.CtrlGD1.NombreTipo = "UsuariosDN.PrincipalDN"
    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        '    Me.CtrlGD1.NombreMapeadoNombreDiseñoTipoy = "UsuariosDN.PrincipalDN"

    End Sub

    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click
        Me.TextBox2.Text = Me.CtrlGD1.InstanciaMap.ToXML

    End Sub

    Private Sub Button7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button7.Click
        Me.CtrlGD1.DN = Nothing
    End Sub
End Class

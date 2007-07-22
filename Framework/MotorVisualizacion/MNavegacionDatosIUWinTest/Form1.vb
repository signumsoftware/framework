Imports Framework.Usuarios.DN
Imports FN.Empresas.DN
Public Class Form1
    Private mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnVincularTipo.Click


        Me.CtrlArbolNavD1.mRecurso = mRecurso
        Me.CtrlArbolNavD1.VincularTipo(GetType(MNavegacionDatosTest.Persona))
    End Sub

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        ObtenerRecurso()
    End Sub

    Private Sub ObtenerRecurso()

        Dim connectionstring As String
        Dim htd As New Dictionary(Of String, Object)

        'connectionstring = "server=localhost;database=MND1;user=sa;pwd=''"
        connectionstring = "server=localhost;database=ssPruebasFT;user=sa;pwd='sa'"
        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        'Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New Framework.AccesoDatos.MotorAD.LN.GestorMapPersistenciaCamposAMVDocsEntrantesLN
        ' Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New Framework.AccesoDatos.MotorAD.LN.GestorMapPersistenciaCamposNULOLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New MNavegacionDatosTest.GestorMapPersistenciaCamposMNavDTest
        Dim rec As MV2DN.RecuperadorMapeadoXFicheroXMLAD

        rec = Me.CtrlArbolNavD1.RecuperadorMap
        rec.RutaDirectorioMapeados = "D:\Signum\Proyectos\AMV\GDocEntrantes\ClientesDir\ClienteMarcoDir\ClienteAdmin\Mapeados"
    End Sub


    Private Sub Button1_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        'Me.CtrlArbolNavD1.mRecurso = mRecurso


        Dim huella As Framework.DatosNegocio.HEDN
        Dim celda As DataGridViewCell = CtrlArbolNavD1.grid.SelectedCells(0)



        huella = CtrlArbolNavD1.grid.Rows(celda.RowIndex).DataBoundItem

        Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Recuperar(huella)

        Me.CtrlArbolNavD1.VincularEntidad(huella.EntidadReferida)



    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        Me.CtrlArbolNavD1.mRecurso = mRecurso


        Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        Dim entidad As Framework.DatosNegocio.IEntidadDN = gi.Recuperar(Me.TextBox1.Text, GetType(MNavegacionDatosTest.Persona))

        Me.CtrlArbolNavD1.VincularEntidad(entidad)
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click

        Dim mndad As New MNavegacionDatosAD.MNDGBD(Me.mRecurso)
        mndad.EliminarTablas()
        mndad.EliminarRelaciones()
        mndad.CrearTablas()

        ' crear las tavals de pruebas

        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(MNavegacionDatosDN.RelacionEntidadesNavDN), Nothing)



        ' introducir los datos de pruebas



    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click

        Me.CtrlArbolNavD1.mRecurso = mRecurso
        Me.CtrlArbolNavD1.VincularTipo(GetType(MNavegacionDatosTest.Concurso))
    End Sub
End Class



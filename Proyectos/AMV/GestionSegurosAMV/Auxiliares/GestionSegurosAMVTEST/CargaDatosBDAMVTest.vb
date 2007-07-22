Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports Framework.DatosNegocio
Imports Framework.LogicaNegocios.Transacciones

<TestClass()> Public Class CargaDatosBDAMVTest
    Dim mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN

#Region "Métodos Test"

    <TestMethod()> Public Sub CargarColaboradoresTEST()

        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            CargarColaboradores()
        End Using

    End Sub

    <TestMethod()> Public Sub CargarPolizasTEST()

        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            CargarPolizas()
        End Using

    End Sub


#End Region

#Region "Métodos privados"

    Private Sub CargarColaboradores()
        Dim ad As New GSAMV.AD.CargaDatosBDAMVAD()
        Dim listaTiposSede As System.Collections.IList
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Using tr As New Transaccion(True)
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            Throw New NotImplementedException
            '  listaTiposSede = gi.RecuperarLista(GetType(FN.Empresas.DN.TipoSedeDN))

            ad.CargaColaboradores(listaTiposSede)

            tr.Confirmar()
        End Using
    End Sub

    Private Sub CargarPolizas()
        Dim ad As New GSAMV.AD.CargaDatosBDAMVAD()
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Using tr As New Transaccion(True)
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)

            ad.CargaPolizas()

            tr.Confirmar()
        End Using
    End Sub


    Private Sub GuardarDatos(ByVal objeto As Object)
        Using tr As New Transaccion
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(objeto)

            tr.Confirmar()

        End Using
    End Sub

    Private Sub ObtenerRecurso()

        Dim connectionstring As String
        Dim htd As New Dictionary(Of String, Object)

        connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd='sa'"
        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GSAMV.AD.GestorMapPersistenciaCamposGSAMV()

    End Sub

#End Region

End Class

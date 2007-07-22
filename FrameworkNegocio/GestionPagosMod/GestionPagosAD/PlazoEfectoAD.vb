Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos
Imports FN.GestionPagos.DN

Public Class PlazoEfectoAD


    Public Function Recuperar(ByVal pCondicionesPago As FN.GestionPagos.DN.CondicionesPagoDN, ByVal pFechaEfecto As Date) As FN.GestionPagos.DN.PlazoEfectoDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)

        Using tr As New Transaccion()




            sql = "SELECT id FROM tlPlazoEfectoDN WHERE  ModalidadDePago=@ModalidadDePago and ((Periodo_FInicio<=@FEfecto and  Periodo_FFinal>=@FEfecto) or (Periodo_FInicio<=@FEfecto and  Periodo_FFinal is null))  "
            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroInteger("ModalidadDePago", pCondicionesPago.ModalidadDePago))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroFecha("FEfecto", pFechaEfecto))

            ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            Dim di As Object = ej.EjecutarEscalar(sql, parametros) ' lo que el deudor debe al acreedor
            Dim gi As New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            Recuperar = gi.Recuperar(Of FN.GestionPagos.DN.PlazoEfectoDN)(di)


            tr.Confirmar()



        End Using
    End Function


End Class

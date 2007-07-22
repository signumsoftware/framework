Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Ficheros.FicherosDN
Public Class RutaAlmacenamientoFicherosAD
    Inherits Framework.AccesoDatos.BaseTransaccionAD
    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

    Public Function RecuperarColRafActivo() As ColRutaAlmacenamientoFicherosDN


        RecuperarColRafActivo = RecuperarColRafxEstado(RutaAlmacenamientoFicherosEstado.Abierta)

       
    End Function
    ''' <summary>
    ''' recupera la coleccion de raf creados (no activos)
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarColRafDisponible() As ColRutaAlmacenamientoFicherosDN

        Return RecuperarColRafxEstado(RutaAlmacenamientoFicherosEstado.Disponible)
    End Function


    Public Function RecuperarColRafxEstado(ByVal pEstado As RutaAlmacenamientoFicherosEstado) As ColRutaAlmacenamientoFicherosDN



        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim col As New ColRutaAlmacenamientoFicherosDN
        'Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


        ' construir la sql y los parametros


        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ProcTl = Me.ObtenerTransaccionDeProceso()

            parametros = New List(Of System.Data.IDataParameter)



            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroInteger("EstadoRAF", pEstado))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))
            sql = "Select ID  from tlRutaAlmacenamientoFicherosDN where EstadoRAF=@EstadoRAF and Baja<>@Baja"


            Dim dts As DataSet
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            dts = ej.EjecutarDataSet(sql, parametros)


            Dim dr As Data.DataRow
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            For Each dr In dts.Tables(0).Rows
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(ProcTl, Me.mRec)
                col.Add(gi.Recuperar(Of RutaAlmacenamientoFicherosDN)(dr(0)))
            Next



            ProcTl.Confirmar()

            Return col

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try
    End Function

End Class

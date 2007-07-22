Imports Framework.LogicaNegocios.Transacciones

Public Class CajonDocumentoAD
    Inherits Framework.AccesoDatos.BaseTransaccionAD
    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub




    Public Function RecuperarColTipoDocumentoRequerido(ByVal colGUIDs As IList(Of String)) As Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN



    End Function
    Public Function RecuperarParesCDyHFVincualbles() As Framework.Ficheros.FicherosDN.ColParCDyHFVincualble






        Using tr As New Transaccion


            Dim sql As String = "select * from vwCDyHFVinculables "
            Dim ej As Framework.AccesoDatos.Ejecutor = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
            Dim dts As DataSet = ej.EjecutarDataSet(sql)

            Dim col As New Framework.Ficheros.FicherosDN.ColParCDyHFVincualble

            If dts.Tables.Count = 1 Then

                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)

                For Each dr As DataRow In dts.Tables(0).Rows
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)

                    Dim par As New Framework.Ficheros.FicherosDN.ParCDyHFVincualble
                    par.HF = gi.Recuperar(Of Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)(dr("idFichero"))
                    par.CD = gi.Recuperar(Of Framework.Ficheros.FicherosDN.CajonDocumentoDN)(dr("id"))
                    col.Add(par)
                Next

            Else
                Throw New Framework.AccesoDatos.ApplicationExceptionAD("numero de tablas inadecuado: " & dts.Tables.Count)
            End If



            RecuperarParesCDyHFVincualbles = col



            tr.Confirmar()

        End Using





    End Function

    Public Function RecuperarDtsCDyHFVincualbles() As DataSet





        Using tr As New Transaccion


            Dim sql As String = "select * from vwCDyHFVinculables "


            Dim ej As Framework.AccesoDatos.Ejecutor = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)

            RecuperarDtsCDyHFVincualbles = ej.EjecutarDataSet(Sql)
            tr.Confirmar()

        End Using





    End Function

    Public Function RecuperarColCajonDocumentoCoincidentes(ByVal colidDoc As Framework.Ficheros.FicherosDN.ColIdentificacionDocumentoDN) As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN


        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim col As New Framework.Ficheros.FicherosDN.ColCajonDocumentoDN


        ' construir la sql y los parametros


        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ProcTl = Me.ObtenerTransaccionDeProceso()

            parametros = New List(Of System.Data.IDataParameter)

            Dim sqlw As String
            Dim a As Int16
            For Each id As Framework.Ficheros.FicherosDN.IdentificacionDocumentoDN In colidDoc
                a += 1
                sqlw = " and idIdentificacionDocumento=@idIdentificacionDocumento" & a
                parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idIdentificacionDocumento" & a, id.ID))
            Next

            If sqlw Is Nothing Then
                Return Nothing
            End If

            sqlw = sqlw.Substring(4)

            'sql = "select id from  vwCajonDocxIdentidadDoc where idTipoDocumento=@idTipoDocumento and Identificacion=@Identificacion"
            sql = "select id from tlCajonDocumentoDN where " & sqlw

            Dim dts As DataSet
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            dts = ej.EjecutarDataSet(sql, parametros)


            Dim dr As Data.DataRow
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            For Each dr In dts.Tables(0).Rows
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(ProcTl, Me.mRec)
                col.Add(gi.Recuperar(Of Framework.Ficheros.FicherosDN.CajonDocumentoDN)(dr(0)))
            Next



            ProcTl.Confirmar()

            Return col

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try


    End Function


End Class




Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Ficheros.FicherosDN

Public Class IdentificacionDocumentoAD
    Inherits Framework.AccesoDatos.BaseTransaccionAD
    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub




    Public Function RecuperarOcrearIdentitific(ByVal pTipoFichero As TipoFicheroDN, ByVal Identificacion As String) As IdentificacionDocumentoDN


        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim col As New Framework.Ficheros.FicherosDN.ColCajonDocumentoDN


        ' construir la sql y los parametros


        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ProcTl = Me.ObtenerTransaccionDeProceso()

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("idTipoFichero", pTipoFichero.ID))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("Identificacion", Identificacion))

            sql = "select id from  tlIdentificacionDocumentoDN where idTipoFichero=@idTipoFichero and Identificacion=@Identificacion"


            Dim dts As DataSet
            Dim ej As Framework.AccesoDatos.Ejecutor
            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            dts = ej.EjecutarDataSet(sql, parametros)


            Select Case dts.Tables(0).Rows.Count

                Case Is = 0
                    Dim id As New IdentificacionDocumentoDN()
                    id.Identificacion = Identificacion
                    id.TipoFichero = pTipoFichero
                    Return id

                Case Is = 1

                    Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(ProcTl, Me.mRec)
                    RecuperarOcrearIdentitific = gi.Recuperar(Of IdentificacionDocumentoDN)(dts.Tables(0).Rows(0)(0))

                Case Is > 1
                    Throw New Framework.AccesoDatos.ApplicationExceptionAD("Error de integridad en la base de datos, maximo una entidad recuperable. Recuperdas: " & dts.Tables(0).Rows.Count)

            End Select




            ProcTl.Confirmar()



        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try


    End Function

End Class

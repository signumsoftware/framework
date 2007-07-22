Imports Framework.LogicaNegocios.Transacciones
Imports Framework.DatosNegocio
Imports Framework.AccesoDatos




Public Class AlertaAD


    Public Function RecuperarDTSNotas(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN)



        Using tr As New Transaccion

            Try



                Dim parametros As List(Of System.Data.IDataParameter)
                Dim sql As String = ConstruirSQL(pEntidad, parametros)


                Dim dts As DataSet
                Dim ej As Framework.AccesoDatos.Ejecutor
                ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)
                dts = ej.EjecutarDataSet(sql, parametros, False)

                RecuperarDTSNotas = dts


            Catch ex As Exception
                tr.Cancelar()
                Throw New Framework.AccesoDatos.ApplicationExceptionAD(ex.Message, ex)

            End Try


            tr.Confirmar()


        End Using

    End Function

    Private Function ConstruirSQL(ByVal pEntidad As Object, ByRef parametros As List(Of System.Data.IDataParameter)) As String


        'Dim InfoTypeInstClase As Framework.TiposYReflexion.DN.InfoTypeInstClaseDN ' esto no debiera estar aqui debiara haber una clase que nombra las clases  a partir de un tipo y debiera estar en ad
        'InfoTypeInstClase = New Framework.TiposYReflexion.DN.InfoTypeInstClaseDN(pEntidad.GetType)


        'Dim nombretalba As String = InfoTypeInstClase.TablaNombre
        'Dim nombreCampoID As String = "id"

        'parametros = New List(Of System.Data.IDataParameter)
        'parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("@ValorId", pEntidad.ID))


        'ConstruirSQL = "SELECT     dbo.vwNotasNotasGUIDentRef.IDnota, dbo.vwNotasNotasGUIDentRef.Nombre, dbo.vwNotasNotasGUIDentRef.Comentario,    dbo.vwNotasNotasGUIDentRef.ToSt " & _
        '                " FROM " & nombretalba & " INNER JOIN  dbo.vwNotasNotasGUIDentRef ON dbo." & nombretalba & " .GUID = dbo.vwNotasNotasGUIDentRef.GUIDReferida " & _
        '                " WHERE  (" & nombretalba & "." & nombreCampoID & " = @ValorId )"



    End Function


End Class

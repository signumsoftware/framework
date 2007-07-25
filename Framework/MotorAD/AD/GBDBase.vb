Public MustInherit Class GBDBase
    Public Shared mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = Nothing

    Public Shared llamadoEliminarVistas As Boolean
    Public Shared llamadoEliminarTablas As Boolean
    Public Shared llamadoEliminarRelaciones As Boolean
    'Public MustOverride Sub RegistrarNavegacionEnsamblado()

    Public Overridable Sub EliminarVistas()

        If llamadoEliminarVistas Then
            Return
        End If
        llamadoEliminarVistas = True


        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim ds As Data.DataSet


        Dim dr As Data.DataRow
        Dim nombretabla As String
        Dim eliminables As Int16
        Dim vueltas As Int16

        Dim sqlElim As String

        Do
            vueltas += 1
            eliminables = 0

            ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
            ds = ej.EjecutarDataSet("SELECT name FROM sysobjects WHERE xtype = 'V'")

            For Each dr In ds.Tables(0).Rows
                nombretabla = dr("name")
                If nombretabla.Substring(0, 2) = "vw" OrElse nombretabla.Substring(0, 2) = "uw" OrElse nombretabla.Substring(0, 2) = "vu" Then
                    eliminables += 1
                    ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
                    Try
                        sqlElim = "Drop view " & nombretabla
                        ej.EjecutarNoConsulta(sqlElim)

                    Catch ex As Exception

                    End Try
                End If
            Next

        Loop Until eliminables = 0 OrElse vueltas > 20


    End Sub

    Public Overridable Sub EliminarTablas()


        If llamadoEliminarTablas Then
            Return
        End If
        llamadoEliminarTablas = True

        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim ds As Data.DataSet


        Dim dr As Data.DataRow
        Dim nombretabla As String
        Dim eliminables As Int16
        Dim vueltas As Int16

        Dim sqlElim As String
        EliminarRelaciones()

        Do
            vueltas += 1
            eliminables = 0

            ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
            ds = ej.EjecutarDataSet("SELECT name FROM sysobjects WHERE xtype = 'U'")

            For Each dr In ds.Tables(0).Rows
                nombretabla = dr("name")
                If nombretabla.Substring(0, 2) = "tl" OrElse nombretabla.Substring(0, 2) = "tr" OrElse nombretabla.Substring(0, 2) = "th" Then
                    eliminables += 1
                    ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
                    Try
                        sqlElim = "Drop Table " & nombretabla
                        ej.EjecutarNoConsulta(sqlElim)

                    Catch ex As Exception

                    End Try
                End If
            Next

        Loop Until eliminables = 0 OrElse vueltas > 20


    End Sub

    Public Overridable Sub EliminarRelaciones()

        If llamadoEliminarRelaciones Then
            Return
        End If
        llamadoEliminarRelaciones = True

        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim ds, dtsTabla As Data.DataSet


        Dim dr As Data.DataRow
        Dim nombretabla, idTablaPadre, NombreRelacion As String
        Dim eliminables As Int16
        Dim vueltas As Int16

        Dim sqlElim As String

        Do
            vueltas += 1
            eliminables = 0

            ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
            ds = ej.EjecutarDataSet("SELECT * FROM sysobjects WHERE xtype = 'F'") ' recupero todas las relaciones externas FK

            For Each dr In ds.Tables(0).Rows
                NombreRelacion = dr("name")
                idTablaPadre = dr("parent_obj")
                ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
                dtsTabla = ej.EjecutarDataSet("SELECT * FROM sysobjects WHERE id='" & idTablaPadre & "'") ' recupero todas las relaciones externas FK
                nombretabla = dtsTabla.Tables(0).Rows(0)("name")

                If nombretabla.Substring(0, 2) = "tl" OrElse nombretabla.Substring(0, 2) = "tr" Then
                    eliminables += 1
                    ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
                    Try
                        sqlElim = "ALTER TABLE " & nombretabla & " DROP CONSTRAINT  " & NombreRelacion
                        'Debug.WriteLine(sqlElim)
                        ej.EjecutarNoConsulta(sqlElim)
                        'Debug.WriteLine("OK")
                    Catch ex As Exception
                        Debug.WriteLine("FALLO")
                    End Try
                End If
            Next

        Loop Until eliminables = 0 OrElse vueltas > 20


    End Sub


    Public MustOverride Sub CrearTablas()
    'Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

    'Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

    '' procesos

    'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
    'gi.GenerarTablas2(GetType(TipoEntidadPruebaDN), Nothing)

    Public MustOverride Sub CrearVistas()


End Class

Imports System.Diagnostics
Imports System.Collections




Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.DatosNegocio
Imports Framework
Imports Framework.TiposYReflexion.DN

Imports Framework.LogicaNegocios.Transacciones



Public Class gbd
    Inherits Framework.AccesoDatos.MotorAD.GBDBase
    ' Public Shared mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = Nothing


    Public Shared llamadoCrearTablas As Boolean
    Public Shared llamadoCrearVistas As Boolean



    Public Sub New(ByVal pRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        If pRecurso Is Nothing Then
            Dim connectionstring As String
            Dim htd As New Generic.Dictionary(Of String, Object)

            connectionstring = "server=localhost;database=sspruebasft;user=sa;pwd=''"
            htd.Add("connectionstring", connectionstring)
            mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)
            ' Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaCamposPruebasMotorTest()

        Else
            mRecurso = pRecurso
        End If
    End Sub


    Public Shared Sub EliminarTablas()

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

            ej = New Framework.AccesoDatos.Ejecutor(Nothing, mrecurso)
            ds = ej.EjecutarDataSet("SELECT name FROM sysobjects WHERE xtype = 'U'")

            For Each dr In ds.Tables(0).Rows
                nombretabla = dr("name")
                If nombretabla.Substring(0, 2) = "tl" OrElse nombretabla.Substring(0, 2) = "tr" Then
                    eliminables += 1
                    ej = New Framework.AccesoDatos.Ejecutor(Nothing, mrecurso)
                    Try
                        sqlElim = "Drop Table " & nombretabla
                        ej.EjecutarNoConsulta(sqlElim)

                    Catch ex As Exception

                    End Try
                End If
            Next

        Loop Until eliminables = 0 OrElse vueltas > 20


    End Sub



    Public Shared Sub EliminarRelaciones()

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
                        Debug.WriteLine(sqlElim)
                        ej.EjecutarNoConsulta(sqlElim)
                        Debug.WriteLine("OK")
                    Catch ex As Exception
                        Debug.WriteLine("FALLO")
                    End Try
                End If
            Next

        Loop Until eliminables = 0 OrElse vueltas > 20


    End Sub

    'Public Shared Sub CrearTablas()
    '    Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

    '    Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

    '    ' procesos

    '    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
    '    gi.GenerarTablas2(GetType(TipoEntidadPruebaDN), Nothing)

    '    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
    '    gi.GenerarTablas2(GetType(MuchosEntidadpDN), Nothing)

    '    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
    '    gi.GenerarTablas2(GetType(ContenedoraHuellaDN), Nothing)



    'End Sub


    Public Overrides Sub CrearTablas()


        'Dim tyrgbd As New Framework.TiposYReflexion.AD.tiposYReflexionGBDAD
        'tyrgbd.mRecurso = Me.mRecurso
        'tyrgbd.CrearTablas()


        If llamadoCrearTablas Then
            Return
        End If
        llamadoCrearTablas = True



        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        ' procesos

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(TipoEntidadPruebaDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(MuchosEntidadpDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(ContenedoraHuellaDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(ContenedoraHtEntidadpDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(EntidadRefCircular1), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(EntidadRefCircular2), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(ContenedoraHuellaTipadaInterfaceA), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(implemtaC1InterfaceA), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(implemtaC2InterfaceA), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(ContenedoraHEDNContenidaDN), Nothing)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Persona), Nothing)

    End Sub

    Public Overrides Sub CrearVistas()


        'Dim tyrgbd As New Framework.TiposYReflexion.AD.tiposYReflexionGBDAD
        'tyrgbd.mRecurso = Me.mRecurso
        'tyrgbd.CrearVistas()


        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True




        Dim ej As Framework.AccesoDatos.Ejecutor




        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwHuellaEntidadNoTipada)





    End Sub




End Class





Public Class GestorMapPersistenciaCamposPruebasMotorTest
    Inherits GestorMapPersistenciaCamposLN

    'TODO: ALEX por implementar: recuperar el mapeado de instanciacion de la fuente de datos para el tipo a procesar
    Public Overrides Function RecuperarMapPersistenciaCampos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As InfoDatosMapInstClaseDN = Nothing
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        mapinst = RecuperarMapPersistenciaCamposPrivado(pTipo)

        ' ojo esta modificación se debe aplicar siempre si el tipo hereda de una huella es decir en el metodo que lo llamo
        If (TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo)) Then
            If mapinst Is Nothing Then
                mapinst = New InfoDatosMapInstClaseDN
            End If
            Me.MapearCampoSimple(mapinst, "mEntidadReferidaHuella", CampoAtributoDN.SoloGuardarYNoReferido)
        End If


        If TiposYReflexion.LN.InstanciacionReflexionHelperLN.HeredaDe(pTipo, GetType(Framework.DatosNegocio.EntidadTemporalDN)) Then
            If mapinst Is Nothing Then
                mapinst = New InfoDatosMapInstClaseDN
            End If

            Dim mapSubInst As New InfoDatosMapInstClaseDN
            ' mapeado de la clase referida por el campo
            mapSubInst.NombreCompleto = "Framework.DatosNegocio.EntidadTemporalDN"
            ParametrosGeneralesNoProcesar(mapSubInst)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mPeriodo"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.MapSubEntidad = mapSubInst

        End If


        Return mapinst
    End Function



    Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If (pTipo Is GetType(InterfaceA)) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(implemtaC1InterfaceA)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            alentidades.Add(New VinculoClaseDN(GetType(implemtaC1InterfaceA)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            Return mapinst
        End If




        If (pTipo Is GetType(ContenedoraHEDNContenidaDN)) Then



            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mHEDN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

            Return mapinst
        End If






        Return Nothing
    End Function


    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class



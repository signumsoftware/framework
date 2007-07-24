Public Class UsuariosGBDAD
    Inherits Framework.AccesoDatos.MotorAD.GBDBase

    Public Shared llamadoCrearTablas As Boolean
    Public Shared llamadoCrearVistas As Boolean


#Region "Constructor"

    Public Sub New(ByVal recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        mRecurso = recurso
    End Sub

#End Region


    Public Overrides Sub CrearTablas()



        If llamadoCrearTablas Then
            Return
        End If
        llamadoCrearTablas = True


        Dim gbdBase As Framework.AccesoDatos.MotorAD.GBDBase

        gbdBase = New Framework.TiposYReflexion.AD.tiposYReflexionGBDAD
        gbdBase.mRecurso = Me.mRecurso
        gbdBase.CrearTablas()

        gbdBase = New Framework.Procesos.ProcesosAD.OperacionesGBDAD
        gbdBase.mRecurso = Me.mRecurso
        gbdBase.CrearTablas()


        gbdBase = New Framework.Notas.NotasAD.NotasGBDAD(mRecurso)
        '     gbdBase.mRecurso = Me.mRecurso
        gbdBase.CrearTablas()

        gbdBase = New MNavegacionDatosAD.MNDGBD(mRecurso)
        gbdBase.CrearTablas()


        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()




        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Usuarios.DN.DatosIdentidadDN), Nothing)



        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Usuarios.DN.PrincipalDN), Nothing)


        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(Framework.Usuarios.DN.PrincipalDN), Nothing)




        ' POrueba de combos -------------------------
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(PruebaTipoaCombo), Nothing)
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(ContenedorPruebaTipoaCombo), Nothing)



    End Sub

    Public Overrides Sub CrearVistas()


        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True

        Dim gbdBase As Framework.AccesoDatos.MotorAD.GBDBase

        gbdBase = New Framework.TiposYReflexion.AD.tiposYReflexionGBDAD
        gbdBase.mRecurso = Me.mRecurso
        gbdBase.CrearVistas()


        gbdBase = New Framework.Procesos.ProcesosAD.OperacionesGBDAD
        gbdBase.mRecurso = Me.mRecurso
        gbdBase.CrearVistas()

        gbdBase = New MNavegacionDatosAD.MNDGBD(mRecurso)
        gbdBase.CrearVistas()


        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwMetodosSistema)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwDatosIdentidad)


    End Sub
End Class

<Serializable()> Public Class ContenedorPruebaTipoaCombo
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mPruebaTipoaComboj As PruebaTipoaCombo
    Public Property PruebaTipoaComboj() As PruebaTipoaCombo
        Get
            Return mPruebaTipoaComboj
        End Get
        Set(ByVal value As PruebaTipoaCombo)
            CambiarValorRef(Of PruebaTipoaCombo)(value, mPruebaTipoaComboj)
        End Set
    End Property

End Class

<Serializable()> Public Class PruebaTipoaCombo

    Inherits Framework.DatosNegocio.EntidadDN



    Protected mPruebaTipoaComboj As PruebaTipoaCombo
    Protected mEnumaracionPrueba As EnumaracionPruebaEnum

    Public Property EnumaracionPrueba() As EnumaracionPruebaEnum

        Get
            Return mEnumaracionPrueba
        End Get

        Set(ByVal value As EnumaracionPruebaEnum)
            CambiarValorVal(Of EnumaracionPruebaEnum)(value, mEnumaracionPrueba)

        End Set
    End Property



    Public Property PruebaTipoaComboj() As PruebaTipoaCombo
        Get
            Return mPruebaTipoaComboj
        End Get
        Set(ByVal value As PruebaTipoaCombo)
            CambiarValorRef(Of PruebaTipoaCombo)(value, mPruebaTipoaComboj)
        End Set
    End Property








End Class





Public Enum EnumaracionPruebaEnum
    uno
    dos
    tres
    cuatro
End Enum

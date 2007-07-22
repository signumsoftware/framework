
Imports System.Collections
Public Class MNDGBD

    Inherits Framework.AccesoDatos.MotorAD.GBDBase


    Public Shared llamadoCrearTablas As Boolean
    Public Shared llamadoCrearVistas As Boolean


    Public Sub New(ByVal pRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN)


        If pRecurso Is Nothing Then
            Dim connectionstring As String
            Dim htd As New Generic.Dictionary(Of String, Object)

            connectionstring = "server=localhost;database=sspruebasft;user=sa;pwd=''"
            htd.Add("connectionstring", connectionstring)
            mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        Else
            mRecurso = pRecurso
        End If


        '  Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaCamposAMVDocsEntrantesLN

    End Sub


    Public Overrides Sub CrearTablas()

        If llamadoCrearTablas Then
            Return
        End If
        llamadoCrearTablas = True

        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.GenerarTablas2(GetType(MNavegacionDatosDN.RelacionEntidadesNavDN), Nothing)

    End Sub

    Public Overrides Sub CrearVistas()


        If llamadoCrearVistas Then
            Return
        End If
        llamadoCrearVistas = True

        ' Dim gi As Framework.AccesoDatos.BaseTransaccionAD


        'Dim vista As String
        'vista = "CREATE VIEW dbo.vwEntidadesMNavD AS SELECT     dbo.tlEntidadNavDN.ID, dbo.tlVinculoClaseDN.NombreEnsamblado, dbo.tlVinculoClaseDN.NombreClase FROM         dbo.tlEntidadNavDN INNER JOIN     dbo.tlVinculoClaseDN ON dbo.tlEntidadNavDN.idVinculoClase = dbo.tlVinculoClaseDN.ID"

        'vista = "CREATE VIEW dbo.vwRelacionesxTipo AS SELECT     dbo.tlRelacionEntidadesNavDN.ID, dbo.tlRelacionEntidadesNavDN.idEntidadDatosOrigen, dbo.tlRelacionEntidadesNavDN.idEntidadDatosDestino,  vwEntidadesMNavD_1.NombreEnsamblado AS NEO, vwEntidadesMNavD_1.NombreClase AS NCO,   dbo.vwEntidadesMNavD.NombreEnsamblado AS NED, dbo.vwEntidadesMNavD.NombreClase AS NCD FROM         dbo.tlRelacionEntidadesNavDN INNER JOIN   dbo.vwEntidadesMNavD vwEntidadesMNavD_1 ON dbo.tlRelacionEntidadesNavDN.idEntidadDatosOrigen = vwEntidadesMNavD_1.ID INNER JOIN dbo.vwEntidadesMNavD ON dbo.tlRelacionEntidadesNavDN.idEntidadDatosDestino = dbo.vwEntidadesMNavD.ID "


        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwEntidadesMNavD)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.vwRelacionesxTipo)

        ej = New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)
        ej.EjecutarNoConsulta(My.Resources.uwNavDatRutas)


    End Sub





    'Public Overrides Sub RegistrarNavegacionEnsamblado()


    'End Sub
End Class

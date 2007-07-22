Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos
Imports Framework.Usuarios.DN

Public Class UsuariosAD
    Inherits Framework.AccesoDatos.BaseTransaccionAD

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub


    ''' <summary>
    ''' Es te metodo devuelve el objeto principal asociado a unos datos de identidad
    ''' tine una minima logíca de comparacion de los valores has de los 
    ''' 1º recupera el didel sistema con el mismo nick que el di suministrado
    ''' 2º verifica la coincidencia de los hases de clave
    ''' 3º recupera el principal asociado a el di
    ''' </summary>
    ''' <param name="di"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function ObtenerPrincipal(ByVal di As DatosIdentidadDN) As PrincipalDN
        Dim ej As Framework.AccesoDatos.Ejecutor

        Dim idPrincipal, idDatosIdentidad As String
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim parametro As Data.SqlClient.SqlParameter
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Dim diSistema As DatosIdentidadDN

        ' construir la sql y los parametros

        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try
            ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
            ctd.IniciarTransaccion(Me.mTL, ProcTl)

            parametros = New List(Of System.Data.IDataParameter)

            parametro = New Data.SqlClient.SqlParameter("@Nick", SqlDbType.NVarChar)
            parametro.Value = di.Nick
            parametros.Add(parametro)

            sql = "Select id  from tlDatosIdentidadDN where Nick=@Nick "

            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            idDatosIdentidad = ej.EjecutarEscalar(sql, parametros)

            If idDatosIdentidad Is Nothing OrElse idDatosIdentidad = "" Then
                Return Nothing
            End If

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(ProcTl, Me.mRec)
            diSistema = gi.Recuperar(idDatosIdentidad, GetType(DatosIdentidadDN))

            ' comparamos los dos di
            If Not diSistema.ValidarClave(di.HashClave) Then
                Return Nothing
            End If

            ' construir la sql y los parametros

            parametros = New List(Of System.Data.IDataParameter)

            parametro = New Data.SqlClient.SqlParameter("@Nick", SqlDbType.NVarChar)
            parametro.Value = di.Nick
            parametros.Add(parametro)

            parametro = New Data.SqlClient.SqlParameter("@Baja", SqlDbType.NVarChar)
            parametro.Value = 0
            parametros.Add(parametro)

            sql = "Select IDPrincipal from vwDatosIdentidad where Nick=@Nick and Baja=@Baja"

            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            idPrincipal = ej.EjecutarEscalar(sql, parametros)

            If idPrincipal Is Nothing OrElse idPrincipal = "" Then
                Return Nothing
            End If


            '2º recuperamos el principal

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(ProcTl, Me.mRec)

            ObtenerPrincipal = gi.Recuperar(idPrincipal, GetType(PrincipalDN))
            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw
        End Try

    End Function

    Public Function RecuperarListado() As DataSet
        Dim ej As Framework.AccesoDatos.Ejecutor

        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter) = Nothing

        ' construir la sql y los parametros

        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try
            ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
            ctd.IniciarTransaccion(Me.mTL, ProcTl)

            ' parametros = New List(Of System.Data.IDataParameter)

            sql = "Select  *  from vwUsuariosxEntidadRef "

            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            RecuperarListado = ej.EjecutarDataSet(sql, parametros)
            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw
        Finally

        End Try

    End Function

    Public Sub BorrarDatosIdentidad(ByVal pNick As String)
        Dim ej As Framework.AccesoDatos.Ejecutor

        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim parametro As Data.SqlClient.SqlParameter

        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try
            ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
            ctd.IniciarTransaccion(Me.mTL, ProcTl)

            ' construir la sql y los parametros
            parametros = New List(Of System.Data.IDataParameter)

            parametro = New Data.SqlClient.SqlParameter("@Nick", SqlDbType.NVarChar)
            parametro.Value = pNick
            parametros.Add(parametro)

            sql = "delete tlDatosIdentidadDN where Nick=@Nick "

            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            ej.EjecutarNoConsulta(sql, parametros)

            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw
        End Try
    End Sub

    Public Function RecuperarDatosIdentidad(ByVal pNick As String) As DatosIdentidadDN
        Dim ej As Framework.AccesoDatos.Ejecutor

        Dim idDatosIdentidad As String
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim parametro As Data.SqlClient.SqlParameter

        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try
            ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
            ctd.IniciarTransaccion(Me.mTL, ProcTl)

            ' construir la sql y los parametros
            parametros = New List(Of System.Data.IDataParameter)

            parametro = New Data.SqlClient.SqlParameter("@Nick", SqlDbType.NVarChar)
            parametro.Value = pNick
            parametros.Add(parametro)

            sql = "Select ID from tlDatosIdentidadDN where Nick=@Nick "

            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            idDatosIdentidad = ej.EjecutarEscalar(sql, parametros)

            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(ProcTl, Me.mRec)
            RecuperarDatosIdentidad = gi.Recuperar(Of DatosIdentidadDN)(idDatosIdentidad)


            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw
        End Try
    End Function

    Public Function ExisteNickDatosIdentidad(ByVal pNick As String) As Boolean
        Dim ej As Framework.AccesoDatos.Ejecutor

        Dim idDatosIdentidad As String
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim parametro As Data.SqlClient.SqlParameter

        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try
            ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
            ctd.IniciarTransaccion(Me.mTL, ProcTl)

            ' construir la sql y los parametros
            parametros = New List(Of System.Data.IDataParameter)

            parametro = New Data.SqlClient.SqlParameter("@Nick", SqlDbType.NVarChar)
            parametro.Value = pNick
            parametros.Add(parametro)

            sql = "Select ID from tlDatosIdentidadDN where Nick=@Nick "

            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            idDatosIdentidad = ej.EjecutarEscalar(sql, parametros)

            If idDatosIdentidad Is Nothing OrElse idDatosIdentidad = "" Then
                ExisteNickDatosIdentidad = False
            Else
                ExisteNickDatosIdentidad = True
            End If

            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw
        End Try
    End Function

    Public Function RecuperarPrincipalxNick(ByVal pNick As String) As PrincipalDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim idPrincipal As String
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim parametro As Data.SqlClient.SqlParameter
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso()

            ' construir la sql y los parametros

            parametros = New List(Of System.Data.IDataParameter)

            parametro = New Data.SqlClient.SqlParameter("@Nick", SqlDbType.NVarChar)
            parametro.Value = pNick
            parametros.Add(parametro)

            parametro = New Data.SqlClient.SqlParameter("@Baja", SqlDbType.NVarChar)
            parametro.Value = 0
            parametros.Add(parametro)

            sql = "Select IDPrincipal from vwDatosIdentidad where Nick=@Nick and Baja=@Baja"

            ej = New Framework.AccesoDatos.Ejecutor(tlproc, Me.mRec)
            idPrincipal = ej.EjecutarEscalar(sql, parametros)

            '2º recuperamos el principal
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, Me.mRec)
            RecuperarPrincipalxNick = gi.Recuperar(idPrincipal, GetType(PrincipalDN))

            tlproc.Confirmar()

        Catch ex As Exception
            If tlproc IsNot Nothing Then
                tlproc.Cancelar()
            End If
            Throw
        End Try
    End Function

    Public Function RecuperarPrincipalxEntidadUser(ByVal tipoEnt As System.Type, ByVal idEntidad As String) As PrincipalDN
        Dim ej As Framework.AccesoDatos.Ejecutor
        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter)
        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing
        Dim principal As PrincipalDN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso()

            sql = "Select ID from vwUsuarioxEntidadUser where TipoEntidadUser=@tipoEntidad and IdEntidadReferida=@IdEntidad and Baja<>@Baja"

            parametros = New List(Of System.Data.IDataParameter)
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroString("tipoEntidad", tipoEnt.FullName))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroID("IdEntidad", idEntidad))
            parametros.Add(Framework.AccesoDatos.ParametrosConstAD.ConstParametroBoolean("Baja", True))

            ej = New Framework.AccesoDatos.Ejecutor(tlproc, mRec)

            Dim dts As DataSet
            dts = ej.EjecutarDataSet(sql, parametros)

            If dts.Tables(0).Rows.Count > 1 Then
                Throw New ApplicationExceptionAD("Error de integridad de la base de datos, no puede existir más de un usuario para la misma entidad")
            ElseIf dts.Tables(0).Rows.Count = 1 Then
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(tlproc, mRec)
                principal = gi.Recuperar(Of PrincipalDN)(dts.Tables(0).Rows(0)(0))
            End If

            tlproc.Confirmar()

            Return principal

        Catch ex As Exception
            If tlproc IsNot Nothing Then
                tlproc.Cancelar()
            End If
            Throw
        End Try

    End Function

End Class

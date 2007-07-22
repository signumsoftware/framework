#Region "Importaciones"

Imports System.Collections.Generic

Imports Framework.AccesoDatos
Imports Framework.DatosNegocio
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos.MotorAD.DN
Imports Framework.TiposYReflexion.DN


#End Region

Namespace AD
    Public Class AccesorMotorAD
        Inherits BaseTransaccionAD
        Implements IBaseAccesorMotorAD




#Region "Atributos"
        Protected mConstructor As IConstructorBusquedaAD
#End Region

#Region "Constructores"
        Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN, ByVal pConstructor As IConstructorBusquedaAD)
            MyBase.New(pTL, pRec)

            If (pConstructor Is Nothing) Then
                Throw New ApplicationException("Error: el constructor no puede ser nulo.")
            End If

            mConstructor = pConstructor
        End Sub
#End Region

#Region "Metodos"



        'Public Function ConstDTSRelInversa(ByVal pSqlBuscador As String, ByVal pObjeto As InfoTypeInstClaseDN, ByVal pInfoReferido As InfoTypeInstClaseDN, ByVal PropiedadDeInstanciaDN As TiposYReflexion.DN.PropiedadDeInstanciaDN, ByRef pParametros As List(Of IDataParameter), ByVal pID As String) As DataSet
        Public Function ModificarSQLRelInversa(ByVal pSqlBuscador As String, ByVal pObjeto As InfoTypeInstClaseDN, ByVal pInfoReferido As InfoTypeInstClaseDN, ByVal PropiedadDeInstanciaDN As TiposYReflexion.DN.PropiedadDeInstanciaDN, ByRef pParametros As List(Of IDataParameter), ByVal pID As String, ByVal pGUID As String) As String


            Dim cosnt As Framework.AccesoDatos.MotorAD.AD.ConstructorSQLSQLsAD = mConstructor

            Dim sql As String = cosnt.ConstSqlSelectInversa(pObjeto, pInfoReferido, PropiedadDeInstanciaDN, pParametros, pID, pGUID)

            Dim sqlEnlazadaAvista As String = JoinSqls(pSqlBuscador, sql)

            '  Dim tipoEntidad As System.Type = pObjeto.Tipo


            'Dim ej As New Framework.AccesoDatos.Ejecutor(Me.mTL, Me.mRec)
            'ConstDTSRelInversa = ej.EjecutarDataSet(sqlEnlazadaAvista, pParametros)

            Return sqlEnlazadaAvista



        End Function



        Private Function JoinSqls(ByVal psqlDatos As String, ByVal pSqlFiltro As String) As String


            If psqlDatos.ToLower.Contains("join") Then


                Dim separadores() As String = {"inner"}
                Dim partessq() As String = psqlDatos.ToLower.Split(separadores, StringSplitOptions.None)
                Dim ParteSeleccion As String = partessq(0)
                separadores(0) = "where"
                Dim JoinOriginal As String = " inner " & partessq(1).Split(separadores, StringSplitOptions.None)(0)
                Dim ParteOriginalWhere As String = ObtenerParteWereSqlSelectFormWhere(psqlDatos)
                Dim nombreTablaDatos As String = ObtenernombreTablaSqlSelectForm(ParteSeleccion)
                Dim JoinFiltroEntidad As String = " inner join ( " & pSqlFiltro & " ) t5645_67981 on t5645_67981.id=" & nombreTablaDatos & ".id "






                JoinSqls = ParteSeleccion & " " & JoinOriginal & " " & JoinFiltroEntidad & " where " & ParteOriginalWhere

            Else
                Dim separadores() As String = {"where"}


                Dim partessq() As String = psqlDatos.ToLower.Split(separadores, StringSplitOptions.None)
                If partessq.Length > 2 Then
                    Throw New Framework.AccesoDatos.ApplicationExceptionAD("division de la sql incorrecta")
                End If

                separadores(0) = "from"
                Dim partesSQLDATOS As String() = partessq(0).ToLower.Split(separadores, StringSplitOptions.None)
                Dim nombreTablaDatos As String = partesSQLDATOS(partesSQLDATOS.Length - 1)
                JoinSqls = partessq(0) & " inner join ( " & pSqlFiltro & " ) t5645_67981 on t5645_67981.id=" & nombreTablaDatos & ".id"


                If partessq.Length > 1 Then
                    JoinSqls += " where " & partessq(1)
                End If
            End If


        End Function

        Public Shared Function ObtenerNombreTablaSqlCONSTRAINT(ByVal psql As String) As String
            Dim posicion As Integer = psql.ToUpper.IndexOf("CONSTRAINT")
            Dim resto As String = psql.Substring(posicion + 10).Trim
            Dim posicionParentesis As Integer = resto.Trim.IndexOf(" ")
            Dim nombreTabla As String = resto.Substring(0, posicionParentesis)

            Return nombreTabla.Trim

        End Function

        Public Shared Function ObtenerNombreTablaSqlCreateTable(ByVal psql As String) As String
            Dim posicion As Integer = psql.ToLower.IndexOf("table")
            Dim resto As String = psql.Substring(posicion + 5)
            Dim posicionParentesis As Integer = resto.ToLower.IndexOf("(")
            Dim nombreTabla As String = resto.Substring(0, posicionParentesis - 1)

            Return nombreTabla.Trim

        End Function
        Public Shared Function ObtenerNombreTablaSqlSelectForm(ByVal psql As String) As String
            Dim separadores() As String = {"from"}
            Dim partessq() As String = psql.ToLower.Split(separadores, StringSplitOptions.None)
            Return partessq(1)
        End Function


        Private Function ObtenerParteWereSqlSelectFormWhere(ByVal psql As String) As String
            Dim separadores() As String = {"where"}
            Dim partessq() As String = psql.ToLower.Split(separadores, StringSplitOptions.None)
            Return partessq(1)
        End Function

        Public Function ConstColHuellasRelInversa(ByVal pObjeto As InfoTypeInstClaseDN, ByVal pInfoReferido As InfoTypeInstClaseDN, ByVal PropiedadDeInstanciaDN As TiposYReflexion.DN.PropiedadDeInstanciaDN, ByRef pParametros As List(Of IDataParameter), ByVal pID As String, ByVal pGUID As String) As Framework.DatosNegocio.ColIHuellaEntidadDN


            Dim cosnt As Framework.AccesoDatos.MotorAD.AD.ConstructorSQLSQLsAD = mConstructor

            Dim sql As String = cosnt.ConstSqlSelectInversa(pObjeto, pInfoReferido, PropiedadDeInstanciaDN, pParametros, pID, pGUID)

            Dim tipoEntidad As System.Type = pObjeto.Tipo


            Dim ej As New Framework.AccesoDatos.Ejecutor(Me.mTL, Me.mRec)
            Dim dt As DataSet = ej.EjecutarDataSet(sql, pParametros)


            ConstColHuellasRelInversa = New Framework.DatosNegocio.ColIHuellaEntidadDN



            For Each fila As DataRow In dt.Tables(0).Rows
                ConstColHuellasRelInversa.Add(New Framework.DatosNegocio.HEDN(tipoEntidad, fila.Item(0), fila.Item(1)))
            Next



        End Function


        Public Function ConstColHuellasRelDirecta(ByVal pObjeto As InfoTypeInstClaseDN, ByVal pInfoReferido As InfoTypeInstClaseDN, ByVal PropiedadDeInstanciaDN As TiposYReflexion.DN.PropiedadDeInstanciaDN, ByRef pParametros As List(Of IDataParameter), ByVal pID As String) As Framework.DatosNegocio.ColIHuellaEntidadDN


            Dim cosnt As Framework.AccesoDatos.MotorAD.AD.ConstructorSQLSQLsAD = mConstructor

            Dim sql As String = cosnt.ConstSqlSelectDirecta(pObjeto, pInfoReferido, PropiedadDeInstanciaDN, pParametros, pID)

            Dim tipoEntidad As System.Type = pInfoReferido.Tipo


            Dim ej As New Framework.AccesoDatos.Ejecutor(Me.mTL, Me.mRec)
            Dim dt As DataSet = ej.EjecutarDataSet(sql, pParametros)


            ConstColHuellasRelDirecta = New Framework.DatosNegocio.ColIHuellaEntidadDN



            For Each fila As DataRow In dt.Tables(0).Rows
                ConstColHuellasRelDirecta.Add(New Framework.DatosNegocio.HEDN(tipoEntidad, fila.Item(0), fila.Item(1)))
            Next



        End Function
        Public Function BuscarGenerico(ByRef pDS As DataSet) As DataSet
            Me.BuscarGenerico(pDS, True)

            Return pDS
        End Function

        Public Function BuscarGenerico(ByRef pDS As DataSet, ByVal pUsarEsquema As Boolean) As DataSet
            Dim sql As String
            Dim ej As Ejecutor
            Dim colparam As New List(Of IDataParameter)

            'Construimos la sql
            sql = mConstructor.ConstruirSQLBusqueda(colparam)

            'Ejecutamos la sql para recuperar los datos
            ej = New Ejecutor(Me.mTL, Me.mRec)
            ej.EjecutarDataSet(sql, colparam, pDS, pUsarEsquema)

            Return pDS
        End Function

        Public Function BuscarGenerico(ByVal pNombreVistaVis As String, ByVal pNombreVistaFiltro As String, ByVal pFiltro As List(Of CondicionRelacionalDN), ByRef pDS As DataSet) As DataSet
            Dim sql As String
            Dim ej As Ejecutor
            Dim colparam As New List(Of IDataParameter)

            'Construimos la sql
            sql = mConstructor.ConstruirSQLBusqueda(pNombreVistaVis, pNombreVistaFiltro, pFiltro, colparam)

            'Ejecutamos la sql para recuperar los datos
            ej = New Ejecutor(Me.mTL, Me.mRec)
            ej.EjecutarDataSet(sql, colparam, pDS)

            Return pDS
        End Function

        Public Function BuscarGenerico(ByVal pNombreVista As String, ByVal pFiltro As List(Of CondicionRelacionalDN), ByRef pDS As DataSet) As DataSet
            Dim sql As String
            Dim ej As Ejecutor
            Dim colparam As New List(Of IDataParameter)

            'Construimos la sql
            sql = mConstructor.ConstruirSQLBusqueda(String.Empty, pNombreVista, pFiltro, colparam)

            'Ejecutamos la sql para recuperar los datos
            ej = New Ejecutor(Me.mTL, Me.mRec)
            ej.EjecutarDataSet(sql, colparam, pDS)

            Return pDS
        End Function

        Public Function BuscarGenericoIDS(ByVal ptypo As System.Type) As ArrayList
            Dim sql As String
            Dim ej As Ejecutor
            Dim datos As ArrayList
            Dim colparam As New List(Of IDataParameter)

            'Construimos la sql
            sql = mConstructor.ConstruirSQLBusqueda(colparam)

            'Ejecutamos la sql para recuperar los datos
            ej = New Ejecutor(Me.mTL, Me.mRec)
            datos = ej.EjecutarRecuperarDatos(sql, colparam, New ConstructorAL(ptypo))

            Return datos
        End Function

        Public Function BuscarGenericoIDS(ByVal pNombreVista As String, ByVal pFiltro As List(Of CondicionRelacionalDN)) As ArrayList
            Dim sql As String
            Dim ej As Ejecutor
            Dim datos As ArrayList
            Dim colparam As New List(Of IDataParameter)

            'Construimos la sql
            sql = mConstructor.ConstruirSQLBusqueda(pNombreVista, String.Empty, pFiltro, colparam)

            'Ejecutamos la sql para recuperar los datos
            ej = New Ejecutor(Me.mTL, Me.mRec)
            ' datos = ej.EjecutarRecuperarDatos(sql, colparam, New ConstructorAL)
            datos = ej.EjecutarRecuperarDatos(sql, colparam, New ConstructorSQLSQLsAD)

            Return datos
        End Function
        Public Function BuscarGenericoIDS(ByVal ptypo As System.Type, ByVal pFiltro As List(Of CondicionRelacionalDN)) As ArrayList
            Dim sql As String
            Dim ej As Ejecutor
            Dim datos As ArrayList
            Dim colparam As New List(Of IDataParameter)









            'Construimos la sql
            sql = mConstructor.ConstruirSQLBusqueda(ptypo, String.Empty, pFiltro, colparam)

            'Ejecutamos la sql para recuperar los datos
            ej = New Ejecutor(Me.mTL, Me.mRec)
            datos = ej.EjecutarRecuperarDatos(sql, colparam, New ConstructorAL(ptypo))

            Return datos
        End Function

        Public Overridable Function RecuperarDatos(ByVal pID As String, ByVal pInfo As InfoTypeInstClaseDN) As ICollection Implements IBaseAccesorMotorAD.RecuperarDatos
            Dim sql As String
            Dim ej As Ejecutor
            Dim datos As ICollection
            Dim csql As IConstructorSQLAD
            Dim colparam As New List(Of IDataParameter)
            Dim ConstructorAdaptador As New ConstructorAdapterAD(mConstructor, pInfo)

            'Construimos la sql
            csql = mConstructor
            sql = csql.ConstSqlSelect(pInfo, colparam, pID)

            'Ejecutamos la sql para recuperar los datos
            ej = New Ejecutor(Me.mTL, Me.mRec)
            datos = ej.EjecutarRecuperarDatos(sql, colparam, ConstructorAdaptador)

            Return datos
        End Function



        Public Overridable Function RecuperarID(ByVal pGUID As String, ByVal pInfo As InfoTypeInstClaseDN) As String Implements IBaseAccesorMotorAD.RecuperarDatosID
            Dim sql As String
            Dim ej As Ejecutor
            Dim datos As String
            Dim csql As IConstructorSQLAD
            Dim colparam As New List(Of IDataParameter)

            'Construimos la sql
            csql = mConstructor
            sql = csql.ConstSqlSelectID(pInfo, colparam, pGUID)

            'Ejecutamos la sql para recuperar los datos
            ej = New Ejecutor(Me.mTL, Me.mRec)
            datos = ej.EjecutarEscalar(sql, colparam)

            Return datos
        End Function

        Public Overridable Function RecuperarDatosVarios(ByVal pColIDs As ArrayList) As ArrayList Implements IBaseAccesorMotorAD.RecuperarDatosVarios

            Throw New NotImplementedException("Error: no implementado")
        End Function

        Public Overridable Function Insertar(ByVal pEntidad As Object) As Object Implements IBaseAccesorMotorAD.Insertar
            Dim sql, sqlHistorica As String
            Dim ej As Ejecutor
            Dim parametros As List(Of IDataParameter)
            Dim datoPer As IDatoPersistenteDN
            Dim resultado As String
            Dim csql As IConstructorSQLAD

            If (pEntidad Is Nothing) Then
                Throw New ApplicationException("Error: la instancia a insertar no puede ser nula.")
            End If

            'Construimos la sql
            parametros = New List(Of IDataParameter)
            csql = mConstructor
            sql = csql.ConstSqlInsert(pEntidad, parametros, FechaModificacionLN.ObtenerFechaModificacion(Me.mTL), sqlHistorica)

            'Ejecutamos la sql para insertar los datos
            ej = New Ejecutor(Me.mTL, Me.mRec)
            resultado = ej.EjecutarEscalar(sql, parametros)

            'Comprobamos el resultado de la query
            If (resultado Is Nothing OrElse resultado = String.Empty) Then
                Throw New ApplicationException("Error: hubo un error al insertar el elemento en la base de datos.")
            End If

            pEntidad.ID = resultado

            'Modificamos el estado de la entidad
            datoPer = pEntidad
            datoPer.FechaModificacion = FechaModificacionLN.ObtenerFechaModificacion(mTL)
            datoPer.EstadoDatos = EstadoDatosDN.SinModificar

            Return pEntidad
        End Function

        Public Overridable Function Modificar(ByVal pEntidad As Object) As Integer Implements IBaseAccesorMotorAD.Modificar
            Dim sql, sqlh As String
            Dim ej As Ejecutor
            Dim parametros As List(Of IDataParameter)
            Dim datoPer As IDatoPersistenteDN
            Dim registrosAfectados As Integer
            Dim csql As IConstructorSQLAD

            If (pEntidad Is Nothing) Then
                Throw New ApplicationException("Error: la instancia a insertar no puede ser nula.")
            End If
            csql = mConstructor

            'Construimos la sql
            parametros = New List(Of IDataParameter)

            sql = csql.ConstSqlUpdate(pEntidad, parametros, FechaModificacionLN.ObtenerFechaModificacion(Me.mTL), sqlh)

            'Ejecutamos la sql para modificar los datos
            ej = New Ejecutor(Me.mTL, Me.mRec)
            registrosAfectados = ej.EjecutarNoConsulta(sql, parametros)

            'Comprobamos el resultado de la query
            If (registrosAfectados <> 1) Then
                Throw New ApplicationException("Error: hubo un error al modificar el elemento en la base de datos.")
            End If

            If Not String.IsNullOrEmpty(sqlh) Then
                'Ejecutamos la sql para modificar los datos
                ej = New Ejecutor(Me.mTL, Me.mRec)
                registrosAfectados = ej.EjecutarNoConsulta(sqlh, parametros)

                'Comprobamos el resultado de la query
                If (registrosAfectados <> 1) Then
                    Throw New ApplicationException("Error: hubo un error al modificar el elemento en la base de datos.")
                End If

            End If




            'Modificamos el estado de la entidad
            datoPer = pEntidad
            datoPer.FechaModificacion = FechaModificacionLN.ObtenerFechaModificacion(mTL)
            datoPer.EstadoDatos = EstadoDatosDN.SinModificar

            Return registrosAfectados
        End Function

        Public Overridable Function GuardarRelacion(ByVal pEntidad As Object, ByVal pMetodoGuardarRelacionTR As GuardarRelacionTR) As Int64 Implements IBaseAccesorMotorAD.GuardarRelacion
            Dim sql As String
            Dim ej As Ejecutor
            Dim parametros As List(Of IDataParameter)
            Dim registrosAfectados As Integer

            If (pEntidad Is Nothing) Then
                Throw New ApplicationException("Error: la instancia a insertar no puede ser nula.")
            End If

            'Construimos la sql
            parametros = New List(Of IDataParameter)

            sql = pMetodoGuardarRelacionTR.Invoke(pEntidad, parametros, FechaModificacionLN.ObtenerFechaModificacion(Me.mTL))

            'Ejecutamos la sql para modificar los datos
            ej = New Ejecutor(Me.mTL, Me.mRec)
            registrosAfectados = ej.EjecutarNoConsulta(sql, parametros)

            Return registrosAfectados
        End Function

        'TODO: (Vicente) ESTA FUNCION NO HACE NADA!!!!!!!!
        Public Overridable Function Eliminar(ByVal pID As Integer) As Integer Implements IBaseAccesorMotorAD.Eliminar
            Dim sql As String = String.Empty
            Dim ej As Ejecutor
            Dim parametros As List(Of IDataParameter)
            Dim resultado As Integer

            'Construimos la sql
            parametros = New List(Of IDataParameter)

            ' sql = mConstructor.ConstSqlBaja(pId, parametros, FechaModificacion.ObtenerFechaModificacion(Me.mTL))

            'Ejecutamos la sql para dar de baja los datos
            ej = New Ejecutor(Me.mTL, Me.mRec)
            resultado = ej.EjecutarNoConsulta(sql, parametros)

            Return resultado
        End Function
#End Region

    End Class
End Namespace

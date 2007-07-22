Imports Framework.AccesoDatos
Imports Framework.LogicaNegocios
Imports Framework.LogicaNegocios.Transacciones
Imports MotorBusquedaBasicasDN

Public Class GestorFiltroAD
    Inherits Framework.AccesoDatos.BaseTransaccionAD



#Region "Constructor"
    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub
#End Region




    Public Function RecuperarEstructuraVista(ByVal nombreVista As String, ByVal CaposDeSeleccion As List(Of String)) As MotorBusquedaDN.EstructuraVistaDN


        Dim ej As Framework.AccesoDatos.Ejecutor

        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter) = Nothing

        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing
        Dim estructuratabla As DataTable
        Try

            ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
            ctd.IniciarTransaccion(Me.mTL, ProcTl)




            If nombreVista.ToLower.Contains("select") Then
                sql = "Select top 1 * from (" & nombreVista & ") as Jparada"
            Else

                sql = "Select top 1 * from  " & nombreVista


            End If


            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            estructuratabla = ej.EjecutarDataSet(sql).Tables(0)
            estructuratabla.TableName = nombreVista

            RecuperarEstructuraVista = ProcesarEstructuraTabla(estructuratabla)


            ' cagar losvalores  campos por tipos
            Dim nombrecampo As String
            If CaposDeSeleccion IsNot Nothing Then
                For Each nombrecampo In CaposDeSeleccion
                    CargarCampo(nombreVista, RecuperarEstructuraVista.ListaCampos.RecuperarxNombreCampo(nombrecampo))
                Next
            End If


            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try

    End Function

    Private Function CargarCampo(ByVal nombreVista As String, ByVal pCampo As MotorBusquedaDN.CampoDN)

        Dim ej As Framework.AccesoDatos.Ejecutor

        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter) = Nothing

        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
            ctd.IniciarTransaccion(Me.mTL, ProcTl)


            sql = "Select distinct " & pCampo.NombreCampo & " from  " & nombreVista


            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            pCampo.Valores = ej.EjecutarDataSet(sql)

            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try
    End Function

    Private Function ProcesarEstructuraTabla(ByVal tabla As DataTable) As MotorBusquedaDN.EstructuraVistaDN


        Dim estructura As MotorBusquedaDN.EstructuraVistaDN
        Dim campo As MotorBusquedaDN.CampoDN
        Dim listacampos As MotorBusquedaDN.ColCalposDN


        Dim coludna As System.Data.DataColumn


        estructura = New MotorBusquedaDN.EstructuraVistaDN
        estructura.NombreVista = tabla.TableName
        listacampos = New MotorBusquedaDN.ColCalposDN
        estructura.ListaCampos = listacampos

        For Each coludna In tabla.Columns

            campo = New MotorBusquedaDN.CampoDN
            listacampos.Add(campo)

            campo.NombreCampo = coludna.ColumnName


            campo.tipoCampo = MotorBusquedaDN.tipocampo.otros

            If coludna.DataType Is GetType(Boolean) Then
                campo.tipoCampo = MotorBusquedaDN.tipocampo.boleano
            End If

            If coludna.DataType Is GetType(Int16) OrElse coludna.DataType Is GetType(Int32) OrElse coludna.DataType Is GetType(Int64) OrElse coludna.DataType Is GetType(Double) OrElse coludna.DataType Is GetType(Decimal) Then
                campo.tipoCampo = MotorBusquedaDN.tipocampo.numerico
            End If

            If coludna.DataType Is GetType(DateTime) Then
                campo.tipoCampo = MotorBusquedaDN.tipocampo.fecha
            End If


            If coludna.DataType Is GetType(String) Then
                campo.tipoCampo = MotorBusquedaDN.tipocampo.texto
            End If

            'Select Case coludna.DataType
            '    Case is GetType(Boolean)


            '    Case "int32", "int16"
            '        campo.tipoCampo = MotorBusquedaDN.tipocampo.numerico
            '    Case ""
            '        campo.tipoCampo = MotorBusquedaDN.tipocampo.fecha
            '    Case ""
            '        campo.tipoCampo = MotorBusquedaDN.tipocampo.Listado
            '    Case "String"
            '        campo.tipoCampo = MotorBusquedaDN.tipocampo.texto
            '    Case ""
            '        campo.tipoCampo = MotorBusquedaDN.tipocampo.otros

            'End Select

        Next

        Return estructura


    End Function


    Public Function RecuperarDatos(ByVal pFiltro As MotorBusquedaDN.FiltroDN) As DataSet


        Dim ej As Framework.AccesoDatos.Ejecutor

        Dim sql As String
        Dim parametros As List(Of System.Data.IDataParameter) = Nothing

        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try

            ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
            ctd.IniciarTransaccion(Me.mTL, ProcTl)


            FiltroaSQLyParametros(pFiltro, sql, parametros)


            ' si se vinira de una vinculacion de una navegacion por dn procesar la sql
            If Not pFiltro.PropiedadDeInstancia Is Nothing Then
                If pFiltro.TipoReferido Is Nothing Then
                    Throw New ApplicationException("si se establce un pFiltro.PropiedadDeInstancia debe establecerse igualemten pFiltro.TipoReferido")
                End If
                Dim ads As New MNavegacionDatosLN.MNavDatosAD ' todo: mover el ad a su proyecto
                sql = ads.ModificarSQLRelInversa(sql, parametros, pFiltro.PropiedadDeInstancia, pFiltro.PropiedadDeInstancia.Propiedad.PropertyType)
            End If

            ej = New Framework.AccesoDatos.Ejecutor(ProcTl, Me.mRec)
            RecuperarDatos = ej.EjecutarDataSet(sql, parametros, False)
            ProcTl.Confirmar()

        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try



    End Function

    ''' <summary>
    ''' convertir un filtro en la correspondiente cadena de sql parametrizada
    ''' </summary>
    ''' <param name="pFiltro"></param>
    ''' <param name="sql"></param>
    ''' <param name="pparametros"></param>
    ''' <remarks></remarks>
    Private Sub FiltroaSQLyParametros(ByVal pFiltro As MotorBusquedaDN.FiltroDN, ByRef sql As String, ByRef pparametros As List(Of System.Data.IDataParameter))



        'inicialmente solo esta implementada para un filtro que muestra una coleccion de condiciones simples

        Dim parametro As Data.SqlClient.SqlParameter


        Dim condicion As MotorBusquedaDN.ICondicionDN
        Dim condSimple As MotorBusquedaDN.CondicionDN
        Dim sqlp, sqlcond As String


        ' construir la sql y los parametros
        pparametros = New List(Of System.Data.IDataParameter)

        If pFiltro.NombreVistaSel Is Nothing OrElse pFiltro.NombreVistaSel = "" Then
            Throw New ApplicationExceptionAD(" pFiltro.NombreVistaSel no puede ser nothing")
        End If

        If pFiltro.NombreVistaVis Is Nothing OrElse pFiltro.NombreVistaVis = "" Then
            pFiltro.NombreVistaVis = pFiltro.NombreVistaSel
        End If

        sqlcond = ""


        Dim colCondSimple As New MotorBusquedaDN.ColCondicionDN


        For Each condicion In pFiltro.condiciones
            sqlp = ""

            condSimple = condicion.Factor1
            ProcesarCondicionSimple(pFiltro.NombreVistaSel, condicion.Factor1, sqlp, pparametros)
            sqlcond += " and " & sqlp



        Next



        ' tratar las condiciones de haberlas


        If pFiltro.ColOperacionesPosibles.Count > 0 Then

            Dim contador As Integer = pFiltro.condiciones.Count
            For Each oper As Framework.Procesos.ProcesosDN.OperacionDN In pFiltro.ColOperacionesPosibles

                contador += 1
                sqlcond += " and idOperacionPosible=@idOperacionPosible" & contador
                pparametros.Add(ParametrosConstAD.ConstParametroID("idOperacionPosible" & contador, oper.ID))


            Next


            Dim cabeceraConsulta As String

            Dim soloDistintos As String = ""
            If Not pFiltro.NombreVistaVis = pFiltro.NombreVistaSel Then
                soloDistintos = " distinct "
            End If


            If String.IsNullOrEmpty(pFiltro.ConsultaSQL) Then

                cabeceraConsulta = " Select " & soloDistintos & pFiltro.NombreVistaVis & ".* from  " & pFiltro.NombreVistaVis
            Else
                cabeceraConsulta = pFiltro.ConsultaSQL

            End If

            If pFiltro.NombreVistaVis = pFiltro.NombreVistaSel Then
                'sql = cabeceraConsulta & pFiltro.NombreVistaVis & " inner Join  dbo.vwUltimasOperacionesTotales ON " & pFiltro.NombreVistaVis & ".ID= vwUltimasOperacionesTotales.ID "
                sql = cabeceraConsulta & " inner Join  dbo.vwUltimasOperacionesTotales ON " & pFiltro.NombreVistaVis & ".ID= vwUltimasOperacionesTotales.ID "
            Else
                sql = cabeceraConsulta & " inner Join  " & pFiltro.NombreVistaSel & " ON " & pFiltro.NombreVistaVis & ".ID=" & pFiltro.NombreVistaSel & ".ID " & " inner Join  dbo.vwUltimasOperacionesTotales ON " & pFiltro.NombreVistaVis & ".ID= vwUltimasOperacionesTotales.ID "
            End If


            If sqlcond IsNot Nothing AndAlso sqlcond <> "" Then
                sql += " where  " & sqlcond.Substring(5)
            End If





        Else




            Dim cabeceraConsulta As String

            Dim soloDistintos As String = ""
            If Not pFiltro.NombreVistaVis = pFiltro.NombreVistaSel Then
                soloDistintos = " distinct "
            End If

            If String.IsNullOrEmpty(pFiltro.ConsultaSQL) Then

                cabeceraConsulta = "Select " & soloDistintos & pFiltro.NombreVistaVis & ".* from  " & pFiltro.NombreVistaVis
            Else
                cabeceraConsulta = pFiltro.ConsultaSQL

            End If

            If Not pFiltro.NombreVistaVis = pFiltro.NombreVistaSel Then

                sql = cabeceraConsulta & " inner Join  " & pFiltro.NombreVistaSel & " ON " & pFiltro.NombreVistaVis & ".ID=" & pFiltro.NombreVistaSel & ".ID "
            Else
                sql = cabeceraConsulta
            End If


            If sqlcond IsNot Nothing AndAlso sqlcond <> "" Then
                sql += " where  " & sqlcond.Substring(5)
            End If


        End If






    End Sub



    Private Sub ProcesarCondicionSimple(ByVal nombreVistaSel As String, ByVal condicionsimple As MotorBusquedaDN.CondicionDN, ByRef sqlParcial As String, ByRef pparametros As List(Of System.Data.IDataParameter))


        If nombreVistaSel.Trim <> nombreVistaSel Then
            Throw New ApplicationExceptionAD("nombreVistaSel no puede tener espacios en blanco")
        End If


        Dim NombreCampo, nombreParametro, operador As String
        Dim parametro As Data.SqlClient.SqlParameter
        Dim sqlp2 As String = ""
        NombreCampo = nombreVistaSel & "." & condicionsimple.Campo.NombreCampo
        nombreParametro = "@" & NombreCampo.Replace(".", "") & pparametros.Count

        Select Case condicionsimple.OperadoresArictmetico
            Case OperadoresAritmeticos.igual
                operador = "="
            Case OperadoresAritmeticos.distinto
                operador = "<>"
            Case OperadoresAritmeticos.mayor
                operador = ">"
            Case OperadoresAritmeticos.mayor_igual
                operador = ">="
            Case OperadoresAritmeticos.menor
                operador = "<"
            Case OperadoresAritmeticos.menor_igual
                operador = "<="
            Case OperadoresAritmeticos.contener_texto
                ' sqlParcial = "instr(" & NombreCampo & "," & nombreParametro & ")>0"
                sqlp2 = NombreCampo & " like '%' +" & nombreParametro & "+'%' "
        End Select

        If Not sqlp2 Is Nothing AndAlso Not sqlp2 = "" Then
            sqlParcial += sqlp2
        Else
            If sqlParcial Is Nothing OrElse sqlParcial = "" Then
                sqlParcial = NombreCampo & operador & nombreParametro
            Else
                sqlParcial += NombreCampo & operador & nombreParametro
            End If
        End If



        'Dim valorCampo As String

        'If condicionsimple.capo.TineLsitaValores Then
        '    valorCampo=condicionsimple.
        'Else
        '    valorCampo = condicionsimple.ValorInicial
        'End If

        Select Case condicionsimple.Campo.tipoCampo

            Case MotorBusquedaDN.tipocampo.boleano
                parametro = ParametrosConstAD.ConstParametroBoolean(nombreParametro, condicionsimple.ValorInicial)

            Case MotorBusquedaDN.tipocampo.numerico
                parametro = ParametrosConstAD.ConstParametroDouble(nombreParametro, condicionsimple.ValorInicial)

            Case MotorBusquedaDN.tipocampo.fecha
                parametro = ParametrosConstAD.ConstParametroFecha(nombreParametro, condicionsimple.ValorInicial)

            Case MotorBusquedaDN.tipocampo.texto
                parametro = ParametrosConstAD.ConstParametroString(nombreParametro, condicionsimple.ValorInicial)

            Case Else ' si no se reconoce el tipo se trata como comparación textual
                parametro = ParametrosConstAD.ConstParametroString(nombreParametro, condicionsimple.ValorInicial)

                'Throw New ApplicationException("tipo incorrecto")
        End Select

        pparametros.Add(parametro)
    End Sub

End Class

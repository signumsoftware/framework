Imports Framework.DatosNegocio
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Operaciones.OperacionesDN

Imports FN.RiesgosVehiculos.DN

Public Class CargadorPrimasBaseAD

    Public Function CargarImpuestosModuladores(ByVal colCoberturas As FN.Seguros.Polizas.DN.ColCoberturaDN) As FN.RiesgosVehiculos.DN.ColImpuestoRVSVDN

        'vwImpuestos

        'SELECT     dbo.SignumImpuestos.ID, dbo.SignumImpuestos.Valor, dbo.SignumTipoImpuesto.TipoImpuesto, dbo.SignumCobertura.Nombre AS cobertura, 
        '                      dbo.SignumTipoImpuesto.TipoOperacion, dbo.SignumImpuestos.FechaEfecto, dbo.SignumImpuestos.FechaBaja
        'FROM         dbo.SignumImpuestos INNER JOIN
        '                      dbo.SignumCobertura ON dbo.SignumImpuestos.FKCobertura = dbo.SignumCobertura.ID INNER JOIN
        '                      dbo.SignumTipoImpuesto ON dbo.SignumImpuestos.FKTipoImpuesto = dbo.SignumTipoImpuesto.ID


        ' recurso a la fuente de los datos de tarificacion
        Dim recFuetne As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim connectionstring As String
        Dim htd As New Generic.Dictionary(Of String, Object)
        'connectionstring = "server=localhost;database=DatosTarificador;user=sa;pwd=''"
        connectionstring = "Data Source=localhost;Initial Catalog=DatosTarificador;Integrated Security=True"
        htd.Add("connectionstring", connectionstring)
        recFuetne = New Framework.LogicaNegocios.Transacciones.RecursoLN("2", "Conexion a MND1", "sqls", htd)




        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' creamos los moduladores para cada  coeficiente en la guente de datos
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim ColImpuestoRV As New FN.RiesgosVehiculos.DN.ColImpuestoRVDN
        Dim ColImpuesto As New FN.RiesgosVehiculos.DN.ColImpuestoDN

        Dim dts As DataSet
        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuetne)
        dts = ej.EjecutarDataSet("select * from vwImpuestos", Nothing, False)

        For Each dr As Data.DataRow In dts.Tables(0).Rows



            Dim Impuesto As FN.RiesgosVehiculos.DN.ImpuestoDN = ColImpuesto.RecuperarPrimeroXNombre(dr("TipoImpuesto"))
            If Impuesto Is Nothing Then
                Impuesto = New FN.RiesgosVehiculos.DN.ImpuestoDN
                Impuesto.Nombre = dr("TipoImpuesto")
                If Impuesto.Nombre = "FNG" OrElse Impuesto.Nombre = "CLEA" OrElse Impuesto.Nombre = "IPS" Then
                    Impuesto.Fraccionable = True
                End If

                ColImpuesto.Add(Impuesto)
            End If

            Dim cob As FN.Seguros.Polizas.DN.CoberturaDN = colCoberturas.RecuperarPrimeroXNombre(dr("cobertura"))
            Dim ImpuestoRV As New FN.RiesgosVehiculos.DN.ImpuestoRVDN
            ImpuestoRV.OperadorAplicable = dr("TipoOperacion")
            ImpuestoRV.Impuesto = Impuesto
            ImpuestoRV.Cobertura = cob
            ImpuestoRV.Periodo.FI = Me.ConvertirFecha(dr("FechaEfecto"))
            ImpuestoRV.Periodo.FF = Me.ConvertirFecha(dr("FechaBaja"))
            ImpuestoRV.Valor = dr("Valor")
            ColImpuestoRV.Add(ImpuestoRV)

            Dim ColImpuestoRVInconsistentes As FN.RiesgosVehiculos.DN.ColImpuestoRVDN = ColImpuestoRV.VerificarIntegridadColColpleta

            If ColImpuestoRVInconsistentes.Count > 0 Then

                Select Case ColImpuestoRVInconsistentes.Item(0).Periodo.SolapadoOContenido(ColImpuestoRVInconsistentes.Item(1))
                    Case Framework.DatosNegocio.IntSolapadosOContenido.Contenedor
                        ColImpuestoRV.Remove(ColImpuestoRVInconsistentes.Item(1))
                    Case Framework.DatosNegocio.IntSolapadosOContenido.Contenido
                        ColImpuestoRV.Remove(ColImpuestoRVInconsistentes.Item(0))

                    Case Framework.DatosNegocio.IntSolapadosOContenido.Iguales
                        ColImpuestoRV.Remove(ColImpuestoRVInconsistentes.Item(0))

                    Case Framework.DatosNegocio.IntSolapadosOContenido.Solapados
                        Beep()

                End Select



            End If



        Next

        If ColImpuestoRV.VerificarIntegridadColColpleta().Count > 0 Then
            Throw New ApplicationException("debe haber algun periodo solapado")
        End If

        Me.GuardarDatos(ColImpuestoRV)





        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' creamos los suministradores de valor de modulador  para cada  par  covertura - impuesto
        ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim ColImpuestoRVSV As New FN.RiesgosVehiculos.DN.ColImpuestoRVSVDN
        Dim ImpuestoRVSV As FN.RiesgosVehiculos.DN.ImpuestoRVSVDN

        For Each impuesto As FN.RiesgosVehiculos.DN.ImpuestoDN In ColImpuesto
            For Each cobertura As FN.Seguros.Polizas.DN.CoberturaDN In colCoberturas
                ImpuestoRVSV = New FN.RiesgosVehiculos.DN.ImpuestoRVSVDN
                ImpuestoRVSV.ColImpuestoRV = ColImpuestoRV.SeleccionarX(cobertura, impuesto)

                If ImpuestoRVSV.ColImpuestoRV.Count > 0 Then ' si no hay impuesto para esa cobertura no se crea
                    ImpuestoRVSV.Cobertura = cobertura
                    ImpuestoRVSV.Impuesto = ImpuestoRVSV.ColImpuestoRV(0).Impuesto 'se supone que es el mismo para todos
                    ImpuestoRVSV.Operadoraplicable = ImpuestoRVSV.ColImpuestoRV(0).OperadorAplicable 'se supone que es el mismo para todos
                    ColImpuestoRVSV.Add(ImpuestoRVSV)
                End If

            Next
        Next

        Me.GuardarDatos(ColImpuestoRVSV)




        Return ColImpuestoRVSV




    End Function

    Public Function CargarModuladoresMultiConductor(ByVal pColCaracteristica As Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN, ByVal pColprimas As FN.RiesgosVehiculos.DN.ColPrimaBaseRVDN, ByVal colcategoriasMD As FN.RiesgosVehiculos.DN.ColCategoriaModDatosDN, ByVal colCoberturas As FN.Seguros.Polizas.DN.ColCoberturaDN) As FN.RiesgosVehiculos.DN.ColModuladorRVSVDN


        'SELECT     dbo.SignumModuladoresMCND.ID, dbo.SignumModuladoresMCND.ValorCoeficiente, dbo.SignumModuladoresMCND.FechaEfecto, 
        '                      dbo.SignumModuladoresMCND.FechaBaja, dbo.SignumModuladoresMCND.InicioEdadAseg, dbo.SignumModuladoresMCND.FinEdadAseg, 
        '                      dbo.SignumModuladoresMCND.InicioEdadMCND, dbo.SignumModuladoresMCND.FinEdadMCND, dbo.SignumTiposModuladores.TipoCoeficiente, 
        '                      dbo.SignumCategoria.Nombre AS categoria, dbo.SignumCobertura.Nombre AS cobertura
        'FROM         dbo.SignumModuladoresMCND INNER JOIN
        '                      dbo.SignumCategoria ON dbo.SignumModuladoresMCND.FKCategoria = dbo.SignumCategoria.ID INNER JOIN
        '                      dbo.SignumCobertura ON dbo.SignumModuladoresMCND.FKCobertura = dbo.SignumCobertura.ID INNER JOIN
        '                      dbo.SignumTiposModuladores ON dbo.SignumModuladoresMCND.FKTipoModulador = dbo.SignumTiposModuladores.ID



        ' recurso a la fuente de los datos de tarificacion
        Dim recFuetne As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim connectionstring As String
        Dim htd As New Generic.Dictionary(Of String, Object)
        'connectionstring = "server=localhost;database=DatosTarificador;user=sa;pwd=''"
        connectionstring = "Data Source=localhost;Initial Catalog=DatosTarificador;Integrated Security=True"
        htd.Add("connectionstring", connectionstring)
        recFuetne = New Framework.LogicaNegocios.Transacciones.RecursoLN("2", "Conexion a MND1", "sqls", htd)




        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' creamos los moduladores para cada  coeficiente en la guente de datos
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim colModuladores As New FN.RiesgosVehiculos.DN.ColModuladorRVDN
        Dim ColModulador As New FN.RiesgosVehiculos.DN.ColModuladorDN

        Dim edadCatracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN = pColCaracteristica.RecuperarPrimeroXNombre("EDAD")

        Dim dts As DataSet
        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuetne)
        dts = ej.EjecutarDataSet("select * from vwModuladorMultiConductor ", Nothing, False)

        For Each dr As Data.DataRow In dts.Tables(0).Rows

            ' la caracteristica y el modu van en relacion 1 -1

            If dr("ValorCoeficiente") = 0 Then
                Beep()
                Debug.WriteLine(dr("TipoCoeficiente") & dr("InicioIntervalo") & "-" & dr("FinalIntervalo") & "-" & ConvertirFecha(dr("fi")) & "-" & ConvertirFecha(dr("ff")))
            Else




                Dim modu As FN.RiesgosVehiculos.DN.ModuladorDN = ColModulador.RecuperarPrimeroXNombre(dr("TipoCoeficiente"))
                If modu Is Nothing Then
                    modu = New FN.RiesgosVehiculos.DN.ModuladorDN
                    modu.Nombre = dr("TipoCoeficiente")
                    modu.NoRequerido = True
                    ColModulador.Add(modu)
                End If

                Dim Caracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN = pColCaracteristica.RecuperarPrimeroXNombre(dr("TipoCoeficiente"))


                If Caracteristica Is Nothing Then
                    Caracteristica = New Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
                    Caracteristica.Nombre = dr("TipoCoeficiente")
                    pColCaracteristica.Add(Caracteristica)
                    Caracteristica.Padre = edadCatracteristica
                End If

                Dim cob As FN.Seguros.Polizas.DN.CoberturaDN = colCoberturas.RecuperarPrimeroXNombre(dr("cobertura"))
                Dim catMD As FN.RiesgosVehiculos.DN.CategoriaModDatosDN = colcategoriasMD.RecuperarxNombreCategoria(dr("categoria"))
                Dim categoria As FN.RiesgosVehiculos.DN.CategoriaDN = catMD.Categoria

                'Dim miColModuladorRVSVDN As FN.RiesgosVehiculos.DN.ColModuladorRVSVDN
                'miColModuladorRVSVDN = pColValorIntervalNumMap.Recuperar(cob)



                Dim modulador As FN.RiesgosVehiculos.DN.ModuladorRVDN = colModuladores.Recuperar(cob, categoria, Caracteristica)
                If modulador Is Nothing Then
                    modulador = New FN.RiesgosVehiculos.DN.ModuladorRVDN
                    modulador.Modulador = modu
                    modulador.Caracteristica = Caracteristica
                    modulador.Cobertura = cob
                    modulador.Categoria = categoria
                    modulador.CategoriaModDatos = catMD
                    modulador.Nombre = dr("TipoCoeficiente") & " " & dr("cobertura") & " " & dr("categoria")
                    colModuladores.Add(modulador)
                End If



                Dim iv, ivpare As Framework.Tarificador.TarificadorDN.ValorIntervalNumMapDN
                ' ivpare= EncontrarIVPadre(miColModuladorRVSVDN.RecuperarColModuladorRV.RecuperarColValorIntervalNumMapDN, edadCatracteristica, dr("FechaEfecto"), dr("InicioEdadAseg"))
                ivpare = New Framework.Tarificador.TarificadorDN.ValorIntervalNumMapDN
                ivpare.Caracteristica = edadCatracteristica
                ivpare.ValorNumerico = 0
                ivpare.Periodo.FI = ConvertirFecha(dr("FechaEfecto"))
                ivpare.Periodo.FF = ConvertirFecha(dr("FechaBaja"))
                ivpare.Intervalo = New Framework.DatosNegocio.IntvaloNumericoDN
                ivpare.Intervalo.ValInf = dr("InicioEdadAseg")
                ivpare.Intervalo.ValSup = dr("FinEdadAseg")

                iv = New Framework.Tarificador.TarificadorDN.ValorIntervalNumMapDN
                iv.NumMapPadre = ivpare
                iv.Caracteristica = Caracteristica
                iv.ValorNumerico = dr("ValorCoeficiente")
                iv.Periodo.FI = ConvertirFecha(dr("FechaEfecto"))
                iv.Periodo.FF = ConvertirFecha(dr("FechaBaja"))
                iv.Intervalo = New Framework.DatosNegocio.IntvaloNumericoDN
                iv.Intervalo.ValInf = dr("InicioEdadMCND")
                iv.Intervalo.ValSup = dr("FinEdadMCND")
                modulador.ColValorIntervalNumMap.Add(iv)

            End If
        Next

        Me.GuardarDatos(colModuladores)



        ' asociamos el recuperador (esto no habria que hacerlo si no se desea clacular en el grafo)
        ' Dim recuperador As New RecSumiValorDN




        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' creamos los suministradores de valor de modulador  para cada par  covertura - caracteristica
        ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim colsuministradorvModuladores As New FN.RiesgosVehiculos.DN.ColModuladorRVSVDN
        Dim suministradorvModuladores As FN.RiesgosVehiculos.DN.ModuladorRVSVDN


        For Each miCaracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN In pColCaracteristica
            For Each cobertura As FN.Seguros.Polizas.DN.CoberturaDN In colCoberturas

                Dim colm As FN.RiesgosVehiculos.DN.ColModuladorRVDN = colModuladores.SeleccionarX(cobertura, miCaracteristica)

                If colm.Count > 0 Then
                    suministradorvModuladores = New FN.RiesgosVehiculos.DN.ModuladorRVSVDN
                    suministradorvModuladores.Cobertura = cobertura
                    suministradorvModuladores.Caracteristica = miCaracteristica

                    suministradorvModuladores.ColModuladorRV = colm
                    suministradorvModuladores.Modulador = ColModulador.RecuperarPrimeroXNombre(miCaracteristica.Nombre)
                    colsuministradorvModuladores.Add(suministradorvModuladores)
                End If

            Next
        Next



        Me.GuardarDatos(colsuministradorvModuladores)





        Return colsuministradorvModuladores




    End Function

    Public Function EncontrarIVPadre(ByVal pColValorIntervalNumMap As Framework.Tarificador.TarificadorDN.ColValorIntervalNumMapDN, ByVal pcaracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN, ByVal fi As Date, ByVal vi As Double) As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN

        Dim col As IList = pColValorIntervalNumMap.Recuperar(vi, pcaracteristica, fi)

        If col.Count <> 1 Then
            Throw New ApplicationException("se recupero distinto de un elemeto")
        End If

        Return col(0)

    End Function

    Public Function CargarModuladores(ByVal pColprimas As FN.RiesgosVehiculos.DN.ColPrimaBaseRVDN, ByVal colcategoriasMD As FN.RiesgosVehiculos.DN.ColCategoriaModDatosDN, ByVal colCoberturas As FN.Seguros.Polizas.DN.ColCoberturaDN) As FN.RiesgosVehiculos.DN.ColModuladorRVSVDN


        'SELECT     dbo.SignumModuladoresIntervalo.ID, dbo.SignumModuladoresIntervalo.ValorCoeficiente, dbo.SignumModuladoresIntervalo.FechaEfecto AS fi, 
        '                      dbo.SignumModuladoresIntervalo.FechaBaja AS ff, dbo.SignumModuladoresIntervalo.InicioIntervalo, dbo.SignumModuladoresIntervalo.FinalIntervalo, 
        '                      dbo.SignumModuladoresIntervalo.FKTipoModulador, dbo.SignumModuladoresIntervalo.FKCategoria, dbo.SignumModuladoresIntervalo.FKCobertura, 
        '                      dbo.SignumCategoria.Nombre AS categoria, dbo.SignumCobertura.Nombre AS cobertura
        'FROM         dbo.SignumModuladoresIntervalo INNER JOIN
        '                      dbo.SignumCategoria ON dbo.SignumModuladoresIntervalo.FKCategoria = dbo.SignumCategoria.ID INNER JOIN
        '                      dbo.SignumCobertura ON dbo.SignumModuladoresIntervalo.FKCobertura = dbo.SignumCobertura.ID




        ' recurso a la fuente de los datos de tarificacion
        Dim recFuente As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim connectionstring As String
        Dim htd As New Generic.Dictionary(Of String, Object)
        'connectionstring = "server=localhost;database=DatosTarificador;user=sa;pwd=''"
        connectionstring = "Data Source=localhost;Initial Catalog=DatosTarificador;Integrated Security=True"
        htd.Add("connectionstring", connectionstring)
        recFuente = New Framework.LogicaNegocios.Transacciones.RecursoLN("2", "Conexion a MND1", "sqls", htd)




        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' creamos los moduladores para cada  coeficiente en la guente de datos
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim colModuladores As New FN.RiesgosVehiculos.DN.ColModuladorRVDN
        Dim ColCaracteristica As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN
        Dim ColModulador As New FN.RiesgosVehiculos.DN.ColModuladorDN


        Dim dts As DataSet
        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuente)
        dts = ej.EjecutarDataSet("select * from vwModuladorIntervaloxCategoriaxCobertura", Nothing, False)

        For Each dr As Data.DataRow In dts.Tables(0).Rows

            ' la caracteristica y el modu van en relacion 1 -1

            If dr("ValorCoeficiente") = 0 Then
                Beep()
                Debug.WriteLine(dr("TipoCoeficiente") & dr("InicioIntervalo") & "-" & dr("FinalIntervalo") & "-" & ConvertirFecha(dr("fi")) & "-" & ConvertirFecha(dr("ff")))
            Else

                Dim modu As FN.RiesgosVehiculos.DN.ModuladorDN = ColModulador.RecuperarPrimeroXNombre(dr("TipoCoeficiente"))
                If modu Is Nothing Then
                    modu = New FN.RiesgosVehiculos.DN.ModuladorDN
                    modu.Nombre = dr("TipoCoeficiente")
                    ColModulador.Add(modu)
                End If

                Dim Caracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN = ColCaracteristica.RecuperarPrimeroXNombre(dr("TipoCoeficiente"))
                If Caracteristica Is Nothing Then
                    Caracteristica = New Framework.Cuestionario.CuestionarioDN.CaracteristicaDN
                    Caracteristica.Nombre = dr("TipoCoeficiente")
                    ColCaracteristica.Add(Caracteristica)
                End If

                Dim cob As FN.Seguros.Polizas.DN.CoberturaDN = colCoberturas.RecuperarPrimeroXNombre(dr("cobertura"))

                Dim catMD As CategoriaModDatosDN = colcategoriasMD.RecuperarxNombreCategoria(dr("categoria"))
                Dim categoria As FN.RiesgosVehiculos.DN.CategoriaDN = catMD.Categoria

                Dim modulador As FN.RiesgosVehiculos.DN.ModuladorRVDN = colModuladores.Recuperar(cob, categoria, Caracteristica)
                If modulador Is Nothing Then
                    modulador = New FN.RiesgosVehiculos.DN.ModuladorRVDN
                    modulador.Modulador = modu
                    modulador.Caracteristica = Caracteristica
                    modulador.Cobertura = cob
                    modulador.Categoria = categoria
                    modulador.CategoriaModDatos = catMD
                    modulador.Nombre = dr("TipoCoeficiente") & " " & dr("cobertura") & " " & dr("categoria")
                    colModuladores.Add(modulador)
                End If



                Dim iv As New Framework.Tarificador.TarificadorDN.ValorIntervalNumMapDN

                iv.Caracteristica = Caracteristica
                iv.ValorNumerico = dr("ValorCoeficiente")
                iv.Periodo.FI = ConvertirFecha(dr("fi"))
                iv.Periodo.FF = ConvertirFecha(dr("ff"))
                iv.Intervalo = New Framework.DatosNegocio.IntvaloNumericoDN
                iv.Intervalo.ValInf = dr("InicioIntervalo")
                iv.Intervalo.ValSup = dr("FinalIntervalo")
                modulador.ColValorIntervalNumMap.Add(iv)

            End If
        Next

        Me.GuardarDatos(colModuladores)



        ' asociamos el recuperador (esto no habria que hacerlo si no se desea clacular en el grafo)
        ' Dim recuperador As New RecSumiValorDN




        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' creamos los suministradores de valor de modulador  para cada par  covertura - caracteristica
        ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim colsuministradorvModuladores As New FN.RiesgosVehiculos.DN.ColModuladorRVSVDN
        Dim suministradorvModuladores As FN.RiesgosVehiculos.DN.ModuladorRVSVDN


        For Each miCaracteristica As Framework.Cuestionario.CuestionarioDN.CaracteristicaDN In ColCaracteristica
            For Each cobertura As FN.Seguros.Polizas.DN.CoberturaDN In colCoberturas

                Dim colm As FN.RiesgosVehiculos.DN.ColModuladorRVDN = colModuladores.SeleccionarX(cobertura, miCaracteristica)

                If colm.Count > 0 Then
                    suministradorvModuladores = New FN.RiesgosVehiculos.DN.ModuladorRVSVDN
                    suministradorvModuladores.Cobertura = cobertura
                    suministradorvModuladores.Caracteristica = miCaracteristica

                    suministradorvModuladores.ColModuladorRV = colm
                    suministradorvModuladores.Modulador = ColModulador.RecuperarPrimeroXNombre(miCaracteristica.Nombre)
                    colsuministradorvModuladores.Add(suministradorvModuladores)
                End If

            Next
        Next



        Me.GuardarDatos(colsuministradorvModuladores)





        ' crear el recuperador de valores e introducirle los datos

        '   recuperador.DataSoucers.Add(GenerarTarifa(pColprimas.RecuperarColCoberturaDN()))






        '' creamos el flujo
        'Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        'op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        'op.Nombre = "op1"
        ''   op.Operando1 = New SumiValFijoDN(1) ' esto debe sustituirse por la operación vinculada a un suministrador de valor que sera un modulador
        '' op.Operando2 = suministradorvPrimas
        'op.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN
        'op.DebeCachear = False
        'op.IRecSumiValorLN = recuperador
        'op.DebeCachear = True






        'System.Diagnostics.Debug.WriteLine(op.GetValor)


        Return colsuministradorvModuladores




    End Function

    Public Function CargarPrimasBase(ByVal colCategoriasMD As ColCategoriaModDatosDN) As FN.RiesgosVehiculos.DN.ColPrimabaseRVSVDN

        Dim recFuente As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim connectionstring As String
        Dim htd As New Generic.Dictionary(Of String, Object)
        'connectionstring = "server=localhost;database=DatosTarificador;user=sa;pwd=''"
        connectionstring = "Data Source=localhost;Initial Catalog=DatosTarificador;Integrated Security=True"
        htd.Add("connectionstring", connectionstring)
        recFuente = New Framework.LogicaNegocios.Transacciones.RecursoLN("2", "Conexion a MND1", "sqls", htd)


        Dim colCobertura As New FN.Seguros.Polizas.DN.ColCoberturaDN
        Dim colPrimaBaseRV As New FN.RiesgosVehiculos.DN.ColPrimaBaseRVDN


        Dim dts As DataSet
        Dim ej As Framework.AccesoDatos.Ejecutor

        '1º Cargamos las coberturas
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuente)
        dts = ej.EjecutarDataSet("select * from SignumCobertura", Nothing, False)

        For Each dr As Data.DataRow In dts.Tables(0).Rows
            Dim cobertura As New FN.Seguros.Polizas.DN.CoberturaDN()
            cobertura.Nombre = dr("Nombre").ToString()
            cobertura.Descripcion = dr("Descripcion").ToString()

            colCobertura.AddUnico(cobertura)
        Next

        '2º Cargamos las primas base
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuente)
        dts = ej.EjecutarDataSet("select SignumPrimasBase.*,SignumCategoria.Nombre as Categoria,SignumCobertura.Nombre as Cobertura from SignumPrimasBase inner join SignumCategoria on SignumCategoria.ID=fkCategoriaSS inner join SignumCobertura on SignumCobertura.ID=fkCoberturaSS", Nothing, False)

        For Each dr As Data.DataRow In dts.Tables(0).Rows
            Dim primaBase As New PrimaBaseRVDN()

            primaBase.CategoriaModDatos = colCategoriasMD.RecuperarxNombreCategoria(dr("Categoria"))
            primaBase.Categoria = primaBase.CategoriaModDatos.Categoria
            primaBase.Cobertura = colCobertura.RecuperarPrimeroXNombre(dr("Cobertura"))
            primaBase.Importe = dr("importe")
            primaBase.Periodo.FI = ConvertirFecha(dr("FechaEfecto"))
            primaBase.Periodo.FF = ConvertirFecha(dr("FechaBaja"))

            colPrimaBaseRV.AddUnico(primaBase)

        Next


        '''''''''''''''''''''''''''''''''''''''''''''''''
        ' verificacion de integridad en la carga
        '''''''''''''''''''''''''''''''''''''''''''''''''

        Dim colPBRVSolapadas As New FN.RiesgosVehiculos.DN.ColPrimaBaseRVDN()
        colPBRVSolapadas = colPrimaBaseRV.VerificarIntegridadColCompleta()
        If colPBRVSolapadas.Count > 1 Then
            Throw New Framework.AccesoDatos.ApplicationExceptionAD("Error de integridad en los datos de carga: no pueden existir periodos solapados de primas base")
        End If

        '''''''''''''''''''''''''''''''''''''''''''''''''

        Using tr As New Transaccion


            Dim listFallos As ArrayList

            listFallos = GuardarDatos(colCobertura, True)
            If listFallos.Count > 0 Then
                Throw New ApplicationException
            End If

            listFallos = GuardarDatos(colPrimaBaseRV, True)
            If listFallos.Count > 0 Then
                Throw New ApplicationException
            End If


            Dim ColPrimabaseRVSV As New FN.RiesgosVehiculos.DN.ColPrimabaseRVSVDN

            For Each cober As FN.Seguros.Polizas.DN.CoberturaDN In colCobertura

                Dim PrimabaseRVSV As New FN.RiesgosVehiculos.DN.PrimabaseRVSVDN
                PrimabaseRVSV.Cobertura = cober
                PrimabaseRVSV.ColPrimasBase = colPrimaBaseRV.SeleccionarPCDeCobertura(cober.GUID)
                ColPrimabaseRVSV.Add(PrimabaseRVSV)

            Next

            listFallos = GuardarDatos(ColPrimabaseRVSV, True)


            If listFallos.Count > 0 Then
                Throw New ApplicationException
            End If

            tr.Confirmar()

            Return ColPrimabaseRVSV

        End Using

    End Function

    Public Function CargarComisiones(ByVal colCoberturas As FN.Seguros.Polizas.DN.ColCoberturaDN) As FN.RiesgosVehiculos.DN.ColComisionRVSVDN
        ' recurso a la fuente de los datos de tarificacion
        Dim recFuente As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim connectionstring As String
        Dim htd As New Generic.Dictionary(Of String, Object)
        'connectionstring = "server=localhost;database=DatosTarificador;user=sa;pwd=''"
        connectionstring = "Data Source=localhost;Initial Catalog=DatosTarificador;Integrated Security=True"
        htd.Add("connectionstring", connectionstring)
        recFuente = New Framework.LogicaNegocios.Transacciones.RecursoLN("2", "Conexion a MND1", "sqls", htd)

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' creamos los moduladores para cada  coeficiente en la guente de datos
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim colComisionesRV As New FN.RiesgosVehiculos.DN.ColComisionRVDN
        Dim colComisiones As New FN.RiesgosVehiculos.DN.ColComisionDN

        Dim dts As DataSet
        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuente)
        dts = ej.EjecutarDataSet("select * from vwComisionesxCobertura", Nothing, False)

        For Each dr As Data.DataRow In dts.Tables(0).Rows

            Dim comision As FN.RiesgosVehiculos.DN.ComisionDN = colComisiones.RecuperarPrimeroXNombre(dr("TipoComision"))
            If comision Is Nothing Then
                comision = New FN.RiesgosVehiculos.DN.ComisionDN()
                comision.Nombre = dr("TipoComision")
                comision.Fraccionable = dr("Fraccionable")
                colComisiones.Add(comision)
            End If

            Dim cob As FN.Seguros.Polizas.DN.CoberturaDN = colCoberturas.RecuperarPrimeroXNombre(dr("Cobertura"))
            Dim comisionRV As New FN.RiesgosVehiculos.DN.ComisionRVDN

            comisionRV.OperadorAplicable = dr("TipoOperacion")
            comisionRV.Comision = comision
            comisionRV.Cobertura = cob
            comisionRV.Periodo.FI = Me.ConvertirFecha(dr("FechaEfecto"))
            comisionRV.Periodo.FF = Me.ConvertirFecha(dr("FechaBaja"))
            comisionRV.Valor = dr("Valor")
            colComisionesRV.Add(comisionRV)

            Dim colComisionesRVInconsistentes As FN.RiesgosVehiculos.DN.ColComisionRVDN = colComisionesRV.VerificarIntegridadColCompleta()

            If colComisionesRVInconsistentes.Count > 0 Then

                Select Case colComisionesRVInconsistentes.Item(0).Periodo.SolapadoOContenido(colComisionesRVInconsistentes.Item(1))
                    Case Framework.DatosNegocio.IntSolapadosOContenido.Contenedor
                        colComisionesRV.Remove(colComisionesRVInconsistentes.Item(1))
                    Case Framework.DatosNegocio.IntSolapadosOContenido.Contenido
                        colComisionesRV.Remove(colComisionesRVInconsistentes.Item(0))

                    Case Framework.DatosNegocio.IntSolapadosOContenido.Iguales
                        colComisionesRV.Remove(colComisionesRVInconsistentes.Item(0))

                    Case Framework.DatosNegocio.IntSolapadosOContenido.Solapados
                        Beep()

                End Select

            End If

        Next

        If colComisionesRV.VerificarIntegridadColCompleta().Count > 0 Then
            Throw New ApplicationException("debe haber algún periodo solapado")
        End If

        Me.GuardarDatos(colComisionesRV)

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' creamos los suministradores de valor de modulador  para cada  par  cobertura - comisión
        ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim colComisionesRVSV As New FN.RiesgosVehiculos.DN.ColComisionRVSVDN
        Dim comisionRVSV As FN.RiesgosVehiculos.DN.ComisionRVSVDN

        For Each eltoComision As FN.RiesgosVehiculos.DN.ComisionDN In colComisiones
            For Each cobertura As FN.Seguros.Polizas.DN.CoberturaDN In colCoberturas
                comisionRVSV = New FN.RiesgosVehiculos.DN.ComisionRVSVDN
                comisionRVSV.ColComisionRV = colComisionesRV.SeleccionarX(cobertura, eltoComision)

                If comisionRVSV.ColComisionRV.Count > 0 Then ' si no hay impuesto para esa cobertura no se crea
                    comisionRVSV.Cobertura = cobertura
                    comisionRVSV.Comision = comisionRVSV.ColComisionRV(0).Comision 'se supone que es el mismo para todos
                    comisionRVSV.Operadoraplicable = comisionRVSV.ColComisionRV(0).OperadorAplicable 'se supone que es el mismo para todos
                    colComisionesRVSV.Add(comisionRVSV)
                End If

            Next
        Next

        Me.GuardarDatos(colComisionesRVSV)

        Return colComisionesRVSV

    End Function

    Public Function CargarCategoriaModDatos() As ColCategoriaModDatosDN
        ' recurso a la fuente de los datos de tarificacion
        Dim recFuente As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim connectionstring As String
        Dim htd As New Generic.Dictionary(Of String, Object)
        'connectionstring = "server=localhost;database=DatosTarificador;user=sa;pwd=''"
        connectionstring = "Data Source=localhost;Initial Catalog=DatosTarificador;Integrated Security=True"
        htd.Add("connectionstring", connectionstring)
        recFuente = New Framework.LogicaNegocios.Transacciones.RecursoLN("2", "Conexion a MND1", "sqls", htd)

        Dim colCategorias As New ColCategoriaDN()
        Dim colModelos As New ColModeloDN()
        Dim colMarcas As New ColMarcaDN()
        Dim colModeloDatos As New ColModeloDatosDN()
        Dim colCategoriaModDatos As New ColCategoriaModDatosDN()

        Dim dts As DataSet
        Dim ej As Framework.AccesoDatos.Ejecutor

        '1º Cargamos las marcas
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuente)
        dts = ej.EjecutarDataSet("select * from SignumMarca", Nothing, False)

        For Each dr As Data.DataRow In dts.Tables(0).Rows
            Dim marca As New MarcaDN
            marca.Nombre = dr("Marca").ToString()

            colMarcas.AddUnico(marca)
        Next

        Using tr As New Transaccion()
            Dim listFallos As ArrayList

            listFallos = GuardarDatos(colMarcas, True)
            If listFallos.Count > 0 Then
                Throw New ApplicationException
            End If

            tr.Confirmar()

        End Using

        '2º Cargamos las categorias
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuente)
        dts = ej.EjecutarDataSet("select * from SignumCategoria", Nothing, False)

        For Each dr As Data.DataRow In dts.Tables(0).Rows
            Dim categoria As New CategoriaDN
            categoria.Nombre = dr("Nombre").ToString()

            colCategorias.AddUnico(categoria)
        Next

        Using tr As New Transaccion()
            Dim listFallos As ArrayList

            listFallos = GuardarDatos(colCategorias, True)
            If listFallos.Count > 0 Then
                Throw New ApplicationException
            End If

            tr.Confirmar()

        End Using

        '3º Cargamos los modelos 
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuente)
        dts = ej.EjecutarDataSet("select distinct SignumModelos.Modelo,SignumMarca.Marca from SignumModelos inner join SignumMarca on SignumModelos.fkmarca=SignumMarca.ID", Nothing, False)

        For Each dr As Data.DataRow In dts.Tables(0).Rows
            Dim modelo As New ModeloDN
            modelo.Nombre = dr("Modelo").ToString()
            modelo.Marca = colMarcas.RecuperarPrimeroXNombre(dr("Marca"))

            colModelos.AddUnico(modelo)
        Next

        Using tr As New Transaccion()
            Dim listFallos As ArrayList

            listFallos = GuardarDatos(colModelos, True)
            If listFallos.Count > 0 Then
                Throw New ApplicationException
            End If

            tr.Confirmar()

        End Using

        '4ºCargamos los ModeloDatos
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuente)
        dts = ej.EjecutarDataSet("select vwCategoriaModeloDatos.*,SignumCategoria.Nombre as Categoria from vwCategoriaModeloDatos inner join SignumCategoria on fkCategoria=SignumCategoria.ID", Nothing, False)

        For Each dr As Data.DataRow In dts.Tables(0).Rows
            Dim categoriaModDatos As CategoriaModDatosDN = Nothing
            Dim modeloDatos As New ModeloDatosDN()

            modeloDatos.Modelo = colModelos.RecuperarModeloxNombreMarca(dr("Modelo"), colMarcas.RecuperarPrimeroXNombre(dr("Marca")))
            modeloDatos.Categoria = colCategorias.RecuperarPrimeroXNombre(dr("Categoria"))

            If modeloDatos.Categoria.Nombre.ToUpper() <> "QUAD" AndAlso modeloDatos.Categoria.Nombre.ToUpper() <> "CROSS" Then
                modeloDatos.Matriculado = True
            End If

            Try
                modeloDatos.FI = ConvertirFecha(dr("FechaEfecto"))
                modeloDatos.FF = ConvertirFecha(dr("FechaBaja"))

            Catch ex As Exception
                If modeloDatos.Modelo.Nombre.ToUpper() = "R 1100 RT" AndAlso modeloDatos.Modelo.Marca.Nombre.ToUpper() = "BMW" Then
                    modeloDatos.FI = ConvertirFecha(dr("FechaEfecto"))
                    modeloDatos.FF = ConvertirFecha(New Date(2005, 2, 1))
                Else
                    modeloDatos = Nothing
                    Debug.WriteLine("El modelo " & modeloDatos.Modelo.Nombre & " de la marca " & modeloDatos.Modelo.Marca.Nombre & " es inconsistente en la base de datos (FI>FF)")
                End If
            End Try

            If modeloDatos IsNot Nothing Then
                For Each cmd As CategoriaModDatosDN In colCategoriaModDatos
                    If cmd.Categoria.GUID = modeloDatos.Categoria.GUID Then
                        categoriaModDatos = cmd
                    End If
                Next

                If categoriaModDatos Is Nothing Then
                    categoriaModDatos = New CategoriaModDatosDN()
                    categoriaModDatos.Categoria = modeloDatos.Categoria
                    colCategoriaModDatos.Add(categoriaModDatos)
                End If

                categoriaModDatos.ColModelosDatos.Add(modeloDatos)
            End If

        Next

        categoriaNoMatriculada(colCategoriaModDatos, colCategorias, "Quad matriculado", "Quad")
        categoriaNoMatriculada(colCategoriaModDatos, colCategorias, "Enduro", "Cross")


        '''''''''''''''''''''''''''''''''''''''''''''''''
        ' verificacion de integridad en la carga
        '''''''''''''''''''''''''''''''''''''''''''''''''


        Dim colMDTotales As New ColModeloDatosDN()
        Dim colMDEliminados As New ColModeloDatosDN()

        For Each catMD As CategoriaModDatosDN In colCategoriaModDatos
            colMDTotales.AddRangeObjectUnico(catMD.ColModelosDatos)
        Next

        Dim colModTotales As ColModeloDN = colMDTotales.RecuperarColModelos()

        For Each md As ModeloDN In colModTotales
            Dim colMDAux As ColModeloDatosDN = colMDTotales.RecuperarColModeloDatosxModelo(md)

            Dim colIT As New Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN()

            colIT.AddRangeObjectUnico(colMDAux.RecuperarColPeridosFechas())
            If colIT.IntervalosFechaSolapados() Then
                For Each mdAux As ModeloDatosDN In colMDAux
                    If mdAux.Periodo.FF = Date.MinValue Then
                        'Dim colITAux As New Framework.DatosNegocio.Localizaciones.Temporales.ColIntervaloFechasDN()

                        'colITAux.AddRangeObjectUnico(colIT)
                        'colITAux.Add(mdAux.Periodo)

                        'If colITAux.IntervalosFechaSolapados() Then

                        'Else
                        '    colIT.Add(mdAux.Periodo)
                        'End If
                    Else
                        colMDEliminados.AddUnico(mdAux)
                        Debug.WriteLine("El modelo " & md.Nombre & " de la marca " & md.Marca.Nombre & " es inconsistente en la base de datos (Intervalos solapados)")
                    End If
                    
                Next
            End If

        Next

        For Each catmd As CategoriaModDatosDN In colCategoriaModDatos
            For Each md As ModeloDatosDN In colMDEliminados
                catmd.ColModelosDatos.EliminarEntidadDNxGUID(md.GUID)
            Next
        Next


        Using tr As New Transaccion
            Dim listFallos As ArrayList

            listFallos = GuardarDatos(colCategoriaModDatos, True)
            If listFallos.Count > 0 Then
                Throw New ApplicationException
            End If

            tr.Confirmar()

            Return colCategoriaModDatos
        End Using

    End Function

    Public Function CargarFraccionamientos(ByVal colCob As FN.Seguros.Polizas.DN.ColCoberturaDN) As ColFraccionamientoRVSVDN
        ' recurso a la fuente de los datos de tarificacion
        Dim recFuetne As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim connectionstring As String
        Dim htd As New Generic.Dictionary(Of String, Object)
        'connectionstring = "server=localhost;database=DatosTarificador;user=sa;pwd=''"
        connectionstring = "Data Source=localhost;Initial Catalog=DatosTarificador;Integrated Security=True"
        htd.Add("connectionstring", connectionstring)
        recFuetne = New Framework.LogicaNegocios.Transacciones.RecursoLN("2", "Conexion a MND1", "sqls", htd)


        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' creamos los fraccionamientos para cada  coeficiente en la guente de datos
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim colFrcRV As New FN.RiesgosVehiculos.DN.ColFraccionamientoRVDN()
        Dim colFrc As New FN.GestionPagos.DN.ColFraccionamientoDN()

        Dim dts As DataSet
        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuetne)
        dts = ej.EjecutarDataSet("select signumfraccionamientos.*,signumcobertura.Nombre as Cobertura, signumtiposfraccionamiento.TipoFraccionamiento,signumtiposfraccionamiento.NumeroPagos, signumtiposfraccionamiento.IntervaloMeses from signumfraccionamientos inner join signumcobertura on fkCobertura=signumcobertura.id inner join signumtiposfraccionamiento on fkTipoFraccionamiento=signumtiposfraccionamiento.id", Nothing, False)

        For Each dr As Data.DataRow In dts.Tables(0).Rows

            Dim frac As FN.GestionPagos.DN.FraccionamientoDN = colFrc.RecuperarPrimeroXNombre(dr("TipoFraccionamiento"))
            If frac Is Nothing Then
                frac = New FN.GestionPagos.DN.FraccionamientoDN()
                frac.Nombre = dr("TipoFraccionamiento")
                frac.NumeroPagos = dr("NumeroPagos")
                frac.FrecuenciaMensual = dr("IntervaloMeses")

                colFrc.Add(frac)
            End If

            Dim cob As FN.Seguros.Polizas.DN.CoberturaDN = colCob.RecuperarPrimeroXNombre(dr("Cobertura"))
            Dim fracRV As New FN.RiesgosVehiculos.DN.FraccionamientoRVDN()


            fracRV.OperadorAplicable = "*"
            fracRV.Fraccionamiento = frac
            fracRV.Cobertura = cob
            fracRV.Periodo.FI = Me.ConvertirFecha(dr("FechaEfecto"))
            fracRV.Periodo.FF = Me.ConvertirFecha(dr("FechaBaja"))
            fracRV.Valor = dr("ValorFraccionamiento")
            fracRV.Fraccionable = dr("Fraccionable")

            colFrcRV.Add(fracRV)

            Dim colFracRVInconsistentes As FN.RiesgosVehiculos.DN.ColFraccionamientoRVDN = colFrcRV.VerificarIntegridadColCompleta()

            If colFracRVInconsistentes.Count > 0 Then

                Select Case colFracRVInconsistentes.Item(0).Periodo.SolapadoOContenido(colFracRVInconsistentes.Item(1))
                    Case Framework.DatosNegocio.IntSolapadosOContenido.Contenedor
                        colFrcRV.Remove(colFracRVInconsistentes.Item(1))
                    Case Framework.DatosNegocio.IntSolapadosOContenido.Contenido
                        colFrcRV.Remove(colFracRVInconsistentes.Item(0))
                    Case Framework.DatosNegocio.IntSolapadosOContenido.Iguales
                        colFrcRV.Remove(colFracRVInconsistentes.Item(0))
                    Case Framework.DatosNegocio.IntSolapadosOContenido.Solapados
                        Beep()
                End Select

            End If

        Next

        If colFrcRV.VerificarIntegridadColCompleta().Count > 0 Then
            Throw New ApplicationException("Debe haber algún periodo solapado en la carga de fraccionamientos")
        End If

        Me.GuardarDatos(colFrcRV)

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' creamos los suministradores de valor de fraccionamiento  para cada  par  cobertura - fraccionamiento
        ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim colFracRVSV As New FN.RiesgosVehiculos.DN.ColFraccionamientoRVSVDN()
        Dim fracRVSV As FN.RiesgosVehiculos.DN.FraccionamientoRVSVDN

        'For Each frac As FN.GestionPagos.DN.FraccionamientoDN In colFrc
        For Each cobertura As FN.Seguros.Polizas.DN.CoberturaDN In colCob
            fracRVSV = New FN.RiesgosVehiculos.DN.FraccionamientoRVSVDN()
            fracRVSV.ColFraccionamientoRV = colFrcRV.SeleccionarX(cobertura)

            If fracRVSV.ColFraccionamientoRV.Count > 0 Then ' si no hay fraccionamiento para esa cobertura no se crea
                fracRVSV.Cobertura = cobertura
                'fracRVSV.Fraccionamiento = fracRVSV.ColFraccionamientoRV(0).Fraccionamiento 'se supone que es el mismo para todos
                fracRVSV.Operadoraplicable = fracRVSV.ColFraccionamientoRV(0).OperadorAplicable 'se supone que es el mismo para todos

                colFracRVSV.Add(fracRVSV)
            End If

        Next
        'Next

        Me.GuardarDatos(colFracRVSV)

        Return colFracRVSV
    End Function


    Public Function CargarBonificaciones(ByVal colCategoriasMD As FN.RiesgosVehiculos.DN.ColCategoriaModDatosDN) As FN.RiesgosVehiculos.DN.ColBonificacionRVSVDN
        ' recurso a la fuente de los datos de tarificacion
        Dim recFuente As Framework.LogicaNegocios.Transacciones.RecursoLN
        Dim connectionstring As String
        Dim htd As New Generic.Dictionary(Of String, Object)
        'connectionstring = "server=localhost;database=DatosTarificador;user=sa;pwd=''"
        connectionstring = "Data Source=localhost;Initial Catalog=DatosTarificador;Integrated Security=True"
        htd.Add("connectionstring", connectionstring)
        recFuente = New Framework.LogicaNegocios.Transacciones.RecursoLN("2", "Conexion a MND1", "sqls", htd)


        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' creamos las bopnificacioens para cada coeficiente en la fuente de datos
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim colBonifRV As New FN.RiesgosVehiculos.DN.ColBonificacionRVDN
        Dim ColBonif As New FN.RiesgosVehiculos.DN.ColBonificacionDN
        Dim bonif As FN.RiesgosVehiculos.DN.BonificacionDN

        Dim dts As DataSet
        Dim ej As Framework.AccesoDatos.Ejecutor
        ej = New Framework.AccesoDatos.Ejecutor(Nothing, recFuente)
        dts = ej.EjecutarDataSet("select * from SignumBonificacionesIntervalo inner join SignumTipoBonificacion on FKTipoBonificacion=SignumTipoBonificacion.id inner join SignumCategoria on FKCategoria=SignumCategoria.ID", Nothing, False)

        For Each dr As Data.DataRow In dts.Tables(0).Rows

            ' la caracteristica y el modu van en relacion 1 -1

            If dr("ValorCoeficiente") = 0 Then
                Beep()
                Debug.WriteLine(dr("TipoCoeficiente") & " - " & dr("InicioIntervalo") & "-" & dr("FinalIntervalo") & "-" & ConvertirFecha(dr("fi")) & "-" & ConvertirFecha(dr("ff")))
            Else

                bonif = ColBonif.RecuperarPrimeroXNombre(dr("TipoCoeficiente"))
                If bonif Is Nothing Then
                    bonif = New FN.RiesgosVehiculos.DN.BonificacionDN
                    bonif.Nombre = dr("TipoCoeficiente")
                    ColBonif.Add(bonif)
                End If

                Dim catMD As CategoriaModDatosDN = colCategoriasMD.RecuperarxNombreCategoria(dr("Nombre"))
                Dim categoria As FN.RiesgosVehiculos.DN.CategoriaDN = catMD.Categoria

                Dim bonifRV As FN.RiesgosVehiculos.DN.BonificacionRVDN = Nothing '= ColBonificacionRVDN(cob, categoria, Caracteristica)
                If bonifRV Is Nothing Then
                    bonifRV = New FN.RiesgosVehiculos.DN.BonificacionRVDN
                    bonifRV.Bonificacion = bonif
                    bonifRV.Categoria = categoria
                    bonifRV.CategoriaModDatos = catMD
                    bonifRV.IntervaloNumerico = New Framework.DatosNegocio.IntvaloNumericoDN()
                    bonifRV.IntervaloNumerico.ValInf = dr("InicioIntervalo")
                    bonifRV.IntervaloNumerico.ValSup = dr("FinalIntervalo")
                    bonifRV.Periodo = New Framework.DatosNegocio.Localizaciones.Temporales.IntervaloFechasDN(ConvertirFecha(dr("FechaEfecto")), ConvertirFecha(dr("FechaBaja")))
                    bonifRV.Nombre = dr("NivelBonificacion")
                    bonifRV.Valor = dr("ValorCoeficiente")
                End If

                colBonifRV.Add(bonifRV)

            End If
        Next

        'Hay que crear las bonificaciones para las categorías que no han sido cargadas (Quad, Cross y Trial)
        bonif = ColBonif.RecuperarPrimeroXNombre("Bonificación Siniestralidad")
        Dim colBonifRVAux As ColBonificacionRVDN = colBonifRV.RecuperarBonificaciones(bonif, colBonifRV.Item(0).Categoria)

        If bonif Is Nothing Then
            bonif = New FN.RiesgosVehiculos.DN.BonificacionDN
            bonif.Nombre = "Bonificación Siniestralidad"
            ColBonif.Add(bonif)
        End If

        For Each catMD As CategoriaModDatosDN In colCategoriasMD
            Dim categoria As FN.RiesgosVehiculos.DN.CategoriaDN = catMD.Categoria

            If categoria.Nombre.ToUpper() = "QUAD" OrElse categoria.Nombre.ToUpper() = "CROSS" OrElse categoria.Nombre.ToUpper() = "TRIAL" Then

                For Each bonRVAux As BonificacionRVDN In colBonifRVAux
                    Dim bonifRV As New FN.RiesgosVehiculos.DN.BonificacionRVDN()
                    bonifRV.Bonificacion = bonif
                    bonifRV.Categoria = categoria
                    bonifRV.CategoriaModDatos = catMD
                    bonifRV.IntervaloNumerico = New Framework.DatosNegocio.IntvaloNumericoDN()
                    bonifRV.IntervaloNumerico.ValInf = bonRVAux.IntervaloNumerico.ValInf
                    bonifRV.IntervaloNumerico.ValSup = bonRVAux.IntervaloNumerico.ValSup
                    bonifRV.Periodo = New Framework.DatosNegocio.Localizaciones.Temporales.IntervaloFechasDN(bonRVAux.Periodo.FI, bonRVAux.Periodo.FF)
                    bonifRV.Nombre = bonRVAux.IntervaloNumerico.Nombre
                    bonifRV.Valor = 1

                    colBonifRV.Add(bonifRV)
                Next

            End If

        Next



        Me.GuardarDatos(colBonifRV)


        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' creamos los suministradores de valor de bonificacion
        ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim colsuministradoRVBonificaciones As New FN.RiesgosVehiculos.DN.ColBonificacionRVSVDN()
        Dim suministradoRVBonificaciones As FN.RiesgosVehiculos.DN.BonificacionRVSVDN

        suministradoRVBonificaciones = New FN.RiesgosVehiculos.DN.BonificacionRVSVDN
        suministradoRVBonificaciones.ColBonificacionRV = colBonifRV
        suministradoRVBonificaciones.Bonificacion = colBonifRV(0).Bonificacion

        colsuministradoRVBonificaciones.Add(suministradoRVBonificaciones)

        Me.GuardarDatos(colsuministradoRVBonificaciones)

        Return colsuministradoRVBonificaciones

    End Function


    Private Function ConvertirFecha(ByVal valor As Object) As Date

        If valor Is System.DBNull.Value Then
            Return Date.MinValue
        Else
            Return Date.Parse(valor)
        End If

    End Function

    Private Sub categoriaNoMatriculada(ByRef colCategoriaMD As FN.RiesgosVehiculos.DN.ColCategoriaModDatosDN, ByVal colCategorias As ColCategoriaDN, ByVal pNombreCatMatriculada As String, ByVal pNombreCatNoMatriculada As String)

        ' poner los no matriculados
        Dim catMat As CategoriaDN = colCategorias.RecuperarPrimeroXNombre(pNombreCatMatriculada)
        Dim catNoMat As CategoriaDN = colCategorias.RecuperarPrimeroXNombre(pNombreCatNoMatriculada)

        Dim catMDMat As CategoriaModDatosDN = colCategoriaMD.RecuperarxNombreCategoria(pNombreCatMatriculada)
        Dim catMDNoMat As CategoriaModDatosDN = colCategoriaMD.RecuperarxNombreCategoria(pNombreCatNoMatriculada)

        If catMDNoMat Is Nothing Then
            catMDNoMat = New CategoriaModDatosDN()
            catMDNoMat.Categoria = catNoMat
        End If

        For Each modeldatosMatriculado As FN.RiesgosVehiculos.DN.ModeloDatosDN In catMDMat.ColModelosDatos
            Dim miModelodatos As FN.RiesgosVehiculos.DN.ModeloDatosDN
            miModelodatos = modeldatosMatriculado.CloneSuperficialSinIdentidad
            miModelodatos.Matriculado = False
            miModelodatos.Nombre = miModelodatos.Nombre & " No Mat"
            miModelodatos.Categoria = catMDNoMat.Categoria

            catMDNoMat.ColModelosDatos.Add(miModelodatos)

        Next

        colCategoriaMD.Add(catMDNoMat)

    End Sub

    Private Function recuperarEntidad(ByVal col As Object, ByVal nombreEntidad As Object) As Framework.DatosNegocio.EntidadDN

        If nombreEntidad Is DBNull.Value Then
            Return Nothing
        End If

        Dim entidad As Framework.DatosNegocio.EntidadDN
        entidad = col.RecuperarPrimeroXNombre(nombreEntidad)
        If entidad Is Nothing Then
            entidad = Activator.CreateInstance(Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(col.GetType, Framework.TiposYReflexion.DN.FijacionDeTipoDN.Indefinida))
            Dim lista As IList = col
            lista.Add(entidad)
            entidad.Nombre = nombreEntidad
        Else
            'Debug.WriteLine(entidad.Nombre)
        End If

        Return entidad

    End Function

    Private Sub GuardarDatos(ByVal objeto As Object)
        Using tr As New Transaccion
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(objeto)

            tr.Confirmar()

        End Using
    End Sub

    Private Function GuardarDatos(ByVal col As IEnumerable, ByVal transaccionesIndividuales As Boolean) As ArrayList

        Dim al As New ArrayList

        For Each o As Object In col

            Using tr As New Transaccion(transaccionesIndividuales)
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

                Try
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    gi.Guardar(o)
                    tr.Confirmar()
                Catch ex As Exception

                    al.Add(o)
                    tr.Cancelar()
                End Try
            End Using

        Next

        Return al

    End Function

End Class

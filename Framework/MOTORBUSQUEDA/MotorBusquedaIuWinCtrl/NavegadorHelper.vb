Imports MotorBusquedaBasicasDN

Public Class NavegadorHelper

#Region "Métodos"

    'Public Shared Sub NavegarABuscador(ByVal pControl As MotorIU.ControlesP.IControlP, ByVal pTipo As System.Type, ByVal PropiedadDeInstancia As Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN)
    '    NavegarABuscador(pControl, pTipo, Nothing, MotorIU.Motor.TipoNavegacion.Normal, PropiedadDeInstancia)
    'End Sub

    'Public Shared Sub NavegarABuscador(ByVal pControl As MotorIU.ControlesP.IControlP, ByVal pTipo As System.Type, ByRef paquete As Hashtable, ByVal tipoNavegacion As MotorIU.Motor.TipoNavegacion, ByVal PropiedadDeInstancia As Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN)
    '    Dim formPadre As MotorIU.FormulariosP.IFormularioP
    '    formPadre = ControlesPBase.ControlHelper.ObtenerFormularioPadre(pControl)
    '    NavegarABuscador(formPadre, pTipo, paquete, tipoNavegacion, PropiedadDeInstancia)
    'End Sub

    Public Shared Function NavegarABuscador(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByVal pTipo As System.Type, ByVal pColOperacionDN As Framework.Procesos.ProcesosDN.ColOperacionDN, ByVal pColvalores As List(Of ValorCampo), ByVal PropiedadDeInstancia As Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN) As Hashtable
        Return NavegarABuscador(pFormulario, pTipo, Nothing, MotorIU.Motor.TipoNavegacion.Normal, pColOperacionDN, pColvalores, PropiedadDeInstancia)
    End Function

    Public Shared Function NavegarABuscador(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByVal pTipo As System.Type)
        Return NavegarABuscador(pFormulario, pTipo, Nothing, MotorIU.Motor.TipoNavegacion.Normal, Nothing, Nothing)
    End Function
    Public Shared Function NavegarABuscador(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByVal pTipo As System.Type, ByVal tipoNavegacion As MotorIU.Motor.TipoNavegacion, ByVal PropiedadDeInstancia As Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN) As Hashtable
        Dim paquete As New Hashtable

        Return NavegarABuscador(pFormulario, pTipo, paquete, tipoNavegacion, Nothing, False, Nothing, Nothing, PropiedadDeInstancia, Nothing)

    End Function
    Public Shared Function NavegarABuscador(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByVal pTipo As System.Type, ByVal destino As String, ByVal PasarDtsResultados As Boolean, ByVal pColOperacionDN As Framework.Procesos.ProcesosDN.ColOperacionDN, ByVal pColvalores As List(Of ValorCampo)) As Hashtable


        Return NavegarABuscador(pFormulario, pTipo, Nothing, MotorIU.Motor.TipoNavegacion.Normal, pColOperacionDN, pColvalores, Nothing)
    End Function



    Public Shared Function NavegarABuscador(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByVal pColvalores As List(Of ValorCampo), ByVal paramCargaEstruct As ParametroCargaEstructuraDN) As Hashtable
        Return NavegarABuscador(pFormulario, Nothing, Nothing, MotorIU.Motor.TipoNavegacion.Normal, Nothing, False, Nothing, pColvalores, Nothing, paramCargaEstruct)
    End Function






    Public Shared Function NavegarABuscador(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByVal emap As MV2DN.EntradaMapNavBuscadorDN, ByVal entidad As Framework.DatosNegocio.IEntidadDN) As Hashtable

        Dim marco As MotorIU.Motor.INavegador

        Dim paquete As Hashtable
        paquete = New Hashtable()
        Dim pTipo As System.Type = emap.Tipo


        Dim pb As ParametroCargaEstructuraDN = ParametrosHelper.CrearParametroCargaEstructura(emap, entidad)


        Dim tipo As System.Type = pTipo
        Dim miPaqueteFormularioBusqueda As MotorBusquedaDN.PaqueteFormularioBusqueda
        'If pColOperacionDN IsNot Nothing Then
        '    paramCargaEstruct.ColOperacion = pColOperacionDN
        'End If


   


        miPaqueteFormularioBusqueda = New MotorBusquedaDN.PaqueteFormularioBusqueda()
        miPaqueteFormularioBusqueda.EnviarDatatableAlNavegar = False
        miPaqueteFormularioBusqueda.MultiSelect = False
        miPaqueteFormularioBusqueda.Agregable = Not miPaqueteFormularioBusqueda.EnviarDatatableAlNavegar
        miPaqueteFormularioBusqueda.Filtrable = emap.Filtrable
        miPaqueteFormularioBusqueda.FiltroVisible = emap.FiltroVisible
        miPaqueteFormularioBusqueda.BusquedaAutomatica = emap.BusquedaAutomatica
        miPaqueteFormularioBusqueda.Titulo = emap.NombreVis
        miPaqueteFormularioBusqueda.ColComandoMap = New MV2DN.ColComandoMapDN
        miPaqueteFormularioBusqueda.ColComandoMap.AddRange(emap.ColComandoMap)
        If emap.MostrarComandosDelTipo Then
            ' cargar el mapeado de el tipo referido si se deben importar sus comandos
            Dim recmap As New MV2DN.RecuperadorMapeadoXFicheroXMLAD(Framework.Configuracion.AppConfiguracion.DatosConfig("RutaCargaMapVis"))
            Dim imap As MV2DN.InstanciaMapDN = recmap.RecuperarInstanciaMap(tipo)
            miPaqueteFormularioBusqueda.ColComandoMap.AddRange(imap.ColComandoMap)
        End If




        paquete.Add("PaqueteFormularioBusqueda", miPaqueteFormularioBusqueda)
        miPaqueteFormularioBusqueda = CType(paquete.Item("PaqueteFormularioBusqueda"), MotorBusquedaDN.PaqueteFormularioBusqueda)
        miPaqueteFormularioBusqueda.ParametroCargaEstructura = pb

        If miPaqueteFormularioBusqueda.ParametroCargaEstructura.ListaValores IsNot Nothing AndAlso miPaqueteFormularioBusqueda.ParametroCargaEstructura.ListaValores.Count > 0 Then
            ParametrosHelper.SustituirParamettrosExterioresPorValores(miPaqueteFormularioBusqueda.ParametroCargaEstructura.ListaValores, pFormulario.cMarco.DatosMarco)
        End If

        ' miPaqueteFormularioBusqueda.ParametroCargaEstructura.PropiedadDeInstancia = PropiedadDeInstancia



        marco = pFormulario.cMarco
        Dim f As System.Windows.Forms.Form = pFormulario

        If Not f.IsMdiContainer Then

            f = f.MdiParent

        End If




        marco.Navegar("Filtro", pFormulario, f, MotorIU.Motor.TipoNavegacion.Normal, paquete)

        Return paquete


    End Function



    Public Shared Function NavegarABuscador(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByVal pTipo As System.Type, ByRef paquete As Hashtable, ByVal tipoNavegacion As MotorIU.Motor.TipoNavegacion, ByVal destino As String, ByVal pasarDtsResultdos As Boolean, ByVal pColOperacionDN As Framework.Procesos.ProcesosDN.ColOperacionDN, ByVal pColvalores As List(Of ValorCampo), ByVal PropiedadDeInstancia As Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN, ByVal paramCargaEstruct As ParametroCargaEstructuraDN) As Hashtable
        Dim marco As MotorIU.Motor.INavegador

        If paquete Is Nothing Then
            paquete = New Hashtable()
        End If

        Dim tipo As System.Type = pTipo

        Dim miPaqueteFormularioBusqueda As MotorBusquedaDN.PaqueteFormularioBusqueda
        If paramCargaEstruct Is Nothing Then
            paramCargaEstruct = RecuperarParametroBusqueda(tipo)
        End If



        If Not String.IsNullOrEmpty(destino) Then
            paramCargaEstruct.DestinoNavegacion = destino
        End If

        If pColOperacionDN IsNot Nothing Then
            paramCargaEstruct.ColOperacion = pColOperacionDN
        End If


        If pColvalores IsNot Nothing Then
            paramCargaEstruct.ListaValores = pColvalores

        End If

        If Not paquete.Contains("PaqueteFormularioBusqueda") Then
            miPaqueteFormularioBusqueda = New MotorBusquedaDN.PaqueteFormularioBusqueda()
            miPaqueteFormularioBusqueda.EnviarDatatableAlNavegar = pasarDtsResultdos
            miPaqueteFormularioBusqueda.MultiSelect = pasarDtsResultdos
            miPaqueteFormularioBusqueda.Agregable = Not pasarDtsResultdos
            miPaqueteFormularioBusqueda.Filtrable = True
            miPaqueteFormularioBusqueda.FiltroVisible = True
            paquete.Add("PaqueteFormularioBusqueda", miPaqueteFormularioBusqueda)

        End If

        miPaqueteFormularioBusqueda = CType(paquete.Item("PaqueteFormularioBusqueda"), MotorBusquedaDN.PaqueteFormularioBusqueda)
        miPaqueteFormularioBusqueda.ParametroCargaEstructura = paramCargaEstruct
        If miPaqueteFormularioBusqueda.ParametroCargaEstructura.ListaValores IsNot Nothing AndAlso miPaqueteFormularioBusqueda.ParametroCargaEstructura.ListaValores.Count > 0 Then
            ParametrosHelper.SustituirParamettrosExterioresPorValores(miPaqueteFormularioBusqueda.ParametroCargaEstructura.ListaValores, pFormulario.cMarco.DatosMarco)
        End If

        miPaqueteFormularioBusqueda.ParametroCargaEstructura.PropiedadDeInstancia = PropiedadDeInstancia



        marco = pFormulario.cMarco
        Dim f As System.Windows.Forms.Form = pFormulario

        If Not f.IsMdiContainer Then

            f = f.MdiParent

        End If




        marco.Navegar("Filtro", pFormulario, f, tipoNavegacion, paquete)


        Return paquete

    End Function








    Public Shared Function NavegarABuscador(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByVal pTipo As System.Type, ByRef paquete As Hashtable, ByVal tipoNavegacion As MotorIU.Motor.TipoNavegacion, ByVal PropiedadDeInstancia As Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN) As Hashtable

        Return NavegarABuscador(pFormulario, pTipo, paquete, tipoNavegacion, Nothing, False, Nothing, Nothing, PropiedadDeInstancia, Nothing)

    End Function


    Public Shared Function NavegarABuscador(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByVal pTipo As System.Type, ByRef paquete As Hashtable, ByVal tipoNavegacion As MotorIU.Motor.TipoNavegacion, ByVal pColOperacionDN As Framework.Procesos.ProcesosDN.ColOperacionDN, ByVal pColvalores As List(Of ValorCampo), ByVal PropiedadDeInstancia As Framework.TiposYReflexion.DN.PropiedadDeInstanciaDN) As Hashtable

        Return NavegarABuscador(pFormulario, pTipo, paquete, tipoNavegacion, Nothing, False, pColOperacionDN, pColvalores, PropiedadDeInstancia, Nothing)

    End Function


    Public Shared Function NavegarFormulario(ByVal pControl As MotorIU.ControlesP.IControlP, ByVal entidad As Framework.DatosNegocio.IEntidadBaseDN) As Hashtable
        Dim formPadre As MotorIU.FormulariosP.IFormularioP
        formPadre = ControlesPBase.ControlHelper.ObtenerFormularioPadre(pControl)
        Return NavegarFormulario(formPadre, entidad)
    End Function



    Public Shared Function NavegarFormulario(ByVal pControl As MotorIU.ControlesP.IControlP, ByRef paquete As Hashtable, ByVal tipoNav As MotorIU.Motor.TipoNavegacion) As Hashtable
        Dim formPadre As MotorIU.FormulariosP.IFormularioP
        formPadre = ControlesPBase.ControlHelper.ObtenerFormularioPadre(pControl)
        Return NavegarFormulario(formPadre, paquete, tipoNav)

    End Function
    Public Shared Function NavegarFormulario(ByVal pControl As MotorIU.ControlesP.IControlP, ByVal tipo As System.Type) As Hashtable
        Dim formPadre As MotorIU.FormulariosP.IFormularioP
        formPadre = ControlesPBase.ControlHelper.ObtenerFormularioPadre(pControl)

        Return NavegarFormulario(pControl, tipo)

    End Function
    Public Shared Function NavegarFormulario(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByVal entidad As Framework.DatosNegocio.IEntidadBaseDN, ByVal pTipoNavegacion As MotorIU.Motor.TipoNavegacion, ByVal pNombreMapeadoVisualizacion As String) As Hashtable
        Dim paquete As New Hashtable
        paquete.Add("DN", entidad)
        paquete.Add("NombreInstanciaMapVis", pNombreMapeadoVisualizacion)

        Return NavegarFormulario(pFormulario, paquete, pTipoNavegacion)

    End Function
    Public Shared Function NavegarFormulario(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByVal entidad As Framework.DatosNegocio.IEntidadBaseDN, ByVal pTipoNavegacion As MotorIU.Motor.TipoNavegacion) As Hashtable
        Dim paquete As New Hashtable
        paquete.Add("DN", entidad)
        Return NavegarFormulario(pFormulario, paquete, pTipoNavegacion)

    End Function
    Public Shared Function NavegarFormulario(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByVal entidad As Framework.DatosNegocio.IEntidadBaseDN) As Hashtable
        Dim paquete As New Hashtable
        paquete.Add("DN", entidad)
        Return NavegarFormulario(pFormulario, paquete, MotorIU.Motor.TipoNavegacion.Normal)

    End Function

    Public Shared Function NavegarFormulario(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByVal tipo As System.Type, ByVal tipoNavegacion As MotorIU.Motor.TipoNavegacion) As Hashtable
        Dim paquete As New Hashtable

        paquete.Add("TipoEntidad", tipo)

        Return NavegarFormulario(pFormulario, paquete, tipoNavegacion)

    End Function

    Public Shared Function NavegarFormulario(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByVal tipo As System.Type) As Hashtable
        Dim paquete As New Hashtable

        paquete.Add("TipoEntidad", tipo)

        Return NavegarFormulario(pFormulario, paquete, MotorIU.Motor.TipoNavegacion.Normal)

    End Function

    Public Shared Function NavegarFormulario(ByVal pFormulario As MotorIU.FormulariosP.IFormularioP, ByRef paquete As Hashtable, ByVal tipoNav As MotorIU.Motor.TipoNavegacion) As Hashtable
        Dim marco As MotorIU.Motor.INavegador
        marco = pFormulario.cMarco
        Dim f As Windows.Forms.Form = pFormulario

        marco.Navegar("FG", pFormulario, f.MdiParent, tipoNav, paquete)
        Return paquete
    End Function


    'TODO: Revisar estos método y tratar de factorizar
    Public Shared Function RecuperarParametroBusqueda(ByVal pTipo As System.Type) As ParametroCargaEstructuraDN
        Dim miIRecuperadorInstanciaMap As MV2DN.IRecuperadorInstanciaMap = New MV2DN.RecuperadorMapeadoXFicheroXMLAD(Framework.Configuracion.AppConfiguracion.DatosConfig("RutaCargaMapVis"))

        If miIRecuperadorInstanciaMap Is Nothing Then
            Throw New ApplicationException("pIRecuperadorInstanciaMap no puede ner nulo")
        End If


        ' debe de haber un lugar donde para una dn se mapee sus mapeados de busqueda disponibles
        ' el mapeado de visualizacion debiera de porder pasar el nombre del mapeado de busqueda a ausar


        Dim elementoMap As MV2DN.ElementoMapDN
        elementoMap = miIRecuperadorInstanciaMap.RecuperarInstanciaMap(pTipo)

        Return RecuperarPB(pTipo, elementoMap)

    End Function

    Public Shared Function RecuperarParametroBusqueda(ByVal pIVincElemento As MV2DN.IVincElemento, ByVal pTipo As System.Type) As ParametroCargaEstructuraDN

        If pTipo Is Nothing Then
            Return RecuperarParametroBusqueda(pIVincElemento)
        End If


        ' debe de haber un lugar donde para una dn se mapee sus mapeados de busqueda disponibles
        ' el mapeado de visualizacion debiera de porder pasar el nombre del mapeado de busqueda a ausar
        Dim pb As New ParametroCargaEstructuraDN()



        Dim elementoMap As MV2DN.ElementoMapDN

        If String.IsNullOrEmpty(pIVincElemento.ElementoMap.DatosBusqueda) Then
            elementoMap = pIVincElemento.InstanciaVinc.IRecuperadorInstanciaMap.RecuperarInstanciaMap(pTipo)


        Else
            elementoMap = pIVincElemento.ElementoMap
        End If


        If elementoMap Is Nothing Then
            Throw New ApplicationException("no se pudieron obtener datos de mapeado de busqueda para el tipo:" & pTipo.ToString)
        End If

        'If String.IsNullOrEmpty(elementoMap.DatosBusqueda) Then
        '    Throw New ApplicationException("no se pudieron obtener datos de busqueda para el tipo:" & pTipo.ToString)
        'Else
        ' Todo: esto debiera de cargarse
        ' si la propiedad reeridora tiene la cadnena de carga completa
        If pb.CargarDesdeTexto(pIVincElemento.ElementoMap.DatosBusqueda) Then
            '' es este caso la cadena referidora solo tiene las condiciones y se añadiran a los datos de carga de la base
            'pb.CargarDesdeTexto(elementoMap.DatosBusqueda & pIVincElemento.ElementoMap.DatosBusqueda)

        Else
            ' es este caso la cadena referidora solo tiene las condiciones y se añadiran a los datos de carga de la base
            If Not pb.CargarDesdeTexto(elementoMap.DatosBusqueda & pIVincElemento.ElementoMap.DatosBusqueda) Then
                pb.CargarDesdeTipo(pTipo)
            End If

        End If



        'End If
        pb.TipodeEntidad = pTipo
        pb.EntidadReferidora = pIVincElemento.InstanciaVinc.DN
        pb.PropiedadReferidora = CType(pIVincElemento, MV2DN.PropVinc).PropertyInfoVinc
        pb.Titulo = pIVincElemento.ElementoMap.NombreVis

        Return pb

    End Function

    Public Shared Function RecuperarParametroBusqueda(ByVal pIVincElemento As MV2DN.IVincElemento) As ParametroCargaEstructuraDN
        ' debe de haber un lugar donde para una dn se mapee sus mapeados de busqueda disponibles
        ' el mapeado de visualizacion debiera de porder pasar el nombre del mapeado de busqueda a ausar
        Try
            Dim pb As New ParametroCargaEstructuraDN()
            Dim tipo As System.Type

            If Not String.IsNullOrEmpty(pIVincElemento.ElementoMap.DatosBusqueda) Then
                pb.CargarDesdeTexto(pIVincElemento.ElementoMap.DatosBusqueda)


            Else

                Dim elementoMap As MV2DN.ElementoMapDN

                If TypeOf pIVincElemento Is MV2DN.PropVinc Then
                    Dim pv As MV2DN.PropVinc = pIVincElemento

                    If pv.EsPropiedadEncadenada Then
                        tipo = pv.TipoRepresentado
                    Else
                        If pv.EsColeccion Then
                            tipo = (pv.TipoFijadoColPropiedad)
                        Else
                            tipo = pIVincElemento.InstanciaVinc.Tipo
                        End If
                    End If

                Else
                    tipo = pIVincElemento.InstanciaVinc.Tipo
                End If



                elementoMap = pIVincElemento.InstanciaVinc.IRecuperadorInstanciaMap.RecuperarInstanciaMap(tipo)

                pb = RecuperarPB(tipo, elementoMap)
                pb.EntidadReferidora = pIVincElemento.InstanciaVinc.DN
                pb.TipodeEntidad = tipo
                pb.Titulo = pIVincElemento.ElementoMap.NombreVis
                If TypeOf pIVincElemento Is MV2DN.PropVinc Then
                    pb.PropiedadReferidora = CType(pIVincElemento, MV2DN.PropVinc).PropertyInfoVinc

                End If

            End If



            Return pb
        Catch ex As Exception
            Beep()
        End Try


    End Function

    Private Shared Function RecuperarPB(ByVal pTipo As System.Type, ByVal elementoMap As MV2DN.ElementoMapDN) As ParametroCargaEstructuraDN
        Dim pb As New ParametroCargaEstructuraDN()

        If elementoMap Is Nothing Then
            Throw New ApplicationException("no se pudieron obtener datos de mapeado de busqueda para el tipo:" & pTipo.ToString)
        End If

        If Not pb.CargarDesdeTexto(elementoMap.DatosBusqueda) Then
            pb.CargarDesdeTipo(pTipo)
        End If

        pb.TipodeEntidad = pTipo
        pb.Titulo = elementoMap.NombreVis
        Return pb

    End Function

#End Region



End Class

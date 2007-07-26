Imports Framework.DatosNegocio
Imports MotorBusquedaDN
Imports MotorBusquedaBasicasDN
Imports Framework.IU.IUComun

Public Class CtrlBusquedaGD
    Implements Framework.IU.IUComun.IctrlBasicoDN




    Protected mMap As MV2DN.PropMapDN




    <RelacionPropCampoAtribute("mMap")> _
    Public Property Map() As MV2DN.PropMapDN

        Get
            Return mMap
        End Get

        Set(ByVal value As MV2DN.PropMapDN)
            mMap = value

        End Set
    End Property



    Public Property DN() As Object Implements Framework.IU.IUComun.IctrlBasicoDN.DN
        Get

        End Get
        Set(ByVal value As Object)

        End Set
    End Property

    Public Sub DNaIUgd() Implements Framework.IU.IUComun.IctrlBasicoDN.DNaIUgd

        Try


            ControlTamaño()





            Dim informacionMapeado As MV2DN.EntradaMapNavBuscadorDN = Me.mMap.ColEntradaMapNavBuscadorDN(0)
            Dim entida As IEntidadDN = CType(BuscarPadreIctrlDinamico(), Framework.IU.IUComun.IctrlBasicoDN).DN

            If entida Is Nothing OrElse informacionMapeado Is Nothing Then
                Exit Sub
            End If


            ' cargar los datos al control de busqueda
            Dim pce As ParametroCargaEstructuraDN = MotorBusquedaIuWinCtrl.ParametrosHelper.CrearParametroCargaEstructura(informacionMapeado, entida)




            'pce.CargarDesdeTexto(Me.mMap.DatosBusqueda)
            'Dim tipoentidad As System.Type = informacionMapeado.Tipo
            '' Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo(, Nothing, tipoentidad)
            'pce.TipodeEntidad = tipoentidad

            Me.ctrlBuscadorGenerico.Inicializar()

            'Me.ctrlBuscadorGenerico.Filtro = mipaqueteconfig.Filtro
            ' Me.ctrlBuscadorGenerico.ListaValores = Me.mMap.ListaValores
            Me.ctrlBuscadorGenerico.ParametroCargaEstructura = pce
            Me.ctrlBuscadorGenerico.MultiSelect = False
            Me.ctrlBuscadorGenerico.TipoNavegacion = TipoNavegacion.Normal
            Me.ctrlBuscadorGenerico.Agregable = False
            Me.ctrlBuscadorGenerico.EnviarDatatableAlNavegar = False
            Me.ctrlBuscadorGenerico.AlternatingBackcolorResultados = Color.AliceBlue
            Me.ctrlBuscadorGenerico.AlternatingBackcolorFiltro = Color.Aquamarine
            Me.ctrlBuscadorGenerico.Navegable = informacionMapeado.EsNavegable
            Me.ctrlBuscadorGenerico.FiltroVisible = informacionMapeado.FiltroVisible
            Me.ctrlBuscadorGenerico.Filtrable = informacionMapeado.FiltroVisible
            Me.ctrlBuscadorGenerico.TituloLsitado = informacionMapeado.NombreVis


            ' Me.Text = "Buscador de: " & mipaqueteconfig.Titulo




            'le dice al control que, si tiene los datos necesarios, genere el filtro
            'de búsqueda de manera automática
            Me.ctrlBuscadorGenerico.GenerarFiltro()




            If informacionMapeado.BusquedaAutomatica AndAlso entida IsNot Nothing AndAlso informacionMapeado IsNot Nothing Then
                Me.ctrlBuscadorGenerico.buscar()
            End If
        Catch ex As Exception
            'Beep()
            'Debug.WriteLine(ex.Message)
        End Try


    End Sub

    Public Sub IUaDNgd() Implements Framework.IU.IUComun.IctrlBasicoDN.IUaDNgd



    End Sub

    Public Sub Poblar() Implements Framework.IU.IUComun.IctrlBasicoDN.Poblar
        Try
            ControlTamaño()
        Catch ex As Exception

        End Try






    End Sub




    Private Function BuscarPadreIctrlDinamico() As MV2Controles.IctrlDinamico
        Return BuscarPadreIctrlDinamico(Me.Parent)
    End Function

    Private Function BuscarPadreIctrlDinamico(ByVal pContenedor As Control) As MV2Controles.IctrlDinamico


        If pContenedor Is Nothing Then
            Return Nothing
        Else
            If TypeOf pContenedor Is MV2Controles.IctrlDinamico Then
                Return pContenedor
            Else
                Return BuscarPadreIctrlDinamico(pContenedor.Parent)
            End If
        End If

    End Function

    Private Sub ControlTamaño()


        If Me.mMap Is Nothing Then
            Throw New ApplicationException("Me.mMap  no puede ser nothing")
        End If

        Dim alto, ancho As Integer

        alto = Me.Height
        ancho = Me.Width

        If Me.mMap.Alto > -1 Then

            alto = Me.mMap.Alto
        End If


        If Me.mMap.Ancho > -1 Then
            ancho = Me.mMap.Ancho

        End If

        Me.Size = New Size(ancho, alto)



    End Sub

    Public Sub SetDN(ByVal entidad As Framework.DatosNegocio.IEntidadDN) Implements Framework.IU.IUComun.IctrlBasicoDN.SetDN

    End Sub
End Class

Imports MotorBusquedaBasicasDN
''' <summary>
''' PAQUETE:
'''   "Filtro" as MotorBusquedaDN.FiltroDN
'''   "ParametroCargaEstructura" as ParametroCargaEstructuraDN
'''   "MultiSelect" as Boolean
'''   "TipoNavegacion" as MotorIU.Motor.TipoNavegacion
'''   "Agregable" as Boolean
'''   "EnviarDatatableAlNavegar" as Boolean
'''   "Titulo" as String
'''   "AlternatingBackcolorResultados" as System.Drawing.Color
'''   "AlternatingBackcolorFiltro" as System.Drawing.Color
''' </summary>

Public Class frmFiltro
    Implements Framework.IU.IUComun.IctrlBasicoDN



    Private mipaqueteconfig As MotorBusquedaDN.PaqueteFormularioBusqueda
    Private WithEvents miBarraComandos As New MV2ControlesBasico.ctrlBarraBotonesGD




#Region "inicializar"
    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        If Not Me.Paquete Is Nothing Then

            If Me.Paquete.Contains("PaqueteFormularioBusqueda") Then

                mipaqueteconfig = Me.Paquete("PaqueteFormularioBusqueda")

                Me.ctrlBuscarGenerico.Filtro = mipaqueteconfig.Filtro
                Me.ctrlBuscarGenerico.ListaValores = mipaqueteconfig.ListaValores
                Me.ctrlBuscarGenerico.ParametroCargaEstructura = mipaqueteconfig.ParametroCargaEstructura
                Me.ctrlBuscarGenerico.MultiSelect = mipaqueteconfig.MultiSelect
                Me.ctrlBuscarGenerico.TipoNavegacion = mipaqueteconfig.TipoNavegacion
                Me.ctrlBuscarGenerico.Agregable = mipaqueteconfig.Agregable
                Me.ctrlBuscarGenerico.EnviarDatatableAlNavegar = mipaqueteconfig.EnviarDatatableAlNavegar
                Me.ctrlBuscarGenerico.AlternatingBackcolorResultados = mipaqueteconfig.AlternatingBackcolorResultados
                Me.ctrlBuscarGenerico.AlternatingBackcolorFiltro = mipaqueteconfig.AlternatingBackcolorFiltro
                Me.ctrlBuscarGenerico.Navegable = mipaqueteconfig.Navegable
                Me.ctrlBuscarGenerico.FiltroVisible = mipaqueteconfig.FiltroVisible
                Me.ctrlBuscarGenerico.Filtrable = mipaqueteconfig.Filtrable




                ' crear la barra de comandos 


                miBarraComandos.Poblar(mipaqueteconfig.ColComandoMap)
                Me.ctrlBuscarGenerico.DataGridViewXT.PanelComandos.Controls.Add(miBarraComandos)








                If String.IsNullOrEmpty(mipaqueteconfig.Titulo) Then
                    Me.Text = Me.ctrlBuscarGenerico.ParametroCargaEstructura.Titulo
                Else
                    Me.Text = "Buscador de: " & mipaqueteconfig.Titulo
                End If



                'le dice al control que, si tiene los datos necesarios, genere el filtro
                'de búsqueda de manera automática
                Me.ctrlBuscarGenerico.GenerarFiltro()




                If mipaqueteconfig.BusquedaAutomatica Then
                    Me.ctrlBuscarGenerico.buscar()
                End If


                If mipaqueteconfig IsNot Nothing AndAlso mipaqueteconfig.DevolucionAutomatica AndAlso Me.ctrlBuscarGenerico.DataGridView.Rows.Count = 1 Then
                    Me.Visible = False
                End If

            Else


                'comprobamos si hay un filtro
                If Me.Paquete.Contains("Filtro") Then
                    Me.ctrlBuscarGenerico.Filtro = CType(Me.Paquete("Filtro"), MotorBusquedaDN.FiltroDN)
                End If

                'comprobamos si hay un parámetro de carga
                If Me.Paquete.Contains("ParametroCargaEstructura") Then
                    Me.ctrlBuscarGenerico.ParametroCargaEstructura = CType(Me.Paquete("ParametroCargaEstructura"), ParametroCargaEstructuraDN)
                End If

                'comprobamos si hay multiselect
                If Me.Paquete.Contains("MultiSelect") Then
                    Me.ctrlBuscarGenerico.MultiSelect = CType(Me.Paquete("MultiSelect"), Boolean)
                End If

                'comprobamos si hay TipoNavegacion
                If Me.Paquete.Contains("TipoNavegacion") Then
                    Me.ctrlBuscarGenerico.TipoNavegacion = CType(Me.Paquete("TipoNavegacion"), MotorIU.Motor.TipoNavegacion)
                End If

                'comprobamos si hay Agregable
                If Me.Paquete.Contains("Agregable") Then
                    Me.ctrlBuscarGenerico.Agregable = CType(Me.Paquete("Agregable"), Boolean)
                End If

                'comprobamos si hay enviardatatablealnavegar
                If Me.Paquete.Contains("EnviarDatatableAlNavegar") Then
                    Me.ctrlBuscarGenerico.EnviarDatatableAlNavegar = CType(Me.Paquete("EnviarDatatableAlNavegar"), Boolean)
                End If

                'comprobamos si hay titulo para el formulario
                If Me.Paquete.Contains("Titulo") Then
                    Me.Text = Me.Paquete("Titulo").ToString
                End If

                'comprobamos si hay AlternatinBakcolorResultados
                If Me.Paquete.Contains("AlternatingBackcolorResultados") Then
                    Me.ctrlBuscarGenerico.AlternatingBackcolorResultados = CType(Me.Paquete("AlternatingBackcolorResultados"), System.Drawing.Color)
                End If

                'comprobamos si hay AlternatingBackcolorFiltro
                If Me.Paquete.Contains("AlternatingBackcolorFiltro") Then
                    Me.ctrlBuscarGenerico.AlternatingBackcolorFiltro = CType(Me.Paquete("AlternatingBackcolorFiltro"), System.Drawing.Color)
                End If

                If Me.Paquete.Contains("ListaValores") Then
                    Me.ctrlBuscarGenerico.ListaValores = CType(Me.Paquete("ListaValores"), List(Of ValorCampo))

                End If


                'le dice al control que, si tiene los datos necesarios, genere el filtro
                'de búsqueda de manera automática
                Me.ctrlBuscarGenerico.GenerarFiltro()

            End If







        End If







    End Sub
#End Region





    Private Sub frmFiltro_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load



        If mipaqueteconfig IsNot Nothing AndAlso mipaqueteconfig.DevolucionAutomatica AndAlso Me.ctrlBuscarGenerico.DataGridView.Rows.Count = 1 Then
            Me.Visible = False
            Me.ctrlBuscarGenerico.Navegar(Me.ctrlBuscarGenerico.DataGridView.Rows(0))
        Else
            '  Me.Visible = True
        End If

    End Sub

    Private Sub miBarraComandos_ComandoSolicitado(ByVal sender As Object, ByVal e As System.EventArgs) Handles miBarraComandos.ComandoSolicitado
        EjecutarCoamndo(miBarraComandos.ComandoAccioando.Map)
    End Sub

    Public Function RecuperarEntidad() As object

        Dim col As Framework.DatosNegocio.ColHEDN = RecuperarColHEDNFilaSeleccioanda()
        Select Case col.Count

            Case Is = 0
                Return Nothing

            Case Is = 1
                ' se recupera la entidad

                Dim mias As New Framework.AS.DatosBasicosAS
                Dim he As Framework.DatosNegocio.HEDN = mias.RecuperarGenerico(col(0))
                Return he.EntidadReferida

            Case Else
                Return col

        End Select

    End Function

    Public Function RecuperarColHEDNFilaSeleccioanda() As Framework.DatosNegocio.ColHEDN

        Dim col As New Framework.DatosNegocio.ColHEDN


        For a As Int16 = 0 To Me.ctrlBuscarGenerico.DataGridView.SelectedRows.Count - 1

            Dim identidad As String = CType(String.Empty & Me.ctrlBuscarGenerico.DataGridView.SelectedRows(a).Cells(0).Value, String)
            Dim he As New Framework.DatosNegocio.HEDN(mipaqueteconfig.ParametroCargaEstructura.TipodeEntidad, identidad, "")
            col.Add(he)

        Next

        Return col

    End Function

    Private Sub EjecutarCoamndo(ByVal Comando As MV2DN.ComandoMapDN)
        '  se trata de un comando de mapeado de mapeado




        Dim miIRecuperadorEjecutoresDeCliente As New Framework.Procesos.ProcesosAS.RecuperadorEjecutoresDeClienteAS

        Try

            Dim col As Framework.DatosNegocio.ColHEDN = RecuperarColHEDNFilaSeleccioanda()

            Select Case col.Count
                Case Is = 0
                    MessageBox.Show("Debe seleccionar almenos una fila")
                Case Is = 1
                    miIRecuperadorEjecutoresDeCliente.EjecutarVinculoMetodo(Me, col(0), Comando.VinculoMetodo)
                Case Else
                    miIRecuperadorEjecutoresDeCliente.EjecutarVinculoMetodo(Me, col, Comando.VinculoMetodo)
            End Select






        Catch ex As Exception
            If ex.InnerException Is Nothing Then
                Me.MostrarError(ex)
            Else
                Me.MostrarError(ex.InnerException)
            End If

        End Try

        ' refrecar los datos 
        Me.ctrlBuscarGenerico.buscar()

    End Sub

    Public Property DN() As Object Implements Framework.IU.IUComun.IctrlBasicoDN.DN
        Get
            Return RecuperarEntidad()
        End Get
        Set(ByVal value As Object)

        End Set
    End Property

    Public Sub DNaIUgd() Implements Framework.IU.IUComun.IctrlBasicoDN.DNaIUgd
        Me.ctrlBuscarGenerico.buscar()

    End Sub

    Public Sub IUaDNgd() Implements Framework.IU.IUComun.IctrlBasicoDN.IUaDNgd

    End Sub

    Public Sub Poblar() Implements Framework.IU.IUComun.IctrlBasicoDN.Poblar

    End Sub
End Class
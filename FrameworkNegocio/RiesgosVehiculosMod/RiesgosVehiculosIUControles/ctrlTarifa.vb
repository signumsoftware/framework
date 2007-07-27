Imports FN.RiesgosVehiculos.DN
Imports Framework.IU.IUComun

Public Class ctrlTarifa
    Inherits MotorIU.ControlesP.BaseControlP
    Implements Framework.IU.IUComun.IctrlBasicoDN


    Private mControlador As FN.RiesgosVehiculos.IU.Controladores.ctrlTarifa
    Private mTarifa As FN.Seguros.Polizas.DN.TarifaDN
    Private mDTProductos As DataTable
    Private mHTProductoRows As New Hashtable()
    Private mDTFormasPago As DataTable

    Private mRellenando As Boolean 'nos indica si estamos rellenando datos

    Private mColProductosAplicables As FN.Seguros.Polizas.DN.ColProductoDN

    Private mTarifaRenovacion As Boolean
    Private mNumeroSiniestros As Integer

    'private mCuestionario as 

#Region "Eventos"
    ''' <summary>
    ''' Se produce cuando el usuario hace click en "Tarificar"
    ''' </summary>
    ''' <remarks></remarks>
    Public Event EventoTarificar()
#End Region

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()
        Me.mControlador = New FN.RiesgosVehiculos.IU.Controladores.ctrlTarifa(Me.Marco, Me)
        Me.Controlador = Me.mControlador

        Me.CrearDataTableProductos()

    End Sub

#Region "propiedades"

    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public WriteOnly Property CuestionarioResuelto() As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        Set(ByVal value As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN)
            Throw New NotImplementedException("No se ha implementado la capacidad del control Tarifa para trabajar a partir de un Cuestionario Rellenao")
            'TODO: 777 - seguir aquí, generar una tarifa a partir del cuestionario que nos han pasado
        End Set
    End Property

    <System.ComponentModel.ReadOnly(True), System.ComponentModel.Browsable(False)> _
    Public Property Tarifa() As FN.Seguros.Polizas.DN.TarifaDN
        Get
            If IUaDN() Then
                Return Me.mTarifa
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As FN.Seguros.Polizas.DN.TarifaDN)
            Me.mTarifa = value
            Me.DNaIU(value)
        End Set
    End Property

    Public Property TarifaRenovacion() As Boolean
        Get
            Return mTarifaRenovacion
        End Get
        Set(ByVal value As Boolean)
            mTarifaRenovacion = value
        End Set
    End Property

    Public Property NumeroSiniestros() As Integer
        Get
            Return mNumeroSiniestros
        End Get
        Set(ByVal value As Integer)
            mNumeroSiniestros = value
        End Set
    End Property

#End Region


#Region "establecer y rellenar datos"
    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Try
            Me.mRellenando = True

            Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN = pDN
            If tarifa Is Nothing Then
                Me.cmdConductoresAdicionales.Enabled = False
                Me.dtpFechaEfecto.Enabled = False
                Me.lblAnosDiasMeses.Text = "-"
                Me.lblRiesgo.Text = "-"
                RellenarLineasProducto(Nothing)
                RellenarTodosProductos()
                Me.txtvImporte.Text = String.Empty
                Me.LimpiarPagos()
            Else
                Dim datos As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN = tarifa.DatosTarifa
                If Not datos Is Nothing Then
                    Me.cmdConductoresAdicionales.Enabled = ((Not datos.ColConductores Is Nothing) AndAlso datos.ColConductores.Count <> 0)
                    Me.txtvImporte.Text = New AuxIU.FormateadorMoneda(2).Formatear(tarifa.Importe.ToString())
                End If
                Me.dtpFechaEfecto.Value = tarifa.FEfecto
                Me.lblAnosDiasMeses.Text = tarifa.AMD.ToString()
                Dim riesgo As FN.RiesgosVehiculos.DN.RiesgoMotorDN = tarifa.Riesgo
                'If Not riesgo Is Nothing Then
                '    Me.lblRiesgo.Text = riesgo.ToString()
                '    RellenarProductosModelo(riesgo.Modelo, riesgo.Matriculado, tarifa.FEfecto)
                'Else
                '    Me.lblRiesgo.Text = "No existe riesgo asignado"
                '    RellenarTodosProductos()
                'End If
                RellenarLineasProducto(tarifa.ColLineaProducto)
                Me.RellenarPagos(tarifa.GrupoFraccionamientos)
                'deshabilitamos el botón de tarificar hasta que el usuario modifique
                'los productos seleccionados
                Me.cmdTarificar.Enabled = False
            End If
        Catch
            Throw
        Finally
            Me.mRellenando = False
        End Try

    End Sub

    Protected Overrides Function IUaDN() As Boolean
        Try
            If Me.mTarifa Is Nothing Then
                Me.mTarifa = New FN.Seguros.Polizas.DN.TarifaDN()
            End If

            'establecemos las líneas de producto
            For Each lp As FN.Seguros.Polizas.DN.LineaProductoDN In Me.mTarifa.ColLineaProducto
                Dim r As DataRow = Me.mHTProductoRows(Me.ProductoEnHTPorID(lp.Producto.ID))
                lp.Ofertado = CType(r("Ofertado"), Boolean)
                lp.Establecido = CType(r("Establecido"), Boolean)
            Next
            'establecemos la fecha de efecto de la tarifa
            Me.mTarifa.FEfecto = Me.dtpFechaEfecto.Value

            'se establece el fraccionamiento seleccionado
            If Me.dtgPagos.SelectedRows.Count > 0 Then
                Me.mTarifa.Fraccionamiento = CType(Me.dtgPagos.SelectedRows.Item(0).Cells("GrupoPagosFraccionadosDN").Value, FN.GestionPagos.DN.GrupoPagosFraccionadosDN).TipoFraccionamiento
            End If

            Return True
        Catch ex As Exception
            Me.MensajeError = ex.Message
            Return False
        End Try
    End Function
#End Region

#Region "Rellenar Pagos"
    Private Sub LimpiarPagos()
        Me.mDTFormasPago = Nothing
        Me.dtgPagos.DataSource = Nothing
    End Sub

    Private Sub RellenarPagos(ByVal pFraccionamientos As FN.GestionPagos.DN.GrupoFraccionamientosDN)
        LimpiarPagos()

        If Not pFraccionamientos Is Nothing Then
            Dim MaximoNumeroPagos As Integer = pFraccionamientos.MaximoNumeroPagos
            Me.mDTFormasPago = New DataTable
            Me.mDTFormasPago.Columns.Add(New DataColumn("GrupoPagosFraccionadosDN", GetType(FN.GestionPagos.DN.GrupoPagosFraccionadosDN)))
            Me.mDTFormasPago.Columns.Add(New DataColumn("Tipo de Pago", GetType(String)))
            'generamos las columnas con el máximo número de pagos que haya
            For a As Integer = 1 To MaximoNumeroPagos
                Me.mDTFormasPago.Columns.Add(New DataColumn("Pago " & a.ToString(), GetType(String)))
            Next

            dtgPagos.DataSource = mDTFormasPago
            dtgPagos.SuspendLayout()
            dtgPagos.DataSource = Me.mDTFormasPago
            dtgPagos.Columns("GrupoPagosFraccionadosDN").Visible = False
            dtgPagos.Columns("Tipo de Pago").Width = 100
            For a As Integer = 1 To MaximoNumeroPagos
                dtgPagos.Columns("Pago " & a.ToString()).Width = 100
            Next
            dtgPagos.ResumeLayout()

            'rellenamos con los valores que tenga cada pago fraccionado
            Dim lista As List(Of FN.GestionPagos.DN.GrupoPagosFraccionadosDN) = pFraccionamientos.ColGrupoPagosF.ListaOrdenada
            For Each pf As FN.GestionPagos.DN.GrupoPagosFraccionadosDN In lista
                Dim r As DataRow = Me.mDTFormasPago.NewRow()
                r("GrupoPagosFraccionadosDN") = pf
                r("Tipo de Pago") = pf.TipoFraccionamiento.Nombre
                Dim listapagos As List(Of FN.GestionPagos.DN.PagoFraccionadoDN) = pf.ColPagoFraccionadoDN.ListaOrdenada
                For a As Integer = 1 To listapagos.Count
                    r("Pago " & a.ToString) = AuxIU.FormateadorMonedaEurosConSimbolo.FormatearRapido(listapagos(a - 1).Importe.ToString())
                Next
                'si tiene menos pagos que el máximo, lo rellenamos con "-"
                If pf.TipoFraccionamiento.NumeroPagos < MaximoNumeroPagos Then
                    For a As Integer = pf.TipoFraccionamiento.NumeroPagos + 1 To MaximoNumeroPagos
                        r("Pago " & a.ToString()) = "-"
                    Next
                End If

                Me.mDTFormasPago.Rows.Add(r)

            Next

            Me.dtgPagos.Refresh()
        End If
    End Sub
#End Region

#Region "RellenarProductos"
    Private Sub RellenarLineasProducto(ByVal pColProductos As FN.Seguros.Polizas.DN.ColLineaProductoDN)
        LimpiarTablaProductos()
        If pColProductos IsNot Nothing Then
            Dim f As New AuxIU.FormateadorMonedaEurosConSimbolo(2)
            For Each lp As FN.Seguros.Polizas.DN.LineaProductoDN In pColProductos
                'obtenemos la fila a la que pertenece el producto al que se asocia la línea de producto
                Dim r As DataRow = Me.mHTProductoRows(ProductoEnHTPorID(lp.Producto.ID))
                'rellenamos los datos de la fila
                r("Importe") = f.Formatear(lp.ImporteLP)
                r("OLineaProducto") = lp
                r("Ofertado") = lp.Ofertado
                r("Alcanzable") = lp.Alcanzable
                r("Establecido") = lp.Establecido

                'TODO: 777 seguir aquí - falta por implementar el descuento
            Next
        End If
        Me.dtgProductos.Refresh()
        DeshabilitarNoAplicables()
        Me.dtgProductos.Columns("OLineaProducto").Visible = False
    End Sub

    Private Sub DeshabilitarNoAplicables()
        If mTarifa IsNot Nothing AndAlso mTarifa.Riesgo IsNot Nothing Then
            Dim rm As FN.RiesgosVehiculos.DN.RiesgoMotorDN = CType(mTarifa.Riesgo, FN.RiesgosVehiculos.DN.RiesgoMotorDN)

            If mColProductosAplicables Is Nothing Then
                mColProductosAplicables = Me.mControlador.RecuperarProductosModelo(rm.Modelo, rm.Matriculado, mTarifa.FEfecto)
            End If

            If mColProductosAplicables Is Nothing Then
                Throw New ApplicationException("No se han recuperado productos aplicables para el riesgo asignado")
            End If

            Dim lb As New List(Of DataRow)

            For a As Integer = 0 To Me.mDTProductos.Rows.Count - 1
                Dim r As DataRow = Me.mDTProductos.Rows(a)
                If Not mColProductosAplicables.Contiene(CType(r("OProducto"), FN.Seguros.Polizas.DN.ProductoDN), Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
                    lb.Add(r)
                End If
            Next

            For Each i As DataRow In lb
                Me.mDTProductos.Rows.Remove(i)
            Next

            Me.dtgProductos.Refresh()

            'For Each r As DataGridViewRow In Me.dtgProductos.Rows
            '    If r.Cells("OProducto").Value IsNot Nothing Then
            '        Dim producto As FN.Seguros.Polizas.DN.ProductoDN = CType(r.Cells("OProducto").Value, FN.Seguros.Polizas.DN.ProductoDN)
            '        If Not mColProductosAplicables.Contiene(producto, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
            '            r.ReadOnly = True
            '            r.Cells("Importe").Value = "-"
            '        End If
            '    End If
            'Next
        End If

    End Sub

    Private Sub LimpiarTablaProductos()
        For Each r As DataRow In Me.mDTProductos.Rows
            r("Ofertado") = False
            r("Alcanzable") = False
            r("Establecido") = False
            r("Importe") = String.Empty
            r("OLineaProducto") = Nothing
        Next
    End Sub


    Private Function ProductoEnHTPorID(ByVal pId As String) As FN.Seguros.Polizas.DN.ProductoDN
        Dim devolucion As FN.Seguros.Polizas.DN.ProductoDN = Nothing
        Dim iterador As IDictionaryEnumerator = Me.mHTProductoRows.GetEnumerator()
        While iterador.MoveNext()
            If CType(iterador.Key, FN.Seguros.Polizas.DN.ProductoDN).ID = pId Then
                devolucion = iterador.Key
                Exit While
            End If
        End While
        Return devolucion
    End Function

    Private Sub CrearDataTableProductos()
        Me.mDTProductos = New DataTable
        Me.mDTProductos.Columns.Add(New DataColumn("OLineaProducto", GetType(FN.Seguros.Polizas.DN.LineaProductoDN)))
        Me.mDTProductos.Columns.Add(New DataColumn("OProducto", GetType(FN.Seguros.Polizas.DN.ProductoDN)))
        Me.mDTProductos.Columns.Add(New DataColumn("Ofertado", GetType(Boolean)))
        Me.mDTProductos.Columns.Add(New DataColumn("Alcanzable", GetType(Boolean)))
        Me.mDTProductos.Columns.Add(New DataColumn("Establecido", GetType(Boolean)))
        Me.mDTProductos.Columns.Add(New DataColumn("Producto", GetType(String)))
        Me.mDTProductos.Columns.Add(New DataColumn("Importe", GetType(String)))
        Me.mDTProductos.Columns.Add(New DataColumn("DImporte", GetType(Double)))
        Me.mDTProductos.Columns.Add(New DataColumn("Descuento", GetType(String)))
        For Each c As DataColumn In Me.mDTProductos.Columns
            c.Caption = c.ColumnName
        Next
        RellenarTodosProductos()
        Me.dtgProductos.SuspendLayout()
        Me.dtgProductos.DataSource = Me.mDTProductos
        Me.dtgProductos.Columns("OLineaProducto").Visible = False
        Me.dtgProductos.Columns("OProducto").Visible = False
        Me.dtgProductos.Columns("DImporte").Visible = False
        Me.dtgProductos.Columns("Ofertado").Width = 50
        Me.dtgProductos.Columns("Alcanzable").Width = 50
        Me.dtgProductos.Columns("Alcanzable").ReadOnly = True
        Me.dtgProductos.Columns("Establecido").Width = 50
        Me.dtgProductos.Columns("Producto").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        Me.dtgProductos.Columns("Producto").ReadOnly = True
        Me.dtgProductos.Columns("Importe").Width = 100
        Me.dtgProductos.Columns("Importe").ReadOnly = True
        Me.dtgProductos.Columns("Importe").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        Me.dtgProductos.Columns("Importe").ReadOnly = True
        Me.dtgProductos.Columns("Descuento").Width = 100
        Me.dtgProductos.Columns("Descuento").ReadOnly = True
        Me.dtgProductos.ResumeLayout()

    End Sub

    Private Sub RellenarTodosProductos()
        Dim colLP As FN.Seguros.Polizas.DN.ColProductoDN = Me.mControlador.RecuperarProductos()
        'TODO: 777 - de momento los cargamos según los tengamos, pero hay que hacer caso al futuro campo 'orden'

        'mDTProductos.Clear()
        'CrearDataTableProductos()


        For Each p As FN.Seguros.Polizas.DN.ProductoDN In colLP
            Dim r As DataRow = Me.mDTProductos.NewRow()
            r("OProducto") = p
            r("OLineaProducto") = Nothing
            r("Ofertado") = False
            r("Alcanzable") = False
            r("Establecido") = False
            r("Producto") = p.Nombre
            r("Importe") = String.Empty
            r("DImporte") = 0
            r("Descuento") = String.Empty
            Me.mDTProductos.Rows.Add(r)
            Me.mHTProductoRows.Add(p, r)
        Next
    End Sub

    Private Sub RellenarProductosModelo(ByVal modelo As FN.RiesgosVehiculos.DN.ModeloDN, ByVal matriculado As Boolean, ByVal fecha As Date)
        Dim colLP As FN.Seguros.Polizas.DN.ColProductoDN = Me.mControlador.RecuperarProductosModelo(modelo, matriculado, fecha)

        'mDTProductos.Clear()
        CrearDataTableProductos()

        For Each p As FN.Seguros.Polizas.DN.ProductoDN In colLP
            Dim r As DataRow = Me.mDTProductos.NewRow()
            r("OProducto") = p
            r("OLineaProducto") = Nothing
            r("Ofertado") = False
            r("Alcanzable") = False
            r("Establecido") = False
            r("Producto") = p.Nombre
            r("Importe") = String.Empty
            r("DImporte") = 0
            r("Descuento") = String.Empty
            Me.mDTProductos.Rows.Add(r)
            Me.mHTProductoRows.Add(p, r)
        Next
    End Sub

#End Region

#Region "Tarificar"
    Private Sub cmdTarificar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdTarificar.Click
        Try
            Using New AuxIU.CursorScope()
                RaiseEvent EventoTarificar()

                Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN = Me.Tarifa
                If tarifa Is Nothing Then
                    MessageBox.Show(Me.MensajeError, "Tarificar", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    Exit Sub
                End If

                If mTarifaRenovacion Then
                    Me.Tarifa = Me.mControlador.Tarificar(tarifa)
                Else
                    Me.Tarifa = Me.mControlador.Tarificar(tarifa)
                End If


                Me.cmdTarificar.Enabled = False
            End Using
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

#End Region


#Region "Controlar Eventos que hacen necesario retarificar"

    Private Sub dtgProductos_CellBeginEdit(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellCancelEventArgs) Handles dtgProductos.CellBeginEdit
        Try
            If Not mRellenando Then
                NecesarioRetarificar()
            End If
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub NecesarioRetarificar()
        'limpiamos las formas de pago
        Me.LimpiarPagos()
        'quitamos el importe
        Me.txtvImporte.Text = String.Empty
        'habilitamos el botón de tarificar
        Me.cmdTarificar.Enabled = True
    End Sub

#End Region


#Region "Implementación de IctrlBasicoDN para compatibilidad con formulario genérico"
    Public Property DN() As Object Implements Framework.IU.IUComun.IctrlBasicoDN.DN
        Get
            Return Me.mTarifa
        End Get
        Set(ByVal value As Object)
            Me.mTarifa = value
        End Set
    End Property

    Public Sub DNaIUgd() Implements Framework.IU.IUComun.IctrlBasicoDN.DNaIUgd
        'no hacemos nada
        Me.DNaIU(Me.mTarifa)
    End Sub

    Public Sub IUaDNgd() Implements Framework.IU.IUComun.IctrlBasicoDN.IUaDNgd
        'si nos piden el objeto y está a medio tarificar, hacemos click en tarificar
        'para poder devolver el objeto correctamente formado
        If Me.cmdTarificar.Enabled = True AndAlso Not Me.mTarifa Is Nothing Then
            Me.cmdTarificar_Click(Nothing, Nothing)
        End If
        Me.IUaDN()
        Me.SetDN(Me.mTarifa)
    End Sub

    Public Sub Poblar() Implements Framework.IU.IUComun.IctrlBasicoDN.Poblar
        'no hacemos nada
        Me.Inicializar()
    End Sub
#End Region

    Private Sub btnNavegarRiesgo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdNavegarRiesgo.Click
        Dim mipaquete As New Hashtable()
        mipaquete.Add("DN", mTarifa.Riesgo)
        Me.Marco.Navegar("FG", Me.FormularioPadre, Nothing, TipoNavegacion.Modal, Me.GenerarDatosCarga, mipaquete, Nothing)

        If mipaquete IsNot Nothing AndAlso mipaquete.Contains("DN") Then
            mTarifa.Riesgo = mipaquete("DN")
            NecesarioRetarificar()
        End If

        Me.DNaIUgd()

    End Sub

    Private Sub cmdConductoresAdicionales_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdConductoresAdicionales.Click
        Try
            Dim dtv As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN = CType(mTarifa.DatosTarifa, FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN)
            Dim mipaquete As New Hashtable()
            mipaquete.Add("DN", dtv.ColConductores)

            If Not mipaquete Is Nothing AndAlso mipaquete.Contains("DN") Then
                dtv.ColConductores = mipaquete("DN")
                NecesarioRetarificar()
            End If
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub lblRiesgo_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles lblRiesgo.Resize
        Try
            Me.cmdNavegarRiesgo.Left = Me.lblRiesgo.Right + 25
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub
    
    Public Sub SetDN(ByVal entidad As Framework.DatosNegocio.IEntidadDN) Implements Framework.IU.IUComun.IctrlBasicoDN.SetDN
        Dim padre As Framework.IU.IUComun.IctrlBasicoDN = RecuperarPrimerPadreDinamico(Me.Parent)
        padre.SetDN(entidad)
    End Sub


    Private Function RecuperarPrimerPadreDinamico(ByVal control As System.Windows.Forms.Control) As Framework.IU.IUComun.IctrlBasicoDN

        If control Is Nothing Then
            Return Nothing
        End If


        If TypeOf control Is Framework.IU.IUComun.IctrlBasicoDN Then
            Return control
        Else
            Return RecuperarPrimerPadreDinamico(control.Parent)
        End If

    End Function
End Class

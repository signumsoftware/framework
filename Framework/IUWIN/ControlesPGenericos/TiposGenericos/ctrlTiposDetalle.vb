Imports Framework.DatosNegocio

Public Class ctrlTiposDetalle

#Region "Atributos"

    Private mTipoAdministrable As IEntidadDN
    'Private mControlador As ctrlTiposDetalle

    Private mTipo As Type

#End Region

#Region "Inicializador"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        ''Establecemos el controlador
        'Me.mControlador = New Controladores.ctrlTiposDetalle(Me.Marco, Me)

    End Sub

#End Region

#Region "Propiedades"

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Public Property TipoAdministrable() As IEntidadDN
        Get
            If Me.IUaDN() Then
                Return Me.mTipoAdministrable
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As IEntidadDN)
            Me.mTipoAdministrable = value
            Me.DNaIU(value)
        End Set
    End Property

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Property Tipo() As Type
        Get
            Return Me.mTipo
        End Get
        Set(ByVal value As Type)
            Me.mTipo = value
        End Set
    End Property

#End Region

#Region "Establecer y rellenar DN"

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Dim miTipo As IEntidadDN
        Dim miObj As Object

        Try
            miTipo = pDN
            If miTipo Is Nothing Then
                Me.txtNombre.Text = ""
                Me.txtOrden.Text = 0
                Me.HabilitarDeshabilitarTodo(False)
            Else
                miObj = System.Activator.CreateInstance(Me.mTipo)
                miObj = miTipo
                Me.txtNombre.Text = miTipo.Nombre
                Me.txtOrden.Text = CInt(miObj.Orden * 100)
                Me.HabilitarDeshabilitarTodo(True)
            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Protected Overrides Function IUaDN() As Boolean
        Dim miObj As Object
        Try
            If Me.ErroresValidadores.Count <> 0 Then
                Return False
            End If

            miObj = System.Activator.CreateInstance(Me.mTipo)
            miObj = Me.mTipoAdministrable

            If Me.txtNombre.Text = "" Then
                If miObj.Nombre <> "" Then
                    Me.txtNombre.Text = miObj.Nombre
                    MsgBox("El campo nombre no puede estar vacío", MsgBoxStyle.Exclamation, "Crear tipo")
                End If
                Return True
            End If

            If Me.txtOrden.Text = "" Then
                MsgBox("El campo orden no puede estar vacíos", MsgBoxStyle.Exclamation, "Crear tipo")
                If miObj.Orden <> "" Then
                    Me.txtOrden.Text = miObj.Orden
                Else
                    Me.txtOrden.Text = 0
                End If
                Return True
            End If

            If Me.mTipoAdministrable Is Nothing Then
                miObj.Nombre = Me.txtNombre.Text
                miObj.Orden = Me.txtOrden.Text / 100
            Else
                miObj.Nombre = Me.txtNombre.Text
                miObj.Orden = Me.txtOrden.Text / 100
            End If

            Return True
        Catch ex As Exception
            Throw ex
        End Try
    End Function

#End Region

#Region "Eventos"
    Public Event Refrescar As EventHandler
#End Region

#Region "Manejadores de eventos"

    Private Sub txtNombre_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtNombre.LostFocus
        Try
            RaiseEvent Refrescar(sender, e)
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub txtOrden_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtOrden.LostFocus
        Try
            If Not IsNumeric(Me.txtOrden.Text) OrElse CInt(txtOrden.Text) > 100 OrElse CInt(txtOrden.Text) < 0 Then
                txtOrden.Text = 0
                Throw New ApplicationException("El orden debe ser un número entero entre 0 y 100")
            End If
            RaiseEvent Refrescar(sender, e)
        Catch ex As Exception
            MostrarError(ex, sender)
        End Try
    End Sub

#End Region

#Region "Métodos"

    Private Sub HabilitarDeshabilitarTodo(ByVal pValor As Boolean)
        Try
            Me.txtNombre.Enabled = pValor
            Me.txtOrden.Enabled = pValor
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

#End Region

End Class

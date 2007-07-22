Imports Framework.DatosNegocio

Public Class ctrlTipos

#Region "Atributos"

    Private mColTipos As IList

    Private mColTiposAlta As IList
    Private mColTiposBaja As IList

    Private mTipoSeleccionadoAlta As IEntidadDN
    Private mTipoSeleccionadoBaja As IEntidadDN

    'Private mControlador As Controladores.ctrlTipos

    Private mTipo As Type

#End Region

#Region "Inicializar"

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        'Establecemos el controlador
        'mControlador = New Controladores.ctrlTipos(Me.Marco, Me)

        AddHandler CtrlListadoTiposAlta.TipoSeleccionado, AddressOf LeerTipoSeleccionado

        AddHandler CtrlListadoTiposBaja.TipoSeleccionado, AddressOf LeerTipoSeleccionado

    End Sub

#End Region

#Region "Propiedades"

    <System.ComponentModel.Browsable(False), System.ComponentModel.ReadOnly(True)> _
    Property ColTipos() As IList
        Get
            If Me.IUaDN() Then
                Return Me.mColTipos
            Else
                Return Nothing
            End If
        End Get
        Set(ByVal value As IList)
            Me.mColTipos = value
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

#Region "Establecer y rellenar datos"

    Protected Overrides Sub DNaIU(ByVal pDN As Object)
        Dim miColTipos As IList 'List(Of Framework.DatosNegocio.TipoConOrdenDN)

        Try
            miColTipos = pDN
            If miColTipos Is Nothing Then
                Me.CtrlListadoTiposAlta.Tipo = Me.mTipo
                Me.CtrlListadoTiposBaja.Tipo = Me.mTipo
                Me.CtrlTiposDetalle1.Tipo = Me.mTipo

                Me.CtrlTiposDetalle1.TipoAdministrable = Nothing

                Me.mColTipos = New ArrayList()
                Me.mColTiposAlta = New ArrayList()
                Me.mColTiposBaja = New ArrayList()
                Me.CtrlListadoTiposAlta.ColTipos = Me.mColTiposAlta
                Me.CtrlListadoTiposBaja.ColTipos = Me.mColTiposBaja
                Me.CtrlListadoTiposBaja.ColSeleccionado.Clear()
                Me.CtrlListadoTiposAlta.ColSeleccionado.Clear()

            Else
                Me.CtrlListadoTiposAlta.Tipo = Me.mTipo
                Me.CtrlListadoTiposBaja.Tipo = Me.mTipo
                Me.CtrlTiposDetalle1.Tipo = Me.mTipo

                Me.CtrlTiposDetalle1.TipoAdministrable = Nothing

                Me.mColTiposAlta = New ArrayList()
                Me.mColTiposBaja = New ArrayList()

                Me.ActualizarListadosTipos(miColTipos)

            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Protected Overrides Function IUaDN() As Boolean
        Dim miTipo As IEntidadDN

        Try
            If Me.ErroresValidadores.Count <> 0 Then
                Return False
            End If

            Me.mColTipos = New ArrayList()
            For Each miTipo In Me.CtrlListadoTiposAlta.ColTipos
                Me.mColTipos.Add(miTipo)
            Next

            For Each miTipo In Me.CtrlListadoTiposBaja.ColTipos
                Me.mColTipos.Add(miTipo)
            Next

            Return True

        Catch ex As Exception
            Throw ex
        End Try

    End Function

#End Region

#Region "Manejadores de eventos"

    Private Sub btnNuevoTipo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnNuevoTipo.Click
        Try
            Me.AgregarTipo()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub btnBaja_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBaja.Click
        Try
            Me.AsignarBajaTipoSeleccionado()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub btnAltaTipo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAltaTipo.Click
        Try
            Me.AsignarAltaTipoSeleccionado()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

    Private Sub CtrlTiposDetalle1_Refrescar(ByVal sender As Object, ByVal e As System.EventArgs) Handles CtrlTiposDetalle1.Refrescar
        Try
            Me.mTipoSeleccionadoAlta = Me.CtrlTiposDetalle1.TipoAdministrable
            Me.CtrlListadoTiposAlta.Refresh()
        Catch ex As Exception
            Me.MostrarError(ex, sender)
        End Try
    End Sub

#End Region

#Region "Métodos"

    Private Sub AgregarTipo()
        Dim miTipo As IEntidadDN

        Try
            miTipo = System.Activator.CreateInstance(Me.mTipo)

            Me.mColTipos.Add(miTipo)
            Me.CtrlListadoTiposAlta.ColTipos.Add(miTipo)
            Me.CtrlListadoTiposAlta.Refresh()

        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub LeerTipoSeleccionado(ByVal pTipo As IEntidadDN, ByVal sender As Object)
        Try
            If sender Is Me.CtrlListadoTiposAlta Then
                Me.mTipoSeleccionadoAlta = pTipo
                Me.CtrlTiposDetalle1.TipoAdministrable = Me.mTipoSeleccionadoAlta
            Else
                Me.mTipoSeleccionadoBaja = pTipo
            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub AsignarBajaTipoSeleccionado()
        Dim objBaja As Framework.DatosNegocio.IDatoPersistenteDN

        Try
            If Me.CtrlListadoTiposAlta.ColSeleccionado.Item(0) IsNot Nothing Then
                objBaja = Me.CtrlListadoTiposAlta.ColSeleccionado.Item(0)
                objBaja.Baja = True
                Me.mColTiposAlta.Remove(objBaja)
                Me.mColTiposBaja.Add(objBaja)
                Me.CtrlListadoTiposAlta.ColTipos = Me.mColTiposAlta
                Me.CtrlListadoTiposBaja.ColTipos = Me.mColTiposBaja
            Else
                Throw New ApplicationException("No ha seleccionado ningún tipo")
            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub AsignarAltaTipoSeleccionado()
        Dim objBaja As Framework.DatosNegocio.IDatoPersistenteDN

        Try
            If Me.CtrlListadoTiposBaja.ColSeleccionado.Item(0) IsNot Nothing Then
                objBaja = Me.CtrlListadoTiposBaja.ColSeleccionado.Item(0)
                objBaja.Baja = False
                Me.mColTiposBaja.Remove(objBaja)
                Me.mColTiposAlta.Add(objBaja)
                Me.CtrlListadoTiposAlta.ColTipos = Me.mColTiposAlta
                Me.CtrlListadoTiposBaja.ColTipos = Me.mColTiposBaja
            Else
                Throw New ApplicationException("No ha seleccionado ningún tipo")
            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub ActualizarListadosTipos(ByVal pColTipos As IList)
        Dim miTipo As IEntidadDN

        Try
            'Me.mColTiposAlta.Clear()
            'Me.mColTiposBaja.Clear()

            For Each miTipo In pColTipos
                '   Me.mColTipos.Add(miTipo)
                If miTipo.Baja Then
                    Me.mColTiposBaja.Add(miTipo)
                Else
                    Me.mColTiposAlta.Add(miTipo)
                End If
            Next

            Me.CtrlListadoTiposAlta.ColTipos = Me.mColTiposAlta
            Me.CtrlListadoTiposBaja.ColTipos = Me.mColTiposBaja
        Catch ex As Exception
            Throw ex
        End Try
    End Sub
#End Region

End Class

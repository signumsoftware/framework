Imports System.Drawing
Imports system.Drawing.Drawing2D


Public Class textboxXT
    Inherits ControlesPBase.txtValidable


#Region "atributos"
    Private mExtendido As Boolean = False
    Private mExtendidoSiexcede As Boolean = True
    'la imagen que se muestra para extender el contenido
    Private mImagenXT As ImagenTextBoxXT
    Private mEstadoXTEnlazado As Boolean = True
    Private mReadonlyXT As Boolean = False
#End Region


#Region "propiedades"
    <System.ComponentModel.DefaultValue(True), System.ComponentModel.Description("Determina si el Readonly del textbox de detalle se corresponden con el del TextboxXT")> _
    Public Property EstadoXTEnlazado() As Boolean
        Get
            Return Me.mEstadoXTEnlazado
        End Get
        Set(ByVal value As Boolean)
            Me.mEstadoXTEnlazado = value
        End Set
    End Property


    <System.ComponentModel.Description("Determina si el Textbox de detalle es Readonly (si EstadoXTEnlazado es Verdadero, sólo se puede leer y dará un error en el Set)")> _
    Public Property ReadonlyXT() As Boolean
        Get
            If Me.mEstadoXTEnlazado Then
                'If Me.ReadOnly OrElse (Not Me.Enabled) Then
                '    Me.mReadonlyXT = True
                'Else
                '    Me.mReadonlyXT = False
                'End If
                Me.mReadonlyXT = (Me.ReadOnly OrElse Not Me.Enabled)
            End If
            Return Me.mReadonlyXT
        End Get
        Set(ByVal value As Boolean)
            If Not Me.mEstadoXTEnlazado Then
                Me.mReadonlyXT = value
            End If
        End Set
    End Property

    <System.ComponentModel.Description("Determina si se comporta como un textbox extendido o no")> _
    Public Property Extendido() As Boolean
        Get
            Return Me.mExtendido
        End Get
        Set(ByVal value As Boolean)
            Me.mExtendido = value
            If Not Me.mExtendido Then
                EliminarImagen()
            Else
                CrearImagen()
            End If
        End Set
    End Property

    <System.ComponentModel.Description("Determina si se comporta como extendido de manera automática cuando el texto no cabe en el textbox")> _
    <System.ComponentModel.DefaultValue(True)> _
    Public Property ExtendidoSiExcede() As Boolean
        Get
            Return Me.mExtendidoSiexcede
        End Get
        Set(ByVal value As Boolean)
            Me.mExtendidoSiexcede = value
        End Set
    End Property

    Private Sub EliminarImagen()
        If Not Me.mImagenXT Is Nothing Then
            Me.Parent.Controls.Remove(Me.mImagenXT)
            Me.mImagenXT = Nothing
        End If
    End Sub

    Private Sub CrearImagen()
        If Me.mImagenXT Is Nothing Then
            Me.mImagenXT = New ImagenTextBoxXT(Me)
            Me.Parent.Controls.Add(Me.mImagenXT)
            Me.mImagenXT.BringToFront()
            Me.Parent.Refresh()
        End If
    End Sub
#End Region

#Region "Eventos"
    Private Sub textboxXT_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.TextChanged
        Try
            CalcularExtendidoSiExcede()
        Catch ex As Exception
            MessageBox.Show(ex.Message, "Error en controlP Base: textboxXT", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Protected Overrides Sub OnResize(ByVal e As System.EventArgs)
        MyBase.OnResize(e)
        Try
            CalcularExtendidoSiExcede()
        Catch ex As Exception
            MessageBox.Show(ex.Message, "Error en controlP Base: textboxXT", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub CalcularExtendidoSiExcede()
        If Me.mExtendidoSiexcede Then
            'si el contenido es mayor que el textbox, hacemos que sea extendido
            Dim g As Graphics = Me.CreateGraphics()
            Me.Extendido = (g.MeasureString(Me.Text, Me.Font).Width > Me.Width)
            g.Dispose()
        End If
    End Sub
#End Region


End Class

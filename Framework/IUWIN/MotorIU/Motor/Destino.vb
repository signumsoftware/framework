Namespace Motor
    'éste objeto contiene un par Controlador/Formulario
    'y sirve para las definiciones de navegación
    Public Class Destino

#Region "campos"
        'Private mFormulario As String
        'Private mControlador As String
        'Private mEnsanmblado As String

        Public Formulario As Type
        Public Controlador As Type


#End Region

#Region "constructor"
        Public Sub New()

        End Sub

        Public Sub New(ByVal pFormulario As Type, ByVal pControlador As Type)
            Me.Formulario = pFormulario
            Me.Controlador = pControlador
        End Sub

        'Public Sub New(ByVal pFormulario As String, ByVal pControlador As String)
        '    mFormulario = pFormulario
        '    mControlador = pControlador
        'End Sub

        'Public Sub New(ByVal pEnsamblado As String, ByVal pFormulario As String, ByVal pControlador As String)
        '    mEnsanmblado = pEnsamblado
        '    mFormulario = pFormulario
        '    mControlador = pControlador
        'End Sub
#End Region

#Region "propiedades"
        'Public Property Formulario() As String
        '    Get
        '        Return mFormulario
        '    End Get
        '    Set(ByVal Value As String)
        '        mFormulario = Value
        '    End Set
        'End Property

        'Public Property Controlador() As String
        '    Get
        '        Return mControlador
        '    End Get
        '    Set(ByVal Value As String)
        '        mControlador = Value
        '    End Set
        'End Property


        'Public Property Ensamblado() As String
        '    Get
        '        Return Me.mEnsanmblado
        '    End Get
        '    Set(ByVal value As String)
        '        Me.mEnsanmblado = value
        '    End Set
        'End Property
#End Region

    End Class
End Namespace

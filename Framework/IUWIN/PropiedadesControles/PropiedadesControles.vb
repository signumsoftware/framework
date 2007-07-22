Imports System.Drawing

<Serializable()> Public Class PropiedadesControlP
    Implements ICloneable


#Region "campos"
    Private mColorFondo As Color 'el backcolor del control
    Private mColorOver As Color 'el backcolor del control cuando el cursor está encima
    Private mColorEdicion As Color 'el backcolor de los txtbox, listbox...
    Private mColorConsulta As Color 'el backcolor de txtbox, listbox... cuando están en sólo lectura
    Private mTipoControl As modControlesp.TipoControl 'si es de sólo lectura o de escritura
    Private mImagenFondo As Image 'la imagen de fondo que se muestra en el control
    Private mImagenOver As Image 'la imagen que se muestra cuando el cursor está encima
    Private mForeColor As Color 'el forecolor del control
    Private mForeColorOver As Color 'el forecolor cuando está el cursor encima
    Private mForeColorError As Color 'el forecolor del control cuando haya una error
    Private mMensajeError As String 'el mensaje que nos da el control cuando hay un error
    Private mFont As Font 'estilo del texto 
    Private mTituloFont As Font 'fuente para títulos
    Private mTituloForeColor As Color 'color fuente de los títulos
    Private mObligatorioForeColor As Color  'color de los elementos obligatorios
    Private mObligatorioFont As Font
    'Private mFlatStyle As System.Windows.Forms.FlatStyle 'el estilo 3d
#End Region

#Region "Constructor"
    Public Sub New()

    End Sub

    Public Sub New(ByVal pColorFondo As Color, ByVal pColorOver As Color, ByVal pColorEdicion As Color, ByVal pcolorConsulta As Color, ByVal pTipoControl As modControlesp.TipoControl, ByVal pImagenFondo As Image, ByVal pImagenOver As Image, ByVal pForeColor As Color, ByVal pForeColorerror As Color, ByVal pMensajeError As String, ByVal pFont As Font, ByVal pForeColorOver As Color)
        ColorFondo = pColorFondo
        ColorEdicion = pColorEdicion
        ColorConsulta = pcolorConsulta
        ColorOver = pColorOver
        TipoControl = pTipoControl
        ImagenFondo = pImagenFondo
        ImagenOver = pImagenOver
        MensajeError = pMensajeError
        Font = pFont
        ForeColorOver = pForeColorOver
    End Sub

#End Region

#Region "propiedades"

    ''el estilo por defecto es el del sistema
    '<System.ComponentModel.DefaultValue(System.Windows.Forms.FlatStyle.Standard)> _
    'Public Property FlatStyle() As System.Windows.Forms.FlatStyle
    '    Get
    '        Return Me.mFlatStyle
    '    End Get
    '    Set(ByVal value As System.Windows.Forms.FlatStyle)
    '        Me.mFlatStyle = value
    '    End Set
    'End Property

    Public Property ObligatorioFont() As Font
        Get
            Return mObligatorioFont
        End Get
        Set(ByVal Value As Font)
            mObligatorioFont = Value
        End Set
    End Property

    Public Property ObligatorioForeColor() As Color
        Get
            Return mObligatorioForeColor
        End Get
        Set(ByVal Value As Color)
            mObligatorioForeColor = Value
        End Set
    End Property

    Public Property ForeColorOver() As Color
        Get
            Return mForeColorOver
        End Get
        Set(ByVal Value As Color)
            mForeColorOver = Value
        End Set
    End Property

    Public Property TituloForeColor() As Color
        Get
            Return mTituloForeColor
        End Get
        Set(ByVal Value As Color)
            mTituloForeColor = Value
        End Set
    End Property

    Public Property TituloFont() As Font
        Get
            Return mTituloFont
        End Get
        Set(ByVal Value As Font)
            mTituloFont = Value
        End Set
    End Property

    Public Property ColorOver() As Color
        Get
            Return mColorOver
        End Get
        Set(ByVal Value As Color)
            mColorOver = Value
        End Set
    End Property

    Public Property Font() As Font
        Get
            Return mFont
        End Get
        Set(ByVal Value As Font)
            mFont = Value
        End Set
    End Property

    Public Property MensajeError() As String
        Get
            Return mMensajeError
        End Get
        Set(ByVal Value As String)
            If Value Is Nothing Then
                Value = ""
            End If
            mMensajeError = Value
        End Set
    End Property

    Public Property ForeColorError() As Color
        Get
            Return mForeColorError
        End Get
        Set(ByVal Value As Color)
            mForeColorError = Value
        End Set
    End Property

    Public Property ForeColor() As Color
        Get
            Return mForeColor
        End Get
        Set(ByVal Value As Color)
            mForeColor = Value
        End Set
    End Property

    Public Property ImagenOver() As Image
        Get
            Return mImagenOver
        End Get
        Set(ByVal Value As Image)
            mImagenOver = Value
        End Set
    End Property

    Public Property ImagenFondo() As Image
        Get
            Return mImagenFondo
        End Get
        Set(ByVal Value As Image)
            mImagenFondo = Value
        End Set
    End Property

    Public Property TipoControl() As modControlesp.TipoControl
        Get
            Return mTipoControl
        End Get
        Set(ByVal Value As modControlesp.TipoControl)
            mTipoControl = Value
        End Set
    End Property

    Public Property ColorFondo() As Color
        Get
            Return mColorFondo
        End Get
        Set(ByVal Value As Color)
            mColorFondo = Value
        End Set
    End Property

    Public Property ColorEdicion() As Color
        Get
            Return mColorEdicion
        End Get
        Set(ByVal Value As Color)
            mColorEdicion = Value
        End Set
    End Property

    Public Property ColorConsulta() As Color
        Get
            Return mColorConsulta
        End Get
        Set(ByVal Value As Color)
            mColorConsulta = Value
        End Set
    End Property
#End Region

    Public Function Clone() As Object Implements System.ICloneable.Clone
        'devuelve un objeto idéntico (pero diferente)
        Dim miPropiedadesControl As PropiedadesControlP

        miPropiedadesControl = New PropiedadesControlP

        miPropiedadesControl.ColorConsulta = Me.mColorConsulta
        miPropiedadesControl.ColorEdicion = Me.mColorEdicion
        miPropiedadesControl.ColorFondo = Me.mColorFondo
        miPropiedadesControl.ColorOver = Me.ColorOver
        miPropiedadesControl.Font = Me.mFont
        miPropiedadesControl.ForeColor = Me.mForeColor
        miPropiedadesControl.ForeColorError = Me.mForeColorError
        miPropiedadesControl.ForeColorOver = Me.mForeColorOver
        miPropiedadesControl.ImagenFondo = Me.mImagenFondo
        miPropiedadesControl.ImagenOver = Me.mImagenOver
        miPropiedadesControl.MensajeError = Me.mMensajeError
        miPropiedadesControl.TipoControl = Me.mTipoControl
        miPropiedadesControl.TituloFont = Me.mTituloFont
        miPropiedadesControl.TituloForeColor = Me.mTituloForeColor
        'miPropiedadesControl.FlatStyle = Me.mFlatStyle


        Return miPropiedadesControl

    End Function
End Class

<Serializable()> Public Module modControlesp '-->aquí se definen las const y los enum, types... públicos del GUI

    'Enumeración que describe el comportamiento del control
    <Serializable()> Public Enum TipoControl As Short
        Entrada = 0 'sólo lectura
        Salida = 1 'escritura (permite agregar y editar los datos)
    End Enum

End Module


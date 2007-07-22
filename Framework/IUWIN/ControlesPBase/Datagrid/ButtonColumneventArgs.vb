Imports System
Imports System.Windows.Forms

Namespace Datagrid

    'argumento de evento para un click en un botón de DataGridButtonColumn
    Public Class ButtonColumnEventArgs
        Inherits EventArgs

#Region "campos"
        Private mRowNum As Integer
        Private mColumNum As Integer
        Private mButtonValue As String
#End Region

#Region "constructor"
        Public Sub New(ByVal pRowNum As Integer, ByVal pColNum As Integer, ByVal pButtonValue As String)
            mRowNum = pRowNum
            mColumNum = pColNum
            mButtonValue = pButtonValue
        End Sub
#End Region

#Region "propiedades"
        Public ReadOnly Property Column() As Integer
            Get
                Return mColumNum
            End Get
        End Property

        Public ReadOnly Property Row() As Integer
            Get
                Return mRowNum
            End Get
        End Property

        Public ReadOnly Property ButtonValue() As String
            Get
                Return mButtonValue
            End Get
        End Property
#End Region

    End Class

End Namespace
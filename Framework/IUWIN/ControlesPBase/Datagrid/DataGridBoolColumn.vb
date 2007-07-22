Namespace Datagrid

    Public Class DatagridBoolColumn
        Inherits Windows.Forms.DataGridBoolColumn

#Region "métodos"

        Protected Overrides Sub Abort(ByVal rowNum As Integer)
            MyBase.Abort(rowNum)
        End Sub

        Protected Overrides Function Commit(ByVal dataSource As System.Windows.Forms.CurrencyManager, ByVal rowNum As Integer) As Boolean
            MyBase.Commit(dataSource, rowNum)
        End Function

        Protected Overloads Overrides Sub Edit(ByVal source As System.Windows.Forms.CurrencyManager, ByVal rowNum As Integer, ByVal bounds As System.Drawing.Rectangle, ByVal [readOnly] As Boolean, ByVal instantText As String, ByVal cellIsVisible As Boolean)
            MyBase.Edit(source, rowNum, bounds, [readOnly], instantText, cellIsVisible)
        End Sub

        Protected Overrides Function GetMinimumHeight() As Integer
            MyBase.GetMinimumHeight()
        End Function

        Protected Overrides Function GetPreferredHeight(ByVal g As System.Drawing.Graphics, ByVal value As Object) As Integer
            MyBase.GetPreferredHeight(g, value)
        End Function

        Protected Overrides Function GetPreferredSize(ByVal g As System.Drawing.Graphics, ByVal value As Object) As System.Drawing.Size
            MyBase.GetPreferredSize(g, value)
        End Function

        Protected Overloads Overrides Sub Paint(ByVal g As System.Drawing.Graphics, ByVal bounds As System.Drawing.Rectangle, ByVal source As System.Windows.Forms.CurrencyManager, ByVal rowNum As Integer)




            MyBase.Paint(g, bounds, source, rowNum)
        End Sub

        Protected Overloads Overrides Sub Paint(ByVal g As System.Drawing.Graphics, ByVal bounds As System.Drawing.Rectangle, ByVal source As System.Windows.Forms.CurrencyManager, ByVal rowNum As Integer, ByVal alignToRight As Boolean)
            MyBase.Paint(g, bounds, source, rowNum, alignToRight)
        End Sub

#End Region

    End Class

End Namespace

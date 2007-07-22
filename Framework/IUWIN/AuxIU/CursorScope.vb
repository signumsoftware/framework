Imports System.Windows.Forms

Public Class CursorScope
    Implements IDisposable

    Private mCursorAnterior As Cursor

    Public Sub New()
        EstablecerCursor(Cursors.WaitCursor)
    End Sub

    Public Sub New(ByVal pCursor As Cursor)
        EstablecerCursor(pCursor)
    End Sub

    Private Sub EstablecerCursor(ByVal pCursor As Cursor)
        mCursorAnterior = Cursor.Current
        Cursor.Current = pCursor
    End Sub

    Private disposedValue As Boolean = False        ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            Cursor.Current = mCursorAnterior
        End If
        Me.disposedValue = True
    End Sub

#Region " IDisposable Support "
    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
#End Region

End Class


'public class CursorScope:IDisposable
'    {
'        Cursor anterior;

'        public CursorScope() : this(Cursors.WaitCursor) { }
'        public CursorScope(Cursor nuevoCursor)
'        {
'            anterior = Cursor.Current; 
'            Cursor.Current = nuevoCursor;
'        }
'        public void Dispose()
'        {
'            Cursor.Current = anterior; 
'        }
'    }
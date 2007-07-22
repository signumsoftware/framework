''' <summary>
''' Esta clase representa un proxy que encapsula una transaccion contra un recurso.
''' </summary>
Public Class TransaccionProxyAD
    Implements IDbTransaction
    Implements IDisposable

#Region "Delegados"
    Delegate Sub CancelarTransaccion()
    Dim mDCT As CancelarTransaccion
#End Region

#Region "Atributos"
    Protected mTR As IDbTransaction
#End Region

#Region "Constructores"
    Public Sub New(ByVal pTR As IDbTransaction)
        mTR = pTR
    End Sub

    Public Sub New(ByVal pTR As IDbTransaction, ByVal pDelegadoCancelacion As CancelarTransaccion)
        mTR = pTR
        mDCT = pDelegadoCancelacion
    End Sub
#End Region

#Region "Propiedades"
    Public ReadOnly Property Connection() As System.Data.IDbConnection Implements System.Data.IDbTransaction.Connection
        Get
            Return Me.mTR.Connection
        End Get
    End Property

    Public ReadOnly Property IsolationLevel() As System.Data.IsolationLevel Implements System.Data.IDbTransaction.IsolationLevel
        Get
            Return Me.mTR.IsolationLevel
        End Get
    End Property
#End Region

#Region "Metodos"
    Public Sub Commit() Implements System.Data.IDbTransaction.Commit
        'Throw New NotImplementedException
    End Sub

    Public Sub Rollback() Implements System.Data.IDbTransaction.Rollback
        ' en este caso se llama a la cancelacion de la transaccion mediante el delagado
        ' de momento cancelamo por error
        If Not mDCT Is Nothing Then
            mDCT.Invoke()
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        'Throw New NotImplementedException
    End Sub
#End Region

End Class

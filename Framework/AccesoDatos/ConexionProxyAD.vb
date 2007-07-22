''' <summary>
''' Esta clase representa un proxy que encapsula una conexion contra un recurso.
''' </summary>
Public Class ConexionProxyAD
    Implements IDbConnection

#Region "Atributos"
    'Conexion de la que hacemos de proxy
    Protected mCN As IDbConnection

    'TODO: ESTO QUE ES???
    Protected mDT As DatosTransaccionGenericoBDAD
#End Region

#Region "Constructores"
    ''' <summary>Constructor por defecto que acepta una transaccion sobre un recurso.</summary>
    ''' <param name="pTR" type="ITransaccionRecurso">
    ''' ITransaccionRecurso del que sacamos los datos de transaccion y conexion.
    ''' </param>
    Public Sub New(ByVal pTR As ITransaccionRecursoLN)
        mDT = pTR.DatosTransaccion
        mCN = mDT.Conexion
    End Sub
#End Region

#Region "Propiedades"
    ''' <summary>Obtiene o asigna la cadena de conexion.</summary>
    ''' <remarks>El set no esta implementado.</remarks>
    Public Property ConnectionString() As String Implements IDbConnection.ConnectionString
        Get
            Return mCN.ConnectionString
        End Get
        Set(ByVal Value As String)
            Throw New NotImplementedException
        End Set
    End Property

    ''' <summary>Obtiene el timeout de la conexion.</summary>
    Public ReadOnly Property ConnectionTimeout() As Integer Implements IDbConnection.ConnectionTimeout
        Get
            Return mCN.ConnectionTimeout
        End Get
    End Property

    ''' <summary>Obtiene el estado de la conexion.</summary>
    Public ReadOnly Property State() As ConnectionState Implements IDbConnection.State
        Get
            Return mCN.State
        End Get
    End Property

    ''' <summary>Obtiene la base de datos de la conexion.</summary>
    Public ReadOnly Property Database() As String Implements IDbConnection.Database
        Get
            Return mCN.Database
        End Get
    End Property
#End Region

#Region "Metodos"
    Public Overloads Function BeginTransaction(ByVal pIl As IsolationLevel) As IDbTransaction Implements IDbConnection.BeginTransaction
        Dim transacProxy As New TransaccionProxyAD(mDT.Transaccion)

        Return transacProxy
    End Function

    Public Overloads Function BeginTransaction() As IDbTransaction Implements IDbConnection.BeginTransaction
        Dim transacProxy As New TransaccionProxyAD(mDT.Transaccion)

        Return transacProxy
    End Function

    Public Sub ChangeDatabase(ByVal pDatabaseName As String) Implements IDbConnection.ChangeDatabase
        'Throw New NotImplementedException
    End Sub

    Public Sub Close() Implements IDbConnection.Close
        'Throw New NotImplementedException
    End Sub

    Public Function CreateCommand() As IDbCommand Implements IDbConnection.CreateCommand
        Dim cm As IDbCommand

        cm = mCN.CreateCommand
        cm.Transaction = mDT.Transaccion

        Return cm
    End Function

    Public Sub Open() Implements IDbConnection.Open
        'Throw New NotImplementedException
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        'Throw New NotImplementedException
    End Sub
#End Region

End Class

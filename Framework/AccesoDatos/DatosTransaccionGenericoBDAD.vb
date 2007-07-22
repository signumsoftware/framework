''' <summary>Esta clase proporciona informacion sobre una transaccion y su conexion.</summary>
Public Class DatosTransaccionGenericoBDAD
    Implements IDatosTransaccionGenericoBDAD

#Region "Atributos"
    'Conexion donde se desarrolla la transaccion
    Protected mConexion As IDbConnection

    'Transaccion
    Protected mTransaccion As IDbTransaction
#End Region

#Region "Propiedades"
    ''' <summary>Obtiene o asigna la conexion.</summary>
    Public Property Conexion() As System.Data.IDbConnection Implements IDatosTransaccionGenericoBDAD.Conexion
        Get
            Return mConexion
        End Get
        Set(ByVal Value As System.Data.IDbConnection)
            mConexion = Value
        End Set
    End Property

    ''' <summary>Obtiene o asigna la transaccion.</summary>
    Public Property Transaccion() As System.Data.IDbTransaction Implements IDatosTransaccionGenericoBDAD.Transaccion
        Get
            Return Me.mTransaccion
        End Get
        Set(ByVal Value As System.Data.IDbTransaction)
            mTransaccion = Value
        End Set
    End Property
#End Region

End Class

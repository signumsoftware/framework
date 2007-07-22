''' <summary>Esta interfaz proporciona la informacion minima sobre una transaccion y su conexion.</summary>
Interface IDatosTransaccionGenericoBDAD
    Inherits IEntidadesTransaccRecursoLN

#Region "Propiedades"
    ''' <summary>Obtiene o asigna la conexion.</summary>
    Property Conexion() As IDbConnection

    ''' <summary>Obtiene o asigna la transaccion.</summary>
    Property Transaccion() As IDbTransaction
#End Region

End Interface

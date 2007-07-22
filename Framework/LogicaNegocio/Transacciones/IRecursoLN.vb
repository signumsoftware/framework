Namespace Transacciones

    ''' <summary>Esta interface define la informacion minima que debe poseer un recurso.</summary>
    ''' <remarks>Un recurso contiene la informacion necesaria para poder acceder al almacen de datos.</remarks>
    Public Interface IRecursoLN

#Region "Propiedades"
        ''' <summary>Obtiene o asigna el id del recurso.</summary>
        Property ID() As String

        ''' <summary>Obtiene o asigna el nombre del recurso.</summary>
        Property Nombre() As String

        ''' <summary>Obtiene o asigna el tipo del recurso.</summary>
        Property Tipo() As String

        ''' <summary>Obtiene o asigna un dato del recurso.</summary>
        Property Dato(ByVal key As String) As Object
#End Region

    End Interface
End Namespace

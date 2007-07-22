''' <summary>Esta interface permite definir colecciones que emiten eventos al ser modificadas.</summary>
Public Interface IColEventos

#Region "Propiedades"
    ''' <summary>Indica si los eventos definidos por la interfaz se emiten o no.</summary>
    Property EventosActivos() As Boolean
    ReadOnly Property ModificadosElemtosCol() As Boolean
#End Region

#Region "Eventos"
    ''' <summary>Evento que indica que se ha añadido un elemento a la coleccion.</summary>
    Event ElementoAñadido(ByVal sender As Object, ByVal elemento As Object)

    ''' <summary>Evento que indica que se ha eliminado un elemento de la coleccion.</summary>
    Event ElementoEliminado(ByVal sender As Object, ByVal elemento As Object)

    ''' <summary>Evento que indica que se ha añadido un elemento a la coleccion.</summary>
    Event ElementoaAñadir(ByVal sender As Object, ByVal elemento As Object, ByRef permitir As Boolean)

    ''' <summary>Evento que indica que se ha eliminado un elemento de la coleccion.</summary>
    Event ElementoaEliminar(ByVal sender As Object, ByVal elemento As Object, ByRef permitir As Boolean)
#End Region

End Interface




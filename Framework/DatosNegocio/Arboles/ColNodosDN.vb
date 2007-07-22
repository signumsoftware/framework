Namespace Arboles

    <Serializable()> Public Class ColNodosDN
        Inherits ArrayListValidable
        Implements IColNodos

        Public Sub New()
            MyBase.New(New ValidadorTipos(GetType(INodoDN), True))
        End Sub
    End Class


    <Serializable()> Public Class ColNodosConHijosDN
        Inherits ArrayListValidable
        Implements IColNodos
        Public Sub New()
            MyBase.New(New ValidadorTipos(GetType(INodoConHijosDN), True))
        End Sub
    End Class

End Namespace


Public Interface IColNodos
    Inherits IList
    Inherits IValidable
    Inherits IColEventos
    Inherits IColDn

End Interface
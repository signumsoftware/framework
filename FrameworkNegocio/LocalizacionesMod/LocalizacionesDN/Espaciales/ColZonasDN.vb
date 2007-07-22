#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' Colección de zonas para el árbol
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public Class ColZonasDN
    Inherits ArrayListValidable(Of ZonaDN)

    'Métodos de la colección
    '

End Class


<Serializable()> _
Public Class ColZonasALVDN
    Inherits ArrayListValidable


    Public Sub New()
        MyBase.New(New ValidadorTipos(GetType(ZonaDN), True))
    End Sub

    'Métodos de la colección
    '

End Class

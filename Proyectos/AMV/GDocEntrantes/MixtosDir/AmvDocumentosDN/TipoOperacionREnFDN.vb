<Serializable()> _
Public Class TipoOperacionREnFDN
    Inherits Framework.DatosNegocio.EntidadTipoDN(Of TipoOperacionREnF)

    Public Sub New()
    End Sub

    Public Sub New(ByVal valor As TipoOperacionREnF)
        MyBase.New(valor)
    End Sub

    Protected Overrides Function CrearInstancia(ByVal valor As TipoOperacionREnF) As Object
        Return New TipoOperacionREnFDN(valor)
    End Function

End Class





'Public Class TipoOperacionREnF2
'    Inherits Framework.DatosNegocio.EntidadTipoDN(Of TipoOperacionREnF)

'    Public Sub New(ByVal valor As TipoOperacionREnF)
'        MyBase.New(valor)
'    End Sub

'End Class



Public Enum TipoOperacionREnF
    Crear = 1
    Anular = 2
    Incidentar = 3
    Rechazar = 4
    Clasificar = 5
    ClasificarYCerrar = 6
    FijarEstado = 7
End Enum
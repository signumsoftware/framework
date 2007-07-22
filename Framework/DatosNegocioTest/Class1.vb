
Imports Framework.DatosNegocio

Public Class telefono
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mPersona As Persona
    <RelacionPropCampoAtribute("mPersona")> Public Property Dueño() As Persona
        Get
            Return mPersona
        End Get
        Set(ByVal value As Persona)
            Me.CambiarValorRef(Of Persona)(value, mPersona)
        End Set
    End Property
End Class
Public Class Hijo
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mPersona As Persona
    <RelacionPropCampoAtribute("mPersona")> Public Property Padre() As Persona
        Get
            Return mPersona
        End Get
        Set(ByVal value As Persona)
            Me.CambiarValorRef(Of Persona)(value, mPersona)
        End Set
    End Property
End Class

Public Class colPersona
    Inherits Framework.DatosNegocio.ArrayListValidable(Of Persona)

End Class
Public Class Persona
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mcabeza As cabeza
    <RelacionPropCampoAtribute("mcabeza")> Public Property cabeza() As cabeza
        Get
            Return mcabeza
        End Get
        Set(ByVal value As cabeza)
            Me.CambiarValorRef(Of cabeza)(value, mcabeza)
        End Set
    End Property
End Class



<Serializable()> Public Class cabeza
    Inherits Framework.DatosNegocio.EntidadDN

End Class


Public Class Equipo
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mcolPersonas As colPersona
    <RelacionPropCampoAtribute("mcolPersonas")> Public Property colPersonas() As colPersona
        Get
            Return mcolPersonas
        End Get
        Set(ByVal value As colPersona)
            Me.CambiarValorRef(Of colPersona)(value, mcolPersonas)
        End Set
    End Property
End Class




Public Class ParticipanteTA
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IParticipante

    Protected mPersona As Persona

    Public Property NombreAlias() As String Implements IParticipante.NombreAlias
        Get
            Return Me.mpersona.Nombre
        End Get
        Set(ByVal value As String)
            Me.mpersona.Nombre = value
        End Set
    End Property

    <RelacionPropCampoAtribute("mPersona")> Public Property persona() As Persona
        Get
            Return Me.mPersona
        End Get
        Set(ByVal value As Persona)
            Me.CambiarValorRef(Of Persona)(value, Me.mPersona)
        End Set
    End Property

End Class

Public Interface IParticipante
    Property NombreAlias() As String

End Interface

Public Class Concurso
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mIParticipante As IParticipante


    <RelacionPropCampoAtribute("mIParticipante")> Property ParticipantePrincipal() As IParticipante
        Get
            Return Me.mIParticipante
        End Get
        Set(ByVal value As IParticipante)
            Me.CambiarValorRef(Of IParticipante)(value, mIParticipante)
        End Set
    End Property
End Class
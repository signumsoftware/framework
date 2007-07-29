



Imports Framework.DatosNegocio


<Serializable()> _
Public Class ImplicadoDN
    Inherits EntidadDN


#Region "Atributos"
    Protected mTiopoImplicacion As TipoImplicacionDN
    Protected mEntidad As IEntidadDN
    Protected mReclamacion As ReclamacionDN


#End Region


#Region "Constructores"

#End Region

#Region "Propiedades"





    <RelacionPropCampoAtribute("mReclamacion")> _
    Public Property Reclamacion() As ReclamacionDN
        Get
            Return mReclamacion
        End Get
        Set(ByVal value As ReclamacionDN)
            CambiarValorRef(Of ReclamacionDN)(value, mReclamacion)
        End Set
    End Property









    <RelacionPropCampoAtribute("mEntidad")> _
    Public Property Entidad() As IEntidadDN
        Get
            Return mEntidad
        End Get
        Set(ByVal value As IEntidadDN)
            CambiarValorRef(Of IEntidadDN)(value, mEntidad)
        End Set
    End Property






    <RelacionPropCampoAtribute("mTiopoImplicacion")> _
    Public Property TiopoImplicacion() As TipoImplicacionDN
        Get
            Return mTiopoImplicacion
        End Get
        Set(ByVal value As TipoImplicacionDN)
            CambiarValorRef(Of TipoImplicacionDN)(value, mTiopoImplicacion)
        End Set
    End Property






#End Region

#Region "Metodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mEntidad Is Nothing Then
            pMensaje = "la entidad referida no puede ser nothing"
            Return EstadoIntegridadDN.Inconsistente
        End If


        Return MyBase.EstadoIntegridad(pMensaje)
    End Function


#End Region



End Class




<Serializable()> _
Public Class ColImplicadoDN
    Inherits ArrayListValidable(Of ImplicadoDN)
End Class











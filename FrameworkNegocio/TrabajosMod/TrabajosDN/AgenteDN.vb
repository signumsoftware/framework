Imports Framework.DatosNegocio
Imports FN.Empresas.DN
Imports FN.Personas.DN

<Serializable()> _
Public Class AgenteDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mColServicios As ColServicioDN
    Protected mSede As SedeEmpresaDN
    Protected mPersona As PersonaDN

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        CambiarValorCol(Of ColServicioDN)(New ColServicioDN(), mColServicios)
    End Sub

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mColServicios")> _
    Public Property ColServicios() As ColServicioDN
        Get
            Return mColServicios
        End Get
        Set(ByVal value As ColServicioDN)
            CambiarValorCol(Of ColServicioDN)(value, mColServicios)
        End Set
    End Property

    <RelacionPropCampoAtribute("mSede")> _
    Public Property Sede() As SedeEmpresaDN
        Get
            Return mSede
        End Get
        Set(ByVal value As SedeEmpresaDN)
            CambiarValorRef(Of SedeEmpresaDN)(value, mSede)
        End Set
    End Property

    <RelacionPropCampoAtribute("mPersona")> _
    Public Property Persona() As PersonaDN
        Get
            Return mPersona
        End Get
        Set(ByVal value As PersonaDN)
            CambiarValorRef(Of PersonaDN)(value, mPersona)
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarPersona(ByRef mensaje As String, ByVal persona As PersonaDN) As Boolean
        If persona Is Nothing OrElse persona.NIF Is Nothing OrElse String.IsNullOrEmpty(persona.NIF.Codigo) Then
            mensaje = "Un agente debe tener a una persona"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String
        Dim cadena As String = ""

        If mPersona IsNot Nothing Then
            cadena = mPersona.ToString()
        End If

        If mSede IsNot Nothing AndAlso mSede.Empresa IsNot Nothing AndAlso mSede.Empresa.EntidadFiscal IsNot Nothing AndAlso mSede.Empresa.EntidadFiscal.IentidadFiscal.IdentificacionFiscal IsNot Nothing Then
            cadena = cadena & ", " & mSede.Empresa.ToString()
        End If

        Return cadena
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarPersona(pMensaje, mPersona) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        If (Persona.NIF Is Nothing OrElse String.IsNullOrEmpty(Persona.NIF.Codigo)) AndAlso (mSede Is Nothing OrElse mSede.Empresa Is Nothing) Then
            pMensaje = "El agente debe tener un NIF válido, o pertenecer a una empresa con CIF"
            Return EstadoIntegridadDN.Inconsistente
        End If

        Me.mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class

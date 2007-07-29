Imports Framework.DatosNegocio
Imports FN.Empresas.DN

<Serializable()> _
Public Class SiniestroDN
    Inherits EntidadTemporalDN

#Region "Atributos"
    ' fi fecha de apertura
    ' ff fecha de cierre
    Protected mFOcurrencia As Date
    Protected mPeridoCobertura As FN.Seguros.Polizas.DN.PeriodoCoberturaDN
    Protected mEstadoTramitSiniestro As EstadoTramitacionDN
    Protected mFComunicacion As Date


#End Region

#Region "Propiedades"

    ''' <summary>
    ''' la minica fecha de comunicación de las reclamaciones 
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property FComunicacion() As Date

        Get
            Return mFComunicacion
        End Get


    End Property

    Public Sub ActualizarEstadoTramitSiniestro()

    End Sub
    Public ReadOnly Property EstadoTramitSiniestro() As EstadoTramitacionDN
        Get

            Return mEstadoTramitSiniestro
        End Get
    End Property

    Public Property FOcurrencia() As Date

        Get
            Return mFOcurrencia
        End Get

        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFOcurrencia)

        End Set
    End Property






    <RelacionPropCampoAtribute("mPeridoCobertura")> _
    Public Property PeridoCobertura() As FN.Seguros.Polizas.DN.PeriodoCoberturaDN
        Get
            Return mPeridoCobertura
        End Get
        Set(ByVal value As FN.Seguros.Polizas.DN.PeriodoCoberturaDN)
            CambiarValorRef(Of FN.Seguros.Polizas.DN.PeriodoCoberturaDN)(value, mPeridoCobertura)
        End Set
    End Property











#End Region


End Class



<Serializable()> _
Public Class ColSiniestroDN
    Inherits ArrayListValidable(Of SiniestroDN)


    Public Function RecuperarDesdeFechaOcurrencia(ByVal pFechaOcurrencia As Date) As ColSiniestroDN

        RecuperarDesdeFechaOcurrencia = New ColSiniestroDN



        For Each sini As SiniestroDN In Me
            If sini.FOcurrencia >= pFechaOcurrencia Then
                RecuperarDesdeFechaOcurrencia.Add(sini)
            End If

        Next


    End Function

    Public Function RecuperarDesdeFechaOcurrencia(ByVal IntervaloFechas As Framework.DatosNegocio.Localizaciones.Temporales.IntervaloFechasDN) As ColSiniestroDN

        RecuperarDesdeFechaOcurrencia = New ColSiniestroDN



        For Each sini As SiniestroDN In Me
            If IntervaloFechas.Contiene(sini.FOcurrencia) Then
                RecuperarDesdeFechaOcurrencia.Add(sini)
            End If

        Next


    End Function
End Class




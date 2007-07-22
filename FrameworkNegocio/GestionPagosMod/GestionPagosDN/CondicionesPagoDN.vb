
Imports Framework.DatosNegocio
<Serializable()> Public Class CondicionesPagoDN
    Inherits Framework.DatosNegocio.EntidadDN


    ' Protected mCadencia As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias
    Protected mModalidadDePago As ModalidadPago
    Protected mCuentaBancria As FN.Financiero.DN.CuentaBancariaDN
    Protected mNumeroRecibos As Integer
    Protected mPlazoEjecucion As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias


    Public Sub New()
        ' CambiarValorRef(Of Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)(New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias, mCadencia)
        CambiarValorRef(Of Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)(New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias, mPlazoEjecucion)

        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

    Public Property ModalidadDePago() As ModalidadPago

        Get
            Return mModalidadDePago
        End Get

        Set(ByVal value As ModalidadPago)
            CambiarValorVal(Of ModalidadPago)(value, mModalidadDePago)

        End Set
    End Property

    Public Property NumeroRecibos() As Integer
        Get
            Return mNumeroRecibos
        End Get
        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mNumeroRecibos)
        End Set
    End Property


    ''' <summary>
    ''' este valor es usado para crear la "fecha de efecto esperada" junto con la feccha de emision 
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <RelacionPropCampoAtribute("mPlazoEjecucion")> _
    Public Property PlazoEjecucion() As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias

        Get
            Return mPlazoEjecucion
        End Get

        Set(ByVal value As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)
            CambiarValorRef(Of Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)(value, mPlazoEjecucion)

        End Set
    End Property





    <RelacionPropCampoAtribute("mCuentaBancria")> _
    Public Property CuentaBancria() As FN.Financiero.DN.CuentaBancariaDN

        Get
            Return mCuentaBancria
        End Get

        Set(ByVal value As FN.Financiero.DN.CuentaBancariaDN)
            CambiarValorRef(Of FN.Financiero.DN.CuentaBancariaDN)(value, mCuentaBancria)

        End Set
    End Property



    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If Me.mCuentaBancria Is Nothing AndAlso (Me.ModalidadDePago = ModalidadPago.Domiciliacion OrElse Me.ModalidadDePago = ModalidadPago.Tranferencia) Then
            pMensaje = " La modalidad de pago " & Me.ModalidadDePago & " requiere una cuenta bancaria"
            Return EstadoIntegridadDN.Inconsistente
        End If


        Return MyBase.EstadoIntegridad(pMensaje)
    End Function




End Class

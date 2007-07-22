Imports Framework.DatosNegocio
Imports FN.Personas.DN

<Serializable()> _
Public Class TomadorDN
    Inherits EntidadDN
    Implements ITomador

#Region "Campos"

    '  Protected mPersona As PersonaDN
    Protected mEsImpago As Boolean
    Protected mEntidadFiscalGenerica As FN.Localizaciones.DN.EntidadFiscalGenericaDN
    Protected mIdentificacionFiscal As String ' campo usado para impedir que haya dos tomadores para la mims aentidad fical, presupone que este valor es unico independientemente de la entidad fiscal

    Protected mNoRenovacion As Boolean

    Protected mVetado As Boolean

    Protected mValorBonificacion As Double

#End Region

#Region "Propiedades"

    Public Property Vetado() As Boolean

        Get
            Return mVetado
        End Get

        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mVetado)

        End Set
    End Property

    Public Property NoRenovacion() As Boolean

        Get
            Return mNoRenovacion
        End Get

        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mNoRenovacion)

        End Set
    End Property

    <RelacionPropCampoAtribute("mEntidadFiscalGenerica")> _
    Public Property EntidadFiscalGenerica() As FN.Localizaciones.DN.EntidadFiscalGenericaDN

        Get
            Return mEntidadFiscalGenerica
        End Get

        Set(ByVal value As FN.Localizaciones.DN.EntidadFiscalGenericaDN)
            CambiarValorRef(Of FN.Localizaciones.DN.EntidadFiscalGenericaDN)(value, mEntidadFiscalGenerica)

        End Set
    End Property

    Public Property EsImpago() As Boolean
        Get
            Return mEsImpago
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mEsImpago)
        End Set
    End Property

    Public Property ValorBonificacion() As Double Implements ITomador.ValorBonificacion
        Get
            Return mValorBonificacion
        End Get
        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mValorBonificacion)
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarPersona(ByRef mensaje As String, ByVal persona As PersonaDN) As Boolean
        If persona Is Nothing OrElse persona.NIF Is Nothing OrElse String.IsNullOrEmpty(persona.NIF.Codigo) Then
            mensaje = "Un tomador debe tener a una persona con un NIF válido"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If mEntidadFiscalGenerica Is Nothing OrElse mEntidadFiscalGenerica.IentidadFiscal Is Nothing OrElse _
                mEntidadFiscalGenerica.IentidadFiscal.IdentificacionFiscal Is Nothing OrElse _
                String.IsNullOrEmpty(mEntidadFiscalGenerica.IentidadFiscal.IdentificacionFiscal.Codigo) Then

            pMensaje = "Un tomador debe  disponer de entidad fiscal con un identificador válido"
            Return EstadoIntegridadDN.Inconsistente
        End If


        If Me.mValorBonificacion < 0.5 OrElse Me.mValorBonificacion > 3.5 Then
            pMensaje = "mValorBonificacion no esta entre los intervalos acceptables"
            Return EstadoIntegridadDN.Inconsistente
        End If


        mIdentificacionFiscal = mEntidadFiscalGenerica.IentidadFiscal.IdentificacionFiscal.Codigo

        Me.mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function


    Public Overrides Function ToString() As String
        If Not Me.mEntidadFiscalGenerica Is Nothing Then
            Me.mToSt = Me.mEntidadFiscalGenerica.ToString
        Else
            mToSt = "NO ASIGNADO"
        End If

        Return mToSt
    End Function

#End Region



    Public Property Direccion() As Localizaciones.DN.DireccionNoUnicaDN Implements ITomador.Direccion
        Get
            Return Me.mEntidadFiscalGenerica.IentidadFiscal.DomicilioFiscal
        End Get
        Set(ByVal value As Localizaciones.DN.DireccionNoUnicaDN)
            Me.mEntidadFiscalGenerica.IentidadFiscal.DomicilioFiscal = value
        End Set
    End Property



    Public Property ValorCifNif() As String Implements ITomador.ValorCifNif
        Get
            Return Me.mEntidadFiscalGenerica.IentidadFiscal.IdentificacionFiscal.Codigo
        End Get
        Set(ByVal value As String)
            Me.mEntidadFiscalGenerica.IentidadFiscal.IdentificacionFiscal.Codigo = value
        End Set
    End Property
End Class


<Serializable()> _
Public Class ColTomadorDN
    Inherits ArrayListValidable(Of TomadorDN)

End Class
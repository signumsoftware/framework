Imports Framework.DatosNegocio

<Serializable()> _
Public Class EmisoraPolizasDN

    Inherits Framework.DatosNegocio.EntidadDN


#Region "Atributos"

    Protected mEnidadFiscalGenerica As FN.Localizaciones.DN.EntidadFiscalGenericaDN

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mEnidadFiscalGenerica")> _
        Public Property EnidadFiscalGenerica() As FN.Localizaciones.DN.EntidadFiscalGenericaDN

        Get
            Return mEnidadFiscalGenerica
        End Get

        Set(ByVal value As FN.Localizaciones.DN.EntidadFiscalGenericaDN)
            CambiarValorRef(Of FN.Localizaciones.DN.EntidadFiscalGenericaDN)(value, mEnidadFiscalGenerica)

        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String

        If mEnidadFiscalGenerica IsNot Nothing Then
            Return mEnidadFiscalGenerica.ToString()
        End If

        Return String.Empty

    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class

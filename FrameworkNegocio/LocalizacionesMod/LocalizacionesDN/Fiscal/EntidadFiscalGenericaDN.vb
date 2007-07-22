Imports Framework.DatosNegocio
<Serializable()> _
Public Class EntidadFiscalGenericaDN
    Inherits Framework.DatosNegocio.EntidadDN


    Protected mIentidadFiscal As IEntidadFiscalDN
    Protected mValorCifNif As String ' cache el valor de un cif o un nif

    Public Property ValorCifNif() As String
        Get
            Return mIentidadFiscal.IdentificacionFiscal.Codigo
        End Get
        Set(ByVal value As String)
            mIentidadFiscal.IdentificacionFiscal.Codigo = value
        End Set
    End Property


    <RelacionPropCampoAtribute("mIentidadFiscal")> _
    Public Property IentidadFiscal() As IEntidadFiscalDN

        Get
            Return mIentidadFiscal
        End Get

        Set(ByVal value As IEntidadFiscalDN)
            CambiarValorRef(Of IEntidadFiscalDN)(value, mIentidadFiscal)
            If Not mIentidadFiscal.EntidadFiscalGenerica Is Me Then
                mIentidadFiscal.EntidadFiscalGenerica = Me
            End If
        End Set
    End Property


    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If mIentidadFiscal Is Nothing Then
            pMensaje = "una EntidadFiscalGenerica debe tener siempre su mIentidadFiscal"
            Return EstadoIntegridadDN.Inconsistente
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("una EntidadFiscalGenerica debe tener siempre su mIentidadFiscal")
        End If


        mValorCifNif = mIentidadFiscal.IdentificacionFiscal.Codigo


        If mIentidadFiscal.EntidadFiscalGenerica IsNot Me Then

            pMensaje = "mi  mIentidadFiscal debe de referir en EntidadFiscalGenerica a ME"
            Return EstadoIntegridadDN.Inconsistente

            Throw New Framework.DatosNegocio.ApplicationExceptionDN("mi  mIentidadFiscal debe de referir en EntidadFiscalGenerica a ME")

        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Overrides Function ToString() As String
        Me.mToSt = Me.mIentidadFiscal.ToString
        Return mToSt
    End Function

End Class





<Serializable()> _
Public Class ColEntidadFiscalGenericaDN
    Inherits ArrayListValidable(Of EntidadFiscalGenericaDN)

    Public Function RecuperarPorIdentificacionFiscal(ByVal identificacionFiscal As String) As EntidadFiscalGenericaDN

        For Each efg As EntidadFiscalGenericaDN In Me
            If efg.IentidadFiscal.IdentificacionFiscal.Codigo = identificacionFiscal Then
                Return efg
            End If
        Next

        Return Nothing

    End Function

End Class





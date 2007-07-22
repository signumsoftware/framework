Imports Framework.DatosNegocio
<Serializable()> _
Public Class TipoDocumentoRequeridoDN
    Inherits Framework.DatosNegocio.EntidadDN


    Protected mTipoDoc As TipoFicheroDN
    Protected mColEntidadesRequeridoras As Framework.DatosNegocio.ColHEDN
    Protected mPlazo As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias
    Protected mPrioridad As Double
    Protected mNecesario As Boolean



    Public Sub New()
        CambiarValorRef(Of Framework.DatosNegocio.ColHEDN)(New Framework.DatosNegocio.ColHEDN, mColEntidadesRequeridoras)
        CambiarValorRef(Of Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)(New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias, mPlazo)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub







    Public Property Prioridad() As Double

        Get
            Return mPrioridad
        End Get

        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mPrioridad)

        End Set
    End Property








    Public Property Necesario() As Boolean

        Get
            Return mNecesario
        End Get

        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mNecesario)

        End Set
    End Property








    <RelacionPropCampoAtribute("mTipoDoc")> _
    Public Property TipoDoc() As TipoFicheroDN

        Get
            Return mTipoDoc
        End Get

        Set(ByVal value As TipoFicheroDN)
            CambiarValorRef(Of TipoFicheroDN)(value, mTipoDoc)

        End Set
    End Property



    <RelacionPropCampoAtribute("mColEntidadesRequeridoras")> _
    Public Property ColEntidadesRequeridoras() As Framework.DatosNegocio.ColHEDN

        Get
            Return mColEntidadesRequeridoras
        End Get

        Set(ByVal value As Framework.DatosNegocio.ColHEDN)
            CambiarValorRef(Of Framework.DatosNegocio.ColHEDN)(value, mColEntidadesRequeridoras)

        End Set
    End Property










    <RelacionPropCampoAtribute("mPlazo")> _
    Public Property Plazo() As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias

        Get
            Return mPlazo
        End Get

        Set(ByVal value As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)
            CambiarValorRef(Of Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias)(value, mPlazo)

        End Set
    End Property







End Class





<Serializable()> _
Public Class ColTipoDocumentoRequeridoDN
    Inherits ArrayListValidable(Of TipoDocumentoRequeridoDN)




    Public Function RecuperarPorEntidadReferida(ByVal entidad As IEntidadBaseDN) As ColTipoDocumentoRequeridoDN
        RecuperarPorEntidadReferida = New ColTipoDocumentoRequeridoDN

        For Each tdr As TipoDocumentoRequeridoDN In Me
            If tdr.ColEntidadesRequeridoras.Contiene(entidad, CoincidenciaBusquedaEntidadDN.Todos) Then
                RecuperarPorEntidadReferida.Add(tdr)
            End If
        Next

    End Function


    Public Function RecuperarPorColEntidadReferida(ByVal colEntidad As IEnumerable) As ColTipoDocumentoRequeridoDN
        RecuperarPorColEntidadReferida = New ColTipoDocumentoRequeridoDN


        For Each edn As IEntidadBaseDN In colEntidad
            For Each tdr As TipoDocumentoRequeridoDN In Me
                If tdr.ColEntidadesRequeridoras.Contiene(edn, CoincidenciaBusquedaEntidadDN.Todos) Then
                    RecuperarPorColEntidadReferida.Add(tdr)
                End If
            Next
        Next



    End Function
End Class





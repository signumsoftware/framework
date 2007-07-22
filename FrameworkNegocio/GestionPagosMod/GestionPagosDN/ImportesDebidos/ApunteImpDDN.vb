Imports Framework.DatosNegocio
''' <summary>
''' 
''' acreedora y deudora no pueden ser la misma entidad
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public Class ApunteImpDDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IImporteDebidoDN






#Region "Atributos"

    Protected mAcreedora As FN.Localizaciones.DN.EntidadFiscalGenericaDN
    Protected mDeudora As FN.Localizaciones.DN.EntidadFiscalGenericaDN
    Protected mFCreación As Date
    Protected mFEfecto As Date
    Protected mFAnulacion As Date
    Protected mImporte As Double
    Protected mHuellaIOrigenImpDebDN As HuellaIOrigenImpDebDN
    ' Protected mHuellaIOrigenImpDebDN As Framework.DatosNegocio.HEDN
    Protected mGUIDAgrupacion As String

#End Region


    Public Sub New()
        Me.modificarEstado = Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
    End Sub


    Public Sub New(ByVal pOrigen As IOrigenIImporteDebidoDN)
        Me.HuellaIOrigenImpDebDN = New HuellaIOrigenImpDebDN(pOrigen)
        'Me.HuellaIOrigenImpDebDN = New Framework.DatosNegocio.HEDN(pOrigen)
    End Sub

    ' TODO: alex
    ' permitir estados de integridad inconsistentes para algunoas dns
    ' podemos poner un boleano que permita guarar o no dn en estado incosistente.



    Public Property Acreedora() As FN.Localizaciones.DN.EntidadFiscalGenericaDN Implements IImporteDebidoDN.Acreedora
        Get
            Return Me.mAcreedora
        End Get
        Set(ByVal value As FN.Localizaciones.DN.EntidadFiscalGenericaDN)


            Dim mensaje As String = ""

            If ValDeudora(mensaje, value) Then
                mensaje = " Acreedora y deudora representan la misma entidad, " & mensaje
                Throw New Framework.DatosNegocio.ApplicationExceptionDN(mensaje)
            Else
                Me.CambiarValorRef(Of FN.Localizaciones.DN.EntidadFiscalGenericaDN)(value, mAcreedora)
            End If

        End Set
    End Property

    Public Property Deudora() As FN.Localizaciones.DN.EntidadFiscalGenericaDN Implements IImporteDebidoDN.Deudora
        Get
            Return Me.mDeudora
        End Get
        Set(ByVal value As FN.Localizaciones.DN.EntidadFiscalGenericaDN)
            Dim mensaje As String = ""

            If ValDeudora(mensaje, value) Then
                mensaje = " Acreedora y deudora representan la misma entidad, " & mensaje
                Throw New Framework.DatosNegocio.ApplicationExceptionDN(mensaje)
            Else
                Me.CambiarValorRef(Of FN.Localizaciones.DN.EntidadFiscalGenericaDN)(value, mDeudora)
            End If

        End Set
    End Property



    Public Property FCreación() As Date Implements IImporteDebidoDN.FCreación
        Get
            Return Me.mFCreación
        End Get
        Set(ByVal value As Date)
            Me.CambiarValorVal(Of Date)(value, mFCreación)
        End Set
    End Property

    Public Property FEfecto() As Date Implements IImporteDebidoDN.FEfecto
        Get
            Return Me.mFEfecto
        End Get
        Set(ByVal value As Date)
            Me.CambiarValorVal(Of Date)(value, mFEfecto)

        End Set
    End Property

    Public Property Importe() As Double Implements IImporteDebidoDN.Importe
        Get
            Return mImporte
        End Get
        Set(ByVal value As Double)
            Me.CambiarValorVal(Of Double)(value, mImporte)
        End Set
    End Property



#Region "Validaciones"

    Public Function ValDeudora(ByRef mensaje As String, ByVal entidad As FN.Localizaciones.DN.EntidadFiscalGenericaDN) As Boolean
        Dim mismaref As Boolean
        Return Framework.DatosNegocio.EntidadBaseDN.RepresentanMismaEntidadBidireccional(Me.mAcreedora, entidad, mensaje, mismaref)
    End Function

    Public Function ValAcreedora(ByRef mensaje As String, ByVal entidad As FN.Localizaciones.DN.IEntidadFiscalDN) As Boolean
        Dim mismaref As Boolean
        Return Framework.DatosNegocio.EntidadBaseDN.RepresentanMismaEntidadBidireccional(Me.mDeudora, entidad, mensaje, mismaref)
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If mHuellaIOrigenImpDebDN Is Nothing Then
            pMensaje = "mHuellaIOrigenImpDebDN no puede ser nulo"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If




        ' el importe debido no puede ser anulado si su origen no va a ser tambien anulado
        If Me.mFAnulacion <> Me.mHuellaIOrigenImpDebDN.FAnulacion Then
            pMensaje = "las fechas de anulación entre un importe debido y su origen deben ser identicas"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente

        End If


        If Not Me.mFAnulacion = Date.MinValue AndAlso Not String.IsNullOrEmpty(Me.mGUIDAgrupacion) Then
            pMensaje = "un importe debido no puede anularse si pertenece a una agrupacion Id" & Me.mID & " guidAgrupacion " & Me.mGUIDAgrupacion
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente

        End If






        Return MyBase.EstadoIntegridad(pMensaje)
    End Function






#End Region




    Public Property HuellaIOrigenImpDebDN() As HuellaIOrigenImpDebDN Implements IImporteDebidoDN.HuellaIOrigenImpDebDN
        Get
            Return mHuellaIOrigenImpDebDN
        End Get
        Set(ByVal value As HuellaIOrigenImpDebDN)
            Me.CambiarValorRef(Of HuellaIOrigenImpDebDN)(value, mHuellaIOrigenImpDebDN)
        End Set
    End Property

    Public Property FAnulacion() As Date Implements IImporteDebidoDN.FAnulacion
        Get
            Return mFAnulacion
        End Get
        Set(ByVal value As Date)
            Me.CambiarValorVal(Of Date)(value, mFAnulacion)
        End Set
    End Property

    Public Function CrearImpDebCompesatorio(ByVal origen As IOrigenIImporteDebidoDN) As IImporteDebidoDN Implements IImporteDebidoDN.CrearImpDebCompesatorio


        Dim impdeb As New ApunteImpDDN(origen)
        impdeb.Importe = Me.Importe
        impdeb.Deudora = Me.Acreedora
        impdeb.Acreedora = Me.Deudora

        impdeb.FCreación = Now
        Return impdeb
    End Function


    Public Property GUIDAgrupacion() As String Implements IImporteDebidoDN.GUIDAgrupacion
        Get
            Return mGUIDAgrupacion
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mGUIDAgrupacion)
        End Set
    End Property

    Public Function AcrredoyDeudorIguales(ByVal pApunteImpDDN As ApunteImpDDN) As Boolean

        If Me.Acreedora.GUID = pApunteImpDDN.Acreedora.GUID AndAlso Me.Deudora.GUID = pApunteImpDDN.Deudora.GUID Then
            Return True
        Else
            Return False
        End If

    End Function

    Public Function AcrredoyDeudorCompatibles(ByVal pApunteImpDDN As ApunteImpDDN) As Boolean

        If (Me.Acreedora.GUID = pApunteImpDDN.Acreedora.GUID OrElse Me.Acreedora.GUID = pApunteImpDDN.Deudora.GUID) AndAlso (Me.Deudora.GUID = pApunteImpDDN.Deudora.GUID OrElse Me.Deudora.GUID = pApunteImpDDN.Acreedora.GUID) Then
            Return True
        Else
            Return False
        End If

    End Function


    ''' <summary>
    ''' no puede estar previamente anulado ni pertenecer a una agrupacion
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function Anulable(ByRef pMensaje As String) As Boolean Implements IImporteDebidoDN.Anulable

        If Me.mFAnulacion = Date.MinValue Then
            If String.IsNullOrEmpty(Me.mGUIDAgrupacion) Then
                Return True
            Else
                pMensaje = "no es dado que forma parte de la agrupacion guidAgrupacion:" & mGUIDAgrupacion
                Return False
            End If
        Else
            pMensaje = "no es naulable dado que ya esta anulado"
            Return False
        End If


    End Function

End Class






<Serializable()> _
Public Class ColApunteImpDDN
    Inherits ArrayListValidable(Of ApunteImpDDN)




    Public Function SaldarCol() As ApunteImpDDN



        If Me.Count = 0 Then
            Return Nothing
        End If

        Dim DeudoraEF, AcreedoraEF As FN.Localizaciones.DN.IEntidadFiscalDN



        DeudoraEF = Me.Item(0).Deudora.IentidadFiscal
        AcreedoraEF = Me.Item(0).Acreedora.IentidadFiscal


        Dim importe As Double

        For Each ie As IImporteDebidoDN In Me

            If Not (ie.Deudora.GUID = DeudoraEF.GUID OrElse ie.Deudora.GUID = AcreedoraEF.GUID) AndAlso (ie.Acreedora.GUID = DeudoraEF.GUID OrElse ie.Acreedora.GUID = AcreedoraEF.GUID) Then
                Throw New ApplicationException("Exiten más de dos entidades fiscales en la coleccion luego no es saldable")
            End If

            ' el importe es respecto de lo que la deudora le debe a la acrredora
            If ie.Deudora.GUID = DeudoraEF.GUID Then

                importe += ie.Importe

            Else
                importe -= ie.Importe

            End If


        Next

        Dim resultado As New ApunteImpDDN

        If importe >= 0 Then
            resultado.Deudora = DeudoraEF.EntidadFiscalGenerica
            resultado.Acreedora = AcreedoraEF.EntidadFiscalGenerica
            resultado.Importe = importe
        Else

            resultado.Deudora = AcreedoraEF.EntidadFiscalGenerica
            resultado.Acreedora = DeudoraEF.EntidadFiscalGenerica
            resultado.Importe = -importe

        End If

        Return resultado


    End Function




    Public Function SoloDosEntidadesFiscales() As Boolean


        Dim ief1, ief2 As FN.Localizaciones.DN.IEntidadFiscalDN
        ief1 = Me.Item(0).Deudora
        ief2 = Me.Item(0).Acreedora


        For Each ie As IImporteDebidoDN In Me

            If Not ((ie.Deudora.GUID = ief1.GUID OrElse ie.Deudora.GUID = ief2.GUID) AndAlso (ie.Acreedora.GUID = ief1.GUID OrElse ie.Acreedora.GUID = ief2.GUID)) Then
                Return False
            End If

        Next

        Return True

    End Function


    ''' <summary>
    ''' genera una nueva coleccion donde todos los elementos refierer a las dos entidades fiscales indistientamente como deudora y acreedora
    ''' </summary>
    ''' <param name="ief1"></param>
    ''' <param name="ief2"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function SeleccioanrSoloDosEntidadesFiscales(ByVal ief1 As FN.Localizaciones.DN.EntidadFiscalGenericaDN, ByVal ief2 As FN.Localizaciones.DN.EntidadFiscalGenericaDN) As ColApunteImpDDN

        Dim col As New ColApunteImpDDN


        For Each ie As IImporteDebidoDN In Me

            If (ie.Deudora.GUID = ief1.GUID OrElse ie.Deudora.GUID = ief2.GUID) AndAlso (ie.Acreedora.GUID = ief1.GUID OrElse ie.Acreedora.GUID = ief2.GUID) Then
                col.Add(ie)
            End If

        Next

        Return col

    End Function





End Class





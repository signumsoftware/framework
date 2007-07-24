
Imports Framework.DatosNegocio
Imports FN.Seguros.Polizas.DN
<Serializable()> _
Public Class PeriodoRenovacionPolizaOidDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements FN.GestionPagos.DN.IOrigenIImporteDebidoDN



    Protected mPeriodoCobertura As PeriodoCoberturaDN
    Protected mColOrigenes As Framework.DatosNegocio.ColHEDN
    Protected mFAnulacion As Date
    Protected mDatosPolizaVehiculos As DatosTarifaVehiculosDN
    Protected mPeriodoRenovacionPoliza As PeriodoRenovacionPolizaDN
    Protected mPRPolizaOidPrevio As PeriodoRenovacionPolizaOidDN
    Protected mPCAnualOidPrevioColPagoDN As FN.GestionPagos.DN.ColPagoDN
    Protected mIImporteDebido As GestionPagos.DN.IImporteDebidoDN
    Protected mOrigenImporteDebido As FN.GestionPagos.DN.OrigenIdevBaseDN
    Protected mPoliza As FN.Seguros.Polizas.DN.PolizaDN


    Public Sub New()
        CambiarValorRef(Of FN.GestionPagos.DN.IOrigenIImporteDebidoDN)(New FN.GestionPagos.DN.OrigenIdevBaseDN, mOrigenImporteDebido)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub


    '<RelacionPropCampoAtribute("mOrigenImporteDebido")> _
    'Public Property OrigenImporteDebido() As FN.GestionPagos.DN.OrigenIdevBaseDN

    '    Get
    '        Return mOrigenImporteDebido
    '    End Get

    '    Set(ByVal value As FN.GestionPagos.DN.OrigenIdevBaseDN)
    '        CambiarValorRef(Of FN.GestionPagos.DN.OrigenIdevBaseDN)(value, mOrigenImporteDebido)
    '        mOrigenImporteDebido.ColHEDN.AddUnicoHuellaPara(mOrigenImporteDebido)
    '    End Set
    'End Property







    <RelacionPropCampoAtribute("mPoliza")> _
    Public Property Poliza() As FN.Seguros.Polizas.DN.PolizaDN

        Get
            Return mPoliza
        End Get

        Set(ByVal value As FN.Seguros.Polizas.DN.PolizaDN)
            CambiarValorRef(Of FN.Seguros.Polizas.DN.PolizaDN)(value, mPoliza)
            mOrigenImporteDebido.ColHEDN.AddUnicoHuellaPara(Poliza)

        End Set
    End Property







    <RelacionPropCampoAtribute("mPRPolizaOidPrevio")> _
    Public Property PRPolizaOidPrevio() As PeriodoRenovacionPolizaOidDN

        Get
            Return mPRPolizaOidPrevio
        End Get

        Set(ByVal value As PeriodoRenovacionPolizaOidDN)
            CambiarValorRef(Of PeriodoRenovacionPolizaOidDN)(value, mPRPolizaOidPrevio)
            mOrigenImporteDebido.ColHEDN.AddUnicoHuellaPara(mPRPolizaOidPrevio)

        End Set
    End Property





    <RelacionPropCampoAtribute("mPeriodoRenovacionPoliza")> _
    Public Property PeriodoRenovacionPoliza() As PeriodoRenovacionPolizaDN

        Get
            Return mPeriodoRenovacionPoliza
        End Get

        Set(ByVal value As PeriodoRenovacionPolizaDN)
            CambiarValorRef(Of PeriodoRenovacionPolizaDN)(value, mPeriodoRenovacionPoliza)
            If Not mOrigenImporteDebido Is Nothing Then
                mOrigenImporteDebido.ColHEDN.AddUnicoHuellaPara(mPeriodoRenovacionPoliza)
                Me.Poliza = mPeriodoRenovacionPoliza.Poliza
            End If

        End Set
    End Property








    <RelacionPropCampoAtribute("mDatosPolizaVehiculos")> _
    Public Property DatosPolizaVehiculos() As DatosTarifaVehiculosDN

        Get
            Return mDatosPolizaVehiculos
        End Get

        Set(ByVal value As DatosTarifaVehiculosDN)
            CambiarValorRef(Of DatosTarifaVehiculosDN)(value, mDatosPolizaVehiculos)
            mOrigenImporteDebido.ColHEDN.AddUnicoHuellaPara(mDatosPolizaVehiculos)

        End Set
    End Property










    <RelacionPropCampoAtribute("mPeriodoCobertura")> _
    Public Property PeriodoCobertura() As PeriodoCoberturaDN

        Get
            Return mPeriodoCobertura
        End Get

        Set(ByVal value As PeriodoCoberturaDN)
            CambiarValorRef(Of PeriodoCoberturaDN)(value, mPeriodoCobertura)
            mOrigenImporteDebido.ColHEDN.AddUnicoHuellaPara(mPeriodoCobertura)
            If value Is Nothing Then
                Me.DatosPolizaVehiculos = Nothing
            Else
                Me.DatosPolizaVehiculos = value.Tarifa.DatosTarifa
            End If

        End Set
    End Property



    Public Property ColHEDN() As Framework.DatosNegocio.ColHEDN Implements GestionPagos.DN.IOrigenIImporteDebidoDN.ColHEDN
        Get
            Return mOrigenImporteDebido.ColHEDN
        End Get
        Set(ByVal value As Framework.DatosNegocio.ColHEDN)
            mOrigenImporteDebido.ColHEDN = value

        End Set
    End Property

    Public Property FAnulacion() As Date Implements GestionPagos.DN.IOrigenIImporteDebidoDN.FAnulacion
        Get
            Return mFAnulacion
        End Get
        Set(ByVal value As Date)
            Me.CambiarValorVal(Of Date)(value, mFAnulacion)
        End Set
    End Property

    Public Property IImporteDebidoDN() As GestionPagos.DN.IImporteDebidoDN Implements GestionPagos.DN.IOrigenIImporteDebidoDN.IImporteDebidoDN
        Get
            Return Me.mOrigenImporteDebido.IImporteDebidoDN
        End Get
        Set(ByVal value As GestionPagos.DN.IImporteDebidoDN)
            If Not mOrigenImporteDebido Is Nothing Then
                Me.mOrigenImporteDebido.IImporteDebidoDN = value
            End If

            CambiarValorRef(Of GestionPagos.DN.IImporteDebidoDN)(value, Me.mIImporteDebido)

        End Set
    End Property



    'Private Function ValColIEntidadDN(ByVal pMensaje As String, ByVal pColIEntidadDN As System.Collections.Generic.IList(Of Framework.DatosNegocio.IEntidadDN)) As Boolean


    '    Return True




    'End Function


    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN



        ' hay que sincronizar la coleccion con las posibles entidades origen


        If Me.mDatosPolizaVehiculos Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("los DatosPolizaVehiculos nop pueden ser nulos")
        End If

        If Not Me.mIImporteDebido Is Me.mOrigenImporteDebido.IImporteDebidoDN Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("los importes debidos no estan sicronizados")
        End If

        ' todo provisional QUITARRRRR
        If Me.mFAnulacion <> Me.mOrigenImporteDebido.FAnulacion OrElse Me.mFAnulacion <> Me.mOrigenImporteDebido.IImporteDebidoDN.FAnulacion Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("las fechas de anulacion estan desincronizadas")
        End If

        If Me.mPoliza IsNot Me.mPeriodoRenovacionPoliza.Poliza Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("la poliza referida no corresponde a la poliza referida por el perido de renovacion")
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function




    Public Function Anulable(ByRef pMensaje As String) As Boolean Implements GestionPagos.DN.IOrigenIImporteDebidoDN.Anulable

        If Me.mFAnulacion <> Date.MinValue Then
            pMensaje = "el origen de importe debido ya esta anulado"
            Return False
        End If

        Return Me.mOrigenImporteDebido.Anulable(pMensaje)
    End Function

    Public Function Anular(ByVal fAnulacion As Date) As Object Implements GestionPagos.DN.IOrigenIImporteDebidoDN.Anular
        Dim mensaje As String


        If Not Anulable(mensaje) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN(mensaje)
        End If


        Me.FAnulacion = fAnulacion
        mOrigenImporteDebido.Anular(fAnulacion)
        Me.mIImporteDebido.FAnulacion = fAnulacion
        Return mIImporteDebido
    End Function
End Class





<Serializable()> _
Public Class ColPeriodoRenovacionPolizaOidDN
    Inherits ArrayListValidable(Of PeriodoRenovacionPolizaOidDN)


    Public Function RecuperarNoAnulado() As PeriodoRenovacionPolizaOidDN


        For Each prp As PeriodoRenovacionPolizaOidDN In Me
            If prp.FAnulacion = Date.MinValue Then
                Return prp
            End If
        Next

        Return Nothing
    End Function

    Public Function RecuperarActivos() As ColPeriodoRenovacionPolizaOidDN

        Dim col As New ColPeriodoRenovacionPolizaOidDN

        For Each prp As PeriodoRenovacionPolizaOidDN In Me
            If prp.FAnulacion = Date.MinValue Then
                col.Add(prp)
            End If
        Next

        Return col
    End Function
End Class





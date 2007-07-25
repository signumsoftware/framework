Imports Framework.DatosNegocio

<Serializable()> _
Public Class BonificacionRVSVDN
    Inherits EntidadDN
    Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN



#Region "Atributos"

    Protected mIRecSumiValorLN As Framework.Operaciones.OperacionesDN.IRecSumiValorLN ' este no debe guardarse en base de datos
    Protected mColBonificacionRV As ColBonificacionRVDN
    Protected mBonificacion As BonificacionDN
    Protected mOperadoraplicable As String
    Protected mValorCacheado As BonificacionRVDN ' este valor no debe guardarse en base de datos

#End Region

#Region "Constructores"

    Public Sub New()
        CambiarValorRef(Of ColBonificacionRVDN)(New ColBonificacionRVDN, mColBonificacionRV)
    End Sub

#End Region

#Region "Propiedades"

    Public Property Operadoraplicable() As String
        Get
            Return mOperadoraplicable
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mOperadoraplicable)
        End Set
    End Property

    <RelacionPropCampoAtribute("mBonificacion")> _
       Public Property Bonificacion() As BonificacionDN
        Get
            Return mBonificacion
        End Get
        Set(ByVal value As BonificacionDN)
            CambiarValorRef(Of BonificacionDN)(value, mBonificacion)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColBonificacionRV")> _
    Public Property ColBonificacionRV() As ColBonificacionRVDN
        Get
            Return mColBonificacionRV
        End Get
        Set(ByVal value As ColBonificacionRVDN)
            CambiarValorRef(Of ColBonificacionRVDN)(value, mColBonificacionRV)
        End Set
    End Property

#End Region

#Region "Propiedades ISuministradorValorDN"

    Public Property IRecSumiValorLN() As Framework.Operaciones.OperacionesDN.IRecSumiValorLN Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.IRecSumiValorLN
        Get
            Return mIRecSumiValorLN
        End Get
        Set(ByVal value As Framework.Operaciones.OperacionesDN.IRecSumiValorLN)
            mIRecSumiValorLN = value
        End Set
    End Property

    Public ReadOnly Property ValorCacheado() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.ValorCacheado
        Get
            Return mValorCacheado
        End Get
    End Property
    Public Function RecuperarModelodatos() As ModeloDatosDN
        ' si un modelo cambia de categoria y la nueva categoria perjudica a un cliente en el momento de su renovacion
        ' el cleinte tine que conservar la categoria que tubiera antes de la renovacion


        For Each o As Object In mIRecSumiValorLN.DataSoucers
            If TypeOf o Is ModeloDatosDN Then
                Return o
            End If

        Next
        Return Nothing

    End Function
#End Region

#Region "Métodos ISuministradorValorDN"

    Public Function GetValor() As Object Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.GetValor
        Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN = RecupearTarifa()
        If tarifa Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("tarifa no puede ser nothing para PrimabaseRVSVDN")
        End If

        Dim miRiesgoMotor As FN.RiesgosVehiculos.DN.RiesgoMotorDN = Nothing
        If TypeOf tarifa.Riesgo Is FN.RiesgosVehiculos.DN.RiesgoMotorDN Then
            miRiesgoMotor = tarifa.Riesgo
        End If

        If miRiesgoMotor Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("el riesgo debiera ser un riesgomotor")
        End If

        Dim miModeloDatos As ModeloDatosDN = RecuperarModelodatos()
        If miModeloDatos Is Nothing Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("el ModeloDatos debiera ser un riesgomotor")
        End If




        Dim valorIntBonificacion As Double = RecuperarValorBonificacion()

        Dim colBonRV As New ColBonificacionRVDN()
        ' colBonRV.AddRangeObject(mColBonificacionRV.RecuperarBonificaciones(mBonificacion, valorIntBonificacion, miRiesgoMotor.Modelo, miRiesgoMotor.Matriculado, tarifa.FEfecto))
        colBonRV.AddRangeObject(mColBonificacionRV.RecuperarBonificaciones(mBonificacion, valorIntBonificacion, miModeloDatos, tarifa.FEfecto))

        Dim bonif As BonificacionRVDN
        Select Case colBonRV.Count
            Case Is = 0
                Throw New ApplicationException("Se debia haber recuperado al menos una bonificacion")

            Case Is = 1
                bonif = colBonRV.Item(0)

            Case Else
                Throw New ApplicationException("Se debia haber recuperado SOLO una bonificación activa")

        End Select

        mValorCacheado = bonif

        Return bonif.Valor
    End Function

    Public Function RecuperarOrden() As Integer Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.RecuperarOrden
        Throw New NotImplementedException("Recuperar orden no está implementado para esta clase")
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mBonificacion Is Nothing Then
            pMensaje = "mBonificacion no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        For Each bonRV As BonificacionRVDN In mColBonificacionRV
            If bonRV.Bonificacion.GUID <> mBonificacion.GUID Then
                pMensaje = "alguno de las BonificacionRVDN dispone de un BonificacionDN distinta a la de BonificacionRVSVDN"
                Return EstadoIntegridadDN.Inconsistente
            End If

        Next

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Private Function RecupearTarifa() As FN.Seguros.Polizas.DN.TarifaDN
        For Each o As Object In mIRecSumiValorLN.DataSoucers
            If TypeOf o Is FN.Seguros.Polizas.DN.TarifaDN Then
                Return o
            End If

        Next
        Return Nothing
    End Function

    Private Function RecuperarValorBonificacion() As Double
        For Each o As Object In mIRecSumiValorLN.DataSoucers
            If TypeOf o Is FN.Seguros.Polizas.DN.TarifaDN Then
                Dim oTar As FN.Seguros.Polizas.DN.TarifaDN = o
                Return oTar.DatosTarifa.ValorBonificacion
            End If
        Next

        Return 1

    End Function

#End Region

    Public Sub Limpiar() Implements Framework.Operaciones.OperacionesDN.ISuministradorValorDN.Limpiar
        mIRecSumiValorLN = Nothing
        ' mColBonificacionRV As ColBonificacionRVDN
        ' mBonificacion As BonificacionDN
        ' mOperadoraplicable As String
        ' mValorCacheado As BonificacionRVDN ' este valor no debe guardarse en base de datos

    End Sub
End Class


<Serializable()> _
Public Class ColBonificacionRVSVDN
    Inherits ArrayListValidable(Of BonificacionRVSVDN)

#Region "Métodos"

    Public Function RecuperarColBonificacionRV() As ColBonificacionRVDN
        Dim col As New ColBonificacionRVDN

        For Each bonif As BonificacionRVSVDN In Me
            col.AddRange(bonif.ColBonificacionRV)
        Next

        Return col
    End Function

#End Region

End Class
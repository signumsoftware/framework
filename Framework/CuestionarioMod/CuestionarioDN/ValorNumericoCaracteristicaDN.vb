<Serializable()> _
Public Class ValorNumericoCaracteristicaDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IValorCaracteristicaDN

    Protected mValorNumerico As Double
    Protected mCaracteristica As CaracteristicaDN
    Protected mIVCPadre As IValorCaracteristicaDN
    Protected mFechaEfectoValor As Date


    Public Property ValorNumerico() As Double
        Get
            Return Me.mValorNumerico
        End Get
        Set(ByVal value As Double)
            Me.CambiarValorVal(Of Double)(value, Me.mValorNumerico)
        End Set
    End Property

    <Framework.DatosNegocio.RelacionPropCampoAtribute("mCaracteristica")> Public Property Caracteristica() As CaracteristicaDN Implements IValorCaracteristicaDN.Caracteristica
        Get
            Return Me.mCaracteristica
        End Get
        Set(ByVal value As CaracteristicaDN)
            Me.CambiarValorRef(Of CaracteristicaDN)(value, Me.mCaracteristica)
        End Set
    End Property

    Public Property Valor() As Object Implements IValorCaracteristicaDN.Valor
        Get
            Return ValorNumerico
        End Get
        Set(ByVal value As Object)
            ValorNumerico = value
        End Set
    End Property

    Public Property ValorCaracPadre() As IValorCaracteristicaDN Implements IValorCaracteristicaDN.ValorCaracPadre
        Get
            Return mIVCPadre
        End Get
        Set(ByVal value As IValorCaracteristicaDN)
            Me.CambiarValorRef(Of IValorCaracteristicaDN)(value, mIVCPadre)
        End Set
    End Property

    Public Property FechaEfectoValor() As Date Implements IValorCaracteristicaDN.FechaEfectoValor
        Get
            Return mFechaEfectoValor
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaEfectoValor)
        End Set
    End Property

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN

        If Not Me.mCaracteristica.Padre Is Nothing AndAlso Me.mIVCPadre Is Nothing Then
            pMensaje = "dado que la cracteristica es subordinada , mVNCPadre no puede ser nothing"
            Return DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Function ClonarIValorCaracteristica() As IValorCaracteristicaDN Implements IValorCaracteristicaDN.ClonarIValorCaracteristica
        Dim valorClon As ValorNumericoCaracteristicaDN
        valorClon = Me.CloneSuperficialSinIdentidad()
        valorClon.FechaEfectoValor = Date.MinValue

        If Me.mIVCPadre IsNot Nothing Then
            valorClon.mIVCPadre = Me.mIVCPadre.ClonarIValorCaracteristica()
        End If

        Return valorClon
    End Function
End Class

<Serializable()> _
Public Class ColValorNumericoCaracteristicaDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of ValorNumericoCaracteristicaDN)


    Public Function ContineValor(ByVal valor As Double) As Boolean

        For Each vn As ValorNumericoCaracteristicaDN In Me

            If vn.ValorNumerico = valor Then
                Return True
            End If
        Next
        Return False

    End Function

    Public Function ContineValor(ByVal valor As Cuestionario.CuestionarioDN.IValorCaracteristicaDN) As Boolean

        For Each vn As ValorNumericoCaracteristicaDN In Me

            If vn.ValorNumerico = valor.Valor Then
                Return True
            End If
        Next
        Return False

    End Function


    Public Function recuperarIntervalo() As Framework.DatosNegocio.IntvaloNumericoDN


        Dim max, min As Double
        If Me.Count > 0 Then
            max = Me.Item(0).ValorNumerico
            min = Me.Item(0).ValorNumerico
        End If


        For Each vn As ValorNumericoCaracteristicaDN In Me

            If vn.ValorNumerico < min Then
                min = vn.ValorNumerico

            End If


            If vn.ValorNumerico > max Then
                max = vn.ValorNumerico

            End If
        Next


        If Me.Count > 0 Then
            recuperarIntervalo = New Framework.DatosNegocio.IntvaloNumericoDN

            recuperarIntervalo.ValInf = min
            recuperarIntervalo.ValSup = max
        Else
            Return Nothing
        End If

    End Function




    Public Function TodosValoresDe(ByVal pCaracteristica As CaracteristicaDN) As Boolean


        For Each valor As ValorNumericoCaracteristicaDN In Me
            If valor.Caracteristica.GUID <> pCaracteristica.GUID Then
                Return False
            End If
        Next

        Return True


    End Function

End Class
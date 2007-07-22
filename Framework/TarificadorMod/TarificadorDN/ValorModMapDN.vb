Imports Framework.Cuestionario.CuestionarioDN




Public Class ValorModMapDN
    Inherits Framework.DatosNegocio.EntidadDN
    '  Implements IValorCaracteristicaDN
    Implements IValorMap


    Protected mColValorNumericoCaracteristicaDN As ColValorNumericoCaracteristicaDN
    Protected mCaracteristica As CaracteristicaDN
    Protected mValor As Double
    Protected mIVCPadre As IValorCaracteristicaDN

    Public Sub New()
        MyBase.New()
        Me.CambiarValorRef(Of ColValorNumericoCaracteristicaDN)(New ColValorNumericoCaracteristicaDN, mColValorNumericoCaracteristicaDN)

    End Sub

    Property ColValorNumericoCaracteristicaDN() As ColValorNumericoCaracteristicaDN
        Get
            Return Me.mColValorNumericoCaracteristicaDN
        End Get
        Set(ByVal value As ColValorNumericoCaracteristicaDN)
            Me.CambiarValorRef(Of ColValorNumericoCaracteristicaDN)(value, mColValorNumericoCaracteristicaDN)
        End Set
    End Property




    Public Property Valor() As Object Implements IValorMap.Valor ',Cuestionario.CuestionarioDN.IValorCaracteristicaDN.Valor
        Get
            Return Me.mValor
        End Get
        Set(ByVal value As Object)
            Me.CambiarValorVal(Of Double)(value, Me.mValor)
        End Set
    End Property

    'Public Property ValorCaracPadre() As IValorCaracteristicaDN Implements IValorCaracteristicaDN.ValorCaracPadre
    '    Get
    '        Return mIVCPadre
    '    End Get
    '    Set(ByVal value As IValorCaracteristicaDN)
    '        Me.CambiarValorRef(Of IValorCaracteristicaDN)(value, mIVCPadre)
    '    End Set
    'End Property



    Public Function TraduceElValor(ByVal pValor As IValorCaracteristicaDN) As Boolean Implements IValorMap.TraduceElValor
        Return Me.mColValorNumericoCaracteristicaDN.ContineValor(pValor)
    End Function




    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN

        ' debo tener una caracteristica

        If mCaracteristica Is Nothing Then
            pMensaje = "La caracteristica no puede ser nothing para el valorCaracteristica"
            Return DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If



        If Not Me.mCaracteristica.Padre Is Nothing AndAlso Me.mIVCPadre Is Nothing Then
            pMensaje = "dado que la cracteristica es subordinada , mVNCPadre no puede ser nothing"
            Return DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If


        ' todos los valores añadidos en la col deben representar la misma caracteristica

        If Not mColValorNumericoCaracteristicaDN Is Nothing AndAlso Not mColValorNumericoCaracteristicaDN.TodosValoresDe(Me.mCaracteristica) Then
            pMensaje = "todos los valores mapados deben pertenecer a la misma caracteristica"
            Return DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If


        Return MyBase.EstadoIntegridad(pMensaje)
    End Function




    Public Property Caracteristica() As Cuestionario.CuestionarioDN.CaracteristicaDN Implements IValorMap.Caracteristica
        Get
            Return Me.mCaracteristica
        End Get
        Set(ByVal value As Cuestionario.CuestionarioDN.CaracteristicaDN)
            Me.CambiarValorRef(Of CaracteristicaDN)(value, mCaracteristica)
        End Set
    End Property
End Class

Public Class ColValorModMapDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of ValorModMapDN)
End Class
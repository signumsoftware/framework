#Region "Importaciones"

#End Region

''' <summary>
''' Esta clase se encarga que dados dos valores, un máximo y un mínimo, cuando se inserte el valor deseado
''' se encuentre en el rango entre el máximo y el mínimo. Además, tenemos un atributo: mPermitirArrastramiento
''' que en caso de ser false, no se podrá hacer el máximo menor que el mínimo, ni el mínimo mayor que el máximo
''' en caso contrario esto será posible.
''' </summary>
''' <remarks></remarks>
''' 

<Serializable()> Public Class LimitadorNumericoDN
    Inherits EntidadDN

#Region "Atributos"

    Private mValorMaximoDelRango As Double
    Private mValorMinimoDelRango As Double
    Private mValorEnElRango As Double
    Private mPermitirArrastramiento As Boolean = False

#End Region

#Region "Constructores"


    Public Sub New()
        MyBase.New()
        Me.CambiarValorVal(Of Double)(0, mValorMinimoDelRango)
        Me.CambiarValorVal(Of Double)(0, mValorEnElRango)
        Me.CambiarValorVal(Of Double)(10, mValorMaximoDelRango)

    End Sub

    Public Sub New(ByVal pValorMaximoDelRango As Double, ByVal pValorMinimoDelRango As Double)
        Dim mensaje As String = ""
        If ValidarValoresMaximoYMinimo(mensaje, pValorMaximoDelRango, pValorMinimoDelRango) Then
            Me.CambiarValorVal(Of Double)(pValorMinimoDelRango, mValorMinimoDelRango)
            Me.CambiarValorVal(Of Double)(pValorMinimoDelRango, mValorEnElRango)
            Me.CambiarValorVal(Of Double)(pValorMaximoDelRango, mValorMaximoDelRango)

        Else
            Throw New Exception(mensaje)
        End If

        Me.modificarEstado = EstadoDatosDN.SinModificar

    End Sub

    Public Sub New(ByVal pValorMaximoDelRango As Double, ByVal pValorMinimoDelRango As Double, ByVal pValorEnElRango As Double)
        Dim mensaje As String = ""
        If ValidarValoresMaximoYMinimo(mensaje, pValorMaximoDelRango, pValorMinimoDelRango) Then
            Me.CambiarValorVal(Of Double)(pValorMaximoDelRango, mValorMaximoDelRango)
            Me.CambiarValorVal(Of Double)(pValorMinimoDelRango, mValorMinimoDelRango)
        Else
            Throw New Exception(mensaje)
        End If

        If Me.ValidarQueElValorEstaEnElRango(mensaje, pValorMaximoDelRango, pValorMinimoDelRango, pValorEnElRango) Then
            Me.CambiarValorVal(Of Double)(pValorEnElRango, mValorEnElRango)
        Else
            Throw New Exception(mensaje)

        End If

        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

    Public Sub New(ByVal pValorMaximoDelRango As Double, ByVal pValorMinimoDelRango As Double, ByVal pPermitirArrastramiento As Boolean)
        Dim mensaje As String = ""
        If ValidarValoresMaximoYMinimo(mensaje, pValorMaximoDelRango, pValorMinimoDelRango) Then
            Me.CambiarValorVal(Of Double)(pValorMaximoDelRango, mValorMaximoDelRango)
            Me.CambiarValorVal(Of Double)(pValorMinimoDelRango, mValorEnElRango)
            Me.CambiarValorVal(Of Double)(pValorMinimoDelRango, mValorMinimoDelRango)
        Else
            Throw New Exception(mensaje)
        End If

        Me.CambiarValorVal(Of Boolean)(pPermitirArrastramiento, mPermitirArrastramiento)

        Me.modificarEstado = EstadoDatosDN.SinModificar

    End Sub

    Public Sub New(ByVal pValorMaximoDelRango As Double, ByVal pValorMinimoDelRango As Double, ByVal pValorEnElRango As Double, ByVal pPermitirArrastramiento As Boolean)
        Dim mensaje As String = ""
        If ValidarValoresMaximoYMinimo(mensaje, pValorMaximoDelRango, pValorMinimoDelRango) Then
            Me.CambiarValorVal(Of Double)(pValorMaximoDelRango, mValorMaximoDelRango)
            Me.CambiarValorVal(Of Double)(pValorMinimoDelRango, mValorMinimoDelRango)
        Else
            Throw New Exception(mensaje)
        End If

        If Me.ValidarQueElValorEstaEnElRango(mensaje, pValorMaximoDelRango, pValorMinimoDelRango, pValorEnElRango) Then
            Me.CambiarValorVal(Of Double)(pValorEnElRango, mValorEnElRango)
        Else
            Throw New Exception(mensaje)
        End If

        Me.CambiarValorVal(Of Boolean)(pPermitirArrastramiento, mPermitirArrastramiento)

        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"

    Public Property ValorEnElRango() As Double
        Get
            Return mValorEnElRango
        End Get
        Set(ByVal value As Double)
            Dim mensaje As String = ""
            If Me.ValidarQueElValorEstaEnElRango(mensaje, mValorMaximoDelRango, mValorMinimoDelRango, value) Then
                Me.CambiarValorVal(Of Double)(value, mValorEnElRango)
            Else
                Throw New Exception(mensaje)
            End If
        End Set
    End Property

    Public Property ValorMaximoDelRango() As Double
        Get
            Return mValorMaximoDelRango
        End Get
        Set(ByVal value As Double)
            Dim mensaje As String = ""
            If ValidarValoresMaximoYMinimo(mensaje, value, mValorMinimoDelRango) Then
                Me.CambiarValorVal(Of Double)(value, mValorMaximoDelRango)
            Else
                Try
                    ArrastraValores(LimitesDeUnRango.LimiteMaximoDelRango, value)
                Catch ex As Exception
                    Throw New Exception(ex.Message & " y " & mensaje)
                End Try
            End If
        End Set
    End Property

    Public Property ValorMinimoDelRango() As Double
        Get
            Return mValorMinimoDelRango
        End Get
        Set(ByVal value As Double)
            Dim mensaje As String = ""
            If ValidarValoresMaximoYMinimo(mensaje, mValorMaximoDelRango, value) Then
                Me.CambiarValorVal(Of Double)(value, mValorMinimoDelRango)
            Else
                Try
                    ArrastraValores(LimitesDeUnRango.LimiteMinimoDelRango, value)
                Catch ex As Exception
                    Throw New Exception(ex.Message & " y " & mensaje)
                End Try
            End If
        End Set
    End Property

    Public Property PermitirArrastramiento() As Boolean
        Get
            Return mPermitirArrastramiento
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mPermitirArrastramiento)
        End Set
    End Property

    Public ReadOnly Property EstadoModificacion() As Framework.DatosNegocio.EstadoDatosDN
        Get
            Return MyBase.Estado
        End Get
    End Property

#End Region

#Region "Métodos"

    ''' <summary>
    ''' Si el arrastramiento no está permitido se lanza una excepción, sin embargo, si el arrastramiento está 
    ''' permitido, entonces miro quien es el que arrastra, y cual es el valor al que se arrastra.
    ''' En el caso de que me pasen un valor máximo menor que el mínimo, pongo el mímimo, el máximo y el
    ''' valor en el mismo punto del máximo, y viceversa
    ''' </summary>
    ''' <param name="pQuienArrastra"></param>
    ''' <param name="pValorDelArrastramiento"></param>
    ''' <remarks></remarks>
    ''' 

    Private Sub ArrastraValores(ByVal pQuienArrastra As LimitesDeUnRango, ByVal pValorDelArrastramiento As Double)
        If mPermitirArrastramiento Then
            If pQuienArrastra = LimitesDeUnRango.LimiteMaximoDelRango Then
                Me.CambiarValorVal(Of Double)(pValorDelArrastramiento, mValorMaximoDelRango)
                Me.CambiarValorVal(Of Double)(pValorDelArrastramiento, mValorMinimoDelRango)
                Me.CambiarValorVal(Of Double)(pValorDelArrastramiento, mValorEnElRango)
            ElseIf pQuienArrastra = LimitesDeUnRango.LimiteMinimoDelRango Then
                Me.CambiarValorVal(Of Double)(pValorDelArrastramiento, mValorMaximoDelRango)
                Me.CambiarValorVal(Of Double)(pValorDelArrastramiento, mValorMinimoDelRango)
                Me.CambiarValorVal(Of Double)(pValorDelArrastramiento, mValorEnElRango)
            End If
        Else
            Throw New Exception("El arrastramiento no está permitido")
        End If
    End Sub

#End Region

#Region "Validaciones"

    ''' <summary>
    ''' Valida que el valor máximo y mínimo sean correctos, de modo que el máximo sea mayor o igual que
    ''' el mínimo
    ''' </summary>
    ''' <param name="mensaje"></param>
    ''' <param name="pValorMaximoDelRango"></param>
    ''' <param name="pValorMinimoDelRango"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' 

    Private Function ValidarValoresMaximoYMinimo(ByRef mensaje As String, ByVal pValorMaximoDelRango As Double, ByVal pValorMinimoDelRango As Double) As Boolean
        If (pValorMaximoDelRango < pValorMinimoDelRango) Then
            mensaje = "El valor máximo no puede ser menor que el mínimo"
            Return False
        End If
        Return True
    End Function

    ''' <summary>
    ''' Valida que el valor se encuentra dentro del rango que tiene la clase
    ''' </summary>
    ''' <param name="mensaje"></param>
    ''' <param name="pValorMaximoDelRango"></param>
    ''' <param name="pValorMinimoDelRango"></param>
    ''' <param name="pValorEnElRango"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' 
    Private Function ValidarQueElValorEstaEnElRango(ByRef mensaje As String, ByVal pValorMaximoDelRango As Double, _
    ByVal pValorMinimoDelRango As Double, ByVal pValorEnElRango As Double) As Boolean
        If (pValorEnElRango > pValorMaximoDelRango OrElse pValorEnElRango < pValorMinimoDelRango) Then
            mensaje = "El valor insertado no se encuentra en el rango: " & pValorMinimoDelRango.ToString _
            & " - " & pValorMaximoDelRango.ToString
            Return False
        End If
        Return True
    End Function

#End Region

End Class

Public Enum LimitesDeUnRango
    LimiteMaximoDelRango
    LimiteMinimoDelRango
End Enum
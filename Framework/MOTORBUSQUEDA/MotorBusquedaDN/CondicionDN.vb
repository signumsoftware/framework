Imports MotorBusquedaBasicasDN
<Serializable()> _
Public Class CondicionDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements ICondicionDN
    Protected mcampo As CampoDN
    Protected mOperadoresAritmetico As OperadoresAritmeticos
    Protected mValorInicial As String
    Protected mValorFinal As String
    Protected mEliminable As Boolean = True

    Public Sub New(ByVal pcampo As CampoDN, ByVal pOperadoresAritmetico As OperadoresAritmeticos, ByVal pValorInicial As String, ByVal pValorFinal As String)

        Me.CambiarValorRef(Of CampoDN)(pcampo, mcampo)
        ' mcampo = pcampo
        mOperadoresAritmetico = pOperadoresAritmetico
        mValorInicial = pValorInicial
        mValorFinal = pValorFinal
        Me.mGUID = System.Guid.NewGuid.ToString()

        Dim mensaje As String = String.Empty
        If Not New ValidadorCondicion().Validacion(mensaje, Me) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN(mensaje)
        End If
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar
    End Sub

    Public Property Eliminable() As Boolean
        Get
            Return Me.mEliminable
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mEliminable)
        End Set
    End Property

    Public Property Campo() As CampoDN
        Get
            Return Me.mcampo
        End Get
        Set(ByVal value As CampoDN)
            'mcampo = value
            Me.CambiarValorRef(Of CampoDN)(value, mcampo) '
        End Set
    End Property


    Public Property OperadoresArictmetico() As OperadoresAritmeticos
        Get
            Return Me.mOperadoresAritmetico
        End Get
        Set(ByVal value As OperadoresAritmeticos)
            'mOperadoresAritmetico = value
            Me.CambiarValorVal(Of Integer)(value, mOperadoresAritmetico)
        End Set
    End Property
    Public Property ValorInicial() As String
        Get
            Return Me.mValorInicial
        End Get
        Set(ByVal value As String)
            ' mValorInicial = value
            Me.CambiarValorVal(Of String)(value, mValorInicial)

        End Set
    End Property
    Public Property ValorFinal() As String
        Get
            Return Me.mValorFinal
        End Get
        Set(ByVal value As String)
            ' mValorFinal = value
            Me.CambiarValorVal(Of String)(value, mValorFinal)
        End Set
    End Property


    Public Property Factor1() As ICondicionDN Implements ICondicionDN.Factor1
        Get
            Return Me
        End Get
        Set(ByVal value As ICondicionDN)

        End Set
    End Property

    Public Property Factor2() As ICondicionDN Implements ICondicionDN.Factor2
        Get
            Return Nothing
        End Get
        Set(ByVal value As ICondicionDN)

        End Set
    End Property

    Public Property OperadorRelacional() As OperadoresRelacionales Implements ICondicionDN.OperadorRelacional
        Get
            Return OperadoresRelacionales.Y
        End Get
        Set(ByVal value As OperadoresRelacionales)

        End Set
    End Property

    Public Function evaluacion() As Boolean Implements ICondicionDN.evaluacion

    End Function

End Class

<Serializable()> _
Public Class ValidadorCondicion
    Implements Framework.DatosNegocio.IValidador

    Public Function Formula() As String Implements Framework.DatosNegocio.IValidador.Formula
        Throw New NotImplementedException()
    End Function

    Public Function Validacion(ByRef mensaje As String, ByVal pValor As Object) As Boolean Implements Framework.DatosNegocio.IValidador.Validacion
        Dim condicion As CondicionDN = CType(pValor, CondicionDN)

        If condicion Is Nothing Then
            mensaje = "El Objeto no está asignado"
            Return False
        End If

        If condicion.Campo Is Nothing Then
            mensaje = "No se ha definido el Campo para la Condición"
            Return False
        End If

        If condicion.ValorInicial Is Nothing Then
            mensaje = "No se ha definido el Valor Inicial para la Condición"
            Return False
        End If

        'If condicion.OperadoresArictmetico = OperadoresAritmeticos.contener_texto AndAlso condicion.ValorFinal Is Nothing Then
        '    mensaje = "No se ha definido el Valor Final del rango de la Condición"
        '    Return False
        'End If

        Return True
    End Function
End Class



<Serializable()> _
Public Class ColCondicionDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of CondicionDN)
End Class

Public Enum OperadoresRelacionales
    Y
    O
End Enum




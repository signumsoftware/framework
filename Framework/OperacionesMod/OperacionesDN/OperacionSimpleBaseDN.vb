Imports Framework.DatosNegocio

<Serializable()> _
Public Class OperacionSimpleBaseDN
    Inherits Framework.DatosNegocio.EntidadDN

    Implements IOperacionSimpleDN


    Protected mOperadorDN As IOperadorDN
    Protected mIRecSumiValorLN As IRecSumiValorLN 'este campo no dee guardarse en la base de datos
    Protected mOperando1 As ISuministradorValorDN
    Protected mOperando2 As ISuministradorValorDN
    Protected mDebeCachear As Boolean

    ' permite recuperar el ultimo valor calculado
    Protected mValorCacheado As Object
    Protected mOrdenOperacion As Integer


    Public Sub New()
        Me.modificarEstado = Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
    End Sub

    Public Sub New(ByVal pIRecuperadorSuministradorValorDN As IRecSumiValorLN)
        Me.CambiarValorRef(Of ISuministradorValorDN)(pIRecuperadorSuministradorValorDN, mIRecSumiValorLN)
    End Sub


    Public Property IOperadorDN() As IOperadorDN Implements IOperacionSimpleDN.IOperadorDN
        Get
            Return mOperadorDN
        End Get
        Set(ByVal value As IOperadorDN)
            Me.CambiarValorRef(Of IOperadorDN)(value, mOperadorDN)

        End Set
    End Property

    Public Property IRecSumiValorLN() As IRecSumiValorLN Implements ISuministradorValorDN.IRecSumiValorLN
        Get
            Return mIRecSumiValorLN
        End Get
        Set(ByVal value As IRecSumiValorLN)
            Me.CambiarValorRef(Of IRecSumiValorLN)(value, mIRecSumiValorLN)

        End Set
    End Property

    Public Property Operando1() As ISuministradorValorDN Implements IOperacionSimpleDN.Operando1
        Get
            Return Me.mOperando1
        End Get
        Set(ByVal value As ISuministradorValorDN)
            Me.CambiarValorRef(Of ISuministradorValorDN)(value, mOperando1)
        End Set
    End Property

    Public Property Operando2() As ISuministradorValorDN Implements IOperacionSimpleDN.Operando2
        Get
            Return Me.mOperando2
        End Get
        Set(ByVal value As ISuministradorValorDN)
            Me.CambiarValorRef(Of ISuministradorValorDN)(value, mOperando2)

        End Set
    End Property

    Public Property DebeCachear() As Boolean Implements IOperacionSimpleDN.DebeCachear
        Get
            Return mDebeCachear
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, mDebeCachear)
        End Set
    End Property

    ''' <summary>
    ''' permite recuperar el ultimo valor calculado
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property ValorCacheado() As Object Implements ISuministradorValorDN.ValorCacheado
        Get
            Return mValorCacheado
        End Get
    End Property

    Public Property OrdenOperacion() As Integer
        Get
            Return mOrdenOperacion
        End Get
        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mOrdenOperacion)
        End Set
    End Property

    Public Function GetValor() As Object Implements ISuministradorValorDN.GetValor

        Dim mivalor, valOp1, valOp2 As Object

        VincularOperandos()
        valOp1 = mOperando1.GetValor
        valOp2 = mOperando2.GetValor

        'Debug.Indent()
        'Debug.WriteLine("")
        'Debug.WriteLine("")
        'Debug.WriteLine("OPERACION inic id " & Me.ID)

        mivalor = Me.mOperadorDN.Ejecutar(valOp1, valOp2)

        'Debug.WriteLine("OPERACION id " & Me.ID & " N:" & Me.Nombre)
        'Debug.WriteLine("op1 " & valOp1 & " N:" & CType(mOperando1, Object).ToString)
        'Debug.WriteLine("operador  N:" & CType(Me.mOperadorDN, IEntidadDN).Nombre)
        'Debug.WriteLine("op2  " & valOp2 & " N:" & CType(mOperando2, Object).ToString)
        'Debug.WriteLine("valor Operacion " & mivalor)
        'Debug.WriteLine("FIN OPERACION id " & Me.ID)
        'Debug.Unindent()



        Me.mValorCacheado = mivalor ' permite recuperar el ultimo valor calculado



        If Me.mDebeCachear Then

            'CachearOperacion(mivalor)
            Me.mIRecSumiValorLN.CachearElemento(Me)

        End If



        Return mivalor

    End Function

    Private Sub CachearOperacion(ByVal mivalor As Object)

        Dim huella As OperResultCacheDN
        ' recuperar la guella de mi operacion
        'If Me.mIRecSumiValorLN.DataResults.Count > 0 Then
        For Each obj As Object In Me.mIRecSumiValorLN.DataSoucers

            If TypeOf obj Is OperResultCacheDN Then
                Dim miOperResultCacheDN As OperResultCacheDN = obj
                If miOperResultCacheDN.GUIDReferida = Me.GUID Then
                    huella = miOperResultCacheDN
                End If

            End If
        Next


        'End If


        If huella Is Nothing Then
            huella = New OperResultCacheDN(Me)
            Me.mIRecSumiValorLN.DataResults.Add(huella)
        End If

        huella.ActualizarValor(mivalor)

    End Sub

    Public Sub VincularOperandos()


        If mOperando1 Is Nothing Then
            If Me.mIRecSumiValorLN Is Nothing Then
                Throw New ApplicationException("El recuperador no puede ser nulo si hay operandos ausentes (mOperando1)")
            Else
                Me.Operando1 = mIRecSumiValorLN.getSuministradorValor(Me, PosicionOperando.Operando1)
                If mOperando1 Is Nothing Then
                    Throw New ApplicationException("No serecupero ningun Operando1 para la operación" & Me.ToString)
                End If


            End If
        End If

        ' aigna el recuperador si el suministrador de valor es una operación y no tine asignado ya uno
        If TypeOf mOperando1 Is ISuministradorValorDN Then
            Asignarrecuperador(mOperando1)
        End If

        If mOperando2 Is Nothing Then
            If Me.mIRecSumiValorLN Is Nothing Then
                Throw New ApplicationException("El recuperador no puede ser nulo si hay operandos ausentes (mOperando2)")
            Else
                Me.Operando2 = mIRecSumiValorLN.getSuministradorValor(Me, PosicionOperando.Operando2)
                If mOperando2 Is Nothing Then
                    Throw New ApplicationException("No serecupero ningun Operando2 para la operación" & Me.ToString)
                End If

            End If
        End If

        ' aigna el recuperador si el suministrador de valor es una operación y no tine asignado ya uno
        If TypeOf mOperando2 Is ISuministradorValorDN Then
            Asignarrecuperador(mOperando2)
        End If





    End Sub

    Private Sub Asignarrecuperador(ByVal ioper As ISuministradorValorDN)
        If ioper.IRecSumiValorLN Is Nothing Then
            ioper.IRecSumiValorLN = Me.mIRecSumiValorLN
        End If
    End Sub

    Public Function RecuperarOrden() As Integer Implements ISuministradorValorDN.RecuperarOrden
        Dim orden As Integer = 1

        If TypeOf mOperando1 Is OperacionSimpleBaseDN Then
            orden = orden + mOperando1.RecuperarOrden()
        End If

        If TypeOf mOperando2 Is OperacionSimpleBaseDN Then
            orden = orden + mOperando2.RecuperarOrden()
        End If

        Return orden
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN
        mOrdenOperacion = Me.RecuperarOrden()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Sub Limpiar() Implements ISuministradorValorDN.Limpiar
        '  mOperadorDN
        mIRecSumiValorLN = Nothing
        mOperando1.Limpiar()
        mOperando2.Limpiar()
        '  mDebeCachear As Boolean

        ' permite recuperar el ultimo valor calculado
        ' mValorCacheado As Object
        ' mOrdenOperacion As Integer
    End Sub
End Class




<Serializable()> _
Public Class ColOperacionSimpleBaseDN
    Inherits ArrayListValidable(Of OperacionSimpleBaseDN)

End Class





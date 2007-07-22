<Serializable()> _
Public Class ValidadorTipos
    Implements IValidador

#Region "Atributos"
    'Tipo que acepta este validador.
    Private mTipo As Type

    'Indica si se admiten clases derivadas del tipo que admite el validador
    Private mAdmitirClasesDerivadas As Boolean
#End Region

#Region "Constructores"
    ''' <summary>Constructor por defecto.</summary>
    ''' <param name="pTipo" type="Type">
    ''' Tipo que acepta el validador.
    ''' </param>
    ''' <param name="pAdmitirClasesDerivadas" type="Boolean">
    ''' Indica si se admiten clases derivadas del tipo que admite el validador
    ''' </param>
    Public Sub New(ByVal pTipo As Type, ByVal pAdmitirClasesDerivadas As Boolean)
        mTipo = pTipo
        mAdmitirClasesDerivadas = pAdmitirClasesDerivadas
    End Sub
#End Region

#Region "Propiedades"
    ''' <summary>Obtiene el tipo que acepta este validador.</summary>
    Public ReadOnly Property Tipo() As Type
        Get
            Return mTipo
        End Get
    End Property

    ''' <summary>Obtiene si el validador admite clases derivadas o no.</summary>
    Public ReadOnly Property AdmitirClasesDerivadas() As Boolean
        Get
            Return mAdmitirClasesDerivadas
        End Get
    End Property
#End Region

#Region "Metodos"
    ''' <summary>Este metodo verifica si el validador tiene los mismos parametros que los introducidos en los argumentos</summary>
    ''' <param name="pTipo" type="Type">
    ''' Tipo que acepta el validador.
    ''' </param>
    ''' <param name="pAdmitirClasesDerivadas" type="Boolean">
    ''' Indica si se admiten clases derivadas del tipo que admite el validador
    ''' </param>
    ''' <returns>Si tenemos los mismos parametros o no.</returns>
    Public Overridable Function VerificarValidacion(ByVal pTipo As Type, ByVal pAdmitirClasesDerivadas As Boolean) As Boolean
        If (mTipo Is pTipo AndAlso pAdmitirClasesDerivadas = mAdmitirClasesDerivadas) Then
            Return True

        Else
            Return False
        End If
    End Function

    ''' <summary>Este metodo valida el tipo un objeto segun los parametros del validador.</summary>
    ''' <param name="pValor" type="Object">
    ''' Objeto del que vamos a comprobar si su tipo es aceptado por este validador.
    ''' </param>
    ''' <returns>Si el objeto es aceptado o no.</returns>
    Public Overridable Function Validacion(ByRef mensaje As String, ByVal pValor As Object) As Boolean Implements IValidador.Validacion
        If (Me.mTipo.IsInterface) Then
            Dim tipo As Type

            'Si el tipo fijado era una interface ver si el objeto la implementa
            For Each tipo In pValor.GetType.GetInterfaces
                If (mTipo Is tipo) Then
                    Return True
                End If
            Next

        Else
            'Si se trata de un objeto miramos si es de la misma clase
            If (pValor.GetType Is mTipo) Then
                Return True
            End If

            'Ver si es una subclase si admitimos clases derivadas
            If (mAdmitirClasesDerivadas = True AndAlso pValor.GetType.IsSubclassOf(mTipo)) Then
                Return True
            End If
        End If
        mensaje = "el tipo no es compatible"
        Return False
    End Function

    ''' <summary>Devuelve la formula de validacion que usa este validador.</summary>
    ''' <remarks>Se utiliza para saber si dos validadores son iguales (realizan la misma validacion.</remarks>
    Public Overridable Function Formula() As String Implements IValidador.Formula
        Return Me.GetType.ToString & "-" & mTipo.ToString & "-" & mAdmitirClasesDerivadas
    End Function
#End Region

End Class

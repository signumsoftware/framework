
Public MustInherit Class RecSumiValorBaseDN
    Implements IRecSumiValorLN

    Protected mDataSoucers As New ArrayList
    Protected mDataResults As New ArrayList

    Public Overridable Sub ClearAll()
        mDataSoucers.Clear()
        mDataResults.Clear()
    End Sub

    Public Overridable Property DataSoucers() As System.Collections.IList Implements IRecSumiValorLN.DataSoucers
        Get
            Return mDataSoucers
        End Get
        Set(ByVal value As System.Collections.IList)
            mDataSoucers = value
        End Set
    End Property

    Public MustOverride Function getSuministradorValor(ByVal pOperacion As IOperacionSimpleDN, ByVal posicion As PosicionOperando) As ISuministradorValorDN Implements IRecSumiValorLN.getSuministradorValor


    Public Overridable Property DataResults() As IList Implements IRecSumiValorLN.DataResults
        Get
            Return mDataResults
        End Get
        Set(ByVal value As IList)
            mDataResults = value
        End Set
    End Property

    Public MustOverride Function CachearElemento(ByVal pElementos As DatosNegocio.IEntidadDN) As IList Implements IRecSumiValorLN.CachearElemento

    Public Function RecuperarPrimerResultado(ByVal pTipo As System.Type) As Object

        For Each o As Object In Me.DataResults
            If o.GetType Is pTipo Then
                Return o
            End If
        Next

        Return Nothing

    End Function
    Public Function RecuperarResultados(ByVal pTipo As System.Type) As ArrayList

        Dim al As New ArrayList

        For Each o As Object In Me.DataResults
            If o.GetType Is pTipo Then
                al.Add(o)
            End If
        Next

        Return al

    End Function
End Class


Public Class RecSumiValorDN
    Inherits RecSumiValorBaseDN

    Public Overrides Function CachearElemento(ByVal pElementos As DatosNegocio.IEntidadDN) As IList
        Throw New NotImplementedException
    End Function

    Public Overrides Function getSuministradorValor(ByVal pOperacion As IOperacionSimpleDN, ByVal posicion As PosicionOperando) As ISuministradorValorDN
        Throw New NotImplementedException

    End Function
End Class
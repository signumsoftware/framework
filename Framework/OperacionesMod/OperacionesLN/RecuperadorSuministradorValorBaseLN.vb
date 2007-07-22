Imports Framework.Operaciones.OperacionesDN



'''' <summary>
'''' clase encargada de suministrar a los operaciones los operando que esten ausentes
'''' pare allo utilizan clases de mapeado que indican tipo de isuministrador de valor se deberá intanciar en casa caso
'''' </summary>
'''' <remarks></remarks>
'Public Class RecuperadorSuministradorValorBaseLN
'    Implements IRecSumiValorLN


'    Protected mLista As System.Collections.IList
'    Protected mDataResults As OperacionesDN.ColOperResultCacheDN

'    Public Function getSuministradorValor(ByVal pOperacion As IOperacionSimpleDN, ByVal pPosicion As PosicionOperando) As ISuministradorValorDN Implements IRecSumiValorLN.getSuministradorValor


'        Dim sumi As ISuministradorValorDN
'        sumi.IRecSumiValorLN = Me 'para que el suministraodr de valor pueda alcanzar las fuentes de datos
'        Return sumi

'    End Function

'    Public Property DataSoucers() As System.Collections.IList Implements OperacionesDN.IRecSumiValorLN.DataSoucers
'        Get
'            Return mLista
'        End Get
'        Set(ByVal value As System.Collections.IList)
'            mLista = value
'        End Set
'    End Property



'    Public Property DataResults() As OperacionesDN.ColOperResultCacheDN Implements OperacionesDN.IRecSumiValorLN.DataResults
'        Get
'            Return mDataResults
'        End Get
'        Set(ByVal value As OperacionesDN.ColOperResultCacheDN)
'            Me.mDataResults = value
'        End Set
'    End Property
'End Class

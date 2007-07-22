#Region "Importaciones"

Imports System.Collections.Generic
Imports Framework.TiposYReflexion.DN
#End Region

Namespace DN
    Public Class ListaRelacionUnoNSqlsDN
        Inherits List(Of RelacionUnoNSQLsDN)

#Region "Metodos"

        Function CrearClonHistorico(ByVal pTodoDatosMapInstClase As InfoDatosMapInstClaseDN, ByVal pParteDatosMapInstClase As InfoDatosMapInstClaseDN) As ListaRelacionUnoNSqlsDN

            Dim col As New ListaRelacionUnoNSqlsDN


            For Each elemento As RelacionUnoNSQLsDN In Me
                col.Add(elemento.CrearClonHistorico(pTodoDatosMapInstClase, pParteDatosMapInstClase))
            Next

            Return col

        End Function


        Public Function GetRelacionDeTipoAParte(ByVal pTipo As System.Type) As RelacionUnoNSQLsDN
            Dim i As Integer

            For i = 0 To Me.Count - 1
                If (Me(i).TipoParte Is pTipo) Then
                    Return Me(i)
                End If
            Next

            Return Nothing
        End Function
#End Region

    End Class
End Namespace

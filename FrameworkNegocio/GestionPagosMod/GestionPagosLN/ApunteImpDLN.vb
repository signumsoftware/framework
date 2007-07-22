Imports Framework.LogicaNegocios.Transacciones
Public Class ApunteImpDLN
    Public Function Saldo(ByVal pAcreedora As FN.Localizaciones.DN.EntidadFiscalGenericaDN, ByVal pDeudora As FN.Localizaciones.DN.EntidadFiscalGenericaDN, ByVal pFecha As Date) As Double



        Using tr As New Transaccion


            Dim ad As New FN.GestionPagos.AD.ApunteImpDAD

            Saldo = ad.Saldo(pAcreedora, pDeudora, pFecha)

            tr.Confirmar()

        End Using




    End Function
End Class

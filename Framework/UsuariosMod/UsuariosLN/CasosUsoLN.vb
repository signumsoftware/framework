Imports Framework.LogicaNegocios.Transacciones

Imports Framework.Usuarios.DN

Public Class CasosUsoLN
    Inherits Framework.ClaseBaseLN.BaseGenericLN

#Region "Constructores"
    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub
#End Region

#Region "Métodos"

    Public Function RecuperarListaCasosUso() As IList(Of CasosUsoDN)
        Return MyBase.RecuperarLista(Of CasosUsoDN)()
    End Function

    Public Function GuardarCasoUso(ByVal casoUso As CasosUsoDN) As CasosUsoDN
        Return MyBase.Guardar(Of CasosUsoDN)(casoUso)
    End Function
    Public Shared Function generaCasosUso(ByVal nombreCasoUso As String, ByVal pColMetodosSistemaTotales As ColMetodosSistemaDN, ByVal pMetodosPropios As String, ByVal pRolesHeredados As ColRolDN) As ColCasosUsoDN
        Dim miColMetodosSistema As ColMetodosSistemaDN = New ColMetodosSistemaDN()

        'Recupero metodos propios
        For Each item As MetodoSistemaDN In pColMetodosSistemaTotales
            If (pMetodosPropios.IndexOf(item.NombreEnsambladoClaseMetodo) >= 0) Then
                miColMetodosSistema.Add(item)
            End If
        Next

        'Recupero metodos heredados
        If Not pRolesHeredados Is Nothing Then
            miColMetodosSistema.AddRange(pRolesHeredados.RecuperarColMetodoSistema())
        End If

        Dim miColCasosUso As ColCasosUsoDN = New ColCasosUsoDN()
        miColCasosUso.Add(New CasosUsoDN(nombreCasoUso, miColMetodosSistema))
        Return miColCasosUso
    End Function
#End Region

End Class

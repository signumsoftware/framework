#Region "Importaciones"

Imports System.Collections.Generic
Imports System.Reflection
Imports Framework.LogicaNegocios.Transacciones
Imports UsuariosDN

#End Region

''' <summary>
''' Esta clase representa la clase base para definir clases de fachada entre los servicios web
''' y la logica de negocio del servidor
''' </summary>
Public MustInherit Class BaseFachadaFL
    Inherits Framework.LogicaNegocios.Transacciones.BaseTransaccionLN


#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region



End Class

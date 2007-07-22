#Region "Importaciones"

Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.Usuarios.DN

#End Region

Public Class RolAD
    Inherits Framework.AccesoDatos.BaseTransaccionAD

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.RecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region

#Region "Métodos"

    Public Function RecuperarListaRol() As ArrayList
        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN

        ' construir la sql y los parametros

        Try
            ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
            ctd.IniciarTransaccion(Me.mTL, ProcTl)


            Dim listaiRol As ArrayList

            Dim mad As Framework.AccesoDatos.MotorAD.AD.AccesorMotorAD
            mad = New Framework.AccesoDatos.MotorAD.AD.AccesorMotorAD(ProcTl, Me.mRec, New Framework.AccesoDatos.MotorAD.AD.ConstructorAL(GetType(RolDN)))


            'listaiRol = mad.BuscarGenericoIDS("tlRolDN", Nothing)

            listaiRol = mad.BuscarGenericoIDS(GetType(RolDN), Nothing)




            Return listaiRol


        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        Finally


        End Try

        '

    End Function

    Public Function RecuperarRol(ByVal pIdRol As String) As RolDN
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN
        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        Dim gi As GestorInstanciacionLN

        Try

            '1º obtener la transaccion de procedimiento
            ctd = New Framework.LogicaNegocios.Transacciones.CTDLN
            ctd.IniciarTransaccion(Me.mTL, ProcTl)

            '2º Verificar las precondiciones de negocio para el procedimiento
            ' si alguna de las condiciones no es cierta suele tener que generarse una excepción


            '3 Realizar las operaciones propieas del procedimiento
            ' pueden implicar codigo propio ollamadas a otros LN,  AD, AS 

            ' recuperar el id del rol

            'TODO: Provisional para probar
            If pIdRol Is Nothing OrElse pIdRol = String.Empty Then
                pIdRol = 1
            End If

            gi = New GestorInstanciacionLN(ProcTl, Me.mRec)
            RecuperarRol = gi.Recuperar(pIdRol, GetType(RolDN), Nothing)

            '4º Verificar las postCondiciones de negocio para el procedimiento
            ProcTl.Confirmar() '5º confirmar transaccion si tudo fue bien 

        Catch ex As Exception
            ProcTl.Cancelar() ' *º si se dio una excepcion en mi o en alguna de mis dependencias cancelar la transaccion y propagar la excepcion
            Throw ex
        End Try
    End Function

#End Region

End Class

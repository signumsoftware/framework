Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols

Imports Framework.Usuarios.DN

<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class EmpresasWS
    Inherits System.Web.Services.WebService

    <WebMethod(True)> _
    Public Function GuardarEmpleadoYPuestosR(ByVal empPuestoR As Byte()) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As FN.Empresas.FS.EmpresaFS
        Dim principal As PrincipalDN
        Dim miEmpPR As FN.Empresas.DN.EmpleadoYPuestosRDN

        'Verificacion de sesión
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Recuperamos el dataset
        fl = New FN.Empresas.FS.EmpresaFS(Nothing, recurso)
        miEmpPR = Framework.Utilidades.Serializador.DesSerializar(empPuestoR)

        miEmpPR = fl.GuardarEmpleadoYPuestoR(principal, Me.Session.SessionID, miEmpPR)

        Return Framework.Utilidades.Serializador.Serializar(miEmpPR)

    End Function

    <WebMethod(True)> _
    Public Function RecuperarSedePrincipalxCIFEmpresa(ByVal cifNifEmpresa As String) As Byte()
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
        Dim fl As FN.Empresas.FS.EmpresaFS
        Dim principal As PrincipalDN
        Dim sedeEmp As FN.Empresas.DN.SedeEmpresaDN

        'Verificacion de sesión
        principal = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)

        'Pedimos los datos al servidor
        recurso = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)

        'Recuperamos el dataset
        fl = New FN.Empresas.FS.EmpresaFS(Nothing, recurso)

        sedeEmp = fl.RecuperarSedePrincipalxCIFEmpresa(principal, Me.Session.SessionID, cifNifEmpresa)

        Return Framework.Utilidades.Serializador.Serializar(sedeEmp)

    End Function


End Class

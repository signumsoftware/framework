Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.Collections.Generic
Imports Framework.Usuarios.DN



<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class RiesgosVehiculosWS
     Inherits System.Web.Services.WebService

    <WebMethod(True)> _
    Public Sub CargarGrafoTarificacion()


        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fs As New FN.RiesgosVehiculos.FS.RiesgosVehículosFS(Nothing, recurso)
        fs.CargarGrafoTarificacion(Me.Session.SessionID, actor)

    End Sub
    <WebMethod(True)> _
    Public Sub DesCargarGrafoTarificacion()


        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fs As New FN.RiesgosVehiculos.FS.RiesgosVehículosFS(Nothing, recurso)
        fs.DesCargarGrafoTarificacion(Me.Session.SessionID, actor)

    End Sub
    <WebMethod(True)> _
    Public Function RecuperarRiesgoMotor(ByVal pMatricula As String, ByVal pNumeroBastidor As String) As Byte()


        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.RiesgosVehiculos.FS.RiesgosVehículosFS(Nothing, recurso)

        Dim rm As FN.RiesgosVehiculos.DN.RiesgoMotorDN = fachada.RecuperarRiesgoMotor(pMatricula, pNumeroBastidor, Me.Session.SessionID, actor)

        Return Framework.Utilidades.Serializador.Serializar(rm)

    End Function
    <WebMethod(True)> _
    Public Function RecuperarModelosPorMarca(ByVal pMarca As Byte()) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.RiesgosVehiculos.FS.RiesgosVehículosFS(Nothing, recurso)

        Dim lista As List(Of FN.RiesgosVehiculos.DN.ModeloDN) = fachada.RecuperarModelosPorMarca(Framework.Utilidades.Serializador.DesSerializar(pMarca), Me.Session.SessionID, actor)

        Return Framework.Utilidades.Serializador.Serializar(lista)

    End Function

    <WebMethod(True)> _
    Public Function ExisteModeloDatos(ByVal nombreModelo As String, ByVal nombreMarca As String, ByVal estadoMatriculacion As Boolean, ByVal fecha As Date) As Boolean
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.RiesgosVehiculos.FS.RiesgosVehículosFS(Nothing, recurso)

        Return fachada.ExisteModeloDatos(nombreModelo, nombreMarca, estadoMatriculacion, fecha, Me.Session.SessionID, actor)

    End Function

    <WebMethod(True)> _
    Public Function VerificarDatosPresupuesto(ByVal presupuesto As Byte()) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.RiesgosVehiculos.FS.RiesgosVehículosFS(Nothing, recurso)
        Dim presDN As FN.Seguros.Polizas.DN.PresupuestoDN

        presDN = Framework.Utilidades.Serializador.DesSerializar(presupuesto)

        Dim presResp As FN.Seguros.Polizas.DN.PresupuestoDN = fachada.VerificarDatosPresupuesto(presDN, Me.Session.SessionID, actor)

        Return Framework.Utilidades.Serializador.Serializar(presResp)

    End Function

    <WebMethod(True)> _
    Public Function TarificarPresupuesto(ByVal presupuesto As Byte()) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.RiesgosVehiculos.FS.RiesgosVehículosFS(Nothing, recurso)
        Dim presDN As FN.Seguros.Polizas.DN.PresupuestoDN

        presDN = Framework.Utilidades.Serializador.DesSerializar(presupuesto)

        Dim presResp = fachada.TarificarPresupuesto(presDN, Me.Session.SessionID, actor)

        Return Framework.Utilidades.Serializador.Serializar(presResp)

    End Function

    <WebMethod(True)> _
    Public Function TarificarTarifa(ByVal pTarifa As Byte()) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.RiesgosVehiculos.FS.RiesgosVehículosFS(Nothing, recurso)

        Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN = Framework.Utilidades.Serializador.DesSerializar(pTarifa)

        Dim presResp = fachada.TarificarTarifa(tarifa, Me.Session.SessionID, actor)

        Return Framework.Utilidades.Serializador.Serializar(presResp)

    End Function

    <WebMethod(True)> _
    Public Sub ModificarPoliza(ByVal periodoR As Byte(), ByVal tarifa As Byte(), ByVal cuestionarioR As Byte(), ByVal fechaInicioPC As Date)
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.RiesgosVehiculos.FS.RiesgosVehículosFS(Nothing, recurso)

        Dim periodoRE As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN
        Dim tarifaE As FN.Seguros.Polizas.DN.TarifaDN
        Dim cuestionarioRE As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN

        periodoRE = Framework.Utilidades.Serializador.DesSerializar(periodoR)
        tarifaE = Framework.Utilidades.Serializador.DesSerializar(tarifa)
        cuestionarioRE = Framework.Utilidades.Serializador.DesSerializar(cuestionarioR)

        fachada.ModificarPoliza(periodoRE, tarifaE, cuestionarioRE, fechaInicioPC, Me.Session.SessionID, actor)

    End Sub

    <WebMethod(True)> _
    Public Function RecuperarModeloDatos(ByVal nombreModelo As String, ByVal nombreMarca As String, ByVal matriculada As Boolean, ByVal fecha As Date) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.RiesgosVehiculos.FS.RiesgosVehículosFS(Nothing, recurso)
        Dim respuesta As Byte()

        Dim modeloDatos As FN.RiesgosVehiculos.DN.ModeloDatosDN

        modeloDatos = fachada.RecuperarModeloDatos(nombreModelo, nombreMarca, matriculada, fecha, Me.Session.SessionID, actor)
        respuesta = Framework.Utilidades.Serializador.Serializar(modeloDatos)

        Return respuesta

    End Function

    <WebMethod(True)> _
    Public Function RecuperarProductosModelo(ByVal modelo As Byte(), ByVal matriculada As Boolean, ByVal fecha As Date) As Byte()
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.RiesgosVehiculos.FS.RiesgosVehículosFS(Nothing, recurso)
        Dim respuesta As Byte()

        Dim modeloObj As FN.RiesgosVehiculos.DN.ModeloDN = Framework.Utilidades.Serializador.DesSerializar(modelo)
        Dim colProductos As FN.Seguros.Polizas.DN.ColProductoDN

        colProductos = fachada.RecuperarProductosModelo(modeloObj, matriculada, fecha, Me.Session.SessionID, actor)
        respuesta = Framework.Utilidades.Serializador.Serializar(colProductos)

        Return respuesta

    End Function

    <WebMethod(True)> _
    Public Function CalcularNivelBonificacion(ByVal valorBonificacion As Double, ByVal categoria As Byte(), ByVal bonificacion As Byte(), ByVal fecha As Date) As String
        Dim actor As PrincipalDN = WSHelper.ControladorSesionLN.ComprobarUsuario(Me)
        Dim recurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = CType(Me.Application.Item("recurso"), Framework.LogicaNegocios.Transacciones.RecursoLN)
        Dim fachada As New FN.RiesgosVehiculos.FS.RiesgosVehículosFS(Nothing, recurso)
        Dim respuesta As String

        Dim categoriaObj As FN.RiesgosVehiculos.DN.CategoriaDN = Framework.Utilidades.Serializador.DesSerializar(categoria)
        Dim bonificacionObj As FN.RiesgosVehiculos.DN.BonificacionDN = Framework.Utilidades.Serializador.DesSerializar(bonificacion)

        respuesta = fachada.CalcularNivelBonificacion(valorBonificacion, categoriaObj, bonificacionObj, fecha, Me.Session.SessionID, actor)

        Return respuesta

    End Function

End Class

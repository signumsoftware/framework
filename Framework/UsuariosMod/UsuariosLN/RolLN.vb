#Region "Importaciones"
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.AccesoDatos.MotorAD.LN

Imports Framework.Usuarios.DN
Imports Framework.Usuarios.AD
#End Region

Public Class RolLN
    Inherits Framework.ClaseBaseLN.BaseGenericLN

#Region "Contructores"
    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub
#End Region

#Region "Métodos"

    Public Function RecuperarColRoles() As ColRolDN
        Dim tlproc As ITransaccionLogicaLN = Nothing
        Dim listaIDsRol As ArrayList
        Dim RolAD As RolAD
        Dim gi As GestorInstanciacionLN
        Dim ListaRol As IList
        Dim miRol As RolDN

        Try
            '1º obtener la transaccion de procedimiento
            tlproc = Me.ObtenerTransaccionDeProceso()

            '2º Verificar las precondiciones de negocio para el procedimiento
            ' si alguna de las condiciones no es cierta suele tener que generarse una excepción


            '3 Realiar las operaciones propieas del procedimiento
            ' pueden implicar codigo propio ollamadas a otros LN,  AD, AS 

            RecuperarColRoles = New ColRolDN
            ' recuperar el lisdo de los ids


            RolAD = New RolAD(tlproc, Me.mRec)
            listaIDsRol = RolAD.RecuperarListaRol()

            gi = New GestorInstanciacionLN(tlproc, Me.mRec)

            If listaIDsRol.Count > 0 Then
                ListaRol = gi.Recuperar(listaIDsRol, GetType(RolDN), Nothing)

                For Each miRol In listaIDsRol
                    RecuperarColRoles.Add(miRol)
                Next

            End If

            '4º Verificar las postCondiciones de negocio para el procedimiento
            tlproc.Confirmar() '5º confirmar transaccion si tudo fue bien 

        Catch ex As Exception
            tlproc.Cancelar() ' *º si se dio una excepcion en mi o en alguna de mis dependencias cancelar la transaccion y propagar la excepcion
            Throw ex
        End Try

    End Function

    Public Function GuardarRol(ByVal rol As RolDN) As RolDN
        Return MyBase.Guardar(Of RolDN)(rol)
    End Function

    ''' <summary>
    ''' Crea un rol con permiso para todo
    ''' </summary>
    ''' <param name="nombre"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GeneraRolAutorizacionTotal(ByVal nombre As String) As RolDN
        Using tr As New Transaccion()

            Dim msln As New Global.Framework.Usuarios.LN.MetodoSistemaLN()
            Dim colMetodosSistemaTotales As New ColMetodosSistemaDN()

            colMetodosSistemaTotales.AddRange(msln.RecuperarMetodos)
            Dim miColCasosUso As ColCasosUsoDN = New ColCasosUsoDN()
            Dim cu As CasosUsoDN = New CasosUsoDN("Todos los permisos", colMetodosSistemaTotales)
            miColCasosUso.Add(cu)

            ' asociar todas las operaciones al rol o al caso de uso

            Dim opln As New Framework.Procesos.ProcesosLN.OperacionesLN
            cu.ColOperaciones = opln.RecuperarTodasOperaciones()

            Dim miRol As RolDN
            miRol = New RolDN(nombre, miColCasosUso)

            tr.Confirmar()

            Return miRol
        End Using

    End Function

    ''' <summary>
    ''' guarda en el sistema una coleccion de casosos de uso y de roles definidos por una cadena de texto codificada
    ''' 
    ''' Precondiciones
    ''' 1º require que los mesots de sistema esten guardados ya en el sistema
    ''' </summary>
    ''' <param name="pDatos"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>

    Public Function GenerarRolesDeInicioDeSistema(ByVal pDatos As String) As ColRolDN
        Dim tlproc As ITransaccionLogicaLN = Nothing

        Try
            tlproc = Me.ObtenerTransaccionDeProceso()

            ' recuperar todos los metodos de sistemas
            Dim colms As New ColMetodosSistemaDN
            colms.AddRange(Me.RecuperarLista(Of MetodoSistemaDN)())

            ' cargar los datos en xml
            Dim dxml As New Xml.XmlDocument
            Dim ms As New IO.MemoryStream
            dxml.LoadXml(pDatos)
            dxml.Save(ms)
            ms.Position = 0

            ' crear el navegador
            Dim gxml As New Xml.XPath.XPathDocument(ms)
            Dim nxml As Xml.XPath.XPathNavigator
            nxml = gxml.CreateNavigator()

            ' crear el iterador para los casos de uso  de todos los roles y crear todos los casos de uso
            ' ojo un mismo caso de uso puede ser referido desde varios roles y no debe duplicarse
            Dim miXPathNodeIterator As Xml.XPath.XPathNodeIterator

            miXPathNodeIterator = nxml.Select("//Cu")

            Dim ColCu As New ColCasosUsoDN
            While (miXPathNodeIterator.MoveNext())

                Debug.WriteLine("<" & miXPathNodeIterator.Current.GetAttribute("Nombre", "") + "> " & miXPathNodeIterator.Current.Value)

                If ColCu.RecuperarPrimeroXNombre(miXPathNodeIterator.Current.GetAttribute("Nombre", "")) Is Nothing Then
                    ColCu.AddRange(CasosUsoLN.generaCasosUso(miXPathNodeIterator.Current.GetAttribute("Nombre", ""), colms, miXPathNodeIterator.Current.OuterXml, Nothing))
                Else
                    Debug.WriteLine("ya contenido")
                End If

            End While

            Me.GuardarLista(Of CasosUsoDN)(ColCu.ToListOFt)

            ' crear el iterador para los casos de uso contenidos en los roles
            ' un rol puede ser referido por varios casos de uso y no debe duplicarse
            miXPathNodeIterator = nxml.Select("//Rol")

            Dim colRol As New ColRolDN
            While (miXPathNodeIterator.MoveNext())

                Debug.WriteLine("<" & miXPathNodeIterator.Current.GetAttribute("Nombre", "") + "> " & miXPathNodeIterator.Current.Value)

                If colRol.RecuperarPrimeroXNombre(miXPathNodeIterator.Current.GetAttribute("Nombre", "")) Is Nothing Then
                    colRol.Add(Me.generaRol(miXPathNodeIterator.Current.GetAttribute("Nombre", ""), miXPathNodeIterator.Current, Nothing))
                Else
                    Debug.WriteLine("ya contenido")
                End If

            End While

            Me.GuardarLista(Of RolDN)(colRol.ToListOFt)

            tlproc.Confirmar()

        Catch ex As Exception
            If tlproc IsNot Nothing Then
                tlproc.Cancelar()
            End If
            Throw ex
        End Try

    End Function


    Public Function generaRol(ByVal nombre As String, ByVal pCasosUsoPropios As Xml.XPath.XPathNavigator, ByVal pRolesHeredados As ColRolDN) As RolDN
        Dim ProcTl As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing
        Dim ctd As Framework.LogicaNegocios.Transacciones.CTDLN
        Try
            ProcTl = Me.ObtenerTransaccionDeProceso

            Dim miRol As RolDN
            Dim miColCasosUso, colCUTotales As ColCasosUsoDN
            miColCasosUso = New ColCasosUsoDN()
            colCUTotales = New ColCasosUsoDN()


            Dim culn As CasosUsoLN
            culn = New CasosUsoLN(ProcTl, Me.mRec)
            colCUTotales.AddRange(culn.RecuperarListaCasosUso())


            ' como nos encontramos en el rol nos movemos a la coleccion  ( colcu)
            pCasosUsoPropios.MoveToFirstChild()
            ' como nos encontramos en la coleccion ( colcu) nos movemos a sus hijos CU
            If pCasosUsoPropios.MoveToFirstChild() Then
                Do

                    Debug.WriteLine("<" & pCasosUsoPropios.GetAttribute("Nombre", "") + "> " & pCasosUsoPropios.Value)


                    If Not miColCasosUso.RecuperarPrimeroXNombre(pCasosUsoPropios.GetAttribute("Nombre", "")) Is Nothing Then
                        ' ya lo contine
                    Else
                        'no lo contine y debe ser añadido
                        Dim cu As CasosUsoDN
                        cu = colCUTotales.RecuperarPrimeroXNombre(pCasosUsoPropios.GetAttribute("Nombre", ""))
                        If cu Is Nothing Then
                            Throw New ApplicationException("no se encontro el caso de uso de nombre:" & pCasosUsoPropios.GetAttribute("Nombre", ""))
                        Else
                            miColCasosUso.Add(cu)

                        End If

                    End If


                Loop While pCasosUsoPropios.MoveToNext
            End If






            miRol = New RolDN(nombre, miColCasosUso)

            ProcTl.Confirmar()

            Return miRol
        Catch ex As Exception
            ProcTl.Cancelar()
            Throw ex
        End Try
    End Function

#End Region

End Class


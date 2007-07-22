Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Framework.DatosNegocio
<TestClass()> Public Class DatosNegocioTest1

#Region "Additional test attributes"
    '
    ' You can use the following additional attributes as you write your tests:
    '
    ' Use ClassInitialize to run code before running the first test in the class
    ' <ClassInitialize()> Public Shared Sub MyClassInitialize(ByVal testContext As TestContext)
    ' End Sub
    '
    ' Use ClassCleanup to run code after all tests in a class have run
    ' <ClassCleanup()> Public Shared Sub MyClassCleanup()
    ' End Sub
    '
    ' Use TestInitialize to run code before running each test
    ' <TestInitialize()> Public Sub MyTestInitialize()
    ' End Sub
    '
    ' Use TestCleanup to run code after each test has run
    ' <TestCleanup()> Public Sub MyTestCleanup()
    ' End Sub
    '
#End Region



    <TestMethod()> Public Sub ProbarHuellaDeT()



        Dim miht As New htClaseA



        System.Diagnostics.Debug.WriteLine(miht.TipoEntidadReferidaFullNme)

        If Not miht.TipoEntidadReferida Is GetType(ClaseA) Then
            Throw New ApplicationException("el tipo debía ser el mismo")
        End If
    End Sub

    <TestMethod()> Public Sub ProbarNododeTAñadirYEliminarNodo()
        ' TODO: Add test logic here

        Dim miHojaDeNodoDeT As HojaDeNodoDeT
        Dim miNodoDeT, miNodoDeT2, nodoRaizArbol As NodoDeT

        nodoRaizArbol = CrearArbol()
        miNodoDeT = nodoRaizArbol.Hijos(0)
        miNodoDeT.Nombre = "nh1"
        miHojaDeNodoDeT = miNodoDeT.ColHojas(0)
        miNodoDeT2 = miNodoDeT.Clone
        miNodoDeT2.Nombre = "clonNh1"


        If Not nodoRaizArbol.Estado = Framework.DatosNegocio.EstadoDatosDN.Modificado Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("debia estar modificada")
        End If

        If Not nodoRaizArbol.Profundidad = 0 Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("debia estar modificada")
        End If

        If Not nodoRaizArbol.ProfundidadMaxDescendenia = 1 Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("debia estar modificada")
        End If

        If Not nodoRaizArbol.ContenidoEnArbol(miNodoDeT, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.MismaRef) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("debia estar modificada")
        End If


        If Not nodoRaizArbol.ContenidoEnArbol(miHojaDeNodoDeT, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.MismaRef) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("debia estar modificada")
        End If

        If Not nodoRaizArbol.ContieneHijo(miNodoDeT, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.MismaRef) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("debia estar modificada")
        End If

        If nodoRaizArbol.Eliminar(miHojaDeNodoDeT, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos).Count <> 1 Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("debia estar modificada")
        End If

        If nodoRaizArbol.Contenido(miHojaDeNodoDeT, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("no debiera existir en el arbol")
        End If


        If nodoRaizArbol.Eliminar(miNodoDeT, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos).Count <> 1 Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("debia estar modificada")
        End If

        If nodoRaizArbol.Contenido(miNodoDeT, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("no debiera existir en el arbol")
        End If


        ''''''''''''''''''''''''''''
        nodoRaizArbol.AñadirHijo(miNodoDeT)
        nodoRaizArbol.AñadirHijo(miNodoDeT2)

        If nodoRaizArbol.Eliminar(miNodoDeT, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos).Count <> 2 Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("debia estar modificada")
        End If

        If nodoRaizArbol.Contenido(miNodoDeT, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("no debiera existir en el arbol")
        End If

        ''''''''''''''''''''''''''''''''




        nodoRaizArbol.AñadirHijo(miNodoDeT)
        nodoRaizArbol.AñadirHijo(miNodoDeT2)

        If nodoRaizArbol.Eliminar(miNodoDeT, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Clones).Count <> 1 Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("debia estar modificada")
        End If

        If nodoRaizArbol.Contenido(miNodoDeT2, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.MismaRef) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("no debiera existir en el arbol")
        End If

        If Not nodoRaizArbol.Contenido(miNodoDeT, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.MismaRef) Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("no debiera existir en el arbol")
        End If

        '''''''''''''''''''
    End Sub
    <TestMethod()> Public Sub CrearArbolNodoDeT()
        CrearArbol()
    End Sub





    Public Shared Function CrearArbol2() As NodoDeT
        Dim miHojaDeNodoDeT As HojaDeNodoDeT
        Dim miNodoDeT, miNodoDeT1, nodoRaizArbol As NodoDeT


        nodoRaizArbol = New NodoDeT


        miHojaDeNodoDeT = New HojaDeNodoDeT
        miNodoDeT = New NodoDeT
        miHojaDeNodoDeT.Nombre = "h1"
        miNodoDeT.Nombre = "N1"
        miNodoDeT.ColHojas.Add(miHojaDeNodoDeT)
        nodoRaizArbol.Hijos.Add(miNodoDeT)

        miNodoDeT1 = New NodoDeT
        miNodoDeT1.Nombre = "N2"
        nodoRaizArbol.Hijos.Add(miNodoDeT1)

        miHojaDeNodoDeT = New HojaDeNodoDeT
        miNodoDeT = New NodoDeT
        miHojaDeNodoDeT.Nombre = "h2"
        miNodoDeT.Nombre = "N3"
        miNodoDeT.ColHojas.Add(miHojaDeNodoDeT)
        miNodoDeT1.AñadirHijo(miNodoDeT)

        miHojaDeNodoDeT = New HojaDeNodoDeT
        miNodoDeT = New NodoDeT
        miHojaDeNodoDeT.Nombre = "h3"
        miNodoDeT.Nombre = "N4"
        miNodoDeT.ColHojas.Add(miHojaDeNodoDeT)
        miNodoDeT1.AñadirHijo(miNodoDeT)

        If Not nodoRaizArbol.Estado = Framework.DatosNegocio.EstadoDatosDN.Modificado Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("nodoRaizArboldebia estar modificada")
        End If


        Return nodoRaizArbol
    End Function

    <TestMethod()> Public Sub PodarHojasTest()
        Dim nodoRaizArbol As NodoDeT
        nodoRaizArbol = CrearArbol2()

        Dim hojas As New ArrayListValidable(Of HojaDeNodoDeT)
        Dim Hoja As HojaDeNodoDeT

        hojas.AddRange(nodoRaizArbol.RecuperarColHojasConenidas())
        Hoja = hojas.Item(0)
        hojas.Remove(Hoja)

        If Not nodoRaizArbol.Contenido(Hoja, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.MismaRef) Then
            Throw New ApplicationException("la hoja  debiera estar referida en el arbol")
        End If


        nodoRaizArbol.PodarNodosHijosNoContenedoresHojas(hojas, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.MismaRef)

        If nodoRaizArbol.Contenido(hojas, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.MismaRef) Then
            Throw New ApplicationException("la hoja no debiera estar referida en el arbol")
        End If


    End Sub


    Public Shared Function CrearArbol() As NodoDeT
        Dim miHojaDeNodoDeT As HojaDeNodoDeT
        Dim miNodoDeT, miNodoDeT1, miNodoDeT2, nodoRaizArbol As NodoDeT


        nodoRaizArbol = New NodoDeT
        miHojaDeNodoDeT = New HojaDeNodoDeT
        miNodoDeT = New NodoDeT

        miHojaDeNodoDeT.Nombre = "h1"

        'If Not miHojaDeNodoDeT.Estado = Framework.DatosNegocio.EstadoDatosDN.Modificado Then
        '    Throw New Framework.DatosNegocio.ApplicationExceptionDN("debia estar modificada")
        'End If

        If miNodoDeT.Estado = Framework.DatosNegocio.EstadoDatosDN.Modificado Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("miNodoDeT NO debia estar modificada")
        End If

        miNodoDeT.ColHojas.Add(miHojaDeNodoDeT)

        If Not miNodoDeT.Estado = Framework.DatosNegocio.EstadoDatosDN.Modificado Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("debia estar modificada")
        End If


        If nodoRaizArbol.Estado = Framework.DatosNegocio.EstadoDatosDN.Modificado Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("miNodoDeT NO debia estar modificada")
        End If


        nodoRaizArbol.Hijos.Add(miNodoDeT)

        If Not nodoRaizArbol.Estado = Framework.DatosNegocio.EstadoDatosDN.Modificado Then
            Throw New Framework.DatosNegocio.ApplicationExceptionDN("nodoRaizArboldebia estar modificada")
        End If










        Return nodoRaizArbol
    End Function

    <TestMethod()> Public Sub PruebaEntidadTipoycol()

        Dim te As Framework.DatosNegocio.EntidadTipoDN(Of EnumarecionPrueba)

        ' te = New Framework.DatosNegocio.EntidadTipoDN(Of EnumarecionPrueba)(EnumarecionPrueba.dos)


        Dim col As Framework.DatosNegocio.ColEntidadTipoDN(Of EnumarecionPrueba)
        ' col = Framework.DatosNegocio.EntidadTipoDN(Of EnumarecionPrueba).RecuperarTiposTodos

        If col.Count = 3 Then

            If Not col.Contiene(te, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
                Throw New ApplicationException
            End If

        Else
            Throw New ApplicationException
        End If

    End Sub



    Public Sub EstadoModificacionNodoTest()

    End Sub

    Public Enum EnumarecionPrueba
        uno
        dos
        tres
    End Enum



    <TestMethod()> Public Sub ProbarMetodo()


        Dim partedeA As New PartedeCalseA
        partedeA.Nombre = "pelotito"


        Dim ca As New ClaseA
        ca.Nombre = "lucas"
        ca.valor = 15
        ca.PartedeCalseA = partedeA


        Dim cha As New CalseHeredaA
        Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ClonSuperfEnClaseCompatible(ca, cha, True)


        If cha.valor <> 15 Then
            Throw New ApplicationException("el valor debia ser 15")
        End If

        If Not cha.PartedeCalseA.Nombre = ca.PartedeCalseA.Nombre Then
            Throw New ApplicationException("error")
        End If

        If Not cha.PartedeCalseA Is ca.PartedeCalseA Then
            Throw New ApplicationException("error")
        End If


        Dim cca As New CalseCompatibleA
        Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ClonSuperfEnClaseCompatible(ca, cca, True)

        If cca.valor <> 15 Then
            Throw New ApplicationException("el valor debia ser 15")
        End If

        If Not cca.PartedeCalseA Is ca.PartedeCalseA Then
            Throw New ApplicationException("error")
        End If


    End Sub


End Class



<Serializable()> _
Public Class NodoDeT
    Inherits Framework.DatosNegocio.Arboles.NodoBaseTDN(Of HojaDeNodoDeT)
End Class


<Serializable()> _
Public Class HojaDeNodoDeT
    Inherits Framework.DatosNegocio.EntidadDN

    Public Sub New()

    End Sub

    'Public Sub New(ByVal pId As String, ByVal pNombre As String, ByVal pGuid As String)
    '    mID = pId
    '    mNombre = pNombre
    '    mGUID = pGuid
    'End Sub

End Class




<Serializable()> Public Class ClaseA
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mValor As Int16
    Protected mPartedeCalseA As PartedeCalseA


    Public Property PartedeCalseA() As PartedeCalseA
        Get
            Return mPartedeCalseA
        End Get
        Set(ByVal value As PartedeCalseA)
            Me.CambiarValorRef(Of PartedeCalseA)(value, mPartedeCalseA)
        End Set
    End Property

    Public Property valor() As Int16
        Get
            Return Me.mValor
        End Get
        Set(ByVal value As Int16)
            Me.CambiarValorVal(Of Int16)(value, Me.mValor)
        End Set
    End Property

End Class


<Serializable()> Public Class PartedeCalseA
    Inherits Framework.DatosNegocio.EntidadDN
End Class


<Serializable()> Public Class CalseHeredaA
    Inherits ClaseA
End Class


<Serializable()> Public Class CalseCompatibleA
    Inherits Framework.DatosNegocio.EntidadDN


    Protected mValor As Int16
    Protected mPartedeCalseA As PartedeCalseA


    Public Property PartedeCalseA() As PartedeCalseA
        Get
            Return mPartedeCalseA
        End Get
        Set(ByVal value As PartedeCalseA)
            Me.CambiarValorRef(Of PartedeCalseA)(value, mPartedeCalseA)
        End Set
    End Property

    Public Property valor() As Int16
        Get
            Return Me.mValor
        End Get
        Set(ByVal value As Int16)
            Me.CambiarValorVal(Of Int16)(value, Me.mValor)
        End Set
    End Property
End Class

Public Class htClaseA
    Inherits Framework.DatosNegocio.HuellaEntidadTipadaDN(Of ClaseA)

End Class
Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Framework.Usuarios.DN

<TestClass()> Public Class MV2DNTest

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

    <TestMethod()> Public Sub TestMethod1()

        Dim instMap As MV2DN.InstanciaMapDN
        Dim agMap As MV2DN.AgrupacionMapDN
        Dim propMap As MV2DN.PropMapDN

        instMap = New MV2DN.InstanciaMapDN
        instMap.Nombre = "Persona vis1"
        Dim tag As New MV2DN.TipoAgrupacionMapDN
        tag.id = "1"
        tag.Nombre = "Solapa"

        agMap = New MV2DN.AgrupacionMapDN(instMap, tag)
        propMap = New MV2DN.PropMapDN
        propMap.Ico = "icono propiedad"
        agMap.ColPropMap.Add(propMap)


        System.Diagnostics.Debug.WriteLine(instMap.ToXML)

    End Sub


    <TestMethod()> Public Sub TestMethod2()

        Dim instMap, instMap2 As MV2DN.InstanciaMapDN
        Dim agMap As MV2DN.AgrupacionMapDN
        Dim propMap As MV2DN.PropMapDN

        instMap = New MV2DN.InstanciaMapDN
        instMap.Nombre = "Persona vis1"

        Dim tag As New MV2DN.TipoAgrupacionMapDN
        tag.id = "1"
        tag.Nombre = "Solapa"
        agMap = New MV2DN.AgrupacionMapDN(instMap, tag)

        propMap = New MV2DN.PropMapDN
        propMap.Ico = "icono propiedad"
        propMap.NombreProp = "id"
        propMap.NombreVis = "codigo"
        agMap.ColPropMap.Add(propMap)


        System.Diagnostics.Debug.WriteLine(instMap.ToXML)

        Dim doc As New System.Xml.XmlDocument
        doc.LoadXml(instMap.ToXML)
        Dim tr As New IO.StringReader(instMap.ToXML)
        instMap2 = New MV2DN.InstanciaMapDN
        instMap2.FromXML(tr)
        System.Diagnostics.Debug.WriteLine(instMap2.ToXML)

    End Sub
    <TestMethod()> Public Sub TestMethod3()

        Dim instMap, instMap2 As MV2DN.InstanciaMapDN
        Dim agMap As MV2DN.AgrupacionMapDN
        Dim propMap As MV2DN.PropMapDN

        instMap = New MV2DN.InstanciaMapDN
        instMap.Nombre = "Principal vis1"

        Dim tag As New MV2DN.TipoAgrupacionMapDN
        tag.ID = "1"
        tag.Nombre = "Solapa"

        agMap = New MV2DN.AgrupacionMapDN(instMap, tag)
        agMap.NombreVis = "Solapa1"

        propMap = New MV2DN.PropMapDN
        propMap.NombreProp = "id"
        propMap.NombreVis = "codigo:"
        agMap.ColPropMap.Add(propMap)

        propMap = New MV2DN.PropMapDN
        propMap.NombreProp = "Nombre"
        propMap.NombreVis = "NombPrincipal:"
        agMap.ColPropMap.Add(propMap)

        propMap = New MV2DN.PropMapDN
        propMap.NombreProp = "UsuarioDN.Nombre"
        propMap.NombreVis = "NombUsuario:"
        agMap.ColPropMap.Add(propMap)

        System.Diagnostics.Debug.WriteLine(instMap.ToXML)

 

        Dim iv As MV2DN.InstanciaVinc
        iv = New MV2DN.InstanciaVinc(GetType(PrincipalDN), instMap, Nothing, Nothing)
        System.Diagnostics.Debug.WriteLine(iv.ColAgrupacionVinc.Item(0).ColPropVinc.Count)
        If iv.ColAgrupacionVinc.Item(0).ColPropVinc.Count <> 3 Then
            Throw New ApplicationException("error")
        End If
    End Sub


    <TestMethod()> Public Sub TestMethod4()

        Dim instMap, instMap2 As MV2DN.InstanciaMapDN
        Dim agMap As MV2DN.AgrupacionMapDN
        Dim propMap As MV2DN.PropMapDN

        instMap = New MV2DN.InstanciaMapDN
        instMap.Nombre = "Principal vis1"

        Dim tag As New MV2DN.TipoAgrupacionMapDN
        tag.ID = "1"
        tag.Nombre = "Solapa"

        agMap = New MV2DN.AgrupacionMapDN(instMap, tag)
        agMap.NombreVis = "Solapa1"

        propMap = New MV2DN.PropMapDN
        propMap.NombreProp = "ID"
        propMap.NombreVis = "codigo:"
        agMap.ColPropMap.Add(propMap)

        propMap = New MV2DN.PropMapDN
        propMap.NombreProp = "Nombre"
        propMap.NombreVis = "NombPrincipal:"
        agMap.ColPropMap.Add(propMap)

        propMap = New MV2DN.PropMapDN
        propMap.NombreProp = "UsuarioDN.Nombre"
        propMap.NombreVis = "NombUsuario:"
        agMap.ColPropMap.Add(propMap)

        System.Diagnostics.Debug.WriteLine(instMap.ToXML)

        Dim iv As MV2DN.InstanciaVinc
        iv = New MV2DN.InstanciaVinc(GetType(PrincipalDN), instMap, Nothing, Nothing)
        System.Diagnostics.Debug.WriteLine(iv.ColAgrupacionVinc.Item(0).ColPropVinc.Count)
        If iv.ColAgrupacionVinc.Item(0).ColPropVinc.Count <> 3 Then
            Throw New ApplicationException("error")
        End If


        ' creacion de la instancia de principal
        Dim usuario As UsuarioDN
        usuario = New UsuarioDN("jose parada", False)

        Dim prin As PrincipalDN
        prin = New PrincipalDN("principal1", usuario, New ColRolDN)
        iv.DN = prin

        For Each pv As MV2DN.PropVinc In iv.ColAgrupacionVinc.Item(0).ColPropVinc
            System.Diagnostics.Debug.WriteLine(pv.Map.NombreVis & pv.Value.ToString)

        Next
        iv.ColAgrupacionVinc.Item(0).ColPropVinc(2).Value = iv.ColAgrupacionVinc.Item(0).ColPropVinc(2).Value.ToString & " *** el feo"

        For Each pv As MV2DN.PropVinc In iv.ColAgrupacionVinc.Item(0).ColPropVinc
            System.Diagnostics.Debug.WriteLine(pv.Map.NombreVis & pv.Value.ToString)

        Next


        
    End Sub

    <TestMethod()> Public Sub TestMethod5()

        Dim instMap, instMap2 As MV2DN.InstanciaMapDN
        Dim agMap As MV2DN.AgrupacionMapDN
        Dim propMap As MV2DN.PropMapDN

        instMap = New MV2DN.InstanciaMapDN
        instMap.Nombre = "Principal vis1"

        Dim tag As New MV2DN.TipoAgrupacionMapDN
        tag.ID = "1"
        tag.Nombre = "Solapa"

        agMap = New MV2DN.AgrupacionMapDN(instMap, tag)
        agMap.NombreVis = "Solapa1"

        propMap = New MV2DN.PropMapDN
        propMap.NombreProp = "ID"
        propMap.NombreVis = "codigo:"
        agMap.ColPropMap.Add(propMap)

        propMap = New MV2DN.PropMapDN
        propMap.NombreProp = "Nombre"
        propMap.NombreVis = "NombPrincipal:"
        agMap.ColPropMap.Add(propMap)

        propMap = New MV2DN.PropMapDN
        propMap.NombreProp = "UsuarioDN.Nombre"
        propMap.NombreVis = "NombUsuario:"
        agMap.ColPropMap.Add(propMap)



        propMap = New MV2DN.PropMapDN
        propMap.NombreProp = "Nombreeeeee"
        propMap.NombreVis = "NoEsta:"
        agMap.ColPropMap.Add(propMap)

        System.Diagnostics.Debug.WriteLine(instMap.ToXML)

        Dim iv As MV2DN.InstanciaVinc
        iv = New MV2DN.InstanciaVinc(GetType(PrincipalDN), instMap, Nothing, Nothing)
        System.Diagnostics.Debug.WriteLine(iv.ColAgrupacionVinc.Item(0).ColPropVinc.Count)
        If iv.ColAgrupacionVinc.Item(0).ColPropVinc.Count <> 4 Then
            Throw New ApplicationException("error")
        End If


        ' creacion de la instancia de principal
        Dim usuario As UsuarioDN
        usuario = New UsuarioDN("jose parada", False)

        Dim prin As PrincipalDN
        prin = New PrincipalDN("principal1", usuario, New ColRolDN)
        iv.DN = prin

        For Each pv As MV2DN.PropVinc In iv.ColAgrupacionVinc.Item(0).ColPropVinc

            If pv.Correcta Then
                System.Diagnostics.Debug.WriteLine("(" & pv.Correcta & ")" & pv.Map.NombreVis & pv.Value.ToString)

            Else
                System.Diagnostics.Debug.WriteLine("(" & pv.Correcta & ")" & pv.Map.NombreVis)

            End If
        Next


        iv.ColAgrupacionVinc.Item(0).ColPropVinc(2).Value = iv.ColAgrupacionVinc.Item(0).ColPropVinc(2).Value.ToString & " *** el feo"


        For Each pv As MV2DN.PropVinc In iv.ColAgrupacionVinc.Item(0).ColPropVinc

            If pv.Correcta Then
                System.Diagnostics.Debug.WriteLine("(" & pv.Correcta & ")" & pv.Map.NombreVis & pv.Value.ToString)

            Else
                System.Diagnostics.Debug.WriteLine("(" & pv.Correcta & ")" & pv.Map.NombreVis)

            End If
        Next

    End Sub
End Class





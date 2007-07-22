Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting



'Imports Framework.Usuarios.DN
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.DatosNegocio
Imports Framework
Imports Framework.TiposYReflexion.DN
Imports System.Collections
Imports Framework.LogicaNegocios.Transacciones
'Imports Framework.Procesos.ProcesosLN

<TestClass()> Public Class UnitTest1

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

    Dim mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN

    Private Sub ObtenerRecurso()

        Dim connectionstring As String
        Dim htd As New Dictionary(Of String, Object)

        connectionstring = "server=localhost;database=ssPruebasFT;user=sa;pwd='sa'"
        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        'Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New Framework.AccesoDatos.MotorAD.LN.GestorMapPersistenciaCamposNULOLN()
        'Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaXXXXXXXXXXXXTest
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New Framework.GestorInformes.AdaptadorInformesQueryBuilding.AD.MapeadoPersistencia()

    End Sub


    <TestMethod()> Public Sub GenerarEntorno()
        ObtenerRecurso()

        Using New Framework.LogicaNegocios.Transacciones.CajonHiloLN(mRecurso)
            '1º invocamos al crear tablas del gbd
            Dim gbd As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.AD.AdaptadorInformesQueryBuildingGBD(mRecurso)
            '2º generamos las tablas a partir de las dns
            gbd.CrearTablas()
        End Using
    End Sub

    <TestMethod()> Public Sub GenerarEntornoPruebas()
        ObtenerRecurso()

        Dim sql As String

        Using New Framework.LogicaNegocios.Transacciones.CajonHiloLN(mRecurso)
            Using New Framework.LogicaNegocios.Transacciones.Transaccion()
                'creamos las tablas para las pruebas
                Dim ej As New Framework.AccesoDatos.Ejecutor(Nothing, mRecurso)

                'eliminamos la tabla de rutas
                sql = "IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tlInformeRutas]') AND type in (N'U')) DROP TABLE [dbo].[tlInformeRutas]"
                ej.EjecutarNoConsulta(sql)

                'eliminamos la tabla de camiones
                sql = "IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tlInformeCamiones]') AND type in (N'U')) DROP TABLE [dbo].[tlInformeCamiones]"
                ej.EjecutarNoConsulta(sql)

                'eliminamos la tabla de camioneros
                sql = "IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tlInformeCamioneros]') AND type in (N'U')) DROP TABLE [dbo].[tlInformeCamioneros]"
                ej.EjecutarNoConsulta(sql)

                'creamos la tabla de ruta
                sql = "CREATE TABLE [dbo].[tlInformeRutas]([id] [int] IDENTITY(1,1) NOT NULL,[Ruta] [nvarchar](50) COLLATE Modern_Spanish_CI_AS NULL) ON [PRIMARY]"
                ej.EjecutarNoConsulta(sql)

                'creamos la tabla de camiones
                sql = "CREATE TABLE [dbo].[tlInformeCamiones]([id] [int] IDENTITY(1,1) NOT NULL,[Camion] [nvarchar](50) COLLATE Modern_Spanish_CI_AS NOT NULL,[Ejes] [int] NULL,CONSTRAINT [PK_tlInformeCamiones_1] PRIMARY KEY CLUSTERED([id] ASC)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]) ON [PRIMARY]"
                ej.EjecutarNoConsulta(sql)

                'creamos la tabla de camioneros
                sql = "CREATE TABLE [dbo].[tlInformeCamioneros]([id] [int] IDENTITY(1,1) NOT NULL,[Camionero] [nvarchar](50) COLLATE Modern_Spanish_CI_AS NULL,[Edad] [int] NOT NULL,[idCamion] [int] NULL,[idRuta] [int] NULL,CONSTRAINT [PK_tlInformeCamiones] PRIMARY KEY CLUSTERED([id] ASC)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]) ON [PRIMARY]"
                ej.EjecutarNoConsulta(sql)

                'insertamos las rutas
                sql = "INSERT INTO [ssPruebasFT].[dbo].[tlInformeRutas]([Ruta])VALUES('Ruta A-1')"
                ej.EjecutarNoConsulta(sql)

                sql = "INSERT INTO [ssPruebasFT].[dbo].[tlInformeRutas]([Ruta])VALUES('Ruta A-2')"
                ej.EjecutarNoConsulta(sql)

                sql = "INSERT INTO [ssPruebasFT].[dbo].[tlInformeRutas]([Ruta])VALUES('Ruta A-3')"
                ej.EjecutarNoConsulta(sql)

                'insertamos los camiones
                sql = "INSERT INTO [ssPruebasFT].[dbo].[tlInformeCamiones] ([Camion],[Ejes]) VALUES ('Camión 1', 2)"
                ej.EjecutarNoConsulta(sql)

                sql = "INSERT INTO [ssPruebasFT].[dbo].[tlInformeCamiones] ([Camion],[Ejes]) VALUES ('Camión 2', 4)"
                ej.EjecutarNoConsulta(sql)

                sql = "INSERT INTO [ssPruebasFT].[dbo].[tlInformeCamiones] ([Camion],[Ejes]) VALUES ('Camión 3', 6)"
                ej.EjecutarNoConsulta(sql)

                'insertamos los camioneros
                sql = "INSERT INTO [ssPruebasFT].[dbo].[tlInformeCamioneros] ([Camionero],[Edad],[idCamion],[idRuta]) VALUES ('Perro Pérez', 40, 1, 1)"
                ej.EjecutarNoConsulta(sql)

                sql = "INSERT INTO [ssPruebasFT].[dbo].[tlInformeCamioneros] ([Camionero],[Edad],[idCamion],[idRuta]) VALUES ('Gato López', 35, 2, 2)"
                ej.EjecutarNoConsulta(sql)

                sql = "INSERT INTO [ssPruebasFT].[dbo].[tlInformeCamioneros] ([Camionero],[Edad],[idCamion],[idRuta]) VALUES ('Pajarito Martínez', 52, 3, 3)"
                ej.EjecutarNoConsulta(sql)

                Transaccion.Actual.Confirmar()
            End Using

        End Using
    End Sub


    <TestMethod()> Public Sub CargarInformeConEsquemaXML()
        ObtenerRecurso()
        Using New Framework.LogicaNegocios.Transacciones.CajonHiloLN(mRecurso)
            Dim aiqb As Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.AdaptadorInformesQueryBuildingDN = Me.CrearAIQB()
            Dim ln As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.LN.AdaptadorInformesQueryBuildingLN()
            Dim archivo As Byte() = ln.GenerarEsquemaXMLEnPlantilla_Archivo(aiqb)
            Dim w As New System.IO.BinaryWriter(System.IO.File.Open("D:\Signum\Signum\PlantillaEsquema.docx", IO.FileMode.Create))
            w.Write(archivo)
            w.Flush()
            w.Close()
        End Using
    End Sub

    <TestMethod()> Public Sub GenerarInformeDePrueba()
        ObtenerRecurso()
        Using New Framework.LogicaNegocios.Transacciones.CajonHiloLN(mRecurso)
            Dim aiqb As Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.AdaptadorInformesQueryBuildingDN = Me.CrearAIQB()
            Dim ln As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.LN.AdaptadorInformesQueryBuildingLN()
            Dim archivo As Byte() = ln.GenerarInforme_Archivo(aiqb)
            Dim w As New System.IO.BinaryWriter(System.IO.File.Open("D:\Signum\Signum\InformeCamioneros.docx", IO.FileMode.Create))
            w.Write(archivo)
            w.Flush()
            w.Close()
        End Using
    End Sub

    Private Function CrearAIQB() As Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.AdaptadorInformesQueryBuildingDN
        Dim aiqb As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.AdaptadorInformesQueryBuildingDN()
        aiqb.TablasPrincipales = Me.CrearTablasPrincipales()
        aiqb.Plantilla = Me.CrearContenedorPlantilla()
        Return aiqb
    End Function

    Private Function CrearTablasPrincipales() As Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.ColTablaPrincipalAIQB
        Dim coltp As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.ColTablaPrincipalAIQB()
        Dim tp As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.TablaPrincipalAIQB()
        tp.CargarDatosSelect("id", "SELECT id FROM tlInformeCamioneros WHERE Edad>30", Nothing)
        tp.NombreTablaBD = "tlInformeCamioneros"
        tp.NombreTabla = "Camioneros"
        tp.TablasRelacionadas = Me.CrearTablasRelacionadas()
        coltp.Add(tp)
        Return coltp
    End Function

    Private Function CrearTablasRelacionadas() As Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.ColTablaRelacionadaAIQB
        Dim coltr As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.ColTablaRelacionadaAIQB()

        Dim tr As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.TablaRelacionadaAIQB()
        tr.NombreTablaBD = "tlInformeCamiones"
        tr.NombreTabla = "Camiones"
        tr.fkPadre = "idCamion"
        tr.fkPropio = "id"
        tr.NombreRelacion = "Camión"

        coltr.Add(tr)

        Dim tr2 As New Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.TablaRelacionadaAIQB()
        tr2.NombreTablaBD = "tlInformeRutas"
        tr2.NombreTabla = "Rutas"
        tr2.fkPadre = "idRuta"
        tr2.fkPropio = "id"
        tr2.NombreRelacion = "Ruta"

        coltr.Add(tr2)

        Return coltr
    End Function

    Private Function CrearTipoPlantilla() As Framework.GestorInformes.ContenedorPlantilla.DN.TipoPlantilla
        Dim tp As New Framework.GestorInformes.ContenedorPlantilla.DN.TipoPlantilla()
        tp.Nombre = "TipoPlantilla1"
        Return tp
    End Function

    Private Function CrearContenedorPlantilla() As Framework.GestorInformes.ContenedorPlantilla.DN.ContenedorPlantillaDN
        Dim c As New Framework.GestorInformes.ContenedorPlantilla.DN.ContenedorPlantillaDN()
        c.TipoPlantilla = Me.CrearTipoPlantilla()
        c.HuellaFichero = Me.CrearHuellaPlantilla()
        Return c
    End Function

    Private Function CrearHuellaPlantilla() As Framework.GestorInformes.ContenedorPlantilla.DN.HuellaFicheroPlantillaDN
        Dim h As New Framework.GestorInformes.ContenedorPlantilla.DN.HuellaFicheroPlantillaDN()
        h.RutaFichero = "D:\Signum\Signum\PlantillaEsquema.docx"
        Return h
    End Function

End Class






Public Class GestorMapPersistenciaXXXXXXXXXXXXTest

    Inherits GestorMapPersistenciaCamposLN



    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub


    Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As Framework.TiposYReflexion.DN.InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        ' ficheros
        If pTipo Is GetType(Framework.GestorInformes.ContenedorPlantilla.DN.HuellaFicheroPlantillaDN) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mDatos"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If


        If pTipo Is GetType(Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mDatos"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If


        If pTipo Is GetType(MotorBusquedaDN.ICondicionDN) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(MotorBusquedaDN.CondicionDN)))
            alentidades.Add(New VinculoClaseDN(GetType(MotorBusquedaDN.CondicionCompuestaDN)))

            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If



        If pTipo Is GetType(Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.ITabla) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.TablaPrincipalAIQB)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.TablaRelacionadaAIQB)))

            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If

        If pTipo Is GetType(Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.TablaPrincipalAIQB) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mSQLDefinicion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mParametros"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mfkTabla"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If




        Return Nothing

    End Function
End Class



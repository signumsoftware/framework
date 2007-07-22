#Region "Importaciones"

Imports Framework.TiposYReflexion.DN
Imports System.Collections.Generic

#End Region

Namespace LN
    Public Class GestorMapPersistenciaCamposGilmarLN
        Inherits GestorMapPersistenciaCamposLN


        'TODO: ALEX por implementar: recuperar el mapeado de instanciacion de la fuente de datos para el tipo a procesar
        Public Overrides Function RecuperarMapPersistenciaCampos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
            Dim mapinst As New InfoDatosMapInstClaseDN
            Dim campodatos As InfoDatosMapInstCampoDN



            If (pTipo.FullName = "UsuariosDN.DatosIdentidadDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mClave"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

                Return mapinst
            End If

            If (pTipo.FullName = "GestionProductoDN.DatosMultimediaACPDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mGruposImagenes"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

                Return mapinst
            End If

            If (pTipo.FullName = "GestionDemandaDN.ProductoOfertadoDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "_TratamientoDemanda"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                Return mapinst
            End If

            If (pTipo.FullName = "GestionProductoDN.EntradaModificacionACPDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "_ACP"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.IInmuebleDN") Then
                Dim alentidades As New ArrayList

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.InmuebleBasicoDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Solar.SolarDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Garaje.GarajeDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Puelo.PU.Piso.PisoDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Puelo.PU.Unifamiliar.UnifamiliarDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Puelo.EL.Edificio.EdificioDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Puelo.EL.Local.LocalDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Puelo.EL.Nave.NaveDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Puelo.Oficina.OficinaDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.Puelo.EL.Nave.NaveDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mEl"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.Puelo.EL.Local.LocalDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mEl"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.Puelo.EL.Edificio.EdificioDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mEl"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mDistribucion"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

                Return mapinst
            End If


            If (pTipo.FullName = "InmueblesDN.Puelo.EL.ElDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mPuelo"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.Puelo.PU.Unifamiliar.UnifamiliarDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mPu"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.Puelo.PU.Piso.PisoDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mPu"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.Puelo.PU.PUDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mPuelo"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.Puelo.Oficina.OficinaDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mPuelo"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.Garaje.IGarajeComunDN") Then
                Dim alentidades As New ArrayList

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Garaje.GarajeComunDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.Garaje.IGarajeCortoDN") Then
                Dim alentidades As New ArrayList

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Garaje.GarajeCortoDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.Garaje.GarajeDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mGarajeComun"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.Garaje.GarajeCortoDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mGarajeComun"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.DireccionInmuebleDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mColEspecificacionesDireccion"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

                'TODO: HE AÑADIDO ESTA LINEA QUE NO ESTABA!!! (Vicente)
                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.OperacionesPermitidasDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mVenta"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mAlquiler"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.InmuebleBasicoDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mDatosRegistro"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mFormaVisita"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            If (pTipo.FullName = "UsuariosDN.PermisoDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "_Metodo"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.TiposInmuebles.TiposInmuebleNodoDN") Then
                Dim mapinstSub As New InfoDatosMapInstClaseDN
                Dim alentidades As New ArrayList

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mPadre"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "_Validador"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mVc"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.TiposInmuebles.SubTiposInmuebleNodoDN"))
                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mhijos"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
                campodatos.MapSubEntidad = mapinstSub

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.TiposInmuebles.TiposInmuebleArbolDN") Then
                Dim mapinstSub As New InfoDatosMapInstClaseDN
                Dim alentidades As New ArrayList

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mPadre"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "_Validador"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "_Datos"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.TiposInmuebles.TiposInmuebleNodoDN"))
                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mhijos"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
                campodatos.MapSubEntidad = mapinstSub

                Return mapinst
            End If

            If (pTipo.FullName = "InmueblesDN.TiposInmuebles.SubTiposInmuebleNodoDN") Then
                Dim mapinstSub As New InfoDatosMapInstClaseDN
                Dim alentidades As New ArrayList

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.TiposInmuebles.TiposInmuebleNodoDN"))
                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.TiposInmuebles.SubTiposInmuebleNodoDN"))
                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mPadre"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
                campodatos.MapSubEntidad = mapinstSub

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "_Validador"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

                mapinstSub = New InfoDatosMapInstClaseDN
                alentidades = New ArrayList

                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.TiposInmuebles.SubTiposInmuebleNodoDN"))
                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mhijos"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
                campodatos.MapSubEntidad = mapinstSub

                Return mapinst
            End If

            If (pTipo.FullName = "LocalizacionesDN.Direcciones.RangoViaDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mImpares"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mPares"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            If (pTipo.FullName = "ZonasDN.ZonaDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mPadre"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mhijos"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "_Validador"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                Return mapinst
            End If

            If (pTipo.FullName = "ZonasDN.ArbolZonasDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mhijos"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)  ' cambiar por serializar

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mPadre"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "_Validador"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

                Return mapinst
            End If

            If (pTipo.FullName = "PersonasDN.PersonaDN") Then
                Dim mapSubInst As New InfoDatosMapInstClaseDN

                mapSubInst.NombreCompleto = "LocalizacionesDN.NifDN"

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapSubInst
                campodatos.NombreCampo = "mID"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mDNI"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
                campodatos.MapSubEntidad = mapSubInst

                Return mapinst
            End If

            If (pTipo.FullName = "PersonasDN.ContactoDN") Then
                Dim mapSubInst As New InfoDatosMapInstClaseDN
                Dim alentidades As ArrayList = Nothing

                mapSubInst.NombreCompleto = "LocalizacionesDN.EmailDN"

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapSubInst
                campodatos.NombreCampo = "mID"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "_EmailDN"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            If (pTipo.FullName = "ClientesDN.EmpresaDN") Then
                Dim mapSubInst As New InfoDatosMapInstClaseDN

                mapSubInst.NombreCompleto = "LocalizacionesDN.CifDN"

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapSubInst
                campodatos.NombreCampo = "mID"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "_CifDN"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            If (pTipo.FullName = "ClientesDN.IClienteDN") Then
                Dim alentidades As New ArrayList

                alentidades.Add(New VinculoClaseDN("ClientesDN", "ClientesDN.ClienteDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                Return mapinst
            End If

            If (pTipo.FullName = "PersonasDN.IContactoDN") Then
                Dim alentidades As New ArrayList

                alentidades.Add(New VinculoClaseDN("PersonasDN", "PersonasDN.ContactoDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                Return mapinst
            End If

            If (pTipo.FullName = "EmpleadosDN.IAvisador") Then
                Dim alentidades As New ArrayList

                alentidades.Add(New VinculoClaseDN("EmpleadosDN", "EmpleadosDN.EmpleadoDN"))
                alentidades.Add(New VinculoClaseDN("EmpleadosDN", "EmpleadosDN.ColaboradorDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                Return mapinst
            End If

            'PRUEBAS!!!
            If (pTipo.FullName = "MotorADTest.IInterface") Then
                Dim alentidades As New ArrayList
                alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.B"))
                alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.C"))
                alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.D"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                Return mapinst
            End If

            If pTipo.FullName = "MotorADTest.A" Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "_CampoColMap"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
                campodatos.Datos.Add("MotorADTest.D")

                Return mapinst
            End If

            If pTipo.FullName = "MotorADTest.InterfacePrueba" Then
                Dim alentidades As New ArrayList
                alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.PruebaAI"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                Return mapinst
            End If

            If pTipo.FullName = "MotorADTest.PruebaF" Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mCRPruebaA"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

                Return mapinst
            End If

            Return Nothing
        End Function

        Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As TiposYReflexion.DN.InfoDatosMapInstClaseDN

        End Function
    End Class



    Public Class GestorMapPersistenciaCamposToyotaPGVDLN
        Inherits GestorMapPersistenciaCamposLN


        'TODO: ALEX por implementar: recuperar el mapeado de instanciacion de la fuente de datos para el tipo a procesar
        Public Overrides Function RecuperarMapPersistenciaCampos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
            Dim mapinst As InfoDatosMapInstClaseDN = Nothing
            Dim campodatos As InfoDatosMapInstCampoDN = Nothing

            mapinst = RecuperarMapPersistenciaCamposPrivado(pTipo)

            ' ojo esta modificación se debe aplicar siempre si el tipo hereda de una huella es decir en el metodo que lo llamo
            If (TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo)) Then
                If mapinst Is Nothing Then
                    mapinst = New InfoDatosMapInstClaseDN
                End If
                Me.MapearClase("mEntidadReferidaHuella", CampoAtributoDN.SoloGuardarYNoReferido, campodatos, mapinst)
                'campodatos = New InfoDatosMapInstCampoDN
                'campodatos.InfoDatosMapInstClase = mapinst
                'campodatos.NombreCampo = "mEntidadReferidaHuella"
                'campodatos.ColCampoAtributo.Add(CampoAtributoDN.SoloGuardarYNoReferido)
            End If

            Return mapinst
        End Function

        Private Sub MapearClase(ByVal pCampoAMapear As String, ByVal pFormatoDeMapeado As CampoAtributoDN, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN)

            pCampoDatos = New InfoDatosMapInstCampoDN
            pCampoDatos.InfoDatosMapInstClase = pMapInst
            pCampoDatos.NombreCampo = pCampoAMapear
            pCampoDatos.ColCampoAtributo.Add(pFormatoDeMapeado)

        End Sub

        Private Sub VincularConClase(ByVal mCampoAMapear As String, ByVal pElementosDeEmsamblado As List(Of ElementosDeEnsamblado), ByVal mFormatoDeMapeado As CampoAtributoDN, ByRef pAlEntidades As ArrayList, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN, ByRef pMapInstSub As InfoDatosMapInstClaseDN)

            pMapInstSub = New InfoDatosMapInstClaseDN
            pAlEntidades = New ArrayList

            Dim pElemento As ElementosDeEnsamblado
            For Each pElemento In pElementosDeEmsamblado
                pAlEntidades.Add(New VinculoClaseDN(pElemento.Ensamblado, pElemento.Clase))
            Next

            pMapInstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = pAlEntidades

            pCampoDatos = New InfoDatosMapInstCampoDN
            pCampoDatos.InfoDatosMapInstClase = pMapInst
            pCampoDatos.NombreCampo = mCampoAMapear
            pCampoDatos.ColCampoAtributo.Add(mFormatoDeMapeado)
            pCampoDatos.MapSubEntidad = pMapInstSub

        End Sub

        Private Sub VincularConClase(ByVal mCampoAMapear As String, ByVal pElementoDeEmsamblado As ElementosDeEnsamblado, ByVal mFormatoDeMapeado As CampoAtributoDN, ByRef pAlEntidades As ArrayList, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN, ByRef pMapInstSub As InfoDatosMapInstClaseDN)

            pMapInstSub = New InfoDatosMapInstClaseDN
            pAlEntidades = New ArrayList

            pAlEntidades.Add(New VinculoClaseDN(pElementoDeEmsamblado.Ensamblado, pElementoDeEmsamblado.Clase))
            pMapInstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = pAlEntidades

            pCampoDatos = New InfoDatosMapInstCampoDN
            pCampoDatos.InfoDatosMapInstClase = pMapInst
            pCampoDatos.NombreCampo = mCampoAMapear
            pCampoDatos.ColCampoAtributo.Add(mFormatoDeMapeado)
            pCampoDatos.MapSubEntidad = pMapInstSub

        End Sub
        Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
            Dim mapinst As New InfoDatosMapInstClaseDN
            Dim campodatos As InfoDatosMapInstCampoDN = Nothing

            ''Esto esta comentado, ya que en principio no nos interesaba tener una huella de departamento, 
            ''pero una vez que esto ya está implementado, nos interesa tener este elemento para poder
            ''tener una referencia desde el empleado a la empresa.
            'If (pTipo.FullName = "EmpresasDN.RolDepartamentoDN") Then
            '    campodatos = New InfoDatosMapInstCampoDN
            '    campodatos.InfoDatosMapInstClase = mapinst
            '    campodatos.NombreCampo = "mHDepartamentoDN"
            '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            '    Return mapinst
            'End If

            ''Esto esta comentado, ya que en principio no nos interesaba tener una huella de departamento, 
            ''pero una vez que esto ya está implementado, nos interesa tener este elemento para poder
            ''tener una referencia desde el empleado a la empresa.
            'If (pTipo.FullName = "EmpresasDN.SedeEmpresaDN") Then
            '    campodatos = New InfoDatosMapInstCampoDN
            '    campodatos.InfoDatosMapInstClase = mapinst
            '    campodatos.NombreCampo = "mHuellaAgrupacionDeEmpresasDN"
            '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            '    Return mapinst
            'End If
            '"EmpresaToyota.AdaptadorEvalConcesionarioDN"

            '----------------------------------------------------------------------------


            ' DOCUMENTOS -----------------------------------------------

            If (pTipo.FullName = "AuditoriasDN.ObservacionDocDN") Then
                MapearClase("mDatosFichero", CampoAtributoDN.PersistenciaContenidaSerializada, Nothing, mapinst)
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mFileInfo"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

                Dim alentidades As New ArrayList

                alentidades.Add(New VinculoClaseDN("AuditoriasDN", "AuditoriasDN.ObservacionDocHuellaCacheDN"))

                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ActualizarClasesHuella) = alentidades
                Return mapinst
            End If

            If (pTipo.FullName = "AuditoriasDN.DocEnBdDN") Then
                MapearClase("mDatosFichero", CampoAtributoDN.PersistenciaContenidaSerializada, Nothing, mapinst)
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mFileInfo"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

                Dim alentidades As New ArrayList

                alentidades.Add(New VinculoClaseDN("AuditoriasDN", "AuditoriasDN.DocHuellaCache"))

                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ActualizarClasesHuella) = alentidades
                Return mapinst
            End If

            If (pTipo.FullName = "AuditoriasDN.InformeVisitaDocDN") Then
                MapearClase("mDatosFichero", CampoAtributoDN.PersistenciaContenidaSerializada, Nothing, mapinst)
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mFileInfo"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)


                Dim alentidades As New ArrayList


                alentidades.Add(New VinculoClaseDN("AuditoriasDN", "AuditoriasDN.InformeVisitaDocHuellaCacheDN"))

                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ActualizarClasesHuella) = alentidades


                Return mapinst
            End If


            '-------------------------------------------------------------






            If (pTipo.FullName = "MotorADTest.ClaseHA") Then
                Dim alentidades As ArrayList
                alentidades = New ArrayList
                alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.ClaseHA2"))
                alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.ClaseHA"))

                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor) = alentidades

                Return mapinst
            End If


            '----------------------------------------------------------------------------




            'ZONA: AuditoriasDN ________________________________________________________________

            'Mapeado de TareaDN con padre otra TareaDN, hijos una colección de TareaDN
            'con hijos AdaptadorEvalEmpleadoDN
            'If (pTipo.FullName = "AuditoriasDN.ArbolDeConceptosAuditablesDN") Then
            '  Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
            '    Dim alentidades As ArrayList = Nothing

            '    MapearClase("mPadre", CampoAtributoDN.NoProcesar, Nothing, mapinst)
            '    VincularConClase("mHijos", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.CategoriaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
            '    MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
            '    MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

            '    Return mapinst

            'End If



            'If (pTipo.FullName = "AuditoriasDN.InformeVisitaDocDN") Then

            '    campodatos = New InfoDatosMapInstCampoDN
            '    campodatos.InfoDatosMapInstClase = mapinst
            '    campodatos.NombreCampo = "mFileInfo"
            '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

            '    Return mapinst

            'End If


            'If (pTipo.FullName = "AuditoriasDN.ObservacionDocDN") Then

            '    campodatos = New InfoDatosMapInstCampoDN
            '    campodatos.InfoDatosMapInstClase = mapinst
            '    campodatos.NombreCampo = "mFileInfo"
            '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

            '    Return mapinst

            'End If


            If (pTipo.FullName = "AuditoriasDN.CampañaDN") Then
                Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
                Dim alentidades As ArrayList = Nothing
                Dim lista As List(Of ElementosDeEnsamblado)

                MapearClase("mPeriodoValidezDeLaCampaña", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)

                lista = New List(Of ElementosDeEnsamblado)
                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.HuellaCahceAdaptadorEvalSedeEmpresaDN"))
                '                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"))
                VincularConClase("mColHuellaDestinatarioDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)

                Return mapinst

            End If



            'Mapeado de TareaDN con padre otra TareaDN, hijos una colección de TareaDN
            'con hijos AdaptadorEvalEmpleadoDN
            If (pTipo.FullName = "EmpresaToyotaDN.TareaGVDDN") Then
                Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
                Dim alentidades As ArrayList = Nothing
                Dim lista As List(Of ElementosDeEnsamblado)

                '   MapearClase("mDatosTemporalesDN", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)

                MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                MapearClase("mConLoQueSeHaceODDN", CampoAtributoDN.NoProcesar, Nothing, mapinst)
                MapearClase("mBeneficiarioDN", CampoAtributoDN.NoProcesar, Nothing, mapinst)
                MapearClase("mAccionVerboDN", CampoAtributoDN.PersistenciaContenidaSerializada, Nothing, mapinst)
                Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                MapearClase("mValidadorCambioEstado", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                'La TareaGVD puede tener como padre otra TareaGVD o una TareaResumenSujeto
                'VincularConClase("mPadre", New ElementosDeEnsamblado("EmpresaToyotaDN", "EmpresaToyotaDN.TareaGVDDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                lista = New List(Of ElementosDeEnsamblado)
                lista.Add(New ElementosDeEnsamblado("EmpresaToyotaDN", "EmpresaToyotaDN.TareaGVDDN"))
                lista.Add(New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.TareaResumenSujetoDN"))
                VincularConClase("mPadre", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)

                VincularConClase("mHijos", New ElementosDeEnsamblado("EmpresaToyotaDN", "EmpresaToyotaDN.TareaGVDDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                VincularConClase("mCausaTareaDN", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.CausaObservacionDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                VincularConClase("mPlanificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
                '  VincularConClase("mSujetoDN", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
                VincularConClase("mSupervisorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
                VincularConClase("mVerificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)

                lista = New List(Of ElementosDeEnsamblado)
                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"))
                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"))
                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN"))
                VincularConClase("mSujetoDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)


                lista = New List(Of ElementosDeEnsamblado)
                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"))
                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"))
                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN"))
                VincularConClase("mSobreQueSeHaceOIDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
                '  VincularConClase("mSujetoDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)

                Return mapinst

            End If

            If (pTipo.FullName = "AuditoriasDN.TareaResumenSujetoDN") Then
                Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
                Dim alentidades As ArrayList = Nothing
                Dim lista As List(Of ElementosDeEnsamblado)

                '    MapearClase("mDatosTemporalesDN", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                MapearClase("mConLoQueSeHaceODDN", CampoAtributoDN.NoProcesar, Nothing, mapinst)
                MapearClase("mBeneficiarioDN", CampoAtributoDN.NoProcesar, Nothing, mapinst)
                MapearClase("mAccionVerboDN", CampoAtributoDN.PersistenciaContenidaSerializada, Nothing, mapinst)
                Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                MapearClase("mCausaTareaDN", CampoAtributoDN.NoProcesar, Nothing, mapinst)
                MapearClase("mValidadorCambioEstado", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                VincularConClase("mPadre", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.TareaResumenSujetoDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                'VincularConClase("mHijos", New ElementosDeEnsamblado("EmpresaToyotaDN", "EmpresaToyotaDN.TareaGVDDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                'VincularConClase("mCausaTareaDN", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.CausaObservacionDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                VincularConClase("mPlanificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
                '  VincularConClase("mSujetoDN", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
                VincularConClase("mSupervisorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
                VincularConClase("mVerificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)



                lista = New List(Of ElementosDeEnsamblado)
                lista.Add(New ElementosDeEnsamblado("EmpresaToyotaDN", "EmpresaToyotaDN.TareaGVDDN"))
                lista.Add(New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.TareaResumenSujetoDN"))
                VincularConClase("mHijos", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)


                lista = New List(Of ElementosDeEnsamblado)
                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"))
                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"))
                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN"))
                VincularConClase("mSobreQueSeHaceOIDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
                VincularConClase("mSujetoDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)


                Return mapinst

            End If

            'Mapeado de TareaDN con padre otra TareaDN, hijos una colección de TareaDN
            'con hijos AdaptadorEvalEmpleadoDN
            If (pTipo.FullName = "AuditoriasDN.TareaDN") Then
                Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
                Dim alentidades As ArrayList = Nothing
                Dim lista As List(Of ElementosDeEnsamblado)
                '    MapearClase("mDatosTemporalesDN", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)

                MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                MapearClase("mConLoQueSeHaceODDN", CampoAtributoDN.NoProcesar, Nothing, mapinst)
                MapearClase("mBeneficiarioDN", CampoAtributoDN.NoProcesar, Nothing, mapinst)
                MapearClase("mAccionVerboDN", CampoAtributoDN.PersistenciaContenidaSerializada, Nothing, mapinst)
                Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                MapearClase("mValidadorCambioEstado", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                VincularConClase("mPadre", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.TareaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                VincularConClase("mHijos", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.TareaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                VincularConClase("mCausaTareaDN", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.CausaObservacionDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                VincularConClase("mPlanificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
                VincularConClase("mSujetoDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
                VincularConClase("mSupervisorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)
                VincularConClase("mVerificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)

                lista = New List(Of ElementosDeEnsamblado)
                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"))
                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"))
                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN"))
                VincularConClase("mSobreQueSeHaceOIDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)



                alentidades = New ArrayList
                alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.TareaGVDDN"))
                alentidades.Add(New VinculoClaseDN("AuditoriasDN", "AuditoriasDN.TareaDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ClaseHeredadaPor) = alentidades


                Return mapinst

            End If




            'If (pTipo.FullName = "TareasDN.DatosTemporalesDN") Then
            '    MapearClase("mPeriodoPlanificado", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
            '    MapearClase("mPeriodoReal", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
            '    Return mapinst
            'End If

            If (pTipo.FullName = "LocalizacionesDN.Temporales.IntervaloFechasSubordinadoDN") Then
                MapearClase("mIntervaloFechas", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                Return mapinst
            End If







            'Mapeado del IEvaluableDN, donde la interfaz se mapea para todas las clases, y habrá que 
            'decir que clases implementan esta interfaz
            If (pTipo.FullName = "AuditoriasDN.IEvaluableDN") Then
                Dim alentidades As New ArrayList
                alentidades.Add(New VinculoClaseDN("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"))
                alentidades.Add(New VinculoClaseDN("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN"))
                alentidades.Add(New VinculoClaseDN("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
                Return mapinst
            End If

            ''esto hay que quitarlo luego, pero es para test ObservacionesDNTest
            If (pTipo.FullName = "AuditoriasDN.ObservacionDN") Then

                Me.MapearClase("mNota", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)

                Return mapinst
            End If

            'TODO: Revisar el mapeado para la visita -> GrupoVisitante_____________________
            If pTipo.FullName = "AuditoriasDN.VisitaDN" Then
                Dim mapinstSub As New InfoDatosMapInstClaseDN
                Dim alentidades As New ArrayList

                'campodatos = New InfoDatosMapInstCampoDN
                'campodatos.InfoDatosMapInstClase = mapinst
                Me.VincularConClase("mPlanificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                Me.VincularConClase("mSujetoDN", New ElementosDeEnsamblado("EmpresaToyotaDN", "EmpresaToyotaDN.GrupoVisitanteDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                Me.VincularConClase("mSupervisorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                VincularConClase("mVerificadorDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                'Me.VincularConClase("mCausaTareaDN", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.ObservacionDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                Me.MapearClase("mCausaTareaDN", CampoAtributoDN.NoProcesar, campodatos, mapinst)
                Me.MapearClase("mBeneficiarioDN", CampoAtributoDN.NoProcesar, campodatos, mapinst)
                Me.MapearClase("mAccionVerboDN", CampoAtributoDN.NoProcesar, campodatos, mapinst)
                Me.MapearClase("mConLoQueSeHaceODDN", CampoAtributoDN.NoProcesar, campodatos, mapinst)
                Me.MapearClase("mHijos", CampoAtributoDN.NoProcesar, campodatos, mapinst)
                Me.MapearClase("mPadre", CampoAtributoDN.NoProcesar, campodatos, mapinst)
                Me.MapearClase("mValidadorp", CampoAtributoDN.NoProcesar, campodatos, mapinst)
                Me.MapearClase("mValidadorh", CampoAtributoDN.NoProcesar, campodatos, mapinst)
                MapearClase("mValidadorCambioEstado", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                '  Me.VincularConClase("mQuienHaceVisita", New ElementosDeEnsamblado("EmpresaToyotaDN", "EmpresaToyotaDN.GrupoVisitanteDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                '  Me.VincularConClase("mEntidadVisitada", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                Dim lista As List(Of ElementosDeEnsamblado)

                lista = New List(Of ElementosDeEnsamblado)
                lista.Add(New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"))
                VincularConClase("mSobreQueSeHaceOIDN", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)


                Return mapinst

            End If

            'Mapeado para el árbol de conceptos auditables
            If (pTipo.FullName = "AuditoriasDN.ArbolDeConceptosAuditablesDN") Then
                Dim mapinstSub As New InfoDatosMapInstClaseDN
                Dim alentidades As New ArrayList

                'Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                'Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                Me.VincularConClase("mValidadorh", New ElementosDeEnsamblado(Nothing, Nothing), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)
                Me.VincularConClase("mValidadorp", New ElementosDeEnsamblado(Nothing, Nothing), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)

                Me.VincularConClase("mHijos", New ElementosDeEnsamblado(Nothing, Nothing), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)

                ' Me.VincularConClase("mHijos", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                Me.VincularConClase("mPadre", New ElementosDeEnsamblado(Nothing, Nothing), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)

                Return mapinst
            End If

            'Mapeado de la clase CategoriaDN, donde indicamos como se mapean los distintos campos de la clase
            If (pTipo.FullName = "AuditoriasDN.CategoriaDN") Then
                Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
                Dim alentidades As ArrayList = Nothing

                Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                Me.VincularConClase("mHijos", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.CategoriaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                Me.VincularConClase("mPadre", New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.CategoriaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

                'Dim lista As List(Of ElementosDeEnsamblado)
                'lista = New List(Of ElementosDeEnsamblado)
                'lista.Add(New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.ArbolDeConceptosAuditablesDN"))
                'lista.Add(New ElementosDeEnsamblado("AuditoriasDN", "AuditoriasDN.CategoriaDN"))
                'VincularConClase("mPadre", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)


                Return mapinst
            End If

            'FINZONA: AuditoriasDN ________________________________________________________________


            'ZONA: EmpresaToyotaDN ________________________________________________________________

            '******************************************************************************************
            'NOTA IMPORTANTE PARA EL ORDEN DE CREACIÓN DE TABLAS
            'Cuando se tengan que crear estas tablas, se crean primero hacia arriba, esto es, se crean
            'mapeando los hijos como no procesados, y solo se procesan los padres, y luego, se mapean con todo
            'y se vuelven a crear las tablas
            '******************************************************************************************

            'Mapeado para el concesionario con hijos AdaptadorEvalPuntoVentaDN
            If (pTipo.FullName = "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN") Then
                Dim mapinstSub As New InfoDatosMapInstClaseDN
                Dim alentidades As New ArrayList

                Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                Me.MapearClase("mCriticidadDeLaEmpresa", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                'Me.VincularConClase("mHijos", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)

                Me.VincularConClase("mHijos", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                Me.VincularConClase("mPadre", New ElementosDeEnsamblado(Nothing, Nothing), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)

                Return mapinst
            End If

            'Mapeado de empleados con padre AdaptadorEvalPuntoVentaDN
            If (pTipo.FullName = "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN") Then
                Dim mapinstSub As New InfoDatosMapInstClaseDN
                Dim alentidades As New ArrayList

                Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                Me.VincularConClase("mHijos", New ElementosDeEnsamblado(Nothing, Nothing), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)
                Me.VincularConClase("mPadre", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

                Return mapinst
            End If

            'Mapeado de puntos de venta con padre AdaptadorEvalConcesionarioDN
            'con hijos AdaptadorEvalEmpleadoDN
            If (pTipo.FullName = "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalSedeEmpresaDN") Then
                Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
                Dim alentidades As ArrayList = Nothing

                Me.MapearClase("mCriticidad", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                ' Me.VincularConClase("mHijos", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN"), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)

                Me.VincularConClase("mHijos", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                Me.VincularConClase("mPadre", New ElementosDeEnsamblado("AdaptadoresAuditoriasDNEmpresasDN", "AdaptadoresAuditoriasDNEmpresasDN.AdaptadorEvalEmpresaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                Return mapinst
            End If

            'Mapeado del DelegadoTerritorialDN, donde la clase mapea sus interfaces, y solo es para ella.
            'En cualquier otro clase que tenga la misma base, habrá que mapearla para esa.
            If (pTipo.FullName = "EmpresaToyotaDN.JefeDelegadoTerritorialDN") Then
                Me.MapearClase("mPeriodo", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                Return mapinst
            End If
            If (pTipo.FullName = "EmpresaToyotaDN.DelegadoTerritorialDN") Then
                Me.MapearClase("mPeriodo", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                Return mapinst
            End If
            If (pTipo.FullName = "EmpresaToyotaDN.SecretariaVentasDN") Then
                Me.MapearClase("mPeriodo", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                Return mapinst
            End If

            'FINZONA: EmpresaToyotaDN ________________________________________________________________

            'ZONA: UsuarioDN ________________________________________________________________

            'Mapeado de UsuarioDN, donde la clase mapea sus interfaces, y solo es para ella.
            'En cualquier otro clase que tenga la misma base, habrá que mapearla para esa.
            If (pTipo.FullName = "UsuariosDN.UsuarioDN") Then
                Dim mapinstSub As New InfoDatosMapInstClaseDN
                Dim alentidades As New ArrayList

                Me.VincularConClase("mHuellaEntidadUserDN", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.HuellaCacheEmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

                Return mapinst
            End If

            If (pTipo.FullName = "UsuariosDN.DatosIdentidadDN") Then

                Me.MapearClase("mHashClave", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst
            End If


            'ZONA: EmpresasDN ________________________________________________________________

            'Mapeado del IResponsableDN, donde la interfaz se mapea para todas las clases, y habrá que 
            'decir que clases implementan esta interfaz
            If (pTipo.FullName = "EmpresasDN.IResponsableDN") Then
                Dim alentidades As New ArrayList
                alentidades.Add(New VinculoClaseDN("EmpresasDN", "EmpresasDN.ResponsableAgrupacionDeEmpresasDN"))
                alentidades.Add(New VinculoClaseDN("EmpresasDN", "EmpresasDN.ResponsableDePersonalDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
                Return mapinst
            End If

            If (pTipo.FullName = "EmpresasDN.IEmpresaDN") Then
                Dim alentidades As New ArrayList
                alentidades.Add(New VinculoClaseDN("EmpresasDN", "EmpresasDN.EmpresaDN"))
                alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.ConcesionarioDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
                Return mapinst
            End If

            If (pTipo.FullName = "EmpresasDN.EmpleadoDN") Then
                Me.MapearClase("mPeriodo", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                Return mapinst
            End If

            If (pTipo.FullName = "EmpresasDN.ResponsableAgrupacionDeEmpresasDN") Then
                Dim mapinstSub As New InfoDatosMapInstClaseDN
                Dim alentidades As New ArrayList

                Me.VincularConClase("mEntidadResponsable", New ElementosDeEnsamblado("EmpresasDN", "EmpresasDN.EmpleadoDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                Me.MapearClase("mPeriodo", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)

                Return mapinst
            End If

            If (pTipo.FullName = "EmpresasDN.ResponsableDePersonalDN") Then
                Me.MapearClase("mPeriodo", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                Return mapinst
            End If
            If (pTipo.FullName = "EmpresasDN.RolDepartamentoDN") Then
                Me.MapearClase("mPeriodo", CampoAtributoDN.PersistenciaContenida, campodatos, mapinst)
                Return mapinst
            End If

            'FINZONA: EmpresasDN ________________________________________________________________

            'ZONA: LocalizacionesDN ________________________________________________________________

            If (pTipo.FullName = "LocalizacionesDN.ZonaDN") Then
                Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
                Dim alentidades As ArrayList = Nothing

                Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                Me.VincularConClase("mHijos", New ElementosDeEnsamblado("LocalizacionesDN", "LocalizacionesDN.ZonaDN"), CampoAtributoDN.NoProcesar, alentidades, campodatos, mapinst, mapinstSub)
                Me.VincularConClase("mPadre", New ElementosDeEnsamblado("LocalizacionesDN", "LocalizacionesDN.ZonaDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

                Return mapinst
            End If

            If (pTipo.FullName = "LocalizacionesDN.IContactoElementoDN") Then
                Dim alentidades As New ArrayList
                alentidades.Add(New VinculoClaseDN("LocalizacionesDN", "LocalizacionesDN.ContactoElementoDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
                Return mapinst
            End If

            If (pTipo.FullName = "LocalizacionesDN.ContactoDN") Then
                Dim mapinstSub As InfoDatosMapInstClaseDN = Nothing
                Dim alentidades As ArrayList = Nothing

                Me.MapearClase("mColRelacionEspecificables", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst
            End If

            If (pTipo.FullName = "LocalizacionesDN.Temporales.IntervaloFechasDN") Then

                Me.MapearClase("mFechaModificacion", CampoAtributoDN.NoProcesar, campodatos, mapinst)
                Me.MapearClase("mBaja", CampoAtributoDN.NoProcesar, campodatos, mapinst)
                Me.MapearClase("mNombre", CampoAtributoDN.NoProcesar, campodatos, mapinst)

                Return mapinst
            End If

            'FINZONA: LocalizacionesDN ________________________________________________________________

            'ZONA: PersonasDN ________________________________________________________________

            If pTipo.FullName = "PersonasDN.PersonaDN" Then
                Dim mapSubInst As New InfoDatosMapInstClaseDN
                Dim alentidades As ArrayList = Nothing

                ' mapeado de la clase referida por el campo
                mapSubInst.NombreCompleto = "LocalizacionesDN.NifDN"

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapSubInst
                campodatos.NombreCampo = "mID"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapSubInst
                campodatos.NombreCampo = "mFechaModificacion"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapSubInst
                campodatos.NombreCampo = "mBaja"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapSubInst
                campodatos.NombreCampo = "mNombre"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

                ' FIN    mapeado de la clase referida por el campo ******************

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mNIF"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

                campodatos.MapSubEntidad = mapSubInst

                Return mapinst
            End If

            'FINZONA: PersonasDN ________________________________________________________________

            'ZONA: Framework.DatosNegocio ________________________________________________________________
            If (pTipo.FullName = "Framework.DatosNegocio.Arboles.NodoBaseDN") Then

                Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst

            End If

            If (pTipo.FullName = "Framework.DatosNegocio.Arboles.ColNodosDN") Then

                Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst

            End If

            If (pTipo.FullName = "DatosNegocio.ArrayListValidable") Then

                Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst

            End If

            'FINZONA: Framework.DatosNegocio ________________________________________________________________


            'ZONA: EmpresaDN ________________________________________________________________

            If pTipo.FullName = "EmpresasDN.EmpresaDN" Then
                Dim mapSubInst As New InfoDatosMapInstClaseDN

                ' mapeado de la clase referida por el campo
                mapSubInst.NombreCompleto = "LocalizacionesDN.CifDN"

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapSubInst
                campodatos.NombreCampo = "mID"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapSubInst
                campodatos.NombreCampo = "mFechaModificacion"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapSubInst
                campodatos.NombreCampo = "mBaja"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapSubInst
                campodatos.NombreCampo = "mNombre"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


                ' FIN    mapeado de la clase referida por el campo ******************

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mCif"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

                campodatos.MapSubEntidad = mapSubInst

                Return mapinst
            End If





            If pTipo.FullName = "EmpresaToyotaDN.ConcesionarioDN" Then

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mIDLocal"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

                Return mapinst
            End If

            If pTipo.FullName = "EmpresasDN.AgrupacionDeEmpresasDN" Then

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mCodigo"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

                Return mapinst
            End If

            'FINZONA: EmpresaDN ________________________________________________________________




            'If (pTipo.FullName = "MotorADTest.pruebaD") Then
            '    Dim alentidades As New ArrayList

            '    alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.PruebaDHC"))
            '    alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.PruebaDHuellaCacheDN"))
            '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ActualizarClasesHuella) = alentidades
            '    Return mapinst
            'End If


            ' ojo esta modificación se debe aplicar siempre si el tipo hereda de una huella es decir en el metodo que lo llamo
            If (TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo)) Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mValidador"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)
            End If


            Return Nothing
        End Function


        Protected Overrides Sub Finalize()
            MyBase.Finalize()
        End Sub


    End Class



    Public Class GestorMapPersistenciaCamposToyotaPORLN
        Inherits GestorMapPersistenciaCamposLN


        'TODO: ALEX por implementar: recuperar el mapeado de instanciacion de la fuente de datos para el tipo a procesar
        Public Overrides Function RecuperarMapPersistenciaCampos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
            Dim mapinst As InfoDatosMapInstClaseDN = Nothing
            Dim campodatos As InfoDatosMapInstCampoDN = Nothing

            mapinst = RecuperarMapPersistenciaCamposPrivado(pTipo)

            ' ojo esta modificación se debe aplicar siempre si el tipo hereda de una huella es decir en el metodo que lo llamo
            If (TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo)) Then
                If mapinst Is Nothing Then
                    mapinst = New InfoDatosMapInstClaseDN
                End If
                Me.MapearClase("mEntidadReferidaHuella", CampoAtributoDN.SoloGuardarYNoReferido, campodatos, mapinst)
                'campodatos = New InfoDatosMapInstCampoDN
                'campodatos.InfoDatosMapInstClase = mapinst
                'campodatos.NombreCampo = "mEntidadReferidaHuella"
                'campodatos.ColCampoAtributo.Add(CampoAtributoDN.SoloGuardarYNoReferido)
            End If

            Return mapinst
        End Function


        Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
            Dim mapinst As New InfoDatosMapInstClaseDN
            Dim campodatos As InfoDatosMapInstCampoDN = Nothing

            'ZONA: UsuarioDN ________________________________________________________________

            'Mapeado de UsuarioDN, donde la clase mapea sus interfaces, y solo es para ella.

            If (pTipo.FullName = "UsuariosDN.DatosIdentidadDN") Then

                Me.MapearClase("mHashClave", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst
            End If

            'FINZONA: UsuarioDN ________________________________________________________________

            'ZONA: Framework.DatosNegocio ________________________________________________________________
            If (pTipo.FullName = "Framework.DatosNegocio.Arboles.NodoBaseDN") Then

                Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst

            End If

            If (pTipo.FullName = "Framework.DatosNegocio.Arboles.ColNodosDN") Then

                Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst

            End If

            If (pTipo.FullName = "DatosNegocio.ArrayListValidable") Then

                Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst

            End If

            'FINZONA: Framework.DatosNegocio ________________________________________________________________

            'ZONA: OrdenesReparacionDN ________________________________________________________________

            If (pTipo.FullName = "OrdenesReparacionDN.KitDN") Then
                Dim alentidades As New ArrayList

                alentidades.Add(New VinculoClaseDN("OrdenesReparacionDN", "OrdenesReparacionDN.HuellaKitDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ActualizarClasesHuella) = alentidades
                Return mapinst
            End If

            If (pTipo.FullName = "OrdenesReparacionDN.OPKDN") Then
                Dim alentidades As New ArrayList

                alentidades.Add(New VinculoClaseDN("OrdenesReparacionDN", "OrdenesReparacionDN.HuellaCacheOPKDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ActualizarClasesHuella) = alentidades
                Return mapinst
            End If

            If (pTipo.FullName = "OrdenesReparacionDN.OperacionDN") Then
                Dim alentidades As New ArrayList

                alentidades.Add(New VinculoClaseDN("OrdenesReparacionDN", "OrdenesReparacionDN.HuellaOperacionDN"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ActualizarClasesHuella) = alentidades
                Return mapinst
            End If

            Return mapinst

            'FINZONA: OrdenesReparacionDN ________________________________________________________________
        End Function

        Private Sub MapearClase(ByVal pCampoAMapear As String, ByVal pFormatoDeMapeado As CampoAtributoDN, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN)

            pCampoDatos = New InfoDatosMapInstCampoDN
            pCampoDatos.InfoDatosMapInstClase = pMapInst
            pCampoDatos.NombreCampo = pCampoAMapear
            pCampoDatos.ColCampoAtributo.Add(pFormatoDeMapeado)

        End Sub

        Private Sub VincularConClase(ByVal mCampoAMapear As String, ByVal pElementosDeEmsamblado As List(Of ElementosDeEnsamblado), ByVal mFormatoDeMapeado As CampoAtributoDN, ByRef pAlEntidades As ArrayList, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN, ByRef pMapInstSub As InfoDatosMapInstClaseDN)

            pMapInstSub = New InfoDatosMapInstClaseDN
            pAlEntidades = New ArrayList

            Dim pElemento As ElementosDeEnsamblado
            For Each pElemento In pElementosDeEmsamblado
                pAlEntidades.Add(New VinculoClaseDN(pElemento.Ensamblado, pElemento.Clase))
            Next

            pMapInstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = pAlEntidades

            pCampoDatos = New InfoDatosMapInstCampoDN
            pCampoDatos.InfoDatosMapInstClase = pMapInst
            pCampoDatos.NombreCampo = mCampoAMapear
            pCampoDatos.ColCampoAtributo.Add(mFormatoDeMapeado)
            pCampoDatos.MapSubEntidad = pMapInstSub

        End Sub

        Private Sub VincularConClase(ByVal mCampoAMapear As String, ByVal pElementoDeEmsamblado As ElementosDeEnsamblado, ByVal mFormatoDeMapeado As CampoAtributoDN, ByRef pAlEntidades As ArrayList, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN, ByRef pMapInstSub As InfoDatosMapInstClaseDN)

            pMapInstSub = New InfoDatosMapInstClaseDN
            pAlEntidades = New ArrayList

            pAlEntidades.Add(New VinculoClaseDN(pElementoDeEmsamblado.Ensamblado, pElementoDeEmsamblado.Clase))
            pMapInstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = pAlEntidades

            pCampoDatos = New InfoDatosMapInstCampoDN
            pCampoDatos.InfoDatosMapInstClase = pMapInst
            pCampoDatos.NombreCampo = mCampoAMapear
            pCampoDatos.ColCampoAtributo.Add(mFormatoDeMapeado)
            pCampoDatos.MapSubEntidad = pMapInstSub

        End Sub

    
    End Class


    Public Class GestorMapPersistenciaCamposMargaritaLN
        Inherits GestorMapPersistenciaCamposLN


        'TODO: ALEX por implementar: recuperar el mapeado de instanciacion de la fuente de datos para el tipo a procesar
        Public Overrides Function RecuperarMapPersistenciaCampos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
            Dim mapinst As InfoDatosMapInstClaseDN = Nothing
            Dim campodatos As InfoDatosMapInstCampoDN = Nothing

            mapinst = RecuperarMapPersistenciaCamposPrivado(pTipo)

            ' ojo esta modificación se debe aplicar siempre si el tipo hereda de una huella es decir en el metodo que lo llamo
            If (TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo)) Then
                If mapinst Is Nothing Then
                    mapinst = New InfoDatosMapInstClaseDN
                End If
                Me.MapearClase("mEntidadReferidaHuella", CampoAtributoDN.SoloGuardarYNoReferido, campodatos, mapinst)
            End If

            Return mapinst
        End Function


        Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
            Dim mapinst As New InfoDatosMapInstClaseDN
            Dim campodatos As InfoDatosMapInstCampoDN = Nothing

            'ZONA: UsuarioDN ________________________________________________________________

            'Mapeado de UsuarioDN, donde la clase mapea sus interfaces, y solo es para ella.

            If (pTipo.FullName = "UsuariosDN.DatosIdentidadDN") Then

                Me.MapearClase("mHashClave", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst
            End If

            If (pTipo.FullName = "UsuariosDN.UsuarioDN") Then
                Dim mapinstSub As New InfoDatosMapInstClaseDN
                Dim alentidades As New ArrayList

                'Hay que mapear esta entidad con la huella del usuario de MargaritaDN
                Me.VincularConClase("mHuellaEntidadUserDN", New ElementosDeEnsamblado("MargaritaDN", "Margarita.DN.HuellaUserDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

                Return mapinst
            End If

            'FINZONA: UsuarioDN ________________________________________________________________

            'ZONA: Framework.DatosNegocio ________________________________________________________________
            If (pTipo.FullName = "Framework.DatosNegocio.Arboles.NodoBaseDN") Then

                Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst

            End If

            If (pTipo.FullName = "Framework.DatosNegocio.Arboles.ColNodosDN") Then

                Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst

            End If

            If (pTipo.FullName = "DatosNegocio.ArrayListValidable") Then

                Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst

            End If

            'FINZONA: Framework.DatosNegocio ________________________________________________________________

            'ZONA: MargaritaDN_______________________________________________________________________________

            If (pTipo.FullName = "Margarita.DN.Mensajeria.SobreDN") Then
                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mMensaje"
                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

                campodatos = New InfoDatosMapInstCampoDN
                campodatos.InfoDatosMapInstClase = mapinst
                campodatos.NombreCampo = "mXmlMensaje"
                campodatos.TamañoCampo = 2000

                Return mapinst
            End If
            Return mapinst
            'FNZONA: MargaritaDN____________________________________________________________________________

        End Function

        Private Sub MapearClase(ByVal pCampoAMapear As String, ByVal pFormatoDeMapeado As CampoAtributoDN, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN)

            pCampoDatos = New InfoDatosMapInstCampoDN
            pCampoDatos.InfoDatosMapInstClase = pMapInst
            pCampoDatos.NombreCampo = pCampoAMapear
            pCampoDatos.ColCampoAtributo.Add(pFormatoDeMapeado)

        End Sub

        Private Sub VincularConClase(ByVal mCampoAMapear As String, ByVal pElementosDeEmsamblado As List(Of ElementosDeEnsamblado), ByVal mFormatoDeMapeado As CampoAtributoDN, ByRef pAlEntidades As ArrayList, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN, ByRef pMapInstSub As InfoDatosMapInstClaseDN)

            pMapInstSub = New InfoDatosMapInstClaseDN
            pAlEntidades = New ArrayList

            Dim pElemento As ElementosDeEnsamblado
            For Each pElemento In pElementosDeEmsamblado
                pAlEntidades.Add(New VinculoClaseDN(pElemento.Ensamblado, pElemento.Clase))
            Next

            pMapInstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = pAlEntidades

            pCampoDatos = New InfoDatosMapInstCampoDN
            pCampoDatos.InfoDatosMapInstClase = pMapInst
            pCampoDatos.NombreCampo = mCampoAMapear
            pCampoDatos.ColCampoAtributo.Add(mFormatoDeMapeado)
            pCampoDatos.MapSubEntidad = pMapInstSub

        End Sub

        Private Sub VincularConClase(ByVal mCampoAMapear As String, ByVal pElementoDeEmsamblado As ElementosDeEnsamblado, ByVal mFormatoDeMapeado As CampoAtributoDN, ByRef pAlEntidades As ArrayList, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN, ByRef pMapInstSub As InfoDatosMapInstClaseDN)

            pMapInstSub = New InfoDatosMapInstClaseDN
            pAlEntidades = New ArrayList

            pAlEntidades.Add(New VinculoClaseDN(pElementoDeEmsamblado.Ensamblado, pElementoDeEmsamblado.Clase))
            pMapInstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = pAlEntidades

            pCampoDatos = New InfoDatosMapInstCampoDN
            pCampoDatos.InfoDatosMapInstClase = pMapInst
            pCampoDatos.NombreCampo = mCampoAMapear
            pCampoDatos.ColCampoAtributo.Add(mFormatoDeMapeado)
            pCampoDatos.MapSubEntidad = pMapInstSub

        End Sub


    End Class


    Public Class ElementosDeEnsamblado

#Region "Atributos"
        Private mEnsamblado As String
        Private mClase As String
#End Region

#Region "Constructores"
        Public Sub New()

        End Sub

        Public Sub New(ByVal pEnsamblado As String, ByVal pClase As String)
            mEnsamblado = pEnsamblado
            mClase = pClase
        End Sub
#End Region

#Region "Propiedades"

        Public Property Ensamblado() As String
            Get
                Return mEnsamblado
            End Get
            Set(ByVal value As String)
                mEnsamblado = value
            End Set
        End Property

        Public Property Clase() As String
            Get
                Return mClase
            End Get
            Set(ByVal value As String)
                mClase = value
            End Set
        End Property

#End Region

    End Class


    Public Class GestorMapPersistenciaPruebasMotorADLN
        Inherits GestorMapPersistenciaCamposLN

        'TODO: ALEX por implementar: recuperar el mapeado de instanciacion de la fuente de datos para el tipo a procesar
        Public Overrides Function RecuperarMapPersistenciaCampos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
            Dim mapinst As InfoDatosMapInstClaseDN = Nothing
            Dim campodatos As InfoDatosMapInstCampoDN = Nothing

            mapinst = RecuperarMapPersistenciaCamposPrivado(pTipo)

            ' ojo esta modificación se debe aplicar siempre si el tipo hereda de una huella es decir en el metodo que lo llamo
            If (TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo)) Then
                If mapinst Is Nothing Then
                    mapinst = New InfoDatosMapInstClaseDN
                End If
                Me.MapearClase("mEntidadReferidaHuella", CampoAtributoDN.SoloGuardarYNoReferido, campodatos, mapinst)
            End If

            Return mapinst
        End Function


        Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
            Dim mapinst As New InfoDatosMapInstClaseDN
            Dim campodatos As InfoDatosMapInstCampoDN = Nothing

            'ZONA: UsuarioDN ________________________________________________________________

            'Mapeado de UsuarioDN, donde la clase mapea sus interfaces, y solo es para ella.

            If (pTipo.FullName = "UsuariosDN.DatosIdentidadDN") Then

                Me.MapearClase("mHashClave", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst
            End If

            If (pTipo.FullName = "UsuariosDN.UsuarioDN") Then
                Dim mapinstSub As New InfoDatosMapInstClaseDN
                Dim alentidades As New ArrayList

                'Hay que mapear esta entidad con la huella del usuario de MargaritaDN
                Me.VincularConClase("mHuellaEntidadUserDN", New ElementosDeEnsamblado("AmvDocumentosDN", "AmvDocumentosDN.HuellaOperadorDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

                Return mapinst
            End If

            'FINZONA: UsuarioDN ________________________________________________________________

            'ZONA: Framework.DatosNegocio ________________________________________________________________
            If (pTipo.FullName = "Framework.DatosNegocio.Arboles.NodoBaseDN") Then

                Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst

            End If

            If (pTipo.FullName = "Framework.DatosNegocio.Arboles.ColNodosDN") Then

                Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst

            End If

            If (pTipo.FullName = "DatosNegocio.ArrayListValidable") Then

                Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst

            End If

            'FINZONA: Framework.DatosNegocio ________________________________________________________________

            'ZONA: mmmmm _______________________________________________________________________________




            If (pTipo.FullName.Contains("Framework.DatosNegocio.Arboles.INodoTDN`1[[DatosNegocioTest.HojaDeNodoDeT")) Then
                Dim alentidades As New ArrayList

                alentidades.Add(New VinculoClaseDN("DatosNegocioTest", "DatosNegocioTest.NodoDeT"))
                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

                Return mapinst
            End If



            If (pTipo.FullName.Contains("DatosNegocioTest.NodoDeT")) Then

                Dim mapinstSub As New InfoDatosMapInstClaseDN
                Dim alentidades As New ArrayList

                Dim lista As List(Of ElementosDeEnsamblado)
                lista = New List(Of ElementosDeEnsamblado)

                lista.Add(New ElementosDeEnsamblado("DatosNegocioTest", "DatosNegocioTest.NodoDeT"))
                VincularConClase("mPadre", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)

                VincularConClase("mHijos", New ElementosDeEnsamblado("DatosNegocioTest", "DatosNegocioTest.NodoDeT"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
                Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
                Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

                Return mapinst
            End If


            Return mapinst

            'FNZONA: AMVGDocs____________________________________________________________________________

        End Function

        Private Sub MapearClase(ByVal pCampoAMapear As String, ByVal pFormatoDeMapeado As CampoAtributoDN, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN)

            pCampoDatos = New InfoDatosMapInstCampoDN
            pCampoDatos.InfoDatosMapInstClase = pMapInst
            pCampoDatos.NombreCampo = pCampoAMapear
            pCampoDatos.ColCampoAtributo.Add(pFormatoDeMapeado)


        End Sub

        Private Sub VincularConClase(ByVal mCampoAMapear As String, ByVal pElementosDeEmsamblado As List(Of ElementosDeEnsamblado), ByVal mFormatoDeMapeado As CampoAtributoDN, ByRef pAlEntidades As ArrayList, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN, ByRef pMapInstSub As InfoDatosMapInstClaseDN)

            pMapInstSub = New InfoDatosMapInstClaseDN
            pAlEntidades = New ArrayList

            Dim pElemento As ElementosDeEnsamblado
            For Each pElemento In pElementosDeEmsamblado
                pAlEntidades.Add(New VinculoClaseDN(pElemento.Ensamblado, pElemento.Clase))
            Next

            pMapInstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = pAlEntidades

            pCampoDatos = New InfoDatosMapInstCampoDN
            pCampoDatos.InfoDatosMapInstClase = pMapInst
            pCampoDatos.NombreCampo = mCampoAMapear
            pCampoDatos.ColCampoAtributo.Add(mFormatoDeMapeado)
            pCampoDatos.MapSubEntidad = pMapInstSub

        End Sub

        Private Sub VincularConClase(ByVal mCampoAMapear As String, ByVal pElementoDeEmsamblado As ElementosDeEnsamblado, ByVal mFormatoDeMapeado As CampoAtributoDN, ByRef pAlEntidades As ArrayList, ByRef pCampoDatos As InfoDatosMapInstCampoDN, ByRef pMapInst As InfoDatosMapInstClaseDN, ByRef pMapInstSub As InfoDatosMapInstClaseDN)

            pMapInstSub = New InfoDatosMapInstClaseDN
            pAlEntidades = New ArrayList

            pAlEntidades.Add(New VinculoClaseDN(pElementoDeEmsamblado.Ensamblado, pElementoDeEmsamblado.Clase))
            pMapInstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = pAlEntidades

            pCampoDatos = New InfoDatosMapInstCampoDN
            pCampoDatos.InfoDatosMapInstClase = pMapInst
            pCampoDatos.NombreCampo = mCampoAMapear
            pCampoDatos.ColCampoAtributo.Add(mFormatoDeMapeado)
            pCampoDatos.MapSubEntidad = pMapInstSub

        End Sub

    End Class



End Namespace

'#Region "Importaciones"

'Imports Framework.TiposYReflexion.DN

'#End Region

'Namespace LN
'    Public Class GestorMapPersistenciaCamposGilmarLN
'        Inherits GestorMapPersistenciaCamposLN

'        'TODO: ALEX por implementar: recuperar el mapeado de instanciacion de la fuente de datos para el tipo a procesar
'        Public Overrides Function RecuperarMapPersistenciaCampos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
'            Dim mapinst As New InfoDatosMapInstClaseDN
'            Dim campodatos As InfoDatosMapInstCampoDN



'            If (pTipo.FullName = "UsuariosDN.DatosIdentidadDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mClave"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "GestionProductoDN.DatosMultimediaACPDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mGruposImagenes"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "GestionDemandaDN.ProductoOfertadoDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "_TratamientoDemanda"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "GestionProductoDN.EntradaModificacionACPDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "_ACP"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.IInmuebleDN") Then
'                Dim alentidades As New ArrayList

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.InmuebleBasicoDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Solar.SolarDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Garaje.GarajeDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Puelo.PU.Piso.PisoDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Puelo.PU.Unifamiliar.UnifamiliarDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Puelo.EL.Edificio.EdificioDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Puelo.EL.Local.LocalDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Puelo.EL.Nave.NaveDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Puelo.Oficina.OficinaDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.Puelo.EL.Nave.NaveDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mEl"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.Puelo.EL.Local.LocalDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mEl"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.Puelo.EL.Edificio.EdificioDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mEl"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mDistribucion"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                Return mapinst
'            End If


'            If (pTipo.FullName = "InmueblesDN.Puelo.EL.ElDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPuelo"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.Puelo.PU.Unifamiliar.UnifamiliarDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPu"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.Puelo.PU.Piso.PisoDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPu"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.Puelo.PU.PUDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPuelo"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.Puelo.Oficina.OficinaDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPuelo"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.Garaje.IGarajeComunDN") Then
'                Dim alentidades As New ArrayList

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Garaje.GarajeComunDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.Garaje.IGarajeCortoDN") Then
'                Dim alentidades As New ArrayList

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.Garaje.GarajeCortoDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.Garaje.GarajeDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mGarajeComun"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.Garaje.GarajeCortoDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mGarajeComun"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.DireccionInmuebleDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mColEspecificacionesDireccion"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                'TODO: HE AÑADIDO ESTA LINEA QUE NO ESTABA!!! (Vicente)
'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.OperacionesPermitidasDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mVenta"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mAlquiler"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.InmuebleBasicoDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mDatosRegistro"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mFormaVisita"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "UsuariosDN.PermisoDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "_Metodo"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.TiposInmuebles.TiposInmuebleNodoDN") Then
'                Dim mapinstSub As New InfoDatosMapInstClaseDN
'                Dim alentidades As New ArrayList

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPadre"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "_Validador"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mVc"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.TiposInmuebles.SubTiposInmuebleNodoDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mhijos"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'                campodatos.MapSubEntidad = mapinstSub

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.TiposInmuebles.TiposInmuebleArbolDN") Then
'                Dim mapinstSub As New InfoDatosMapInstClaseDN
'                Dim alentidades As New ArrayList

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPadre"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "_Validador"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "_Datos"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.TiposInmuebles.TiposInmuebleNodoDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mhijos"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'                campodatos.MapSubEntidad = mapinstSub

'                Return mapinst
'            End If

'            If (pTipo.FullName = "InmueblesDN.TiposInmuebles.SubTiposInmuebleNodoDN") Then
'                Dim mapinstSub As New InfoDatosMapInstClaseDN
'                Dim alentidades As New ArrayList

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.TiposInmuebles.TiposInmuebleNodoDN"))
'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.TiposInmuebles.SubTiposInmuebleNodoDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPadre"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'                campodatos.MapSubEntidad = mapinstSub

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "_Validador"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                mapinstSub = New InfoDatosMapInstClaseDN
'                alentidades = New ArrayList

'                alentidades.Add(New VinculoClaseDN("InmueblesDN", "InmueblesDN.TiposInmuebles.SubTiposInmuebleNodoDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mhijos"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'                campodatos.MapSubEntidad = mapinstSub

'                Return mapinst
'            End If

'            If (pTipo.FullName = "LocalizacionesDN.Direcciones.RangoViaDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mImpares"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPares"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "ZonasDN.ZonaDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPadre"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mhijos"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "_Validador"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "ZonasDN.ArbolZonasDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mhijos"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)  ' cambiar por serializar

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPadre"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "_Validador"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "PersonasDN.PersonaDN") Then
'                Dim mapSubInst As New InfoDatosMapInstClaseDN

'                mapSubInst.NombreCompleto = "LocalizacionesDN.NifDN"

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapSubInst
'                campodatos.NombreCampo = "mID"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mDNI"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
'                campodatos.MapSubEntidad = mapSubInst

'                Return mapinst
'            End If

'            If (pTipo.FullName = "PersonasDN.ContactoDN") Then
'                Dim mapSubInst As New InfoDatosMapInstClaseDN

'                mapSubInst.NombreCompleto = "LocalizacionesDN.EmailDN"

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapSubInst
'                campodatos.NombreCampo = "mID"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "_EmailDN"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "ClientesDN.EmpresaDN") Then
'                Dim mapSubInst As New InfoDatosMapInstClaseDN

'                mapSubInst.NombreCompleto = "LocalizacionesDN.CifDN"

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapSubInst
'                campodatos.NombreCampo = "mID"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "_CifDN"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            If (pTipo.FullName = "ClientesDN.IClienteDN") Then
'                Dim alentidades As New ArrayList

'                alentidades.Add(New VinculoClaseDN("ClientesDN", "ClientesDN.ClienteDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                Return mapinst
'            End If

'            If (pTipo.FullName = "PersonasDN.IContactoDN") Then
'                Dim alentidades As New ArrayList

'                alentidades.Add(New VinculoClaseDN("PersonasDN", "PersonasDN.ContactoDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                Return mapinst
'            End If

'            If (pTipo.FullName = "EmpleadosDN.IAvisador") Then
'                Dim alentidades As New ArrayList

'                alentidades.Add(New VinculoClaseDN("EmpleadosDN", "EmpleadosDN.EmpleadoDN"))
'                alentidades.Add(New VinculoClaseDN("EmpleadosDN", "EmpleadosDN.ColaboradorDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                Return mapinst
'            End If

'            'PRUEBAS!!!
'            If (pTipo.FullName = "MotorADTest.IInterface") Then
'                Dim alentidades As New ArrayList
'                alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.B"))
'                alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.C"))
'                alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.D"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                Return mapinst
'            End If

'            If pTipo.FullName = "MotorADTest.A" Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "_CampoColMap"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'                campodatos.Datos.Add("MotorADTest.D")

'                Return mapinst
'            End If

'            If pTipo.FullName = "MotorADTest.InterfacePrueba" Then
'                Dim alentidades As New ArrayList
'                alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.PruebaAI"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                Return mapinst
'            End If

'            If pTipo.FullName = "MotorADTest.PruebaF" Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mCRPruebaA"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

'                Return mapinst
'            End If

'            Return Nothing
'        End Function
'    End Class







'    Public Class GestorMapPersistenciaCamposToyotaPGVDLN
'        Inherits GestorMapPersistenciaCamposLN

'        'TODO: ALEX por implementar: recuperar el mapeado de instanciacion de la fuente de datos para el tipo a procesar
'        Public Overrides Function RecuperarMapPersistenciaCampos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
'            Dim mapinst As InfoDatosMapInstClaseDN
'            Dim campodatos As InfoDatosMapInstCampoDN

'            mapinst = RecuperarMapPersistenciaCamposPrivado(pTipo)

'            ' ojo esta modificación se debe aplicar siempre si el tipo hereda de una huella es decir en el metodo que lo llamo
'            If (TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo)) Then
'                If mapinst Is Nothing Then
'                    mapinst = New InfoDatosMapInstClaseDN
'                End If
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mEntidadReferidaHuella"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.SoloGuardarYNoReferido)
'            End If




'            Return mapinst
'        End Function


'        protected overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
'            Dim mapinst As New InfoDatosMapInstClaseDN
'            Dim campodatos As InfoDatosMapInstCampoDN

'            ''Esto esta comentado, ya que en principio no nos interesaba tener una huella de departamento, 
'            ''pero una vez que esto ya está implementado, nos interesa tener este elemento para poder
'            ''tener una referencia desde el empleado a la empresa.
'            'If (pTipo.FullName = "EmpresasDN.RolDepartamentoDN") Then
'            '    campodatos = New InfoDatosMapInstCampoDN
'            '    campodatos.InfoDatosMapInstClase = mapinst
'            '    campodatos.NombreCampo = "mHDepartamentoDN"
'            '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'            '    Return mapinst
'            'End If

'            ''Esto esta comentado, ya que en principio no nos interesaba tener una huella de departamento, 
'            ''pero una vez que esto ya está implementado, nos interesa tener este elemento para poder
'            ''tener una referencia desde el empleado a la empresa.
'            'If (pTipo.FullName = "EmpresasDN.SedeEmpresaDN") Then
'            '    campodatos = New InfoDatosMapInstCampoDN
'            '    campodatos.InfoDatosMapInstClase = mapinst
'            '    campodatos.NombreCampo = "mHuellaAgrupacionDeEmpresasDN"
'            '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'            '    Return mapinst
'            'End If
'            '"EmpresaToyota.AdaptadorEvalConcesionarioDN"





'            'TODO: REVISAR________________________________________________________________
'            'Mapeado de TareaDN con padre otra TareaDN, hijos una colección de TareaDN
'            'con hijos AdaptadorEvalEmpleadoDN
'            If (pTipo.FullName = "AuditoriasDN.TareaDN") Then
'                Dim mapinstSub As InfoDatosMapInstClaseDN
'                Dim alentidades As ArrayList


'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidadorh"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidadorp"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                mapinstSub = New InfoDatosMapInstClaseDN
'                alentidades = New ArrayList
'                alentidades.Add(New VinculoClaseDN("AuditoriasDN", "AuditoriasDN.TareaDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPadre"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'                campodatos.MapSubEntidad = mapinstSub

'                mapinstSub = New InfoDatosMapInstClaseDN
'                alentidades = New ArrayList
'                alentidades.Add(New VinculoClaseDN("AuditoriasDN", "AuditoriasDN.TareaDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mHijos"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'                campodatos.MapSubEntidad = mapinstSub



'                mapinstSub = New InfoDatosMapInstClaseDN
'                alentidades = New ArrayList
'                alentidades.Add(New VinculoClaseDN("AuditoriasDN", "AuditoriasDN.CausaObservacionDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mCausaTareaDN"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'                campodatos.MapSubEntidad = mapinstSub


'                Return mapinst
'            End If
'            '_____________________________________________________________________________



'            '_________________________________________________________________________
'            '_________________________________________________________________________
'            '_________________________________________________________________________
'            'TODO: Revisar
'            'Mapeado del IEvaluableDN, donde la interfaz se mapea para todas las clases, y habrá que 
'            'decir que clases implementan esta interfaz
'            If (pTipo.FullName = "AuditoriasDN.IEvaluableDN") Then
'                Dim alentidades As New ArrayList
'                alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.AdaptadorEvalConcesionarioDN"))
'                alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.AdaptadorEvalEmpleadoDN"))
'                alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.AdaptadorEvalPuntoVentaDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
'                Return mapinst
'            End If

'            '******************************************************************************************
'            'NOTA IMPORTANTE PARA EL ORDEN DE CREACIÓN DE TABLAS
'            'Cuando se tengan que crear estas tablas, se crean primero hacia arriba, esto es, se crean
'            'mapeando los hijos como no procesados, y solo se procesan los padres, y luego, se mapean con todo
'            'y se vuelven a crear las tablas
'            '******************************************************************************************

'            'Mapeado para el concesionario con hijos AdaptadorEvalPuntoVentaDN
'            If (pTipo.FullName = "EmpresaToyotaDN.AdaptadorEvalConcesionarioDN") Then
'                Dim mapinstSub As New InfoDatosMapInstClaseDN
'                Dim alentidades As New ArrayList

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidadorh"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidadorp"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.AdaptadorEvalPuntoVentaDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mHijos"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
'                campodatos.MapSubEntidad = mapinstSub

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPadre"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
'                campodatos.MapSubEntidad = mapinstSub

'                Return mapinst
'            End If

'            'Mapeado de empleados con padre AdaptadorEvalPuntoVentaDN
'            If (pTipo.FullName = "EmpresaToyotaDN.AdaptadorEvalEmpleadoDN") Then
'                Dim mapinstSub As New InfoDatosMapInstClaseDN
'                Dim alentidades As New ArrayList

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidadorh"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidadorp"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.AdaptadorEvalPuntoVentaDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPadre"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'                campodatos.MapSubEntidad = mapinstSub

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mHijos"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                Return mapinst
'            End If


'            'Mapeado de puntos de venta con padre AdaptadorEvalConcesionarioDN
'            'con hijos AdaptadorEvalEmpleadoDN
'            If (pTipo.FullName = "EmpresaToyotaDN.AdaptadorEvalPuntoVentaDN") Then
'                Dim mapinstSub As InfoDatosMapInstClaseDN
'                Dim alentidades As ArrayList

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidadorh"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidadorp"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                mapinstSub = New InfoDatosMapInstClaseDN
'                alentidades = New ArrayList
'                alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.AdaptadorEvalConcesionarioDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPadre"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'                campodatos.MapSubEntidad = mapinstSub

'                mapinstSub = New InfoDatosMapInstClaseDN
'                alentidades = New ArrayList
'                alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.AdaptadorEvalEmpleadoDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mHijos"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
'                campodatos.MapSubEntidad = mapinstSub

'                Return mapinst
'            End If

'            'FINTODO: Revisar
'            '_________________________________________________________________________
'            '_________________________________________________________________________
'            '_________________________________________________________________________


'            '_________________________________________________________________________
'            'esto hay que quitarlo luego, pero es para test ObservacionesDNTest


'            If (pTipo.FullName = "AuditoriasDN.ObservacionDN") Then

'                Dim mapinstSub As InfoDatosMapInstClaseDN
'                Dim alentidades As ArrayList


'                mapinstSub = New InfoDatosMapInstClaseDN
'                alentidades = New ArrayList

'                alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.AdaptadorEvalEmpleadoDN"))
'                alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.AdaptadorEvalConcesionarioDN"))
'                alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.AdaptadorEvalPuntoVentaDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mIEvaluable"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                Return mapinst
'            End If

'            'Mapeado de UsuarioDN, donde la clase mapea sus interfaces, y solo es para ella.
'            'En cualquier otro clase que tenga la misma base, habrá que mapearla para esa.
'            If (pTipo.FullName = "UsuariosDN.UsuarioDN") Then
'                Dim mapinstSub As New InfoDatosMapInstClaseDN
'                Dim alentidades As New ArrayList


'                alentidades.Add(New VinculoClaseDN("EmpresasDN", "EmpresasDN.HuellaEmpleadoDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mHuellaEntidadUserDN"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'                campodatos.MapSubEntidad = mapinstSub

'                Return mapinst
'            End If


'            If (pTipo.FullName = "UsuariosDN.DatosIdentidadDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mHashClave"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                Return mapinst
'            End If


'            'TODO: Revisar el mapeado para la visita -> GrupoVisitante_____________________
'            If pTipo.FullName = "AuditoriasDN.VisitaDN" Then

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst

'                campodatos.NombreCampo = "mPlanificadorDN"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mSujetoDN"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mSupervisorDN"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mBeneficiarioDN"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mConLoQueSeHaceODDN"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mSobreQueSeHaceOIDN"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                'campodatos.NombreCampo = "mIEvaluable"
'                'campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mHijos"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mPadre"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidadorp"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidadorh"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


'                Dim mapinstSub As New InfoDatosMapInstClaseDN
'                Dim alentidades As New ArrayList

'                alentidades.Add(New VinculoClaseDN("EmpresaToyotaDN", "EmpresaToyotaDN.GrupoVisitanteDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mQuienHaceVisita"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'                campodatos.MapSubEntidad = mapinstSub


'                Return mapinst

'            End If
'            '______________________________________________________________________________


'            'Mapeado del DelegadoTerritorialDN, donde la clase mapea sus interfaces, y solo es para ella.
'            'En cualquier otro clase que tenga la misma base, habrá que mapearla para esa.
'            If (pTipo.FullName = "EmpresaToyotaDN.DelegadoTerritorialDN") Then
'                Dim mapinstSub As New InfoDatosMapInstClaseDN
'                Dim alentidades As New ArrayList


'                alentidades.Add(New VinculoClaseDN("EmpresasDN", "EmpresasDN.EmpleadoDN"))
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mEntidadResponsableDN"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
'                campodatos.MapSubEntidad = mapinstSub

'                Return mapinst
'            End If


'            'Mapeado del IResponsableDN, donde la interfaz se mapea para todas las clases, y habrá que 
'            'decir que clases implementan esta interfaz
'            If (pTipo.FullName = "EmpresasDN.IResponsableDN") Then
'                Dim alentidades As New ArrayList
'                alentidades.Add(New VinculoClaseDN("EmpresasDN", "EmpresasDN.ResponsableAgrupacionDeEmpresasDN"))
'                alentidades.Add(New VinculoClaseDN("EmpresasDN", "EmpresasDN.ResponsableDePersonalDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
'                Return mapinst
'            End If


'            'Mapeado de la clase CategoriaDN, donde indicamos como se mapean los distintos campos de la clase
'            If (pTipo.FullName = "AuditoriasDN.CategoriaDN") Then
'                Dim mapinstSub As InfoDatosMapInstClaseDN
'                Dim alentidades As ArrayList


'                ' INICIO MAPEADO DE CAMPO --- INTERFACE
'                campodatos = New InfoDatosMapInstCampoDN ' el mapeado para uno de los campos de la iantancia
'                campodatos.InfoDatosMapInstClase = mapinst ' el mapeado para la intancia solicitada
'                campodatos.NombreCampo = "mPadre" '(el nombre del campoobjetivo ) este campo esta declarado como un INodo y el mapeado lo fija en una entidad ZonaDN
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor) ' comportamiento que se realizara para este campo 

'                ' cro el mapeado para la interface referida segun este campo
'                mapinstSub = New InfoDatosMapInstClaseDN
'                alentidades = New ArrayList

'                alentidades.Add(New VinculoClaseDN("AuditoriasDN", "AuditoriasDN.CategoriaDN")) 'añadir una linea de estas por cada clase que puede admitir el objeto en este campo y claro esta que implemetne la itnerface
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos.MapSubEntidad = mapinstSub
'                ' FIN MAPEADO DE CAMPO --- INTERFACE



'                ' INICIO MAPEADO DE CAMPO --- INTERFACE
'                campodatos = New InfoDatosMapInstCampoDN ' el mapeado para uno de los campos de la iantancia
'                campodatos.InfoDatosMapInstClase = mapinst ' el mapeado para la intancia solicitada
'                campodatos.NombreCampo = "mHijos" '(el nombre del campoobjetivo ) este campo esta declarado como un INodo y el mapeado lo fija en una entidad ZonaDN
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor) ' comportamiento que se realizara para este campo 

'                ' cro el mapeado para la interface referida segun este campo
'                mapinstSub = New InfoDatosMapInstClaseDN
'                alentidades = New ArrayList

'                alentidades.Add(New VinculoClaseDN("AuditoriasDN", "AuditoriasDN.CategoriaDN")) 'añadir una linea de estas por cada clase que puede admitir el objeto en este campo y claro esta que implemetne la itnerface
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos.MapSubEntidad = mapinstSub
'                ' FIN MAPEADO DE CAMPO --- INTERFACE


'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidadorh"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidadorp"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)



'                Return mapinst
'            End If

'            If (pTipo.FullName = "LocalizacionesDN.ZonaDN") Then
'                Dim mapinstSub As InfoDatosMapInstClaseDN
'                Dim alentidades As ArrayList


'                ' INICIO MAPEADO DE CAMPO --- INTERFACE
'                campodatos = New InfoDatosMapInstCampoDN ' el mapeado para uno de los campos de la iantancia
'                campodatos.InfoDatosMapInstClase = mapinst ' el mapeado para la intancia solicitada
'                campodatos.NombreCampo = "mPadre" '(el nombre del campoobjetivo ) este campo esta declarado como un INodo y el mapeado lo fija en una entidad ZonaDN
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor) ' comportamiento que se realizara para este campo 

'                ' cro el mapeado para la interface referida segun este campo
'                mapinstSub = New InfoDatosMapInstClaseDN
'                alentidades = New ArrayList

'                alentidades.Add(New VinculoClaseDN("LocalizacionesDN", "LocalizacionesDN.ZonaDN")) 'añadir una linea de estas por cada clase que puede admitir el objeto en este campo y claro esta que implemetne la itnerface
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos.MapSubEntidad = mapinstSub
'                ' FIN MAPEADO DE CAMPO --- INTERFACE



'                ' INICIO MAPEADO DE CAMPO --- INTERFACE
'                campodatos = New InfoDatosMapInstCampoDN ' el mapeado para uno de los campos de la iantancia
'                campodatos.InfoDatosMapInstClase = mapinst ' el mapeado para la intancia solicitada
'                campodatos.NombreCampo = "mHijos" '(el nombre del campoobjetivo ) este campo esta declarado como un INodo y el mapeado lo fija en una entidad ZonaDN
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor) ' comportamiento que se realizara para este campo 

'                ' cro el mapeado para la interface referida segun este campo
'                mapinstSub = New InfoDatosMapInstClaseDN
'                alentidades = New ArrayList

'                alentidades.Add(New VinculoClaseDN("LocalizacionesDN", "LocalizacionesDN.ZonaDN")) 'añadir una linea de estas por cada clase que puede admitir el objeto en este campo y claro esta que implemetne la itnerface
'                mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

'                campodatos.MapSubEntidad = mapinstSub
'                ' FIN MAPEADO DE CAMPO --- INTERFACE


'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidadorh"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidadorp"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)



'                Return mapinst
'            End If


'            If (pTipo.FullName = "Framework.DatosNegocio.Arboles.NodoBaseDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidador"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                Return mapinst

'            End If

'            If (pTipo.FullName = "Framework.DatosNegocio.Arboles.ColNodosDN") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidador"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                Return mapinst

'            End If


'            If (pTipo.FullName = "DatosNegocio.ArrayListValidable") Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidador"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

'                Return mapinst

'            End If

'            If (pTipo.FullName = "MotorADTest.pruebaD") Then
'                Dim alentidades As New ArrayList

'                alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.PruebaDHC"))
'                alentidades.Add(New VinculoClaseDN("MotorADTest", "MotorADTest.PruebaDHuellaCacheDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ActualizarClasesHuella) = alentidades
'                Return mapinst
'            End If

'            If (pTipo.FullName = "LocalizacionesDN.IContactoElementoDN") Then
'                Dim alentidades As New ArrayList
'                'alentidades.Add(New VinculoClaseDN("LocalizacionesDN", "LocalizacionesDN.DireccionNoUnicaDN"))
'                alentidades.Add(New VinculoClaseDN("LocalizacionesDN", "LocalizacionesDN.ContactoElementoDN"))
'                mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
'                Return mapinst
'            End If


'            If pTipo.FullName = "PersonasDN.PersonaDN" Then
'                Dim mapSubInst As New InfoDatosMapInstClaseDN

'                ' mapeado de la clase referida por el campo
'                mapSubInst.NombreCompleto = "LocalizacionesDN.NifDN"

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapSubInst
'                campodatos.NombreCampo = "mID"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapSubInst
'                campodatos.NombreCampo = "mFechaModificacion"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapSubInst
'                campodatos.NombreCampo = "mBaja"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

'                ' FIN    mapeado de la clase referida por el campo ******************


'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mNIF"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
'                campodatos.MapSubEntidad = mapSubInst

'                Return mapinst
'            End If


'            ' ojo esta modificación se debe aplicar siempre si el tipo hereda de una huella es decir en el metodo que lo llamo
'            If (TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo)) Then
'                campodatos = New InfoDatosMapInstCampoDN
'                campodatos.InfoDatosMapInstClase = mapinst
'                campodatos.NombreCampo = "mValidador"
'                campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)
'            End If


'            Return Nothing
'        End Function


'    End Class
'End Namespace
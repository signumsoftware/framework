Imports Framework
Public Class ComandoMapDN
    Inherits ElementoMapDN
    Protected mComandoBasico As ComandosMapBasicos = -1
    Protected mEsComandoBasico As Boolean
    Protected mVinculoMetodo As Framework.TiposYReflexion.DN.VinculoMetodoDN

    Public Property VinculoMetodo() As Framework.TiposYReflexion.DN.VinculoMetodoDN
        Get
            Return Me.mVinculoMetodo
        End Get
        Set(ByVal value As Framework.TiposYReflexion.DN.VinculoMetodoDN)
            Me.CambiarValorRef(Of Framework.TiposYReflexion.DN.VinculoMetodoDN)(value, Me.mVinculoMetodo)
        End Set
    End Property

    Public ReadOnly Property EsComandoBasico() As Boolean
        Get
            Return mComandoBasico <> -1
        End Get
    End Property

    Public ReadOnly Property ComandoBasico() As ComandosMapBasicos
        Get
            Return mComandoBasico
        End Get
    End Property


    Public Overrides Function ElementoContenedor(ByVal pElementoMap As ElementoMapDN) As IElemtoMap
        Return Nothing
    End Function

    Public Sub New()

    End Sub

    Public Sub New(ByVal pComandoBasico As ComandosMapBasicos)

        Dim comando As New ComandoMapDN


        mID = pComandoBasico
        mNombre = pComandoBasico.ToString
        mNombreVis = pComandoBasico.ToString
        mIco = pComandoBasico.ToString
        mComandoBasico = pComandoBasico

        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar



    End Sub


    'Public Shared Function CrearComandoMapBasico(ByVal pComandoBasico As ComandosMapBasicos) As ComandoMapDN

    '    Dim comando As New ComandoMapDN


    '    comando.ID = pComandoBasico
    '    comando.Nombre = pComandoBasico.ToString
    '    comando.NombreVis = pComandoBasico.ToString
    '    comando.Ico = pComandoBasico.ToString

    '    Return comando


    'End Function
    Public Overrides Function ToString() As String
        Return Me.mNombreVis
    End Function
End Class


Public Class ColComandoMapDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of ComandoMapDN)
End Class


Public Enum ComandosMapBasicos
    Crear = 1
    Buscar = 2
    NoReferir = 3
    NoReferirTodos = 4
    NavegarTransicion = 5
    NavegarEditar = 6
    Baja = 7
    Eliminar = 8
    Aceptar = 9
    Cancelar = 10
    Guardar = 11
End Enum




Public Class ComandoMapDNXml
    Inherits ElementoMapXML

    Public VinculoMetodo As New Framework.TiposYReflexion.DN.VinculoMetodoDNMapXml


    Public Overrides Sub ObjetoToXMLAdaptador(ByVal pEntidad As DatosNegocio.IEntidadBaseDN)
        Dim mientidad As ComandoMapDN = pEntidad
        If mientidad.VinculoMetodo IsNot Nothing Then
            VinculoMetodo.ObjetoToXMLAdaptador(mientidad.VinculoMetodo)

        End If
        MyBase.ObjetoToXMLAdaptador(pEntidad)


    End Sub

    Public Overrides Sub XMLAdaptadorToObjeto(ByVal pEntidad As DatosNegocio.IEntidadBaseDN)

        Dim mientidad As ComandoMapDN = pEntidad
        Dim v As New Framework.TiposYReflexion.DN.VinculoMetodoDN
        VinculoMetodo.XMLAdaptadorToObjeto(v)
        mientidad.VinculoMetodo = v
        MyBase.XMLAdaptadorToObjeto(pEntidad)



    End Sub
End Class
#Region "Importaciones"

Imports System.Reflection

#End Region

Namespace DN
    <Serializable()> Public Class VinculoClaseDN
        Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"
        Protected mNombreEnsamblado As String
        Protected mNombreClase As String
#End Region

#Region "Constructores"
        Public Sub New()
        End Sub
        Public Sub New(ByVal pNombreEnsamblado As String, ByVal pNombreClase As String)
            Me.CambiarValorVal(Of String)(pNombreEnsamblado, mNombreEnsamblado)
            Me.CambiarValorVal(Of String)(pNombreClase, mNombreClase)
            Me.modificarEstado = DatosNegocio.EstadoDatosDN.SinModificar
        End Sub

        Public Sub New(ByVal pTipo As System.Type)

            'mNombreEnsamblado = pTipo.Assembly.FullName
            'mNombreClase = pTipo.Name


            Me.CambiarValorVal(Of String)(pTipo.Assembly.FullName, mNombreEnsamblado)
            Me.CambiarValorVal(Of String)(pTipo.Namespace & "." & pTipo.Name, mNombreClase)
            Me.modificarEstado = DatosNegocio.EstadoDatosDN.SinModificar


        End Sub


#End Region

#Region "Propiedades"

        Public ReadOnly Property NombreEnsambladoCorto() As String
            Get
                If String.IsNullOrEmpty(mNombreEnsamblado) Then
                    Return Nothing
                End If
                Return Me.mNombreEnsamblado.Split(",")(0)
            End Get
        End Property
        Public Property NombreEnsamblado() As String
            Get
                Return Me.mNombreEnsamblado
                ' Return Me.mNombreEnsamblado.Split(",")(0)
            End Get
            Set(ByVal value As String)
                Me.CambiarValorVal(Of String)(value, mNombreEnsamblado)

            End Set
        End Property
        Public ReadOnly Property NombreEnsambladoClaseCorto() As String
            Get
                If String.IsNullOrEmpty(mNombreEnsamblado) Then
                    Return Nothing
                End If
                Return Me.mNombreEnsamblado.Split(",")(0) & "-" & mNombreClase
            End Get
        End Property
        Public ReadOnly Property NombreEnsambladoClase() As String
            Get
                If String.IsNullOrEmpty(mNombreEnsamblado) OrElse String.IsNullOrEmpty(Me.mNombreClase) Then
                    Return Nothing
                End If
                Return Me.mNombreEnsamblado & "//" & mNombreClase
            End Get
        End Property
        Public Property NombreClase() As String
            Get
                Return mNombreClase
            End Get
            Set(ByVal value As String)
                Me.CambiarValorVal(Of String)(value, mNombreClase)

            End Set
        End Property

        Public ReadOnly Property Ensamblado() As Assembly
            Get
                If Not String.IsNullOrEmpty(mNombreEnsamblado) Then
                    Return Assembly.Load(mNombreEnsamblado)
                End If

            End Get
        End Property

        Public ReadOnly Property TipoClase() As System.Type
            Get

                If String.IsNullOrEmpty(mNombreEnsamblado) OrElse String.IsNullOrEmpty(Me.mNombreClase) Then
                    Return Nothing
                End If

                Dim ensamblado As Assembly = Nothing
                Dim miTipoClase As System.Type = Nothing

                If ensamblado Is Nothing Then
                    LN.InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo(mNombreEnsamblado.Split(",")(0) & "/" & mNombreClase, ensamblado, miTipoClase)
                    TipoClase = miTipoClase
                End If

                If TipoClase Is Nothing Then
                    Throw New Framework.DatosNegocio.ApplicationExceptionDN("no se recupero un tipo valido")
                End If
            End Get
        End Property
#End Region

        Public Function CrearInstancia() As Object
            Return Activator.CreateInstance(Me.TipoClase)
        End Function


        Public Shared Function RecuperarNombreEnsambladoClase(ByVal pTipo As System.Type) As String
            Return pTipo.Assembly.FullName & "/" & pTipo.FullName
        End Function


        Public Overrides Function ToXML() As String
            Return Me.ToXML(GetType(VinculoClaseMapXml))
        End Function
        Public Overrides Function FromXML(ByVal ptr As System.IO.TextReader) As Object
            Return FromXML(GetType(VinculoClaseMapXml), ptr)
        End Function
        'Friend Sub AsignarNombreEnsamblado(ByVal pNombreEnsamblado As String)
        '    Me.mNombreEnsamblado = pNombreEnsamblado
        'End Sub
        'Friend Sub AsignarNombreClase(ByVal pNombreClase As String)
        '    Me.mNombreClase = NombreClase
        'End Sub
        Public Overrides Function ToString() As String
            Return Me.mNombreEnsamblado & "/" & Me.mNombreClase
        End Function

    End Class
    <Serializable()> Public Class ColVinculoClaseDN
        Inherits Framework.DatosNegocio.ArrayListValidable(Of VinculoClaseDN)
        Public Function ContieneTipo(ByVal pTipo As System.Type) As Boolean

            For Each vc As VinculoClaseDN In Me
                If vc.TipoClase Is pTipo Then
                    Return True
                End If

            Next
            Return False
        End Function
    End Class


    <Serializable()> Public Class VinculoClaseMapXml
        Implements Framework.DatosNegocio.IXMLAdaptador

        <Xml.Serialization.XmlAttribute()> Public NombreEnsamblado As String
        <Xml.Serialization.XmlAttribute()> Public NombreClase As String

        Public Sub ObjetoToXMLAdaptador(ByVal pEntidad As DatosNegocio.IEntidadBaseDN) Implements DatosNegocio.IXMLAdaptador.ObjetoToXMLAdaptador
            Dim mientidad As DN.VinculoClaseDN = pEntidad

            NombreEnsamblado = mientidad.NombreEnsamblado
            NombreClase = mientidad.NombreClase
        End Sub

        Public Sub XMLAdaptadorToObjeto(ByVal pEntidad As DatosNegocio.IEntidadBaseDN) Implements DatosNegocio.IXMLAdaptador.XMLAdaptadorToObjeto

            Dim mientidad As DN.VinculoClaseDN = pEntidad

            mientidad.NombreEnsamblado = (NombreEnsamblado)
            mientidad.NombreClase = (NombreClase)

        End Sub
    End Class
End Namespace


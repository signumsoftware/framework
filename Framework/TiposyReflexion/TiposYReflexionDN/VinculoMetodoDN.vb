Namespace DN
    <Serializable()> Public Class VinculoMetodoDN
        Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"
        Protected mVinculoClase As VinculoClaseDN
        Protected mNombreMetodo As String
#End Region

        Public Sub New()

        End Sub

        Public Sub New(ByVal pmetodo As System.Reflection.MethodInfo)
            mNombreMetodo = pmetodo.Name
            mVinculoClase = New VinculoClaseDN(pmetodo.ReflectedType)

        End Sub

        Public Sub New(ByVal pNombreEnsamblado As String, ByVal pNombreClase As String, ByVal pNombreMetodo As String)
            mVinculoClase = New VinculoClaseDN(pNombreEnsamblado, pNombreClase)
            mNombreMetodo = pNombreMetodo
        End Sub

        Public Sub New(ByVal metodo As System.Reflection.MethodInfo, ByVal vinculoClase As VinculoClaseDN)
            If metodo.ReflectedType IsNot vinculoClase.TipoClase Then
                Throw New Framework.DatosNegocio.ApplicationExceptionDN("El tipo del método y del vínculo clase no coinciden")
            End If

            mNombreMetodo = metodo.Name
            mVinculoClase = vinculoClase
        End Sub

        Public Sub New(ByVal nombreMetodo As String, ByVal vinculoClase As VinculoClaseDN)
            If vinculoClase.TipoClase.GetMethod(nombreMetodo) Is Nothing Then
                Throw New Framework.DatosNegocio.ApplicationExceptionDN("El nombre del método y el vínculo clase no coinciden")
            End If

            mNombreMetodo = nombreMetodo
            mVinculoClase = vinculoClase
        End Sub

#Region "Propiedades"

        Public Property VinculoClase() As VinculoClaseDN
            Get
                Return mVinculoClase
            End Get
            Set(ByVal value As VinculoClaseDN)
                mVinculoClase = value
            End Set
        End Property

        Public Property NombreMetodo() As String
            Get
                Return mNombreMetodo
            End Get
            Set(ByVal value As String)
                mNombreMetodo = value
            End Set
        End Property

        Public ReadOnly Property NombreEnsambladoClaseMetodo() As String
            Get
                If Not Me.mVinculoClase Is Nothing Then
                    Return Me.mVinculoClase.NombreEnsambladoClase & "." & mNombreMetodo
                Else
                    Return Nothing
                End If
            End Get
        End Property
#End Region


        Public Function RecuperarMethodInfo() As Reflection.MethodInfo

            Return mVinculoClase.TipoClase.GetMethod(mNombreMetodo)


        End Function
        Public Shared Function RecuperarNombreEnsambladoClaseMetodo(ByVal mi As Reflection.MethodInfo) As String
            Return VinculoClaseDN.RecuperarNombreEnsambladoClase(mi.ReflectedType) & "." & mi.Name
        End Function

        Public Overrides Function ToString() As String
            Return Me.VinculoClase.ToString & "." & Me.mNombreMetodo


        End Function

    End Class



    Public Class ColVinculoMetodoDN
        Inherits Framework.DatosNegocio.ArrayListValidable(Of VinculoMetodoDN)
    End Class

    Public Class VinculoMetodoDNMapXml
        Implements Framework.DatosNegocio.IXMLAdaptador

        Public VinculoClase As New VinculoClaseMapXml
        <Xml.Serialization.XmlAttribute()> Public NombreMetodo As String

        Public Sub ObjetoToXMLAdaptador(ByVal pEntidad As DatosNegocio.IEntidadBaseDN) Implements DatosNegocio.IXMLAdaptador.ObjetoToXMLAdaptador
            Dim mientidad As DN.VinculoMetodoDN = pEntidad

            VinculoClase = New VinculoClaseMapXml
            VinculoClase.ObjetoToXMLAdaptador(mientidad.VinculoClase)
            NombreMetodo = mientidad.NombreMetodo
        End Sub

        Public Sub XMLAdaptadorToObjeto(ByVal pEntidad As DatosNegocio.IEntidadBaseDN) Implements DatosNegocio.IXMLAdaptador.XMLAdaptadorToObjeto

            Dim mientidad As DN.VinculoMetodoDN = pEntidad

            Dim vc As New VinculoClaseDN
            VinculoClase.XMLAdaptadorToObjeto(vc)

            mientidad.VinculoClase = vc
            mientidad.NombreMetodo = (NombreMetodo)

        End Sub
    End Class

End Namespace

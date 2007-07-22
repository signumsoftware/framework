Imports Framework.DatosNegocio

Namespace DN

    <Serializable()> Public Class PropiedadDeInstanciaDN
        Inherits Framework.DatosNegocio.EntidadDN

        Protected mIdInstancia As String
        Protected mGUIDInstancia As String
        '  Protected mPropiedad As Reflection.PropertyInfo
        Protected mNombreCampoRel As String
        Protected mNombreTipo As String
        Protected mNombrePropiedad As String

        Public Sub New(ByVal pPropiedad As Reflection.PropertyInfo, ByVal pid As String, ByVal pguid As String)
            Actualizar(pPropiedad, pid, pguid)

        End Sub
        Public Sub New(ByVal pTipoEntidadReferidora As Type, ByVal pNombrePropiedadReferidora As String, ByVal pEntidadReferida As Framework.DatosNegocio.IEntidadDN)
            Actualizar(pTipoEntidadReferidora, pNombrePropiedadReferidora, pEntidadReferida.ID, pEntidadReferida.GUID)
        End Sub
        Public Sub New(ByVal pTipoEntidadReferidora As Type, ByVal pNombrePropiedadReferidora As String, ByVal pid As String, ByVal pguid As String)

            Actualizar(pTipoEntidadReferidora, pNombrePropiedadReferidora, pid, pguid)
        End Sub
        Public Sub New(ByVal entidad As Framework.DatosNegocio.IEntidadBaseDN, ByVal pNombrePropiedad As String)

            Actualizar(entidad, pNombrePropiedad)
        End Sub


        Public Sub Actualizar(ByVal pPropiedad As Reflection.PropertyInfo, ByVal pid As String, ByVal pguid As String)
            Actualizar(pPropiedad.ReflectedType, pPropiedad.Name, pid, pguid)

        End Sub

        Public Sub Actualizar(ByVal pTipoEntidad As Type, ByVal pNombrePropiedad As String, ByVal pid As String, ByVal pguid As String)
            mGUIDInstancia = pguid
            mIdInstancia = pid
            Propiedad = pTipoEntidad.GetProperty(pNombrePropiedad)
            'mNombreTipo = mPropiedad.ReflectedType.FullName
            'mNombrePropiedad = mPropiedad.Name
            'Dim atributos As Object() = mPropiedad.GetCustomAttributes(GetType(Framework.DatosNegocio.RelacionPropCampoAtribute), True)

            'If atributos Is Nothing OrElse atributos.Length = 0 Then
            '    Throw New Framework.DatosNegocio.ApplicationExceptionDN("la propiedad " & mPropiedad.Name & " del tipo " & mPropiedad.ReflectedType.FullName & " no cuenta con un atributo de viculación a campo")
            'End If

            'Dim atributoPropCampo As RelacionPropCampoAtribute = atributos(0)
            mNombreCampoRel = RecuperarNombreCampoVinculado(Me.Propiedad)

        End Sub

        Public Shared Function RecuperarNombreCampoVinculado(ByVal pPropiedad As Reflection.PropertyInfo) As String

            Dim atributos As Object() = pPropiedad.GetCustomAttributes(GetType(Framework.DatosNegocio.RelacionPropCampoAtribute), True)

            'If atributos Is Nothing OrElse atributos.Length = 0 Then
            '    Throw New Framework.DatosNegocio.ApplicationExceptionDN("la propiedad " & pPropiedad.Name & " del tipo " & pPropiedad.ReflectedType.FullName & " no cuenta con un atributo de viculación a campo")
            'End If
            If atributos.Length > 0 Then
                Dim atributoPropCampo As RelacionPropCampoAtribute = atributos(0)
                RecuperarNombreCampoVinculado = atributoPropCampo.NombreCampo
            End If


        End Function

        Public Sub Actualizar(ByVal entidad As Framework.DatosNegocio.IEntidadBaseDN, ByVal pNombrePropiedad As String)
            Dim o As Object = entidad
            ' Me.Propiedad = o.GetType.GetProperty(pNombrePropiedad)
            Actualizar(o.GetType, pNombrePropiedad, entidad.ID, entidad.GUID)
        End Sub


        Property NombreCampoRel() As String
            Get
                Return mNombreCampoRel
            End Get
            Set(ByVal value As String)
                Me.CambiarValorVal(Of String)(value, Me.mNombreCampoRel)
                'mNombreCampoRel = value
            End Set
        End Property

        Public Property IdInstancia() As String
            Get
                Return mIdInstancia
            End Get
            Set(ByVal value As String)
                Me.CambiarValorVal(Of String)(value, Me.mIdInstancia)
                'mIdInstancia = value
            End Set
        End Property

        Public Property GUIDInstancia() As String
            Get
                Return mGUIDInstancia
            End Get
            Set(ByVal value As String)
                Me.CambiarValorVal(Of String)(value, Me.mGUIDInstancia)
                'mGUIDInstancia = value
            End Set
        End Property

        Public Property Propiedad() As Reflection.PropertyInfo
            Get
                Dim tipo As System.Type
                Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarEnsambladoYTipo(mNombreTipo, Nothing, tipo)
                Return Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarPropiedad(tipo, Me.mNombrePropiedad, Nothing)
            End Get
            Set(ByVal value As Reflection.PropertyInfo)
                '  mPropiedad = value
                Me.CambiarValorVal(Of String)(value.Name, Me.mNombrePropiedad)
                Me.CambiarValorVal(Of String)(Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.TipoToString(value.ReflectedType), Me.mNombreTipo)
            End Set
        End Property





        Public Function RecuperarCampoRef(ByVal pInfoTypeInstClase As InfoTypeInstClaseDN) As Framework.TiposYReflexion.DN.InfoTypeInstCampoRefDN

            Dim camporef As Framework.TiposYReflexion.DN.InfoTypeInstCampoRefDN

            For Each camporef In pInfoTypeInstClase.CamposRef
                If camporef.Campo.Name = Me.NombreCampoRel Then
                    Return camporef
                End If
            Next

            Return Nothing

        End Function


        Public Shared Function RecuperarCampoRef(ByVal pInfoTypeInstClase As InfoTypeInstClaseDN, ByVal PropiedadDeInstanciaDN As TiposYReflexion.DN.PropiedadDeInstanciaDN) As Framework.TiposYReflexion.DN.InfoTypeInstCampoRefDN

            Dim camporef As Framework.TiposYReflexion.DN.InfoTypeInstCampoRefDN

            For Each camporef In pInfoTypeInstClase.CamposRef
                If camporef.Campo.Name = PropiedadDeInstanciaDN.NombreCampoRel Then
                    Return camporef
                End If
            Next

            Return Nothing

        End Function

    End Class

End Namespace

#Region "Importaciones"

Imports System.Collections.Generic
Imports System.Reflection

Imports Framework.DatosNegocio
Imports Framework.TiposYReflexion.DN

#End Region

Namespace LN
    Public Class InstanciacionReflexionHelperLN


#Region "Metodos"




        Public Shared Sub AsignarValoraEntidadDN(ByVal pValor As Object, ByVal pCanpoDestino As System.Reflection.FieldInfo, ByVal pInstanciaDestino As Object)


            If TypeOf pInstanciaDestino Is IDatoPersistenteDN Then

                Dim idp As IDatoPersistenteDN = pInstanciaDestino

                If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.Implementa(pCanpoDestino.FieldType, GetType(IDatoPersistenteDN)) Then

                    idp.DesRegistrarParte(pCanpoDestino.GetValue(pInstanciaDestino))
                    ' si el campo tine una propiedad del mismo nombre en ese caso se trata de un withevent y es en ese campo donde deve asignarse el valor

                    'Dim pi As PropertyInfo = pInstanciaDestino.GetType.GetProperty(pCanpoDestino.Name.Substring(1), BindingFlags.Instance Or BindingFlags.NonPublic)
                    'If pi Is Nothing Then
                    '    pCanpoDestino.SetValue(pInstanciaDestino, pValor)


                    'Else
                    '    pi.SetValue(pInstanciaDestino, pValor, Nothing)
                    'End If
                    AsignarElValor(pValor, pCanpoDestino, pInstanciaDestino)

                    idp.RegistrarParte(pValor)

                Else
                    AsignarElValor(pValor, pCanpoDestino, pInstanciaDestino)

                    '  pCanpoDestino.SetValue(pInstanciaDestino, pValor)
                End If

            Else

                'pCanpoDestino.SetValue(pInstanciaDestino, pValor)
                AsignarElValor(pValor, pCanpoDestino, pInstanciaDestino)
            End If






        End Sub


        Public Shared Sub AsignarElValor(ByVal pValor As Object, ByVal pCanpoDestino As System.Reflection.FieldInfo, ByVal pInstanciaDestino As Object)

            Dim pi As PropertyInfo = pInstanciaDestino.GetType.GetProperty(pCanpoDestino.Name.Substring(1), BindingFlags.Instance Or BindingFlags.NonPublic)
            If pi Is Nothing Then
                pCanpoDestino.SetValue(pInstanciaDestino, pValor)


            Else
                pi.SetValue(pInstanciaDestino, pValor, Nothing)
            End If

        End Sub



        Public Shared Sub ClonSuperfEnClaseCompatible(ByVal pclaseOrigen As EntidadDN, ByVal pclaseDestino As EntidadDN, ByVal registrados As Boolean)
            ' recorrer cada atributo y ponerlo dentro de la clse heredada

            Dim colcamposOrigen, colcamposDestino As IList(Of System.Reflection.FieldInfo)


            colcamposOrigen = InstanciacionReflexionHelperLN.RecuperarCampos(pclaseOrigen.GetType)

            colcamposDestino = InstanciacionReflexionHelperLN.RecuperarCampos(pclaseDestino.GetType)


            'pasar el valor de los campos
            For Each campoOrigen As Reflection.FieldInfo In colcamposOrigen


                Dim encontrado As Boolean = False
                For Each campoDestino As Reflection.FieldInfo In colcamposDestino

                    If campoDestino.Name = campoOrigen.Name Then

                        If registrados Then
                            AsignarValoraEntidadDN(campoOrigen.GetValue(pclaseOrigen), campoDestino, pclaseDestino)


                        Else

                            campoDestino.SetValue(pclaseDestino, campoOrigen.GetValue(pclaseOrigen))

                        End If

                        encontrado = True
                        Exit For
                    End If

                Next

                If Not encontrado Then
                    Throw New Framework.DatosNegocio.ApplicationExceptionDN("no se encontro el campo " & campoOrigen.Name & " del tipo " & campoOrigen.ReflectedType.FullName & " en el tipo" & pclaseDestino.GetType.FullName)
                End If


            Next



        End Sub



        Public Shared Function RecuperarMetodosDeEnsamblado(ByVal pEnsamblado As String) As ColVinculoMetodoDN
            Dim tipos As Type()
            Dim ensamblado As Assembly


                Dim colvm As New ColVinculoMetodoDN



                'Cargamos el ensamblado y obtenemos sus tipos
                ensamblado = Assembly.Load(pEnsamblado)
                tipos = ensamblado.GetExportedTypes()

                For Each tipo As System.Type In tipos

                    colvm.AddRange(RecuperarColVinculoMetodoDN(tipo))

                Next


                Return colvm


        End Function


        Public Shared Function RecuperarColVinculoMetodoDN(ByVal pTipo As Type) As ColVinculoMetodoDN
            Dim metodos As MethodInfo()
            Dim colvm As New ColVinculoMetodoDN

            'Obtenemos el tipo de la clase y los metodos que declara
            metodos = pTipo.GetMethods(BindingFlags.DeclaredOnly Or BindingFlags.Public Or BindingFlags.Instance)

            Dim vc As New VinculoClaseDN(pTipo)
            For Each mimetodo As Reflection.MethodInfo In metodos
                Dim vm As New VinculoMetodoDN
                vm.VinculoClase = vc
                vm.NombreMetodo = mimetodo.Name
                colvm.Add(vm)
            Next

            Return colvm
        End Function





        Public Shared Function RecuperarPropiedad(ByVal pTipo As System.Type, ByVal pNombrePropiedad As String, ByVal pTipoPropietarioPropiedad As System.Type) As Reflection.PropertyInfo


            Dim propiedades As IList(Of Reflection.PropertyInfo) = RecuperarPropiedades(pTipo)

            For Each pi As Reflection.PropertyInfo In propiedades

                If pi.Name = pNombrePropiedad Then
                    If pTipoPropietarioPropiedad Is Nothing Then
                        Return pi
                    Else
                        If pi.ReflectedType Is pTipoPropietarioPropiedad Then
                            Return pi
                        End If
                    End If
                End If

            Next

            Return Nothing
        End Function

        Public Shared Function RecuperarPropiedades(ByVal pTipo As System.Type) As IList(Of Reflection.PropertyInfo)
            Dim flags As BindingFlags = BindingFlags.Public _
                                 Or BindingFlags.Static _
                                 Or BindingFlags.Instance

            If pTipo.IsInterface Then
                Dim propeidades As New List(Of PropertyInfo)
                propeidades.AddRange(pTipo.GetProperties(flags))
                For Each miInterface As Type In pTipo.GetInterfaces
                    '  propeidades.AddRange(miInterface.GetProperties(flags))
                    propeidades.AddRange(RecuperarPropiedades(miInterface))

                Next

                Return propeidades.ToArray

            Else


            End If





            Return pTipo.GetProperties(flags)
        End Function

        Public Shared Sub SepararPropiedadesValorRef(ByVal pPropiedades As Reflection.PropertyInfo(), ByRef pPropiedadesValor As Reflection.PropertyInfo(), ByRef pPropiedadesRef As Reflection.PropertyInfo())
            Dim p As Reflection.PropertyInfo
            Dim t As Type
            Dim alVal As New ArrayList
            Dim alRef As New ArrayList

            For Each p In pPropiedades
                t = p.PropertyType

                If EsRef(t) Then
                    alRef.Add(p)

                Else
                    alVal.Add(p)
                End If
            Next

            pPropiedadesValor = alVal.ToArray(GetType(Reflection.PropertyInfo))
            pPropiedadesRef = alRef.ToArray(GetType(Reflection.PropertyInfo))
        End Sub


        Public Shared Function EsColeccion(ByVal t As Type) As Boolean
            Dim al As ArrayList

            al = New ArrayList(t.GetInterfaces)
            If al.Contains(GetType(ICollection)) Then
                Return True
            End If

            Return False
        End Function


        Public Shared Function RecuperarCampo(ByRef pTipo As Type, ByVal nombreCampo As String) As FieldInfo
            Dim al As New ArrayList
            Dim m As MemberInfo
            Dim ms As MemberInfo()

            ms = pTipo.FindMembers(MemberTypes.Field, BindingFlags.NonPublic Or BindingFlags.Instance, Nothing, Nothing)

            For Each m In ms
                If (m.MemberType = MemberTypes.Field AndAlso m.Name.ToLower = nombreCampo) Then
                    Return m
                End If
            Next

            Return Nothing
        End Function

        Public Shared Function RecuperarCampos(ByRef pTipo As Type) As FieldInfo()
            Dim al As New ArrayList
            Dim m As MemberInfo
            Dim ms As MemberInfo()

            ms = pTipo.FindMembers(MemberTypes.Field, BindingFlags.NonPublic Or BindingFlags.Instance, Nothing, Nothing)
            'ms = pTipo.FindMembers(MemberTypes.Field, BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.Public, Nothing, Nothing)
            'ms = pTipo.FindMembers(MemberTypes.Property, BindingFlags.NonPublic Or BindingFlags.Instance, Nothing, Nothing)

            For Each m In ms
                '  Debug.WriteLine(m.Name)
                If (m.MemberType = MemberTypes.Field AndAlso m.Name.EndsWith("Event") = False) Then
                    al.Add(m)
                End If
            Next

            Return al.ToArray(GetType(FieldInfo))
        End Function

        Public Shared Sub SepararCamposValorRef(ByVal pCampos As Reflection.FieldInfo(), ByRef pCamposValor As Reflection.FieldInfo(), ByRef pCamposRef As Reflection.FieldInfo())
            Dim f As Reflection.FieldInfo
            Dim t As Type
            Dim alVal As New ArrayList
            Dim alRef As New ArrayList

            For Each f In pCampos
                t = f.FieldType
                ' Debug.WriteLine(f.Name)
                If EsRef(t) Then
                    alRef.Add(f)

                Else
                    alVal.Add(f)
                End If
            Next

            pCamposValor = alVal.ToArray(GetType(Reflection.FieldInfo))
            pCamposRef = alRef.ToArray(GetType(Reflection.FieldInfo))
        End Sub

        Public Shared Function EsRef(ByVal t As Type) As Boolean
            If (t.IsPrimitive = True OrElse t Is GetType(String) OrElse t Is GetType(Date) OrElse t.IsEnum) Then
                Return False

            Else
                Return True
            End If
        End Function
        Public Shared Function RecuperarNombreBusquedaTipo(ByRef pTipo As Type) As String


            Return pTipo.Assembly.FullName.Split(",")(0) & "." & pTipo.Name

        End Function


        Public Shared Sub RecuperarEnsambladoYTipoxRuta(ByVal pRuta As String, ByVal pNombreCompletoClase As String, ByRef pEnsamblado As Assembly, ByRef pTipo As Type)
            'Dim posicionPunto As Int64

            'posicionPunto = pNombreCompletoClase.LastIndexOf("."c)

            pEnsamblado = Assembly.LoadFile(pRuta)

            pTipo = pEnsamblado.GetType(pNombreCompletoClase)

            If (pTipo Is Nothing) Then
                Throw New ApplicationException("Error: imposible resolver el tipo")
            End If
        End Sub

        Public Shared Function obtenerLaPropiedad(ByVal pTipo As Type, ByVal pRutaPropiedad As String) As Reflection.PropertyInfo
            Dim mipi As Reflection.PropertyInfo
            If Not pTipo Is Nothing Then

                If pRutaPropiedad.Contains(".") Then

                    If pTipo Is Nothing Then
                        Return Nothing
                    End If

                    Dim partes() As String
                    partes = pRutaPropiedad.Split(".")

                    Dim miTipo As System.Type
                    Dim pi As Reflection.PropertyInfo = pTipo.GetProperty(partes(0))
                    If pi IsNot Nothing Then
                        miTipo = pi.PropertyType
                        mipi = obtenerLaPropiedad(miTipo, pRutaPropiedad.Substring(partes(0).Length + 1))


                    End If

                    Return mipi

                Else
                    mipi = pTipo.GetProperty(pRutaPropiedad)
                    If mipi Is Nothing Then
                        mipi = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarPropiedad(pTipo, pRutaPropiedad, Nothing)
                    End If

                    Return mipi
                End If
            End If

        End Function

        Public Shared Sub RecuperarEnsambladoYTipo(ByVal pNombreCompletoClase As String, ByRef pEnsamblado As Assembly, ByRef pTipo As Type)
            Dim posicionPunto As Int64

            posicionPunto = pNombreCompletoClase.IndexOf("/"c)

            'TODO:alex Por implementaar esto debe de tener el nombre del ensamblado y de la clase claramene diferenciado
            pEnsamblado = Assembly.Load(pNombreCompletoClase.Substring(0, posicionPunto))
            'pEnsamblado = Assembly.LoadFrom(pNombreCompletoClase.Substring(0, posicionPunto) & ".dll")

            Dim nombreClase As String = pNombreCompletoClase.Substring(posicionPunto + 1)

            pTipo = pEnsamblado.GetType(nombreClase)

            If (pTipo Is Nothing) Then


                Throw New ApplicationException("Error: imposible resolver el tipo:" & pNombreCompletoClase & " en el ensamblado:" & pEnsamblado.FullName)
            End If
        End Sub
        Public Shared Function TipoToString(ByVal ptipo As System.Type) As String
            Return ptipo.Assembly.FullName & "/" & ptipo.FullName
        End Function

        Public Shared Sub RecuperarEnsambladoYTipo(ByVal pNombreEnsamblado As String, ByVal pNombreCompletoClase As String, ByRef pEnsamblado As Assembly, ByRef pTipo As Type)
            pEnsamblado = Assembly.Load(pNombreEnsamblado)
            pTipo = pEnsamblado.GetType(pNombreCompletoClase)

            If (pTipo Is Nothing) Then
                Throw New ApplicationException("Error: imposible resolver el tipo")
            End If
        End Sub

        Public Shared Function RecuperarMetodo(ByRef pTipo As System.Type, ByVal pNombreMetodo As String, ByRef pEntidadQueLoDeclara As System.Type, ByVal pParametros As System.Type()) As MethodInfo
            Dim al As New ArrayList

            Dim m As System.Reflection.MemberInfo
            Dim ms As System.Reflection.MemberInfo()
            Dim metodo As MethodInfo

            Dim alParametrosEntrada As New ArrayList
            Dim alParametrosTipo As New ArrayList

            Dim parametro As ParameterInfo
            Dim t As System.Type
            Dim contineParametros As Boolean

            If (pParametros IsNot Nothing) Then
                alParametrosEntrada.AddRange(pParametros)
            End If

            ms = pTipo.FindMembers(MemberTypes.Method, BindingFlags.Instance Or BindingFlags.Public, Nothing, Nothing)

            For Each m In ms
                If (m.MemberType = MemberTypes.Method AndAlso m.Name = pNombreMetodo) Then
                    metodo = m

                    alParametrosTipo.AddRange(metodo.GetParameters)
                    contineParametros = True

                    For Each t In alParametrosEntrada
                        contineParametros = False

                        For Each parametro In alParametrosTipo
                            If (parametro.ParameterType Is t) Then
                                contineParametros = True
                                Exit For
                            End If
                        Next

                        If (contineParametros = False) Then
                            Exit For
                        End If
                    Next

                    If (contineParametros = True) Then
                        Return m
                    End If
                End If
            Next

            Return Nothing
        End Function


        Public Shared Function ObtenerTipoFijado(ByVal ptipo As System.Type, ByRef fijacion As FijacionDeTipoDN) As System.Type
            Dim colValidable As IValidable
            Dim validadorTipos As ValidadorTipos

            fijacion = FijacionDeTipoDN.Indefinida


            If (ptipo.GetInterface("IEnumerable", True) IsNot Nothing) OrElse TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(ptipo) Then

                ' puede que se trate de una coleccion validable o de una coleccion generica
                'ptipo.HasGenericArguments 
                If ptipo.IsGenericType OrElse ptipo.BaseType.IsGenericType Then
                    'se trata de una coleccion generica o una clase que hereda de ella, o una huella tipada
                    If ptipo.IsGenericType Then
                        If ptipo.GetGenericArguments.Length > 1 Then
                            Throw New ApplicationException("La colección tiene demasiados parametros genericos")
                        End If
                        If ptipo.BaseType.IsGenericType Then
                            ObtenerTipoFijado = ptipo.BaseType.GetGenericArguments(0)
                        Else
                            ObtenerTipoFijado = ptipo.GetGenericArguments(0)
                        End If
                        fijacion = FijacionDeTipoDN.ColeccionGenerica

                    Else
                        If ptipo.BaseType.GetGenericArguments.Length > 1 Then
                            Throw New ApplicationException("La colección tiene demasiados parametros genericos")
                        End If
                        ObtenerTipoFijado = ptipo.BaseType.GetGenericArguments(0)
                        fijacion = FijacionDeTipoDN.ColeccionGenerica

                    End If
                Else
                    ' se trata de una coleccion validable o de una ilist(of xxxxx)
                    Try
                        colValidable = Activator.CreateInstance(ptipo)

                    Catch ex As Exception
                        Debug.Write(ptipo.ToString)
                        Throw
                    End Try

                    validadorTipos = colValidable.Validador
                    ObtenerTipoFijado = validadorTipos.Tipo
                    fijacion = FijacionDeTipoDN.ColeccionValidable
                End If



            Else


                'If ptipo.IsGenericType OrElse ptipo.BaseType.IsGenericType Then

                '    If ptipo.IsGenericType Then
                '        If ptipo.GetGenericArguments.Length > 1 Then
                '            Throw New ApplicationException("La entidad tipo tiene demasiados parametros genericos")
                '        End If
                '        If ptipo.BaseType.IsGenericType Then
                '            ObtenerTipoFijado = ptipo.BaseType.GetGenericArguments(0)
                '        Else
                '            ObtenerTipoFijado = ptipo.GetGenericArguments(0)
                '        End If
                '        fijacion = FijacionDeTipoDN.ColeccionGenerica

                '    Else
                '        If ptipo.BaseType.GetGenericArguments.Length > 1 Then
                '            Throw New ApplicationException("La entidad tipo tiene demasiados parametros genericos")
                '        End If
                '        ObtenerTipoFijado = ptipo.BaseType.GetGenericArguments(0)
                '        fijacion = FijacionDeTipoDN.ColeccionGenerica

                '    End If


                'Else

                fijacion = FijacionDeTipoDN.EnLaClase
                ObtenerTipoFijado = ptipo

                'End If
            End If






        End Function
        Public Shared Function EsEntidadBaseNoEntidadDN(ByVal pTipo As System.Type) As Boolean

            If HeredaDe(pTipo, GetType(Framework.DatosNegocio.EntidadBaseDN)) Then

                If HeredaDe(pTipo, GetType(Framework.DatosNegocio.EntidadDN)) Then
                    Return False
                Else
                    Return True

                End If
            Else
                Return False
            End If
        End Function


        Public Shared Function EsHuellaTipada(ByVal pTipo As System.Type) As Boolean
            If EsHuella(pTipo) Then


                If pTipo.BaseType.FullName.Contains(GetType(Framework.DatosNegocio.HuellaEntidadTipadaDN(Of )).FullName) Then
                    Return True
                Else

                    Return False
                End If


            Else
                Return False
            End If
        End Function
        Public Shared Function EsHuellaNoTipada(ByVal pInstancia As Object) As Boolean
            Return EsHuellaNoTipada(pInstancia.GetType)
        End Function
        Public Shared Function EsHuellaNoTipada(ByVal pTipo As System.Type) As Boolean
            Return EsHuella(pTipo) AndAlso Not EsHuellaTipada(pTipo)
        End Function
        Public Shared Function EsHuellaNoTipadaOaInterface(ByVal pTipo As System.Type) As Boolean



            If EsHuella(pTipo) Then

                If Not EsHuellaTipada(pTipo) Then
                    Return True
                End If
                Dim tipofijacion As Framework.TiposYReflexion.DN.FijacionDeTipoDN
                If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.ObtenerTipoFijado(pTipo, tipofijacion).IsInterface Then
                    Return True
                End If

                Return False
            Else
                Return False
            End If
        End Function

        Public Shared Function EsHuella(ByVal pTipo As System.Type) As Boolean
            If GetType(Framework.DatosNegocio.HEDN) Is pTipo Then
                Return True
            End If
            Return HeredaDe(pTipo, GetType(Framework.DatosNegocio.HEDN))
        End Function
        Public Shared Function EsEntidaTipo(Of T)(ByVal pTipo As System.Type) As Boolean
            Return HeredaDe(pTipo, GetType(Framework.DatosNegocio.EntidadTipoDN(Of T)))
        End Function
        Public Shared Function EsHuellaCacheable(ByVal pTipo As System.Type) As Boolean
            Return HeredaDe(pTipo, GetType(Framework.DatosNegocio.HuellaEntidadCacheableDN))
        End Function

        Public Shared Function HeredaDe(ByVal pTipo As System.Type, ByVal pTipoBase As System.Type) As Boolean
            If pTipo.BaseType Is Nothing OrElse pTipo.BaseType Is GetType(Object) Then
                Return False
            Else
                If pTipo.BaseType Is pTipoBase Then
                    Return True
                Else
                    Return HeredaDe(pTipo.BaseType, pTipoBase)
                End If
            End If
        End Function

        Public Shared Function Implementa(ByVal pTipo As System.Type, ByVal pInterace As System.Type) As Boolean


            Return Not pTipo.GetInterface(pInterace.Name) Is Nothing




        End Function

#End Region

    End Class
End Namespace

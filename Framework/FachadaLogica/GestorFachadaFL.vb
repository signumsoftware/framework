Imports Framework.LogicaNegocios.Transacciones
#Region "Importaciones"

Imports System.Collections.Generic
Imports System.Reflection

Imports Framework.Usuarios.DN

Imports Framework.TiposYReflexion.DN

#End Region

''' <summary>
''' Esta clase representa un gestor global que permite obtener todos los metodos declarados por una
''' fachada completa
''' </summary>
Public Class GestorFachadaFL

#Region "Metodos"

    ''' <summary>
    ''' Metodo que publica en la BD todos los metodos publicos de todas las clases de fachada declaradas en una DLL
    ''' </summary>
    ''' <param name="pEnsamblado">Nombre del ensamblado del que queremos obtener los metodos de fachada</param>
    ''' <param name="pRecurso">Recurso que nos identifica el almacen donde se va a publicar la lista de metodos de la fachada</param>
    ''' <remarks></remarks>
    Public Shared Sub PublicarFachada(ByVal pEnsamblado As String, ByVal pRecurso As IRecursoLN)




        Using tr As New Transaccion


            Dim tipos As Type()
            Dim ensamblado As Assembly
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            Dim metodos As New List(Of MethodInfo)()
            Dim metodoSistema As MetodoSistemaDN

            '  Using New CajonHiloLN(pRecurso)
            'Cargamos el ensamblado y obtenemos sus tipos
            ensamblado = Assembly.Load(pEnsamblado)
            tipos = ensamblado.GetExportedTypes()

            'Obtenemos el tipo de la clase y los metodos que declara
            For Each tipo As Type In tipos
                metodos.AddRange(tipo.GetMethods(BindingFlags.DeclaredOnly Or BindingFlags.Public Or BindingFlags.Instance))
            Next

            'Una vez tenemos la lista de permisos, la publicamos en la BD, comprobando antes si ya existe
            Dim msLN As Framework.Usuarios.LN.MetodoSistemaLN
            For Each metodo As MethodInfo In metodos
                msLN = New Framework.Usuarios.LN.MetodoSistemaLN()
                metodoSistema = msLN.CrearMetodoSistema(metodo)

                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                gi.Guardar(metodoSistema)
            Next

            tr.Confirmar()

        End Using






        '  End Using

    End Sub


    ''' <summary>
    ''' Este metodo recupera una lista con todos los métodos públicos que una clase de fachada
    ''' expone al exterior
    ''' </summary>
    ''' <returns>La lista de metodos que la fachada expone</returns>
    <Obsolete()> _
    Public Shared Function RecuperarMetodosSistema(ByVal pTipo As Type) As List(Of MetodoSistemaDN)
        Dim metodos As MethodInfo()
        Dim metodo As MetodoSistemaDN
        Dim lista As List(Of MetodoSistemaDN)
        Dim i As Integer

        'Obtenemos el tipo de la clase y los metodos que declara
        metodos = pTipo.GetMethods(BindingFlags.DeclaredOnly Or BindingFlags.Public Or BindingFlags.Instance)

        'Para cada metodo generamos su MetodoSistemaDN y lo guardamos en la lista de metodos de la fachada
        lista = New List(Of MetodoSistemaDN)

        For i = 0 To metodos.Length - 1

            metodo = New MetodoSistemaDN(metodos(i)) 'todo: ojo con esto como se diferencian los metodos sobrecargados y no se duplican en la base de datos

            If (metodo.Nombre.Contains("_") = False) Then
                lista.Add(metodo)
            End If
        Next

        Return lista

    End Function


    Public Shared Function PublicarMetodos(ByVal pEnsamblado As String, ByVal pRecurso As IRecursoLN) As ColVinculoMetodoDN

        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        '   Dim vm As TiposYReflexion.DN.VinculoMetodoDN

        'For Each vm In Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarMetodosDeEnsamblado(pEnsamblado)




        'Next
        Dim col As ColVinculoMetodoDN = Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.RecuperarMetodosDeEnsamblado(pEnsamblado)

        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, pRecurso)
        gi.Guardar(col)
        Return col

    End Function


#End Region

End Class


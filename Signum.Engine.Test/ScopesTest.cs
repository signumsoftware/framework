using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum;
using System.Data.SqlClient;
using System.Data;
using Signum.Engine;


namespace Signum.Engine.Test
{
    [TestClass]
    public class ScopesTest
    {
        public ScopesTest()
        {
            ConnectionScope.Default = new Connection("Data Source=INSPIRON;Initial Catalog=LINQ3Capas;Integrated Security=True", null);
            Administrator.RemoveAllScript().ToSimple().ExecuteNonQuery();
            Executor.ExecuteNonQuery(
@"CREATE TABLE Persona (id INT PRIMARY KEY, nombre nvarchar(200));
INSERT INTO Persona VALUES (73,'Juan');");
        }

        [TestMethod]
        public void ScopeTest()
        {
            using (Transaction tr = new Transaction())
            {
                SetNombre(73, "Olmo");
                tr.Commit();
            }

            using (Transaction tr = new Transaction())
            {
                Assert.AreEqual(GetNombre(73), "Olmo");

                SetNombre(73, "Olmiano");

                Assert.AreEqual(GetNombre(73), "Olmiano");

                tr.Commit();
            }

            using (Transaction tr = new Transaction())
            {
                Assert.AreEqual(GetNombre(73), "Olmiano");

                SetNombre(73, "John Connor");

                //no se confirma 
            }

        

            using (Transaction tr = new Transaction())
            {
                using (Transaction tr2 = new Transaction())
                {
                    Assert.AreEqual(GetNombre(73), "Olmiano");

                    SetNombre(73, "Olmo");

                    Assert.AreEqual(GetNombre(73), "Olmo");

                    tr2.Commit();
                }

                tr.Commit();
            }

            using (Transaction tr = new Transaction())
            {

            }

            using (Transaction tr = new Transaction())
            {
                tr.Commit(); 
            }

        }

        [TestMethod]
        public void ScopeNoConfirmadoTest()
        {
            string nombre = null;
            try
            {
                using (Transaction tr = new Transaction())
                {
                    using (Transaction tr2 = new Transaction())
                    {
                        nombre = GetNombre(73);

                        SetNombre(73, "Marciano");

                        Assert.AreEqual(GetNombre(73), "Marciano");

                        //no se confirma
                    }

                    tr.Commit();
                }
            }
            catch { }

            using (Transaction tr = new Transaction())
            {
                Assert.AreEqual(GetNombre(73), nombre);

                tr.Commit(); 
            }
        }


        private string GetNombre(int id)
        {
            SqlCommand cm = new SqlCommand("SELECT Nombre from Persona where id = @id");
            cm.Parameters.AddWithValue("id", id);
            return (string)cm.ExecuteScalar();
        }

        private void SetNombre(int id, string nombre)
        {
            SqlCommand cm = new SqlCommand("UPDATE Persona SET Nombre = @nombre where id = @id");
            cm.Parameters.AddWithValue("nombre", nombre);
            cm.Parameters.AddWithValue("id", id);
            cm.ExecuteNonQuery();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient; 

namespace AlmacenEg
{
    public class Conexion
    {

        private static string cadena = "Server=localhost\\SQLEXPRESS;Database=AlmacenEg;Integrated Security=true;";

        public static SqlConnection ObtenerConexion()
        {
            SqlConnection conexion = new SqlConnection(cadena);
            try
            {
                conexion.Open(); 
            }
            catch (Exception ex)
            {
                throw new Exception("No se pudo conectar a la base de datos: " + ex.Message);
            }
            return conexion;
        }
    }
}
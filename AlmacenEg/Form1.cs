using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlmacenEg
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        // BOTON INICIAR SESION: donde los usuarios, el admin y los cajeros ponen sus datos, si son validos lo envia directo al menú de ventas.
        private void btnIngresar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsuario.Text) || string.IsNullOrWhiteSpace(txtContrasena.Text))
            {
                MessageBox.Show("Por favor, complete todos los campos.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "SELECT Rol, Turno FROM Usuarios WHERE NombreUsuario = @usuario AND Contrasena = @password";

            try
            {
                using (SqlConnection conexion = Conexion.ObtenerConexion())
                {
                    using (SqlCommand comando = new SqlCommand(query, conexion))
                    {
                        
                        comando.Parameters.AddWithValue("@usuario", txtUsuario.Text.Trim());
                        comando.Parameters.AddWithValue("@password", txtContrasena.Text.Trim());

                        using (SqlDataReader lector = comando.ExecuteReader())
                        {
                            if (lector.Read())
                            {
                               
                                string rol = lector["Rol"].ToString();
                                string turno = lector["Turno"] != DBNull.Value ? lector["Turno"].ToString() : "N/A";

                                MessageBox.Show($"Sesión iniciada con éxito.\nRol: {rol}\nTurno: {turno}",
                                                "Acceso Concedido", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                FrmPrincipal principal = new FrmPrincipal();
                                principal.Show();

                                this.Hide(); 
                            }
                            else
                            {
                                MessageBox.Show("Usuario o contraseña incorrectos.", "Error de acceso", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error crítico de conexión: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //BOTON DE ENTRAR A LOS REPORTES: aqui se entra directamente a la parte de los reportes, cuidando que la persona qque entre sea solo el administrador.
        private void button1_Click(object sender, EventArgs e)
        {
            string usuarioIngresado = txtUsuario.Text.Trim(); 
            string claveIngresada = txtContrasena.Text.Trim(); 

            string cadenaConexion = "Server=localhost\\SQLEXPRESS;Database=AlmacenEg;Integrated Security=true;";

            try
            {
                using (SqlConnection conexion = new SqlConnection(cadenaConexion))
                {
                    conexion.Open();

                    string query = "SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario = @usuario AND Contrasena = @clave AND Rol = 'Administrador'";

                    using (SqlCommand cmd = new SqlCommand(query, conexion))
                    {
                        cmd.Parameters.AddWithValue("@usuario", usuarioIngresado);
                        cmd.Parameters.AddWithValue("@clave", claveIngresada);

                        int resultado = (int)cmd.ExecuteScalar();

                        if (resultado > 0)
                        {
                            FrmReportes frm = new FrmReportes();
                            this.Hide();
                            frm.ShowDialog();
                            this.Show();
                        }
                        else
                        {
                            MessageBox.Show("Solo el administrador autorizado puede entrar a los reportes.", "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al validar el acceso: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
 
}

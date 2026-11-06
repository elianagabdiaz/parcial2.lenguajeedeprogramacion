using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace AlmacenEg
{
    public partial class FrmReportes : Form
    {
        private string cadenaConexion = "Server=localhost\\SQLEXPRESS;Database=AlmacenEg;Integrated Security=true;";

        public FrmReportes()
        {
            InitializeComponent();
            CargarOpcionesReporte();
        }

        private void CargarOpcionesReporte()
        {
            // BOTON DE OPCIONES DE REPORTES: llena el menú con las opciones disponibles
            cmbTipoReporte.Items.Clear();
            cmbTipoReporte.Items.Add("Cuadre del Día (Ventas de hoy)");
            cmbTipoReporte.Items.Add("Ventas por Cajero");
            cmbTipoReporte.Items.Add("Inventario en Stock");
            cmbTipoReporte.Items.Add("Facturas por Tipo (Contado / Crédito)");
            cmbTipoReporte.SelectedIndex = 0;
        }

        private void btnGenerar_Click(object sender, EventArgs e)
        {
            //BOTON GENERAR EL REPORTE: es el que muestra en la tabla los datos de la opcion
            string query = "";

            switch (cmbTipoReporte.SelectedIndex)
            {
                case 0: 
                    query = "SELECT Cajero, IdFactura, RIGHT('00000' + CAST(IdFactura AS VARCHAR), 5) AS NumeroFactura, Fecha, Hora, TotalAPagar FROM Facturas WHERE CONVERT(date, Fecha) = CONVERT(date, GETDATE())";
                    break;

                case 1: 
                    query = "SELECT Cajero, COUNT(*) AS CantidadFacturas, SUM(TotalAPagar) AS TotalVendido FROM Facturas GROUP BY Cajero";
                    break;

                case 2: 
                    query = "SELECT IdProducto, NombreProducto, Precio, Stock FROM Productos";
                    break;

                case 3: 
                    query = "SELECT IdFactura, NumeroFactura, TipoVenta, Cajero, TotalAPagar, Fecha FROM Facturas ORDER BY TipoVenta";
                    break;

                default:
                    MessageBox.Show("Selecciona un reporte válido.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
            }

            try
            {
                using (SqlConnection conexion = new SqlConnection(cadenaConexion))
                {
                    conexion.Open();
                    using (SqlDataAdapter adaptador = new SqlDataAdapter(query, conexion))
                    {
                        DataTable dt = new DataTable();
                        adaptador.Fill(dt);
                        dgvReportes.DataSource = dt;

                        if (dt.Rows.Count == 0)
                        {
                            MessageBox.Show("No se encontraron registros para este reporte.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar el reporte: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvReportes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
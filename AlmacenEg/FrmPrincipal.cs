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
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;

namespace AlmacenEg
{
    public partial class FrmPrincipal : Form
    {
        private DataTable tablaDetalle = new DataTable();
        private decimal totalGeneral = 0;
        private decimal totalItbis = 0;

        public FrmPrincipal()
        {
            InitializeComponent();
            InicializarTablaDetalle();
            CargarCajeros();

        }

        private void InicializarTablaDetalle()
        {
            tablaDetalle.Columns.Add("Código", typeof(string));
            tablaDetalle.Columns.Add("Producto", typeof(string));
            tablaDetalle.Columns.Add("Precio", typeof(decimal));
            tablaDetalle.Columns.Add("Cantidad", typeof(int));
            tablaDetalle.Columns.Add("Subtotal", typeof(decimal));

            dgvDetalleVenta.DataSource = tablaDetalle;
        }

        
        private void CargarCajeros()
        {
            //BOTON CARGAR CAJEROS: es la lista de cajeros que se encuentran disponibles
            try
            {
                using (SqlConnection conexion = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=AlmacenEg;Integrated Security=true;"))
                {
                    conexion.Open();
                    string query = "SELECT NombreUsuario FROM Usuarios WHERE Rol = 'Cajero'";

                    using (SqlCommand cmd = new SqlCommand(query, conexion))
                    {
                        SqlDataAdapter adaptador = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adaptador.Fill(dt);

                        cmbCajero.DataSource = dt;
                        cmbCajero.DisplayMember = "NombreUsuario";
                        cmbCajero.ValueMember = "NombreUsuario";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los cajeros: " + ex.Message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnBuscarProducto_Click(object sender, EventArgs e)
        {
            //BOTON BUSCAR PRODUCTO: busca el producto cuando se agrega el ID del mismo

            if (string.IsNullOrWhiteSpace(txtCodigoProducto.Text) || string.IsNullOrWhiteSpace(txtCantidad.Text))
            {
                MessageBox.Show("Por favor, ingrese el código del producto y la cantidad.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            if (!int.TryParse(txtCantidad.Text.Trim(), out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Por favor, ingrese una cantidad válida.", "Error de cantidad", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string codigoBuscado = txtCodigoProducto.Text.Trim();
            string query = "SELECT IdProducto, NombreProducto, Precio, Stock FROM Productos WHERE IdProducto = @codigo OR NombreProducto LIKE @nombre";

            try
            {
                using (SqlConnection conexion = Conexion.ObtenerConexion())
                {
                    using (SqlCommand comando = new SqlCommand(query, conexion))
                    {
                        comando.Parameters.AddWithValue("@codigo", codigoBuscado);
                        comando.Parameters.AddWithValue("@nombre", "%" + codigoBuscado + "%");

                        using (SqlDataReader lector = comando.ExecuteReader())
                        {
                            if (lector.Read())
                            {
                                string id = lector["IdProducto"].ToString();
                                string nombre = lector["NombreProducto"].ToString();
                                decimal precio = Convert.ToDecimal(lector["Precio"]);
                                int stock = Convert.ToInt32(lector["Stock"]);

                                if (cantidad > stock)
                                {
                                    MessageBox.Show($"Stock insuficiente. Solo quedan {stock} unidades disponibles.", "Stock agotado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }

                                decimal subtotal = precio * cantidad;

                                tablaDetalle.Rows.Add(id, nombre, precio, cantidad, subtotal);

                                decimal itbisLinea = subtotal * 0.18m;
                                totalItbis += itbisLinea;
                                totalGeneral += (subtotal + itbisLinea);

                                txtTotal.Text = totalGeneral.ToString("N2");
                                txtItbis.Text = totalItbis.ToString("N2");

                                txtCodigoProducto.Clear();
                                txtCantidad.Clear();
                                txtCodigoProducto.Focus();
                            }
                            else
                            {
                                MessageBox.Show("No se encontró ningún producto con ese código.", "Sin resultados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al procesar el producto: " + ex.Message, "Error crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void btnCobrar_Click(object sender, EventArgs e)
        {
            //BOTON COBRAR: una de las partes más dificiles, esta parte procesa la transaccion completa
            if (tablaDetalle.Rows.Count == 0)
            {
                MessageBox.Show("No hay productos en la venta actual.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int idVentaGenerado = 0;
            string numeroFacturaGenerado = "00001";

            // calcula los valores basados en lo que está en pantalla
            decimal totalPagar = 0;
            decimal itbis = 0;

            decimal.TryParse(txtTotal.Text, out totalPagar);
            decimal.TryParse(txtItbis.Text, out itbis);
            decimal subtotalBruto = totalPagar - itbis; 

         
            using (SqlConnection conexion = new SqlConnection("Server=localhost\\SQLEXPRESS;Database=AlmacenEg;Integrated Security=true;"))
            {
                conexion.Open();
                using (SqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        string queryFactura = "INSERT INTO Facturas (NumeroFactura, Fecha, Hora, TipoVenta, Cajero, TotalBruto, TotalItbis, TotalAPagar, MontoPagado, Devuelta) " +
                                              "OUTPUT INSERTED.IdFactura " +
                                              "VALUES (@numeroFactura, @fecha, @hora, @tipoVenta, @cajero, @totalBruto, @totalItbis, @totalAPagar, @montoPagado, @devuelta)";

                        using (SqlCommand cmdVenta = new SqlCommand(queryFactura, conexion, transaccion))
                        {
                            cmdVenta.Parameters.AddWithValue("@numeroFactura", numeroFacturaGenerado);
                            cmdVenta.Parameters.AddWithValue("@fecha", DateTime.Now.Date);
                            cmdVenta.Parameters.AddWithValue("@hora", DateTime.Now.ToString("HH:mm:ss"));
                            cmdVenta.Parameters.AddWithValue("@tipoVenta", "Contado");
                            cmdVenta.Parameters.AddWithValue("@cajero", cmbCajero.Text);
                            cmdVenta.Parameters.AddWithValue("@totalBruto", subtotalBruto);
                            cmdVenta.Parameters.AddWithValue("@totalItbis", itbis);
                            cmdVenta.Parameters.AddWithValue("@totalAPagar", totalPagar);
                            cmdVenta.Parameters.AddWithValue("@montoPagado", totalPagar);
                            cmdVenta.Parameters.AddWithValue("@devuelta", 0.00m);

                            idVentaGenerado = (int)cmdVenta.ExecuteScalar();
                        }

                        foreach (DataRow fila in tablaDetalle.Rows)
                        {
                            string idProducto = fila[0].ToString();
                            int cantidadComprada = Convert.ToInt32(fila[3]);

                            string queryStock = "UPDATE Productos SET Stock = Stock - @cantidad WHERE IdProducto = @idProducto";
                            using (SqlCommand cmdStock = new SqlCommand(queryStock, conexion, transaccion))
                            {
                                cmdStock.Parameters.AddWithValue("@cantidad", cantidadComprada);
                                cmdStock.Parameters.AddWithValue("@idProducto", idProducto);
                                cmdStock.ExecuteNonQuery();
                            }
                        }

                        transaccion.Commit();

                        // genera la factura automáticamente leyendo los datos desde la tabla en memoria
                        GenerarRecibo(idVentaGenerado);

                        MessageBox.Show($"¡Venta procesada con éxito! Factura #${idVentaGenerado.ToString("D5")} guardada y PDF generado en C:\\AlmacénEgFactura.",
                                        "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        tablaDetalle.Rows.Clear();
                    }
                    catch (Exception ex)
                    {
                        transaccion.Rollback();
                        MessageBox.Show("Error al procesar la venta: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void GenerarRecibo(int idVenta)
        {
            try
            {
                //BOTON GENERAR RECIBO: la mas dicil, se usa la librería de iText para darle forma a la factura en pdf

                string rutaCarpeta = @"C:\AlmacénEgFactura";

                if (!Directory.Exists(rutaCarpeta))
                {
                    Directory.CreateDirectory(rutaCarpeta);
                }

                string nombreArchivo = Path.Combine(rutaCarpeta, $"Factura_{idVenta.ToString("D5")}.pdf");

                PdfWriter escritor = new PdfWriter(nombreArchivo);
                PdfDocument pdfDoc = new PdfDocument(escritor);
                Document documento = new Document(pdfDoc);


                Paragraph titulo = new Paragraph("AlmacénEg")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(24);
                documento.Add(titulo);

                Paragraph subtitulo = new Paragraph("Santo Domingo, Repíblica Dominicana\nby: Eliana Díaz")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(10);
                documento.Add(subtitulo);

                documento.Add(new Paragraph("\n"));

                documento.Add(new Paragraph($"No. Factura: #{idVenta.ToString("D5")}"));
                documento.Add(new Paragraph($"Fecha y Hora: {DateTime.Now}"));
                documento.Add(new Paragraph($"Cajero: {cmbCajero.Text}"));
                documento.Add(new Paragraph("------------------------------------------------------------------------------------------------------------------"));

                Table tabla = new Table(4);
                tabla.SetWidth(UnitValue.CreatePercentValue(100));

                tabla.AddHeaderCell(new Cell().Add(new Paragraph("Descripción")));
                tabla.AddHeaderCell(new Cell().Add(new Paragraph("Cant")));
                tabla.AddHeaderCell(new Cell().Add(new Paragraph("Precio")));
                tabla.AddHeaderCell(new Cell().Add(new Paragraph("Subtotal")));

                foreach (DataRow fila in tablaDetalle.Rows)
                {
                    string producto = fila[1].ToString();
                    string cantidad = fila[3].ToString();
                    string precio = Convert.ToDecimal(fila[2]).ToString("N2");
                    string subtotal = Convert.ToDecimal(fila[4]).ToString("N2");

                    tabla.AddCell(new Cell().Add(new Paragraph(producto)));
                    tabla.AddCell(new Cell().Add(new Paragraph(cantidad)));
                    tabla.AddCell(new Cell().Add(new Paragraph($"${precio}")));
                    tabla.AddCell(new Cell().Add(new Paragraph($"${subtotal}")));
                }

                documento.Add(tabla);

                documento.Add(new Paragraph("\n"));

                Paragraph totales = new Paragraph()
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetFontSize(12);

                totales.Add($"ITBIS: ${txtItbis.Text}\n");
                totales.Add($"TOTAL A PAGAR: ${txtTotal.Text}");

                documento.Add(totales);

                documento.Add(new Paragraph("\n\n"));

                Paragraph pie = new Paragraph("¡Gracias por preferirnos!")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(9);
                documento.Add(pie);

                documento.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("La venta se guardó en la base de datos, pero hubo un error al exportar el PDF: " + ex.Message,
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }
    }



}
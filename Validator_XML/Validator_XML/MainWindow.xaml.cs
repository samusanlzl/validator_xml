using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Validator_XML
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        private void Button_Click_xml(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textXML.Text = dialog.SelectedPath;
            }
        }

        private void Button_Click_xsd(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textXSD.Text = dialog.SelectedPath;
            }
        }
        //static bool validation = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            if (textXML.Text.Length == 0)
            {
                textLog.Text = "Seleccione una carpeta con archivos en formato compatible (.xml)\n";
                textLog.Text += "la carpeta de ficheros XML no se ha seleccionado\n";
                return;
            }
            if (textXSD.Text.Length == 0)
            {
                textLog.Text = "Seleccione una carpeta con archivos en formato compatible (.xsd)\n";
                textLog.Text += "la carpeta de ficheros XSD no se ha seleccionado\n";
                return;
            }
            //Obtenemos los path de los archivos xml y xsd de todos los directorios incluidos en los path almacenados en textXML.Text y textXSD.Text
            string[] xml_files = Directory.GetFiles(textXML.Text, "*.xml", SearchOption.AllDirectories);
            string[] xsd_files = Directory.GetFiles(textXSD.Text, "*.xsd", SearchOption.AllDirectories);
            if (xml_files.Length == 0)
            {
                textLog.Text = "Seleccione una carpeta con archivos en formato compatible (.xml)\n";
                textLog.Text += "la carpeta " + textXML.Text + " no tiene ficheros XML.\n";
                return;
            }
            if (xsd_files.Length == 0)
            {
                textLog.Text = "Seleccione una carpeta con archivos en formato compatible (.xsd)\n";
                textLog.Text += "la carpeta " + textXSD.Text + " no tiene ficheros XSD.\n";
                return;
            }
            
            // Creamos un diccionario que utilizará como clave el nombre del fichero xsd sin la extensión,
            //   y como valor un array de strings que contienen el path del xsd en la primera posición 
            //   y el path de los xml a validar con ese xsd en el resto de item de la lista
            var xml_xsd_files = xsd_files.ToDictionary(x => Path.GetFileNameWithoutExtension(x), x => new List<string>(){x});
            string xml_filename, xml_filename_without_digits;
            textLog.Text = ""; // Vaciamos el log
            foreach (string xml_path in xml_files)
            {
                xml_filename = Path.GetFileNameWithoutExtension(xml_path);
                
                // Comprobamos si el nombre del archivo xml tiene digitos
                if (xml_filename.Any(char.IsDigit)){
                    // Quitamos los digitos del nombre del xml para emparejarlo con su xsd correspondiente
                    xml_filename_without_digits = Regex.Replace(xml_filename, @"[\d-]", string.Empty);
                    // Otra forma de quitar los digitos del string (no sé cual será más rápida)
                    //char[] numbers = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
                    //int index_of_number = xml_filename.IndexOfAny(numbers);
                    //textLog.Text += "index es " + index_of_number + "  xml_filename: "+ xml_filename.Substring(0,index_of_number)+ "\n";
                }
                else
                {
                    xml_filename_without_digits = xml_filename;
                }
                // Si el diccionario (xml_xsd_files) tiene una key (las keys son los nombres de los archivos xsd sin extensión) con el mismo nombre que 
                //  el archivo xml (xml_filename_without_digits) entonces entendemos que podemos incluir el xml para validarlo
                if (xml_xsd_files.ContainsKey(xml_filename_without_digits))
                {
                    xml_xsd_files[xml_filename_without_digits].Add(xml_path);
                    textLog.Text += "Archivo xml añadido para ser validado: " + xml_filename + ".xml\n";
                }
                else
                {
                    textLog.Text += "El archivo: " + xml_filename_without_digits + " ("+ xml_filename + ") no tiene un archivo xsd con el mismo nombre para ser validado.\n";
                }
            }
            textLog.Text += "\n";
            string xsd_path;
            // Recorremos el diccionario (xml_xsd_files) para ir validando los xml que hemos añadido
            foreach (KeyValuePair<string, List<string>> item in xml_xsd_files)
            {
                if (item.Key.Length > 1 && item.Value.Count() > 1)
                {
                    xsd_path = item.Value.First(); // El primer path del array de string corresponde al del archivo xsd
                    // Recorremos todos los path (menos el primero que es el del archivo xsd) guardados en el item.Value que corresponden a los xml
                    foreach(string xml_path in item.Value.Skip(1))
                    {
                        textLog.Text += "Validando " + Path.GetFileName(xml_path) + " con " + item.Key + ".xsd\n";
                        //textLog.Text += "Validando " + xml_path + " con " + xsd_path + "\n";
                        XmlSchemaSet schema = new XmlSchemaSet();
                        schema.Add("", xsd_path);
                        try
                        {
                            XmlReader rd = XmlReader.Create(xml_path);
                            XDocument doc = XDocument.Load(rd);
                            // Intentamos validar los xml con su xsd correspondiente
                            doc.Validate(schema, null);
                            textLog.Text += "Se ha validado correctamente.\n";
                        }
                        catch (Exception ex)
                        {
                            textLog.Text += "Ha ocurrido un error en la validación.\n";
                            textLog.Text += "Detalles del error: " + ex.Message + "\n";
                        }
                        textLog.Text += "\n";
                    }
                }
            }
        }

        private void Button_Click_File_Rep(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textRep.Text = dialog.FileName;
            }
        }
        // Método para eliminar las filas no duplicadas de un xml
        private bool Drop_No_Duplicated(string key_name, string path_file, string output_file)
        {
            XmlReader rd = XmlReader.Create(path_file);
            XDocument doc;
            try
            {
                doc = XDocument.Load(rd);

                textLog.Text = "Buscando valores repetidos para el campo: " + key_name + "\n";
                var grouped_duplicates = doc.Descendants(key_name).GroupBy(x => x.Value).Where(g => g.Count() > 1);
                var grouped = doc.Descendants(key_name).GroupBy(x => x.Value).Where(g => g.Count() <= 1);
                textLog.Text += "Búsqueda terminada\n";
                textLog.Text += "Número de valores repetidos: " + grouped_duplicates.Count() + "\n";
                if (grouped_duplicates.Count() >= 1)
                {
                    foreach (var groupItem in grouped)
                    {
                        foreach (var item in groupItem)
                        {
                            item.Parent.Remove();
                        }
                    }
                }
                else
                {
                    textLog.Text += "Todo correcto, no hay valores repetidos para el campo: " + key_name + "\n";
                }
                foreach (XElement element in doc.Elements())
                {
                    foreach (XElement child in element.Elements().ToList())
                    {
                        textLog.Text += "Resultado: " + child.ToString() + "\n";
                    }
                }
                doc.Save(output_file);
                return true;
            }
            catch (Exception ex)
            {
                textLog.Text += "Seleccione un archivo compatible, el archivo " + Path.GetFileName(textRep.Text) + " no es compatible.\n";
                textLog.Text += "Error: " + ex.Message + "\n";
                return false;
            }
        }
        
        private void Button_Click_Find_Rep(object sender, RoutedEventArgs e)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            // the code that you want to measure comes here
            string path_file = textRep.Text;
            string key_name = textKey.Text;
            if (path_file.Length > 1) {
                if (Path.GetExtension(path_file) == ".xsl")
                {
                    textLog.Text = "Seleccione un archivo compatible, el archivo " + Path.GetFileName(textRep.Text) + " no es compatible.\n";
                }
                else
                {
                    string output_file = "C:/Users/slopez/Documents/archivo.xml";
                    if (Drop_No_Duplicated(key_name, path_file, output_file))
                    {
                        textLog.Text +="Se han eliminado las filas no repetidas\n";
                    }
                }
            }
            else
            {
                textLog.Text = "Seleccione un archivo para poder comprobar los repetidos\n";
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            textLog.Text += "Tiempo de ejecución: "+ elapsedMs + " milisegundos\n";
        }
    }
}

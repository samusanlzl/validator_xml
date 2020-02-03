using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
            //Obtenemos los archivos xml y xsd de todos los directorios incluidos en los path almacenados en textXML.Text y textXSD.Text
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
            string xml_filename, xml_filename_without_digits;
            var pair = new { xml = "xml", xsd = "xsd" };
            textLog.Text = "";
            var xml_xsd_files = xsd_files.ToDictionary(x => Path.GetFileNameWithoutExtension(x), x => new List<string>() { x });
            foreach (string xml_path in xml_files)
            {
                xml_filename = Path.GetFileNameWithoutExtension(xml_path);
                xml_filename_without_digits = "";
                // Comprobamos si el nombre del xml tiene digitos
                if (xml_filename.Any(char.IsDigit)) {
                    // Quitamos los digitos del nombre del xml para emparejarlo con su xsd correspondiente
                    xml_filename_without_digits = Regex.Replace(xml_filename, @"[\d-]", string.Empty);
                    // Otra forma de quitar los digitos del string (no sé cual será más rápida)
                    //char[] numbers = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
                    //int index_of_number = xml_filename.IndexOfAny(numbers);
                    //textLog.Text += "index es " + index_of_number + "  xml_filename: "+ xml_filename.Substring(0,index_of_number)+ "\n";
                }
                // Si el diccionario tiene una key (xsd) con el mismo nombre que el xml (xml_filename_without_digits) entonces podemos añadir el xml
                if (xml_filename_without_digits.Length > 1 && xml_xsd_files.ContainsKey(xml_filename_without_digits))
                {
                    xml_xsd_files[xml_filename_without_digits].Add(xml_path);
                    textLog.Text += "Archivo xml añadido para ser validado: " + xml_filename + "\n";
                }
                else
                {
                    textLog.Text += "El archivo: " + xml_filename_without_digits + " (" + xml_filename + ") no tiene un archivo xsd con el mismo nombre para ser validado.\n";
                }
            }

            string xsd_path;
            foreach (KeyValuePair<string, List<string>> item in xml_xsd_files)
            {
                if (item.Key.Length > 1 && item.Value.Count() > 1)
                {
                    xsd_path = item.Value.First();
                    foreach (string xml_path in item.Value.Skip(1))
                    {
                        textLog.Text += "Validando " + Path.GetFileName(xml_path) + " con " + item.Key + ".xsd\n";
                        textLog.Text += "Validando " + xml_path + " con " + xsd_path + "\n";
                        XmlSchemaSet schema = new XmlSchemaSet();
                        schema.Add("", xsd_path);
                        XmlReader rd = XmlReader.Create(xml_path);
                        XDocument doc = XDocument.Load(rd);
                        try
                        {
                            doc.Validate(schema, null);
                            textLog.Text += "Se ha validado correctamente.\n";
                        }
                        catch (XmlSchemaValidationException ex)
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
                //textLog.Text += "Seleccione un archivo compatible, el archivo " + Path.GetFileName(textRep.Text) + " no es compatible.\n";
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
                    textLogCSV.Text = "Seleccione un archivo compatible, el archivo " + Path.GetFileName(path_file) + " no es compatible.\n";
                }
                else
                {
                    string output_file = "C:/Users/" + Environment.UserName + "/Documents/archivo.xml";
                    if (Drop_No_Duplicated(key_name, path_file, output_file))
                    {
                        textLogCSV.Text += "Se han eliminado las filas no repetidas\n";
                    }
                }
            }
            else
            {
                textLog.Text = "Seleccione un archivo para poder comprobar los repetidos\n";
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            textLog.Text += "Tiempo de ejecución: " + elapsedMs + " milisegundos\n";
        }

        private void btnFindCSV_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textCSV.Text = dialog.FileName;
            }

        }

        private async void btnValidarCSV_Click(object sender, RoutedEventArgs e)
        {/*
            var watch = System.Diagnostics.Stopwatch.StartNew();
            // the code that you want to measure comes here
            string path_file = textCSV.Text;
            string key_name = textCampoCSV.Text;
            if (path_file.Length > 1)
            {
                if (Path.GetExtension(path_file) == ".csv")
                {
                    textLog.Text = "Seleccione un archivo compatible, el archivo " + Path.GetFileName(textRep.Text) + " no es compatible.\n";
                }
                else
                {
                    string output_file = "C:/Users/" + Environment.UserName + "/Documents/archivo.xml";
                    if (Drop_No_DuplicatedCSV(key_name, path_file, output_file))
                    {
                        textLog.Text += "Se han eliminado las filas no repetidas\n";
                    }
                }
            }
            else
            {
                textLog.Text = "Seleccione un archivo para poder comprobar los repetidos\n";
            }
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            textLog.Text += "Tiempo de ejecución: " + elapsedMs + " milisegundos\n";*/
            /*
            List<Persona> details = new List<Persona>() {
            new Persona{ edad = 11, name = "Liza" },
                new Persona{ edad = 52, name = "Stewart" },
                new Persona{ edad = 23, name = "wewe" },
                new Persona{ edad = 14, name = "Stefani" },
                new Persona { edad = 95, name = "wewe" }
            };

            var newDetails = details.Where(x => x.name == "wewe").OrderBy(x => x.edad);
            //var newDetails = details.OrderBy(x => x.edad);

            textLogCSV.Text = "";
            foreach (var value in newDetails)
            {
                textLogCSV.Text += (value.edad + " " + value.name) + "\n";
            }
            *//*


            
            textLogCSV.Text += "Inicio";
            string v = await Task.Run(() => Prueba());
            //meter(v);
            textLogCSV.Text += "\n" + v + "   1";
            string b = await Task.Run(() => Prueba());
            textLogCSV.Text += "\n" + b + "    2";
            //meter(b);

            textLogCSV.SelectionStart = textLogCSV.Text.Length;
            //textLogCSV.ScrollToCaret(); //scrolls to the bottom of textbox*/
        }

        private string Prueba()
        {
            Thread.Sleep(1000);
            return "aaaa\n";
        }

        private void meter(string s)
        {
            textLogCSV.Text += "\n" + s + "   dqwdqdw";
        }


        private bool Drop_No_DuplicatedCSV(string key_name, string path_file, string output_file)
        {
            try
            {
                string[] lines = File.ReadAllLines(System.IO.Path.ChangeExtension(path_file, ".csv"));

                string[] campos = lines[0].Split(';');
                int posCampo = -1;

                for (int i = 0; i < campos.Length; i++)
                {
                    if (campos[i] == key_name)
                    {
                        posCampo = i;
                    }
                }

                if (posCampo == -1)
                {
                    textLogCSV.Text += "No se ha encontrado el campo " + key_name + "\n";
                }
                else
                {
                    foreach (string l in lines)
                    {
                        string[] col = l.Split(';');

                        //
                        //TO DO
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                textLog.Text += "Error: " + ex.Message + "\n";
                return false;
            }
        }
    }

    public class Persona
    {
        public string name;
        public int edad;
    }
}

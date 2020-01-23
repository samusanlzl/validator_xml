using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        static bool validation = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string[] xml_files = Directory.GetFiles(textXML.Text, "*.xml");
            string[] xsd_files = Directory.GetFiles(textXSD.Text, "*.xsd");
            if (xml_files.Length == 0 || xsd_files.Length == 0)
            {
                if (xml_files.Length == 0)
                {
                    textLog.Text = "Seleccione una carpeta con archivos en formato compatible (.xml)\n";
                    textLog.Text += "la carpeta " + textXML.Text + " no tiene ficheros XML.\n";
                }
                if (xsd_files.Length == 0)
                {
                    textLog.Text = "Seleccione una carpeta con archivos en formato compatible (.xsd)\n";
                    textLog.Text += "la carpeta " + textXSD.Text + " no tiene ficheros XSD.\n";
                }
            }
            else
            {
                if (validation == false)
                {
                    string xml_filename, xsd_filename;
                    var pair = new { xml = "xml", xsd = "xsd" };
                    textLog.Text = "";
                    var xml_xsd_files = new Dictionary<string, Tuple<string, string>>();
                    foreach (string xml_path in xml_files)
                    {
                        xml_filename = Path.GetFileNameWithoutExtension(xml_path);
                        xml_xsd_files.Add(xml_filename, new Tuple<string, string>(xml_path, ""));
                    }
                    foreach (string xsd_path in xsd_files)
                    {
                        xsd_filename = Path.GetFileNameWithoutExtension(xsd_path);
                        if (xml_xsd_files.ContainsKey(xsd_filename))
                        {
                            xml_xsd_files[xsd_filename] = new Tuple<string, string>(xml_xsd_files[xsd_filename].Item1, xsd_path);
                        }
                    }
                    foreach (string key in xml_xsd_files.Keys)
                    {
                        if (xml_xsd_files[key].Item1.Length > 1 && xml_xsd_files[key].Item2.Length > 1)
                        {
                            textLog.Text += "Validando " + key + ".xml con " + key + ".xsd\n";
                            XmlSchemaSet schema = new XmlSchemaSet();
                            schema.Add("", xml_xsd_files[key].Item2);
                            XmlReader rd = XmlReader.Create(xml_xsd_files[key].Item1);
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
                    validation = true;
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
                    XmlReader rd = XmlReader.Create(path_file);
                    try
                    {
                        XDocument doc = XDocument.Load(rd);
                        
                        textLog.Text = "Buscando valores repetidos para el campo: " + key_name + "\n";
                        var grouped = doc.Descendants(key_name).GroupBy(x => x.Value).Where(g => g.Count() > 1);
                        textLog.Text += "Búsqueda terminada\n";
                        textLog.Text += "Número de valores repetidos: " + grouped.Count() + "\n";

                        foreach (var groupItem in grouped)
                        {
                            textLog.Text += groupItem.First().Parent.ToString() + "\n";
                        }
                        if (grouped.Count() == 0)
                        {
                            textLog.Text += "Todo correcto, no hay valores repetidos para el campo: " + key_name + "\n";
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        textLog.Text = "Seleccione un archivo compatible, el archivo " + Path.GetFileName(textRep.Text) + " no es compatible.\n";
                        textLog.Text += "Error: " + ex.Message + "\n";
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

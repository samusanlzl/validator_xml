using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private void Button_Click_File_Rep(object sender, RoutedEventArgs e)
        {
            /*var dialog = new OpenFileDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textRep.Text = dialog.FileName;
            }*/
            var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                textRep.Text = dialog.SelectedPath;
            }
        }

        private void AddLogMessage(string text)
        {
            this.Dispatcher.Invoke(() => {
                textLog.Text += text;
                textLog.ScrollToEnd();
            });
            //textLog.Text += text;
        }

        private void SetLogMessage(string text)
        {
            this.Dispatcher.Invoke(() => {
                textLog.Text = text;
            });
            //textLog.Text = text;
        }

        private string [] Load_Xml_files()
        {
            if (textRep.Text.Length == 0)
            {
                SetLogMessage("Seleccione una carpeta con archivos en formato compatible (.xml)\n");
                AddLogMessage("la carpeta de ficheros XML no se ha seleccionado\n");
                return null;
            }
            string[] xml_files = Directory.GetFiles(textRep.Text, "*.xml", SearchOption.AllDirectories);
            AddLogMessage("La carpeta " + textRep.Text + " tiene "+ xml_files.Length + " ficheros XML.\n");
            if (xml_files.Length == 0)
            {
                SetLogMessage("Seleccione una carpeta con archivos en formato compatible (.xml)\n");
                AddLogMessage("la carpeta " + textRep.Text + " no tiene ficheros XML.\n");
                return null;
            }
            return xml_files;
        }
        // Creamos un diccionario que utilizará como clave el nombre del fichero xsd sin la extensión,
        //   y como valor un array de strings que contienen el path del xsd en la primera posición 
        //   y el path de los xml a validar con ese xsd en el resto de item de la lista
        private Dictionary<string, List<string>> Load_Xml_Xsd_files()
        {
            if (textXML.Text.Length == 0)
            {
                SetLogMessage("Seleccione una carpeta con archivos en formato compatible (.xml)\n");
                AddLogMessage("la carpeta de ficheros XML no se ha seleccionado\n");
                return null;
            }
            if (textXSD.Text.Length == 0)
            {
                SetLogMessage("Seleccione una carpeta con archivos en formato compatible (.xsd)\n");
                AddLogMessage("la carpeta de ficheros XSD no se ha seleccionado\n");
                return null;
            }
            //Obtenemos los path de los archivos xml y xsd de todos los directorios incluidos en los path almacenados en textXML.Text y textXSD.Text
            string[] xml_files = Directory.GetFiles(textXML.Text, "*.xml", SearchOption.AllDirectories);
            string[] xsd_files = Directory.GetFiles(textXSD.Text, "*.xsd", SearchOption.AllDirectories);
            if (xml_files.Length == 0)
            {
                SetLogMessage("Seleccione una carpeta con archivos en formato compatible (.xml)\n");
                AddLogMessage("la carpeta " + textXML.Text + " no tiene ficheros XML.\n");
                return null;
            }
            if (xsd_files.Length == 0)
            {
                SetLogMessage("Seleccione una carpeta con archivos en formato compatible (.xsd)\n");
                AddLogMessage("la carpeta " + textXSD.Text + " no tiene ficheros XSD.\n");
                return null;
            }
            //actualizar_log(textLog, "Comenzamos \n");
            var xml_xsd_files = xsd_files.ToDictionary(x => Path.GetFileNameWithoutExtension(x), x => new List<string>() { x });
            string xml_filename, xml_filename_without_digits;
            //setLogMessage(""; // Vaciamos el log

            //char[] numbers = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            foreach (string xml_path in xml_files)
            {
                xml_filename = Path.GetFileNameWithoutExtension(xml_path);

                // Comprobamos si el nombre del archivo xml tiene digitos
                if (xml_filename.Any(char.IsDigit))
                {
                    // Quitamos los digitos del nombre del xml para emparejarlo con su xsd correspondiente
                    xml_filename_without_digits = Regex.Replace(xml_filename, @"[\d-]", string.Empty);
                    // Otra forma de quitar los digitos del string (no sé cual será más rápida)
                    //xml_filename_without_digits = xml_filename.Substring(0, xml_filename.IndexOfAny(numbers));
                    //addLogMessage("index es " + index_of_number + "  xml_filename: "+ xml_filename.Substring(0,index_of_number)+ "\n";
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
                    AddLogMessage("Archivo xml añadido para ser validado: " + xml_filename + ".xml\n");
                }
                else
                {
                    AddLogMessage("El archivo: " + xml_filename_without_digits + " (" + xml_filename + ") no tiene un archivo xsd con el mismo nombre para ser validado.\n");
                }
            }
            return xml_xsd_files;
        }

        private void ValidarXML(Dictionary<string, List<string>> xml_xsd_files)
        {
            //string xsd_path;
            //Dictionary<string, List<string>> xml_xsd_files = (Dictionary<string, List<string>>)obj;
            // Recorremos el diccionario (xml_xsd_files) para ir validando los xml que hemos añadido
            foreach (KeyValuePair<string, List<string>> item in xml_xsd_files)
            //Parallel.ForEach(xml_xsd_files, item =>
            {
                if (item.Key.Length > 1 && item.Value.Count() > 1)
                {
                    string xsd_path = item.Value.First(); // El primer path del array de string corresponde al del archivo xsd
                    // Recorremos todos los path (menos el primero que es el del archivo xsd) guardados en el item.Value que corresponden a los xml
                    //foreach (string xml_path in item.Value.Skip(1))
                    Parallel.ForEach(item.Value.Skip(1), xml_path =>
                    {
                        //AddLogMessage("Validando " + Path.GetFileName(xml_path) + " con " + item.Key + ".xsd\n");
                        //AddLogMessage("Validando " + xml_path + " con " + xsd_path + "\n");
                        XmlSchemaSet schema = new XmlSchemaSet();
                        schema.Add("", xsd_path);
                        try
                        {
                            XmlReader rd = XmlReader.Create(xml_path);
                            XDocument doc = XDocument.Load(rd);
                            // Intentamos validar los xml con su xsd correspondiente
                            doc.Validate(schema, null);
                            //AddLogMessage("Se ha validado correctamente.\n");
                            AddLogMessage("El archivo " + Path.GetFileName(xml_path) + " se ha validado correctamente con " + item.Key + ".xsd\n");
                        }
                        catch (Exception ex)
                        {
                            AddLogMessage("Ha ocurrido un error en la validación.\n");
                            AddLogMessage("Detalles del error: " + ex.Message + "\n");
                        }
                        //AddLogMessage("\n");
                    });
                }
            };
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            boton_validar.IsEnabled = false;
            try
            {
                SetLogMessage("Comenzando...");
                AddLogMessage("\n");
                // Creamos un diccionario que utilizará como clave el nombre del fichero xsd sin la extensión,
                //   y como valor un array de strings que contienen el path del xsd en la primera posición 
                //   y el path de los xml a validar con ese xsd en el resto de item de la lista
                Dictionary<string, List<string>> xml_xsd_files = Load_Xml_Xsd_files();
                if (xml_xsd_files != null)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    await Task.Run(() => ValidarXML(xml_xsd_files));
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                    AddLogMessage("Tiempo de ejecución: " + elapsedMs + " milisegundos\n");
                }
            }
            catch (Exception ex)
            {
                AddLogMessage("Ha ocurrido un error en la validación.\n");
                AddLogMessage("Detalles del error: " + ex.Message + "\n");
            }
            AddLogMessage("\n");
            boton_validar.IsEnabled = true;
        }

        private void ComprobarRepetidosXML()
        {
            string key_name = textKey.Text;
            string output_file, file_name, file_name_without_digits;
            string[] xml_files = Load_Xml_files();
            if (xml_files != null)
            {
                XDocument doc;
                try
                {
                    foreach (string path_file in xml_files)
                    {
                        XmlReader xr = XmlReader.Create(path_file);
                        doc = XDocument.Load(xr);
                        xr.Close();
                        XElement root = new XElement(doc.Root);
                        root.RemoveAll();
                        XElement child;
                        file_name = Path.GetFileName(path_file);
                        file_name_without_digits = Regex.Replace(file_name, @"[\d-]", string.Empty);
                        var grouped_duplicates = doc.Descendants(key_name).GroupBy(x => x.Value).Where(g => g.Count() > 1);
                        if (grouped_duplicates.Count() >= 1)
                        {
                            AddLogMessage("\nFilename: " + file_name + "\n");
                            AddLogMessage("Número de valores repetidos: " + grouped_duplicates.Count() + "\n\n");
                            foreach (var groupItem in grouped_duplicates)
                            {
                                foreach (var item in groupItem)
                                {
                                    child = new XElement(item.Parent);
                                    root.Add(child);
                                    //item.Parent.Remove();
                                }
                            }
                            AddLogMessage("Se han eliminado las filas no repetidas\n");
                            output_file = "C:\\Users\\" + Environment.UserName + "\\Documents\\" + file_name_without_digits;
                            if (File.Exists(output_file))
                            {
                                AddLogMessage("Añadimos los repetidos del archivo: " + file_name + " \n al archivo: " + output_file + "\n");
                                XmlReader xr_exist = XmlReader.Create(output_file);
                                XDocument doc_exist = XDocument.Load(xr_exist);
                                xr_exist.Close();
                                doc_exist.Root.AddFirst(root.Nodes());
                                doc_exist.Save(output_file);
                                AddLogMessage("Se ha actualizado el archivo: " + output_file + " con los valores repetidos\n");
                            }
                            else
                            {
                                XDocument new_doc = new XDocument();
                                new_doc.Add(root);
                                AddLogMessage("Se ha creado el archivo: " + output_file + " con los valores repetidos\n");
                                new_doc.Save(output_file);
                            }
                        }
                        else
                        {
                            AddLogMessage("Todo correcto, no hay valores repetidos para el campo: " + key_name + " en el fichero: "+ path_file + "\n");
                        }
                    }
                }
                catch (Exception ex)
                {
                    //addLogMessage("Seleccione un archivo compatible, el archivo " + Path.GetFileName(textRep.Text) + " no es compatible.\n";
                    AddLogMessage("Error: " + ex.Message + "\n");
                    return;
                }
            }
        }
        private void Button_Click_Find_Rep(object sender, RoutedEventArgs e)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            SetLogMessage("Comienza ejecución...\n");
            // the code that you want to measure comes here
            string path_xml = textRep.Text;
            ComprobarRepetidosXML();
            //await ComprobarRepetidosXML();

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            AddLogMessage("Tiempo de ejecución: " + elapsedMs + " milisegundos\n");
            return;
        }
    }

}

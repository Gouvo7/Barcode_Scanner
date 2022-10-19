using System;
using System.IO;
using IronBarCode;
using BitMiracle.Docotic.Pdf;
using System.Drawing;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace Barcode_Scanner
{
    class Program
    {

        // Converts the first page of the PDF file to a PNG.
        public void Create_Png(string path,string pdf_file)
        {
            PdfDocument pdf = new PdfDocument(path+pdf_file);
            PdfDrawOptions options = PdfDrawOptions.Create();
            options.BackgroundColor = new PdfRgbColor(255, 255, 255);
            options.HorizontalResolution = 300;
            options.VerticalResolution = 300;
            pdf_file = pdf_file.Remove(pdf_file.Length - 3);
            string new_path = path + pdf_file + "png";
            pdf.Pages[0].Save(new_path, options);
        }


        // Crops the image to a fixed location where the barcode is
        [Obsolete]
        public string Crop_Img(string path,string png_path)
        {

            try
            {
                Image img = Image.FromFile(png_path);
                Bitmap bmpImage = new Bitmap(img);
                Rectangle r = new Rectangle(250, 80, 500, 500);
                Bitmap bmpCrop = bmpImage.Clone(r, bmpImage.PixelFormat);
                bmpCrop.Save("a.png");
                string barcode = Scan_Barcode("a.png");
                return barcode;
                
            }
            catch (Exception e)
            {
                throw new ArgumentNullException("The cropped image file was not found.", e);
            }
        }

        // Scans the barcode number
        [Obsolete]
        public string Scan_Barcode(string cropped)
        {
            BarcodeResult Result = BarcodeReader.QuicklyReadOneBarcode(cropped);
            BarcodeReader.QuicklyReadOneBarcode("a.png");
            if (Result != null)
            {
                Console.WriteLine("Barcode read with no errors! Barcode is:" + Result.Text+ "\n\n");
            }
            else
            {
                Console.WriteLine("Could not identify barcode\n\n");
            }
            if (Result!= null)
                return Result.Text;
            else
                return null;
        }

        // Deletes the temporary files
        public void File_Deleter(string pdf_file, string png_path, string cropped)
        {
            //File.Delete(pdf_file);        // If you want to delete the PDF afterwards, uncomment this line.
            File.Delete(png_path);
            File.Delete(cropped);
        }

        [Obsolete]
        static void Main(string[] args)
        {
            //Stopwatch timer = new Stopwatch();
            //timer.Start();
            List<string> barcode_list = new List<string> { };
            string pdf_path = "C:\\Users\\ngouvousis\\Desktop\\PDFS\\";

            string[] dirs = Directory.GetFiles(pdf_path, "*.pdf", SearchOption.AllDirectories);
            string filename = null;
            Program p = new Program();
            foreach (string dir in dirs)
            {
                //Console.WriteLine(dir); // dir is the whole path
                filename = Path.GetFileName(dir); // filename is the file name
                string f = filename.Remove(filename.Length - 3); // f is the file name without the extension
                p.Create_Png(pdf_path, filename);
                string png_f = f + "png";
                string png_path = pdf_path + png_f;
                Console.WriteLine("PNG_Path is : " + png_path);
                string barc = p.Crop_Img(pdf_path, png_path);
                if (barc!=null && barc!=" ")
                    barcode_list.Add(barc);                
            }
            MySql.Data.MySqlClient.MySqlConnection conn;
            string myConnectionString;
            //myConnectionString = "server=10.1.11.28:3306;uid=ngouvousis;pwd=Nek@niro_{Gou22};database=lawdb";
            myConnectionString = "server=localhost;uid=root;" +
            "pwd=root;database=lawdb";
            foreach (var x in barcode_list)
            {
                try
                {

                    conn = new MySql.Data.MySqlClient.MySqlConnection(myConnectionString);
                    //conn.ConnectionString = myConnectionString;
                    conn.Open();
                    MySql.Data.MySqlClient.MySqlCommand query = new MySql.Data.MySqlClient.MySqlCommand("SELECT * FROM WCM_LAC where WCMLAC_ID = '"+x+"'" , conn);

                    using (MySqlDataReader reader = query.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Console.WriteLine(String.Format("{0}", reader["wcmlac_ID"]));
                        }
                    }

                    //var res = cmd.ExecuteScalar().ToString();
                    //Console.WriteLine("Res is: " + res);
                }
                catch (MySql.Data.MySqlClient.MySqlException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            //p.File_Deleter(pdf_file, png_path, cropped);
            //timer.Stop();
            //Console.WriteLine("time taken = " + timer.Elapsed);

            Console.WriteLine("Finished");
        }
    }
}
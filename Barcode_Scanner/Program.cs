using BitMiracle.Docotic.Pdf;
using IronBarCode;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;


namespace Barcode_Scanner
{
    class Program
    {

        // Crops the image to a fixed location where the barcode is
        [Obsolete]
        public string Crop_Img(string png_path)
        {
            try
            {
                Image img = Image.FromFile(png_path);
                Bitmap bmpImage = new Bitmap(img);
                Rectangle r = new Rectangle(250, 80, 500, 500);
                Bitmap bmpCrop = bmpImage.Clone(r, bmpImage.PixelFormat);
                bmpCrop.Save("a.png");
                string barcode = Scan_Barcode("a.png");
                img.Dispose();
                bmpImage.Dispose();
                return barcode;
            }
            catch (Exception e)
            {
                throw new ArgumentNullException("The cropped image file was not found.", e);
            }
        }

        /* Receives the name of the png file, scans the barcode number and returns it into a string */
        [Obsolete]
        public string Scan_Barcode(string cropped_png)
        {
            BarcodeResult res = BarcodeReader.QuicklyReadOneBarcode(cropped_png);
            if (res != null)
            {
                Console.WriteLine("Barcode read with no errors! Barcode is:" + res.Text+ "\n");
                return res.Text;
            }
            else
            {
                return null;
            }
        }

        /* Deletes the temporary files */
        public void File_Deleter(string pdf_file, string png_path)
        {
            // If you want to delete the PDF afterwards, uncomment this line.
            try
            {
                if (File.Exists(png_path))
                    File.Delete(png_path);
            }
            catch (System.IO.IOException)
            {
                throw new IOException("Something went wrong with the deletion of temporary files");
            }
        }

        // Converts the first page of the PDF file to a PNG and returns the number of pages
        // (only for database appendance).
        public int Create_Png(string pdf_path, string pdf_file)
        {
            PdfDocument pdf = new PdfDocument(pdf_path + pdf_file);
            PdfDrawOptions options = PdfDrawOptions.Create();
            options.BackgroundColor = new PdfRgbColor(255, 255, 255);
            options.HorizontalResolution = 300;
            options.VerticalResolution = 300;
            pdf_file = pdf_file.Remove(pdf_file.Length - 3);
            string new_path = pdf_path + pdf_file + "png";
            pdf.Pages[0].Save(new_path, options);
            int pages = pdf.PageCount;
            pdf.Dispose();
            return pages;
        }

        [Obsolete]
        static void Main(string[] args)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            List<List<string>> mylist = new List<List<string>>();

            Program p = new Program();
            string pdf_path = "C:\\Users\\ngouvousis\\Desktop\\PDFS\\";  // Change if your pdfs are in another directory
            string png_path, pdf_file;
            try
            {
                string[] dirs = Directory.GetFiles(pdf_path, "*.pdf", SearchOption.AllDirectories);
                Console.WriteLine("Number of PDF files found in the path '" + pdf_path + "' is: " + dirs.Length);
                
                foreach (string dir in dirs) // For each file found in the directory given
                {
                    pdf_file = Path.GetFileName(dir); // filename is the name of the file
                    string f = pdf_file.Remove(pdf_file.Length - 3); // f is the file name without the extension
                    int pages = p.Create_Png(pdf_path, pdf_file); // Converts the pdf into an image
                    string png_f = f + "png";                     
                    png_path = pdf_path + png_f;
                    string barc = p.Crop_Img(png_path);
                    if (barc != null && barc != " ")
                        mylist.Add(new List<string> { barc, pdf_file, pages.ToString() });
                    p.File_Deleter(pdf_file, png_path);
                }
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                Console.Write("Could not find any PDF files in the path that you have given. Program will now exit");
                Thread.Sleep(750);
                Console.Write(".");
                Thread.Sleep(750);
                Console.Write(".");
                Thread.Sleep(750);
                Console.Write(".");
                Thread.Sleep(1000);
            }

            string tmp = "Test Eggrafo";
            DateTime now = DateTime.Now;
            //myConnectionString = "server=10.1.11.28:3306;uid=ngouvousis;pwd=Nek@niro_{Gou22};database=lawdb";
            string connectionString = "server=localhost;uid=root;pwd=root;database=lawdb";

            /* For each successfully scanned barcode, we will insert the corresponding values 
             * to the table */
            foreach (List<String> sublist in mylist)
            {
                bool canInsert = false;
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query_tmp = "select * from wcm_doc where wcmdoc_URL like '%"+"@url"+"%'";
                    using (MySqlCommand command = new MySqlCommand(query_tmp, conn))
                    {
                        command.Parameters.AddWithValue("@url", sublist[0]);
                        conn.Open();
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                    Console.WriteLine("CANNOT INSERT");
                                else
                                    canInsert = true;
                            }
                    }
                }
                if (canInsert)
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        string query_tmp = "INSERT INTO WCM_DOC VALUES (NULL, @pr_id, 'wcm_lac', 'wcmLAC_ID', '" + tmp + "', " +
                            "200, 9, @wcmDoc_URL, @wcmDoc_Pages, @create_dt, @last_upd, 0, 0, NULL, NULL)";
                        using (MySqlCommand command = new MySqlCommand(query_tmp, conn))
                        {
                            command.Parameters.AddWithValue("@pr_id", sublist[0]);
                            command.Parameters.AddWithValue("@wcmDoc_URL", sublist[1]);
                            command.Parameters.AddWithValue("@wcmDOC_Pages", sublist[2]);
                            command.Parameters.AddWithValue("@create_dt", now);
                            command.Parameters.AddWithValue("@last_upd", now);
                            conn.Open();
                            try
                            {
                                int res = command.ExecuteNonQuery();
                                if (res < 0)
                                    Console.WriteLine("There was an error during data insertion.");
                            }
                            catch (MySqlException)
                            {
                                Console.WriteLine("Error: Duplicate entry was found for doc: " + sublist[1]);
                            }
                        }
                    }
                }
            }
            timer.Stop();
            Console.WriteLine("time taken = " + timer.Elapsed);
        }
    }
}
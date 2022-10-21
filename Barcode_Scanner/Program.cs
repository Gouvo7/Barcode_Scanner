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
                //Console.WriteLine("Barcode read with no errors! Barcode is:" + res.Text+ "\n");
                return res.Text;
            }
            else
                return null;
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
            string backup_path = "C:\\Users\\ngouvousis\\Desktop\\Backups\\"; // Change if you want to store your backups in another directory
            string png_path, pdf_file;
            int no_Changes = 0;
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
                    string barc = p.Crop_Img(png_path); // Crops the image and keeps the upper left part of the photo (that's where the barcode is). Returns    
                    
                    if (barc != null && barc != " ")
                        mylist.Add(new List<string> { barc, pdf_file, pages.ToString() }); 
                    p.File_Deleter(pdf_file, png_path);

                    /* Moving files to backup directory */
                    string source = pdf_path + pdf_file;
                    string dest = backup_path + pdf_file;
                    //System.IO.File.Move(source,dest); // Uncomment to move files into backup directory
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

            /* For each successfully scanned barcode, we will try to find it the doc already exists in the table.
             * If not, then we will insert the corresponding values to the table */
            foreach (List<String> sublist in mylist)
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query_tmp = "INSERT INTO WCM_DOC (wcmDOC_PR_ID,wcmDOC_TBL,wcmDOC_FieldID,wcmDOC_Title,wcmDOC_Type,wcmDOC_Filetype,wcmDOC_URL,wcmDOC_Pages) VALUES " +
                        "(@pr_id, 'wcm_lac', 'wcmLAC_ID', '" + tmp + "', 200, 9, @wcmDoc_URL, @wcmDoc_Pages)";
                    using (MySqlCommand command = new MySqlCommand(query_tmp, conn))
                    {
                        command.Parameters.AddWithValue("@pr_id", sublist[0]);
                        command.Parameters.AddWithValue("@wcmDoc_URL", sublist[1]);
                        command.Parameters.AddWithValue("@wcmDOC_Pages", sublist[2]);
                        conn.Open();
                        try
                        {
                            int res = command.ExecuteNonQuery();
                            if (res == 1)
                                no_Changes++;
                                Console.WriteLine("Succsefully inserted file: " + sublist[1]);
                        }
                        catch (MySqlException)
                        {
                            Console.WriteLine("Error: Duplicate entry was found for doc: " + sublist[1]);
                        }
                            conn.Close();
                    }
                }
            }
            timer.Stop();
            Console.WriteLine("Total time elapsed: " + timer.Elapsed);
            Console.WriteLine("Number of insertions in database: " + no_Changes);
        }
    }
}
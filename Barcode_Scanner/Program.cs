using BitMiracle.Docotic.Pdf;
using IronBarCode;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace Barcode_Scanner
{
    class Program
    {

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
                img.Dispose();
                bmpImage.Dispose();
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
        public void File_Deleter(string pdf_file, string png_path)
        {
            //try
            //{
                //File.Delete(pdf_file);        // If you want to delete the PDF afterwards, uncomment this line.
            if (File.Exists(png_path))
            {
                File.Delete(png_path);
                
            }
            Console.WriteLine(png_path);

            //File.Delete(cropped);
            //}
            //catch (System.IO.IOException) { 
            //}
        }

        // Converts the first page of the PDF file to a PNG.
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
            Console.WriteLine("Number of pages for each document: " + pages);
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
            string png_path = "";
            string pdf_file = null;
            string[] dirs = Directory.GetFiles(pdf_path, "*.pdf", SearchOption.AllDirectories);
            
            foreach (string dir in dirs)
            {
                //Console.WriteLine(dir); // dir is the whole path
                pdf_file = Path.GetFileName(dir); // filename is the name of the file
                string f = pdf_file.Remove(pdf_file.Length - 3); // f is the file name without the extension
                int pages = p.Create_Png(pdf_path, pdf_file);
                string png_f = f + "png";
                png_path = pdf_path + png_f;
                Console.WriteLine("PNG_Path is : " + png_path);
                string barc = p.Crop_Img(pdf_path, png_path);
                if (barc!=null && barc!=" ") { 
                    mylist.Add(new List<string> { barc, pdf_file, pages.ToString() });
                    
                }
                p.File_Deleter(pdf_file, png_path);
            }
            string connectionString = "server=localhost;uid=root;pwd=root;database=lawdb";
            //myConnectionString = "server=10.1.11.28:3306;uid=ngouvousis;pwd=Nek@niro_{Gou22};database=lawdb";
            int i = 0;
            string tmp = "Test Eggrafo";
            DateTime now = DateTime.Now;

            foreach (List<String> sublist in mylist)
            {
                i++;
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    //MySql.Data.MySqlClient.MySqlCommand query = new MySql.Data.MySqlClient.MySqlCommand("SELECT * FROM WCM_LAC where WCMLAC_ID = '"+x+"'" , conn);
                    //string query = "INSERT INTO WCM_DOC VALUES (NULL,@pr_id,wcm_lac,wcmLAC_ID,@wcnDoc_Title,@wcmDoc_Type,wcmDOC_FileType,@wcmDoc_URL,wcmDOC_Pages,@create_dt,@last_upd,@wdc_lock,0,NULL,NULL";
                    string query_tmp = "INSERT INTO WCM_DOC VALUES (NULL, @pr_id, 'wcm_lac', 'wcmLAC_ID', '"+ tmp +"', 200, 9, @wcmDoc_URL, @wcmDoc_Pages, @create_dt, @last_upd, 0, 0, NULL, NULL)";

                    using (MySqlCommand command = new MySqlCommand(query_tmp, conn))
                    {
                        command.Parameters.AddWithValue("@pr_id",sublist[0] );
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
                            else
                                Console.WriteLine("Very good very nice!");
                        }
                        catch (MySqlException){
                            Console.WriteLine("Error: Duplicate entry was found for doc: " + sublist[1]);}
                    }
                }
            }            
            timer.Stop();
            Console.WriteLine("time taken = " + timer.Elapsed);
        }

        
    }
}
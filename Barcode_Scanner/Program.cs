using System;
using System.IO;
using IronBarCode;
using BitMiracle.Docotic.Pdf;
using System.Drawing;

namespace Barcode_Scanner
{
    class Program
    {

        // Converts the first page of the PDF file to a PNG.
        public void Create_Png(string pdf_file)
        {
            var pdf = new PdfDocument(@pdf_file);
            PdfDrawOptions options = PdfDrawOptions.Create();
            options.BackgroundColor = new PdfRgbColor(255, 255, 255);
            options.HorizontalResolution = 300;
            options.VerticalResolution = 300;
            pdf.Pages[0].Save($"tmp.png", options);
        }


        // Crops the image to a fixed location where the barcode is
        public void Crop_Img(string png_path, string cropped)
        {
            try
            {
                Image img = Image.FromFile(png_path);
                Bitmap bmpImage = new Bitmap(img);
                Rectangle r = new Rectangle(250, 100, 500, 500);
                Bitmap bmpCrop = bmpImage.Clone(r, bmpImage.PixelFormat);
                bmpCrop.Save(cropped);
            }
            catch (Exception e)
            {
                throw new ArgumentNullException("The cropped image file was not found.", e);
            }
        }

        // Scans the barcode number
        [Obsolete]
        public void Scan_Barcode(string cropped)
        {
            BarcodeResult Result = BarcodeReader.QuicklyReadOneBarcode(cropped);
            if (Result != null)
            {
                Console.WriteLine("Barcode read with no errors!");
                Console.WriteLine("Barcode = " + Result.Text);
            }
            else
            {
                Console.WriteLine("Could not identify barcode");
            }
        }

        // Deletes the temporary files
        public void File_Deleter(string pdf_file, string png_path, string cropped)
        {
            //File.Delete(pdf_file);        // Should be uncommented out once the .exe is released
            File.Delete(png_path);
            File.Delete(cropped);
        }

        [Obsolete]
        static void Main(string[] args)
        {
            string pdf_file = "C:\\Users\\ngouvousis\\source\\repos\\Barcode_Scanner\\Barcode_Scanner\\bin\\Debug\\netcoreapp3.1\\tmp.pdf";
            string png_path = "C:\\Users\\ngouvousis\\source\\repos\\Barcode_Scanner\\Barcode_Scanner\\bin\\Debug\\netcoreapp3.1\\tmp.png";
            string cropped = "C:\\Users\\ngouvousis\\source\\repos\\Barcode_Scanner\\Barcode_Scanner\\bin\\Debug\\netcoreapp3.1\\cropped.png";

            Program p = new Program();
            try
            {
                p.Create_Png(pdf_file);
                
            } 
            catch (Exception e)
            {
                Console.WriteLine($"The PDF file was not found: '{e}'");
                return;
            }
            try
            {
                p.Crop_Img(png_path, cropped);
            }
            catch (Exception e)
            { 
                Console.WriteLine($"The image was not found or something went wrong with the cropping process: '{e}'");
                return;
            }
            try
            {
                p.Scan_Barcode(cropped);
            }
            catch (Exception e)
            {
                Console.WriteLine($"The cropped image file was not found: '{e}'");
                return;
            }
            p.File_Deleter(pdf_file, png_path, cropped);

        }

        
    }
}
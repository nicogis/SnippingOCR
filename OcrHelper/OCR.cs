namespace StudioAT.Utilitites.SnippingOCR
{
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
    using System;
    using System.Configuration;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public enum FormatImageCognitiveService
    {
        jpeg,
        png,
        bmp
    }

    /// <summary>
    /// cognitive services azure
    /// Supported image formats: JPEG, PNG and BMP.
    /// Image file size must be less than 4MB.
    /// Image dimensions must be at least 50 x 50, at most 3200 x 3200.
    /// </summary>
    public class OCR
    {
        static readonly string endPoint = null;
        static readonly string key = null;
        const int dimensioneMin = 50;
        const int dimensioneMax = 3200;
        const int delta = 50;

        static OCR()
        {
            OCR.endPoint = Convert.ToString(ConfigurationManager.AppSettings["EndPoint"]);
            OCR.key = Convert.ToString(ConfigurationManager.AppSettings["Key"]);
        }

        public static async Task<ResultOCR> Process(Image image, OcrLanguages ocrLanguages, FormatImageCognitiveService formatImage)
        {
            bool result = false;
            string error = null;
            string text = null;
            Bitmap destinationImage = null;

            try
            {
                
                Clipboard.Clear();

                
                IComputerVisionClient client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
                {
                    Endpoint = OCR.endPoint                     
                };

                if ((image.Height < dimensioneMin) || (image.Width < dimensioneMin))
                {
                    int newW = image.Width < dimensioneMin ? dimensioneMin + delta : image.Width;
                    int newH = image.Height < dimensioneMin ? dimensioneMin + delta : image.Height;

                    destinationImage = new Bitmap(newW, newH);

                    using (Graphics g = Graphics.FromImage(destinationImage))
                    {
                        g.DrawImage(image, new Rectangle((newW - image.Width) / 2, (newH - image.Height) / 2, image.Width, image.Height), new Rectangle(0,0, image.Width, image.Height), GraphicsUnit.Pixel);
                    }

                    image = destinationImage;

                    //image.Save(@"c:\temp\prova.png",ImageFormat.Png);
                }

                if ((image.Height > dimensioneMax) || (image.Width > dimensioneMax))
                {
                    throw new ApplicationException("Capture a smaller area!");
                }
  
                ImageFormat format = ImageFormat.Png;
                if (formatImage == FormatImageCognitiveService.jpeg)
                {
                    format = ImageFormat.Jpeg;
                }
                else if (formatImage == FormatImageCognitiveService.bmp)
                {
                    format = ImageFormat.Bmp;
                }

                using (Stream imageFileStream = new MemoryStream())
                {
                    image.Save(imageFileStream, format);

                    if (Helper.ConvertBytesToMegabytes(imageFileStream.Length) > 4d)
                    {
                        throw new ApplicationException("Capture a smaller area!");
                    }

                    imageFileStream.Seek(0, SeekOrigin.Begin);

                    OcrResult t = await client.RecognizePrintedTextInStreamAsync(false, imageFileStream, ocrLanguages);

                    if (t.Regions.Count == 0)
                    {
                        throw new ApplicationException("Failed to convert the text!");
                    }


                    foreach (OcrRegion c in t.Regions)
                    {
                        foreach (OcrLine l in c.Lines)
                        {
                            foreach (OcrWord w in l.Words)
                            {
                                text += w.Text + " ";
                            }

                            text += Environment.NewLine;
                        }
                    }


                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        Clipboard.SetText(text);
                    }

                    result = true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            finally
            {
                if (destinationImage != null)
                {
                    destinationImage.Dispose();
                }
            }

            return new ResultOCR() { Success = result, Error = error, Text= text };
        }
    }  
}

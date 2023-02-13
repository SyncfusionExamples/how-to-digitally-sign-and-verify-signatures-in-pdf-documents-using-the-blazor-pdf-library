using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Drawing;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace BlazorPDF.Data
{
    public class ExportService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        public ExportService(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }
        public MemoryStream CreatePDF()
        {
            //return SignExisting();
            //return ExternalSignature();
            //return WindowsStore();
            //return LTV();
            return ValidatePDFSignatures();

        }
        public MemoryStream ValidatePDFSignatures()
        {
            FileStream pdfstream = new FileStream(_hostingEnvironment.WebRootPath + "//MultipleSignature.pdf", FileMode.Open, FileAccess.Read);
            PdfLoadedDocument document = new PdfLoadedDocument(pdfstream);
            PdfLoadedForm form = document.Form;
            List<PdfSignatureValidationResult> results;
            X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;

            if (form!=null)
            {
                //bool isValid = form.Fields.ValidateSignatures(out results);

                foreach(PdfLoadedField field in form.Fields)
                {
                    if(field is PdfLoadedSignatureField)
                    {
                        PdfLoadedSignatureField signatureField = field as PdfLoadedSignatureField;
                        if(signatureField.IsSigned)
                        {
                            PdfSignatureValidationResult result = signatureField.ValidateSignature(collection);
                        }
                    }
                }
            }
            MemoryStream stream = new MemoryStream();
            document.Save(stream);
            document.Close(true);
            return stream;
        }
        public MemoryStream LTV()
        {
            FileStream pdfstream = new FileStream(_hostingEnvironment.WebRootPath + "//PDF_Succinctly.pdf", FileMode.Open, FileAccess.Read);
            PdfLoadedDocument document = new PdfLoadedDocument(pdfstream);
            FileStream pfxstream = new FileStream(_hostingEnvironment.WebRootPath + "//DigitalSignatureTest.pfx", FileMode.Open, FileAccess.Read);
            PdfCertificate certificate = new PdfCertificate(pfxstream, "DigitalPass123");
            PdfSignature signature = new PdfSignature(document, document.Pages[0], certificate, "DigitalSignature");
            signature.Settings.CryptographicStandard = CryptographicStandard.CADES;
            signature.Settings.DigestAlgorithm = DigestAlgorithm.SHA512;
            signature.TimeStampServer = new TimeStampServer(new Uri("http://timestamping.ensuredca.com"));
            signature.EnableLtv = true;
            MemoryStream stream = new MemoryStream();
            document.Save(stream);
            document.Close(true);
            return stream;
        }
        public MemoryStream WindowsStore()
        {
            FileStream pdfstream = new FileStream(_hostingEnvironment.WebRootPath + "//PDF_Succinctly.pdf", FileMode.Open, FileAccess.Read);
            PdfLoadedDocument document = new PdfLoadedDocument(pdfstream);
            X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
            X509Certificate2 digitalID = collection[0];
            PdfCertificate certificate = new PdfCertificate(digitalID);
            PdfSignature signature = new PdfSignature(document, document.Pages[0], certificate, "DigitalSignature");
            MemoryStream stream = new MemoryStream();
            document.Save(stream);
            document.Close(true);
            return stream;
        }
        public MemoryStream ExternalSignature()
        {
            FileStream pdfstream = new FileStream(_hostingEnvironment.WebRootPath + "//PDF_Succinctly.pdf", FileMode.Open, FileAccess.Read);
            PdfLoadedDocument document = new PdfLoadedDocument(pdfstream);
            PdfSignature signature = new PdfSignature(document, document.Pages[0], null, "DigitalSignature");
            signature.ComputeHash += Signature_ComputeHash;
            MemoryStream stream = new MemoryStream();
            document.Save(stream);
            document.Close(true);
            return stream;

        }

        void Signature_ComputeHash(object sender, PdfSignatureEventArgs arguments)
        {
            byte[] documentHash = arguments.Data;
            SignedCms signedCms = new SignedCms(new ContentInfo(documentHash), detached: true);
            X509Certificate2 certificate = new X509Certificate2(_hostingEnvironment.WebRootPath + "//DigitalSignatureTest.pfx", "DigitalPass123");
            var cmsSigner = new CmsSigner(certificate);
            cmsSigner.DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1");
            signedCms.ComputeSignature(cmsSigner);
            arguments.SignedData = signedCms.Encode();
        }

        public MemoryStream SignExisting()
        {
            FileStream pdfstream = new FileStream(_hostingEnvironment.WebRootPath + "//PDF_SignField.pdf", FileMode.Open, FileAccess.Read);
            PdfLoadedDocument document = new PdfLoadedDocument(pdfstream);
            PdfLoadedForm form = document.Form;
            PdfLoadedPage page = document.Pages[0] as PdfLoadedPage;
            PdfLoadedSignatureField field = document.Form.Fields[0] as PdfLoadedSignatureField;
            FileStream pfxstream = new FileStream(_hostingEnvironment.WebRootPath + "//DigitalSignatureTest.pfx", FileMode.Open, FileAccess.Read);
            PdfCertificate certificate = new PdfCertificate(pfxstream, "DigitalPass123");
            field.Signature = new PdfSignature(document, page, certificate, "Signature", field);

            FileStream imageStream = new FileStream(_hostingEnvironment.WebRootPath + "//signature.png", FileMode.Open, FileAccess.Read);
            PdfImage image = PdfImage.FromStream(imageStream);
            PdfStandardFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 15);
            PdfGraphics graphics = field.Signature.Appearance.Normal.Graphics;
            graphics.DrawRectangle(PdfPens.Black, PdfBrushes.White, new RectangleF(50, 0, field.Bounds.Width - 50, field.Bounds.Height));
            graphics.DrawImage(image, 0, 0, 100, field.Bounds.Height);
            graphics.DrawString("Digitally Signed by Syncfusion", font, PdfBrushes.Black, 120, 17);
            graphics.DrawString("Reason: Testing signature", font, PdfBrushes.Black, 120, 39);
            graphics.DrawString("Location: USA", font, PdfBrushes.Black, 120, 60);

            MemoryStream stream = new MemoryStream();
            document.Save(stream);
            document.Close(true);
            return stream;

        }
    }
}

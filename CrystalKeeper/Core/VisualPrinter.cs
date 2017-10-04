using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CrystalKeeper.Core
{
    /// <summary>
    /// A print mechanism that prints a large visual as bitmaps across
    /// multiple pages. Adapted from https://www.codeproject.com/Articles/339416/Printing-large-WPF-UserControls
    /// under the Code Project Open License (CPOL): https://www.codeproject.com/info/cpol10.aspx
    /// </summary>
    class VisualPrinter
    {
        #region Members
        /// <summary>
        /// The left and right-hand margins in pixels.
        /// </summary>
        public static int horzBorder;

        /// <summary>
        /// The top and bottom margins in pixels.
        /// </summary>
        public static int vertBorder;

        /// <summary>
        /// The expected horizontal DPI.
        /// </summary>
        private static double dpiX;

        /// <summary>
        /// The expected vertical DPI.
        /// </summary>
        private static double dpiY;
        #endregion

        #region Static Constructor
        /// <summary>
        /// Sets default member values.
        /// </summary>
        static VisualPrinter()
        {
            horzBorder = 48;
            vertBorder = 96;
            dpiX = 300;
            dpiY = 300;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Prints a visual, breaking across pages. The user should've already
        /// accepted the print job. Returns success.
        /// </summary>
        public static bool PrintAcrossPages(PrintDialog dlg, FrameworkElement element)
        {
            FrameworkElement printable = element;
            System.Drawing.Bitmap bmp = null;

            if (dlg != null && printable != null)
            {
                Mouse.OverrideCursor = Cursors.Wait;

                System.Printing.PrintCapabilities capabilities =
                    dlg.PrintQueue.GetPrintCapabilities(dlg.PrintTicket);
                dlg.PrintTicket.PageBorderless = System.Printing.PageBorderless.None;

                double dpiScale = dpiY / 96.0;
                FixedDocument document = new FixedDocument();

                try
                {
                    //Sets width and waits for changes to settle.
                    printable.Width = capabilities.PageImageableArea.ExtentWidth;
                    printable.UpdateLayout();

                    //Recomputes the desired height.
                    printable.Measure(new Size(
                        double.PositiveInfinity,
                        double.PositiveInfinity));

                    //Sets the new desired size.
                    Size size = new Size(
                        capabilities.PageImageableArea.ExtentWidth,
                        printable.DesiredSize.Height);

                    //Measures and arranges to the desired size.
                    printable.Measure(size);
                    printable.Arrange(new Rect(size));

                    //Converts GUI to bitmap at 300 DPI
                    RenderTargetBitmap bmpTarget = new RenderTargetBitmap(
                        (int)(capabilities.PageImageableArea.ExtentWidth * dpiScale),
                        (int)(printable.ActualHeight * dpiScale),
                        dpiX, dpiY, PixelFormats.Pbgra32);
                    bmpTarget.Render(printable);

                    //Converts RenderTargetBitmap to bitmap.
                    PngBitmapEncoder png = new PngBitmapEncoder();
                    png.Frames.Add(BitmapFrame.Create(bmpTarget));

                    using (MemoryStream memStream = new MemoryStream())
                    {
                        png.Save(memStream);
                        bmp = new System.Drawing.Bitmap(memStream);
                        png = null;
                    }

                    using (bmp)
                    {
                        document.DocumentPaginator.PageSize =
                            new Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);

                        //Breaks bitmap to fit across pages.
                        int pageBreak = 0;
                        int previousPageBreak = 0;
                        int pageHeight = (int)
                            (capabilities.PageImageableArea.ExtentHeight * dpiScale);

                        //Adds each full page.
                        while (pageBreak < bmp.Height - pageHeight)
                        {
                            pageBreak += pageHeight;

                            //Moves up in rows to find a place to break the page.
                            while (pageBreak > previousPageBreak &&
                                !IsRowGoodBreakingPoint(bmp, pageBreak))
                            {
                                pageBreak--;
                            }

                            //If no good row to break on was found,
                            //forcefully breaks at the original page size.
                            if (pageBreak == previousPageBreak)
                            {
                                pageBreak += pageHeight;
                            }

                            PageContent pageContent = GeneratePageContent(
                                bmp, previousPageBreak,
                                pageBreak, document.DocumentPaginator.PageSize.Width,
                                document.DocumentPaginator.PageSize.Height, capabilities);

                            document.Pages.Add(pageContent);
                            previousPageBreak = pageBreak;
                        }

                        //Adds remaining page contents.
                        PageContent lastPageContent = GeneratePageContent(
                            bmp, previousPageBreak,
                            bmp.Height, document.DocumentPaginator.PageSize.Width,
                            document.DocumentPaginator.PageSize.Height, capabilities);

                        document.Pages.Add(lastPageContent);
                    }
                }
                catch (Exception e)
                {
                    Utils.Log("Printing error: " + e.Message);
                    MessageBox.Show("An error occurred while trying " +
                        "to print.");
                }

                //Drops visual size adjustments.
                finally
                {
                    //Unsets width and waits for changes to settle.
                    printable.Width = double.NaN;
                    printable.UpdateLayout();

                    printable.LayoutTransform = new ScaleTransform(1, 1);

                    //Recomputes the desired height.
                    Size size = new Size(
                        capabilities.PageImageableArea.ExtentWidth,
                        capabilities.PageImageableArea.ExtentHeight);

                    //Measures and arranges to the desired size.
                    printable.Measure(size);
                    printable.Arrange(new Rect(new Point(
                        capabilities.PageImageableArea.OriginWidth,
                        capabilities.PageImageableArea.OriginHeight), size));

                    Mouse.OverrideCursor = null;
                }

                dlg.PrintDocument(document.DocumentPaginator, "Crystal Keeper");
                return true;
            }

            return false;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Sizes the given bitmap to the page size and returns it as part
        /// of a printable page.
        /// </summary>
        private static PageContent GeneratePageContent(
            System.Drawing.Bitmap bmp,
            int top,
            int bottom,
            double pageWidth,
            double PageHeight,
            System.Printing.PrintCapabilities capabilities)
        {
            Image pageImage;
            BitmapSource bmpSource;

            //Creates a page with a specific width/height.
            FixedPage printPage = new FixedPage();
            printPage.Width = pageWidth;
            printPage.Height = PageHeight;

            //Cuts the given image at a reasonable boundary.
            int newImageHeight = bottom - top;

            //Creates a clone of the image.
            using (System.Drawing.Bitmap bmpCut =
                bmp.Clone(new System.Drawing.Rectangle(0, top, bmp.Width, newImageHeight),
                bmp.PixelFormat))
            {
                //Prepares the bitmap source.
                pageImage = new Image();
                bmpSource =
                    System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        bmpCut.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(bmpCut.Width, bmpCut.Height));
            }

            //Adds the bitmap to the page.
            pageImage.Source = bmpSource;
            pageImage.VerticalAlignment = VerticalAlignment.Top;
            printPage.Children.Add(pageImage);

            PageContent pageContent = new PageContent();
            ((System.Windows.Markup.IAddChild)pageContent).AddChild(printPage);

            //Adds a margin.
            printPage.Margin = new Thickness(
                horzBorder, vertBorder,
                horzBorder, vertBorder);

            FixedPage.SetLeft(pageImage, capabilities.PageImageableArea.OriginWidth);
            FixedPage.SetTop(pageImage, capabilities.PageImageableArea.OriginHeight);

            //Adjusts for the margins and to fit the page.
            pageImage.Width = capabilities.PageImageableArea.ExtentWidth - horzBorder*2;
            pageImage.Height = capabilities.PageImageableArea.ExtentHeight - vertBorder*2;
            return pageContent;
        }

        /// <summary>
        /// If the colors in a bitmap row do not vary heavily, the image is
        /// deemed to be separable at that row.
        /// </summary>
        private static bool IsRowGoodBreakingPoint(
            System.Drawing.Bitmap bmp,
            int row)
        {
            double maxDeviationForEmptyLine = 1627500; //A deviation tolerance
            bool goodBreakingPoint = false;

            if (RowPixelDeviation(bmp, row) < maxDeviationForEmptyLine)
            {
                goodBreakingPoint = true;
            }

            return goodBreakingPoint;
        }

        /// <summary>
        /// Returns a double indicating the level of color deviation across a
        /// row in the bitmap.
        /// </summary>
        private static unsafe double RowPixelDeviation(
            System.Drawing.Bitmap bmp,
            int row)
        {
            int count = 0;
            double total = 0;
            double totalVariance = 0;
            double standardDeviation = 0;

            //Locks to read data.
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr firstPixelInImage = bmpData.Scan0;

            //Gets the initial row's leftmost pixel.
            byte* p = (byte*)(void*)firstPixelInImage;
            p += stride * row;  //Finds starting pixel of the specified row

            //Iterates through each consecutive pixel in the given row.
            for (int column = 0; column < bmp.Width; column++)
            {
                count++;

                byte blue = p[0];
                byte green = p[1];
                byte red = p[3];

                int pixelValue = System.Drawing.Color.FromArgb(0, red, green, blue).ToArgb();
                total += pixelValue;
                double average = total / count;
                totalVariance += Math.Pow(pixelValue - average, 2);
                standardDeviation = Math.Sqrt(totalVariance / count);

                //Skips three channels to next pixel.
                p += 3;
            }

            bmp.UnlockBits(bmpData);

            return standardDeviation;
        }
        #endregion
    }
}
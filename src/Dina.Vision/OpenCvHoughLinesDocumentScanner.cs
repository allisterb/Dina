namespace Dina;

using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;


    public class OpenCvHoughLinesDocumentScanner : Runtime
    {
        // Computes the intersection point of two lines given in polar coordinates.
        // Returns null if the lines are nearly parallel.
        static Point? Intersection(Vec2f line1, Vec2f line2)
        {
            float rho1 = line1.Item0, theta1 = line1.Item1;
            float rho2 = line2.Item0, theta2 = line2.Item1;
            // Only compute intersection if the lines are not almost parallel.
            if (Math.Abs(theta1 - theta2) <= 1.3)
                return null;

            double cos1 = Math.Cos(theta1), sin1 = Math.Sin(theta1);
            double cos2 = Math.Cos(theta2), sin2 = Math.Sin(theta2);
            double det = cos1 * sin2 - sin1 * cos2;
            if (Math.Abs(det) < 1e-10)
                return null;

            double x = (rho1 * sin2 - sin1 * rho2) / det;
            double y = (cos1 * rho2 - rho1 * cos2) / det;
            return new Point((int)Math.Round(x), (int)Math.Round(y));
        }

        // Finds intersections among lines (each represented by (rho, theta)).
        static List<Point> FindIntersections(List<Vec2f> lines)
        {
            List<Point> intersections = new List<Point>();
            for (int i = 0; i < lines.Count - 1; i++)
            {
                for (int j = i + 1; j < lines.Count; j++)
                {
                    Point? pt = Intersection(lines[i], lines[j]);
                    if (pt.HasValue)
                        intersections.Add(pt.Value);
                }
            }
            return intersections;
        }

        public static void Scan(string filepath)
        {
            // Read the input image.
            Mat img = Cv2.ImRead(filepath);
            if (img.Empty())
            {
                Console.WriteLine("Could not load image.");
                return;
            }

            // Convert to grayscale.
            Mat gray = new Mat();
            Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);

            // Apply dilation.
            Mat kernel = Mat.Ones(rows:5, cols:5, MatType.CV_8U);
            Mat dilation = new Mat();
            Cv2.Dilate(gray, dilation, kernel, iterations: 5);

            // Apply Gaussian blur.
            Mat blur = new Mat();
            Cv2.GaussianBlur(dilation, blur, new Size(3, 3), 0);

            // Apply erosion.
            Cv2.Erode(blur, blur, kernel, iterations: 5);

            // Edge detection using Canny.
            Mat edge = new Mat();
            Cv2.Canny(blur, edge, 100, 200);

            // Adjust threshold for HoughLines until we have a sufficient number of lines.
            int t = 300, c = 0, j = 0;
            LineSegmentPolar[]? houghLines = null;
            Vec2f[]? houghLinesVec = null;   
            while (j < 8 && c < 30)
            {
                try
                {
                    houghLines = Cv2.HoughLines(edge, 1, Math.PI / 180, t);
                    j = (houghLines != null) ? houghLines.Length : 0;
                }
                catch
                {
                    j = 0;
                }
                c++;
                t -= 10;
            }
            houghLinesVec = houghLines.Select(l => new Vec2f(l.Rho * MathF.Cos(l.Theta), -l.Rho * MathF.Sin(l.Theta))).ToArray();

            // Create a copy of the original image to draw lines.
            Mat imgr = img.Clone();
            // Filter out similar lines.
            List<Vec2f> uniqueLines = new List<Vec2f>();
            if (houghLines != null)
            {
                // Process each line.
                for (int i = 0; i < houghLines.Length; i++)
                {
                    Vec2f l = houghLinesVec[i];
                    bool skip = false;
                    // Compare with subsequent lines.
                    for (int k = i + 1; k < houghLines.Length; k++)
                    {
                        Vec2f lt = houghLinesVec[k];
                        if (Math.Abs(lt.Item0 - l.Item0) < 50 && Math.Abs(lt.Item1 - l.Item1) < 0.5)
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip)
                        continue;

                    float rho = houghLines[i].Rho, theta = houghLines[i].Theta;
                    double a = Math.Cos(theta), b = Math.Sin(theta);
                    double x0 = a * rho, y0 = b * rho;
                    int x1 = (int)(x0 + 1000 * (-b));
                    int y1 = (int)(y0 + 1000 * (a));
                    int x2 = (int)(x0 - 1000 * (-b));
                    int y2 = (int)(y0 - 1000 * (a));
                    Cv2.Line(imgr, new Point(x1, y1), new Point(x2, y2), new Scalar(0, 0, 255), 2);
                    uniqueLines.Add(l);
                }
            }

            // Find intersections from the unique lines.
            List<Point> intersections = FindIntersections(uniqueLines);

            // Draw intersections on a copy of the original image.
            Mat imgt = img.Clone();
            List<Point> validPoints = new List<Point>();
            foreach (Point pt in intersections)
            {
                if (pt.X < 0 || pt.Y < 0 || pt.X > imgt.Width || pt.Y > imgt.Height)
                    continue;
                validPoints.Add(pt);
                Cv2.Circle(imgt, pt, 10, new Scalar(0, 255, 0), -1);
            }

            // For perspective transform, assume we need four corner points.
            if (validPoints.Count < 4)
            {
                Console.WriteLine("Not enough intersection points found.");
                return;
            }
            // Take the first four valid points.
            Point2f[] pts = validPoints.Take(4)
                                         .Select(p => new Point2f(p.X, p.Y))
                                         .ToArray();

            // Order points: top-left, top-right, bottom-right, bottom-left.
            Point2f[] r = new Point2f[4];
            float[] sums = pts.Select(p => p.X + p.Y).ToArray();
            r[0] = pts[Array.IndexOf(sums, sums.Min())]; // top-left
            r[2] = pts[Array.IndexOf(sums, sums.Max())]; // bottom-right
            float[] diff = pts.Select(p => p.X - p.Y).ToArray();
            r[1] = pts[Array.IndexOf(diff, diff.Min())]; // top-right
            r[3] = pts[Array.IndexOf(diff, diff.Max())]; // bottom-left

            Point2f tl = r[0], tr = r[1], br = r[2], bl = r[3];

            // Compute width and height for the new transformed image.
            double wA = Math.Sqrt(Math.Pow(tl.X - tr.X, 2) + Math.Pow(tl.Y - tr.Y, 2));
            double wB = Math.Sqrt(Math.Pow(bl.X - br.X, 2) + Math.Pow(bl.Y - br.Y, 2));
            int maxW = (int)Math.Max(wA, wB);

            double hA = Math.Sqrt(Math.Pow(tl.X - bl.X, 2) + Math.Pow(tl.Y - bl.Y, 2));
            double hB = Math.Sqrt(Math.Pow(tr.X - br.X, 2) + Math.Pow(tr.Y - br.Y, 2));
            int maxH = (int)Math.Max(hA, hB);

            // Set destination points for the perspective transform.
            Point2f[] dstPts = new Point2f[]
            {
                new Point2f(0, 0),
                new Point2f(maxW - 1, 0),
                new Point2f(maxW - 1, maxH - 1),
                new Point2f(0, maxH - 1)
            };

            // Get the perspective transform matrix and apply it.
            Mat transformMatrix = Cv2.GetPerspectiveTransform(r, dstPts);
            Mat scan = new Mat();
            Cv2.WarpPerspective(gray, scan, transformMatrix, new Size(maxW, maxH));

            // Apply adaptive thresholding.
            Mat scanBW = new Mat();
            // AdaptiveThreshold in OpenCvSharp uses blockSize (must be odd) and C offset.
            // This mimics the skimage threshold_local with a blockSize of 21 and offset 10.
            Cv2.AdaptiveThreshold(scan, scanBW, 255, AdaptiveThresholdTypes.GaussianC,
                                    ThresholdTypes.Binary, 21, 10);

            // Save the resulting image.
            Cv2.ImWrite("document_adv_res.png", scanBW);

            // Optionally, show windows for debugging.
            Cv2.ImShow("Edges", edge);
            Cv2.ImShow("Hough Lines", imgr);
            Cv2.ImShow("Intersections", imgt);
            Cv2.ImShow("Transformed", scanBW);
            Cv2.WaitKey();
            Cv2.DestroyAllWindows();
        }
    }

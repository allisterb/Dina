namespace Dina;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using OpenCvSharp;

/* 
 * Translated from: https://github.com/andrewdcampbell/OpenCV-Document-Scanner/blob/master/scan.py
 */
public class OpenCvDocumentScanner
{
    double MIN_QUAD_AREA_RATIO;
    double MAX_QUAD_ANGLE_RANGE;

    public OpenCvDocumentScanner(double minQuadAreaRatio = 0.25, double maxQuadAngleRange = 40)
    {
        MIN_QUAD_AREA_RATIO = minQuadAreaRatio;
        MAX_QUAD_ANGLE_RANGE = maxQuadAngleRange;
    }

    // Returns the angle (in degrees) between two 2D vectors.
    private double AngleBetweenVectorsDegrees(Point2f u, Point2f v)
    {
        double dot = u.X * v.X + u.Y * v.Y;
        double normU = Math.Sqrt(u.X * u.X + u.Y * u.Y);
        double normV = Math.Sqrt(v.X * v.X + v.Y * v.Y);
        return Math.Acos(dot / (normU * normV)) * 180.0 / Math.PI;
    }

    // Returns the angle (in degrees) at p2 formed with points p1 and p3.
    private double GetAngle(Point2f p1, Point2f p2, Point2f p3)
    {
        Point2f u = new Point2f(p1.X - p2.X, p1.Y - p2.Y);
        Point2f v = new Point2f(p3.X - p2.X, p3.Y - p2.Y);
        return AngleBetweenVectorsDegrees(u, v);
    }

    // Returns the difference between the maximum and minimum interior angles
    // of a quadrilateral. It expects the points are ordered as: top-left, top-right, bottom-right, bottom-left.
    private double AngleRange(Point2f[] quad)
    {
        double ura = GetAngle(quad[0], quad[1], quad[2]); // upper right angle at top-right
        double ula = GetAngle(quad[3], quad[0], quad[1]); // upper left angle at top-left
        double lra = GetAngle(quad[1], quad[2], quad[3]); // lower right angle at bottom-right
        double lla = GetAngle(quad[2], quad[3], quad[0]); // lower left angle at bottom-left

        double maxAngle = Math.Max(Math.Max(ura, ula), Math.Max(lra, lla));
        double minAngle = Math.Min(Math.Min(ura, ula), Math.Min(lra, lla));
        return maxAngle - minAngle;
    }

    // Returns true if the contour (4 points) has sufficient area and a tight range of angles.
    private bool IsValidContour(Point[] contour, int imWidth, int imHeight)
    {
        if (contour.Length != 4)
            return false;
        double area = Cv2.ContourArea(contour);
        if (area < imWidth * imHeight * MIN_QUAD_AREA_RATIO)
            return false;
        // Convert to Point2f[] and order the points.
        Point2f[] pts = Array.ConvertAll(contour, p => new Point2f(p.X, p.Y));
        pts = OrderPoints(pts);
        double range = AngleRange(pts);
        return range < MAX_QUAD_ANGLE_RANGE;
    }

    // Attempts to find the document contour from the rescaled image.
    public Point2f[] GetContour(Mat rescaledImage)
    {
        int MORPH = 9;
        int CANNY = 84;
        int imHeight = rescaledImage.Rows;
        int imWidth = rescaledImage.Cols;

        Mat gray = new Mat();
        Cv2.CvtColor(rescaledImage, gray, ColorConversionCodes.BGR2GRAY);
        Mat blurred = new Mat();
        Cv2.GaussianBlur(gray, blurred, new Size(7, 7), 0);
        Mat dilated = new Mat();
        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(MORPH, MORPH));
        Cv2.MorphologyEx(blurred, dilated, MorphTypes.Close, kernel);
        Mat edged = new Mat();
        Cv2.Canny(dilated, edged, 0, CANNY);

        Point[][] contours;
        HierarchyIndex[] hierarchy;
        Cv2.FindContours(edged.Clone(), out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        // Sort contours by descending area and take the top 5.
        var sorted = contours.OrderByDescending(c => Cv2.ContourArea(c)).Take(5).ToArray();
        List<Point[]> approxContours = new List<Point[]>();

        foreach (var c in sorted)
        {
            Point[] approx = Cv2.ApproxPolyDP(c, 80, true);
            if (IsValidContour(approx, imWidth, imHeight))
            {
                approxContours.Add(approx);
                break;
            }
        }

        Point[] screenCnt;
        if (approxContours.Count == 0)
        {
            // If no valid contour is found, use the image corners.
            screenCnt = new Point[]
            {
                    new Point(imWidth, 0),
                    new Point(imWidth, imHeight),
                    new Point(0, imHeight),
                    new Point(0, 0)
            };
        }
        else
        {
            screenCnt = approxContours.OrderByDescending(c => Cv2.ContourArea(c)).First();
        }
        return Array.ConvertAll(screenCnt, p => new Point2f(p.X, p.Y));
    }

    // Orders the points in the following order: top-left, top-right, bottom-right, bottom-left.
    public static Point2f[] OrderPoints(Point2f[] pts)
    {
        Point2f[] rect = new Point2f[4];
        float[] sum = pts.Select(p => p.X + p.Y).ToArray();
        rect[0] = pts[Array.IndexOf(sum, sum.Min())];
        rect[2] = pts[Array.IndexOf(sum, sum.Max())];
        float[] diff = pts.Select(p => p.X - p.Y).ToArray();
        rect[1] = pts[Array.IndexOf(diff, diff.Min())];
        rect[3] = pts[Array.IndexOf(diff, diff.Max())];
        return rect;
    }

    // Applies a four-point perspective transform to obtain a top-down view of the document.
    public static Mat FourPointTransform(Mat image, Point2f[] pts)
    {
        pts = OrderPoints(pts);
        Point2f tl = pts[0], tr = pts[1], br = pts[2], bl = pts[3];

        double widthA = Math.Sqrt(Math.Pow(br.X - bl.X, 2) + Math.Pow(br.Y - bl.Y, 2));
        double widthB = Math.Sqrt(Math.Pow(tr.X - tl.X, 2) + Math.Pow(tr.Y - tl.Y, 2));
        int maxWidth = (int)Math.Max(widthA, widthB);

        double heightA = Math.Sqrt(Math.Pow(tr.X - br.X, 2) + Math.Pow(tr.Y - br.Y, 2));
        double heightB = Math.Sqrt(Math.Pow(tl.X - bl.X, 2) + Math.Pow(tl.Y - bl.Y, 2));
        int maxHeight = (int)Math.Max(heightA, heightB);

        Point2f[] dst = new Point2f[]
        {
                new Point2f(0, 0),
                new Point2f(maxWidth - 1, 0),
                new Point2f(maxWidth - 1, maxHeight - 1),
                new Point2f(0, maxHeight - 1)
        };

        Mat M = Cv2.GetPerspectiveTransform(pts, dst);
        Mat warped = new Mat();
        Cv2.WarpPerspective(image, warped, M, new Size(maxWidth, maxHeight));
        return warped;
    }

    // Scans the image at imagePath, detects the document, applies perspective transform,
    // sharpens, thresholds, and saves the output to the "output" directory.
    public void Scan(string imagePath, string outputPath)
    {
        double RESCALED_HEIGHT = 500.0;
        string OUTPUT_DIR = "output";

        Mat image = Cv2.ImRead(imagePath);
        if (image.Empty())
        {
            Console.WriteLine("Unable to open image " + imagePath);
            return;
        }

        double ratio = image.Height / RESCALED_HEIGHT;
        Mat orig = image.Clone();
        int newHeight = (int)RESCALED_HEIGHT;
        int newWidth = (int)(image.Width * RESCALED_HEIGHT / image.Height);
        Mat rescaled = new Mat();
        Cv2.Resize(image, rescaled, new Size(newWidth, newHeight));

        Point2f[] screenCnt = GetContour(rescaled);
        // (Interactive mode to adjust corners is not implemented.)
        // Multiply the contour coordinates by the ratio to map back to the original image.
        Point2f[] scaledCnt = screenCnt.Select(p => new Point2f(p.X * (float)ratio, p.Y * (float)ratio)).ToArray();
        Mat warped = FourPointTransform(orig, scaledCnt);

        // Convert to grayscale.
        Mat gray = new Mat();
        Cv2.CvtColor(warped, gray, ColorConversionCodes.BGR2GRAY);
        // Apply Gaussian blur and sharpen the image.
        Mat blurred = new Mat();
        Cv2.GaussianBlur(gray, blurred, new Size(0, 0), 3);
        Mat sharpened = new Mat();
        Cv2.AddWeighted(gray, 1.5, blurred, -0.5, 0, sharpened);
        // Apply adaptive threshold for a black-and-white effect.
        Mat thresh = new Mat();
        Cv2.AdaptiveThreshold(sharpened, thresh, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 21, 15);
        Cv2.ImWrite(outputPath, thresh);
    }
}

namespace LinkedInPuzzles.Service.ZipProblem.ImageProcessing
{
    // Class to hold detection results
    public class DetectionResult
    {
        public int Number { get; set; }
        public float Confidence { get; set; }
        public PreprocessingParameters Parameters { get; set; }
    }
}


namespace QueensProblem.Service.ZipProblem.ImageProcessing
{
    /// <summary>
    /// Parameters for image preprocessing operations
    /// </summary>
    public class PreprocessingParameters
    {
        /// <summary>
        /// Size of kernel for morphological operations
        /// </summary>
        public int MorphKernelSize { get; set; } = 3;
        
        /// <summary>
        /// Number of dilation iterations to apply
        /// </summary>
        public int DilateIterations { get; set; } = 1;
    }
} 
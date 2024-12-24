using System.Drawing;
using System;

namespace ClipboardManager
{
    public class ClipboardEntry
    {
        public string TextData { get; set; } // Holds text data (if any)
        public Image OriginalImage { get; set; } // Original image from the clipboard
        public Image PreviewImage { get; set; } // Scaled-down image for preview

        public bool IsText => TextData != null; // Check if this entry contains text
        public bool IsImage => OriginalImage != null; // Check if this entry contains an image

        // Constructor for text data
        public ClipboardEntry(string textData)
        {
            TextData = textData;
            OriginalImage = null;
            PreviewImage = null;
        }

        // Constructor for image data
        public ClipboardEntry(Image originalImage, int previewHeight)
        {
            OriginalImage = originalImage;
            PreviewImage = GetThumbnailImage(originalImage, previewHeight); // Generate a fast thumbnail
            TextData = null;
        }

        // Method to generate a thumbnail image for preview
        private Image GetThumbnailImage(Image originalImage, int height)
        {
            int width = originalImage.Width * height / originalImage.Height; // Maintain aspect ratio
            return originalImage.GetThumbnailImage(width, height, null, IntPtr.Zero); // Get thumbnail
        }

        // Dispose method to release resources
        public void Dispose()
        {
            OriginalImage?.Dispose();
            PreviewImage?.Dispose();
        }
    }
}


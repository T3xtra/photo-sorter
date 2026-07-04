namespace PhotoSorter.Core.ViewModels;

/// <summary>Display mode of the image viewer.</summary>
public enum ZoomMode
{
    /// <summary>The image is scaled to fit entirely within the viewer (the default for a newly shown image).</summary>
    FitToWindow,

    /// <summary>The image is shown at its native resolution, scaled by <c>ImageViewerViewModel.ZoomFactor</c> ("100 %" is 1.0).</summary>
    Manual,
}

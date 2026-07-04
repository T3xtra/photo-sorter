using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.ViewModels;

/// <summary>One entry in the thumbnail strip.</summary>
public sealed partial class ThumbnailItemViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;

    [ObservableProperty]
    private byte[]? _thumbnailBytes;

    [ObservableProperty]
    private bool _isCurrent;

    /// <summary>Set when the thumbnail failed to generate (corrupted/missing file, no access - Roadmap Phase 16).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToolTipText))]
    private bool _hasError;

    public ThumbnailItemViewModel(ImageFile imageFile, IProjectService projectService)
    {
        ImageFile = imageFile;
        _projectService = projectService;
    }

    public ImageFile ImageFile { get; }

    public string FileName => ImageFile.FileName;

    public string ToolTipText => HasError ? $"{FileName} (Bild konnte nicht geladen werden)" : FileName;

    [RelayCommand]
    private void Select()
    {
        var images = _projectService.Current.Images;
        for (var i = 0; i < images.Count; i++)
        {
            if (ReferenceEquals(images[i], ImageFile))
            {
                _projectService.MoveTo(i);
                return;
            }
        }
    }
}

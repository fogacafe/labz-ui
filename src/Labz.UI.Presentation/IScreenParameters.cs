namespace Labz.UI.Presentation;

/// <summary>Optional typed input contract for a screen presenter.</summary>
public interface IScreenParameters<in TParameters>
{
    void SetParameters(TParameters parameters);
}

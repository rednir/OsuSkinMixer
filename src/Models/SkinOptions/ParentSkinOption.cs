using System.Linq;

namespace OsuSkinMixer.Models;

/// <summary>Represents a skin option category that acts as a parent of one or more skin options. It does not target any skin elements on its own.</summary>
public class ParentSkinOption : SkinOption
{
    public SkinOption[] Children { get; set; }

    public override string ToString() => string.Join(", ", Children.Select(c => c.Name));
}
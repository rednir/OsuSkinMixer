using System.Linq;

namespace OsuSkinMixer.Models.SkinOptions;

public class ParentSkinOption : SkinOption
{
    public SkinOption[] Children { get; set; }

    public override string ToString() => string.Join(", ", Children.Select(c => c.Name));
}
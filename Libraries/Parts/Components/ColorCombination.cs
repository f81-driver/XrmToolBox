using System.Windows.Media;

namespace Formula81.XrmToolBox.Libraries.Parts.Components
{
    public readonly struct ColorCombination
    {
        public Color PrimaryBackground { get; }
        public Color SecondaryBackground { get; }
        public Color PrimaryForeground { get; }
        public Color SecondaryForeground { get; }

        public ColorCombination(Color primaryBackground, Color secondaryBackground, Color primaryForeground)
            : this(primaryBackground, secondaryBackground, primaryForeground, primaryForeground) { }

        public ColorCombination(Color primaryBackground, Color secondaryBackground, Color primaryForeground, Color secondaryForeground)
        {
            PrimaryBackground = primaryBackground;
            SecondaryBackground = secondaryBackground;
            PrimaryForeground = primaryForeground;
            SecondaryForeground = secondaryForeground;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + PrimaryBackground.GetHashCode();
                hash = hash * 23 + SecondaryBackground.GetHashCode();
                hash = hash * 23 + PrimaryForeground.GetHashCode();
                hash = hash * 23 + SecondaryForeground.GetHashCode();
                return hash;
            }
        }

    }
}

using Formula81.XrmToolBox.Shared.Parts.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Components
{
    public static class ColorCombinations
    {
        public static Color AutumnLeaves1Primary = FromHexString("#FFB343");
        public static Color AutumnLeaves1Secondary = FromHexString("#DB9A39");
        public static ColorCombination AutumnLeaves1 = new ColorCombination(AutumnLeaves1Primary, AutumnLeaves1Secondary, Colors.Black);

        public static Color AutumnLeaves2Primary = FromHexString("#B37E2E");
        public static Color AutumnLeaves2Secondary = FromHexString("#614419");
        public static ColorCombination AutumnLeaves2 = new ColorCombination(AutumnLeaves2Primary, AutumnLeaves2Secondary, Colors.Black, Colors.White);

        public static Color EmeraldOdyssey1Primary = FromHexString("#00674F");
        public static Color EmeraldOdyssey1Secondary = FromHexString("#0A3C30");
        public static ColorCombination EmeraldOdyssey1 = new ColorCombination(EmeraldOdyssey1Primary, EmeraldOdyssey1Secondary, Colors.White, Colors.Black);

        public static Color EmeraldOdyssey2Primary = FromHexString("#73E6CB");
        public static Color EmeraldOdyssey2Secondary = FromHexString("#3EBB9E");
        public static ColorCombination EmeraldOdyssey2 = new ColorCombination(EmeraldOdyssey2Primary, EmeraldOdyssey2Secondary, Colors.Black);

        public static Color IslandOasis1Primary = FromHexString("#CCFF00");
        public static Color IslandOasis1Secondary = FromHexString("#9DE05A");
        public static ColorCombination IslandOasis1 = new ColorCombination(IslandOasis1Primary, IslandOasis1Secondary, Colors.Black);

        public static Color IslandOasis2Primary = FromHexString("#FFF37E");
        public static Color IslandOasis2Secondary = FromHexString("#FFAE25");
        public static ColorCombination IslandOasis2 = new ColorCombination(IslandOasis2Primary, IslandOasis2Secondary, Colors.Black);

        public static Color OceanTidePrimary = FromHexString("#00ADAD");
        public static Color OceanTideSecondary = FromHexString("#005C5C");
        public static ColorCombination OceanTide = new ColorCombination(OceanTidePrimary, OceanTideSecondary, Colors.Black, Colors.White);

        public static Color PeachSkylinePrimary = FromHexString("#BADDFF");
        public static Color PeachSkylineSecondary = FromHexString("#496580");
        public static ColorCombination PeachSkyline = new ColorCombination(PeachSkylinePrimary, PeachSkylineSecondary, Colors.Black, Colors.White);

        public static Color PumpkinSpiceSeason1Primary = FromHexString("#BE5103");
        public static Color PumpkinSpiceSeason1Secondary = FromHexString("#8C4C1F");
        public static ColorCombination PumpkinSpiceSeason1 = new ColorCombination(PumpkinSpiceSeason1Primary, PumpkinSpiceSeason1Secondary, Colors.White);

        public static Color PumpkinSpiceSeason2Primary = FromHexString("#544823");
        public static Color PumpkinSpiceSeason2Secondary = FromHexString("#332216");
        public static ColorCombination PumpkinSpiceSeason2 = new ColorCombination(PumpkinSpiceSeason2Primary, PumpkinSpiceSeason2Secondary, Colors.White);

        public static Color RetroCalmPrimary = FromHexString("#81D8D0");
        public static Color RetroCalmSecondary = FromHexString("#D7D982");
        public static ColorCombination RetroCalm = new ColorCombination(RetroCalmPrimary, RetroCalmSecondary, Colors.Black);

        public static Color Siltstone1Primary = FromHexString("#CBBD93");
        public static Color Siltstone1Secondary = FromHexString("#CCA25A");
        public static ColorCombination Siltstone1 = new ColorCombination(Siltstone1Primary, Siltstone1Secondary, Colors.Black);

        public static Color Siltstone2Primary = FromHexString("#FFF5B8");
        public static Color Siltstone2Secondary = FromHexString("#FFB16E");
        public static ColorCombination Siltstone2 = new ColorCombination(Siltstone2Primary, Siltstone2Secondary, Colors.Black);

        public static Color SoftSpring1Primary = FromHexString("#6395EE");
        public static Color SoftSpring1Secondary = FromHexString("#90B8D6");
        public static ColorCombination SoftSpring1 = new ColorCombination(SoftSpring1Primary, SoftSpring1Secondary, Colors.Black);

        public static Color SoftSpring2Primary = FromHexString("#88CFA8");
        public static Color SoftSpring2Secondary = FromHexString("#85DECB");
        public static ColorCombination SoftSpring2 = new ColorCombination(SoftSpring2Primary, SoftSpring2Secondary, Colors.Black);

        public static Color SpringEnergyPrimary = FromHexString("#89F336");
        public static Color SpringEnergySecondary = FromHexString("#00BF33");
        public static ColorCombination SpringEnergy = new ColorCombination(SpringEnergyPrimary, SpringEnergySecondary, Colors.Black, Colors.White);

        public static Color WinterChill1Primary = FromHexString("#B8E3E9");
        public static Color WinterChill1Secondary = FromHexString("#93B1B5");
        public static ColorCombination WinterChill1 = new ColorCombination(WinterChill1Primary, WinterChill1Secondary, Colors.Black);

        public static Color WinterChill2Primary = FromHexString("#4F7C82");
        public static Color WinterChill2Secondary = FromHexString("#0B2E33");
        public static ColorCombination WinterChill2 = new ColorCombination(WinterChill2Primary, WinterChill2Secondary, Colors.White);

        public static Color StormyMorning1Primary = FromHexString("#BDDDFC");
        public static Color StormyMorning1Secondary = FromHexString("#88BDF2");
        public static ColorCombination StormyMorning1 = new ColorCombination(StormyMorning1Primary, StormyMorning1Secondary, Colors.Black);

        public static Color StormyMorning2Primary = FromHexString("#6A89A7");
        public static Color StormyMorning2Secondary = FromHexString("#384959");
        public static ColorCombination StormyMorning2 = new ColorCombination(StormyMorning2Primary, StormyMorning2Secondary, Colors.White);

        public static Color MossyHollow1Primary = FromHexString("#636B2F");
        public static Color MossyHollow1Secondary = FromHexString("#3D4127");
        public static ColorCombination MossyHollow1 = new ColorCombination(MossyHollow1Primary, MossyHollow1Secondary, Colors.White);

        public static Color MossyHollow2Primary = FromHexString("#BAC095");
        public static Color MossyHollow2Secondary = FromHexString("#D4DE95");
        public static ColorCombination MossyHollow2  = new ColorCombination(MossyHollow2Primary, MossyHollow2Secondary, Colors.Black);

        public static Color BlueEclipse1Primary = FromHexString("#272757");
        public static Color BlueEclipse1Secondary = FromHexString("#0F0E47");
        public static ColorCombination BlueEclipse1 = new ColorCombination(BlueEclipse1Primary, BlueEclipse1Secondary, Colors.White);

        public static Color BlueEclipse2Primary = FromHexString("#8686AC");
        public static Color BlueEclipse2Secondary = FromHexString("#505081");
        public static ColorCombination lipse2 = new ColorCombination(BlueEclipse2Primary, BlueEclipse2Secondary, Colors.White);

        public static Color LushForest1Primary = FromHexString("#2E6F40");
        public static Color LushForest1Secondary = FromHexString("#253D2C");
        public static ColorCombination LushForest1 = new ColorCombination(LushForest1Primary, LushForest1Secondary, Colors.White);

        public static Color LushForest2Primary = FromHexString("#CFFFDC");
        public static Color LushForest2Secondary = FromHexString("#68BA7F");
        public static ColorCombination LushForest2 = new ColorCombination(LushForest2Primary, LushForest2Secondary, Colors.Black, Colors.White);

        public static Color GreenJuice1Primary = FromHexString("#48872B");
        public static Color GreenJuice1Secondary = FromHexString("#4CBB17");
        public static ColorCombination GreenJuice1 = new ColorCombination(GreenJuice1Primary, GreenJuice1Secondary, Colors.White, Colors.Black);

        public static Color GreenJuice2Primary = FromHexString("#38542C");
        public static Color GreenJuice2Secondary = FromHexString("#293325");
        public static ColorCombination GreenJuice2 = new ColorCombination(GreenJuice2Primary, GreenJuice2Secondary, Colors.White);

        public static Color ChiliSpice1Primary = FromHexString("#CD1C18");
        public static Color ChiliSpice1Secondary = FromHexString("#FFA896");
        public static ColorCombination ChiliSpice1 = new ColorCombination(ChiliSpice1Primary, ChiliSpice1Secondary, Colors.White, Colors.Black);

        public static Color ChiliSpice2Primary = FromHexString("#9B1313");
        public static Color ChiliSpice2Secondary = FromHexString("#38000A");
        public static ColorCombination ChiliSpice2 = new ColorCombination(ChiliSpice2Primary, ChiliSpice2Secondary, Colors.White);

        static ColorCombinations()
        {
            All = GetAllColorCombinations();
        }

        public static IEnumerable<ColorCombination> All { get; }

        private static IEnumerable<ColorCombination> GetAllColorCombinations()
        {
            return typeof(ColorCombinations)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(ColorCombination))
                .Select(field => (ColorCombination)field.GetValue(null))
                .ToList();
        }

        public static ColorCombination GetRandom(IEnumerable<ColorCombination> exclude, out bool allUsed)
        {
            var colors = All.Except(exclude).ToList();
            allUsed = colors.Count == 0;
            if (allUsed)
            {
                colors = All.ToList();
            }
            var random = new Random();
            return colors[random.Next(colors.Count)];
        }

        public static string GetColorName(Color color)
        {
            var colorProperties = typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static);
            foreach (var prop in colorProperties)
            {
                var namedColor = (Color)prop.GetValue(null);
                if (color.Equals(namedColor))
                {
                    return prop.Name;
                }
            }
            return color.ToString();
        }

        private static Color FromHexString(string hexColor)
        {
            if (!hexColor.StartsWith("#"))
                hexColor = "#" + hexColor;

            return (Color)ColorConverter.ConvertFromString(hexColor);
        }
    }
}

using UnityEngine;

namespace Managers.Bots
{
    public static class PseudoGenerator
    {
        static readonly string[] s_NameRoots = new[]
        {
            "Grim", "Rapt", "Kair", "Sel", "Nept", "Exo", "Max", "Fel", "Aza",
            "Dia", "Frex", "Geo", "Kir", "Grut", "Frid", "Flux", "Drast", "Vii",
            "Crist", "Scub", "Xano", "Tim", "Dank", "Flux", "Rof", "Zylo", "Mey"
        };

        static readonly string[] s_NameEndings = new[]
        {
            "dal", "or", "fan", "tade", "rin", "nus", "minate", "riu",
            "berto", "epic", "alee", "love", "aly", "ina", "name", "man",
            "mad", "ster", "fly", "max", "bio", "ser", "bane", "dex", "zy", "ix"
        };

        static readonly string[] s_Modifiers = new[]
        {
            "Little", "Big", "Epic", "The", "Just", "Super", "Its", "Ultra", "Mega"
        };

        static readonly string[] s_Nouns = new[]
        {
            "Potato", "Hamster", "Fairy", "Bat", "Serpent", "Tank", "Rainbow", "Rocket", "Grimace"
        };

        public static string GeneratePseudo()
        {
            int style = Random.Range(0, 4);

            switch (style)
            {
                case 0:
                    // Example: "Grimdal", "Kairrin", "Fridame"
                    return $"{GetRoot()}{GetEnding()}";

                case 1:
                    // Example: "LittleDank", "JustEpic", "TheFlyingBat"
                    return $"{GetModifier()}{GetNounOrRoot()}";

                case 2:
                    // Example: "xXSerpentXx", "Viiper", "Fluxy"
                    string name = $"{GetRoot()}{GetEnding()}";
                    return Random.value > 0.5 ? $"xX{name}Xx" : $"{name}{RandomCharacter()}";

                default:
                    // Example: "TankerTanker", "Rocketman"
                    string noun = GetNounOrRoot();
                    return Random.value > 0.5 ? $"{noun}{noun}" : $"{noun}{GetEnding()}";
            }
        }

        static string GetRoot() => s_NameRoots.GetRandom();
        static string GetEnding() => s_NameEndings.GetRandom();
        static string GetModifier() => s_Modifiers.GetRandom();
        static string GetNounOrRoot() => Random.value > 0.5 ? s_Nouns.GetRandom() : GetRoot();
        static char RandomCharacter() => (char)Random.Range(97, 123); // a-z

        static T GetRandom<T>(this T[] array) => array[Random.Range(0, array.Length)];
    }
}
using Data;
using Enums;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Game.Loaders.Filters
{
    public static class AchievementFilters
    {
        public static List<T> FilterByType<T>(this IEnumerable<IAchievement> source)
        {
            return source.OfType<T>().ToList();
        }

        public static List<T> FilterByCharacter<T>(this IEnumerable<T> source, ECharacter character, bool strict = false) where T : IAchievement
        {
            return source.Where(a => (!strict && a.Character == ECharacter.None) || a.Character == character).ToList();
        }

        public static T FilterByName<T>(this IEnumerable<T> source, string name) where T : IAchievement
        {
            return source.First(data => data.GetName() == name);
        }
    }
}
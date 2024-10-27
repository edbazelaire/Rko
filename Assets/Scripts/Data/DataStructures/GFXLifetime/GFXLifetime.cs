using Enums;
using System;
using Tools;

namespace Assets.Scripts.Data.DataStructures.GFXLifetime
{
    [Serializable]
    public class GFXLifetime
    {
        public ESpellEvent  StartSpellPart;
        public ESpellEvent  EndSpellPart;
        public float        StartAt;
        public float        EndAt;
        public float        Persistance;

        public GFXLifetime(ESpellEvent startSpellSpart, ESpellEvent endSpellSpart = ESpellEvent.None, float startAt = 0f, float endAt = 1f, float persistance = 0f)
        {
            StartSpellPart = startSpellSpart;
            EndSpellPart = endSpellSpart;

            if (startAt < 0)
            {
                ErrorHandler.Error($"StartAt ({startAt}) set with value < 0 - setting by default with value 0 ");
                startAt = 0;
            }

            if (endAt < 0)
            {
                ErrorHandler.Error($"EndAt ({endAt}) set with value < 0 - setting by default with value 0 ");
                startAt = 0;
            }

            if (persistance < 0)
            {
                ErrorHandler.Error($"PersistanceAfterEnd ({persistance}) set with value < 0 - setting by default with value 0 ");
                startAt = 0;
            }

            if (startAt > 1)
            {
                ErrorHandler.Error($"StartAt ({startAt}) set with value > 1 - setting by default with value 0 ");
                startAt = 0;
            }

            if (startAt > endAt)
            {
                ErrorHandler.Error($"StartAt ({startAt}) set with value > ({endAt}) - setting with default value 0 and 1");
                startAt = 0;
                endAt = 1f;
            }

            StartAt = startAt;
            EndAt = endAt;
            Persistance = persistance;
        }
    }
}
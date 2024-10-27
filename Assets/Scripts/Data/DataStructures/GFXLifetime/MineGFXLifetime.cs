using Enums;
using Game.Spells;
using System;
using Tools;

namespace Assets.Scripts.Data.DataStructures.GFXLifetime
{
    [Serializable]
    public class MineGFXLifetime: GFXLifetime
    {
        public new EMineState StartSpellPart;
        public new EMineState EndSpellPart;

        public MineGFXLifetime(EMineState startSpellSpart, EMineState endSpellSpart = EMineState.None, float startAt = 0f, float endAt = 1f, float persistance = 0f) : base(ESpellEvent.None, ESpellEvent.None, startAt, endAt, persistance)
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
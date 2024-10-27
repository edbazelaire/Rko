using UnityEngine;

namespace Game.Spells
{
    public class PositionMarker : MonoBehaviour
    {
        #region Members


        #endregion


        #region Update Manipulators

        protected void OnTriggerEnter2D(Collider2D collider)
        {
            if (collider.gameObject.layer != LayerMask.NameToLayer("Player"))
                return;

            // apply collision effect
            Destroy(gameObject);
        }

        #endregion

    }
}
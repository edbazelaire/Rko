using Menu.Common.Filters;
using System.Collections;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.Menu.Common.Filters
{
    public class MDropdown : TMP_Dropdown
    {
        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);

            // override items UI
            SetUpDropDownIcons();
        }

        void SetUpDropDownIcons()
        {
            var content = Finder.Find(Finder.Find(gameObject, "Dropdown List"), "Content");

            foreach (Transform child in content.transform)
            {
                if (!child.gameObject.activeSelf)
                    continue;

                // split child name : "Item X: Name" into value name
                if (!child.name.Contains(": "))
                {
                    ErrorHandler.Error("Unable to parse name of the child : " + child.name);
                    child.gameObject.SetActive(false);
                    continue;
                }
                string value = child.name.Split(": ")[1];

                // ignore "Everything"
                if (value == "Everything")
                {
                    child.gameObject.SetActive(false);
                    continue;
                }

                // init label text
                var labelText = Finder.FindComponent<TMP_Text>(child.gameObject, "LabelText");
                if (labelText != null)
                {
                    labelText.text = value;
                }

                // init FilterIcon component with value
                var filterIcon = Finder.FindComponent<FilterIcon>(child.gameObject);
                if (filterIcon == null)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }
                filterIcon.Initialize(value);
            }
        }
    }
}
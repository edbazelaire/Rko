using System.Collections.Generic;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Buttons
{
    public class SubButtons : MObject
    {
        [SerializeField]
        GameObject m_ButtonPrefab;
        Button m_BackgroundButton;

        private Dictionary<string, GameObject> m_Buttons = new Dictionary<string, GameObject>();
        private bool m_IsVisible = false;

        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_BackgroundButton = Finder.FindComponent<Button>(gameObject);
        }

        public override void Initialize()
        {
            base.Initialize();

            m_BackgroundButton.onClick.AddListener(Hide);
            ClearButtons();
            Hide();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (m_BackgroundButton != null)
                m_BackgroundButton.onClick.RemoveAllListeners();
        }

        #endregion


        #region GUI Manipulators

        public void Toggle()
        {
            m_IsVisible = !m_IsVisible;
            gameObject.SetActive(m_IsVisible);
        }

        public void Hide()
        {
            m_IsVisible = false;
            gameObject.SetActive(false);
        }

        #endregion


        #region Buttons 

        public void SetButtons(List<SubButton> buttons)
        {
            ClearButtons();

            foreach (var subButton in buttons)
            {
                AddButton(subButton);
            }

            Hide();
        }

        public void AddButton(SubButton subButton)
        {
            if (m_Buttons.ContainsKey(subButton.Name))
                RemoveButton(subButton.Name);

            GameObject newButton = Instantiate(m_ButtonPrefab, transform);
            newButton.name = subButton.Name;
            Finder.FindComponent<Image>(newButton).sprite = AssetLoader.LoadButtonWithColor(subButton.Color);
            Finder.FindComponent<TMP_Text>(newButton, "Text").text = subButton.Name;

            Text buttonText = newButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = subButton.Name;

            Button btn = newButton.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => subButton.OnClick?.Invoke());

            m_Buttons[subButton.Name] = newButton;
        }

        public void RemoveButton(string name)
        {
            if (!m_Buttons.TryGetValue(name, out GameObject button)) return;

            Destroy(button);
            m_Buttons.Remove(name);
        }

        private void ClearButtons()
        {
            UIHelper.CleanContent(gameObject);

            foreach (var btn in m_Buttons.Values)
            {
                Destroy(btn);
            }
            m_Buttons.Clear();
        }

        #endregion

    }
}

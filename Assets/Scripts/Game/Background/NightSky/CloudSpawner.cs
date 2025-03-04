using System.Collections.Generic;
using Tools;
using UnityEngine;


namespace Game.Background.NightSky
{
    [System.Serializable]
    public struct SMinMax
    {
        public float Min;
        public float Max;
    }

    public class CloudSpawner : MObject
    {
        #region Members

        [SerializeField] Cloud m_CloudObject;

        [SerializeField] string m_LayerName;
        [SerializeField] int m_NLayers;
        [SerializeField] int m_BaseSorterOrder;
        [SerializeField] int m_SorterOrderLayerFactor;
        [SerializeField] float m_CloudSpeed;
        [SerializeField] SMinMax m_YPosition;
        [SerializeField] SMinMax m_CloudSize;
        [SerializeField] SMinMax m_ProcInterval;
        [SerializeField] List<Sprite> m_Clouds;

        Canvas m_Canvas;
        RectTransform m_RectTransform;

        float m_NextProcTimer;

        #endregion


        #region Init & End

        private void Awake()
        {
            Initialize();
        }

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Canvas = UIHelper.GetFirstCanvas(transform);
            m_RectTransform = Finder.FindComponent<RectTransform>(gameObject);
        }

        public override void Initialize()
        {
            base.Initialize();

            SpawnRandomClouds(6);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        #endregion


        #region Update

        private void Update()
        {
            if (!m_Initialized)
                return;

            if (m_NextProcTimer > 0)
            {
                m_NextProcTimer -= Time.deltaTime;
                return;
            }

            m_NextProcTimer = Random.Range(m_ProcInterval.Min, m_ProcInterval.Max);
            SpawnCloud(transform.position.x);
        }



        #endregion


        #region GUI Manipulators

        void SpawnRandomClouds(int nClouds)
        {
            for (int i = 0; i < nClouds; i++)
            {
                SpawnCloud(Random.Range(-3f, 3f));
            }
        }

        void SpawnCloud(float xPos)
        {
            int sorterOrder = Random.Range(0, m_NLayers + 1);

            Cloud cloud = Instantiate(m_CloudObject, transform.position, Quaternion.identity, m_Canvas.transform);
            var yPos = Random.Range(m_YPosition.Min, m_YPosition.Max);
            cloud.transform.position = new Vector3(xPos, yPos, 0f);
            cloud.Initialize(
                m_Clouds[Random.Range(0, m_Clouds.Count)], 
                m_CloudSpeed * (sorterOrder + 1 / m_NLayers + 1), 
                (sorterOrder + 1 / m_NLayers + 1) * Random.Range(m_CloudSize.Min, m_CloudSize.Max), 
                m_Canvas.sortingOrder + m_BaseSorterOrder + sorterOrder * m_SorterOrderLayerFactor,
                m_LayerName == "" ? m_Canvas.sortingLayerName : m_LayerName
            );
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
        }

        #endregion
    }
}

using Enums;
using Game.Arena;
using Game.Background;
using Network;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class ArenaManager : MonoBehaviour
    {

        #region Members

        static ArenaManager s_Instance;

        const string c_PlateformPrefix          = "Plateform_";
        const string c_SpawnerPrefix            = "SpawnPoint_";
        const string c_TargettableAreaPrefix    = "TargettableArea_";
        const string c_TargetHight              = "TargetHight";
        const int c_NumTeams                    = 2;

        GameObject              m_Arena;
        Transform               m_TargetHight;
        List<TargettableArea>   m_TargettableAreas;
        List<List<Transform>>   m_Spawns;

        ArenaBackground         m_ArenaBackground;

        float                   m_TargettableAreaSize;

        public GameObject               Arena                   => m_Arena;
        public List<List<Transform>>    Spawns                  => m_Spawns;
        public Transform                TargetHight             => m_TargetHight;
        public float                    TargettableAreaSize     => m_TargettableAreaSize;
        public ArenaBackground          ArenaBackground         => m_ArenaBackground;

        #endregion


        #region Private Manipulators

        void FindComponents()
        {
            m_Arena = gameObject;
            m_ArenaBackground = Finder.FindComponent<ArenaBackground>(gameObject);
            m_TargetHight = Finder.FindComponent<Transform>(m_Arena, c_TargetHight);
        }

        public void Initialize()
        {
            FindComponents();

            m_ArenaBackground.Initialize();
            InitializeSpawns();
            InitializeTargetabbleArea();

            s_Instance = this;
        }

        /// <summary>
        /// Initialize all spawns in the scene
        /// </summary>
        void InitializeSpawns()
        {
            m_Spawns = new List<List<Transform>>();

            List<GameObject> plateforms = Finder.Finds(Arena, c_PlateformPrefix);
            int team = 0;
            foreach (GameObject plateform in plateforms)
            {
                List<Transform> spawns = new List<Transform>();
                foreach (GameObject spawner in Finder.Finds(plateform, c_SpawnerPrefix))
                    spawns.Add(spawner.transform);

                m_Spawns.Add(spawns);
                team++;
            }

            if (m_Spawns.Count < c_NumTeams)
                ErrorHandler.FatalError("Not enough spawns for each teams");
        }

        /// <summary>
        /// Get all Targettable Areas ordered by id
        /// </summary>
        void InitializeTargetabbleArea()
        {
            m_TargettableAreas = new List<TargettableArea>();

            // re-order targettable areas by id
            int i = 0;
            bool end = false;
            while (!end)
            {
                // set end to true by default
                end = true;

                foreach (TargettableArea area in Finder.FindComponents<TargettableArea>(Arena, c_TargettableAreaPrefix))
                {
                    // if a targettable area with this id is found
                    if (area.gameObject.name.EndsWith(i.ToString()))
                    {
                        // init the are
                        area.Initialize();

                        // add it to the list
                        m_TargettableAreas.Add(area);

                        // continue to search
                        end = false;
                        break;
                    }
                }

                // increment id of target area to search
                i++;
            }

            m_TargettableAreaSize = m_TargettableAreas[0].Size;
        }

        #endregion


        #region Static Accessors

        /// <summary>
        /// Depending on the team, the enemy and ally areas are inverted
        /// </summary>
        /// <param name="team"></param>
        /// <param name="enemyArea"></param>
        /// <returns></returns>
        public static TargettableArea GetTargettableArea(int team, bool enemyArea = true)
        {
            return Instance.m_TargettableAreas[GetTargettableAreaIndex(team, enemyArea)];
        }

        /// <summary>
        /// Depending on the team, the enemy and ally areas are inverted
        /// </summary>
        /// <param name="team"></param>
        /// <param name="enemyArea"></param>
        /// <returns></returns>
        public static Transform GetTargettableAreaTransform(int team, bool enemyArea = true)
        {
            return GetTargettableArea(team, enemyArea).transform;
        }

        /// <summary>
        /// Get index of the arena side
        /// </summary>
        /// <param name="team"></param>
        /// <param name="enemyArea"></param>
        /// <returns></returns>
        public static int GetTargettableAreaIndex(int team, bool enemyArea = true)
        {
            return (team == 0 && enemyArea || team == 1 && !enemyArea) ? 1 : 0;
        }

        /// <summary>
        /// Depending on which area is selected (ally or enemy) and the team of the player, the "movement direction" (= what is consider to be "forward")
        /// of a spell will not be the same.
        /// </summary>
        /// <param name="team"></param>
        /// <param name="enemyArea"></param>
        /// <returns></returns>
        public static int GetAreaMovementDirection(int team, bool enemyArea = true)
        {
            return (team == 0 && enemyArea || team == 1 && !enemyArea) ? 1 : -1;
        }

        /// <summary>
        /// Check if xpos is in the bounds of the requested arena platform
        /// </summary>
        /// <param name="x"></param>
        /// <param name="team"></param>
        /// <param name="enemyArea"></param>
        /// <param name="marge"></param>
        /// <returns></returns>
        public static bool IsInAreaBounds(float x, int team, bool enemyArea, float marge = 0f)
        {
            (float xMin, float xMax) = GetAreaBounds(team, enemyArea);
            return xMax + marge > x && x > xMin - marge;
        } 

        /// <summary>
        /// Check if Xpos is on the requested arena side
        /// </summary>
        /// <param name="x"></param>
        /// <param name="team"></param>
        /// <param name="enemyArea"></param>
        /// <returns></returns>
        public static bool IsOnArenaSide(float x, int team, bool enemyArea)
        {
            return GetTargettableAreaIndex(team, enemyArea) == 0 ? x < 0 : x > 0;
        } 

        public static (float Min, float Max) GetAreaBounds(float xPos)
        {
            var area = (xPos <= 0 ? Instance.m_TargettableAreas[0] : Instance.m_TargettableAreas[1]).transform;
            return (area.position.x - Instance.TargettableAreaSize / 2 + (area.position.x < 0 ? 0.5f : 0.1f), area.position.x + Instance.TargettableAreaSize / 2 - (area.position.x > 0 ? 0.5f : 0.1f));
        }

        public static (float Min, float Max) GetAreaBounds(int team, bool enemyArea = true)
        {
            var area = GetTargettableAreaTransform(team, enemyArea);
            return (area.position.x - Instance.TargettableAreaSize / 2 + (area.position.x < 0 ? 0.5f : 0.1f), area.position.x + Instance.TargettableAreaSize / 2 - (area.position.x > 0 ? 0.5f : 0.1f));
        }

        public static bool IsInVoid(float x)
        {
            return x > Instance.m_TargettableAreas[0].transform.position.x + Instance.TargettableAreaSize / 2 + 0.1f
                && x < Instance.m_TargettableAreas[1].transform.position.x - Instance.TargettableAreaSize / 2 - 0.1f;
        }
 
        /// <summary>
        /// 
        /// </summary>
        public static ArenaManager Instance => s_Instance;

        public static void Clear()
        {
            ArenaManager arenaManager = s_Instance;
            if (arenaManager == null)
            {
                arenaManager = FindAnyObjectByType<ArenaManager>();
            }

            if (arenaManager == null)
                return;

            s_Instance = null;
            Destroy(arenaManager.gameObject);
        }

        #endregion

    }
}
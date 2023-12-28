﻿using Game;
using Game.Managers;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Character", menuName = "Game/Character")]
    public class CharacterData : ScriptableObject
    {
        public ECharacter       Name;
        public Sprite           Image;
        public GameObject       Prefab;
        public List<SpellData>  SpellData;

        public GameObject Instantiate(int team, bool isPlayer, Transform spawn, Vector3 position = default, Quaternion rotation = default)
        {
            var character = GameObject.Instantiate(Prefab, position, default, spawn);

            // get controller of the character
            Controller controller = character.GetComponent<Controller>();
            if (!Checker.NotNull(controller))
                return null;

            // init controller with a new health bar and add to list of controllers
            HealthBar healthBar = GameUIManager.Instance.CreateHealthBar(team);
            if (!Checker.NotNull(healthBar))
                return null;

            controller.Initialize(Name, team, isPlayer, team == 0, healthBar);

            return character;
        }
    }
}
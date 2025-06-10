//using UnityEditor;
//using UnityEngine;
//using System.Reflection;
//using System.Collections.Generic;

//public class SpellDataSearcher: EditorWindow
//{
//    [MenuItem("Tools/Find StateEffect")]
//    private static void FindSpellDataWithEnergy()
//    {
//        string[] guids = AssetDatabase.FindAssets("t:StateEffect");

//        List<Object> matchingAssets = new List<Object>();

//        List<string> fields = new() {
//            //"m_IsTrueDamage",
//            //"m_CooldownReduction",
//            //"m_CooldownReductionPerc",
//            //"m_Shield",
//            //"m_ResistanceFix",
//            //"m_ResistancePerc",
//            //"m_BonusDamage",
//            //"m_BonusDamagePerc",
//            //"m_BonusLifeSteal",
//            //"m_BonusHeal",
//            //"m_BonusHealPerc",
//            //"m_BonusBurnDamage",
//            //"m_BonusSlowPerc",
//            "m_VisualEffects",
//        };

//        foreach (string fieldName in fields)
//        {
//            foreach (string guid in guids)
//            {
//                string path = AssetDatabase.GUIDToAssetPath(guid);
//                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

//                if (asset == null) continue;

//                var type = asset.GetType();

//                FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
//                if (field == null) continue;

//                object fieldValue = field.GetValue(asset);
//                if (fieldValue == null) continue;

//                if (field.FieldType == typeof(int))
//                {
//                    int value = (int)fieldValue;
//                    if (value != 0)
//                    {
//                        Debug.Log($"Found: {asset.name} at {path} with {fieldName} = {value}", asset);
//                        matchingAssets.Add(asset);
//                    }
//                } 
                
//                else if (field.FieldType == typeof(float))
//                {
//                    float value = (float)fieldValue;
//                    if (value != 0)
//                    {
//                        Debug.Log($"Found: {asset.name} at {path} with {fieldName} = {value}", asset);
//                        matchingAssets.Add(asset);
//                    }
//                }

//                else if (field.FieldType == typeof(bool))
//                {
//                    bool value = (bool)fieldValue;
//                    if (value != false)
//                    {
//                        Debug.Log($"Found: {asset.name} at {path} with {fieldName} = {value}", asset);
//                        matchingAssets.Add(asset);
//                    }
//                }

//                else if (field.FieldType == typeof(string))
//                {
//                    string value = (string)fieldValue;
//                    if (value == "")
//                    {
//                        Debug.Log($"Found: {asset.name} at {path} with {fieldName} = {value}", asset);
//                        matchingAssets.Add(asset);
//                    }
//                }

//                else if (fieldValue is System.Collections.IList list)
//                {
//                    if (list.Count > 0)
//                    {
//                        Debug.Log($"[Array] Found: {asset.name} at {path} with {fieldName} length = {list.Count}", asset);
//                        matchingAssets.Add(asset);
//                    }
//                }

//                else
//                {
//                    Debug.LogWarning($"Unhandled type {field.FieldType} for property {fieldName}");
//                }
//            }
//        }

//        if (matchingAssets.Count > 0)
//        {
//            Selection.objects = matchingAssets.ToArray(); // auto-select in Project
//        }
//        else
//        {
//            Debug.Log("No StateEffect assets found with any of these fields");
//        }
//    }
//}

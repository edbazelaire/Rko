using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using Tools;
using System.Reflection;
using Enums;
using System;
using System.Linq;

public class AnimationBoneRenamer : EditorWindow
{
    private GameObject originalObject;
    private GameObject newObject;

    [MenuItem("Tools/Animation Bone Renamer")]
    public static void ShowWindow()
    {
        GetWindow<AnimationBoneRenamer>("Animation Bone Renamer");
    }

    void OnGUI()
    {
        GUILayout.Label("Remap Animation Bones", EditorStyles.boldLabel);
        originalObject = (GameObject)EditorGUILayout.ObjectField("Original Object", originalObject, typeof(GameObject), true);
        newObject = (GameObject)EditorGUILayout.ObjectField("New Object", newObject, typeof(GameObject), true);

        if (GUILayout.Button("Remap Bones"))
        {
            RemapBones();
        }
    }

    void RemapBones()
    {
        if (originalObject == null || newObject == null)
        {
            Debug.LogError("Original or New Object is not set.");
            return;
        }

        Dictionary<string, string> boneNameMap = new Dictionary<string, string>();

        
        Transform[] originalBones = Finder.FindComponents<Transform>(originalObject, "bone_").ToArray();
        Transform[] newBones = Finder.FindComponents<Transform>(newObject, "bone_").ToArray();

        int boneIndex = 0;
        foreach (Transform newBone in newBones)
        {
            if (boneIndex >= originalBones.Length || boneIndex > newBones.Length)
                break;

            boneNameMap[originalBones[boneIndex].name] = newBones[boneIndex].name;
            boneIndex++;
        }

        // rename the animation with new bones names
        var animator = Finder.FindComponent<Animator>(originalObject);
        var animatorController = animator.runtimeAnimatorController;
        foreach (var animationClip in animatorController.animationClips)
        {
            RemapAnimationClip(animationClip, boneNameMap);
        }

        // rename the bones once its done
        boneIndex = 0;
        foreach (Transform newBone in newBones)
        {
            if (boneIndex >= originalBones.Length || boneIndex > newBones.Length)
                break;

            originalBones[boneIndex].name = newBone.name;
            boneIndex++;
        }
    }

    void RemapAnimationClip(AnimationClip clip, Dictionary<string, string> boneNameMap)
    {
        // Collect old bindings to remove later

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (EditorCurveBinding binding in bindings)
        {
            string[] pathParts = binding.path.Split('/');
            bool pathChanged = false;

            for (int i = 0; i < pathParts.Length; i++)
            {
                if (boneNameMap.ContainsKey(pathParts[i]))
                {
                    pathParts[i] = boneNameMap[pathParts[i]];
                    pathChanged = true;
                }
            }

            if (pathChanged)
            {
                string newPath = string.Join("/", pathParts);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                EditorCurveBinding newBinding = binding;
                newBinding.path = newPath;
                AnimationUtility.SetEditorCurve(clip, newBinding, curve);
            }
        }
    }

    public static void AddEffectorsToBones(GameObject root)
    {
        foreach (EBodyPart bodyPart in Enum.GetValues(typeof(EBodyPart)))
        {
            if (bodyPart == EBodyPart.None)
                continue;

            string effectorName = bodyPart.ToString() + "Effector";
            // check already existing
            if (root.transform.Find(effectorName) != null)
                continue;

            string boneName = bodyPart != EBodyPart.SpellSpawn ? "bone_" + bodyPart.ToString() : "bone_" + EBodyPart.R_Hand.ToString();

            // Find the bone by name
            Transform boneTransform = Finder.FindComponent<Transform>(root, boneName);
            if (boneTransform == null)
                continue;    

            // Create the effector GameObject
            GameObject effector = new GameObject(effectorName);

            // Set the effector as a child of the bone
            effector.transform.SetParent(boneTransform);
            effector.transform.localPosition = Vector3.zero;
            effector.transform.localRotation = Quaternion.identity;

            Debug.Log($"Added {effector.name} to {boneName}");
        }
    }
}

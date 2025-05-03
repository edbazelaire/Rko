using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tools
{
    public static class Finder
    {
        public static GameObject Find(string name, bool throwError = true)
        {
            var gameObject = GameObject.Find(name);

            if (throwError && !Checker.NotNull(gameObject))
                return null;

            return gameObject;
        }

        public static GameObject Find(GameObject parent,  string name, bool throwError = true)
        {
            if (parent == null)
            {
                if (throwError)
                    ErrorHandler.Error("Provided parent " + parent.name + " is null when looking for " + name);
                return null;
            }

            foreach (Transform child in parent.transform)
            {
                if (child.name == name)
                    return child.gameObject;

                // [RECURSIVE] check of child's children
                if (child.childCount > 0)
                {
                    GameObject childFound = Find(child.gameObject, name, false);
                    if (childFound != null)
                        return childFound;
                }
            }

            if (throwError)
                ErrorHandler.Error("No child with name " + name + " found in " + parent.name);

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="throwError"></param>
        /// <returns></returns>
        public static List<GameObject> Finds(GameObject parent, string name, bool throwError = true)
        {
            // find all child with that name in parent
            List<GameObject> list = new List<GameObject>();

            if (parent == null)
                return list;

            foreach (Transform child in parent.transform)
            {
                // if name is prefix, get all childs with such prefix
                if (name.EndsWith("_"))
                {
                    if (child.name.StartsWith(name))
                        list.Add(child.gameObject);
                }
                // otherwise get exact match
                else if (child.name == name)
                    list.Add(child.gameObject);

                // [RECURSIVE] check of child's children
                if (child.childCount > 0)
                    list.AddRange(Finds(child.gameObject, name, false));        // deactivate error durring recursivity
            }

            if (throwError && !Checker.CheckEmpty(list))
                ErrorHandler.Error("Unable to find any child named " + name + " in parent " + parent.name);

            return list;
        }

        public static T FindComponent<T>(string name, bool throwError = true)
        {
            GameObject go = Find(name, throwError);
            if (go == null)
                return default(T);

            var component = go.GetComponent<T>();

            if (throwError && !Checker.NotNull(component))
                return default(T);

            return component;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="throwError"></param>
        /// <returns></returns>
        public static T FindComponent<T>(GameObject parent, string name = "", bool throwError = true)
        {
            // No name : find first with request component
            if (name == "")
            {
                var component = parent.GetComponent<T>();
                if (component != null && component.ToString() != "null")
                    return component;

                foreach  (Transform child in parent.transform)
                {
                    component = child.GetComponent<T>();
                    if (component != null && component.ToString() != "null")
                        return component;

                    if (child.childCount > 0)
                        component = FindComponent<T>(child.gameObject, "", false);        // deactivate error durring recursivity

                    if (component != null && component.ToString() != "null")
                        return component;
                }
                
                if (throwError && !Checker.NotNull(component))
                {
                    ErrorHandler.Error("Unable to find any game object with component " + typeof(T).ToString() + " in parent " + parent.name);
                    return default;
                }

                return component;
            }

            // Name provided : check all childs with requested name and find the one with the requested component
            else
            {
                var childs = Finds(parent, name, throwError);       // already recusive & already handle error display

                if (childs.Count == 0)
                    return default;

                foreach (GameObject child in childs)
                {
                    var component = child.GetComponent<T>();
                    if (component != null)
                        return component;
                }
            }

            if (throwError)
                ErrorHandler.Error("GameObject " + name + " had no component " + typeof(T).ToString() + " in parent " + parent.name);

            return default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="throwError"></param>
        /// <returns></returns>
        public static List<T> FindComponents<T>(GameObject parent, string name = "", bool throwError = true)
        {
            if (name == "")
            {
                var childComponents = parent.transform.GetComponentsInChildren<T>(true);

                if (throwError && !Checker.CheckSize<T>(childComponents, 1, atLeast: true))
                    return new List<T>();

                return new List<T>(childComponents);
            }

            List<T> components = new List<T>();
            var childs = Finds(parent, name, throwError);       // already recusive
            foreach (GameObject child in childs)
            {
                var component = child.GetComponent<T>();
                if (component != null)
                    components.Add(component);
            }

            if (throwError)
                Checker.CheckEmpty(components);

            return components;
        }

        public static void CleanContent(GameObject gameObject)
        {
            foreach (Transform child in gameObject.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }
    }
}
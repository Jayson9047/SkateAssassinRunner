using UnityEditor;
using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.MeshExtractor
{
    static class HierarchyPathUtils
    {
        static List<Transform> s_tmpList = new List<Transform>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        /// <param name="separator">TAB by default because that can not be easily used in the hierarchy.</param>
        /// <returns></returns>
        public static Transform ResolveRelativePath(Transform root, string path, char separator = '\t')
        {
            if (string.IsNullOrEmpty(path))
                return root;

            var childChain = path.Split(separator);
            Transform current = root;
            for (int i = 0; i < childChain.Length; i+=2)
            {
                string childName = childChain[i];
                if (!int.TryParse(childChain[i + 1], out int childSiblingIndex))
                    continue;

                var siblings = getSameNameChildren(current, childName, s_tmpList, clearResults: true);
                if (childSiblingIndex >= s_tmpList.Count)
                    continue;

                current = s_tmpList[childSiblingIndex];
            }
            s_tmpList.Clear();

            return current;
        }

        /// <summary>
        /// Path will look like this (separator = ","): root,0,child,0,sub-child,4,...
        /// Read as: Name,Same-Name-Index,Name,Same-Name-Index, ...
        /// </summary>
        /// <param name="root"></param>
        /// <param name="target"></param>
        /// <param name="separator">TAB by default because that can not be easily used in the hierarchy.</param>
        /// <returns></returns>
        public static string GetRelativePath(Transform root, Transform target, char separator = '\t')
        {
            string path = null;

            if (target == null)
                return path;

            // Path will look like this: root\t0\tchild\t0\tsub-child\t4...
            var current = target;
            path = "";
            while (current != root && current != null)
            {
                if (string.IsNullOrEmpty(path))
                    path = current.name + separator + getSameNameSiblingIndex(current);
                else
                    path = current.name + separator + getSameNameSiblingIndex(current) + separator + path;
                current = current.parent;
            }

            if (current == root)
                return path;
            else
                return null;
        }

        static int getSameNameSiblingIndex(Transform child)
        {
            int count = 0;
            if (child.parent != null)
            {
                for (int i = 0; i < child.parent.childCount; ++i)
                {
                    if (child.parent.GetChild(i).name == child.name && child.parent.GetChild(i).GetSiblingIndex() < child.GetSiblingIndex())
                    {
                        count++;
                    }
                }
            }
            else
            {
                var rootObjects = child.gameObject.scene.GetRootGameObjects();
                for (int i = 0; i < rootObjects.Length; ++i)
                {
                    if (rootObjects[i].name == child.name && rootObjects[i].transform.GetSiblingIndex() < child.GetSiblingIndex())
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        static List<Transform> getSameNameChildren(Transform parent, string name, List<Transform> results = null, bool clearResults = true)
        {
            if (results == null)
            {
                results = new List<Transform>();
            }
            else
            {
                if (clearResults)
                    results.Clear();
            }

            for (int i = 0; i < parent.childCount; ++i)
            {
                if (parent.GetChild(i).name == name)
                {
                    results.Add(parent.GetChild(i));
                }
            }
            return results;
        }

        public static string GetComponentPath(Component component, char separator = '\t')
        {
            if (component == null)
                return null;

            string path = null;
            var components = component.transform.GetComponents(component.GetType());
            // loop until we find the component (i = the index)
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == component)
                {
                    path = component.GetType().FullName + separator + i;
                    break;
                }
            }

            return path;
        }

        /// <summary>
        /// Returns NULL if path is empty. In that case maybe the path belongs to a GameObject and not a Component.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="path"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static Component ResolveComponentPath(Transform transform, string path, char separator = '\t')
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var parts = path.Split(separator);
            string typeName = parts[0];
            if (!int.TryParse(parts[1], out var index))
                return null;

            var components = transform.GetComponents<Component>();
            // loop until we find the component (i = the index)
            int i = -1;
            for (int c = 0; c < components.Length; c++)
            {
                if (components[c].GetType().FullName == typeName)
                {
                    i++;
                    if (i == index)
                    {
                        return components[c];
                    }
                }
            }

            return null;
        }
    }
}

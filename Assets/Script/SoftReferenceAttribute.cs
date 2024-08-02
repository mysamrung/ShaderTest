using UnityEngine;
using System;

namespace Xeen.AssetReference
{
    [System.Serializable]
    public class SoftReference<T>
    {
        [HideInInspector]
        public string assetAddress;

        public Type GetReferenceType()
        {
            return typeof(T);
        }
    }

    public class SoftReferenceAttribute : PropertyAttribute
    {
        public Type referencedType;
        public SoftReferenceAttribute(Type type)
        {
            this.referencedType = type;
        }
    }
}
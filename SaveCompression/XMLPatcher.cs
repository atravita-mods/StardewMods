using System.Reflection;
using System.Xml.Serialization;

using HarmonyLib;

using Microsoft.Xna.Framework;

namespace SaveCompression;

[HarmonyPatch(typeof(XmlReflectionImporter))]
internal static class XMLPatcher
{

    [HarmonyPatch("GetAttributes", [typeof(MemberInfo)])]
    private static void Postfix(XmlReflectionImporter __instance, MemberInfo memberInfo, XmlAttributes __result)
    {
        if (__result.XmlIgnore || __result.XmlDefaultValue is not null)
        {
            return;
        }

        if (memberInfo is FieldInfo fieldInfo)
        {
            EditXMLAttr(fieldInfo.FieldType, __result);
        }
        else if (memberInfo is PropertyInfo propertyInfo)
        {
            EditXMLAttr(propertyInfo.PropertyType, __result);
        }

    }

    private static void EditXMLAttr(Type type, XmlAttributes attr)
    {
        if (type == typeof(Vector2))
        {
            attr.XmlDefaultValue = Vector2.Zero;
        }
        else if (type == typeof(Point))
        {
            attr.XmlDefaultValue = Point.Zero;
        }
        else if (type == typeof(int) || type == typeof(long) || type == typeof(uint) || type == typeof(ulong))
        {
            attr.XmlDefaultValue = 0;
        }
        else if (type == typeof(float))
        {
            attr.XmlDefaultValue = 0f;
        }
        else if (type == typeof(double))
        {
            attr.XmlDefaultValue = 0.0;
        }
        else if (type == typeof(bool))
        {
            attr.XmlDefaultValue = false;
        }
        else if (type == typeof(Color))
        {
            attr.XmlDefaultValue = Color.White;
        }
        else
        {
             ModEntry.ModMonitor.LogOnce(type.FullName ?? "");
        }
    }
}

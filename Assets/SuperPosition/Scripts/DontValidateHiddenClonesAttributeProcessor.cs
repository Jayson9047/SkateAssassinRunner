#if ODIN_VALIDATOR_3_1
using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

//[ExtensionOfNativeClass]
public class DontValidateHiddenClonesAttributeProcessor : OdinAttributeProcessor<object>
{
    public override bool CanProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member)
    {
        return false;
    }

    public override bool CanProcessSelfAttributes(InspectorProperty property)
    {
        // Check what the root is; we only tell the validator to not validate for all properties
        // that are on a clone root object.
        UnityEngine.Object target = property.Tree.WeakTargets[0] as UnityEngine.Object;
        return target != null;
    }

    public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
    {
        attributes.Add(new DontValidateAttribute());
    }
}
#endif
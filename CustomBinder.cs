using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;

namespace Diaxic
{
    public class CustomBinder : DefaultSerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            switch (typeName)
            {
                case "SavedData": return typeof(SavedData);
                case "NodeData": return typeof(NodeData);
                case "LineData": return typeof(LineData);
                case "DialogueLineData": return typeof(DialogueLineData);
                case "ActionLineData": return typeof(ActionLineData);
                case "GoToLineData": return typeof(GoToLineData);
                case "ConditionalType": return typeof(ConditionalType);
                case "ConditionalLineData": return typeof(ConditionalLineData);
                case "ChoiceData": return typeof(ChoiceData);
                case "System.Collections.Generic.List`1[[NodeData, Assembly-CSharp]]": return typeof(List<NodeData>);
                case "System.Collections.Generic.List`1[[LineData, Assembly-CSharp]]": return typeof(List<LineData>);
                case "System.Collections.Generic.List`1[[ConditionalLineData, Assembly-CSharp]]": return typeof(List<ConditionalLineData>);
                case "Diaxic.SavedData": return typeof(SavedData);
                case "Diaxic.NodeData": return typeof(NodeData);
                case "Diaxic.LineData": return typeof(LineData);
                case "Diaxic.DialogueLineData": return typeof(DialogueLineData);
                case "Diaxic.ActionLineData": return typeof(ActionLineData);
                case "Diaxic.GoToLineData": return typeof(GoToLineData);
                case "Diaxic.ConditionalType": return typeof(ConditionalType);
                case "Diaxic.ConditionalLineData": return typeof(ConditionalLineData);
                case "Diaxic.ChoiceData": return typeof(ChoiceData);
                case "System.Collections.Generic.List`1[[Diaxic.NodeData, Assembly-CSharp-firstpass]]": return typeof(List<NodeData>);
                case "System.Collections.Generic.List`1[[Diaxic.LineData, Assembly-CSharp-firstpass]]": return typeof(List<LineData>);
                case "System.Collections.Generic.List`1[[Diaxic.ConditionalLineData, Assembly-CSharp-firstpass]]": return typeof(List<ConditionalLineData>);
#if UNITY_EDITOR || UNITY_STANDALONE
                case "UnityEngine.Vector3": return typeof(UnityEngine.Vector3);
#endif
                default: return base.BindToType(assemblyName, typeName);
            }
        }

        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.FullName;
        }
    }
}

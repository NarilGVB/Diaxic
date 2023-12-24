using System;
using System.Collections.Generic;

#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
#endif

namespace Diaxic
{
    [Serializable]
    public class SavedData
    {
        public string version;
        public List<NodeData> story;
        public List<string> variables;
        public List<string> variablesValues;
    }

    [Serializable]
    public class NodeData
    {
        public string Id => string.IsNullOrEmpty(name) ? index.ToString() : name;

        public int index = -1;
        public string name;
    #if UNITY_EDITOR || UNITY_STANDALONE
        [SerializeReference]
    #endif
        public List<LineData> lines;
    #if UNITY_EDITOR || UNITY_STANDALONE
        [SerializeReference]
    #endif
        public List<LineData> choices;
    #if UNITY_EDITOR || UNITY_STANDALONE
        public Vector3 position;
    #endif
    }

    [Serializable]
    public class LineData
    {
        public int index = -1;
    }

    [Serializable]
    public class DialogueLineData : LineData
    {
        public string speaker;
    #if UNITY_EDITOR || UNITY_STANDALONE
        [Multiline]
    #endif
        public string text;
    }

    [Serializable]
    public class ActionLineData : LineData
    {
    #if UNITY_EDITOR || UNITY_STANDALONE
        [Multiline]
    #endif
        public string text;
    }

    [Serializable]
    public class GoToLineData : LineData
    {
        public int targetIndex = -1;
    }

    public enum ConditionalType {If, ElseIf, Else}

    [Serializable]
    public class ConditionalLineData : LineData
    {
        public ConditionalType conditionalType;
        public string comparison;
    #if UNITY_EDITOR || UNITY_STANDALONE
        [SerializeReference]
    #endif
        public List<ConditionalLineData> nestedConditionals;
    #if UNITY_EDITOR || UNITY_STANDALONE
        [SerializeReference]
    #endif
        public List<LineData> lines;
    }

    [Serializable]
    public class ChoiceData : LineData
    {
        public string text;
        public int targetIndex = -1;
    }
}